using System.Collections;
using Forma.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Forma.Tests
{
    public sealed class SetupSmokeTests
    {
        [UnityTest] public IEnumerator SpatialPanelCreatesClickableTargetsAndPlacesInFront()
        {
            var cameraObject=new GameObject("Test camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,1.1f,0);
            var root=new GameObject("Test setup panel");
            try
            {
                var panel=root.AddComponent<SetupPanel>();panel.Build();panel.PlaceInFront(camera);
                yield return null;
                Assert.AreEqual(6,panel.choices.Length);Assert.AreEqual(2.4f,root.transform.position.z,0.001f);
                Assert.Greater(root.transform.position.y,camera.transform.position.y);
                bool selected=false;panel.Set(panel.choices[0],"Choose coach",()=>selected=true);panel.choices[0].Invoke();Assert.True(selected);
                Assert.NotNull(panel.choices[0].GetComponent<Collider>());
                panel.Set(panel.choices[0],"",null,false);Assert.False(panel.choices[0].gameObject.activeSelf);Assert.False(panel.choices[0].Label.gameObject.activeSelf);
            }
            finally{Object.Destroy(root);Object.Destroy(cameraObject);}
            yield return null;
        }
    }
}
