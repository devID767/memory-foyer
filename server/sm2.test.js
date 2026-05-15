import { test } from 'node:test';
import assert from 'node:assert/strict';
import { schedule } from './sm2.js';

const T0 = new Date('2026-01-01T10:00:00.000Z');

const newCard = () => ({
    reps: 0,
    easeFactor: 2.5,
    intervalDays: 0,
    dueAt: T0,
    stage: 'new',
    learningStep: 0,
});

test('new card + Good → stage=learning, step=1, due+10min', () => {
    const r = schedule(newCard(), 4, T0);
    assert.equal(r.stage, 'learning');
    assert.equal(r.learningStep, 1);
    assert.equal(r.dueAt.getTime() - T0.getTime(), 10 * 60 * 1000);
    assert.equal(r.reps, 0);
});

test('new card + Again → stage=learning, step=0, due+10min', () => {
    const r = schedule(newCard(), 0, T0);
    assert.equal(r.stage, 'learning');
    assert.equal(r.learningStep, 0);
    assert.equal(r.dueAt.getTime() - T0.getTime(), 10 * 60 * 1000);
});

test('new card + Easy → graduate to review, reps=1, interval=4', () => {
    const r = schedule(newCard(), 5, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 1);
    assert.equal(r.intervalDays, 4);
});

test('learning step 1 + Good → graduate, reps=1, interval=1', () => {
    const s = { ...newCard(), stage: 'learning', learningStep: 1 };
    const r = schedule(s, 4, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 1);
    assert.equal(r.intervalDays, 1);
});

test('learning step 1 + Easy → graduate, reps=1, interval=4', () => {
    const s = { ...newCard(), stage: 'learning', learningStep: 1 };
    const r = schedule(s, 5, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 1);
    assert.equal(r.intervalDays, 4);
});

test('review reps=1 + Good → reps=2, interval=6', () => {
    const s = { reps: 1, easeFactor: 2.5, intervalDays: 1, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, T0);
    assert.equal(r.reps, 2);
    assert.equal(r.intervalDays, 6);
    assert.equal(r.easeFactor, 2.5);
});

test('review reps=2 + Good → reps=3, interval=round(6*2.5)=15', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 6, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, T0);
    assert.equal(r.reps, 3);
    assert.equal(r.intervalDays, 15);
});

test('review + Again from long interval → relearning, reps preserved, EF-=0.20', () => {
    const s = { reps: 5, easeFactor: 2.5, intervalDays: 60, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 0, T0);
    assert.equal(r.stage, 'relearning');
    assert.equal(r.learningStep, 0);
    assert.equal(r.reps, 5);
    assert.equal(r.intervalDays, 30);
    assert.ok(Math.abs(r.easeFactor - 2.30) < 1e-9);
    assert.equal(r.dueAt.getTime() - T0.getTime(), 10 * 60 * 1000);
});

test('relearning step 0 + Good → step 1, stage stays relearning', () => {
    const s = { reps: 5, easeFactor: 2.30, intervalDays: 30, dueAt: T0, stage: 'relearning', learningStep: 0 };
    const r = schedule(s, 4, T0);
    assert.equal(r.stage, 'relearning');
    assert.equal(r.learningStep, 1);
    assert.equal(r.reps, 5);
});

test('relearning step 1 + Good → graduate preserves reps', () => {
    const s = { reps: 5, easeFactor: 2.30, intervalDays: 30, dueAt: T0, stage: 'relearning', learningStep: 1 };
    const r = schedule(s, 4, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 5);
    assert.equal(r.intervalDays, 1);
});

test('learning step 1 + Good after fresh learning → graduate resets reps to 1', () => {
    const s = { reps: 0, easeFactor: 2.5, intervalDays: 0, dueAt: T0, stage: 'learning', learningStep: 1 };
    const r = schedule(s, 4, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 1);
    assert.equal(r.intervalDays, 1);
});

test('EF floor: Again from EF=1.4 clamps to 1.3', () => {
    const s = { reps: 3, easeFactor: 1.4, intervalDays: 10, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 0, T0);
    assert.equal(r.easeFactor, 1.3);
});

test('Interval cap: large interval × EF clamps to 365', () => {
    const s = { reps: 9, easeFactor: 2.8, intervalDays: 200, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, T0);
    assert.equal(r.intervalDays, 365);
});

test('Hard adjusts EF downward: 2.5 + (0.1 - 2*(0.08+2*0.02)) = 2.36', () => {
    const s = { reps: 3, easeFactor: 2.5, intervalDays: 10, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 3, T0);
    assert.ok(Math.abs(r.easeFactor - 2.36) < 1e-9);
});

// --- Overdue credit (mirrors C# Sm2AlgorithmSubsequentReviewsAndEfBoundsTests, byte-identical) ---

const DAY = 24 * 60 * 60 * 1000;
const after = (ms) => new Date(T0.getTime() + ms);

test('overdue credit: late remembered review credits elapsed time', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 10, dueAt: T0, stage: 'review', learningStep: 0 };
    const reviewedAt = after(40 * DAY);
    const r = schedule(s, 4, reviewedAt);
    // effective = max(10, 40) = 40 → round(40 * 2.5) = 100 (was 25 pre-credit).
    assert.equal(r.intervalDays, 100);
    assert.equal(r.dueAt.getTime() - reviewedAt.getTime(), 100 * DAY);
});

test('overdue credit: late but elapsed below stored interval keeps stored', () => {
    const s = { reps: 2, easeFactor: 2.0, intervalDays: 30, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, after(12 * DAY));
    // effective = max(30, 12) = 30 → round(30 * 2.0) = 60.
    assert.equal(r.intervalDays, 60);
});

test('overdue credit: early review ignores negative elapsed', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 10, dueAt: after(5 * DAY), stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, T0);
    // elapsed = -5 → effective = max(10, -5) = 10 → round(25) = 25 (unchanged).
    assert.equal(r.intervalDays, 25);
});

test('overdue credit: on-time review unchanged', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 10, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, T0);
    // elapsed = 0 → effective = 10 → round(25) = 25.
    assert.equal(r.intervalDays, 25);
});

test('overdue credit: very late review still clamps at 365', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 100, dueAt: T0, stage: 'review', learningStep: 0 };
    const reviewedAt = after(300 * DAY);
    const r = schedule(s, 4, reviewedAt);
    // effective = 300 → round(300 * 2.5) = 750 → clamped 365.
    assert.equal(r.intervalDays, 365);
    assert.equal(r.dueAt.getTime() - reviewedAt.getTime(), 365 * DAY);
});

test('overdue credit: literal interval arms unaffected by lateness', () => {
    const reviewedAt = after(100 * DAY);
    const first = schedule(
        { reps: 0, easeFactor: 2.5, intervalDays: 999, dueAt: T0, stage: 'review', learningStep: 0 }, 4, reviewedAt);
    const second = schedule(
        { reps: 1, easeFactor: 2.5, intervalDays: 999, dueAt: T0, stage: 'review', learningStep: 0 }, 4, reviewedAt);
    // newReps 1 and 2 use the literal 1 / 6 arms — overdue credit must not leak in.
    assert.equal(first.intervalDays, 1);
    assert.equal(second.intervalDays, 6);
});

test('overdue credit: sub-day lateness floors elapsed to zero', () => {
    const s = { reps: 2, easeFactor: 2.5, intervalDays: 10, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 4, after(23 * 60 * 60 * 1000));
    // floor(23h) = 0 → effective = max(10, 0) = 10 → round(25) = 25.
    assert.equal(r.intervalDays, 25);
});
