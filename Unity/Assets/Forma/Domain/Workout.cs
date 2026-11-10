using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Forma.Domain
{
    public enum WorkoutState { Ready, Introduction, Work, Rest, Paused, Completed, Aborted }
    public sealed class Exercise
    {
        public string Id { get; }
        public string Name { get; }
        public string Guidance { get; }
        public double WorkSeconds { get; }
        public double RestSeconds { get; }
        public int Sets { get; }
        public Exercise(string id, string name, string guidance, double work, double rest, int sets)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(guidance))
                throw new ArgumentException("Exercise text required.");
            if (!Finite(work) || !Finite(rest) || work <= 0 || rest < 0 || sets < 1 || sets > 20)
                throw new ArgumentOutOfRangeException(nameof(work));
            Id=id; Name=name; Guidance=guidance; WorkSeconds=work; RestSeconds=rest; Sets=sets;
        }
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
    public sealed class WorkoutPlan
    {
        public string Id { get; }
        public ReadOnlyCollection<Exercise> Exercises { get; }
        public WorkoutPlan(string id, IEnumerable<Exercise> exercises)
        {
            if (string.IsNullOrWhiteSpace(id) || exercises == null) throw new ArgumentException("Plan required.");
            var copy = new List<Exercise>(exercises);
            if (copy.Count == 0 || copy.Count > 20 || copy.Exists(e => e == null)) throw new ArgumentException("Invalid exercises.");
            Id=id; Exercises=copy.AsReadOnly();
        }
    }
    public static class StarterWorkouts
    {
        public static readonly Exercise March = new Exercise("march", "Gentle march",
            "Stand comfortably. Lift one foot slightly, then the other.\nKeep steps small. Stay in place; use a comfortable pace.", 20, 15, 2);
        public static readonly Exercise Shoulders = new Exercise("shoulders", "Shoulder rolls",
            "Stand comfortably with arms relaxed.\nSlowly roll shoulders back in small circles. Do not force range.", 20, 15, 2);
        public static WorkoutPlan Create(bool march, bool shoulders)
        {
            var selected = new List<Exercise>();
            if (march) selected.Add(March);
            if (shoulders) selected.Add(Shoulders);
            return new WorkoutPlan("studio-gentle-v1", selected);
        }
    }
    // Caller supplies monotonic elapsed time. No wall clock, rendering or platform dependencies.
    public sealed class WorkoutMachine
    {
        public const double IntroductionSeconds=10;
        public WorkoutPlan Plan { get; }
        public WorkoutState State { get; private set; } = WorkoutState.Ready;
        public WorkoutState SuspendedState { get; private set; }
        public double Remaining { get; private set; }
        public double ActiveSeconds { get; private set; }
        public int ExerciseIndex { get; private set; }
        public int SetNumber { get; private set; } = 1;
        public int CompletedSets { get; private set; }
        public Exercise Current => Plan.Exercises[ExerciseIndex];
        public double CurrentIntervalSeconds => (State==WorkoutState.Paused?SuspendedState:State) switch
        { WorkoutState.Introduction=>IntroductionSeconds, WorkoutState.Work=>Current.WorkSeconds, WorkoutState.Rest=>Current.RestSeconds, _=>0 };
        public bool IsRunning => State==WorkoutState.Introduction || State==WorkoutState.Work || State==WorkoutState.Rest;
        public WorkoutMachine(WorkoutPlan plan) { Plan=plan ?? throw new ArgumentNullException(nameof(plan)); }
        public bool Start(bool safetyReady)
        {
            if (State != WorkoutState.Ready || !safetyReady) return false;
            State=WorkoutState.Introduction; Remaining=IntroductionSeconds; return true;
        }
        public void Tick(double seconds)
        {
            if (!Exercise.Finite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            while (IsRunning && seconds > 0)
            {
                double elapsed=Math.Min(seconds, Remaining);
                if (State==WorkoutState.Work) ActiveSeconds+=elapsed;
                Remaining-=elapsed; seconds-=elapsed;
                if (Remaining<=0) Advance();
            }
        }
        void Advance()
        {
            if (State==WorkoutState.Introduction) { State=WorkoutState.Work; Remaining=Current.WorkSeconds; return; }
            if (State==WorkoutState.Work)
            {
                CompletedSets++;
                if (ExerciseIndex==Plan.Exercises.Count-1 && SetNumber==Current.Sets)
                { State=WorkoutState.Completed; Remaining=0; return; }
                State=WorkoutState.Rest; Remaining=Current.RestSeconds;
                if (Remaining==0) Advance();
                return;
            }
            if (State==WorkoutState.Rest)
            {
                if (SetNumber<Current.Sets) SetNumber++;
                else { ExerciseIndex++; SetNumber=1; }
                State=WorkoutState.Work; Remaining=Current.WorkSeconds;
            }
        }
        // Explicit manual skip; advancing does not invent a completed set or elapsed work.
        public bool SkipCurrentWork()
        {
            bool paused=State==WorkoutState.Paused;
            if(paused && SuspendedState!=WorkoutState.Work || !paused && State!=WorkoutState.Work)return false;
            if(paused)State=SuspendedState;
            int completed=CompletedSets;Advance();CompletedSets=completed;
            if(paused)Pause();return true;
        }
        public void Pause() { if (IsRunning) { SuspendedState=State; State=WorkoutState.Paused; } }
        public bool Resume(bool safetyReady)
        { if (State!=WorkoutState.Paused || !safetyReady) return false; State=SuspendedState; return true; }
        public void Abort()
        { if (IsRunning || State==WorkoutState.Paused) { State=WorkoutState.Aborted; Remaining=0; } }
    }
}
