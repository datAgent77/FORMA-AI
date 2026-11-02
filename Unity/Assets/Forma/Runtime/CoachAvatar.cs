using UnityEngine;
using Forma.Domain;
namespace Forma.Runtime
{
    public sealed class CoachAvatar : MonoBehaviour
    {
        public Transform leftLeg,rightLeg,leftArm,rightArm;
        float phase;
        public void Present(WorkoutMachine workout,float dt)
        {
            // Stylized motions are illustrative, never a measurement or form assessment.
            bool moving=workout.State==WorkoutState.Work;
            if (moving) phase+=dt*3;
            float a=moving?Mathf.Sin(phase):0;
            bool march=workout.Current.Id=="march";
            leftLeg.localRotation=Quaternion.Euler(march?a*12:0,0,0);
            rightLeg.localRotation=Quaternion.Euler(march?-a*12:0,0,0);
            leftArm.localRotation=Quaternion.Euler(march?-a*8:0,0,march?0:a*5);
            rightArm.localRotation=Quaternion.Euler(march?a*8:0,0,march?0:-a*5);
        }
    }
}
