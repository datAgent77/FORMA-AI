using UnityEngine;
namespace Forma.Runtime
{
    // Floor comes from the configured floor-level rig plus an explicit manual offset, never eye-height inference.
    public sealed class ManualStudioPlacement:MonoBehaviour
    {
        public Camera view;
        public Transform floorOrigin,coachAnchor,controlPanel;
        public LineRenderer exerciseBoundary;
        public float Distance=2.4f;
        public float FloorOffset { get; private set; }
        public bool HasPlacement { get; private set; }
        public bool AdjustFloor(float delta)
        {
            FloorOffset=Mathf.Clamp(FloorOffset+delta,-.25f,.25f);
            return Place();
        }
        public bool Place()
        {
            HasPlacement=false;
            if(view==null || floorOrigin==null || coachAnchor==null || controlPanel==null || exerciseBoundary==null)return false;
            var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up);
            if(forward.sqrMagnitude<.01f)return false; // Ask the user to look ahead; do not pick an arbitrary direction.
            forward.Normalize();float floorY=floorOrigin.position.y+FloorOffset;
            Vector3 centre=view.transform.position;centre.y=floorY;
            coachAnchor.SetPositionAndRotation(centre+forward*Mathf.Clamp(Distance,2f,3f),Quaternion.LookRotation(-forward,Vector3.up));
            controlPanel.SetPositionAndRotation(view.transform.position+forward*2f+Vector3.up*.2f,Quaternion.LookRotation(forward,Vector3.up));
            exerciseBoundary.useWorldSpace=true;exerciseBoundary.loop=true;exerciseBoundary.positionCount=48;
            for(int i=0;i<48;i++)
            {
                float angle=i*Mathf.PI*2/48;
                exerciseBoundary.SetPosition(i,centre+new Vector3(Mathf.Cos(angle)*.8f,.015f,Mathf.Sin(angle)*.8f));
            }
            HasPlacement=true;return true;
        }
    }
}
