using System;
using System.IO;
using Forma.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Forma.Editor
{
    // Milestone A only: placement, input, passthrough and stop controls. No avatar/workout/media stand-ins.
    public static class FoundationSceneBuilder
    {
        public const string QuestScene="Assets/Forma/Scenes/FoundationQuest.unity";
        [MenuItem("FORMA/Foundation A/Create Quest Foundation Scene")]
        public static void CreateQuest(){Create(true);}
        [MenuItem("FORMA/Foundation A/Create Labelled Editor Simulation")]
        public static void CreatePreview(){Create(false);}
        static void Create(bool quest)
        {
            string path=quest?QuestScene:"Assets/Forma/Scenes/FoundationPreview.unity";
            if(File.Exists(path))throw new InvalidOperationException("Refusing to overwrite: "+path);
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Type bridge=null;GameObject rigPrefab=null;
            if(quest)
            {
                bridge=Type.GetType("Forma.Meta.MetaStudioPlatform, Forma.Meta");
                if(bridge==null)throw new InvalidOperationException("Resolve licensed Meta SDK and FORMA_META_XR first. No scene created.");
                foreach(var guid in AssetDatabase.FindAssets("OVRCameraRig t:Prefab"))
                {var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));if(candidate!=null && candidate.name=="OVRCameraRig"){rigPrefab=candidate;break;}}
                if(rigPrefab==null)throw new InvalidOperationException("Actual SDK OVRCameraRig prefab missing. No scene created.");
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("FORMA Native Foundation A");
            var controller=root.AddComponent<FoundationController>();controller.editorSimulation=!quest;
            Camera view;Transform floorOrigin,trackingSpace=null;
            if(quest)
            {
                var rig=(GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                controller.platform=(StudioPlatform)root.AddComponent(bridge);
                var rigType=Type.GetType("OVRCameraRig, Oculus.VR",true);
                var layerType=Type.GetType("OVRPassthroughLayer, Oculus.VR",true);
                bridge.GetField("rig").SetValue(controller.platform,rig.GetComponent(rigType));
                bridge.GetField("layer").SetValue(controller.platform,rig.AddComponent(layerType));
                foreach(var camera in rig.GetComponentsInChildren<Camera>(true)){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;}
                floorOrigin=rig.transform;trackingSpace=rig.transform.Find("TrackingSpace");
                view=rig.transform.Find("TrackingSpace/CenterEyeAnchor").GetComponent<Camera>();
            }
            else
            {
                floorOrigin=new GameObject("Simulated floor origin — not detected").transform;
                view=new GameObject("Editor simulation camera").AddComponent<Camera>();view.transform.position=new Vector3(0,1.6f,0);
                view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.07f,.09f,.1f);view.gameObject.AddComponent<AudioListener>();
                controller.platform=root.AddComponent<EditorPreviewPlatform>();
            }
            view.tag="MainCamera";
            var pointer=root.AddComponent<StudioPointer>();controller.pointer=pointer;pointer.pauseReceiver=controller;pointer.view=view;pointer.trackingSpace=trackingSpace;
            if(quest)pointer.hands=(SpatialInputSource)root.AddComponent(Type.GetType("Forma.Meta.MetaHandPointer, Forma.Meta",true));
            pointer.ray=new GameObject("Selection ray").AddComponent<LineRenderer>();pointer.ray.positionCount=2;pointer.ray.startWidth=.003f;pointer.ray.endWidth=.003f;
            pointer.ray.sharedMaterial=StudioSceneBuilder.Material(new Color(.94f,.78f,.35f));
            var placement=root.AddComponent<ManualStudioPlacement>();controller.placement=placement;placement.view=view;placement.floorOrigin=floorOrigin;
            placement.coachAnchor=new GameObject("Future coach placement anchor — no avatar").transform;
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Manual placement marker";marker.transform.SetParent(placement.coachAnchor,false);
            marker.transform.localScale=new Vector3(.18f,.005f,.18f);marker.transform.localPosition=Vector3.up*.005f;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());marker.GetComponent<Renderer>().sharedMaterial=StudioSceneBuilder.Material(new Color(.94f,.78f,.35f));
            var line=new GameObject("User-confirmed exercise area — NOT obstacle detection").AddComponent<LineRenderer>();
            line.startWidth=.018f;line.endWidth=.018f;line.sharedMaterial=StudioSceneBuilder.Material(Color.white);placement.exerciseBoundary=line;
            placement.controlPanel=new GameObject("Foundation controls").transform;
            var panel=placement.controlPanel;
            controller.status=StudioSceneBuilder.Text(panel,"Foundation status",new Vector3(-.78f,.55f,0),"Waiting for XR",.022f);
            controller.floorButton=StudioSceneBuilder.Button(panel,"Zemin burada: onayla",-.42f,-.16f,.78f,false,0);
            controller.spaceButton=StudioSceneBuilder.Button(panel,"Alan/görüş: onayla",.42f,-.16f,.78f,false,0);
            controller.pauseButton=StudioSceneBuilder.Button(panel,"DURAKLAT",-.42f,-.4f,.78f,true,0);
            controller.recenterButton=StudioSceneBuilder.Button(panel,"Yeniden yerleştir",.42f,-.4f,.78f,false,0);
            controller.lowerFloorButton=StudioSceneBuilder.Button(panel,"Zemin -2 cm",-.42f,-.64f,.78f,false,0);
            controller.raiseFloorButton=StudioSceneBuilder.Button(panel,"Zemin +2 cm",.42f,-.64f,.78f,false,0);
            controller.exitButton=StudioSceneBuilder.Button(panel,"Uygulamadan çık",0,-.88f,1.62f,false,0);
            Directory.CreateDirectory("Assets/Forma/Scenes");
            if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("Editor did not save scene: "+path);
            AssetDatabase.SaveAssets();Debug.Log("Editor-created foundation scene: "+path+". Configure actual hand prefabs, validate, test and build; hardware is not verified.");
        }
    }
}
