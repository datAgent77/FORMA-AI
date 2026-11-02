using Forma.Domain;
using Forma.Product;

public sealed partial class Simulation
{
    DailyReadiness readiness=new DailyReadiness();
    WorkoutSpace dailySpace=new WorkoutSpace();
    bool dailyEnabled;
    string dailyStage="CheckIn",dailyChoice;
    void InitializeDaily()
    {
        readiness=new DailyReadiness();var p=flow.Draft;
        dailySpace=new WorkoutSpace{kind=p.posture==Posture.Seated?SpaceKind.SeatedArea:SpaceKind.StandingInPlace,equipmentReported=p.hasEquipment};
        dailyStage="CheckIn";dailyChoice=null;dailyEnabled=false;
    }
    DailyRecommendation DailyResult()
    {
        dailySpace.confirmed=gate.EnvironmentConfirmed;
        return new DailyRecommendationEngine().Recommend(flow.Draft,readiness,dailySpace,history,TrackingCapabilities.None,DateTimeOffset.UtcNow);
    }
    WorkoutPlan ApprovedDailyPlan()
    {
        var result=DailyResult();return result.Status==DailyStatus.Ready && dailyStage=="Recommendation"?(result.Find(dailyChoice)??(dailyChoice==null?result.Primary:null))?.Plan:null;
    }
    bool ActDaily(SimulationAction a)
    {
        if(!SetupReady || closed)return Reject("Complete setup and return to Studio first.");
        if(SessionActive && !(a.Action=="dailyEdit" && a.Field=="concern"))return Reject("End the active session before changing daily conditions.");
        switch(a.Action)
        {
            case "dailyEdit":
                var draft=readiness.Copy();var space=dailySpace.Copy();
                switch(a.Field)
                {
                    case "energy":draft.energy=Enum.Parse<DailyEnergy>(a.Value.GetString());draft.skipped=false;break;
                    case "availableMinutes":draft.availableMinutes=a.Value.GetInt32();draft.skipped=false;break;
                    case "activity":draft.activity=Enum.Parse<DailyActivity>(a.Value.GetString());draft.skipped=false;break;
                    case "concern":draft.concern=Enum.Parse<SafetyConcern>(a.Value.GetString());break;
                    case "space":space.kind=Enum.Parse<SpaceKind>(a.Value.GetString());space.confirmed=false;break;
                    case "equipment":space.equipmentReported=a.Value.GetBoolean();break;
                    default:throw new ArgumentException("Unknown daily field.");
                }
                if(!DailyReadiness.Valid(draft) || !Enum.IsDefined(typeof(SpaceKind),space.kind))return Reject("Invalid daily preference.");
                readiness=draft;dailySpace=space;dailyStage="CheckIn";dailyChoice=null;dailyEnabled=true;
                if(a.Field=="space")gate.EnvironmentConfirmed=false;
                if(draft.concern!=SafetyConcern.NoneReported){Interrupt();notice=DailyResult().Message;}
                return true;
            case "dailyQuick":
                // Skip optional check-in fields, never a previously reported safety concern.
                readiness=new DailyReadiness{skipped=true,energy=DailyEnergy.Unspecified,availableMinutes=0,concern=readiness.concern};
                goto case "dailyRecommend";
            case "dailyRecommend":
                dailyEnabled=true;dailyStage="Recommendation";var result=DailyResult();dailyChoice=result.Primary?.Id;
                if(result.Status!=DailyStatus.Ready)notice=result.Message;
                Log("Daily recommendation evaluated using the shared offline rules.");return result.Status==DailyStatus.Ready;
            case "dailyChoose":
                var choices=DailyResult();var chosen=choices.Find(a.Value.GetString());
                if(choices.Status!=DailyStatus.Ready || chosen==null)return Reject("That option does not match the current constraints.");
                dailyChoice=chosen.Id;dailyEnabled=true;dailyStage="Recommendation";return true;
            case "dailyBack":Persist();workout=null;record=null;dailyStage="CheckIn";dailyChoice=null;dailyEnabled=true;return true;
            default:throw new ArgumentException("Unknown daily action.");
        }
    }
    object DailySnapshot()
    {
        var p=flow.Draft;var facts=new RecordedHistory(history,DateTimeOffset.UtcNow);var result=DailyResult();
        var selected=result.Find(dailyChoice);
        object Card(DailyWorkout w)=>w==null?null:new{id=w.Id,name=w.Name,durationSeconds=w.DurationSeconds,intensity=w.Intensity,equipment=w.Equipment,activity=w.Activity.ToString(),exercises=w.Plan.Exercises.Select(e=>e.Name)};
        var reasons=result.Reasons.ToList();
        if(selected!=null && selected.Id!=result.Primary?.Id)reasons.Insert(0,DailyText.T(p.language,"You selected another compatible gentle option.","Başka bir uyumlu hafif seçenek seçtin."));
        bool terminal=workout!=null && (workout.State==WorkoutState.Completed || workout.State==WorkoutState.Aborted);
        return new {
            enabled=dailyEnabled,unsupportedStrength=readiness.activity==DailyActivity.Strength,stage=dailyStage,readiness,space=dailySpace,
            greeting=facts.Greeting(p),status=result.Status.ToString(),message=result.Message,reasons,
            primary=Card(selected??result.Primary),alternatives=result.Primary==null?Array.Empty<object>():new[]{result.Primary}.Concat(result.Alternatives).Where(x=>x.Id!=(selected??result.Primary).Id).Select(Card).ToArray(),
            coachState=RecordedHistory.CoachState(SessionActive||terminal?workout:null,dailyStage=="Recommendation",terminal,closed).ToString(),
            summary=terminal?new{outcome=workout.State.ToString(),activeSeconds=workout.ActiveSeconds,sets=workout.CompletedSets,comparison=facts.Comparison(record,p.language),completedSessions=facts.CompletedCount,recordSaved=record!=null&&history.Any(r=>r.sessionId==record.sessionId)}:null,
            scope=DailyText.T(p.language,"Reviewed starters are 1–3 minutes. Time choices are budgets, not promised program lengths. No strength program is available yet.","İncelenmiş başlangıçlar 1–3 dakika. Süreler bütçedir, program uzunluğu vaadi değildir. Kuvvet programı henüz yok.")
        };
    }
}
