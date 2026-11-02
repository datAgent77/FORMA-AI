using UnityEngine;
using Forma.Runtime;
namespace Forma.Meta
{
    [DefaultExecutionOrder(-100)]
    public sealed class MetaStudioPlatform : StudioPlatform
    {
        public OVRCameraRig rig;
        public OVRPassthroughLayer layer;
        public override bool PassthroughReady => OVRManager.instance != null &&
            OVRManager.instance.isInsightPassthroughEnabled && OVRManager.IsInsightPassthroughInitialized() &&
            !OVRManager.HasInsightPassthroughInitFailed() && layer != null && layer.isActiveAndEnabled;
        public override bool TrackingReady => OVRManager.isHmdPresent && OVRManager.hasVrFocus && OVRManager.tracker != null && OVRManager.tracker.isPositionTracked;
        public override bool HasFocus => Application.isFocused && OVRManager.hasInputFocus && OVRManager.hasVrFocus;
        void Start()
        {
            if (rig==null || layer==null) { Debug.LogError("FORMA Meta rig or passthrough layer missing."); return; }
            OVRManager.instance.isInsightPassthroughEnabled=true;
            layer.overlayType=OVROverlay.OverlayType.Underlay;
            layer.textureOpacity=1;
            foreach (Camera camera in rig.GetComponentsInChildren<Camera>(true))
            { camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear; }
        }
    }
}
