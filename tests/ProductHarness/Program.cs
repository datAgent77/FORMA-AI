using System;
using System.IO;
using System.Text.Json;
using Forma.Product;
using Forma.Storage;
using Forma.Domain;
static class Program
{
    static int passed;
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions { IncludeFields=true };
    static void Check(bool value,string name) { if(!value)throw new Exception(name);passed++;Console.WriteLine("PASS "+name); }
    static void Throws<T>(Action action,string name) where T:Exception
    { bool threw=false;try{action();}catch(T){threw=true;}Check(threw,name); }
    static void Main()
    {
        var latch=new PointerActivationLatch();
        Check(!latch.Update(true,true,"right"),"held trigger at acquisition does not select");
        Check(!latch.Update(true,false,"right"),"release arms pointer");
        Check(latch.Update(true,true,"right"),"fresh press selects once");
        Check(!latch.Update(true,true,"right"),"held press does not repeat");
        Check(!latch.Update(false,false,null),"tracking loss invalidates pointer");
        Check(!latch.Update(true,true,"right"),"tracking recovery cannot select while held");
        Check(!latch.Update(true,false,"right") && latch.Update(true,true,"right"),"release after recovery permits fresh selection");
        Check(!latch.Update(true,true,"left"),"switching held input source cannot select");
        Check(!latch.Update(true,false,"left") && latch.Update(true,true,"left"),"new source release and press selects");
        var catalog=new CoachCatalog(JsonSerializer.Deserialize<CoachCatalogDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Coaches.json")),Json));
        Check(catalog.All.Count==3,"three real data-driven coach presets");
        var original=catalog.Get("sol");original.displayName="mutated";original.languages[0]="xx";
        Check(catalog.Get("sol").displayName=="Sol" && catalog.Get("sol").languages[0]=="en","coach catalog snapshots are isolated");
        Throws<ArgumentException>(()=>catalog.Get("celebrity"),"unknown coach rejected");
        var bad=JsonSerializer.Deserialize<CoachCatalogDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Coaches.json")),Json);
        bad.coaches[1].id="sol";Throws<ArgumentException>(()=>new CoachCatalog(bad),"duplicate coach rejected");
        var configured=new ConfiguredOfflineCoach(catalog.Get("sol"),PersonalityStyle.CalmGuide);
        var trial=new WorkoutMachine(StarterWorkouts.Create(true,true));trial.Start(true);
        Check(configured.Evaluate(trial).Text.Contains("comfortable pace") && configured.Evaluate(trial).Text.Contains("Stop if uncomfortable"),"personality preserves shared safety introduction");
        trial.Tick(10);Check(configured.Evaluate(trial).Text==new DeterministicCoach().Evaluate(trial).Text,"personality cannot replace exercise safety guidance");
        foreach(string locale in new[]{"en","tr"})
            foreach(MovementLimits limits in new[]{MovementLimits.Undisclosed,MovementLimits.AvoidLowerBody,MovementLimits.AvoidShoulders,MovementLimits.AvoidLowerBody|MovementLimits.AvoidShoulders})
                Check(!string.IsNullOrWhiteSpace(SetupCopy.Value(limits,locale)),"movement exclusion localized "+locale+" "+limits);
        var profile=new ProductProfile();Check(ProductProfile.IsValid(profile),"default profile valid with no sensitive data");
        profile.audioVolume=float.NaN;Check(!ProductProfile.IsValid(profile),"nonfinite audio rejected");profile.audioVolume=0.5f;
        profile.language="xx";Check(!ProductProfile.IsValid(profile),"unsupported locale rejected");profile.language="en";
        profile.movementLimits=(MovementLimits)8;Check(!ProductProfile.IsValid(profile),"unknown limitation flag rejected");profile.movementLimits=0;
        var fake=new MemoryRepository();var flow=new OnboardingFlow(fake,profile);
        Check(!flow.Finish() && !flow.Back(),"finish/back invalid from welcome");
        Check(flow.Next() && flow.Step==OnboardingStep.Coach,"welcome to coach");
        Check(!flow.Edit(p=>p.coachId="unknown") && flow.Draft.coachId=="sol","invalid edit cannot mutate draft");
        Check(flow.Edit(p=>{p.coachId="mira";p.language="tr";}),"coach and supported language selectable");
        var copy=flow.Draft;copy.coachId="atlas";Check(flow.Draft.coachId=="mira","draft getter cannot mutate flow");
        flow.Next();flow.Edit(p=>{p.goal=FitnessGoal.BuildRoutine;p.movementLimits=MovementLimits.AvoidLowerBody;});
        Check(flow.SkipOptionalProfile() && flow.Draft.goal==FitnessGoal.Skip && flow.Draft.movementLimits==0,"optional profile can be skipped and data cleared");
        Check(!flow.Next() && !flow.Finish(),"safety cannot be skipped");
        flow.ConfirmSafety(true,false);Check(!flow.Next(),"both safety and space confirmation required");
        flow.ConfirmSafety(true,true);Check(flow.Next() && flow.CanFinish,"review requires complete safety confirmation");
        flow.Interrupt();Check(flow.Step==OnboardingStep.Safety && !flow.SpaceConfirmed && !flow.Finish(),"interruption clears stale safety before finish");
        flow.ConfirmSafety(true,true);flow.Next();fake.FailSave=true;
        Check(!flow.Finish() && flow.Step==OnboardingStep.Review && !flow.Draft.onboardingComplete && flow.PersistenceBlocked,"save failure cannot complete onboarding");
        fake.FailSave=false;Check(flow.Finish() && fake.Value.onboardingComplete,"retry commits profile before completing onboarding");
        Check(!flow.Next() && !flow.Back() && !flow.Edit(p=>p.language="en"),"complete state rejects edits and navigation");
        var restored=new OnboardingFlow(fake,fake.Load().Profile);
        Check(restored.Step==OnboardingStep.Complete && !restored.SpaceConfirmed,"returning profile never restores physical-space confirmation");
        restored.Reopen();Check(restored.Step==OnboardingStep.Welcome && !restored.SafetyAcknowledged,"settings reopens explicit flow");
        var validator=new BeginnerWorkoutValidator();var recommender=new OfflineWorkoutRecommendations(validator);
        var plan=recommender.Recommend(new ProductProfile());Check(plan.Exercises.Count==2,"offline beginner recommendation");
        var seated=new ProductProfile {posture=Posture.Seated};plan=recommender.Recommend(seated);
        Check(plan.Exercises.Count==1 && plan.Exercises[0].Id=="shoulders" && plan.Exercises[0].Guidance.StartsWith("Sit"),"seated profile gets seated guidance without standing march");
        seated.movementLimits=MovementLimits.AvoidShoulders;Check(recommender.Recommend(seated)==null,"no compatible movement fails closed");
        var limited=new ProductProfile {movementLimits=MovementLimits.AvoidLowerBody};
        Check(recommender.Recommend(limited).Exercises.Count==1,"movement limitation filters lower body");
        Check(!validator.IsAllowed(StarterWorkouts.Create(true,true),limited,TrackingCapabilities.None),"recommendation safety validator rejects incompatible exercise");
        var unknown=new WorkoutPlan("unknown",new[]{new Exercise("jump","Jump","jump",20,15,2)});
        Check(!validator.IsAllowed(unknown,new ProductProfile(),TrackingCapabilities.ValidatedBody),"unknown generated exercise rejected even with tracking");
        var unsafeText=new WorkoutPlan("unsafe",new[]{new Exercise("march","Gentle march","Pick up heavy weights and jump",20,15,2)});
        Check(!validator.IsAllowed(unsafeText,new ProductProfile(),TrackingCapabilities.None),"known ID cannot smuggle unreviewed exercise instructions");
        var overload=new WorkoutPlan("too-much",new[]{new Exercise("march",StarterWorkouts.March.Name,StarterWorkouts.March.Guidance,60,15,8)});
        Check(!validator.IsAllowed(overload,new ProductProfile(),TrackingCapabilities.None),"unsupported dose rejected");
        foreach(var p in catalog.All)
        { var dialogue=new OfflineCoachDialogue();Check(!string.IsNullOrWhiteSpace(dialogue.Welcome(p,"en")) && !string.IsNullOrWhiteSpace(dialogue.Welcome(p,"tr")),"offline dialogue "+p.id); }
        Check(!new NoVoiceInput().Available && !new NoVoiceOutput().Available,"offline fallback never activates microphone or voice");
        foreach(SubscriptionTier tier in Enum.GetValues(typeof(SubscriptionTier)))
        {
            var e=new DevelopmentEntitlements(tier);
            Check(e.Allows(ProductFeature.Pause) && e.Allows(ProductFeature.Stop) && e.Allows(ProductFeature.Exit) && e.Allows(ProductFeature.Safety) && e.Allows(ProductFeature.PrivacyDelete),"safety and deletion never paywalled "+tier);
        }
        Check(!new DevelopmentEntitlements(SubscriptionTier.Free).Allows(ProductFeature.AiCoach),"free mock cannot grant AI feature");
        Check(new DevelopmentEntitlements(SubscriptionTier.Premium).Allows(ProductFeature.AiCoach),"premium development mock only");
        string directory=Path.Combine(Path.GetTempPath(),"forma-product-"+Guid.NewGuid().ToString("N"));
        try
        {
            var repo=new ProductProfileRepository(directory,new Codec());
            Check(repo.Load().Status==ProfileLoadStatus.Missing,"missing product profile");
            var saved=new ProductProfile {onboardingComplete=true,coachId="atlas",language="tr",posture=Posture.Seated};repo.Save(saved);
            var reload=new ProductProfileRepository(directory,new Codec()).Load();
            Check(reload.Status==ProfileLoadStatus.Loaded && reload.Profile.coachId=="atlas" && reload.Profile.posture==Posture.Seated,"actual file repository survives reload");
            var decoded=new Codec().Decode(repo.Export());Check(decoded.coachId=="atlas","export reflects recorded profile");
            saved.coachId="mira";repo.Save(saved);Check(repo.Load().Profile.coachId=="mira" && !File.Exists(Path.Combine(directory,"product-profile.json.tmp")),"atomic replacement leaves no temp file");
            File.WriteAllText(Path.Combine(directory,"product-profile.json"),"{\"schemaVersion\":99}");
            Check(repo.Load().Status==ProfileLoadStatus.Unsupported,"future schema explicitly identified");
            Throws<InvalidOperationException>(()=>repo.Save(saved),"future schema cannot be silently overwritten");
            Check(File.ReadAllText(Path.Combine(directory,"product-profile.json")).Contains("99"),"unknown profile bytes preserved");
            File.WriteAllText(Path.Combine(directory,"product-profile.json"),"broken json");
            Check(repo.Load().Status==ProfileLoadStatus.Invalid,"corrupt profile recognized");
            Throws<InvalidOperationException>(()=>repo.Save(saved),"corrupt profile needs explicit reset");
            File.WriteAllText(Path.Combine(directory,"product-profile.json"),"{}");
            Check(repo.Load().Status==ProfileLoadStatus.Unsupported,"missing schema never treated as current profile");
            repo.Delete();Check(repo.Load().Status==ProfileLoadStatus.Missing,"delete resets product profile");
            repo.Save(saved);Check(repo.Load().Status==ProfileLoadStatus.Loaded,"reset allows fresh save");
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        Console.WriteLine(passed+" product assertions passed.");
    }
    sealed class MemoryRepository:IProductProfileRepository
    {
        public ProductProfile Value=new ProductProfile();public bool FailSave;
        public ProfileLoadResult Load()=>new ProfileLoadResult(ProfileLoadStatus.Loaded,Value);
        public void Save(ProductProfile p){if(FailSave)throw new IOException("simulated");Value=p.Copy();}
        public void Delete(){Value=new ProductProfile();}public string Export()=>"";
    }
    sealed class Codec:IProfileCodec
    {
        public string Encode(ProductProfile p)=>JsonSerializer.Serialize(p,Json);
        public ProductProfile Decode(string data)
        {try{using(var document=JsonDocument.Parse(data)){if(!document.RootElement.TryGetProperty("schemaVersion",out var version))return new ProductProfile{schemaVersion=0};if(version.GetInt32()!=2)return new ProductProfile{schemaVersion=version.GetInt32()};}return JsonSerializer.Deserialize<ProductProfile>(data,Json);}catch(JsonException){throw new ArgumentException("Invalid JSON.");}}
    }
}
