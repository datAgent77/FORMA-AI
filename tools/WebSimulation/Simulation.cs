using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forma.Domain;
using Forma.Product;
using Forma.Storage;

public record SimulationAction(string Action,string Field=null,JsonElement Value=default,double Seconds=0,bool Enabled=false);
public static class SimulationJson
{
    public static readonly JsonSerializerOptions Options=Create();
    static JsonSerializerOptions Create(){var o=new JsonSerializerOptions(JsonSerializerDefaults.Web);Configure(o);return o;}
    public static void Configure(JsonSerializerOptions o){o.IncludeFields=true;o.Converters.Add(new JsonStringEnumConverter());}
}
public sealed partial class Simulation
{
    readonly string root;
    readonly CoachCatalog catalog;
    readonly FaultableProfiles profiles;
    OnboardingFlow flow;
    ProfileLoadStatus loadStatus;
    WorkoutMachine workout;
    SessionRecord record;
    readonly SafetyGate gate=new SafetyGate{HasFocus=true,TrackingReady=true,PassthroughReady=true};
    readonly List<SessionRecord> history;
    readonly List<string> events=new List<string>();
    string notice="";
    bool closed;
    bool selectMarch=true,selectShoulders=true;
    public Simulation(string root,CoachCatalog catalog)
    {
        this.root=root;this.catalog=catalog;
        profiles=new FaultableProfiles(new ProductProfileRepository(root,new WebProfileCodec()));
        history=LoadHistory();LoadProfile();
        Log("Local simulation opened. No camera, headset or microphone is connected.");
    }
    public static CoachCatalog LoadCatalog(string path)=>new CoachCatalog(JsonSerializer.Deserialize<CoachCatalogDocument>(File.ReadAllText(path),SimulationJson.Options));
    bool SessionActive=>workout!=null && (workout.IsRunning || workout.State==WorkoutState.Paused);
    bool LoadBlocked=>loadStatus!=ProfileLoadStatus.Loaded && loadStatus!=ProfileLoadStatus.Missing;
    bool SetupReady=>!LoadBlocked && flow.Step==OnboardingStep.Complete;
    void LoadProfile()
    {
        var loaded=profiles.Load();loadStatus=loaded.Status;flow=new OnboardingFlow(profiles,loaded.Profile);
        workout=null;record=null;gate.EnvironmentConfirmed=false;closed=false;
        InitializeDaily();
        if(SetupReady)PrepareWorkout();
    }
    void PrepareWorkout()
    {
        var plan=new OfflineWorkoutRecommendations(new BeginnerWorkoutValidator()).Recommend(flow.Draft);
        if(plan!=null)
        {
            var chosen=plan.Exercises.Where(e=>e.Id=="march"?selectMarch:selectShoulders).ToList();
            workout=chosen.Count==0?null:new WorkoutMachine(new WorkoutPlan(plan.Id,chosen));
        }
        else workout=null;
        record=null;
    }
    public bool Act(SimulationAction a)
    {
        bool accepted=false;notice="";
        if(a.Action.StartsWith("daily",StringComparison.Ordinal))return ActDaily(a);
        switch(a.Action)
        {
            case "edit":
                if(LoadBlocked || closed)return Reject("Reset or reload the stored setup before editing.");
                accepted=flow.Edit(p=>Edit(p,a.Field,a.Value));break;
            case "next":accepted=!LoadBlocked && !closed && flow.Next();break;
            case "back":accepted=!LoadBlocked && !closed && flow.Back();break;
            case "skip":accepted=!LoadBlocked && !closed && flow.SkipOptionalProfile();break;
            case "safety":
                if(closed || SetupReady)return Reject("This check belongs to setup.");
                flow.ConfirmSafety(a.Field=="ack"?a.Enabled:flow.SafetyAcknowledged,a.Field=="space"?a.Enabled:flow.SpaceConfirmed);accepted=true;break;
            case "finish":
                accepted=!LoadBlocked && !closed && flow.Finish();
                if(accepted){loadStatus=ProfileLoadStatus.Loaded;InitializeDaily();PrepareWorkout();Log("Setup saved by shared OnboardingFlow.");}
                else if(flow.PersistenceBlocked)notice="Simulated save failed. Disable the write fault and retry; setup is still open.";
                break;
            case "reopen":
                if(SessionActive)return Reject("End the workout before changing setup.");
                if(LoadBlocked)return Reject("Reset or reload unreadable setup first.");
                Persist();flow.Reopen();workout=null;record=null;gate.EnvironmentConfirmed=false;closed=false;accepted=true;break;
            case "selectExercise":
                if(!SetupReady || SessionActive || closed)return Reject("Choose movements before starting a workout.");
                var available=new OfflineWorkoutRecommendations(new BeginnerWorkoutValidator()).Recommend(flow.Draft);
                if(available==null || !available.Exercises.Any(e=>e.Id==a.Field))return Reject("Movement excluded by setup preferences.");
                bool oldMarch=selectMarch,oldShoulders=selectShoulders;
                if(a.Field=="march")selectMarch=a.Enabled;else if(a.Field=="shoulders")selectShoulders=a.Enabled;
                if(!available.Exercises.Any(e=>e.Id=="march"?selectMarch:selectShoulders))
                {selectMarch=oldMarch;selectShoulders=oldShoulders;return Reject("Keep at least one compatible movement selected.");}
                Persist();PrepareWorkout();accepted=true;break;
            case "confirmSpace":gate.EnvironmentConfirmed=a.Enabled;accepted=true;break;
            case "start":
                if(!SetupReady || closed)return Reject("Finish setup before entering a workout.");
                if(SessionActive)return Reject("A workout is already running or paused.");
                if(!gate.CanExercise)return Reject("Confirm clear space and restore every simulated readiness signal.");
                if(readiness.concern!=SafetyConcern.NoneReported)return Reject("A reported safety concern blocks exercise. Stop and seek appropriate advice.");
                var approved=dailyEnabled?ApprovedDailyPlan():null;
                if(dailyEnabled && approved==null)return Reject("Review a compatible daily recommendation after confirming your space.");
                Persist();
                if(dailyEnabled){workout=new WorkoutMachine(approved);record=null;}else PrepareWorkout();
                if(workout==null)return Reject("No compatible starter movement. Edit your setup preferences.");
                accepted=workout.Start(gate.CanExercise);
                if(accepted)
                {
                    record=new SessionRecord{sessionId=Guid.NewGuid().ToString("N"),planId=workout.Plan.Id,startedUtc=DateTime.UtcNow.ToString("O")};
                    Persist();Log("Workout started by shared WorkoutMachine.");
                }
                break;
            case "tick":case "advance":
                if(!double.IsFinite(a.Seconds) || a.Seconds<0 || a.Seconds>600)throw new ArgumentException("Invalid test clock.");
                if(workout==null || !workout.IsRunning)return false;
                if(!gate.CanExercise){Interrupt();return false;}
                workout.Tick(a.Seconds);Persist();accepted=true;break;
            case "pause":Interrupt();accepted=true;break;
            case "resume":
                accepted=!closed && SetupReady && readiness.concern==SafetyConcern.NoneReported && workout!=null && workout.Resume(gate.CanExercise);
                if(accepted){Persist();Log("Workout explicitly resumed.");}break;
            case "stop":
                if(!SessionActive)return Reject("No active workout to stop.");
                workout.Abort();Persist();gate.EnvironmentConfirmed=false;accepted=true;Log("Workout ended safely.");break;
            case "exit":
                if(!a.Enabled){Interrupt();return Reject("Paused. Confirm exit to leave the simulation.");}
                workout?.Abort();Persist();gate.EnvironmentConfirmed=false;closed=true;accepted=true;Log("Simulation closed; local records retained.");break;
            case "return":closed=false;gate.EnvironmentConfirmed=false;accepted=true;break;
            case "signal":
                switch(a.Field)
                {
                    case "focus":gate.HasFocus=a.Enabled;break;
                    case "tracking":gate.TrackingReady=a.Enabled;break;
                    case "passthrough":gate.PassthroughReady=a.Enabled;break;
                    default:throw new ArgumentException("Unknown simulated signal.");
                }
                if(!a.Enabled)Interrupt();accepted=true;Log("Simulated "+a.Field+" "+(a.Enabled?"restored":"lost")+"; never auto-resumed.");break;
            case "writeFault":profiles.FailWrites=a.Enabled;accepted=true;Log("Profile write fault "+(a.Enabled?"enabled":"disabled")+".");break;
            case "reload":
                if(SessionActive)Interrupt();Persist();LoadProfile();accepted=true;Log("Reloaded stored profile. Active workout recovery is not implemented.");break;
            case "reset":
                if(!a.Enabled)return Reject("Explicit reset confirmation required.");
                if(SessionActive)return Reject("End the workout before deleting simulation data.");
                profiles.Delete();history.Clear();if(File.Exists(HistoryPath))File.Delete(HistoryPath);
                profiles.FailWrites=false;selectMarch=true;selectShoulders=true;LoadProfile();events.Clear();accepted=true;Log("This browser's simulation data reset. Quest data is untouched.");break;
            default:throw new ArgumentException("Unknown simulation action.");
        }
        if(!accepted && notice.Length==0)notice="Action rejected by the current state or safety checks.";
        return accepted;
    }
    bool Reject(string text){notice=text;return false;}
    void Interrupt()
    {flow.Interrupt();workout?.Pause();gate.EnvironmentConfirmed=false;Persist();Log("Paused; physical-space confirmation cleared.");}
    static void Edit(ProductProfile p,string field,JsonElement v)
    {
        switch(field)
        {
            case "coachId":p.coachId=v.GetString();break;
            case "language":p.language=v.GetString();break;
            case "posture":p.posture=Enum.Parse<Posture>(v.GetString());break;
            case "personality":p.personality=Enum.Parse<PersonalityStyle>(v.GetString());break;
            case "goal":p.goal=Enum.Parse<FitnessGoal>(v.GetString());break;
            case "experience":p.experience=Enum.Parse<ExperienceLevel>(v.GetString());break;
            case "workoutPreference":p.workoutPreference=Enum.Parse<WorkoutPreference>(v.GetString());break;
            case "preferredMinutes":p.preferredMinutes=v.GetInt32();break;
            case "movementLimits":p.movementLimits=(MovementLimits)v.GetInt32();break;
            case "hasEquipment":p.hasEquipment=v.GetBoolean();break;
            case "muted":p.muted=v.GetBoolean();break;
            case "reducedMotion":p.reducedMotion=v.GetBoolean();break;
            case "audioVolume":p.audioVolume=v.GetSingle();break;
            default:throw new ArgumentException("Unknown setup field.");
        }
    }
    string HistoryPath=>Path.Combine(root,"simulation-history.json");
    List<SessionRecord> LoadHistory()
    {
        try{return File.Exists(HistoryPath)?JsonSerializer.Deserialize<List<SessionRecord>>(File.ReadAllText(HistoryPath),SimulationJson.Options)??new List<SessionRecord>():new List<SessionRecord>();}
        catch(JsonException){return new List<SessionRecord>();}
    }
    void Persist()
    {
        if(record==null || workout==null)return;
        record.activeSeconds=workout.ActiveSeconds;record.completedSets=workout.CompletedSets;
        bool terminal=workout.State==WorkoutState.Completed || workout.State==WorkoutState.Aborted;
        if(terminal && record.outcome!=workout.State.ToString())gate.EnvironmentConfirmed=false;
        record.outcome=terminal?workout.State.ToString():"Interrupted";
        if(terminal && string.IsNullOrEmpty(record.endedUtc))record.endedUtc=DateTime.UtcNow.ToString("O");
        int index=history.FindIndex(r=>r.sessionId==record.sessionId);
        if(index<0)history.Insert(0,record);else history[index]=record;
        Directory.CreateDirectory(root);
        string temporary=HistoryPath+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(history,SimulationJson.Options));
        File.Move(temporary,HistoryPath,true);
    }
    void Log(string text){events.Insert(0,text);if(events.Count>12)events.RemoveAt(12);}
    public byte[] Export()=>Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{simulationOnly=true,profile=flow.Draft,sessions=history},SimulationJson.Options));
    public object Snapshot()
    {
        var p=flow.Draft;
        var look=catalog.Get(p.coachId);
        var selected=look.Copy();selected.personality=p.personality;
        var cue=workout==null?new CoachCue(CoachState.Welcome,new OfflineCoachDialogue().Welcome(selected,p.language)):
            new ConfiguredOfflineCoach(look,p.personality).Evaluate(workout);
        string key=flow.Step.ToString().ToLowerInvariant();
        string title=flow.Step==OnboardingStep.Complete?"FORMA Studio":SetupCopy.Get(key,p.language);
        string body=flow.Step==OnboardingStep.Review?SetupCopy.Get("reviewNote",p.language):flow.Step==OnboardingStep.Complete?cue.Text:SetupCopy.Get(key+"Body",p.language);
        if(flow.Step==OnboardingStep.Welcome)
            body=string.Join("\n",body.Split('\n').Take(2))+"\n"+(p.language=="tr"?"Fare/dokunma ile seç; boşluk tuşu ile duraklat. XR girdileri simüledir.":"Select with mouse or touch; Space pauses. XR inputs are simulated.");
        var compatible=new OfflineWorkoutRecommendations(new BeginnerWorkoutValidator()).Recommend(p);
        return new {
            engine="Shared C# / .NET 8",simulationOnly=true,closed,notice,
            setup=new{step=flow.Step.ToString(),index=(int)flow.Step,profile=p,title,body,flow.SafetyAcknowledged,flow.SpaceConfirmed,flow.CanFinish,flow.PersistenceBlocked,loadStatus=loadStatus.ToString(),loadBlocked=LoadBlocked},
            gate=new{focus=gate.HasFocus,tracking=gate.TrackingReady,passthrough=gate.PassthroughReady,space=gate.EnvironmentConfirmed,ready=gate.CanExercise},
            writeFault=profiles.FailWrites,coaches=catalog.All,
            workout=workout==null?null:new{state=workout.State.ToString(),remaining=workout.Remaining,activeSeconds=workout.ActiveSeconds,completedSets=workout.CompletedSets,set=workout.SetNumber,totalSets=workout.Current.Sets,exerciseId=workout.Current.Id,exerciseName=workout.Current.Name,instructions=workout.Current.Guidance,workout.IsRunning},
            compatible=compatible==null?Array.Empty<string>():compatible.Exercises.Select(e=>e.Name).ToArray(),
            coach=new{id=p.coachId,name=look.displayName,color=look.primaryColor,accent=look.accentColor,state=cue.State.ToString(),text=cue.Text},
            exercises=compatible==null?Array.Empty<object>():compatible.Exercises.Select(e=>(object)new{id=e.Id,name=e.Name,selected=e.Id=="march"?selectMarch:selectShoulders}).ToArray(),
            history,events,daily=DailySnapshot(),
            labels=new Dictionary<string,string>{["next"]=SetupCopy.Get("next",p.language),["back"]=SetupCopy.Get("back",p.language),["skip"]=SetupCopy.Get("skip",p.language),["finish"]=SetupCopy.Get("finish",p.language),["ack"]=SetupCopy.Get("ack",p.language),["space"]=SetupCopy.Get("space",p.language)},
            options=new{
                goals=Enum.GetValues<FitnessGoal>().Select(x=>new{value=x.ToString(),label=SetupCopy.Value(x,p.language)}),
                experiences=Enum.GetValues<ExperienceLevel>().Select(x=>new{value=x.ToString(),label=SetupCopy.Value(x,p.language)}),
                personalities=Enum.GetValues<PersonalityStyle>().Select(x=>new{value=x.ToString(),label=SetupCopy.Value(x,p.language)}),
                preferences=Enum.GetValues<WorkoutPreference>().Select(x=>new{value=x.ToString(),label=SetupCopy.Value(x,p.language)})
            }
        };
    }
}
public sealed class WebProfileCodec:IProfileCodec
{
    public string Encode(ProductProfile p)=>JsonSerializer.Serialize(p,SimulationJson.Options);
    public ProductProfile Decode(string json)
    {
        try
        {
            using var document=JsonDocument.Parse(json);
            if(!document.RootElement.TryGetProperty("schemaVersion",out var version) || version.GetInt32()!=2)
                return new ProductProfile{schemaVersion=version.ValueKind==JsonValueKind.Number?version.GetInt32():0};
            return JsonSerializer.Deserialize<ProductProfile>(json,SimulationJson.Options);
        }
        catch(Exception e) when(e is JsonException || e is InvalidOperationException || e is FormatException)
        {throw new ArgumentException("Invalid simulation profile JSON.");}
    }
}
public sealed class FaultableProfiles:IProductProfileRepository
{
    readonly IProductProfileRepository inner;
    public bool FailWrites {get;set;}
    public FaultableProfiles(IProductProfileRepository inner){this.inner=inner;}
    public ProfileLoadResult Load()=>inner.Load();
    public void Save(ProductProfile p){if(FailWrites)throw new IOException("Injected simulation write failure.");inner.Save(p);}
    public void Delete()=>inner.Delete();
    public string Export()=>inner.Export();
}
