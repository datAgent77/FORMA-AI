using Forma.Product;
using System.Text.Json;
public record AthleteRequest(string Action,AthleteProfileInput Profile=null,int ExpectedRevision=0,string ConfirmAthleteId=null,string Confirmation=null);
public sealed partial class Simulation
{
    AthleteRepository AthleteStore=>athleteStore??=new AthleteRepository(root);
    AthleteRepository athleteStore;
    readonly string legacyRoot;
    public object AthleteSnapshot()
    {
        try
        {
            var document=AthleteStore.Load();
            return new{status="Ready",schemaVersion=document.schemaVersion,revision=document.revision,profile=document.profile,error=(string)null,
                legacySessionCount=history.Count,completedSessionCount=PersonalCompletionCount(),
                defaults=new{language=flow.Draft.language,preferredMinutes=flow.Draft.preferredMinutes==0?5:flow.Draft.preferredMinutes,
                    experience=flow.Draft.experience==ExperienceLevel.Skip?ExperienceLevel.New:flow.Draft.experience,movementLimits=flow.Draft.movementLimits,
                    primaryGoal=flow.Draft.goal==FitnessGoal.BuildRoutine?AthleteGoal.Consistency:flow.Draft.goal==FitnessGoal.GentleMovement?AthleteGoal.Mobility:AthleteGoal.GeneralFitness}};
        }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is JsonException)
        {return new{status="Unavailable",schemaVersion=2,revision=0,profile=(AthleteProfile)null,error="Yerel sporcu verisi okunamıyor. Dosya değiştirilmedi. Dışa aktar veya erişimi düzeltip yeniden dene."};}
    }
    public object SaveAthlete(AthleteRequest request)
    {
        if(SessionActive)throw new InvalidOperationException("End the active workout before changing athlete preferences.");
        if(request.Action!="save")throw new ArgumentException("Unknown athlete action.");
        AthleteStore.Save(request.Profile,request.ExpectedRevision);return AthleteSnapshot();
    }
    public void DeleteAthlete(AthleteRequest request)
    {
        if(SessionActive)throw new InvalidOperationException("End the active workout before deleting data.");
        AthleteDocument document;
        try{document=AthleteStore.Load();}
        catch(AthleteStorageException){if(request.Confirmation!="DELETE" || request.ConfirmAthleteId!="unreadable")throw;Act(new SimulationAction("reset",Enabled:true));return;}
        if(request.Confirmation!="DELETE" || document.profile==null || request.ConfirmAthleteId!=document.profile.athleteId)
            throw new ArgumentException("Explicit confirmation for this athlete is required.");
        if(request.ExpectedRevision!=document.revision)throw new AthleteConflictException();
        // Existing reset clears setup/history/cached workout. A failure never returns a success response.
        Act(new SimulationAction("reset",Enabled:true));
    }
    void DeleteLegacyCopies()
    {
        if(legacyRoot==null)return;
        string old=Path.Combine(legacyRoot,Path.GetFileName(root));
        foreach(var name in new[]{"product-profile.json","simulation-history.json","athlete-profile.json","personal-training.json"})
            foreach(var suffix in new[]{"",".tmp"}){string path=Path.Combine(old,name+suffix);if(File.Exists(path))File.Delete(path);}
    }
    bool AthleteAllows(Forma.Domain.WorkoutPlan plan)
    {
        try
        {
            var document=AthleteStore.Load();var p=document.profile?.preferences;if(p==null)return true;
            if(new AdaptiveTrainingEngine().Recommend(document.profile,document.revision,Memory(TrainingStore.Load()),DateTimeOffset.UtcNow).status!="Ready")return false;
            return !plan.Exercises.Any(e=>(e.Id=="march" && (p.movementLimits & MovementLimits.AvoidLowerBody)!=0) ||
                (e.Id=="shoulders" && (p.movementLimits & MovementLimits.AvoidShoulders)!=0));
        }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){return false;}
    }
    int? PersonalCompletionCount()
    {try{return history.Count(x=>x.outcome=="Completed")+TrainingStore.Load().sessions.Count(x=>x.outcome=="Completed");}catch(IOException){return null;}}
    object AthleteExport()=>new{schemaVersion=2,data=AthleteStore.Raw(),source="local-manual-profile",legacySetup=File.Exists(Path.Combine(root,"product-profile.json"))?File.ReadAllText(Path.Combine(root,"product-profile.json")):null,
        legacyHistory=File.Exists(HistoryPath)?File.ReadAllText(HistoryPath):null};
}
