using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Forma.Domain;
namespace Forma.Product
{
    public enum DailyEnergy { Unspecified, Low, Normal, High }
    public enum DailyActivity { Choose, Strength, Cardio, Mobility, Recovery }
    public enum SafetyConcern { NoneReported, Pain, Dizziness, InjuryConcern, OtherConcern }
    public enum SpaceKind { StandingInPlace, SeatedArea }
    public enum DailyStatus { Ready, ConfirmSpace, SafetyStop, NoCompatibleWorkout, InvalidInput }
    public enum DailyCoachState { Greeting, CheckIn, Recommending, Demonstrating, Coaching, Resting, Completing, Reflecting, Farewell }
    [Serializable] public sealed class DailyReadiness
    {
        public DailyEnergy energy=DailyEnergy.Normal;
        public int availableMinutes=5;
        public DailyActivity activity;
        public SafetyConcern concern;
        public bool skipped;
        public DailyReadiness Copy()=>(DailyReadiness)MemberwiseClone();
        public static bool Valid(DailyReadiness d)=>d!=null && Enum.IsDefined(typeof(DailyEnergy),d.energy) &&
            Enum.IsDefined(typeof(DailyActivity),d.activity) && Enum.IsDefined(typeof(SafetyConcern),d.concern) &&
            (d.availableMinutes==0 || d.availableMinutes==5 || d.availableMinutes==10 || d.availableMinutes==20 || d.availableMinutes==30);
    }
    [Serializable] public sealed class WorkoutSpace
    {
        public SpaceKind kind;
        public bool confirmed;
        public bool equipmentReported; // Self-reported inventory only. All current sessions require none.
        public WorkoutSpace Copy()=>(WorkoutSpace)MemberwiseClone();
    }
    public sealed class DailyWorkout
    {
        public string Id=>Plan.Id;
        public string Name { get; }
        public DailyActivity Activity { get; }
        public WorkoutPlan Plan { get; }
        public string Intensity=>"Gentle";
        public string Equipment=>"None";
        public double DurationSeconds { get; }
        public DailyWorkout(string name,DailyActivity activity,WorkoutPlan plan)
        {
            Name=name;Activity=activity;Plan=plan;
            DurationSeconds=10+plan.Exercises.Sum(e=>(e.WorkSeconds+e.RestSeconds)*e.Sets)-plan.Exercises[plan.Exercises.Count-1].RestSeconds;
        }
    }
    public sealed class DailyRecommendation
    {
        public DailyStatus Status { get; }
        public DailyWorkout Primary { get; }
        public ReadOnlyCollection<DailyWorkout> Alternatives { get; }
        public ReadOnlyCollection<string> Reasons { get; }
        public string Message { get; }
        public int BudgetMinutes { get; }
        public DailyRecommendation(DailyStatus status,string message,IEnumerable<string> reasons=null,DailyWorkout primary=null,IEnumerable<DailyWorkout> alternatives=null,int budget=0)
        { Status=status;Message=message;Reasons=new List<string>(reasons??Array.Empty<string>()).AsReadOnly();Primary=primary;Alternatives=new List<DailyWorkout>(alternatives??Array.Empty<DailyWorkout>()).AsReadOnly();BudgetMinutes=budget; }
        public DailyWorkout Find(string id)=>Primary?.Id==id?Primary:Alternatives.FirstOrDefault(x=>x.Id==id);
    }
    public static class DailyText
    {
        public static string T(string language,string english,string turkish)=>language=="tr"?turkish:english;
    }
    public static class DailyWorkoutCatalog
    {
        public static ReadOnlyCollection<DailyWorkout> Available(ProductProfile profile,WorkoutSpace space)
        {
            var effective=profile.Copy();
            if(space.kind==SpaceKind.SeatedArea)effective.posture=Posture.Seated; // Most restrictive posture wins.
            var allowed=ExerciseCatalog.FirstWorkout(effective);
            var list=new List<DailyWorkout>();if(allowed==null)return list.AsReadOnly();
            var shoulders=allowed.Exercises.FirstOrDefault(e=>e.Id=="shoulders");var march=allowed.Exercises.FirstOrDefault(e=>e.Id=="march");
            string posture=effective.posture==Posture.Seated?"seated":"standing";
            if(shoulders!=null)list.Add(new DailyWorkout(DailyText.T(profile.language,"Gentle reset","Hafif başlangıç"),DailyActivity.Mobility,new WorkoutPlan("daily-mobility-"+posture+"-v1",new[]{shoulders})));
            if(march!=null)list.Add(new DailyWorkout(DailyText.T(profile.language,"In-place steps","Yerinde adımlar"),DailyActivity.Cardio,new WorkoutPlan("daily-cardio-standing-v1",new[]{march})));
            if(march!=null && shoulders!=null)list.Add(new DailyWorkout(DailyText.T(profile.language,"Easy movement pair","Kolay hareket ikilisi"),DailyActivity.Choose,new WorkoutPlan("daily-mixed-standing-v1",new[]{march,shoulders})));
            return list.AsReadOnly();
        }
        public static string RecordedName(string planId,string language)
        {
            switch(planId)
            {
                case "daily-mobility-standing-v1":case "daily-mobility-seated-v1":return DailyText.T(language,"Gentle reset","Hafif başlangıç");
                case "daily-cardio-standing-v1":return DailyText.T(language,"In-place steps","Yerinde adımlar");
                case "daily-mixed-standing-v1":return DailyText.T(language,"Easy movement pair","Kolay hareket ikilisi");
                case "studio-gentle-v1":return DailyText.T(language,"Studio starter","Stüdyo başlangıcı");
                default:return DailyText.T(language,"recorded session","kayıtlı seans");
            }
        }
    }
    public sealed class DailyRecommendationEngine
    {
        public DailyRecommendation Recommend(ProductProfile p,DailyReadiness day,WorkoutSpace space,IEnumerable<SessionRecord> history,TrackingCapabilities tracking,DateTimeOffset now)
        {
            string l=p?.language??"en";
            string T(string en,string tr)=>DailyText.T(l,en,tr);
            const TrackingCapabilities mask=TrackingCapabilities.Head|TrackingCapabilities.Hands|TrackingCapabilities.ValidatedBody;
            if(!ProductProfile.IsValid(p) || !DailyReadiness.Valid(day) || space==null || !Enum.IsDefined(typeof(SpaceKind),space.kind) || (tracking & ~mask)!=0)
                return new DailyRecommendation(DailyStatus.InvalidInput,T("Check the supplied preferences before recommending.","Öneriden önce girilen tercihleri kontrol et."));
            if(day.concern!=SafetyConcern.NoneReported)
                return new DailyRecommendation(DailyStatus.SafetyStop,T("Do not start a workout with a reported safety concern. Stop, rest, and seek appropriate professional advice for pain, dizziness or injury concerns. This is not a diagnosis.","Bildirilen bir güvenlik endişesi varken egzersize başlama. Dur, dinlen; ağrı, baş dönmesi veya yaralanma endişesi için uygun uzman desteği al. Bu bir tanı değildir."));
            if(!space.confirmed)
                return new DailyRecommendation(DailyStatus.ConfirmSpace,T("Confirm a clear physical space first. Manual configuration cannot guarantee that an area is safe.","Önce açık fiziksel alanını onayla. Elle yapılandırma alanın güvenli olduğunu garanti etmez."));
            int budget=day.skipped || day.availableMinutes==0 ? (p.preferredMinutes>0?p.preferredMinutes:5) : day.availableMinutes;
            var effective=p.Copy();if(space.kind==SpaceKind.SeatedArea)effective.posture=Posture.Seated;
            var validator=new BeginnerWorkoutValidator();
            var available=DailyWorkoutCatalog.Available(p,space).Where(x=>x.DurationSeconds<=budget*60 && validator.IsAllowed(x.Plan,effective,tracking)).ToList();
            if(available.Count==0)return new DailyRecommendation(DailyStatus.NoCompatibleWorkout,T("No reviewed starter matches these movement/space constraints. Update preferences or rest; no substitute has been invented.","Bu hareket/alan kısıtlarına uygun incelenmiş başlangıç seansı yok. Tercihleri değiştir veya dinlen; yeni bir hareket uydurulmadı."),budget:budget);
            var facts=new RecordedHistory(history,now);
            bool frequent=facts.Last48Hours>=3 || facts.Last7Days>=4;
            bool returning=facts.Last!=null && now-facts.LastEnded>=TimeSpan.FromDays(14);
            DailyActivity wanted=day.skipped?DailyActivity.Choose:day.activity;
            if(wanted==DailyActivity.Choose)
                wanted=p.workoutPreference==WorkoutPreference.Mobility || p.goal==FitnessGoal.GentleMovement?DailyActivity.Mobility:
                    p.workoutPreference==WorkoutPreference.GentleCardio?DailyActivity.Cardio:DailyActivity.Choose;
            bool gentlePriority=day.energy==DailyEnergy.Low || frequent || returning || wanted==DailyActivity.Recovery || wanted==DailyActivity.Strength;
            DailyActivity target=gentlePriority?DailyActivity.Mobility:wanted;
            var primary=available.FirstOrDefault(x=>x.Activity==target)??available[0];
            var reasons=new List<string>{day.skipped?
                T("Check-in skipped: using a conservative starter and your saved time preference (or 5 minutes if unset).","Check-in atlandı: ihtiyatlı başlangıç ve kayıtlı süre tercihin (yoksa 5 dakika) kullanılıyor."):
                T("Your time budget is "+budget+" minutes; this starter is shorter and does not fill the budget automatically.","Zaman bütçen "+budget+" dakika; bu başlangıç daha kısadır ve bütçeyi otomatik doldurmaz.")};
            if(day.energy==DailyEnergy.Low)reasons.Add(T("You reported low energy, so a gentle mobility option is preferred when compatible.","Düşük enerji bildirdiğin için uyumluysa hafif hareketlilik tercih edildi."));
            if(day.energy==DailyEnergy.High)reasons.Add(T("High energy does not increase the approved starter's intensity or volume.","Yüksek enerji, başlangıcın yoğunluğunu veya hacmini artırmaz."));
            if(frequent)reasons.Add(T(facts.Last48Hours+" recorded completions in 48 hours / "+facts.Last7Days+" in 7 days: keeping today's default gentle. This is a participation heuristic, not physiological load.","48 saatte "+facts.Last48Hours+", 7 günde "+facts.Last7Days+" kayıtlı tamamlama: bugünün varsayılanı hafif tutuluyor. Bu katılım kuralıdır, fizyolojik yük ölçümü değildir."));
            if(returning)reasons.Add(T("Welcome back after your recorded break. A gentle restart is available without a missed-day penalty.","Kayıtlı aradan sonra hoş geldin. Kaçırılan gün cezası olmadan hafif bir başlangıç var."));
            if(wanted==DailyActivity.Strength)reasons.Add(T("A reviewed strength program is not available yet. This is an optional gentle alternative, not a strength workout.","İncelenmiş kuvvet programı henüz yok. Bu isteğe bağlı hafif bir alternatiftir, kuvvet antrenmanı değildir."));
            else if(wanted!=DailyActivity.Choose)reasons.Add(T("Activity preference: "+wanted+". Only compatible reviewed starters are offered.","Etkinlik tercihi: "+wanted+". Yalnızca uyumlu, incelenmiş başlangıçlar sunulur."));
            if(p.experience==ExperienceLevel.New)reasons.Add(T("Your saved experience is New; the reviewed beginner dose is retained.","Kayıtlı deneyimin Yeni; incelenmiş başlangıç dozu korunuyor."));
            if(space.kind==SpaceKind.SeatedArea || p.posture==Posture.Seated)reasons.Add(T("Your seated configuration excludes standing steps.","Oturarak yapılandırman ayakta adımları dışlar."));
            if(p.movementLimits!=MovementLimits.Undisclosed)reasons.Add(T("Your voluntary movement exclusions filter every option.","Gönüllü hareket kısıtların her seçeneği filtreler."));
            if(space.equipmentReported)reasons.Add(T("Equipment was reported manually; this starter still requires none.","Ekipman elle bildirildi; bu başlangıç yine de ekipman gerektirmez."));
            reasons.Add(T("No automatic form or repetition detection is required. Keep movements comfortable and stop for discomfort.","Otomatik form veya tekrar algılama gerekmez. Rahat hareket et, rahatsızlıkta dur."));
            return new DailyRecommendation(DailyStatus.Ready,T("A small start that fits today.","Bugüne uygun küçük bir başlangıç."),reasons,primary,available.Where(x=>x.Id!=primary.Id),budget);
        }
    }
}
