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

test('review + Again from long interval → collapsed-relearning, reps preserved, EF-=0.20', () => {
    const s = { reps: 5, easeFactor: 2.5, intervalDays: 60, dueAt: T0, stage: 'review', learningStep: 0 };
    const r = schedule(s, 0, T0);
    assert.equal(r.stage, 'learning');
    assert.equal(r.learningStep, 0);
    assert.equal(r.reps, 5);
    assert.equal(r.intervalDays, 30);
    assert.ok(Math.abs(r.easeFactor - 2.30) < 1e-9);
    assert.equal(r.dueAt.getTime() - T0.getTime(), 10 * 60 * 1000);
});

test('collapsed-relearning step 1 + Good → graduate preserves reps', () => {
    // After previous test pattern: reps=5, stage=learning, step=1.
    const s = { reps: 5, easeFactor: 2.30, intervalDays: 30, dueAt: T0, stage: 'learning', learningStep: 1 };
    const r = schedule(s, 4, T0);
    assert.equal(r.stage, 'review');
    assert.equal(r.reps, 5);
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
