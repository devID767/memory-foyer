import { test, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import request from 'supertest';
import Database from 'better-sqlite3';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { createApp } from './server.js';
import { applySeed, loadDeckRegistry, findOrphans } from './registry.js';
import { migrate } from './db.js';

const here = dirname(fileURLToPath(import.meta.url));
const schemaSql = readFileSync(join(here, 'schema.sql'), 'utf8');

const FIXED_NOW = new Date('2026-05-03T12:00:00.000Z');

const FIXTURE_DECKS = [
    {
        deckId: 'capitals',
        displayName: 'Capitals of Europe',
        description: 'Test capitals.',
        newCardsPerDay: 10,
        cardIds: ['capitals:1', 'capitals:2', 'capitals:3'],
    },
    {
        deckId: 'idioms',
        displayName: 'English Idioms',
        description: 'Test idioms.',
        newCardsPerDay: 8,
        cardIds: ['idioms:1', 'idioms:2'],
    },
    {
        deckId: 'periodic',
        displayName: 'Periodic Table',
        description: 'Test periodic.',
        newCardsPerDay: 5,
        cardIds: ['periodic:1', 'periodic:2'],
    },
];

let db;
let app;
let currentNow;

const CAP_TEST_DECK = {
    deckId: 'cap-test',
    displayName: 'Cap Test',
    description: 'Daily-cap fixture (cards > newCardsPerDay).',
    newCardsPerDay: 2,
    cardIds: ['cap-test:1', 'cap-test:2', 'cap-test:3', 'cap-test:4', 'cap-test:5'],
};

beforeEach(() => {
    db = new Database(':memory:');
    db.pragma('foreign_keys = ON');
    db.exec(schemaSql);
    applySeed(db, FIXTURE_DECKS);
    currentNow = FIXED_NOW;
    app = createApp({ db, now: () => currentNow });
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
    const capitals = res.body.find((d) => d.deckId === 'capitals');
    assert.equal(capitals.totalCount, 3);
    assert.equal(capitals.newCount, 3);
    assert.equal(capitals.dueCount, 3);
});

test('GET /decks/:id/schedule returns capped new pool', async () => {
    const res = await request(app).get('/decks/capitals/schedule');
    assert.equal(res.status, 200);
    assert.equal(res.body.deckId, 'capitals');
    assert.equal(res.body.cards.length, 3);
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

test('POST /sessions advances schedule and returns released-subset snapshot', async () => {
    // No prior GET → unreleased new cards (capitals:2, capitals:3) are filtered out;
    // capitals:1 transitions to 'learning' on grading and passes the filter.
    const body = {
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals',
        reviews: [sampleReview('capitals:1', 4)],
    };
    const res = await request(app).post('/sessions').send(body);
    assert.equal(res.status, 200);
    assert.equal(res.body.ok, true);
    assert.equal(res.body.dedup, undefined);
    assert.equal(res.body.updatedSchedule.deckId, 'capitals');
    assert.equal(res.body.updatedSchedule.cards.length, 1);
    const updated = res.body.updatedSchedule.cards[0];
    assert.equal(updated.cardId, 'capitals:1');
    assert.equal(updated.stage, 'learning');
    assert.equal(updated.learningStep, 1);
});

test('POST /sessions updatedSchedule equals subsequent GET /:id/schedule', async () => {
    applySeed(db, [CAP_TEST_DECK]);

    const firstGet = await request(app).get('/decks/cap-test/schedule');
    assert.equal(firstGet.status, 200);
    assert.equal(firstGet.body.cards.length, 2, 'GET releases exactly newCardsPerDay');

    const post = await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'cap-test',
        reviews: [sampleReview('cap-test:1', 4)],
    });
    assert.equal(post.status, 200);

    const secondGet = await request(app).get('/decks/cap-test/schedule');
    assert.equal(secondGet.status, 200);
    assert.deepEqual(
        post.body.updatedSchedule.cards,
        secondGet.body.cards,
        'updatedSchedule must match a fresh GET — same released-subset filter',
    );
    assert.equal(post.body.updatedSchedule.cards.length, 2);
});

test('POST /sessions dedup retry returns stored snapshot unaffected by quota shift', async () => {
    applySeed(db, [CAP_TEST_DECK]);

    currentNow = new Date('2026-05-03T12:00:00.000Z');
    await request(app).get('/decks/cap-test/schedule');
    const original = await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'cap-test',
        reviews: [sampleReview('cap-test:1', 4)],
    });
    assert.equal(original.status, 200);
    assert.equal(original.body.updatedSchedule.cards.length, 2);

    // Cross UTC day boundary WITHOUT calling GET on day 2 — released_on for
    // cap-test:3..5 is still NULL. The dedup retry must replay the exact
    // snapshot stored at first POST, not re-derive it from current DB state.
    currentNow = new Date('2026-05-04T12:00:00.000Z');
    const retry = await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'cap-test',
        reviews: [sampleReview('cap-test:1', 4)],
    });
    assert.equal(retry.status, 200);
    assert.equal(retry.body.dedup, true);
    assert.deepEqual(retry.body.updatedSchedule, original.body.updatedSchedule);
});

