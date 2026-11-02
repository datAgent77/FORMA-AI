using UnityEngine;
using Forma.Domain;
namespace Forma.Runtime
{
    public sealed class CoachAvatar : MonoBehaviour
    {
        public Transform leftLeg,rightLeg,leftArm,rightArm;
        public bool reducedMotion;
        float phase;
        public void Present(WorkoutMachine workout,float dt)
        {
            // Stylized motions are illustrative, never a measurement or form assessment.
            bool moving=workout.State==WorkoutState.Work;
            bool idle=workout.State==WorkoutState.Ready || workout.State==WorkoutState.Introduction || workout.State==WorkoutState.Rest || workout.State==WorkoutState.Completed;
            if (moving || (idle && !reducedMotion)) phase+=dt*(moving?3:0.7f);
            float a=moving?Mathf.Sin(phase):(idle && !reducedMotion?Mathf.Sin(phase)*0.12f:0);
            bool march=workout.Current.Id=="march";
            leftLeg.localRotation=Quaternion.Euler(march?a*12:0,0,0);
            rightLeg.localRotation=Quaternion.Euler(march?-a*12:0,0,0);
            leftArm.localRotation=Quaternion.Euler(march?-a*8:0,0,march?0:a*5);
            rightArm.localRotation=Quaternion.Euler(march?a*8:0,0,march?0:-a*5);
        }
    }
}
