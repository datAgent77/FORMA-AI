using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Forma.Domain;
namespace Forma.Product
{
    public enum ExerciseCategory { GentleCardio, Mobility }
    [Flags] public enum TrackingCapabilities { None=0, Head=1, Hands=2, ValidatedBody=4 }
    public sealed class ExerciseDefinition
    {
        public Exercise Timing { get; }
        public ExerciseCategory Category { get; }
        public string SafetyNotes { get; }
        public string DemonstrationReference { get; }
        public string Equipment => "None";
        public TrackingCapabilities RequiredTracking => TrackingCapabilities.None;
        public bool ManualCompletionSupported => true;
        public bool SupportsSeated { get; }
        public MovementLimits ExcludedBy { get; }
        public ExerciseDefinition(Exercise timing,ExerciseCategory category,string notes,string demo,bool seated,MovementLimits excluded)
        {
            if(timing==null || string.IsNullOrWhiteSpace(notes) || string.IsNullOrWhiteSpace(demo) || !Enum.IsDefined(typeof(ExerciseCategory),category))
                throw new ArgumentException("Invalid exercise definition.");
            Timing=timing;Category=category;SafetyNotes=notes;DemonstrationReference=demo;SupportsSeated=seated;ExcludedBy=excluded;
        }
    }
    public static class ExerciseCatalog
    {
        public static readonly ReadOnlyCollection<ExerciseDefinition> Beginner=new List<ExerciseDefinition>{
            new ExerciseDefinition(StarterWorkouts.March,ExerciseCategory.GentleCardio,"Stay in place. Stop for discomfort or unsteadiness.","procedural:march-v1",false,MovementLimits.AvoidLowerBody),
            new ExerciseDefinition(StarterWorkouts.Shoulders,ExerciseCategory.Mobility,"Small comfortable circles only. Stop for discomfort.","procedural:shoulders-v1",true,MovementLimits.AvoidShoulders)
        }.AsReadOnly();
        public static bool Allowed(ExerciseDefinition e,ProductProfile p) => ProductProfile.IsValid(p) &&
            (p.posture!=Posture.Seated || e.SupportsSeated) && (e.ExcludedBy & p.movementLimits)==0;
        public static WorkoutPlan FirstWorkout(ProductProfile p)
        {
            if(!ProductProfile.IsValid(p)) throw new ArgumentException("Invalid profile.");
            var selected=new List<Exercise>();
            foreach(var e in Beginner)
                if(Allowed(e,p)) selected.Add(e.Timing.Id=="shoulders" && p.posture==Posture.Seated ?
                    new Exercise("shoulders","Seated shoulder rolls","Sit on a stable chair, feet supported. Keep arms relaxed.\nRoll shoulders slowly in small circles; stop for discomfort.",20,15,2):e.Timing);
            return selected.Count==0?null:new WorkoutPlan("studio-gentle-v1",selected);
        }
    }
    public interface IWorkoutSafetyValidator { bool IsAllowed(WorkoutPlan plan,ProductProfile profile,TrackingCapabilities available); }
    public sealed class BeginnerWorkoutValidator : IWorkoutSafetyValidator
    {
        public bool IsAllowed(WorkoutPlan plan,ProductProfile p,TrackingCapabilities available)
        {
            if(plan==null || !ProductProfile.IsValid(p))return false;
            var seen=new HashSet<string>();
            var canonical=ExerciseCatalog.FirstWorkout(p);if(canonical==null)return false;
            foreach(var e in plan.Exercises)
            {
                ExerciseDefinition known=null;foreach(var d in ExerciseCatalog.Beginner)if(d.Timing.Id==e.Id)known=d;
                if(known==null || !ExerciseCatalog.Allowed(known,p) || !seen.Add(e.Id) || e.WorkSeconds>20 || e.Sets>2 ||
                    e.RestSeconds<15 || e.RestSeconds>60 || (known.RequiredTracking & available)!=known.RequiredTracking) return false;
                Exercise expected=null;foreach(var value in canonical.Exercises)if(value.Id==e.Id)expected=value;
                if(expected==null || e.Name!=expected.Name || e.Guidance!=expected.Guidance)return false;
            }
            return true;
        }
    }
}
