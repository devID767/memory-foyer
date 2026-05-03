import { test, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import request from 'supertest';
import Database from 'better-sqlite3';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { createApp } from './server.js';
import { seed } from './seed.js';
import { migrate } from './db.js';

const here = dirname(fileURLToPath(import.meta.url));
const schemaSql = readFileSync(join(here, 'schema.sql'), 'utf8');

const FIXED_NOW = new Date('2026-05-03T12:00:00.000Z');

let db;
let app;

beforeEach(() => {
    db = new Database(':memory:');
    db.pragma('foreign_keys = ON');
    db.exec(schemaSql);
    seed(db);
    app = createApp({ db, now: () => FIXED_NOW });
});

const SAMPLE_SESSION = '11111111-1111-4111-8111-111111111111';
const ANOTHER_SESSION = '22222222-2222-4222-8222-222222222222';
const sampleReview = (cardId, grade) => ({
    cardId,
    grade,
    reviewedAt: '2026-05-03T11:00:00.000Z',
});

test('GET /health returns ok', async () => {
    const res = await request(app).get('/health');
    assert.equal(res.status, 200);
    assert.equal(res.body.status, 'ok');
    assert.ok(res.body.version);
});

test('GET /decks lists three seeded decks with counts', async () => {
    const res = await request(app).get('/decks');
    assert.equal(res.status, 200);
    assert.equal(res.body.length, 3);
    const capitals = res.body.find((d) => d.deckId === 'capitals-eu');
    assert.equal(capitals.totalCount, 44);
    assert.equal(capitals.newCount, 10);
    assert.equal(capitals.dueCount, 10);
});

test('GET /decks/:id/schedule returns capped new pool', async () => {
    const res = await request(app).get('/decks/capitals-eu/schedule');
    assert.equal(res.status, 200);
    assert.equal(res.body.deckId, 'capitals-eu');
    assert.equal(res.body.cards.length, 10);
    for (const c of res.body.cards) {
        assert.equal(c.stage, 'new');
        assert.match(c.dueAt, /Z$/);
    }
});

test('GET /decks/unknown/schedule → 404 unknown-deck', async () => {
    const res = await request(app).get('/decks/nope/schedule');
    assert.equal(res.status, 404);
    assert.equal(res.body.error, 'unknown-deck');
});

test('POST /sessions advances schedule and returns full deck snapshot', async () => {
    const body = {
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals-eu',
        reviews: [sampleReview('capitals-eu:1', 4)],
    };
    const res = await request(app).post('/sessions').send(body);
    assert.equal(res.status, 200);
    assert.equal(res.body.ok, true);
    assert.equal(res.body.dedup, undefined);
    assert.equal(res.body.updatedSchedule.deckId, 'capitals-eu');
    assert.equal(res.body.updatedSchedule.cards.length, 44);
    const updated = res.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals-eu:1');
    assert.equal(updated.stage, 'learning');
    assert.equal(updated.learningStep, 1);
});

test('POST /sessions retry with same payload → dedup', async () => {
    const body = {
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals-eu',
        reviews: [sampleReview('capitals-eu:1', 4)],
    };
    const first = await request(app).post('/sessions').send(body);
    assert.equal(first.status, 200);
    const second = await request(app).post('/sessions').send(body);
    assert.equal(second.status, 200);
    assert.equal(second.body.dedup, true);
    assert.deepEqual(second.body.updatedSchedule, first.body.updatedSchedule);

    const logs = db.prepare('SELECT COUNT(*) AS n FROM review_log WHERE session_id = ?').get(SAMPLE_SESSION);
    assert.equal(logs.n, 1, 'review_log not duplicated on dedup');
});

test('POST /sessions retry with different payload → 409', async () => {
    const first = {
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals-eu',
        reviews: [sampleReview('capitals-eu:1', 4)],
    };
    await request(app).post('/sessions').send(first);
    const conflicting = { ...first, reviews: [sampleReview('capitals-eu:1', 5)] };
    const res = await request(app).post('/sessions').send(conflicting);
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'session-payload-mismatch');
});

