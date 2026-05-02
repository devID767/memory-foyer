using System;
using NUnit.Framework;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Domain.Scheduling
{
    [TestFixture]
    public sealed class Sm2AlgorithmNewAndFirstReviewTests
    {
        private static readonly DateTime ReviewedAt =
            new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static Sm2State NewCard() => new(
            Repetitions: 0,
            EaseFactor: 2.5,
            IntervalDays: 0,
            DueAt: ReviewedAt,
            Stage: LearningStage.New,
            LearningStepIndex: 0);

        private static Sm2State LearningStepOne() => NewCard() with
        {
            Stage = LearningStage.Learning,
            LearningStepIndex = 1,
        };

        [Test]
        public void Schedule_NewCardWithAgain_StaysAtLearningStepZero()
        {
            Sm2State result = Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Learning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(0));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(result.Repetitions, Is.EqualTo(0));
            Assert.That(result.IntervalDays, Is.EqualTo(0));
        }

        [Test]
        public void Schedule_NewCardWithHard_AdvancesToLearningStepOne()
        {
            Sm2State result = Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Hard, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Learning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void Schedule_NewCardWithGood_AdvancesToLearningStepOne()
        {
            Sm2State result = Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Learning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void Schedule_NewCardWithEasy_GraduatesToReviewWithFourDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Easy, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(1));
            Assert.That(result.IntervalDays, Is.EqualTo(4));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(4)));
            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Schedule_LearningStepOneWithAgain_DropsToStepZero()
        {
            Sm2State result = Sm2Algorithm.Schedule(LearningStepOne(), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Learning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(0));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Schedule_LearningStepOneWithHard_GraduatesWithOneDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(LearningStepOne(), ReviewGrade.Hard, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(1));
            Assert.That(result.IntervalDays, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(1)));
        }

        [Test]
        public void Schedule_LearningStepOneWithGood_GraduatesWithOneDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(LearningStepOne(), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(1));
            Assert.That(result.IntervalDays, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(1)));
        }

        [Test]
        public void Schedule_LearningStepOneWithEasy_GraduatesWithFourDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(LearningStepOne(), ReviewGrade.Easy, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(1));
            Assert.That(result.IntervalDays, Is.EqualTo(4));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(4)));
        }

        [Test]
        public void Schedule_LearningPath_DoesNotMutateEaseFactor()
        {
            Assert.That(Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Again, ReviewedAt).EaseFactor, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Hard, ReviewedAt).EaseFactor, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Good, ReviewedAt).EaseFactor, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(Sm2Algorithm.Schedule(NewCard(), ReviewGrade.Easy, ReviewedAt).EaseFactor, Is.EqualTo(2.5).Within(1e-9));
        }
    }
}
