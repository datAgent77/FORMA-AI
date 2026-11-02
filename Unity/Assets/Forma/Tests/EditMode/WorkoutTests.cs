using System;
using Forma.Domain;
using NUnit.Framework;
namespace Forma.Tests
{
    public sealed class WorkoutTests
    {
        static WorkoutMachine New() => new WorkoutMachine(StarterWorkouts.Create(true,true));
        [Test] public void StartRequiresSafety() { var w=New(); Assert.False(w.Start(false)); Assert.AreEqual(WorkoutState.Ready,w.State); }
        [Test] public void PauseFreezesCountdownAndWorkTime()
        { var w=New();w.Start(true);w.Tick(15);w.Pause();w.Tick(100);Assert.AreEqual(15,w.Remaining);Assert.AreEqual(5,w.ActiveSeconds);Assert.False(w.Resume(false));Assert.True(w.Resume(true)); }
        [Test] public void ExactCompletionHasNoTrailingRest()
        { var w=New();w.Start(true);w.Tick(135);Assert.AreEqual(WorkoutState.Completed,w.State);Assert.AreEqual(80,w.ActiveSeconds);Assert.AreEqual(4,w.CompletedSets); }
        [Test] public void LargeDeltaStopsAtTerminalState()
        { var w=New();w.Start(true);w.Tick(10000);Assert.AreEqual(80,w.ActiveSeconds);w.Abort();Assert.AreEqual(WorkoutState.Completed,w.State); }
        [Test] public void AbortCannotResume() { var w=New();w.Start(true);w.Pause();w.Abort();Assert.False(w.Resume(true)); }
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void RejectInvalidClock(double delta) { Assert.Throws<ArgumentOutOfRangeException>(()=>New().Tick(delta)); }
        [Test] public void EmptySelectionRejected() { Assert.Throws<ArgumentException>(()=>StarterWorkouts.Create(false,false)); }
        [Test] public void CoachRespondsToPauseAndCompletion()
        { var w=New();var coach=new DeterministicCoach();w.Start(true);w.Tick(10);Assert.AreEqual(CoachState.Guiding,coach.Evaluate(w).State);w.Pause();Assert.AreEqual(CoachState.Paused,coach.Evaluate(w).State);w.Resume(true);w.Tick(999);Assert.AreEqual(CoachState.Celebrating,coach.Evaluate(w).State); }
        [Test] public void SafetyFailsClosed() { Assert.False(new SafetyGate().CanExercise); }
    }
}
