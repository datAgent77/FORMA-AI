using System;
using System.Linq;
namespace Forma.Product
{
    public enum AthleteGoal { GeneralFitness, Strength, MuscleDevelopment, WeightManagement, Mobility, Endurance, Consistency }
    public enum AthleteAgeRange { Unspecified, Under18, From18To29, From30To44, From45To59, From60 }
    public enum MeasurementUnits { Metric, Imperial }
    public enum TrainingEnvironment { HolographicStudio, Beach, Forest }
    public enum AthleteEquipment { Mat, ResistanceBands, Dumbbells }
    // Canonical measurement units are kg/cm. Input is explicitly manual, never XR-derived.
    [Serializable] public sealed class AthleteProfileInput
    {
        public string displayName="";
        public AthleteAgeRange ageRange;
        public double? heightCm;
        public double? currentWeightKg;
        public AthleteGoal primaryGoal;
        public ExperienceLevel experience=ExperienceLevel.New;
        public AthleteEquipment[] equipment=Array.Empty<AthleteEquipment>();
        public int preferredMinutes=5;
        public TrainingEnvironment preferredEnvironment;
        public int trainingDaysPerWeek=3;
        public MovementLimits movementLimits;
        public string voluntaryLimitations="";
        public MeasurementUnits units;
        public string language="tr";
        public AthleteProfileInput Copy()
        {
            var p=(AthleteProfileInput)MemberwiseClone();p.equipment=equipment==null?null:(AthleteEquipment[])equipment.Clone();return p;
        }
        public static void Validate(AthleteProfileInput p)
        {
            bool Range(double? x,double min,double max)=>!x.HasValue || (!double.IsNaN(x.Value) && !double.IsInfinity(x.Value) && x>=min && x<=max);
            if(p==null || string.IsNullOrWhiteSpace(p.displayName) || p.displayName.Trim().Length>60 || p.displayName.Any(char.IsControl) ||
                !Enum.IsDefined(typeof(AthleteGoal),p.primaryGoal) || !Enum.IsDefined(typeof(AthleteAgeRange),p.ageRange) ||
                !Enum.IsDefined(typeof(ExperienceLevel),p.experience) || !Enum.IsDefined(typeof(TrainingEnvironment),p.preferredEnvironment) ||
                !Enum.IsDefined(typeof(MeasurementUnits),p.units) || (p.language!="tr" && p.language!="en") ||
                !Range(p.heightCm,40,250) || !Range(p.currentWeightKg,20,400) ||
                (p.preferredMinutes!=5 && p.preferredMinutes!=10 && p.preferredMinutes!=15 && p.preferredMinutes!=20 && p.preferredMinutes!=30) ||
                p.trainingDaysPerWeek<1 || p.trainingDaysPerWeek>7 || p.equipment==null || p.equipment.Length>3 ||
                p.equipment.Distinct().Count()!=p.equipment.Length || p.equipment.Any(x=>!Enum.IsDefined(typeof(AthleteEquipment),x)) ||
                (p.movementLimits & ~(MovementLimits.AvoidLowerBody|MovementLimits.AvoidShoulders))!=0 ||
                p.voluntaryLimitations==null || p.voluntaryLimitations.Length>400 || p.voluntaryLimitations.Any(c=>char.IsControl(c) && c!='\n'))
                throw new ArgumentException("Invalid athlete profile.");
        }
    }
    [Serializable] public sealed class AthleteProfile
    {
        public string athleteId;
        public string createdUtc;
        public string updatedUtc;
        public AthleteProfileInput preferences=new AthleteProfileInput();
        public static AthleteProfile Update(AthleteProfile existing,AthleteProfileInput input,DateTimeOffset now)
        {
            AthleteProfileInput.Validate(input);var copy=input.Copy();copy.displayName=copy.displayName.Trim();copy.voluntaryLimitations=copy.voluntaryLimitations.Trim();
            return new AthleteProfile{athleteId=existing?.athleteId??Guid.NewGuid().ToString("N"),createdUtc=existing?.createdUtc??now.ToString("O"),updatedUtc=now.ToString("O"),preferences=copy};
        }
        public static bool Valid(AthleteProfile p)
        {
            try{AthleteProfileInput.Validate(p?.preferences);return p!=null && Guid.TryParseExact(p.athleteId,"N",out _) &&
                DateTimeOffset.TryParse(p.createdUtc,out var created) && DateTimeOffset.TryParse(p.updatedUtc,out var updated) && updated>=created;}
            catch(ArgumentException){return false;}
        }
    }
}
