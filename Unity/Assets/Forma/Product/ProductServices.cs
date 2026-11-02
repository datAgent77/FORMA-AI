using System;
using Forma.Domain;
namespace Forma.Product
{
    public interface ICoachDialogueService { string Welcome(CoachDefinition coach,string language); }
    public interface IWorkoutRecommendationService { WorkoutPlan Recommend(ProductProfile profile); }
    public interface ICoachContextProvider { ProductProfile ReadProfile(); }
    public interface IVoiceInputService { bool Available { get; } void Stop(); }
    public interface IVoiceOutputService { bool Available { get; } void Stop(); }
    public sealed class OfflineCoachDialogue : ICoachDialogueService
    {
        public string Welcome(CoachDefinition coach,string language)
        {
            bool tr=language=="tr";
            switch(coach.personality)
            {
                case PersonalityStyle.Motivator:return tr?"Birlikte küçük bir başlangıç yapalım. Kendi hızında ilerle.":"Let's make a small start together. Move at your own pace.";
                case PersonalityStyle.CalmGuide:return tr?"Rahat bir tempo bulalım. Hazır olduğunda başlayacağız.":"Let's find a comfortable pace. We will begin when you are ready.";
                default:return tr?"Önce hareketi incele, sonra rahat bir aralıkta uygula.":"Review the movement first, then use a comfortable range.";
            }
        }
    }
    public sealed class OfflineWorkoutRecommendations : IWorkoutRecommendationService
    {
        readonly IWorkoutSafetyValidator validator;
        public OfflineWorkoutRecommendations(IWorkoutSafetyValidator validator) { this.validator=validator ?? throw new ArgumentNullException(nameof(validator)); }
        public WorkoutPlan Recommend(ProductProfile profile)
        { var plan=ExerciseCatalog.FirstWorkout(profile);return validator.IsAllowed(plan,profile,TrackingCapabilities.None)?plan:null; }
    }
    public sealed class NoVoiceInput : IVoiceInputService { public bool Available=>false;public void Stop(){} }
    public sealed class NoVoiceOutput : IVoiceOutputService { public bool Available=>false;public void Stop(){} }
    public enum ProductFeature { Pause, Stop, Exit, Safety, PrivacyDelete, BasicStudio, ExpandedLibrary, PersonalizedPlans, AiCoach }
    public enum SubscriptionTier { Free, Pro, Premium }
    public interface IFeatureEntitlements { bool Allows(ProductFeature feature); }
    // Unwired test/development provider. Never a substitute for store app entitlement checks.
    public sealed class DevelopmentEntitlements : IFeatureEntitlements
    {
        readonly SubscriptionTier tier;
        public DevelopmentEntitlements(SubscriptionTier tier) { this.tier=tier; }
        public bool Allows(ProductFeature f)
        {
            switch(f)
            {
                case ProductFeature.Pause:case ProductFeature.Stop:case ProductFeature.Exit:case ProductFeature.Safety:
                case ProductFeature.PrivacyDelete:case ProductFeature.BasicStudio:return true;
                case ProductFeature.ExpandedLibrary:case ProductFeature.PersonalizedPlans:return tier==SubscriptionTier.Pro || tier==SubscriptionTier.Premium;
                case ProductFeature.AiCoach:return tier==SubscriptionTier.Premium;
                default:return false;
            }
        }
    }
    public interface IAuthenticationService { bool HasVerifiedAccount { get; } }
    public interface IStoreAppEntitlement { bool HasVerifiedAccess { get; } }
    public interface IPurchaseService { bool Available { get; } }
    public interface ICloudSyncService { bool ConsentGranted { get; } }
    public interface ITelemetryService { bool ConsentGranted { get; } }
    public interface ICrashReportingService { void ReportCode(string nonPersonalErrorCode); }
    public interface IGhostMotionArchive { bool HasCapturedMotion(string sessionId); }
}
