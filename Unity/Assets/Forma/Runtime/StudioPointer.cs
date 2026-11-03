using Forma.Product;
using UnityEngine;
using UnityEngine.XR;
namespace Forma.Runtime
{
    // Controller rays in tracking-space coordinates, or a validated platform hand pointer.
    public sealed class StudioPointer : MonoBehaviour
    {
        public StudioController controller;
        public Camera view;
        public Transform trackingSpace;
        public SpatialInputSource hands;
        public LineRenderer ray;
        static readonly XRNode[] Nodes={XRNode.RightHand,XRNode.LeftHand};
        readonly PointerActivationLatch activation=new PointerActivationLatch();
        bool lastStop,hadPointer;
        void Update()
        {
            bool available=TryController(out SpatialPointerSample sample,out bool stop);
            if(!available && hands!=null)available=hands.TrySample(out sample);
            if(stop && !lastStop)controller.Pause();
            lastStop=stop;
            if(hadPointer && !available)controller.Pause();
            hadPointer=available;
            bool click=activation.Update(available,sample.Pressed,sample.SourceId);
            if(ray!=null)ray.enabled=available;
            if(available)
            {
                bool hit=Physics.Raycast(sample.Ray,out RaycastHit target,8);
                if(ray!=null){ray.SetPosition(0,sample.Ray.GetPoint(0.1f));ray.SetPosition(1,hit?target.point:sample.Ray.GetPoint(3));}
                if(click && hit)target.collider.GetComponent<StudioButton>()?.Invoke();
            }
#if UNITY_EDITOR
            if(Input.GetKeyDown(KeyCode.Space))controller.Pause();
            if(Input.GetMouseButtonDown(0) && view!=null)
            {
                var mouse=view.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(mouse,out RaycastHit h,8))h.collider.GetComponent<StudioButton>()?.Invoke();
            }
#endif
        }
        bool TryController(out SpatialPointerSample sample,out bool stop)
        {
            sample=default;stop=false;bool found=false;
            foreach(var node in Nodes)
            {
                var device=InputDevices.GetDeviceAtXRNode(node);
                if(device.TryGetFeatureValue(CommonUsages.secondaryButton,out bool s))stop|=s;
                // Hand devices are handled by the high-confidence platform adapter, not controller emulation.
                if((device.characteristics & InputDeviceCharacteristics.Controller)==0 ||
                    !device.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked) || !tracked ||
                    !device.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 position) ||
                    !device.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion rotation))continue;
                device.TryGetFeatureValue(CommonUsages.triggerButton,out bool pressed);
                if(found && !pressed)continue;
                var origin=trackingSpace!=null?trackingSpace.TransformPoint(position):position;
                var direction=trackingSpace!=null?trackingSpace.TransformDirection(rotation*Vector3.forward):rotation*Vector3.forward;
                sample=new SpatialPointerSample(new Ray(origin,direction),pressed,node.ToString());found=true;
            }
            return found;
        }
        void OnDisable(){activation.Update(false,false,null);hadPointer=false;lastStop=false;if(ray!=null)ray.enabled=false;}
    }
}