test('POST /sessions retry with same payload → dedup', async () => {
    const body = {
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals',
        reviews: [sampleReview('capitals:1', 4)],
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
        deckId: 'capitals',
        reviews: [sampleReview('capitals:1', 4)],
    };
    await request(app).post('/sessions').send(first);
    const conflicting = { ...first, reviews: [sampleReview('capitals:1', 5)] };
    const res = await request(app).post('/sessions').send(conflicting);
    assert.equal(res.status, 409);
    assert.equal(res.body.error, 'session-payload-mismatch');
});

test('POST /sessions with grade=2 → 400 validation', async () => {
    const res = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals',
        reviews: [{ cardId: 'capitals:1', grade: 2, reviewedAt: '2026-05-03T11:00:00.000Z' }],
    });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'validation');
    assert.ok(Array.isArray(res.body.details));
});

test('POST /sessions with unknown cardId → 400 unknown-card', async () => {
    const res = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals',
        reviews: [sampleReview('capitals:9999', 4)],
    });
    assert.equal(res.status, 400);
    assert.equal(res.body.error, 'unknown-card');
});

test('GET /sessions/:id returns processed session', async () => {
    await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals',
        reviews: [sampleReview('capitals:1', 4)],
    });
    const res = await request(app).get(`/sessions/${SAMPLE_SESSION}`);
    assert.equal(res.status, 200);
    assert.equal(res.body.sessionId, SAMPLE_SESSION);
    assert.equal(res.body.deckId, 'capitals');
    assert.equal(res.body.reviews.length, 1);
    assert.equal(res.body.reviews[0].cardId, 'capitals:1');
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
    applySeed(legacyDb, FIXTURE_DECKS);
    legacyDb.prepare(
        `UPDATE card_schedules SET stage='learning', reps=4 WHERE card_id='capitals:1'`
    ).run();
    legacyDb.prepare(
        `UPDATE card_schedules SET stage='learning', reps=0 WHERE card_id='capitals:2'`
    ).run();
    legacyDb.pragma('user_version = 0');

    migrate(legacyDb);

    assert.equal(legacyDb.pragma('user_version', { simple: true }), 3);
    const lapsed = legacyDb.prepare('SELECT stage FROM card_schedules WHERE card_id=?').get('capitals:1');
    assert.equal(lapsed.stage, 'relearning');
    const fresh = legacyDb.prepare('SELECT stage FROM card_schedules WHERE card_id=?').get('capitals:2');
    assert.equal(fresh.stage, 'learning');
});

test('Migration v2 drops front/back columns from legacy cards table', () => {
    const legacyDb = new Database(':memory:');
    legacyDb.pragma('foreign_keys = ON');
    legacyDb.exec(`
        CREATE TABLE decks (deck_id TEXT PRIMARY KEY, display_name TEXT NOT NULL,
                            description TEXT NOT NULL DEFAULT '', new_cards_per_day INTEGER NOT NULL);
        CREATE TABLE cards (
            card_id TEXT PRIMARY KEY,
            deck_id TEXT NOT NULL REFERENCES decks(deck_id),
            front TEXT NOT NULL,
            back TEXT NOT NULL,
            ord INTEGER NOT NULL
        );
    `);
    legacyDb.pragma('user_version = 1');

    migrate(legacyDb);

    assert.equal(legacyDb.pragma('user_version', { simple: true }), 3);
    const cols = legacyDb.prepare('PRAGMA table_info(cards)').all().map((c) => c.name);
    assert.ok(!cols.includes('front'));
    assert.ok(!cols.includes('back'));
    assert.ok(cols.includes('card_id'));
    assert.ok(cols.includes('ord'));
});

test('applySeed is idempotent — schedule progress preserved across re-run', async () => {
    await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals',
        reviews: [sampleReview('capitals:1', 4)],
    });

    applySeed(db, FIXTURE_DECKS);

    const row = db.prepare('SELECT stage, learning_step FROM card_schedules WHERE card_id=?').get('capitals:1');
    assert.equal(row.stage, 'learning');
    assert.equal(row.learning_step, 1);
});

