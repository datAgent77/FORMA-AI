namespace Forma.Product
{
    public enum FoundationState { Unavailable, FloorConfirmation, SpaceConfirmation, Ready, Paused }
    // Readiness is a gate, not obstacle detection, calibration measurement or a workout engine.
    public sealed class NativeFoundationSafety
    {
        public bool DevicesReady { get; private set; }
        public bool FloorConfirmed { get; private set; }
        public bool SpaceConfirmed { get; private set; }
        public bool IsPaused { get; private set; }
        public FoundationState State => !DevicesReady?FoundationState.Unavailable:
            !FloorConfirmed?FoundationState.FloorConfirmation:IsPaused?FoundationState.Paused:
            !SpaceConfirmed?FoundationState.SpaceConfirmation:FoundationState.Ready;
        public void UpdateReadiness(bool passthrough,bool tracking,bool focus,bool pointer)
        {
            bool wasReady=DevicesReady;
            DevicesReady=passthrough && tracking && focus && pointer;
            if(!tracking)FloorConfirmed=false;
            if(!DevicesReady){SpaceConfirmed=false;if(wasReady)IsPaused=true;}
        }
        public bool ConfirmFloor()
        {
            if(!DevicesReady)return false;
            FloorConfirmed=true;SpaceConfirmed=false;return true;
        }
        public bool ConfirmSpace()
        {
            if(!DevicesReady || !FloorConfirmed)return false;
            SpaceConfirmed=true;return true;
        }
        public void Pause(){IsPaused=true;SpaceConfirmed=false;}
        public void InvalidatePlacement(){Pause();FloorConfirmed=false;}
        public bool Resume()
        {
            if(!IsPaused || !DevicesReady || !FloorConfirmed || !SpaceConfirmed)return false;
            IsPaused=false;return true;
        }
    }
}
