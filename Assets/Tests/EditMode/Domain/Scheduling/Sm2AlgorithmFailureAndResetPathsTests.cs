using System;
using NUnit.Framework;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Domain.Scheduling
{
    [TestFixture]
    public sealed class Sm2AlgorithmFailureAndResetPathsTests
    {
        private static readonly DateTime ReviewedAt =
            new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static Sm2State ReviewCard(int repetitions, int intervalDays, double easeFactor) => new(
            Repetitions: repetitions,
            EaseFactor: easeFactor,
            IntervalDays: intervalDays,
            DueAt: ReviewedAt,
            Stage: LearningStage.Review,
            LearningStepIndex: 0);

        [Test]
        public void Schedule_ReviewWithAgain_RevertsToRelearningAndHalvesInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(5, 20, 2.5), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Relearning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(0));
            Assert.That(result.IntervalDays, Is.EqualTo(10));
            Assert.That(result.EaseFactor, Is.EqualTo(2.30).Within(1e-9));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.Repetitions, Is.EqualTo(5));
        }

        [Test]
        public void Schedule_ReviewAgainOnLowEaseFactor_ClampsAtOnePointThree()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(3, 10, 1.4), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.EaseFactor, Is.EqualTo(1.3).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewAgainOnIntervalOne_KeepsIntervalAtOne()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(3, 1, 2.5), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.IntervalDays, Is.EqualTo(1));
        }

        [Test]
        public void Schedule_ReviewAgain_PreservesRepetitions()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(7, 30, 2.0), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Repetitions, Is.EqualTo(7));
        }
    }
}
