namespace Forma.Domain
{
    public enum CoachState { Welcome, Guiding, Recovering, Paused, Celebrating, Stopped }
    public readonly struct CoachCue
    {
        public CoachState State { get; }
        public string Text { get; }
        public CoachCue(CoachState state, string text) { State=state; Text=text; }
    }
    public interface ICoachPolicy { CoachCue Evaluate(WorkoutMachine workout); }
    public sealed class DeterministicCoach : ICoachPolicy
    {
        public CoachCue Evaluate(WorkoutMachine w)
        {
            switch (w.State)
            {
                case WorkoutState.Introduction: return new CoachCue(CoachState.Welcome,
                    "Welcome to FORMA Studio. We will move gently.\nStay inside your cleared boundary. Stop if uncomfortable.");
                case WorkoutState.Work: return new CoachCue(CoachState.Guiding, w.Current.Guidance);
                case WorkoutState.Rest: return new CoachCue(CoachState.Recovering,
                    "Rest. Relax and breathe normally.\nNext: " + Next(w) + ". Resume at your own pace.");
                case WorkoutState.Paused: return new CoachCue(CoachState.Paused,
                    "Paused. Stop moving and check your surroundings.\nResume only when ready; you can end the session anytime.");
                case WorkoutState.Completed: return new CoachCue(CoachState.Celebrating,
                    "Session complete. Thank you for moving with me.\nTake a moment to rest. Your summary is ready.");
                case WorkoutState.Aborted: return new CoachCue(CoachState.Stopped, "Session ended. Take your time.");
                default: return new CoachCue(CoachState.Welcome, "I am your FORMA coach. Choose your movements below.");
            }
        }
        static string Next(WorkoutMachine w) => w.SetNumber<w.Current.Sets ? w.Current.Name : w.Plan.Exercises[w.ExerciseIndex+1].Name;
    }
    public sealed class SafetyGate
    {
        public bool EnvironmentConfirmed { get; set; }
        public bool PassthroughReady { get; set; }
        public bool TrackingReady { get; set; }
        public bool HasFocus { get; set; }
        public bool EquipmentFreeVirtualMode { get; set; } // Explicit equipment-free Worlds choice; never inferred passthrough.
        public bool CanExercise => EnvironmentConfirmed && (PassthroughReady || EquipmentFreeVirtualMode) && TrackingReady && HasFocus;
    }
}
