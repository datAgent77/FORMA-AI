using Forma.Runtime;
using NUnit.Framework;
using UnityEngine;
namespace Forma.Tests
{
    public sealed class FoundationPlacementTests
    {
        [TestCase(1.0f)] [TestCase(1.8f)]
        public void ManualFloorAndAreaAreIndependentOfEyeHeight(float eyeHeight)
        {
            var root=new GameObject("Foundation placement fixture");
            try
            {
                var camera=new GameObject("View").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=new Vector3(1,eyeHeight,2);
                var placement=root.AddComponent<ManualStudioPlacement>();placement.view=camera;
                placement.floorOrigin=new GameObject("Floor origin").transform;placement.floorOrigin.SetParent(root.transform);placement.floorOrigin.position=Vector3.up*.3f;
                placement.coachAnchor=new GameObject("Anchor").transform;placement.coachAnchor.SetParent(root.transform);
                placement.controlPanel=new GameObject("Controls").transform;placement.controlPanel.SetParent(root.transform);
                placement.exerciseBoundary=root.AddComponent<LineRenderer>();
                Assert.That(placement.Place(),Is.True);
                Assert.That(placement.coachAnchor.position.y,Is.EqualTo(.3f).Within(.001));
                Assert.That(placement.coachAnchor.position.z,Is.EqualTo(4.4f).Within(.001));
                Assert.That(placement.exerciseBoundary.positionCount,Is.EqualTo(48));
                Assert.That(placement.exerciseBoundary.GetPosition(0).y,Is.EqualTo(.315f).Within(.001));
                placement.AdjustFloor(10);
                Assert.That(placement.FloorOffset,Is.EqualTo(.25f));
                Assert.That(placement.coachAnchor.position.y,Is.EqualTo(.55f).Within(.001));
                camera.transform.rotation=Quaternion.Euler(90,0,0);
                Assert.That(placement.Place(),Is.False);
                Assert.That(placement.HasPlacement,Is.False,"failed placement must not permit a stale floor confirmation");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
