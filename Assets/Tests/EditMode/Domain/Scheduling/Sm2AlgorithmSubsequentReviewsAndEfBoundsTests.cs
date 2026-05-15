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

        // Card whose DueAt differs from the moment it is actually reviewed —
        // used to exercise overdue credit (GDD §4.4 / §4.6).
        private static Sm2State ReviewCardDue(
            int repetitions, int intervalDays, double easeFactor, DateTime dueAt) => new(
            Repetitions: repetitions,
            EaseFactor: easeFactor,
            IntervalDays: intervalDays,
            DueAt: dueAt,
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

        [Test]
        public void Schedule_LateRememberedReview_CreditsElapsedTime()
        {
            DateTime reviewedAt = ReviewedAt + TimeSpan.FromDays(40);

            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 10, 2.5, ReviewedAt), ReviewGrade.Good, reviewedAt);

            // effective = max(10, 40) = 40 → round(40 * 2.5) = 100 (was 25 pre-credit).
            Assert.That(result.IntervalDays, Is.EqualTo(100));
            Assert.That(result.DueAt - reviewedAt, Is.EqualTo(TimeSpan.FromDays(100)));
        }

        [Test]
        public void Schedule_LateButElapsedBelowStoredInterval_KeepsStoredInterval()
        {
            DateTime reviewedAt = ReviewedAt + TimeSpan.FromDays(12);

            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 30, 2.0, ReviewedAt), ReviewGrade.Good, reviewedAt);

            // effective = max(30, 12) = 30 → round(30 * 2.0) = 60.
            Assert.That(result.IntervalDays, Is.EqualTo(60));
        }

        [Test]
        public void Schedule_EarlyReview_IgnoresNegativeElapsed()
        {
            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 10, 2.5, ReviewedAt + TimeSpan.FromDays(5)), ReviewGrade.Good, ReviewedAt);

            // elapsed = -5 → effective = max(10, -5) = 10 → round(25) = 25 (unchanged).
            Assert.That(result.IntervalDays, Is.EqualTo(25));
        }

        [Test]
        public void Schedule_OnTimeReview_Unchanged()
        {
            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 10, 2.5, ReviewedAt), ReviewGrade.Good, ReviewedAt);

            // elapsed = 0 → effective = 10 → round(25) = 25 (unchanged).
            Assert.That(result.IntervalDays, Is.EqualTo(25));
        }

        [Test]
        public void Schedule_VeryLateReview_StillClampsAt365Days()
        {
            DateTime reviewedAt = ReviewedAt + TimeSpan.FromDays(300);

            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 100, 2.5, ReviewedAt), ReviewGrade.Good, reviewedAt);

            // effective = 300 → round(300 * 2.5) = 750 → clamped 365.
            Assert.That(result.IntervalDays, Is.EqualTo(365));
            Assert.That(result.DueAt - reviewedAt, Is.EqualTo(TimeSpan.FromDays(365)));
        }

        [Test]
        public void Schedule_LiteralIntervalArms_UnaffectedByLateness()
        {
            DateTime reviewedAt = ReviewedAt + TimeSpan.FromDays(100);

            Sm2State firstReview = Sm2Algorithm.Schedule(
                ReviewCardDue(0, 999, 2.5, ReviewedAt), ReviewGrade.Good, reviewedAt);
            Sm2State secondReview = Sm2Algorithm.Schedule(
                ReviewCardDue(1, 999, 2.5, ReviewedAt), ReviewGrade.Good, reviewedAt);

            // newReps 1 and 2 use the literal 1 / 6 arms — overdue credit must not leak in.
            Assert.That(firstReview.IntervalDays, Is.EqualTo(1));
            Assert.That(secondReview.IntervalDays, Is.EqualTo(6));
        }

        [Test]
        public void Schedule_SubDayLateness_FloorsElapsedToZero()
        {
            DateTime reviewedAt = ReviewedAt + TimeSpan.FromHours(23);

            Sm2State result = Sm2Algorithm.Schedule(
                ReviewCardDue(2, 10, 2.5, ReviewedAt), ReviewGrade.Good, reviewedAt);

            // floor(23h) = 0 → effective = max(10, 0) = 10 → round(25) = 25.
            Assert.That(result.IntervalDays, Is.EqualTo(25));
        }
    }
}
