using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Forma.Domain;
using Forma.Product;
using UnityEngine;
namespace Forma.Runtime
{
    [DefaultExecutionOrder(20)]
    public sealed class DailyExperienceController:MonoBehaviour
    {
        public StudioController studio;
        public OnboardingController onboarding;
        public Camera view;
        public GameObject workoutPresentation;
        public StudioButton todayButton;
        SetupPanel panel;
        ProductProfile profile;
        DailyReadiness readiness=new DailyReadiness();
        WorkoutSpace space=new WorkoutSpace();
        IReadOnlyList<SessionRecord> history=Array.Empty<SessionRecord>();
        DailyRecommendation recommendation;
        string selected;
        bool initialized,card,entered,historyUnavailable;
        string T(string en,string tr)=>DailyText.T(profile?.language??"en",en,tr);
        void Start()
        {
            var root=new GameObject("Today with FORMA");panel=root.AddComponent<SetupPanel>();panel.Build();panel.gameObject.SetActive(false);
            todayButton.Click=ReturnToToday;
        }
        void Update()
        {
            if(!onboarding.ReadyForWorkout){panel.gameObject.SetActive(false);initialized=false;return;}
            if(!initialized)
            {
                profile=onboarding.CurrentProfile;readiness=new DailyReadiness();space=new WorkoutSpace{kind=profile.posture==Posture.Seated?SpaceKind.SeatedArea:SpaceKind.StandingInPlace,equipmentReported=profile.hasEquipment};
                entered=false;card=false;selected=null;ReadHistory();initialized=true;panel.PlaceInFront(view);Render();
            }
            var w=studio.CurrentWorkout;
            bool working=w!=null && (w.IsRunning || w.State==WorkoutState.Paused || w.State==WorkoutState.Completed || w.State==WorkoutState.Aborted);
            bool visible=!working&&!entered;panel.gameObject.SetActive(visible);workoutPresentation.SetActive(!visible);
        }
        void ReadHistory()
        {
            try{history=new LocalStore(Path.Combine(Application.persistentDataPath,"forma-v1")).Load();historyUnavailable=false;}
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {history=Array.Empty<SessionRecord>();historyUnavailable=true;Debug.LogWarning("FORMA_DAILY_HISTORY_UNAVAILABLE");}
        }
        DailyRecommendation Evaluate()=>new DailyRecommendationEngine().Recommend(profile,readiness,space,history,TrackingCapabilities.None,DateTimeOffset.UtcNow);
        public bool CanStart(string planId)
        {if(!initialized || !entered || selected!=planId)return false;var r=Evaluate();return r.Status==DailyStatus.Ready && r.Find(planId)!=null;}
        void Recommend(bool skip)
        {
            if(skip)readiness=new DailyReadiness{skipped=true,energy=DailyEnergy.Unspecified,availableMinutes=0,concern=readiness.concern};
            recommendation=Evaluate();selected=recommendation.Primary?.Id;card=true;Render();
        }
        void ReturnToToday()
        {
            if(!studio.ResetForDaily())return;
            entered=false;card=false;space.confirmed=false;selected=null;ReadHistory();Render();
        }
        void Render()
        {
            if(panel==null || profile==null)return;
            panel.progress.text=T("Today with FORMA","Bugün, FORMA ile");
            panel.notice.text=historyUnavailable?T("Recorded history unavailable; no history-based claim is made.","Kayıtlı geçmiş okunamadı; geçmişe dayalı iddia yapılmaz."):T("Text cues / controller input. No measured room or body inference.","Metin / kontrolcü. Oda veya beden ölçümü çıkarımı yok.");
            for(int i=0;i<6;i++)panel.Set(panel.choices[i],"",null,false);
            panel.Set(panel.place,T("Place panel","Paneli yerleştir"),()=>{panel.PlaceInFront(view);space.confirmed=false;card=false;studio.InvalidateSpaceConfirmation();Render();});
            panel.Set(panel.coachPosition,T("Move coach","Koçu taşı"),()=>{onboarding.RequestCoachMove();space.confirmed=false;card=false;Render();});
            panel.Set(panel.exit,T(studio.ExitAwaitingConfirmation?"Confirm exit":"Exit",studio.ExitAwaitingConfirmation?"Çıkışı onayla":"Çıkış"),()=>{studio.RequestExit();Render();});
            panel.Set(panel.back,T("Edit today","Bugünü düzenle"),()=>{card=false;Render();},card);
            panel.Set(panel.skip,T("Quick choice","Hızlı seçim"),()=>Recommend(true),!card);
            if(!card)
            {
                panel.title.text=T("What fits today?","Bugün sana ne uygun?");panel.body.characterSize=0.022f;
                panel.body.text=Wrap(historyUnavailable?T("History is unavailable. Choose a gentle starter.","Geçmiş okunamıyor. Hafif bir başlangıç seç."):new RecordedHistory(history,DateTimeOffset.UtcNow).Greeting(profile),64);
                Choice(0,T("Energy: ","Enerji: ")+readiness.energy,()=>{readiness.energy=(DailyEnergy)(((int)readiness.energy+1)%4);readiness.skipped=false;Render();});
                Choice(1,T("Time budget: ","Süre bütçesi: ")+readiness.availableMinutes+" min",()=>{readiness.availableMinutes=readiness.availableMinutes==5?10:readiness.availableMinutes==10?20:readiness.availableMinutes==20?30:5;readiness.skipped=false;Render();});
                Choice(2,T("Activity: ","Etkinlik: ")+readiness.activity,()=>{readiness.activity=(DailyActivity)(((int)readiness.activity+1)%5);readiness.skipped=false;Render();});
                Choice(3,T("Safety concern: ","Güvenlik endişesi: ")+readiness.concern,()=>{readiness.concern=(SafetyConcern)(((int)readiness.concern+1)%5);Render();});
                Choice(4,T("Area: ","Alan: ")+space.kind,()=>{space.kind=space.kind==SpaceKind.SeatedArea?SpaceKind.StandingInPlace:SpaceKind.SeatedArea;space.confirmed=false;Render();});
                Choice(5,(space.confirmed?"[x] ":"[ ] ")+T("I checked my clear space","Açık alanımı kontrol ettim"),()=>{space.confirmed=!space.confirmed;Render();});
                panel.Set(panel.next,T("Recommend","Öner"),()=>Recommend(false));
            }
            else
            {
                var chosen=recommendation.Find(selected);bool ready=recommendation.Status==DailyStatus.Ready && chosen!=null;
                panel.title.text=ready?chosen.Name:T("Pause and check","Dur ve kontrol et");panel.body.characterSize=0.018f;
                string detail=ready?chosen.DurationSeconds+T(" seconds / Gentle / No equipment\n"," saniye / Hafif / Ekipmansız\n")+string.Join(" + ",chosen.Plan.Exercises.Select(x=>x.Name))+"\n"+
                    (readiness.activity==DailyActivity.Strength?T("Strength is unavailable. This is an optional gentle alternative.\n","Kuvvet seansı yok. Bu isteğe bağlı hafif bir alternatiftir.\n"):"")+recommendation.Reasons[0]:recommendation.Message;
                panel.body.text=Wrap(detail,74);
                var options=ready?new[]{recommendation.Primary}.Concat(recommendation.Alternatives).Where(x=>x.Id!=selected).ToArray():Array.Empty<DailyWorkout>();
                for(int i=0;i<options.Length && i<3;i++){var option=options[i];Choice(i,T("Change: ","Değiştir: ")+option.Name,()=>{selected=option.Id;Render();});}
                panel.Set(panel.next,T("Enter Studio","Stüdyoya gir"),()=>{var latest=Evaluate();var plan=latest.Find(selected);if(latest.Status==DailyStatus.Ready && plan!=null && studio.UseDailyPlan(plan.Plan)){entered=true;panel.gameObject.SetActive(false);workoutPresentation.SetActive(true);}},ready);
            }
        }
        void Choice(int i,string label,Action action)=>panel.Set(panel.choices[i],label,action);
        static string Wrap(string text,int width)
        {
            var lines=new List<string>();foreach(var source in text.Split('\n')){string line="";foreach(string word in source.Split(' ')){if(line.Length+word.Length>width){lines.Add(line);line="";}line+=(line.Length==0?"":" ")+word;}lines.Add(line);}return string.Join("\n",lines);
        }
        void OnApplicationFocus(bool focus){if(!focus)Invalidate();}
        void OnApplicationPause(bool paused){if(paused)Invalidate();}
        void Invalidate(){space.confirmed=false;if(!entered){card=false;if(initialized)Render();}}
        void OnDestroy(){if(panel!=null)Destroy(panel.gameObject);}
    }
}
