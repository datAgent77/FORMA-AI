using UnityEngine;
using UnityEngine.XR;
namespace Forma.Runtime
{
    // Head gaze + either trigger. Large targets; pause also available on either secondary button.
    public sealed class StudioPointer : MonoBehaviour
    {
        public StudioController controller;
        public Camera view;
        bool lastTrigger, lastStop;
        public LineRenderer ray;
        void Update()
        {
            bool trigger=false, stop=false;
            foreach (XRNode hand in new[]{XRNode.LeftHand,XRNode.RightHand})
            {
                var d=InputDevices.GetDeviceAtXRNode(hand);
                if (d.TryGetFeatureValue(CommonUsages.triggerButton,out bool t)) trigger|=t;
                if (d.TryGetFeatureValue(CommonUsages.secondaryButton,out bool s)) stop|=s;
            }
            if (stop && !lastStop) controller.Pause();
            if (view!=null)
            {
                var r=new Ray(view.transform.position,view.transform.forward);
                bool hit=Physics.Raycast(r,out RaycastHit h,8);
                if (ray!=null) { ray.SetPosition(0,r.origin+r.direction*0.1f); ray.SetPosition(1,hit?h.point:r.GetPoint(3)); }
                if (trigger && !lastTrigger && hit) h.collider.GetComponent<StudioButton>()?.Invoke();
            }
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space)) controller.Pause();
            if (Input.GetMouseButtonDown(0) && view!=null)
            {
                var r=view.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(r,out RaycastHit h,8)) h.collider.GetComponent<StudioButton>()?.Invoke();
            }
#endif
            lastTrigger=trigger; lastStop=stop;
        }
    }
}
