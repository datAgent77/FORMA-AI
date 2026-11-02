using System;
using System.IO;
using System.Text;
using Forma.Domain;
using Forma.Product;
using System.Collections.Generic;
using UnityEngine;
namespace Forma.Runtime
{
    public sealed class StudioController : MonoBehaviour
    {
        public StudioPlatform platform;
        public OnboardingController onboarding;
        public DailyExperienceController daily;
        WorkoutPlan dailyOverride;
        public WorkoutMachine CurrentWorkout=>workout;
        public bool UseDailyPlan(WorkoutPlan plan)
        {
            if(!CanEditPreferences || plan==null || (record!=null && !Persist()))return false;
            record=null;dailyOverride=plan;workoutAvailable=true;workout=new WorkoutMachine(plan);InvalidateSpaceConfirmation();return true;
        }
        public bool ResetForDaily()
        {
            if(!CanEditPreferences || (record!=null && !Persist()))return false;
            record=null;dailyOverride=null;CreateWorkout();InvalidateSpaceConfirmation();return true;
        }
        ProductProfile productProfile;
        WorkoutPlan compatiblePlan;
        bool workoutAvailable=true;
        public bool CanEditPreferences => workout!=null && Editable;
        public bool ExitAwaitingConfirmation => exitPending;
        public void RequestExit()=>Exit();
        public void ShowProductNotice(string text){message=text;}
        public void InvalidateSpaceConfirmation(){safety.EnvironmentConfirmed=false;}
        public void ConfigureCoach(CoachDefinition appearance,PersonalityStyle style){policy=new ConfiguredOfflineCoach(appearance,style);}
        public void ApplyProductProfile(ProductProfile value)
        {
            dailyOverride=null;
            productProfile=value.Copy();
            compatiblePlan=new OfflineWorkoutRecommendations(new BeginnerWorkoutValidator()).Recommend(productProfile);
            if(workout!=null && Editable)
            {
                // Preserve a terminal record before replacing its machine for new preferences.
                if(record!=null && !Persist())return;
                record=null;CreateWorkout();
            }
            InvalidateSpaceConfirmation();
        }
        public CoachAvatar avatar;
        public TextMesh status,coach,history,summary;
        public StudioButton marchButton,shoulderButton,confirmButton,startButton,pauseButton,endButton,exitButton;
        public StudioButton repsButton,weightButton,entryExerciseButton,deleteButton;
        readonly SafetyGate safety=new SafetyGate();
        ICoachPolicy policy=new DeterministicCoach();
        LocalStore store;
        LocalProfile profile;
        WorkoutMachine workout;
        SessionRecord record;
        bool focused=true,appPaused,deletePending,exitPending;
        string message="";
        string entryExercise="march";
        double last;
        void Start()
        {
            store=new LocalStore(Path.Combine(Application.persistentDataPath,"forma-v1"));
            try { profile=store.LoadProfile(); } catch (Exception e) when (StorageError(e)) { profile=new LocalProfile(); message="Profile unavailable; using defaults."; }
            CreateWorkout();
            marchButton.Click=()=>Select(true);
            shoulderButton.Click=()=>Select(false);
            confirmButton.Click=()=>{ safety.EnvironmentConfirmed=!safety.EnvironmentConfirmed; deletePending=false; };
            startButton.Click=StartWorkout;
            pauseButton.Click=TogglePause;
            endButton.Click=()=>{ workout.Abort(); Persist(); };
            exitButton.Click=Exit;
            repsButton.Click=()=>EditEntry(false);
            weightButton.Click=()=>EditEntry(true);
            entryExerciseButton.Click=()=>{ entryExercise=entryExercise=="march"?"shoulders":"march"; };
            deleteButton.Click=Delete;
            last=Time.realtimeSinceStartupAsDouble;
            RefreshHistory();
        }
        bool Editable => workout.State==WorkoutState.Ready || workout.State==WorkoutState.Completed || workout.State==WorkoutState.Aborted;
        bool Allowed(string id)
        {
            if(productProfile==null)return true;
            if(compatiblePlan==null)return false;
            foreach(var e in compatiblePlan.Exercises)if(e.Id==id)return true;
            return false;
        }
        void CreateWorkout()
        {
            if(dailyOverride!=null){workoutAvailable=true;workout=new WorkoutMachine(dailyOverride);return;}
            if(productProfile==null)
            {
                if(!profile.selectMarch && !profile.selectShoulders)profile.selectMarch=true;
                workout=new WorkoutMachine(StarterWorkouts.Create(profile.selectMarch,profile.selectShoulders));
                return;
            }
            workoutAvailable=compatiblePlan!=null;
            if(!workoutAvailable)
            {
                // Inert Ready machine only, never startable; preserve old view references.
                workout=new WorkoutMachine(StarterWorkouts.Create(false,true));return;
            }
            profile.selectMarch &= Allowed("march");profile.selectShoulders &= Allowed("shoulders");
            if(!profile.selectMarch && !profile.selectShoulders)
            { profile.selectMarch=Allowed("march");profile.selectShoulders=Allowed("shoulders"); }
            var chosen=new List<Exercise>();
            foreach(var e in compatiblePlan.Exercises)
                if(e.Id=="march"?profile.selectMarch:profile.selectShoulders)chosen.Add(e);
            workout=new WorkoutMachine(new WorkoutPlan(compatiblePlan.Id,chosen));
        }
        void Select(bool march)
        {
            if (!Editable) return;
            if(dailyOverride!=null){message="Use Today to choose another compatible session.";return;}
            if (!Allowed(march?"march":"shoulders")){message="That movement is excluded by your setup preferences.";return;}
            if (record!=null && !Persist()) return;
            if (march) profile.selectMarch=!profile.selectMarch; else profile.selectShoulders=!profile.selectShoulders;
            if (!profile.selectMarch && !profile.selectShoulders) { if (march && Allowed("shoulders")) profile.selectShoulders=true; else if (!march && Allowed("march")) profile.selectMarch=true; else { if(march)profile.selectMarch=true;else profile.selectShoulders=true; } }
            CreateWorkout(); record=null;
            try { store.SaveProfile(profile); } catch (Exception e) when (StorageError(e)) { message="Could not save exercise preferences."; }
        }
        void StartWorkout()
        {
            if (!Editable) return;
            if(onboarding!=null && !onboarding.ReadyForWorkout){message="Finish setup before starting a workout.";return;}
            if(daily!=null && !daily.CanStart(workout.Plan.Id)){message="Review today's compatible recommendation before starting.";return;}
            if(!workoutAvailable){message="No compatible starter session. Edit movement preferences; no workout has started.";return;}
            UpdateSafety();
            if (!safety.CanExercise) { message="Check passthrough, tracking and your clear space before starting."; return; }
            if (record!=null && !Persist()) return;
            CreateWorkout();
            if (!workout.Start(safety.CanExercise)) return;
            record=new SessionRecord { sessionId=Guid.NewGuid().ToString("N"),planId=workout.Plan.Id,startedUtc=DateTime.UtcNow.ToString("O"), outcome="InProgress" };
            exitPending=false; message="";
            // Checkpoint immediately; an unexpected process kill remains explicitly incomplete.
            Persist();
        }
        public void Pause()
        {
            if (workout==null) return;
            onboarding?.SuspendSetup();
            workout.Pause(); safety.EnvironmentConfirmed=false;
            Persist();
        }
        void TogglePause()
        {
            if (workout.State==WorkoutState.Paused)
            {
                UpdateSafety();
                if (!workout.Resume(safety.CanExercise)) message="Confirm clear space and visible surroundings before resuming.";
                else { last=Time.realtimeSinceStartupAsDouble; exitPending=false; message=""; }
            }
            else Pause();
        }
        void UpdateSafety()
        {
            safety.PassthroughReady=platform!=null && platform.PassthroughReady;
            safety.TrackingReady=platform!=null && platform.TrackingReady;
            safety.HasFocus=focused && !appPaused && platform!=null && platform.HasFocus;
        }
        void Update()
        {
            if (workout==null) return;
            double now=Time.realtimeSinceStartupAsDouble;
            double dt=now-last; last=now;
            UpdateSafety();
            if (workout.IsRunning && (!safety.CanExercise || dt>1)) { Pause(); message="Stopped for focus, tracking, passthrough or timing interruption."; }
            var before=workout.State; int beforeSets=workout.CompletedSets;
            workout.Tick(Math.Max(0,Math.Min(dt,1)));
            if (before!=workout.State || beforeSets!=workout.CompletedSets) Persist();
            bool terminal=workout.State==WorkoutState.Completed || workout.State==WorkoutState.Aborted;
            summary.gameObject.SetActive(terminal);
            if (terminal) summary.text=(workout.State==WorkoutState.Completed?"WORKOUT COMPLETE":"SESSION ENDED")+
                "\nSummary: "+Math.Floor(workout.ActiveSeconds)+" seconds of work / "+workout.CompletedSets+" timed sets";
            var cue=policy.Evaluate(workout); coach.text=cue.Text;
            if (avatar!=null) avatar.Present(workout,(float)Math.Min(dt,0.1));
            string preview=Application.isEditor?"EDITOR PREVIEW - no physical passthrough\n":"";
            status.text=preview+"FORMA STUDIO  /  "+workout.State+"\n"+workout.Current.Name+"  |  Set "+workout.SetNumber+"/"+workout.Current.Sets+
                "  |  "+Math.Ceiling(workout.Remaining)+"s\n"+"Work time: "+Math.Floor(workout.ActiveSeconds)+"s  /  Sets finished: "+workout.CompletedSets+
                "\n"+message;
            if(!workoutAvailable)status.text="No compatible starter session.\nEdit movement preferences before starting.\n"+message;
            marchButton.Label.text=(profile.selectMarch?"[x] ":"[ ] ")+"Gentle march";
            shoulderButton.Label.text=(profile.selectShoulders?"[x] ":"[ ] ")+"Shoulder rolls";
            confirmButton.Label.text=safety.EnvironmentConfirmed?"[x] Space clear / surroundings visible":"Confirm: space clear / surroundings visible";
            pauseButton.Label.text=workout.State==WorkoutState.Paused?"Resume":"PAUSE / STOP MOVING";
            startButton.Label.text=workout.State==WorkoutState.Ready?"Start guided workout":"Start new session";
            deleteButton.Label.text=deletePending?"Confirm delete local data":"Delete local data";
            exitButton.Label.text=exitPending?"Confirm exit app":"Exit app";
            entryExerciseButton.Label.text="Manual entry: "+entryExercise;
            ManualEntry entry=record?.manualEntries.Find(x=>x.exerciseId==entryExercise);
            repsButton.Label.text="Reps +1 (manual): "+(entry?.repetitions ?? 0);
            weightButton.Label.text="Kg +0.5 (manual): "+(entry?.weightKg ?? 0)+" / max 5";
        }
        void EditEntry(bool weight)
        {
            if (record==null || (workout.State!=WorkoutState.Paused && workout.State!=WorkoutState.Completed && workout.State!=WorkoutState.Aborted))
            { message="Pause or finish before entering manual values."; return; }
            if (!workout.Plan.Exercises.ExistsById(entryExercise)) { message="Choose an exercise from this session."; return; }
            var entry=record.manualEntries.Find(x=>x.exerciseId==entryExercise);
            if (entry==null) { entry=new ManualEntry {exerciseId=entryExercise}; record.manualEntries.Add(entry); }
            if (weight) entry.weightKg=entry.weightKg>=5?0:entry.weightKg+0.5f;
            else entry.repetitions=entry.repetitions>=100?0:entry.repetitions+1;
            message="Manual log only. This workout uses no equipment. Values wrap to zero."; Persist();
        }
        bool Persist()
        {
            if (record==null) return true;
            record.activeSeconds=workout.ActiveSeconds; record.completedSets=workout.CompletedSets;
            bool terminal=workout.State==WorkoutState.Completed || workout.State==WorkoutState.Aborted;
            if(terminal && record.outcome!=workout.State.ToString())safety.EnvironmentConfirmed=false;
            record.outcome=terminal?workout.State.ToString():"Interrupted";
            if (terminal && string.IsNullOrEmpty(record.endedUtc)) record.endedUtc=DateTime.UtcNow.ToString("O");
            try { store.Save(record); RefreshHistory(); return true; }
            catch (Exception e) when (StorageError(e)) { message="Local save failed. Free device storage, then pause/end to retry."; return false; }
        }
        void RefreshHistory()
        {
            try
            {
                var rows=store.Load(); var text=new StringBuilder("LOCAL SESSION HISTORY\n");
                for (int i=0;i<Math.Min(4,rows.Count);i++)
                {
                    var r=rows[i]; text.Append(r.startedUtc?.Substring(0,Math.Min(16,r.startedUtc.Length))).Append("  ").Append(r.outcome)
                        .Append("  ").Append(Math.Floor(r.activeSeconds)).Append("s / ").Append(r.completedSets).Append(" sets\n");
                    foreach(var entry in r.manualEntries ?? new System.Collections.Generic.List<ManualEntry>())
                        text.Append("  Manual ").Append(entry.exerciseId).Append(": ").Append(entry.repetitions).Append(" reps, ").Append(entry.weightKg).Append(" kg\n");
                }
                if (rows.Count==0) text.Append("No sessions yet. Stored only on this device.");
                history.text=text.ToString();
            }
            catch(Exception e) when(StorageError(e)) { history.text="Local history unavailable."; }
        }
        void Delete()
        {
            if (!Editable) { message="End the session before deleting history."; return; }
            if (!deletePending) { deletePending=true; return; }
            try { store.DeleteAll(); onboarding?.DeleteProductProfile();
                string exports=Path.Combine(Application.persistentDataPath,"forma-exports");if(Directory.Exists(exports))Directory.Delete(exports,true);
                record=null;productProfile=null;compatiblePlan=null;workoutAvailable=true; profile=new LocalProfile(); CreateWorkout(); deletePending=false; RefreshHistory(); message="Local profile and history deleted."; }
            catch(Exception e) when(StorageError(e)) { message="Could not delete local data."; }
        }
        void Exit()
        {
            if (!exitPending) { Pause(); exitPending=true; message="Session paused. Select Confirm exit app to leave."; return; }
            workout.Abort(); if (!Persist()) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnApplicationFocus(bool value) { focused=value; if (!value) Pause(); }
        void OnApplicationPause(bool value) { appPaused=value; if (value) Pause(); }
        void OnApplicationQuit() { if (workout!=null) { workout.Abort(); Persist(); } }
        static bool StorageError(Exception e) => e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException;
    }
    static class ExerciseListExtensions
    {
        public static bool ExistsById(this System.Collections.ObjectModel.ReadOnlyCollection<Exercise> values,string id)
        { foreach(var v in values) if(v.Id==id) return true; return false; }
    }
}
