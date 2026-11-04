using System;
using Forma.Product;
static class Program
{
    static int passed;
    static void Check(bool value,string name){if(!value)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
    static NativeFoundationSafety Ready()
    {
        var gate=new NativeFoundationSafety();gate.UpdateReadiness(true,true,true,true);
        if(!gate.ConfirmFloor() || !gate.ConfirmSpace())throw new Exception("Test setup did not reach Ready");
        return gate;
    }
    static void Main()
    {
        var gate=new NativeFoundationSafety();
        Check(gate.State==FoundationState.Unavailable,"unobserved XR is unavailable");
        Check(!gate.ConfirmFloor(),"floor cannot be confirmed without readiness");
        Check(!gate.ConfirmSpace(),"space cannot be confirmed without readiness");
        Check(!gate.Resume(),"unavailable scene cannot resume");
        gate.UpdateReadiness(true,true,true,true);
        Check(gate.State==FoundationState.FloorConfirmation,"valid signals do not invent a confirmed floor");
        Check(!gate.ConfirmSpace(),"space confirmation requires floor review");
        Check(gate.ConfirmFloor() && gate.State==FoundationState.SpaceConfirmation,"floor review leads to separate space confirmation");
        Check(gate.ConfirmSpace() && gate.State==FoundationState.Ready,"explicit floor and space confirmations reach Ready");
        gate.Pause();
        Check(gate.State==FoundationState.Paused && !gate.SpaceConfirmed,"pause invalidates space");
        Check(!gate.Resume(),"resume without renewed space is blocked");
        Check(gate.ConfirmSpace() && gate.State==FoundationState.Paused,"space review alone never auto-resumes");
        Check(gate.Resume() && gate.State==FoundationState.Ready,"explicit resume restores readiness");
        Check(!gate.Resume(),"resume is not an implicit start command");
        for(int lost=0;lost<4;lost++)
        {
            gate=Ready();gate.UpdateReadiness(lost!=0,lost!=1,lost!=2,lost!=3);
            Check(gate.State==FoundationState.Unavailable,"lost signal "+lost+" makes foundation unavailable");
            Check(!gate.SpaceConfirmed,"lost signal "+lost+" invalidates space");
            Check(!gate.ConfirmSpace(),"lost signal "+lost+" blocks confirmation");
            Check(!gate.Resume(),"lost signal "+lost+" blocks resume");
            gate.UpdateReadiness(true,true,true,true);
            Check(gate.State!=FoundationState.Ready,"signal recovery "+lost+" never auto-resumes");
            Check(gate.FloorConfirmed==(lost!=1),"only tracking loss invalidates floor calibration "+lost);
            if(!gate.FloorConfirmed)Check(gate.ConfirmFloor(),"tracking recovery requires renewed floor confirmation");
            Check(gate.ConfirmSpace() && gate.State==FoundationState.Paused,"recovered space remains paused "+lost);
            Check(gate.Resume() && gate.State==FoundationState.Ready,"recovered scene resumes only explicitly "+lost);
        }
        gate=Ready();gate.InvalidatePlacement();
        Check(!gate.FloorConfirmed && !gate.SpaceConfirmed,"reposition invalidates floor and space");
        Check(gate.State==FoundationState.FloorConfirmation,"reposition requires floor review");
        Check(!gate.Resume(),"reposition cannot resume with stale confirmations");
        Check(gate.ConfirmFloor() && gate.State==FoundationState.Paused,"reposition floor review does not resume");
        Check(gate.ConfirmSpace() && gate.Resume(),"reposition requires floor, space and explicit resume");
        gate.ConfirmFloor();Check(gate.State==FoundationState.SpaceConfirmation,"changing floor review clears prior space consent");
        Console.WriteLine(passed+" foundation safety assertions passed (.NET only; no Unity/XR/device verification).");
    }
}
