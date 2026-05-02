using System;
using NUnit.Framework;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Domain.Scheduling
{
    [TestFixture]
    public sealed class Sm2AlgorithmRelearningTests
    {
        private static readonly DateTime ReviewedAt =
            new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static Sm2State RelearningCard(int repetitions, int stepIndex) => new(
            Repetitions: repetitions,
            EaseFactor: 2.3,
            IntervalDays: 10,
            DueAt: ReviewedAt,
            Stage: LearningStage.Relearning,
            LearningStepIndex: stepIndex);

        [Test]
        public void Schedule_RelearningStepZeroWithAgain_StaysAtRelearningStepZero()
        {
            Sm2State result = Sm2Algorithm.Schedule(RelearningCard(5, 0), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Relearning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(0));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.Repetitions, Is.EqualTo(5));
        }

        [Test]
        public void Schedule_RelearningStepZeroWithGood_AdvancesToRelearningStepOne()
        {
            Sm2State result = Sm2Algorithm.Schedule(RelearningCard(5, 0), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Relearning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.Repetitions, Is.EqualTo(5));
        }

        [Test]
        public void Schedule_RelearningStepOneWithGood_GraduatesPreservingRepetitions()
        {
            Sm2State result = Sm2Algorithm.Schedule(RelearningCard(5, 1), ReviewGrade.Good, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(5));
            Assert.That(result.IntervalDays, Is.EqualTo(1));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(1)));
        }

        [Test]
        public void Schedule_RelearningStepZeroWithEasy_GraduatesPreservingRepetitionsWithFourDayInterval()
        {
            Sm2State result = Sm2Algorithm.Schedule(RelearningCard(5, 0), ReviewGrade.Easy, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Review));
            Assert.That(result.Repetitions, Is.EqualTo(5));
            Assert.That(result.IntervalDays, Is.EqualTo(4));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromDays(4)));
        }

        [Test]
        public void Schedule_RelearningStepOneWithAgain_DropsToRelearningStepZero()
        {
            Sm2State result = Sm2Algorithm.Schedule(RelearningCard(5, 1), ReviewGrade.Again, ReviewedAt);

            Assert.That(result.Stage, Is.EqualTo(LearningStage.Relearning));
            Assert.That(result.LearningStepIndex, Is.EqualTo(0));
            Assert.That(result.DueAt - ReviewedAt, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(result.Repetitions, Is.EqualTo(5));
        }
    }
}
