using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Forma.Domain;
namespace Forma.Product
{
    public enum BodyMeasurementKind { Weight, BodyFat, MuscleMass, Waist }
    public enum MeasurementProvenance { ManualEntry, ExternalMeasurementReported, Estimate }
    [Serializable] public sealed class BodyMeasurementInput
    {
        public BodyMeasurementKind kind;
        public double value;
        public string measuredOn;
        public string source;
        public MeasurementProvenance provenance;
        public static void Validate(BodyMeasurementInput input,DateTimeOffset now)
        {
            if(input==null || !Enum.IsDefined(typeof(BodyMeasurementKind),input.kind) || !Enum.IsDefined(typeof(MeasurementProvenance),input.provenance) ||
                (double.IsNaN(input.value) || double.IsInfinity(input.value)) || input.source==null || string.IsNullOrWhiteSpace(input.source) || input.source.Length>100 || input.source.Any(char.IsControl) ||
                !DateTime.TryParseExact(input.measuredOn,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date) || date.Year<1900 || date>now.UtcDateTime.Date.AddDays(1))throw new ArgumentException("Invalid dated measurement.");
            double min=input.kind==BodyMeasurementKind.Weight?20:input.kind==BodyMeasurementKind.Waist?20:0.1;
            double max=input.kind==BodyMeasurementKind.Weight?400:input.kind==BodyMeasurementKind.BodyFat?80:input.kind==BodyMeasurementKind.MuscleMass?200:300;
            if(input.value<min || input.value>max)throw new ArgumentException("Measurement outside supported entry range.");
        }
    }
    [Serializable] public sealed class BodyMeasurement
    {
        public string measurementId,athleteId,recordedUtc;
        public bool isTestData;
        public BodyMeasurementInput entry;
        public bool Verified=>false; // No verified sensor/import integration exists.
        public string Unit=>entry.kind==BodyMeasurementKind.BodyFat?"%":entry.kind==BodyMeasurementKind.Waist?"cm":"kg";
    }
    public sealed class ProgressSession
    {
        public string SessionId { get; set; }
        public string DateUtc { get; set; }
        public string Outcome { get; set; }
        public double WorkTimerSeconds { get; set; }
        public double? ElapsedSeconds { get; set; }
        public string Source { get; set; }
        public string[] ExerciseIds { get; set; }
        public IReadOnlyList<ExerciseFeedback> Feedback { get; set; }
        public IReadOnlyList<PersonalBlock> TargetIntervals { get; set; }
    }
    public sealed class ProgressPeriod
    {
        public string StartUtc { get; set; }
        public string EndUtc { get; set; }
        public int CompletedSessions { get; set; }
        public int CompletedDays { get; set; }
        public double RecordedWorkTimerSeconds { get; set; }
        public double KnownSessionTimerSeconds { get; set; }
        public int SessionsWithoutElapsedTime { get; set; }
        public IReadOnlyList<ProgressSession> Sessions { get; set; }
    }
    public sealed class ProgressReport
    {
        public string Timezone=>"UTC";
        public ProgressPeriod Daily { get; set; }
        public ProgressPeriod Weekly { get; set; }
        public ProgressPeriod Monthly { get; set; }
        public int WeeklyTargetDays { get; set; }
        public string[] SuggestedDatesThisWeek { get; set; }
        public bool ScheduledToday { get; set; }
        public IReadOnlyList<ProgressPeriod> WeekTrend { get; set; }
        public IReadOnlyList<BodyMeasurement> Measurements { get; set; }
        public string GoalProgressKind { get; set; }
        public int? GoalProgressPercent { get; set; }
    }
    public sealed class ProgressAnalyticsService
    {
        public ProgressReport Build(AthleteProfile athlete,CoachMemoryService memory,DateTimeOffset now)
        {
            if(!AthleteProfile.Valid(athlete))throw new ArgumentException("Valid athlete required.");
            var entries=memory.Sessions.Select(s=>new ProgressSession{SessionId=s.sessionId,DateUtc=CoachMemoryService.Date(s.endedUtc??s.startedUtc).UtcDateTime.ToString("O"),Outcome=s.outcome,WorkTimerSeconds=s.timedActiveSeconds,ElapsedSeconds=s.elapsedSeconds,Source="Personal",ExerciseIds=s.blocks.Select(b=>b.exerciseId).Distinct().ToArray(),Feedback=s.feedback,TargetIntervals=s.blocks.Where(b=>b.stage=="Exercise").ToList()})
                .Concat(memory.LegacySessions.Select(s=>new ProgressSession{SessionId=s.sessionId,DateUtc=CoachMemoryService.Date(s.endedUtc??s.startedUtc).UtcDateTime.ToString("O"),Outcome=s.outcome,WorkTimerSeconds=s.activeSeconds,ElapsedSeconds=null,Source="Legacy",ExerciseIds=Array.Empty<string>(),Feedback=Array.Empty<ExerciseFeedback>(),TargetIntervals=Array.Empty<PersonalBlock>()}))
                .Where(s=>CoachMemoryService.Date(s.DateUtc)<=now && CoachMemoryService.Date(s.DateUtc)>DateTimeOffset.MinValue).OrderByDescending(s=>s.DateUtc).ToList();
            ProgressPeriod Period(DateTime start,DateTime end)
            {
                var records=entries.Where(s=>CoachMemoryService.Date(s.DateUtc)>=start && CoachMemoryService.Date(s.DateUtc)<end).ToList();
                var completed=records.Where(s=>s.Outcome=="Completed").ToList();
                return new ProgressPeriod{StartUtc=start.ToString("O"),EndUtc=end.ToString("O"),CompletedSessions=completed.Count,CompletedDays=completed.Select(s=>CoachMemoryService.Date(s.DateUtc).UtcDateTime.Date).Distinct().Count(),RecordedWorkTimerSeconds=records.Sum(s=>s.WorkTimerSeconds),KnownSessionTimerSeconds=records.Sum(s=>s.ElapsedSeconds??0),SessionsWithoutElapsedTime=records.Count(s=>!s.ElapsedSeconds.HasValue),Sessions=records};
            }
            var today=now.UtcDateTime.Date;var week=today.AddDays(-(((int)today.DayOfWeek+6)%7));var month=new DateTime(today.Year,today.Month,1,0,0,0,DateTimeKind.Utc);int target=athlete.preferences.trainingDaysPerWeek;
            var dates=Enumerable.Range(0,target).Select(i=>week.AddDays((int)Math.Floor(i*7.0/target)).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)).ToArray();
            var weekly=Period(week,week.AddDays(7));bool habits=athlete.preferences.primaryGoal==AthleteGoal.Consistency;
            return new ProgressReport{Daily=Period(today,today.AddDays(1)),Weekly=weekly,Monthly=Period(month,month.AddMonths(1)),WeeklyTargetDays=target,SuggestedDatesThisWeek=dates,ScheduledToday=dates.Contains(today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),
                WeekTrend=Enumerable.Range(0,8).Reverse().Select(i=>Period(week.AddDays(-7*i),week.AddDays(-7*i+7))).ToList(),Measurements=memory.Measurements.Where(m=>m.athleteId==athlete.athleteId).OrderBy(m=>m.entry.measuredOn).ThenBy(m=>m.recordedUtc).ToList(),GoalProgressKind=habits?"WeeklyParticipation":"NoObjectiveGoalMetric",GoalProgressPercent=habits?(int?)Math.Min(100,weekly.CompletedDays*100/target):null};
        }
    }
}
