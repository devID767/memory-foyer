using System;
using NUnit.Framework;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Domain.Scheduling
{
    [TestFixture]
    public sealed class Sm2AlgorithmSubsequentReviewsAndEfBoundsTests
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
        public void Schedule_SecondReviewWithGood_UsesSixDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(1, 1, 2.5), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Repetitions, Is.EqualTo(2));
            Assert.That(result.IntervalDays, Is.EqualTo(6));
            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Schedule_ThirdReviewWithGood_MultipliesByEaseFactor()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(2, 6, 2.5), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Repetitions, Is.EqualTo(3));
            Assert.That(result.IntervalDays, Is.EqualTo(15));
            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewWithHard_DecreasesEaseFactor()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(2, 6, 2.5), ReviewGrade.Hard, ReviewedAt);

            Assert.That(result.EaseFactor, Is.EqualTo(2.36).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewWithGood_PreservesEaseFactor()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(2, 6, 2.5), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewWithEasy_IncreasesEaseFactor()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(2, 6, 2.5), ReviewGrade.Easy, ReviewedAt);

            Assert.That(result.EaseFactor, Is.EqualTo(2.6).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewWithHardOnLowEaseFactor_ClampsAtOnePointThree()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(3, 10, 1.35), ReviewGrade.Hard, ReviewedAt);

            Assert.That(result.EaseFactor, Is.EqualTo(1.3).Within(1e-9));
        }

        [Test]
        public void Schedule_ReviewWithLargePreviousInterval_ClampsAt365Days()
        {
            Sm2State result = Sm2Algorithm.Schedule(ReviewCard(3, 200, 2.5), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.IntervalDays, Is.EqualTo(365));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(365)));
        }
    }
}
