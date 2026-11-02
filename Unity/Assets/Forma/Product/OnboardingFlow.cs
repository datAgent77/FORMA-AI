using System;
namespace Forma.Product
{
    public enum OnboardingStep { Welcome, Coach, Profile, Safety, Review, Complete }
    public sealed class OnboardingFlow
    {
        readonly IProductProfileRepository repository;
        ProductProfile draft;
        public OnboardingStep Step { get; private set; }
        public ProductProfile Draft => draft.Copy();
        public bool SafetyAcknowledged { get; private set; }
        public bool SpaceConfirmed { get; private set; }
        public bool CanFinish => Step==OnboardingStep.Review && SafetyAcknowledged && SpaceConfirmed && ProductProfile.IsValid(draft);
        public bool PersistenceBlocked { get; private set; }
        public OnboardingFlow(IProductProfileRepository repository,ProductProfile initial)
        {
            this.repository=repository ?? throw new ArgumentNullException(nameof(repository));
            draft=ProductProfile.IsValid(initial)?initial.Copy():new ProductProfile();
            // Completing a prior onboarding never carries physical-space confirmation into a new launch.
            Step=draft.onboardingComplete?OnboardingStep.Complete:OnboardingStep.Welcome;
        }
        public bool Edit(Action<ProductProfile> edit)
        {
            if(Step==OnboardingStep.Complete || edit==null) return false;
            var changed=draft.Copy();edit(changed);
            if(!ProductProfile.IsValid(changed)) return false;
            changed.onboardingComplete=false;draft=changed;PersistenceBlocked=false;return true;
        }
        public bool Next()
        {
            if(Step==OnboardingStep.Review || Step==OnboardingStep.Complete) return false;
            if(Step==OnboardingStep.Safety && (!SafetyAcknowledged || !SpaceConfirmed)) return false;
            Step++;return true;
        }
        public bool Back()
        { if(Step==OnboardingStep.Welcome || Step==OnboardingStep.Complete)return false;Step--;return true; }
        public bool SkipOptionalProfile()
        {
            if(Step!=OnboardingStep.Profile) return false;
            draft.goal=FitnessGoal.Skip;draft.experience=ExperienceLevel.Skip;draft.preferredMinutes=0;
            draft.workoutPreference=WorkoutPreference.Skip;draft.movementLimits=MovementLimits.Undisclosed;draft.hasEquipment=false;
            Step=OnboardingStep.Safety;return true;
        }
        public void ConfirmSafety(bool acknowledged,bool space)
        { if(Step!=OnboardingStep.Complete) { SafetyAcknowledged=acknowledged;SpaceConfirmed=space; } }
        public void Interrupt()
        {
            SafetyAcknowledged=false;SpaceConfirmed=false;
            if(Step==OnboardingStep.Review) Step=OnboardingStep.Safety;
        }
        public bool Finish()
        {
            if(!CanFinish) return false;
            var saved=draft.Copy();saved.onboardingComplete=true;
            try { repository.Save(saved); }
            catch(Exception e) when(e is System.IO.IOException || e is UnauthorizedAccessException || e is InvalidOperationException)
            { PersistenceBlocked=true;return false; }
            draft=saved;Step=OnboardingStep.Complete;PersistenceBlocked=false;return true;
        }
        public void Reopen()
        { Step=OnboardingStep.Welcome;SafetyAcknowledged=false;SpaceConfirmed=false;PersistenceBlocked=false; }
    }
}
