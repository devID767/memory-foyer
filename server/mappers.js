// Wire DTO mapping. Mirrors Assets/Scripts/Infrastructure/Dtos/ScheduleMappers.cs.

const round4 = (x) => Math.sign(x) * Math.floor(Math.abs(x) * 1e4 + 0.5) / 1e4;

export function rowToCardScheduleDto(row) {
    return {
        cardId: row.card_id,
        reps: row.reps,
        easeFactor: round4(row.ease_factor),
        intervalDays: row.interval_days,
        dueAt: new Date(row.due_at).toISOString(),
        stage: row.stage,
        learningStep: row.learning_step,
    };
}

export function rowToReviewDto(row) {
    return {
        cardId: row.card_id,
        grade: row.grade,
        reviewedAt: new Date(row.reviewed_at).toISOString(),
    };
}

export function rowToSm2State(row) {
    return {
        reps: row.reps,
        easeFactor: row.ease_factor,
        intervalDays: row.interval_days,
        dueAt: new Date(row.due_at),
        stage: row.stage,
        learningStep: row.learning_step,
    };
}

export function sm2StateToRowValues(state) {
    return {
        reps: state.reps,
        ease_factor: state.easeFactor,
        interval_days: state.intervalDays,
        due_at: state.dueAt.toISOString(),
        stage: state.stage,
        learning_step: state.learningStep,
    };
}
