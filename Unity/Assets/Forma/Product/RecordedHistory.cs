using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Forma.Domain;
namespace Forma.Product
{
    public sealed class RecordedHistory
    {
        readonly List<SessionRecord> completed;
        public int CompletedCount=>completed.Count;
        public int Last48Hours { get; }
        public int Last7Days { get; }
        public double ActiveSeconds=>completed.Sum(x=>x.activeSeconds);
        public SessionRecord Last=>completed.Count==0?null:completed[0];
        public DateTimeOffset LastEnded=>Last==null?DateTimeOffset.MinValue:End(Last);
        public RecordedHistory(IEnumerable<SessionRecord> records,DateTimeOffset now)
        {
            completed=(records??Array.Empty<SessionRecord>()).Where(r=>ValidCompletion(r,now)).GroupBy(r=>r.sessionId).Select(g=>g.First()).OrderByDescending(End).ToList();
            Last48Hours=completed.Count(r=>End(r)>=now-TimeSpan.FromHours(48));
            Last7Days=completed.Count(r=>End(r)>=now-TimeSpan.FromDays(7));
        }
        public static bool ValidCompletion(SessionRecord r,DateTimeOffset now)
        {
            return r!=null && r.schemaVersion==1 && Guid.TryParseExact(r.sessionId,"N",out _) && r.outcome=="Completed" &&
                !string.IsNullOrWhiteSpace(r.planId) && !double.IsNaN(r.activeSeconds) && !double.IsInfinity(r.activeSeconds) && r.activeSeconds>0 && r.completedSets>0 &&
                DateTimeOffset.TryParse(r.startedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var start) &&
                DateTimeOffset.TryParse(r.endedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var end) && end>=start && end<=now;
        }
        static DateTimeOffset End(SessionRecord r)=>DateTimeOffset.Parse(r.endedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind);
        public SessionRecord PreviousComparable(SessionRecord current)
        {
            if(current==null || !DateTimeOffset.TryParse(current.startedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var start))return null;
            return completed.FirstOrDefault(r=>r.sessionId!=current.sessionId && r.planId==current.planId && End(r)<=start);
        }
        public string Greeting(ProductProfile profile)
        {
            string l=profile.language;
            string hello=profile.personality==PersonalityStyle.CalmGuide?DailyText.T(l,"Welcome. We can take this at your pace.","Hoş geldin. Kendi temponda ilerleyebiliriz."):
                profile.personality==PersonalityStyle.TechnicalCoach?DailyText.T(l,"Welcome. Choose today's conditions, then review a session.","Hoş geldin. Bugünün koşullarını seç, sonra seansı incele."):
                DailyText.T(l,"Welcome to FORMA. Let's find a small start for today.","FORMA'ya hoş geldin. Bugüne küçük bir başlangıç bulalım.");
            if(Last==null)return hello;
            return DailyText.T(l,"Welcome back. Your last recorded completion was ","Tekrar hoş geldin. Son kayıtlı tamamlaman ")+DailyWorkoutCatalog.RecordedName(Last.planId,l)+
                DailyText.T(l,": ",": ")+Last.activeSeconds.ToString("0.#",CultureInfo.InvariantCulture)+DailyText.T(l," active seconds and "," aktif saniye ve ")+Last.completedSets+
                DailyText.T(l," timed sets. What feels right today?"," zamanlı set. Bugün sana ne uygun?");
        }
        public string Comparison(SessionRecord current,string language)
        {
            if(current==null || current.outcome!="Completed")return DailyText.T(language,"A stopped session is recorded without a completion claim.","Durdurulan seans tamamlandı iddiası olmadan kaydedilir.");
            var previous=PreviousComparable(current);
            return previous==null?DailyText.T(language,"No earlier comparable completion is recorded. This session is a starting point.","Daha önce karşılaştırılabilir tamamlama yok. Bu seans bir başlangıç noktası."):
                DailyText.T(language,"Same session: previous ","Aynı seans: önceki ")+previous.activeSeconds.ToString("0.#",CultureInfo.InvariantCulture)+DailyText.T(language," active seconds; today "," aktif saniye; bugün ")+
                current.activeSeconds.ToString("0.#",CultureInfo.InvariantCulture)+DailyText.T(language,". No physiological improvement is inferred.",". Fizyolojik gelişim çıkarımı yapılmaz.");
        }
        public static DailyCoachState CoachState(WorkoutMachine workout,bool recommending,bool reflecting,bool closed)
        {
            if(closed)return DailyCoachState.Farewell;
            if(reflecting)return DailyCoachState.Reflecting;
            if(workout==null || workout.State==WorkoutState.Ready)return recommending?DailyCoachState.Recommending:DailyCoachState.CheckIn;
            switch(workout.State)
            {
                case WorkoutState.Introduction:return DailyCoachState.Demonstrating;
                case WorkoutState.Work:return DailyCoachState.Coaching;
                case WorkoutState.Rest:case WorkoutState.Paused:return DailyCoachState.Resting;
                case WorkoutState.Completed:return DailyCoachState.Completing;
                default:return DailyCoachState.Farewell;
            }
        }
    }
}
