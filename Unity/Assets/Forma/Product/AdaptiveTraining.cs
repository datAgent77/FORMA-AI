using System;
using System.Collections.Generic;
using System.Linq;
using Forma.Domain;
namespace Forma.Product
{
    [Serializable] public sealed class PersonalRecommendation
    {
        public string recommendationId,athleteId,createdUtc;
        public int profileRevision;
        public string rule="Beginner";
        public int rulesVersion=1;
        public string status="Ready";
        public string[] reasons=Array.Empty<string>();
        public string[] evidenceSessionIds=Array.Empty<string>();
        public List<PersonalBlock> blocks=new List<PersonalBlock>();
        public double PlannedSeconds=>blocks.Count==0?0:WorkoutMachine.IntroductionSeconds+blocks.Sum(x=>x.workSeconds+x.restSeconds)-blocks[blocks.Count-1].restSeconds;
    }
    public sealed class CoachMemoryService
    {
        public IReadOnlyList<PersonalWorkoutRecord> Sessions { get; }
        public IReadOnlyList<SessionRecord> LegacySessions { get; }
        public CoachMemoryService(IEnumerable<PersonalWorkoutRecord> sessions,IEnumerable<SessionRecord> legacy)
        {Sessions=(sessions??Array.Empty<PersonalWorkoutRecord>()).OrderByDescending(x=>Date(x.startedUtc)).ToList();LegacySessions=(legacy??Array.Empty<SessionRecord>()).ToList();}
        public static DateTimeOffset Date(string value)=>DateTimeOffset.TryParse(value,out var result)?result:DateTimeOffset.MinValue;
        public int CompletedCount=>Sessions.Count(x=>x.outcome=="Completed")+LegacySessions.Count(x=>x.outcome=="Completed");
        public int CompletedThisWeek(DateTimeOffset now)
        {var start=now.UtcDateTime.Date.AddDays(-(((int)now.UtcDateTime.DayOfWeek+6)%7));return Sessions.Count(x=>x.outcome=="Completed" && Date(x.endedUtc)>=start && Date(x.endedUtc)<=now)+LegacySessions.Count(x=>x.outcome=="Completed" && Date(x.endedUtc)>=start && Date(x.endedUtc)<=now);}
        public string Greeting(AthleteProfile profile,DateTimeOffset now)
        {int n=CompletedThisWeek(now);return profile.preferences.language=="tr"?$"Merhaba {profile.preferences.displayName}. Bu hafta UTC takviminde {n} tamamlanan seansın var.":$"Welcome, {profile.preferences.displayName}. You have {n} completed sessions this UTC calendar week.";}
    }
    public sealed class AdaptiveTrainingEngine
    {
        public PersonalRecommendation Recommend(AthleteProfile athlete,int revision,CoachMemoryService memory,DateTimeOffset now)
        {
            if(!AthleteProfile.Valid(athlete))throw new ArgumentException("Valid athlete required.");
            var p=athlete.preferences;string T(string tr,string en)=>p.language=="tr"?tr:en;
            var r=new PersonalRecommendation{recommendationId=Guid.NewGuid().ToString("N"),athleteId=athlete.athleteId,createdUtc=now.ToString("O"),profileRevision=revision};var reasons=new List<string>();
            var known=memory.Sessions.Where(x=>x.athleteId==athlete.athleteId && CoachMemoryService.Date(x.startedUtc)<=now).ToList();
            var recent=known.Where(x=>x.outcome=="Completed").Take(3).ToList();
            // Unresolved discomfort in the latest recorded session stops a new recommendation; no diagnosis.
            var last=known.FirstOrDefault();
            if(last!=null && last.feedback.Any(x=>x.discomfort))
            {r.status="SafetyStop";r.rule="Discomfort";r.evidenceSessionIds=new[]{last.sessionId};r.reasons=new[]{T("Son seansında rahatsızlık bildirdin. Yeni egzersiz önermiyorum. Rahatsızlık sürüyorsa dur ve uygun uzman desteği al; bu bir tanı değildir.","You reported discomfort in your last session. No new exercise is recommended. Stop and seek appropriate professional advice if it persists; this is not a diagnosis.")};return r;}
            var selected=PersonalWorkoutCatalog.All.Where(e=>PersonalWorkoutCatalog.Allowed(e,p)).ToList();
            if(p.primaryGoal==AthleteGoal.Mobility)selected=selected.Where(e=>e.Category=="Mobility").ToList();
            if(p.primaryGoal==AthleteGoal.Endurance)selected=selected.Where(e=>e.Category=="GentleCardio" || e.Id=="calf").ToList();
            if(p.primaryGoal==AthleteGoal.Strength || p.primaryGoal==AthleteGoal.MuscleDevelopment)selected=selected.Where(e=>e.Category=="StrengthFoundation" || e.Id=="mobility").ToList();
            if(selected.Count==0){r.status="NoCompatibleWorkout";r.rule="Constraints";r.reasons=new[]{T("Seçtiğin hareket kısıtlarıyla uyumlu bir başlangıç yok. Yeni bir hareket uydurulmadı.","No starter matches your movement exclusions. No substitute was invented.")};return r;}
            double work=p.experience==ExperienceLevel.Regular && p.preferredMinutes>=10?30:20,rest=15;
            var allCompletionTimes=known.Where(x=>x.outcome=="Completed").Select(x=>CoachMemoryService.Date(x.endedUtc)).Concat(memory.LegacySessions.Where(x=>x.outcome=="Completed").Select(x=>CoachMemoryService.Date(x.endedUtc))).Where(x=>x<=now && x>DateTimeOffset.MinValue).ToList();
            bool missed=known.Count>=3 && known.Take(3).All(x=>x.outcome=="Cancelled" || x.outcome=="Skipped");
            bool returning=allCompletionTimes.Count>0 && now-allCompletionTimes.Max()>=TimeSpan.FromDays(14);

            bool difficult=known.Count>=2 && known.Take(2).All(x=>x.feedback.Any(f=>f.difficulty>=4));
            bool easy=recent.Count==3 && recent.All(x=>x.feedback.Count==x.blocks.Count && x.feedback.All(f=>f.outcome==ExerciseReport.Completed && f.difficulty.HasValue && f.difficulty<=2 && !f.discomfort)) && recent.All(x=>now-CoachMemoryService.Date(x.endedUtc)<TimeSpan.FromDays(14));
            if(missed)
            {r.rule="Returning";work=15;rest=20;r.evidenceSessionIds=known.Take(3).Select(x=>x.sessionId).ToArray();reasons.Add(T("Son üç girişimin tamamlanmadı. Daha kısa hareket aralıklarıyla küçük bir geri dönüş öneriyorum.","Your last three attempts were not completed. A manageable return uses shorter work intervals."));}
            else if(returning)
            {r.rule="Returning";work=15;rest=20;r.evidenceSessionIds=known.Where(x=>x.outcome=="Completed").Take(1).Select(x=>x.sessionId).Concat(memory.LegacySessions.Where(x=>x.outcome=="Completed").Take(1).Select(x=>x.sessionId)).ToArray();reasons.Add(T($"Son kayıtlı tamamlamanın üzerinden {(int)(now-allCompletionTimes.Max()).TotalDays} gün geçti. Kısa hareket aralıkları ve daha uzun dinlenme ile geri dönelim.",$"Your last recorded completion was {(int)(now-allCompletionTimes.Max()).TotalDays} days ago. Return with shorter work intervals and longer rests."));}
            else if(difficult)
            {r.rule="HighDifficulty";work=15;rest=20;r.evidenceSessionIds=known.Take(2).Select(x=>x.sessionId).ToArray();reasons.Add(T("Son iki seansında 4 veya 5 zorluk bildirdin. Hareket aralıklarını 15 saniyeye indirip dinlenmeyi 20 saniye yapıyorum.","You reported difficulty 4 or 5 in both of your last two sessions. Work intervals are reduced to 15 seconds with 20-second rests."));}
            else if(easy)
            {r.rule="Consistent";r.evidenceSessionIds=recent.Select(x=>x.sessionId).ToArray();reasons.Add(T("Son üç seansını tamamladın; tüm egzersizleri tamamlandı ve zorluğu 1–2 olarak bildirdin. Bir hareketin süresi en fazla 5 saniye artacak.","Your last three sessions were completed, with every exercise reported completed at difficulty 1–2. One movement may increase by at most five seconds."));}
            else reasons.Add(T("Tamamlanmış, düşük zorluklu üç seanslık kanıt henüz yok. Deneyimine uygun başlangıç dozunu koruyorum.","There are not yet three completed, low-difficulty sessions as evidence. Keeping the starter dose appropriate to your experience."));
            if(p.primaryGoal==AthleteGoal.Strength || p.primaryGoal==AthleteGoal.MuscleDevelopment)reasons.Add(T("Hedefin kayıtlı. Bu rutin ekipmansız temel hareket pratiğidir; kişiselleştirilmiş kuvvet veya kas geliştirme programı değildir.","Your goal is saved. This routine is equipment-free movement practice, not a personalized strength or muscle-building program."));
            if(p.equipment.Length>0)reasons.Add(T("Ekipman envanterin kayıtlı; bu rutin yine de ekipmansızdır.","Your equipment inventory is saved; this routine still uses no equipment."));
            if(!string.IsNullOrWhiteSpace(p.voluntaryLimitations))reasons.Add(T("Serbest notların otomatik yorumlanmıyor; seçtiğin yapılandırılmış hareket kısıtları uygulanıyor.","Free-text notes are not automatically interpreted; your structured movement exclusions are applied."));
            var warm=selected.FirstOrDefault(e=>e.Category=="Mobility")??selected[0];
            r.blocks.Add(new PersonalBlock{exerciseId=warm.Id,stage="WarmUp",workSeconds=35,restSeconds=rest});
            string progressId=selected.FirstOrDefault(x=>x.Id=="march")?.Id??selected[0].Id;
            foreach(var exercise in selected)
            {
                double dose=easy?Math.Min(work,recent.Select(x=>x.blocks.Where(b=>b.stage=="Exercise" && b.exerciseId==exercise.Id).Select(b=>b.workSeconds).DefaultIfEmpty(work).Min()).Min()):work;
                if(easy && exercise.Id==progressId)
                {var doses=recent.Select(x=>x.blocks.Where(b=>b.stage=="Exercise" && b.exerciseId==progressId).Select(b=>b.workSeconds).DefaultIfEmpty(work).Min());dose=Math.Min(35,doses.Min()+5);}
                r.blocks.Add(new PersonalBlock{exerciseId=exercise.Id,stage="Exercise",workSeconds=dose,restSeconds=rest});
            }
            r.blocks.Add(new PersonalBlock{exerciseId=warm.Id,stage="Cooldown",workSeconds=30,restSeconds=0});
            PersonalWorkoutCatalog.ValidatePlan(r.blocks,p);
            reasons.Add(T($"Tercihin {p.preferredMinutes} dakika; bu rutinin zamanlayıcı süresi {Math.Round(r.PlannedSeconds)} saniye. Geri bildirim ve duraklamalar buna eklenir.",$"Your preference is {p.preferredMinutes} minutes; this routine has {Math.Round(r.PlannedSeconds)} timer seconds. Feedback and pauses add to that time."));
            r.reasons=reasons.ToArray();return r;
        }
    }
}
