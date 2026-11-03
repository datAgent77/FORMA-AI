using Forma.Runtime;
using UnityEngine;
namespace Forma.Meta
{
    // Optional SDK-backed input. Assign actual SDK OVRHand prefabs in the Editor.
    // No body/form inference. Invalid, low-confidence or system-gesture hands cannot select.
    public sealed class MetaHandPointer:SpatialInputSource
    {
        public OVRHand left,right;
        public override bool TrySample(out SpatialPointerSample sample)
        {
            sample=default;
            if(!OVRManager.hasInputFocus || !OVRManager.hasVrFocus)return false;
            bool r=Valid(right),l=Valid(left);
            OVRHand hand=r?right:l?left:null;
            if(l && left.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                !(r && right.GetFingerIsPinching(OVRHand.HandFinger.Index)))hand=left;
            if(hand==null)return false;
            sample=new SpatialPointerSample(new Ray(hand.PointerPose.position,hand.PointerPose.forward),
                hand.GetFingerIsPinching(OVRHand.HandFinger.Index),hand==right?"meta-right-hand":"meta-left-hand");
            return true;
        }
        static bool Valid(OVRHand hand)=>hand!=null && hand.isActiveAndEnabled && hand.IsTracked &&
            hand.IsDataValid && hand.IsDataHighConfidence && hand.IsPointerPoseValid &&
            !hand.IsSystemGestureInProgress && hand.PointerPose!=null;
    }
}
