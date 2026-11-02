using System;
using System.IO;
using Forma.Product;
using Forma.Storage;
using UnityEngine;
namespace Forma.Runtime
{
    [DefaultExecutionOrder(-50)]
    public sealed class OnboardingController : MonoBehaviour
    {
        public StudioController studio;
        public Camera view;
        public GameObject workoutPresentation;
        public CoachAppearance appearance;
        public StudioButton editButton,exportButton,repositionButton;
        IProductProfileRepository repository;
        CoachCatalog catalog;
        OnboardingFlow flow;
        SetupPanel panel;
        string error="";
        bool loadBlocked,resetArmed;
        int coachSlot;
        public bool ReadyForWorkout => !loadBlocked && flow!=null && flow.Step==OnboardingStep.Complete && catalog!=null;
        public ProductProfile CurrentProfile => flow?.Draft;
        string Language => flow?.Draft.language ?? "en";
        string Copy(string key)=>SetupCopy.Get(key,Language);
        void Start()
        {
            var panelObject=new GameObject("FORMA first-use setup");panel=panelObject.AddComponent<SetupPanel>();panel.Build();panel.PlaceInFront(view);
            repository=new ProductProfileRepository(Path.Combine(Application.persistentDataPath,"forma-v1"),new UnityProfileCodec());
            try
            {
                var data=Resources.Load<TextAsset>("Forma/Coaches");
                if(data==null)throw new ArgumentException("Coach definitions missing.");
                catalog=new CoachCatalog(JsonUtility.FromJson<CoachCatalogDocument>(data.text));
            }
            catch(ArgumentException){error="Coach setup unavailable. Reinstall the app; workouts are blocked.";Debug.LogError("FORMA_COACH_CATALOG_INVALID");}
            Load();
            if(catalog!=null)Preview();
            if(appearance!=null)appearance.Place(view,coachSlot);
            editButton.Click=OpenSettings;exportButton.Click=ExportProfile;repositionButton.Click=MoveCoach;
        }
        void Load()
        {
            var result=repository.Load();
            loadBlocked=result.Status!=ProfileLoadStatus.Loaded && result.Status!=ProfileLoadStatus.Missing;
            flow=new OnboardingFlow(repository,result.Profile);
            if(!loadBlocked && ReadyForWorkout)Apply();
            Refresh();
        }
        public void OpenSettings()
        {
            if(!studio.CanEditPreferences)return;
            flow.Reopen();error="";resetArmed=false;Refresh();
        }
        public void SuspendSetup()
        { if(flow!=null && !ReadyForWorkout){flow.Interrupt();Refresh();} }
        public void DeleteProductProfile()
        {
            repository.Delete();loadBlocked=false;resetArmed=false;flow=new OnboardingFlow(repository,new ProductProfile());
            error="";Refresh();
        }
        void Reset()
        {
            if(!resetArmed){resetArmed=true;Refresh();return;}
            try{DeleteProductProfile();}
            catch(Exception e) when(StorageFailure(e)){error=Copy("saveError");Refresh();}
        }
        void Edit(Action<ProductProfile> action)
        { flow.Edit(action);error="";Preview();Refresh(); }
        void Preview()
        {
            var p=flow.Draft;
            AudioListener.volume=p.muted?0:p.audioVolume;
            if(catalog!=null && appearance!=null)appearance.Apply(catalog.Get(p.coachId));
            if(studio.avatar!=null)studio.avatar.reducedMotion=p.reducedMotion;
        }
        void Apply()
        { Preview();studio.ApplyProductProfile(flow.Draft);studio.ConfigureCoach(catalog.Get(flow.Draft.coachId),flow.Draft.personality);if(appearance!=null)appearance.Place(view,coachSlot); }
        public void RequestCoachMove()=>MoveCoach();
        void MoveCoach()
        {
            if(!studio.CanEditPreferences && ReadyForWorkout)return;
            coachSlot=(coachSlot+1)%3;appearance?.Place(view,coachSlot);studio.InvalidateSpaceConfirmation();
            if(!ReadyForWorkout)flow.Interrupt();
            Refresh();
        }
        void Next()
        {
            if(flow.Step==OnboardingStep.Review)
            {
                if(flow.Finish()){Apply();error="";}else if(flow.PersistenceBlocked)error=Copy("saveError");
            }
            else if(!flow.Next())error=Copy("needSafety");
            Refresh();
        }
        void Refresh()
        {
            if(panel==null || flow==null)return;
            bool ready=ReadyForWorkout;panel.gameObject.SetActive(!ready);workoutPresentation.SetActive(ready);
            if(ready){editButton.Label.text=Copy("edit");exportButton.Label.text=Copy("export");repositionButton.Label.text=Copy("moveCoach");return;}
            panel.progress.text="FORMA / "+((int)flow.Step+1)+" / 5";
            for(int i=0;i<panel.choices.Length;i++)panel.Set(panel.choices[i],"",null,false);
            panel.notice.text=error;
            panel.Set(panel.place,Copy("place"),()=>{panel.PlaceInFront(view);flow.Interrupt();Refresh();});
            panel.Set(panel.coachPosition,Copy("moveCoach"),MoveCoach,catalog!=null);
            panel.Set(panel.exit,Copy(studio.ExitAwaitingConfirmation?"confirmExit":"exit"),()=>{studio.RequestExit();Refresh();});
            panel.Set(panel.back,Copy("back"),()=>{flow.Back();error="";resetArmed=false;Refresh();},flow.Step!=OnboardingStep.Welcome && !loadBlocked);
            panel.Set(panel.skip,Copy("skip"),()=>{flow.SkipOptionalProfile();error="";Refresh();},flow.Step==OnboardingStep.Profile && !loadBlocked);
            panel.Set(panel.next,Copy(flow.Step==OnboardingStep.Review?"finish":"next"),Next,!loadBlocked && catalog!=null);
            var p=flow.Draft;
            if(catalog==null){panel.title.text=Copy("welcome");panel.body.text=error;return;}
            if(loadBlocked)
            {
                panel.title.text=Copy("welcome");panel.body.text=Copy("loadError");
                Option(0,Copy("retry"),()=>{resetArmed=false;Load();});Option(1,Copy(resetArmed?"confirmReset":"reset"),Reset);return;
            }
            switch(flow.Step)
            {
                case OnboardingStep.Welcome:
                    panel.title.text=Copy("welcome");panel.body.text=Copy("welcomeBody");
                    Option(0,Copy("posture")+": "+SetupCopy.Value(p.posture,Language),()=>Edit(v=>v.posture=v.posture==Posture.Seated?Posture.Standing:Posture.Seated));
                    Option(1,Copy("audio")+": "+Copy(p.muted?"off":"on"),()=>Edit(v=>v.muted=!v.muted));
                    Option(2,Copy("volume")+": "+Math.Round(p.audioVolume*100)+"%",()=>Edit(v=>v.audioVolume=v.audioVolume>=1?0:Math.Min(1,v.audioVolume+0.25f)));
                    Option(3,Copy("motion")+": "+Copy(p.reducedMotion?"on":"off"),()=>Edit(v=>v.reducedMotion=!v.reducedMotion));
                    Option(4,Copy("language")+": "+(p.language=="en"?"English":"Türkçe"),()=>Edit(v=>v.language=v.language=="en"?"tr":"en"));break;
                case OnboardingStep.Coach:
                    panel.title.text=Copy("coach");panel.body.text=Copy("coachBody");
                    Option(0,Copy("appearance")+": "+catalog.Get(p.coachId).displayName,()=>Edit(v=>v.coachId=v.coachId=="sol"?"mira":v.coachId=="mira"?"atlas":"sol"));
                    Option(1,Copy("personality")+": "+SetupCopy.Value(p.personality,Language),()=>Edit(v=>v.personality=(PersonalityStyle)(((int)v.personality+1)%3)));
                    Option(2,Copy("language")+": "+(p.language=="en"?"English":"Türkçe"),()=>Edit(v=>v.language=v.language=="en"?"tr":"en"));
                    var definition=catalog.Get(p.coachId);definition.personality=p.personality;
                    panel.notice.text=new OfflineCoachDialogue().Welcome(definition,Language);break;
                case OnboardingStep.Profile:
                    panel.title.text=Copy("profile");panel.body.text=Copy("profileBody");
                    Option(0,Copy("goal")+": "+SetupCopy.Value(p.goal,Language),()=>Edit(v=>v.goal=(FitnessGoal)(((int)v.goal+1)%4)));
                    Option(1,Copy("experience")+": "+SetupCopy.Value(p.experience,Language),()=>Edit(v=>v.experience=(ExperienceLevel)(((int)v.experience+1)%4)));
                    Option(2,Copy("minutes")+": "+(p.preferredMinutes==0?Copy("skipped"):p.preferredMinutes.ToString()),()=>Edit(v=>v.preferredMinutes=v.preferredMinutes==0?3:v.preferredMinutes==3?5:v.preferredMinutes==5?10:0));
                    Option(3,Copy("equipment")+": "+Copy(p.hasEquipment?"on":"off"),()=>Edit(v=>v.hasEquipment=!v.hasEquipment));
                    Option(4,Copy("type")+": "+SetupCopy.Value(p.workoutPreference,Language),()=>Edit(v=>v.workoutPreference=(WorkoutPreference)(((int)v.workoutPreference+1)%4)));
                    Option(5,Copy("limits")+": "+SetupCopy.Value(p.movementLimits,Language),()=>Edit(v=>v.movementLimits=(MovementLimits)(((int)v.movementLimits+1)%4)));break;
                case OnboardingStep.Safety:
                    panel.title.text=Copy("safety");panel.body.text=Copy("safetyBody");
                    Option(0,(flow.SafetyAcknowledged?"[x] ":"[ ] ")+Copy("ack"),()=>{flow.ConfirmSafety(!flow.SafetyAcknowledged,flow.SpaceConfirmed);Refresh();});
                    Option(1,(flow.SpaceConfirmed?"[x] ":"[ ] ")+Copy("space"),()=>{flow.ConfirmSafety(flow.SafetyAcknowledged,!flow.SpaceConfirmed);Refresh();});break;
                case OnboardingStep.Review:
                    panel.title.text=Copy("review");panel.body.text=catalog.Get(p.coachId).displayName+" / "+SetupCopy.Value(p.personality,Language)+"\n"+
                        SetupCopy.Value(p.posture,Language)+" / "+Copy("voice")+"\n"+Copy(ExerciseCatalog.FirstWorkout(p)==null?"noPlan":"reviewNote");
                    Option(0,Copy(resetArmed?"confirmReset":"reset"),Reset);break;
            }
        }
        void Option(int index,string label,Action action)=>panel.Set(panel.choices[index],label,action);
        void ExportProfile()
        {
            if(!studio.CanEditPreferences)return;
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"forma-exports");Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,"setup-profile.json"),repository.Export());
                studio.ShowProductNotice("Setup profile exported locally to forma-exports/setup-profile.json (ADB access). No data was uploaded.");
            }
            catch(Exception e) when(StorageFailure(e)){studio.ShowProductNotice("Setup profile export failed. Retry after checking local storage.");}
        }
        void OnApplicationFocus(bool focused){if(!focused)SuspendSetup();}
        void OnApplicationPause(bool paused){if(paused)SuspendSetup();}
        void OnDestroy(){if(panel!=null)Destroy(panel.gameObject);}
        static bool StorageFailure(Exception e)=>e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException || e is ArgumentException;
    }
}