test('POST /sessions with grade=2 → 400 validation', async () => {
    const res = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals-eu',
        reviews: [{ cardId: 'capitals-eu:1', grade: 2, reviewedAt: '2026-05-03T11:00:00.000Z' }],
    });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'validation');
    assert.ok(Array.isArray(res.body.details));
});

test('POST /sessions with unknown cardId → 400 unknown-card', async () => {
    const res = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals-eu',
        reviews: [sampleReview('capitals-eu:9999', 4)],
    });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'unknown-card');
});

test('GET /sessions/:id returns processed session', async () => {
    await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals-eu',
        reviews: [sampleReview('capitals-eu:1', 4)],
    });
    const res = await request(app).get(`/sessions/${SAMPLE_SESSION}`);
    assert.equal(res.status, 200);
    assert.equal(res.body.sessionId, SAMPLE_SESSION);
    assert.equal(res.body.deckId, 'capitals-eu');
    assert.equal(res.body.reviews.length, 1);
    assert.equal(res.body.reviews[0].cardId, 'capitals-eu:1');
});

test('GET /sessions/:id unknown → 404', async () => {
    const res = await request(app).get(`/sessions/${ANOTHER_SESSION}`);
    assert.equal(res.status, 404);
    assert.equal(res.body.error, 'unknown-session');
});

test('Migration v1 retags pre-existing learning+reps>0 rows as relearning', () => {
    const legacyDb = new Database(':memory:');
    legacyDb.pragma('foreign_keys = ON');
    legacyDb.exec(schemaSql);
    seed(legacyDb);
    legacyDb.prepare(
        `UPDATE card_schedules SET stage='learning', reps=4 WHERE card_id='capitals-eu:1'`
    ).run();
    legacyDb.prepare(
        `UPDATE card_schedules SET stage='learning', reps=0 WHERE card_id='capitals-eu:2'`
    ).run();
    assert.equal(legacyDb.pragma('user_version', { simple: true }), 0);

    migrate(legacyDb);

    assert.equal(legacyDb.pragma('user_version', { simple: true }), 1);
    const lapsed = legacyDb.prepare('SELECT stage FROM card_schedules WHERE card_id=?').get('capitals-eu:1');
    assert.equal(lapsed.stage, 'relearning');
    const fresh = legacyDb.prepare('SELECT stage FROM card_schedules WHERE card_id=?').get('capitals-eu:2');
    assert.equal(fresh.stage, 'learning');
});

test('Relearning roundtrip: lapse → relearning, then graduate preserves reps', async () => {
    db.prepare(
        `UPDATE card_schedules
         SET stage='review', reps=3, ease_factor=2.5, interval_days=10, due_at=?
         WHERE card_id='capitals-eu:1'`
    ).run('2026-05-03T11:00:00.000Z');

    const lapse = await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals-eu',
        reviews: [{ cardId: 'capitals-eu:1', grade: 0, reviewedAt: '2026-05-03T11:30:00.000Z' }],
    });
    assert.equal(lapse.status, 200);
    const afterLapse = lapse.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals-eu:1');
    assert.equal(afterLapse.stage, 'relearning');
    assert.equal(afterLapse.reps, 3);
    assert.ok(Math.abs(afterLapse.easeFactor - 2.30) < 1e-9);

    const step = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals-eu',
        reviews: [{ cardId: 'capitals-eu:1', grade: 4, reviewedAt: '2026-05-03T11:40:00.000Z' }],
    });
    assert.equal(step.status, 200);
    const afterStep = step.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals-eu:1');
    assert.equal(afterStep.stage, 'relearning');
    assert.equal(afterStep.learningStep, 1);
    assert.equal(afterStep.reps, 3);

    const graduate = await request(app).post('/sessions').send({
        sessionId: '33333333-3333-4333-8333-333333333333',
        deckId: 'capitals-eu',
        reviews: [{ cardId: 'capitals-eu:1', grade: 4, reviewedAt: '2026-05-03T11:50:00.000Z' }],
    });
    assert.equal(graduate.status, 200);
    const afterGraduate = graduate.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals-eu:1');
    assert.equal(afterGraduate.stage, 'review');
    assert.equal(afterGraduate.reps, 3, 'graduate from relearning must preserve reps');
    assert.equal(afterGraduate.intervalDays, 1);
});
