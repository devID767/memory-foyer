using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Domain.Scheduling
{
    public static class Sm2Algorithm
    {
        public static Sm2State Schedule(Sm2State state, ReviewGrade grade, DateTime reviewedAt)
        {
            switch (state.Stage)
            {
                case LearningStage.New:
                case LearningStage.Learning:
                case LearningStage.Relearning:
                    return ScheduleLearning(state, grade, reviewedAt);

                case LearningStage.Review:
                    return ScheduleReview(state, grade, reviewedAt);

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state.Stage, null);
            }
        }

        private static Sm2State ScheduleLearning(Sm2State state, ReviewGrade grade, DateTime reviewedAt)
        {
            LearningStage nextStage = state.Stage == LearningStage.Relearning
                ? LearningStage.Relearning
                : LearningStage.Learning;

            switch (state.LearningStepIndex)
            {
                case 0:
                    switch (grade)
                    {
                        case ReviewGrade.Again:
                            return state with
                            {
                                Stage = nextStage,
                                LearningStepIndex = 0,
                                DueAt = reviewedAt + TimeSpan.FromMinutes(10),
                            };

                        case ReviewGrade.Hard:
                        case ReviewGrade.Good:
                            return state with
                            {
                                Stage = nextStage,
                                LearningStepIndex = 1,
                                DueAt = reviewedAt + TimeSpan.FromMinutes(10),
                            };

                        case ReviewGrade.Easy:
                            return Graduate(state, 4, reviewedAt);

                        default:
                            throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
                    }

                case 1:
                    switch (grade)
                    {
                        case ReviewGrade.Again:
                            return state with
                            {
                                Stage = nextStage,
                                LearningStepIndex = 0,
                                DueAt = reviewedAt + TimeSpan.FromMinutes(10),
                            };

                        case ReviewGrade.Hard:
                        case ReviewGrade.Good:
                            return Graduate(state, 1, reviewedAt);

                        case ReviewGrade.Easy:
                            return Graduate(state, 4, reviewedAt);

                        default:
                            throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(state.LearningStepIndex), state.LearningStepIndex, null);
            }
        }

        private static Sm2State Graduate(Sm2State state, int intervalDays, DateTime reviewedAt)
        {
            int repetitions = state.Stage == LearningStage.Relearning ? state.Repetitions : 1;

            return state with
            {
                Stage = LearningStage.Review,
                Repetitions = repetitions,
                IntervalDays = intervalDays,
                DueAt = reviewedAt + TimeSpan.FromDays(intervalDays),
                LearningStepIndex = 0,
            };
        }

        private static Sm2State ScheduleReview(Sm2State state, ReviewGrade grade, DateTime reviewedAt)
        {
            switch (grade)
            {
                case ReviewGrade.Again:
                    {
                        // AwayFromZero matches server-side Math.round (item 3.5.4)
                        int newInterval = (int)Math.Round(state.IntervalDays * 0.5, MidpointRounding.AwayFromZero);
                        int newIntervalClamped = Math.Clamp(newInterval, 1, 365);
                        double newEf = Math.Max(1.3, state.EaseFactor - 0.20);
                        return state with
                        {
                            EaseFactor = newEf,
                            IntervalDays = newIntervalClamped,
                            Stage = LearningStage.Relearning,
                            LearningStepIndex = 0,
                            DueAt = reviewedAt + TimeSpan.FromMinutes(10),
                        };
                    }

                case ReviewGrade.Hard:
                case ReviewGrade.Good:
                case ReviewGrade.Easy:
                    {
                        int newReps = state.Repetitions + 1;
                        int rawInterval = newReps switch
                        {
                            1 => 1,
                            2 => 6,
                            _ => (int)Math.Round(state.IntervalDays * state.EaseFactor, MidpointRounding.AwayFromZero),
                        };
                        int newInterval = Math.Clamp(rawInterval, 1, 365);
                        int g = (int)grade;
                        double newEf = Math.Max(1.3, state.EaseFactor + (0.1 - (5 - g) * (0.08 + (5 - g) * 0.02)));
                        return state with
                        {
                            Repetitions = newReps,
                            EaseFactor = newEf,
                            IntervalDays = newInterval,
                            Stage = LearningStage.Review,
                            LearningStepIndex = 0,
                            DueAt = reviewedAt + TimeSpan.FromDays(newInterval),
                        };
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }
    }
}
