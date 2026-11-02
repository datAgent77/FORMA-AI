using System;
using Forma.Domain;
static class Program
{
    static int passed;
    static void Check(bool v,string name) { if (!v) throw new Exception(name); passed++; Console.WriteLine("PASS "+name); }
    static WorkoutMachine New() => new WorkoutMachine(StarterWorkouts.Create(true,true));
    static void Main()
    {
        var w=New(); Check(!w.Start(false) && w.State==WorkoutState.Ready,"unsafe start denied");
        Check(w.Start(true) && !w.Start(true),"start is single-use");
        w.Tick(9.5); Check(w.State==WorkoutState.Introduction && w.Remaining==0.5,"introduction countdown");
        w.Tick(0.5); Check(w.State==WorkoutState.Work && w.Remaining==20,"intro enters work");
        w.Tick(7); w.Pause(); double remaining=w.Remaining; w.Tick(100);
        Check(w.State==WorkoutState.Paused && w.Remaining==remaining && w.ActiveSeconds==7,"pause freezes time");
        Check(!w.Resume(false) && w.Resume(true),"resume needs safety");
        w.Tick(13); Check(w.State==WorkoutState.Rest && w.CompletedSets==1,"work enters rest");
        w.Tick(15); Check(w.SetNumber==2 && w.State==WorkoutState.Work,"rest advances set");
        w.Tick(20); w.Tick(15); Check(w.ExerciseIndex==1 && w.SetNumber==1,"exercise rollover");
        w.Tick(55); Check(w.State==WorkoutState.Completed && w.ActiveSeconds==80 && w.CompletedSets==4,"complete exact workout");
        w.Tick(999); w.Abort(); Check(w.State==WorkoutState.Completed && w.ActiveSeconds==80,"terminal state is stable");
        var bulk=New(); bulk.Start(true); bulk.Tick(10000);
        Check(bulk.State==WorkoutState.Completed && bulk.ActiveSeconds==80,"overshoot stops at completion");
        var paused=New(); paused.Start(true); paused.Pause(); paused.Abort();
        Check(paused.State==WorkoutState.Aborted && !paused.Resume(true),"abort paused session");
        var zero=new WorkoutMachine(new WorkoutPlan("zero",new[]{new Exercise("a","A","Cue",1,0,2)}));
        zero.Start(true); zero.Tick(12); Check(zero.State==WorkoutState.Completed && zero.CompletedSets==2,"zero rest cannot stall");
        bool threw=false; try { New().Tick(double.NaN); } catch(ArgumentOutOfRangeException) { threw=true; }
        Check(threw,"reject NaN clock");
        foreach(double invalid in new[]{-1,double.PositiveInfinity,double.NegativeInfinity})
        { threw=false; try{New().Tick(invalid);}catch(ArgumentOutOfRangeException){threw=true;} Check(threw,"reject invalid clock "+invalid); }
        threw=false; try{StarterWorkouts.Create(false,false);}catch(ArgumentException){threw=true;} Check(threw,"reject empty selection");
        var gate=new SafetyGate{EnvironmentConfirmed=true,PassthroughReady=true,TrackingReady=true,HasFocus=true};
        Check(gate.CanExercise,"safety all ready"); gate.PassthroughReady=false; Check(!gate.CanExercise,"passthrough required");
        gate.PassthroughReady=true;gate.TrackingReady=false;Check(!gate.CanExercise,"tracking required");
        gate.TrackingReady=true;gate.HasFocus=false;Check(!gate.CanExercise,"focus required");
        gate.HasFocus=true;gate.EnvironmentConfirmed=false;Check(!gate.CanExercise,"physical confirmation required");
        var coach=new DeterministicCoach();var cw=New();
        Check(coach.Evaluate(cw).State==CoachState.Welcome,"coach ready");cw.Start(true);
        Check(coach.Evaluate(cw).State==CoachState.Welcome,"coach introduction");cw.Tick(10);
        Check(coach.Evaluate(cw).State==CoachState.Guiding,"coach guidance");cw.Tick(20);
        Check(coach.Evaluate(cw).State==CoachState.Recovering,"coach rest");cw.Pause();
        Check(coach.Evaluate(cw).State==CoachState.Paused,"coach pause");cw.Resume(true);cw.Tick(10000);
        Check(coach.Evaluate(cw).State==CoachState.Celebrating,"coach celebrates");
        var single=new WorkoutMachine(StarterWorkouts.Create(false,true));single.Start(true);single.Tick(65);
        Check(single.State==WorkoutState.Completed && single.ActiveSeconds==40,"single selected exercise");
        Console.WriteLine(passed+" domain assertions passed.");
    }
}
