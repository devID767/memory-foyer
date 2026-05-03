using System;
using System.Collections.Generic;
using System.Globalization;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Domain.Time;

namespace MemoryFoyer.Infrastructure.Dtos
{
    /// <summary>
    /// Static conversion helpers between Domain models and wire DTOs.
    /// All ISO-8601 timestamps use UTC with millisecond precision and a 'Z' suffix.
    /// </summary>
    public static class ScheduleMappers
    {
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        // ── CardSchedule ──────────────────────────────────────────────────────

        public static CardScheduleDto ToDto(CardSchedule schedule)
        {
            Sm2State state = schedule.State;
            return new CardScheduleDto
            {
                cardId = schedule.CardId.Value,
                reps = state.Repetitions,
                easeFactor = Math.Round(state.EaseFactor, 4, MidpointRounding.AwayFromZero),
                intervalDays = state.IntervalDays,
                dueAt = state.DueAt.ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture),
                stage = StageToWire(state.Stage),
                learningStep = (state.Stage == LearningStage.Learning || state.Stage == LearningStage.Relearning)
                    ? state.LearningStepIndex
                    : 0,
            };
        }

        public static CardSchedule FromDto(CardScheduleDto dto)
        {
            LearningStage stage = StageFromWire(dto.stage);
            DateTime dueAt = DateTime.Parse(
                dto.dueAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            int learningStepIndex = (stage == LearningStage.Learning || stage == LearningStage.Relearning)
                ? dto.learningStep
                : 0;

            Sm2State state = new Sm2State(
                Repetitions: dto.reps,
                EaseFactor: dto.easeFactor,
                IntervalDays: dto.intervalDays,
                DueAt: dueAt,
                Stage: stage,
                LearningStepIndex: learningStepIndex);

            return new CardSchedule(new CardId(dto.cardId), state);
        }

        // ── DeckSchedule ──────────────────────────────────────────────────────

        public static DeckSchedule FromDto(DeckScheduleDto dto, IClock clock, ScheduleSource source)
        {
            CardScheduleDto[] dtoCards = dto.cards;
            List<CardSchedule> cards = new List<CardSchedule>(dtoCards.Length);
            foreach (CardScheduleDto cardDto in dtoCards)
            {
                cards.Add(FromDto(cardDto));
            }

            return new DeckSchedule(
                DeckId: new DeckId(dto.deckId),
                Cards: cards,
                FetchedAt: clock.UtcNow,
                Source: source);
        }

        // ── SessionResult ─────────────────────────────────────────────────────

        public static SessionResultDto ToDto(SessionResult result)
        {
            System.Collections.Generic.IReadOnlyList<CardReview> reviews = result.Reviews;
            CardReviewDto[] reviewDtos = new CardReviewDto[reviews.Count];
            for (int i = 0; i < reviews.Count; i++)
            {
                reviewDtos[i] = ToDto(reviews[i]);
            }

            return new SessionResultDto
            {
                sessionId = result.SessionId.ToString(),
                deckId = result.DeckId.Value,
                reviews = reviewDtos,
            };
        }

        public static CardReviewDto ToDto(CardReview review)
        {
            return new CardReviewDto
            {
                cardId = review.CardId.Value,
                grade = (int)review.Grade,
                reviewedAt = review.ReviewedAt.ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture),
            };
        }

        public static SessionResult FromDto(SessionResultDto dto)
        {
            if (!Guid.TryParse(dto.sessionId, out Guid sessionId))
            {
                throw new FormatException($"Invalid sessionId GUID: '{dto.sessionId}'.");
            }

            CardReviewDto[] reviewDtos = dto.reviews;
            List<CardReview> reviews = new List<CardReview>(reviewDtos.Length);
            foreach (CardReviewDto reviewDto in reviewDtos)
            {
                reviews.Add(FromDto(reviewDto));
            }

            return new SessionResult(
                SessionId: sessionId,
                DeckId: new DeckId(dto.deckId),
                Reviews: reviews);
        }

        public static CardReview FromDto(CardReviewDto dto)
        {
            if (dto.grade != 0 && dto.grade != 3 && dto.grade != 4 && dto.grade != 5)
            {
                throw new FormatException($"Invalid grade value: {dto.grade}. Expected one of {{0, 3, 4, 5}}.");
            }

            ReviewGrade grade = (ReviewGrade)dto.grade;
            DateTime reviewedAt = DateTime.Parse(
                dto.reviewedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            return new CardReview(new CardId(dto.cardId), grade, reviewedAt);
        }

        // ── Stage wire helpers ────────────────────────────────────────────────

        /// <summary>
        /// Maps a Domain LearningStage to its wire string.
        /// Relearning maps to "learning" (lossy, intentional — server does not distinguish).
        /// </summary>
        public static string StageToWire(LearningStage stage)
        {
            return stage switch
            {
                LearningStage.New => "new",
                LearningStage.Learning => "learning",
                LearningStage.Review => "review",
                LearningStage.Relearning => "learning",
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown LearningStage value."),
            };
        }

        /// <summary>
        /// Maps a wire stage string to a Domain LearningStage.
        /// Throws <see cref="FormatException"/> for unknown values — the caller
        /// (<see cref="MemoryFoyer.Infrastructure.Persistence.HttpScheduleStore"/>) re-wraps
        /// this as <see cref="MemoryFoyer.Application.Persistence.ScheduleStoreContractException"/>.
        /// </summary>
        public static LearningStage StageFromWire(string stage)
        {
            return stage switch
            {
                "new" => LearningStage.New,
                "learning" => LearningStage.Learning,
                "review" => LearningStage.Review,
                "relearning" => LearningStage.Relearning,
                _ => throw new FormatException($"Unknown stage value from wire: '{stage}'."),
            };
        }
    }
}
