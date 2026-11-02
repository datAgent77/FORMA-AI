using System;
namespace Forma.Product
{
    public enum Posture { Standing, Seated }
    public enum FitnessGoal { Skip, BuildRoutine, GentleMovement, GeneralFitness }
    public enum ExperienceLevel { Skip, New, SomeExperience, Regular }
    public enum WorkoutPreference { Skip, Mobility, GentleCardio, Mixed }
    [Flags] public enum MovementLimits { Undisclosed=0, AvoidLowerBody=1, AvoidShoulders=2 }
    [Serializable] public sealed class ProductProfile
    {
        public int schemaVersion=2;
        public bool onboardingComplete;
        public string coachId="sol";
        public string language="en";
        public PersonalityStyle personality=PersonalityStyle.Motivator;
        public Posture posture=Posture.Standing;
        public FitnessGoal goal;
        public ExperienceLevel experience;
        public int preferredMinutes; // 0 means skipped; not an automatic workout prescription.
        public WorkoutPreference workoutPreference;
        public MovementLimits movementLimits;
        public bool hasEquipment; // An inventory preference, never authorizes equipment use.
        public bool muted=true;
        public float audioVolume=0.5f;
        public bool reducedMotion;
        public string voiceId="text-only";
        public ProductProfile Copy() => (ProductProfile)MemberwiseClone();
        public static bool IsValid(ProductProfile p)
        {
            return p!=null && p.schemaVersion==2 && CoachCatalog.KnownId(p.coachId) &&
                (p.language=="en" || p.language=="tr") && Enum.IsDefined(typeof(PersonalityStyle),p.personality) && Enum.IsDefined(typeof(Posture),p.posture) &&
                Enum.IsDefined(typeof(FitnessGoal),p.goal) && Enum.IsDefined(typeof(ExperienceLevel),p.experience) &&
                Enum.IsDefined(typeof(WorkoutPreference),p.workoutPreference) &&
                (p.movementLimits & ~(MovementLimits.AvoidLowerBody|MovementLimits.AvoidShoulders))==0 &&
                (p.preferredMinutes==0 || p.preferredMinutes==3 || p.preferredMinutes==5 || p.preferredMinutes==10) &&
                !float.IsNaN(p.audioVolume) && !float.IsInfinity(p.audioVolume) && p.audioVolume>=0 && p.audioVolume<=1 &&
                p.voiceId=="text-only";
        }
    }
    public enum ProfileLoadStatus { Missing, Loaded, Invalid, Unsupported, Unavailable }
    public sealed class ProfileLoadResult
    {
        public ProfileLoadStatus Status { get; }
        readonly ProductProfile value;
        public ProductProfile Profile => value.Copy();
        public ProfileLoadResult(ProfileLoadStatus status,ProductProfile profile=null)
        { Status=status; value=(profile ?? new ProductProfile()).Copy(); }
    }
    public interface IProductProfileRepository
    {
        ProfileLoadResult Load();
        void Save(ProductProfile profile);
        void Delete();
        string Export();
    }
}
