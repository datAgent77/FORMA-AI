using System;
using System.Collections.Generic;
using System.Linq;
using Forma.Domain;
using Forma.Product;
static class Program
{
    static int checks,cases;
    static readonly DateTimeOffset Now=new DateTimeOffset(2026,11,2,12,0,0,TimeSpan.Zero);
    static readonly DailyRecommendationEngine Engine=new DailyRecommendationEngine();
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static DailyRecommendation Recommend(ProductProfile p=null,DailyReadiness d=null,WorkoutSpace s=null,IEnumerable<SessionRecord> h=null,TrackingCapabilities t=TrackingCapabilities.None)=>
        Engine.Recommend(p??new ProductProfile(),d??new DailyReadiness(),s??new WorkoutSpace{confirmed=true},h??Array.Empty<SessionRecord>(),t,Now);
    static SessionRecord Record(string plan,double seconds=40,int sets=2,int hours=1)=>new SessionRecord{
        sessionId=Guid.NewGuid().ToString("N"),planId=plan,outcome="Completed",activeSeconds=seconds,completedSets=sets,
        startedUtc=Now.AddHours(-hours).AddMinutes(-5).ToString("O"),endedUtc=Now.AddHours(-hours).ToString("O")};
    static void Main()
    {
        var normal=Recommend();Check(normal.Status==DailyStatus.Ready,"default daily readiness");Check(normal.Primary.DurationSeconds==135,"default reviewed mixed starter");
        Check(normal.Primary.Intensity=="Gentle" && normal.Primary.Equipment=="None","no inferred equipment or intensity");
        Check(normal.Alternatives.Count==2,"small compatible alternative set");
        var low=Recommend(d:new DailyReadiness{energy=DailyEnergy.Low,activity=DailyActivity.Cardio});
        Check(low.Primary.Activity==DailyActivity.Mobility && low.Reasons.Any(x=>x.Contains("low energy")),"low energy conservatively prefers mobility");
        var strength=Recommend(d:new DailyReadiness{activity=DailyActivity.Strength,availableMinutes=20});
        Check(strength.Primary.Activity!=DailyActivity.Strength && strength.Reasons.Any(x=>x.Contains("not a strength workout")),"strength request never relabels unsupported starter");
        foreach(SafetyConcern concern in Enum.GetValues(typeof(SafetyConcern)))
            if(concern!=SafetyConcern.NoneReported)
            {
                var blocked=Recommend(d:new DailyReadiness{energy=DailyEnergy.High,activity=DailyActivity.Strength,skipped=true,concern=concern});
                Check(blocked.Status==DailyStatus.SafetyStop && blocked.Primary==null && blocked.Alternatives.Count==0,"concern blocks skipped/high energy "+concern);
            }
        Check(Recommend(s:new WorkoutSpace()).Status==DailyStatus.ConfirmSpace,"space is required");
        Check(Recommend(d:new DailyReadiness{availableMinutes=999}).Status==DailyStatus.InvalidInput,"invalid time budget");
        Check(Recommend(d:new DailyReadiness{energy=(DailyEnergy)99}).Status==DailyStatus.InvalidInput,"invalid energy");
        Check(Recommend(s:new WorkoutSpace{confirmed=true,kind=(SpaceKind)99}).Status==DailyStatus.InvalidInput,"invalid spatial constraint");
        Check(Recommend(t:(TrackingCapabilities)64).Status==DailyStatus.InvalidInput,"unknown capability mask");
        var seatedProfile=new ProductProfile{posture=Posture.Seated};
        var seated=Recommend(p:seatedProfile);Check(seated.Primary.Id.Contains("seated") && seated.Alternatives.Count==0,"saved seated preference cannot be relaxed by standing space");
        var standingProfile=new ProductProfile();var limitedSpace=Recommend(p:standingProfile,s:new WorkoutSpace{confirmed=true,kind=SpaceKind.SeatedArea});
        Check(limitedSpace.Primary.Id.Contains("seated") && standingProfile.posture==Posture.Standing,"daily space restriction cannot mutate saved posture");
        var impossible=Recommend(p:new ProductProfile{posture=Posture.Seated,movementLimits=MovementLimits.AvoidShoulders});
        Check(impossible.Status==DailyStatus.NoCompatibleWorkout && impossible.Primary==null,"conflicting limitations fail closed");
        var recent=new[]{Record("daily-cardio-standing-v1",hours:1),Record("daily-cardio-standing-v1",hours:4),Record("daily-mixed-standing-v1",hours:20)};
        var frequent=Recommend(d:new DailyReadiness{energy=DailyEnergy.Normal,activity=DailyActivity.Cardio},h:recent);
        Check(frequent.Primary.Activity==DailyActivity.Mobility && frequent.Reasons.Any(x=>x.Contains("3 recorded completions")),"actual recent participation affects default with truthful count");
        var old=Record("daily-mobility-standing-v1",hours:24*20);
        Check(Recommend(h:new[]{old}).Reasons.Any(x=>x.Contains("recorded break")),"return after recorded gap has no penalty");
        var future=Record("daily-mobility-standing-v1",hours:-1);var aborted=Record("daily-mobility-standing-v1");aborted.outcome="Aborted";
        var malformed=Record("daily-mobility-standing-v1");malformed.activeSeconds=double.NaN;
        var facts=new RecordedHistory(new[]{recent[0],recent[0],future,aborted,malformed},Now);
        Check(facts.CompletedCount==1 && facts.Last48Hours==1,"deduplicate and exclude invalid/future/noncompleted records");
        string first=new RecordedHistory(Array.Empty<SessionRecord>(),Now).Greeting(new ProductProfile());
        Check(!first.Contains("last") && !first.Contains("15-minute"),"first greeting invents no history");
        string returned=facts.Greeting(new ProductProfile());Check(returned.Contains("40 active seconds") && returned.Contains("2 timed sets"),"return greeting uses actual active seconds and sets");
        string unknown=new RecordedHistory(new[]{Record("unknown-version")},Now).Greeting(new ProductProfile());Check(unknown.Contains("recorded session") && !unknown.Contains("strength"),"unknown historical plan is not invented");
        var previous=Record("daily-cardio-standing-v1",hours:2);var current=Record("daily-cardio-standing-v1",seconds:40,hours:1);
        var comparisons=new RecordedHistory(new[]{current,previous,Record("daily-mixed-standing-v1",hours:3)},Now);
        Check(comparisons.PreviousComparable(current)?.sessionId==previous.sessionId,"compare exact same versioned plan only");
        Check(comparisons.Comparison(current,"en").Contains("previous 40 active seconds; today 40"),"equal sessions do not fabricate personal best");
        var empty=new RecordedHistory(Array.Empty<SessionRecord>(),Now);Check(empty.Comparison(current,"en").Contains("No earlier comparable"),"insufficient history is explicit");
        var skip=Recommend(p:new ProductProfile{preferredMinutes=3},d:new DailyReadiness{skipped=true,availableMinutes=0,energy=DailyEnergy.Unspecified});
        Check(skip.BudgetMinutes==3 && skip.Primary.DurationSeconds<=180,"skipped check-in uses supported saved budget without extending dose");
        var machine=new WorkoutMachine(normal.Primary.Plan);Check(RecordedHistory.CoachState(machine,true,false,false)==DailyCoachState.Recommending,"coach recommends before movement");
        machine.Start(true);Check(RecordedHistory.CoachState(machine,false,false,false)==DailyCoachState.Demonstrating,"coach demonstrates introduction");machine.Tick(10);
        Check(RecordedHistory.CoachState(machine,false,false,false)==DailyCoachState.Coaching,"coach maps active state");machine.Tick(20);
        Check(RecordedHistory.CoachState(machine,false,false,false)==DailyCoachState.Resting,"coach maps rest state");machine.Tick(999);
        Check(RecordedHistory.CoachState(machine,false,false,false)==DailyCoachState.Completing,"coach completes from actual state");
        Check(RecordedHistory.CoachState(machine,false,true,false)==DailyCoachState.Reflecting && RecordedHistory.CoachState(machine,false,false,true)==DailyCoachState.Farewell,"reflection/farewell are explicit");
        foreach(DailyEnergy energy in new[]{DailyEnergy.Low,DailyEnergy.Normal,DailyEnergy.High})
        foreach(int minutes in new[]{5,10,20,30})
        foreach(DailyActivity activity in Enum.GetValues(typeof(DailyActivity)))
        foreach(Posture posture in Enum.GetValues(typeof(Posture)))
        foreach(MovementLimits limits in new[]{MovementLimits.Undisclosed,MovementLimits.AvoidLowerBody,MovementLimits.AvoidShoulders,MovementLimits.AvoidLowerBody|MovementLimits.AvoidShoulders})
        {
            cases++;
            var p=new ProductProfile{posture=posture,movementLimits=limits};var d=new DailyReadiness{energy=energy,availableMinutes=minutes,activity=activity};
            var result=Recommend(p,d);if(result.Primary==null){Check(result.Status==DailyStatus.NoCompatibleWorkout,"matrix valid block");continue;}
            foreach(var candidate in new[]{result.Primary}.Concat(result.Alternatives))
            {
                Check(candidate.DurationSeconds<=minutes*60 && candidate.Intensity=="Gentle" && candidate.Equipment=="None","matrix budget/dose/equipment invariant");
                Check(new BeginnerWorkoutValidator().IsAllowed(candidate.Plan,p,TrackingCapabilities.None),"matrix approved guidance and exclusion invariant");
            }
        }
        Console.WriteLine(checks+" adaptive assertions passed across "+cases+" recommendation matrix cases.");
    }
}
