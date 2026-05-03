// Server-side port of MemoryFoyer.Domain.Scheduling.Sm2Algorithm.
// Spec: docs/GDD.md §4. Reference: Assets/Scripts/Domain/Scheduling/Sm2Algorithm.cs.
//
// Four stages match the C# domain 1:1: 'new' | 'learning' | 'review' | 'relearning'.
// Graduate from Relearning preserves Repetitions; graduate from Learning resets to 1.

const STEP_DUE_MS = 10 * 60 * 1000;
const DAY_MS = 24 * 60 * 60 * 1000;
const INTERVAL_MIN = 1;
const INTERVAL_MAX = 365;
const EF_MIN = 1.3;

const GRADE_AGAIN = 0;
const GRADE_HARD = 3;
const GRADE_GOOD = 4;
const GRADE_EASY = 5;

const roundAwayFromZero = (x) => Math.sign(x) * Math.floor(Math.abs(x) + 0.5);
const clamp = (x, lo, hi) => Math.min(Math.max(x, lo), hi);
const addMinutes = (date, minutes) => new Date(date.getTime() + minutes * 60 * 1000);
const addDays = (date, days) => new Date(date.getTime() + days * DAY_MS);

/**
 * @param {{reps:number, easeFactor:number, intervalDays:number, dueAt:Date, stage:'new'|'learning'|'review'|'relearning', learningStep:number}} state
 * @param {0|3|4|5} grade
 * @param {Date} reviewedAt
 */
export function schedule(state, grade, reviewedAt) {
    if (state.stage === 'review') {
        return scheduleReview(state, grade, reviewedAt);
    }
    return scheduleLearning(state, grade, reviewedAt);
}

function scheduleLearning(state, grade, reviewedAt) {
    const nextStage = state.stage === 'relearning' ? 'relearning' : 'learning';
    if (state.learningStep === 0) {
        switch (grade) {
            case GRADE_AGAIN:
                return { ...state, stage: nextStage, learningStep: 0, dueAt: addMinutes(reviewedAt, 10) };
            case GRADE_HARD:
            case GRADE_GOOD:
                return { ...state, stage: nextStage, learningStep: 1, dueAt: addMinutes(reviewedAt, 10) };
            case GRADE_EASY:
                return graduate(state, 4, reviewedAt);
            default:
                throw new Error(`invalid grade: ${grade}`);
        }
    }
    if (state.learningStep === 1) {
        switch (grade) {
            case GRADE_AGAIN:
                return { ...state, stage: nextStage, learningStep: 0, dueAt: addMinutes(reviewedAt, 10) };
            case GRADE_HARD:
            case GRADE_GOOD:
                return graduate(state, 1, reviewedAt);
            case GRADE_EASY:
                return graduate(state, 4, reviewedAt);
            default:
                throw new Error(`invalid grade: ${grade}`);
        }
    }
    throw new Error(`invalid learningStep: ${state.learningStep}`);
}

function graduate(state, intervalDays, reviewedAt) {
    const reps = state.stage === 'relearning' ? state.reps : 1;
    return {
        reps,
        easeFactor: state.easeFactor,
        intervalDays,
        dueAt: addDays(reviewedAt, intervalDays),
        stage: 'review',
        learningStep: 0,
    };
}

function scheduleReview(state, grade, reviewedAt) {
    if (grade === GRADE_AGAIN) {
        const newInterval = clamp(roundAwayFromZero(state.intervalDays * 0.5), INTERVAL_MIN, INTERVAL_MAX);
        const newEf = Math.max(EF_MIN, state.easeFactor - 0.20);
        return {
            reps: state.reps,
            easeFactor: newEf,
            intervalDays: newInterval,
            dueAt: addMinutes(reviewedAt, 10),
            stage: 'relearning',
            learningStep: 0,
        };
    }
    if (grade === GRADE_HARD || grade === GRADE_GOOD || grade === GRADE_EASY) {
        const newReps = state.reps + 1;
        let raw;
        if (newReps === 1) raw = 1;
        else if (newReps === 2) raw = 6;
        else raw = roundAwayFromZero(state.intervalDays * state.easeFactor);
        const newInterval = clamp(raw, INTERVAL_MIN, INTERVAL_MAX);
        const g = grade;
        const newEf = Math.max(EF_MIN, state.easeFactor + (0.1 - (5 - g) * (0.08 + (5 - g) * 0.02)));
        return {
            reps: newReps,
            easeFactor: newEf,
            intervalDays: newInterval,
            dueAt: addDays(reviewedAt, newInterval),
            stage: 'review',
            learningStep: 0,
        };
    }
    throw new Error(`invalid grade: ${grade}`);
}
