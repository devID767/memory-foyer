using System;
using NUnit.Framework;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Domain.Time;
using MemoryFoyer.Infrastructure.Dtos;

namespace MemoryFoyer.Tests.EditMode.Infrastructure.Dtos
{
    [TestFixture]
    public sealed class ScheduleMappersTests
    {
        private static readonly DateTime DueAt =
            new DateTime(2026, 5, 3, 12, 0, 0, 123, DateTimeKind.Utc);

        private static readonly DateTime ReviewedAt =
            new DateTime(2026, 5, 3, 13, 30, 0, 456, DateTimeKind.Utc);

        private sealed class FakeClock : IClock
        {
            public DateTime UtcNow { get; init; }
        }

        [Test]
        public void ToDto_CardSchedule_RoundTripsThroughFromDto()
        {
            // Arrange
            Sm2State state = new Sm2State(
                Repetitions: 3,
                EaseFactor: 2.34,
                IntervalDays: 15,
                DueAt: DueAt,
                Stage: LearningStage.Learning,
                LearningStepIndex: 1);
            CardSchedule original = new CardSchedule(new CardId("c1"), state);

            // Act
            CardSchedule roundTripped = ScheduleMappers.FromDto(ScheduleMappers.ToDto(original));

            // Assert
            Assert.That(roundTripped.CardId.Value, Is.EqualTo("c1"));
            Assert.That(roundTripped.State.Repetitions, Is.EqualTo(3));
            Assert.That(roundTripped.State.EaseFactor, Is.EqualTo(2.34));
            Assert.That(roundTripped.State.IntervalDays, Is.EqualTo(15));
            Assert.That(roundTripped.State.DueAt, Is.EqualTo(DueAt));
            Assert.That(roundTripped.State.Stage, Is.EqualTo(LearningStage.Learning));
            Assert.That(roundTripped.State.LearningStepIndex, Is.EqualTo(1));
        }

        [Test]
        public void ToDto_CardSchedule_RelearningStageSerializesAsLearning()
        {
            // Arrange
            Sm2State state = new Sm2State(
                Repetitions: 2,
                EaseFactor: 2.5,
                IntervalDays: 1,
                DueAt: DueAt,
                Stage: LearningStage.Relearning,
                LearningStepIndex: 0);
            CardSchedule original = new CardSchedule(new CardId("c1"), state);

            // Act
            CardScheduleDto dto = ScheduleMappers.ToDto(original);
            CardSchedule roundTripped = ScheduleMappers.FromDto(dto);

            // Assert
            Assert.That(dto.stage, Is.EqualTo("learning"));
            Assert.That(roundTripped.State.Stage, Is.EqualTo(LearningStage.Learning));
        }

        [Test]
        public void ToDto_CardSchedule_ReviewStageZeroesLearningStep()
        {
            // Arrange
            Sm2State state = new Sm2State(
                Repetitions: 5,
                EaseFactor: 2.5,
                IntervalDays: 30,
                DueAt: DueAt,
                Stage: LearningStage.Review,
                LearningStepIndex: 99);
            CardSchedule original = new CardSchedule(new CardId("c1"), state);

            // Act
            CardScheduleDto dto = ScheduleMappers.ToDto(original);

            // Assert
            Assert.That(dto.learningStep, Is.EqualTo(0));
        }

        [Test]
        public void ToDto_CardSchedule_RoundsEaseFactorToFourDecimals()
        {
            // Arrange
            Sm2State state = new Sm2State(
                Repetitions: 1,
                EaseFactor: 2.123456789,
                IntervalDays: 1,
                DueAt: DueAt,
                Stage: LearningStage.Learning,
                LearningStepIndex: 0);
            CardSchedule original = new CardSchedule(new CardId("c1"), state);

            // Act
            CardScheduleDto dto = ScheduleMappers.ToDto(original);

            // Assert
            Assert.That(dto.easeFactor, Is.EqualTo(2.1235));
        }

        [Test]
        public void FromDto_CardSchedule_UnknownStageThrowsFormatException()
        {
            // Arrange
            CardScheduleDto dto = new CardScheduleDto
            {
                cardId = "c1",
                reps = 0,
                easeFactor = 2.5,
                intervalDays = 0,
                dueAt = "2026-05-03T12:00:00.000Z",
                stage = "bogus",
                learningStep = 0,
            };

            // Act + Assert
            Assert.Throws<FormatException>(() => ScheduleMappers.FromDto(dto));
        }

        [Test]
        public void FromDto_DeckSchedule_StampsFetchedAtAndSourceFromArguments()
        {
            // Arrange
            DateTime fetchTime = new DateTime(2026, 5, 3, 14, 0, 0, DateTimeKind.Utc);
            FakeClock clock = new FakeClock { UtcNow = fetchTime };
            DeckScheduleDto dto = new DeckScheduleDto
            {
                deckId = "capitals",
                cards = new[]
                {
                    new CardScheduleDto
                    {
                        cardId = "c1",
                        reps = 0,
                        easeFactor = 2.5,
                        intervalDays = 0,
                        dueAt = "2026-05-03T12:00:00.000Z",
                        stage = "new",
                        learningStep = 0,
                    },
                },
            };

            // Act
            DeckSchedule schedule = ScheduleMappers.FromDto(dto, clock, ScheduleSource.Cache);

            // Assert
            Assert.That(schedule.DeckId.Value, Is.EqualTo("capitals"));
            Assert.That(schedule.Source, Is.EqualTo(ScheduleSource.Cache));
            Assert.That(schedule.FetchedAt, Is.EqualTo(fetchTime));
        }

        [Test]
        public void ToDto_SessionResult_RoundTripsThroughFromDto()
        {
            // Arrange
            Guid sessionId = Guid.NewGuid();
            CardReview review = new CardReview(new CardId("c1"), ReviewGrade.Good, ReviewedAt);
            SessionResult original = new SessionResult(
                SessionId: sessionId,
                DeckId: new DeckId("capitals"),
                Reviews: new[] { review });

            // Act
            SessionResult roundTripped = ScheduleMappers.FromDto(ScheduleMappers.ToDto(original));

            // Assert
            Assert.That(roundTripped.SessionId, Is.EqualTo(sessionId));
            Assert.That(roundTripped.DeckId.Value, Is.EqualTo("capitals"));
            Assert.That(roundTripped.Reviews.Count, Is.EqualTo(1));
            Assert.That(roundTripped.Reviews[0].CardId.Value, Is.EqualTo("c1"));
            Assert.That(roundTripped.Reviews[0].Grade, Is.EqualTo(ReviewGrade.Good));
            Assert.That(roundTripped.Reviews[0].ReviewedAt, Is.EqualTo(ReviewedAt));
        }

        [Test]
        public void FromDto_CardReview_InvalidGradeThrowsFormatException()
        {
            // Arrange
            CardReviewDto dto = new CardReviewDto
            {
                cardId = "c1",
                grade = 2,
                reviewedAt = "2026-05-03T13:30:00.456Z",
            };

            // Act + Assert
            Assert.Throws<FormatException>(() => ScheduleMappers.FromDto(dto));
        }
    }
}