test('applySeed propagates metadata changes from registry', () => {
    applySeed(db, [
        { ...FIXTURE_DECKS[0], displayName: 'Renamed Capitals', newCardsPerDay: 2 },
        FIXTURE_DECKS[1],
        FIXTURE_DECKS[2],
    ]);

    const row = db.prepare(
        'SELECT display_name, new_cards_per_day FROM decks WHERE deck_id=?'
    ).get('capitals');
    assert.equal(row.display_name, 'Renamed Capitals');
    assert.equal(row.new_cards_per_day, 2);
});

test('applySeed inserts new card and gives it default new schedule', () => {
    const extended = [
        { ...FIXTURE_DECKS[0], cardIds: [...FIXTURE_DECKS[0].cardIds, 'capitals:4'] },
        FIXTURE_DECKS[1],
        FIXTURE_DECKS[2],
    ];
    applySeed(db, extended);

    const card = db.prepare('SELECT deck_id, ord FROM cards WHERE card_id=?').get('capitals:4');
    assert.equal(card.deck_id, 'capitals');
    assert.equal(card.ord, 4);
    const schedule = db.prepare('SELECT stage FROM card_schedules WHERE card_id=?').get('capitals:4');
    assert.equal(schedule.stage, 'new');
});

test('findOrphans returns card_ids in DB but absent from registry', () => {
    db.prepare(
        `INSERT INTO cards (card_id, deck_id, ord) VALUES ('capitals:99', 'capitals', 99)`
    ).run();

    const orphans = findOrphans(db, FIXTURE_DECKS);
    assert.deepEqual(orphans, ['capitals:99']);
});

test('loadDeckRegistry parses fixture file', () => {
    const decks = loadDeckRegistry(join(here, '__fixtures__', 'decks.json'));
    assert.equal(decks.length, 1);
    assert.equal(decks[0].deckId, 'fixture-deck');
    assert.equal(decks[0].cardIds.length, 2);
});

test('loadDeckRegistry rejects non-array top level', () => {
    assert.throws(
        () => loadDeckRegistry(join(here, 'package.json')),
        /expected top-level array/
    );
});

test('loadDeckRegistry rejects missing required fields', () => {
    assert.throws(
        () => loadDeckRegistry(join(here, '__fixtures__', 'decks-missing-field.json')),
        /missing field/
    );
});

test('loadDeckRegistry rejects missing file', () => {
    assert.throws(
        () => loadDeckRegistry(join(here, 'nonexistent.json')),
        /failed to read/
    );
});

test('Relearning roundtrip: lapse → relearning, then graduate preserves reps', async () => {
    db.prepare(
        `UPDATE card_schedules
         SET stage='review', reps=3, ease_factor=2.5, interval_days=10, due_at=?
         WHERE card_id='capitals:1'`
    ).run('2026-05-03T11:00:00.000Z');

    const lapse = await request(app).post('/sessions').send({
        sessionId: SAMPLE_SESSION,
        deckId: 'capitals',
        reviews: [{ cardId: 'capitals:1', grade: 0, reviewedAt: '2026-05-03T11:30:00.000Z' }],
    });
    assert.equal(lapse.status, 200);
    const afterLapse = lapse.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals:1');
    assert.equal(afterLapse.stage, 'relearning');
    assert.equal(afterLapse.reps, 3);
    assert.ok(Math.abs(afterLapse.easeFactor - 2.30) < 1e-9);

    const step = await request(app).post('/sessions').send({
        sessionId: ANOTHER_SESSION,
        deckId: 'capitals',
        reviews: [{ cardId: 'capitals:1', grade: 4, reviewedAt: '2026-05-03T11:40:00.000Z' }],
    });
    assert.equal(step.status, 200);
    const afterStep = step.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals:1');
    assert.equal(afterStep.stage, 'relearning');
    assert.equal(afterStep.learningStep, 1);
    assert.equal(afterStep.reps, 3);

    const graduate = await request(app).post('/sessions').send({
        sessionId: '33333333-3333-4333-8333-333333333333',
        deckId: 'capitals',
        reviews: [{ cardId: 'capitals:1', grade: 4, reviewedAt: '2026-05-03T11:50:00.000Z' }],
    });
    assert.equal(graduate.status, 200);
    const afterGraduate = graduate.body.updatedSchedule.cards.find((c) => c.cardId === 'capitals:1');
    assert.equal(afterGraduate.stage, 'review');
    assert.equal(afterGraduate.reps, 3, 'graduate from relearning must preserve reps');
    assert.equal(afterGraduate.intervalDays, 1);
});

