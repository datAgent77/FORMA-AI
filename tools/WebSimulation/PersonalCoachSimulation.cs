using Forma.Product;
using Forma.Domain;
using System.Text.Json;
public sealed partial class Simulation
{
    PersonalTrainingRepository trainingStore;
    PersonalTrainingRepository TrainingStore=>trainingStore??=new PersonalTrainingRepository(root);
    PersonalSessionEngine personalSession;
    PersonalRecommendation personalRecommendation;
    bool personalMode,personalSaveFailed;
    bool PersonalActive=>personalSession?.IsActive==true;
    AthleteProfile RequiredAthlete()=>AthleteStore.Load().profile??throw new InvalidOperationException("Create an athlete profile first.");
    CoachMemoryService Memory(PersonalTrainingDocument d)=>new CoachMemoryService(d.sessions,history);
    bool PersonalReject(string tr,string en)
    {try{return Reject(AthleteStore.Load().profile?.preferences.language=="en"?en:tr);}catch(IOException){return Reject(tr);}}
    bool PersonalAction(SimulationAction a)
    {
        var now=DateTimeOffset.UtcNow;
        switch(a.Action)
        {
            case "coachRecommend":
                if(SessionActive)return PersonalReject("Önce açık seansı bitir.","End the active session first.");
                var doc=AthleteStore.Load();if(doc.profile==null)return PersonalReject("Önce sporcu profilini oluştur.","Create an athlete profile first.");
                var data=TrainingStore.Load();var memory=Memory(data);
                var recommendation=new AdaptiveTrainingEngine().Recommend(doc.profile,doc.revision,memory,now);
                // Persist decisions before they are offered. Reopening does not manufacture training history.
                data.recommendations.Add(recommendation);TrainingStore.Save(data);personalRecommendation=recommendation;personalSession=null;personalMode=true;gate.EnvironmentConfirmed=false;return true;
            case "coachStart":
                if(SessionActive || !SetupReady || !gate.CanExercise || personalRecommendation?.status!="Ready" || readiness.concern!=SafetyConcern.NoneReported)return PersonalReject("Başlamadan önce planı incele, kurulumu tamamla ve alanını onayla.","Review a plan, finish setup and confirm space before starting.");
                var athlete=RequiredAthlete();if(AthleteStore.Load().revision!=personalRecommendation.profileRevision)return PersonalReject("Sporcu tercihleri değişti; yeni bir plan iste.","Athlete preferences changed; request a new plan.");
                var candidate=new PersonalSessionEngine(athlete.athleteId,personalRecommendation.recommendationId,personalRecommendation.blocks,athlete.preferences,now);
                if(!candidate.Start(gate.CanExercise))return false;
                var startData=TrainingStore.Load();
                var fresh=new AdaptiveTrainingEngine().Recommend(athlete,AthleteStore.Load().revision,Memory(startData),now);
                if(fresh.status!="Ready" || !fresh.blocks.Select(b=>$"{b.exerciseId}/{b.stage}/{b.workSeconds}/{b.restSeconds}").SequenceEqual(personalRecommendation.blocks.Select(b=>$"{b.exerciseId}/{b.stage}/{b.workSeconds}/{b.restSeconds}")))return PersonalReject("Antrenman kayıtları değişti. Başlamadan önce yeni öneriyi incele.","Training evidence changed. Review a new recommendation before starting.");
                startData.sessions.Insert(0,candidate.Record);TrainingStore.Save(startData);personalSession=candidate;personalMode=true;return true;
            case "tick":case "advance":
                if(personalSession==null || !personalSession.IsRunning)return false;
                if(!gate.CanExercise){personalSession.Pause();SavePersonal();return false;}
                personalSession.Tick(a.Seconds,gate.CanExercise);SavePersonal();return true;
            case "pause":personalSession?.Pause();gate.EnvironmentConfirmed=false;SavePersonal();return true;
            case "resume":
                if(!SetupReady || readiness.concern!=SafetyConcern.NoneReported || personalSession==null || !personalSession.Resume(gate.CanExercise))return false;
                SavePersonal();return true;
            case "coachSkip":
                if(personalSession==null || !personalSession.Skip())return false;SavePersonal();return true;
            case "coachFeedback":
                var feedback=JsonSerializer.Deserialize<ExerciseFeedback>(a.Value.GetRawText(),SimulationJson.Options);
                if(personalSession==null || !personalSession.Report(feedback,gate.CanExercise && readiness.concern==SafetyConcern.NoneReported,now))return false;
                if(!personalSession.IsActive)gate.EnvironmentConfirmed=false;SavePersonal();return true;
            case "coachRetrySave":SavePersonal();return true;
            case "stop":case "exit":
                if(a.Action=="exit" && !a.Enabled){personalSession?.Pause();gate.EnvironmentConfirmed=false;SavePersonal();return false;}
                personalSession?.Cancel(now);gate.EnvironmentConfirmed=false;SavePersonal();return true;
            case "coachBack":
                if(PersonalActive)return false;personalSession=null;personalRecommendation=null;personalMode=false;gate.EnvironmentConfirmed=false;return true;
            default:throw new ArgumentException("Unknown personal coach action.");
        }
    }
    void SavePersonal()
    {
        if(personalSession==null)return;try{var d=TrainingStore.Load();int i=d.sessions.FindIndex(x=>x.sessionId==personalSession.Record.sessionId);
        if(i<0)d.sessions.Insert(0,personalSession.Record);else d.sessions[i]=personalSession.Record;TrainingStore.Save(d);personalSaveFailed=false;}catch{personalSaveFailed=true;personalSession.Pause();gate.EnvironmentConfirmed=false;throw;}
    }
    object PersonalSnapshot()
    {
        try
        {
            var d=TrainingStore.Load();var profile=AthleteStore.Load().profile;
            var memory=Memory(d);string language=profile?.preferences.language??"tr";
            object ExerciseView(PersonalExercise e)=>new{id=e.Id,name=e.Name(language),description=e.Guidance(language),category=e.Category,difficulty=e.Difficulty,equipment=e.Equipment,animation=e.AnimationReference,safety=e.Safety(language),alternatives=e.Alternatives};
            var r=personalRecommendation;
            return new{status="Ready",enabled=personalMode,savePending=personalSaveFailed,greeting=profile==null?"":memory.Greeting(profile,DateTimeOffset.UtcNow),completedCount=memory.CompletedCount,
                completedThisWeek=memory.CompletedThisWeek(DateTimeOffset.UtcNow),recommendation=r==null?null:new{id=r.recommendationId,r.rule,r.status,r.reasons,r.evidenceSessionIds,durationSeconds=r.PlannedSeconds,blocks=r.blocks.Select((b,i)=>new{index=i,b.stage,b.workSeconds,b.restSeconds,exercise=ExerciseView(PersonalWorkoutCatalog.Find(b.exerciseId))})},
                session=personalSession==null?null:new{id=personalSession.Record.sessionId,state=personalSession.State.ToString(),awaitingFeedback=personalSession.AwaitingFeedback,index=personalSession.Clock.ExerciseIndex,record=personalSession.Record},
                history=d.sessions,decisions=d.recommendations,catalog=PersonalWorkoutCatalog.All.Select(ExerciseView)};
        }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException){return new{status="Unavailable",enabled=personalMode};}
    }
    CoachCue PersonalCoachCue()
    {
        var p=RequiredAthlete();bool tr=p.preferences.language=="tr";string T(string turkish,string english)=>tr?turkish:english;
        if(personalSaveFailed)return new CoachCue(CoachState.Paused,T("Sonuç henüz diske kaydedilemedi. Hareketi durdur ve kaydetmeyi yeniden dene.","Results have not been saved to disk. Stop moving and retry saving."));
        if(personalSession==null)return new CoachCue(CoachState.Welcome,Memory(TrainingStore.Load()).Greeting(p,DateTimeOffset.UtcNow));
        var s=personalSession;var w=s.Clock;
        if(s.AwaitingFeedback)return new CoachCue(CoachState.Paused,T("Aralık bitti. Tamamlanma durumunu ve zorluğunu bildir. Tekrar sayısı ölçülmedi.","Interval finished. Report completion and difficulty. Repetitions were not measured."));
        if(s.State==PersonalSessionState.COMPLETED)return new CoachCue(CoachState.Celebrating,T($"Seans kaydedildi. {s.Record.feedback.Count(x=>x.outcome==ExerciseReport.Completed)} aralığı tamamlandı olarak bildirdin. Dinlen; sonraki planını bu kayıtla değerlendireceğim.",$"Session saved. You reported {s.Record.feedback.Count(x=>x.outcome==ExerciseReport.Completed)} intervals completed. Rest; the next plan will use this record."));
        if(s.Record.outcome=="Skipped")return new CoachCue(CoachState.Stopped,T("Bu seansı atlandı olarak bildirdin; tamamlanmış sayılmadı.","You reported this session skipped; it was not counted as completed."));
        if(s.State==PersonalSessionState.CANCELLED)return new CoachCue(CoachState.Stopped,T("Seans sonlandırıldı; tamamlanmış sayılmadı. Rahatsızlık varsa dur ve gerektiğinde uygun uzman desteği al.","Session ended and was not counted as completed. Stop for discomfort and seek appropriate advice when needed."));
        if(w.State==WorkoutState.Introduction)return new CoachCue(CoachState.Welcome,T($"Bugün temel hareket pratiği yapacağız. Planın {Math.Round(personalRecommendation.PlannedSeconds)} saniye; ısınma, hareketler ve soğuma içeriyor. Rahat bir tempoda kal.",$"Today is foundational movement practice. The plan has {Math.Round(personalRecommendation.PlannedSeconds)} timer seconds, including warm-up, exercises and cooldown. Keep a comfortable pace."));
        if(w.State==WorkoutState.Paused)return new CoachCue(CoachState.Paused,T("Duraklatıldı. Hareketi bırak, çevreni kontrol et. Alanını onayladıktan sonra açıkça devam et.","Paused. Stop moving and check your surroundings. Confirm space, then explicitly resume."));
        if(w.State==WorkoutState.Rest){var next=PersonalWorkoutCatalog.Find(s.Record.blocks[Math.Min(w.ExerciseIndex+1,s.Record.blocks.Count-1)].exerciseId);return new CoachCue(CoachState.Recovering,T("Dinlen, rahat nefes al. Sıradaki: "+next.Name("tr"),"Rest and breathe comfortably. Next: "+next.Name("en")));}
        return new CoachCue(CoachState.Guiding,w.Current.Guidance);
    }
    object PersonalWorkoutSnapshot()
    {
        if(personalSession==null)return null;var s=personalSession;var w=s.Clock;
        string state=personalSaveFailed?"Paused":s.State==PersonalSessionState.COMPLETED?"Completed":(s.State==PersonalSessionState.CANCELLED || s.Record.outcome=="Skipped")?"Aborted":s.AwaitingFeedback?"Paused":w.State.ToString();
        return new{state,remaining=w.Remaining,intervalSeconds=w.CurrentIntervalSeconds,activeSeconds=w.ActiveSeconds,completedSets=s.Record.feedback.Count(x=>x.outcome==ExerciseReport.Completed),set=1,totalSets=1,exerciseId=w.Current.Id,exerciseName=w.Current.Name,instructions=w.Current.Guidance,isRunning=!personalSaveFailed && s.IsRunning};
    }
    object[] PersonalPlanSnapshot()=>(personalRecommendation?.blocks??new List<PersonalBlock>()).Select(b=>(object)new{id=b.exerciseId,name=PersonalWorkoutCatalog.Find(b.exerciseId).Name(RequiredAthlete().preferences.language),sets=1,workSeconds=b.workSeconds}).ToArray();
    object PersonalSummarySnapshot()
    {
        if(personalSession==null)return new{enabled=true,stage="Recommendation",summary=(object)null};
        var r=personalSession.Record;bool terminal=!personalSaveFailed && (r.outcome=="Completed" || r.outcome=="Cancelled" || r.outcome=="Skipped");
        var d=TrainingStore.Load();return new{enabled=true,stage="Recommendation",summary=terminal?(object)new{outcome=r.outcome=="Completed"?"Completed":"Aborted",activeSeconds=r.timedActiveSeconds,sets=r.feedback.Count(x=>x.outcome==ExerciseReport.Completed),completedSessions=Memory(d).CompletedCount,recordSaved=d.sessions.Any(x=>x.sessionId==r.sessionId && x.outcome==r.outcome)}:null};
    }
}
