using Forma.Runtime;
using NUnit.Framework;
using UnityEngine;
namespace Forma.Tests
{
    public sealed class NativePlacementTests
    {
        [TestCase(1.1f)] [TestCase(1.8f)]
        public void PlacementUsesFloorRatherThanAssumedEyeHeight(float eyeHeight)
        {
            var cameraObject=new GameObject("test camera");
            var floor=new GameObject("test floor origin");
            var coach=new GameObject("test coach");
            try
            {
                var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,eyeHeight+0.2f,0);
                floor.transform.position=new Vector3(0,0.2f,0);
                var appearance=coach.AddComponent<CoachAppearance>();appearance.floorOrigin=floor.transform;
                coach.transform.localScale=Vector3.one*2;
                appearance.Place(camera,0);
                Assert.That(coach.transform.position.y,Is.EqualTo(0.2f).Within(0.001f));
                Assert.That(Vector3.ProjectOnPlane(coach.transform.position-camera.transform.position,Vector3.up).magnitude,Is.EqualTo(2.6f).Within(0.001f));
                Assert.That(coach.transform.localScale,Is.EqualTo(Vector3.one));
            }
            finally { Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(floor);Object.DestroyImmediate(coach); }
        }
    }
}
