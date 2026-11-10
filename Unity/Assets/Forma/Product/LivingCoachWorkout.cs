using System;
using System.Collections.Generic;
using System.Linq;
namespace Forma.Product
{
    // Data-defined fixed session. Demonstration is the first 8 seconds of each exercise, included in its 60 seconds.
    public static class LivingCoachWorkout
    {
        public const string Id="forma-start-5-v1";
        public const double WelcomeSeconds=30,DemonstrationSeconds=8;
        public static IReadOnlyList<PersonalBlock> Blocks=>new[]{
            new PersonalBlock{exerciseId="march",stage="WarmUp",workSeconds=60,restSeconds=0},
            new PersonalBlock{exerciseId="shoulders",stage="Exercise",workSeconds=60,restSeconds=0},
            new PersonalBlock{exerciseId="knees",stage="Exercise",workSeconds=60,restSeconds=0},
            new PersonalBlock{exerciseId="mobility",stage="Exercise",workSeconds=60,restSeconds=0},
            new PersonalBlock{exerciseId="mobility",stage="Cooldown",workSeconds=30,restSeconds=0}};
        public static double DurationSeconds=>WelcomeSeconds+Blocks.Sum(b=>b.workSeconds+b.restSeconds);
        public static void Validate(IEnumerable<PersonalBlock> blocks,AthleteProfileInput profile)
        {
            AthleteProfileInput.Validate(profile);var actual=blocks?.ToArray();var expected=Blocks;
            if(actual==null || actual.Length!=expected.Count || !actual.Select(b=>$"{b?.exerciseId}/{b?.stage}/{b?.workSeconds}/{b?.restSeconds}").SequenceEqual(expected.Select(b=>$"{b.exerciseId}/{b.stage}/{b.workSeconds}/{b.restSeconds}")) || actual.Any(b=>!PersonalWorkoutCatalog.Allowed(PersonalWorkoutCatalog.Find(b.exerciseId),profile)))throw new ArgumentException("FORMA START requires a compatible standing, equipment-free profile.");
        }
    }
}