test('GET /:id/schedule daily cap holds across two consecutive fetches in same UTC day', async () => {
    applySeed(db, [CAP_TEST_DECK]);

    const first = await request(app).get('/decks/cap-test/schedule');
    assert.equal(first.status, 200);
    const firstNewIds = first.body.cards.filter((c) => c.stage === 'new').map((c) => c.cardId);
    assert.deepEqual(firstNewIds, ['cap-test:1', 'cap-test:2'], 'first fetch releases exactly newCardsPerDay cards in ord order');

    const second = await request(app).get('/decks/cap-test/schedule');
    assert.equal(second.status, 200);
    const secondNewIds = second.body.cards.filter((c) => c.stage === 'new').map((c) => c.cardId);
    assert.deepEqual(secondNewIds, firstNewIds, 'second fetch on same UTC day releases nothing new');
});

test('GET /:id/schedule leftover from yesterday surfaces today plus today fresh quota', async () => {
    applySeed(db, [CAP_TEST_DECK]);

    currentNow = new Date('2026-05-03T12:00:00.000Z');
    const day1 = await request(app).get('/decks/cap-test/schedule');
    const day1NewIds = day1.body.cards.filter((c) => c.stage === 'new').map((c) => c.cardId);
    assert.deepEqual(day1NewIds, ['cap-test:1', 'cap-test:2']);

    currentNow = new Date('2026-05-04T12:00:00.000Z');
    const day2 = await request(app).get('/decks/cap-test/schedule');
    const day2NewIds = day2.body.cards.filter((c) => c.stage === 'new').map((c) => c.cardId);
    assert.deepEqual(
        day2NewIds,
        ['cap-test:1', 'cap-test:2', 'cap-test:3', 'cap-test:4'],
        'day-2 surfaces 2 ungraded leftovers + 2 fresh from today\'s quota',
    );

    const day2Second = await request(app).get('/decks/cap-test/schedule');
    const day2SecondNewIds = day2Second.body.cards.filter((c) => c.stage === 'new').map((c) => c.cardId);
    assert.deepEqual(day2SecondNewIds, day2NewIds, 'second day-2 fetch releases nothing further (cap honored)');
});

test('GET /decks newCount projects daily cap without mutating release state', async () => {
    applySeed(db, [CAP_TEST_DECK]);

    const beforeFetch = await request(app).get('/decks');
    const cap0 = beforeFetch.body.find((d) => d.deckId === 'cap-test');
    assert.equal(cap0.newCount, 2, 'pre-/schedule projection equals daily cap');
    assert.equal(cap0.totalCount, 5);
    const releasedBefore = db.prepare(
        `SELECT COUNT(*) AS n FROM card_schedules cs JOIN cards c ON c.card_id = cs.card_id
         WHERE c.deck_id = 'cap-test' AND cs.released_on IS NOT NULL`
    ).get().n;
    assert.equal(releasedBefore, 0, '/decks must not mutate released_on');

    await request(app).get('/decks/cap-test/schedule');
    const afterFetch = await request(app).get('/decks');
    const cap1 = afterFetch.body.find((d) => d.deckId === 'cap-test');
    assert.equal(cap1.newCount, 2, 'post-/schedule projection unchanged (already-released = released = 2)');

    currentNow = new Date('2026-05-04T12:00:00.000Z');
    const day2 = await request(app).get('/decks');
    const cap2 = day2.body.find((d) => d.deckId === 'cap-test');
    assert.equal(cap2.newCount, 4, 'day-2 projection: 2 leftover + 2 fresh');
});

test('Migration v3 adds released_on column to card_schedules', () => {
    const legacyDb = new Database(':memory:');
    legacyDb.pragma('foreign_keys = ON');
    legacyDb.exec(`
        CREATE TABLE decks (deck_id TEXT PRIMARY KEY, display_name TEXT NOT NULL,
                            description TEXT NOT NULL DEFAULT '', new_cards_per_day INTEGER NOT NULL);
        CREATE TABLE cards (card_id TEXT PRIMARY KEY,
                            deck_id TEXT NOT NULL REFERENCES decks(deck_id), ord INTEGER NOT NULL);
        CREATE TABLE card_schedules (
            card_id TEXT PRIMARY KEY REFERENCES cards(card_id),
            reps INTEGER NOT NULL DEFAULT 0,
            ease_factor REAL NOT NULL DEFAULT 2.5,
            interval_days INTEGER NOT NULL DEFAULT 0,
            due_at TEXT NOT NULL,
            stage TEXT NOT NULL DEFAULT 'new',
            learning_step INTEGER NOT NULL DEFAULT 0
        );
    `);
    legacyDb.pragma('user_version = 2');

    migrate(legacyDb);

    assert.equal(legacyDb.pragma('user_version', { simple: true }), 3);
    const cols = legacyDb.prepare('PRAGMA table_info(card_schedules)').all().map((c) => c.name);
    assert.ok(cols.includes('released_on'), 'released_on column added by v3');
});
