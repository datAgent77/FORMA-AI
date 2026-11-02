using System;
using System.IO;
using Forma.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Forma.Editor
{
    public static class StudioSceneBuilder
    {
        const string PreviewPath="Assets/Forma/Scenes/StudioPreview.unity";
        const string QuestPath="Assets/Forma/Scenes/StudioQuest.unity";
        [MenuItem("FORMA/Create Desktop Preview Scene")]
        public static void CreatePreview() { Create(false); }
        [MenuItem("FORMA/Create Quest Studio Scene")]
        public static void CreateQuest() { Create(true); }
        static void Create(bool quest)
        {
            string path=quest?QuestPath:PreviewPath;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(path)) throw new InvalidOperationException("Scene already exists. Rename it before explicitly regenerating: "+path);
            Type bridge=quest?Type.GetType("Forma.Meta.MetaStudioPlatform, Forma.Meta"):null;
            GameObject rigPrefab=null;
            if (quest)
            {
                if (bridge==null) throw new InvalidOperationException("Install Meta XR Core SDK and enable FORMA_META_XR; see docs/SETUP.md.");
                foreach(string guid in AssetDatabase.FindAssets("OVRCameraRig t:Prefab"))
                {
                    var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (candidate!=null && candidate.name=="OVRCameraRig") { rigPrefab=candidate; break; }
                }
                if (rigPrefab==null) throw new InvalidOperationException("Meta OVRCameraRig prefab not found.");
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("FORMA Studio");
            var c=root.AddComponent<StudioController>();
            Camera camera;
            if (quest)
            {
                var rig=(GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                var platform=(StudioPlatform)root.AddComponent(bridge);
                var rigType=Type.GetType("OVRCameraRig, Oculus.VR",true);
                var layerType=Type.GetType("OVRPassthroughLayer, Oculus.VR",true);
                var rigComponent=rig.GetComponent(rigType);
                bridge.GetField("rig").SetValue(platform,rigComponent);
                bridge.GetField("layer").SetValue(platform,rig.AddComponent(layerType));
                camera=rig.transform.Find("TrackingSpace/CenterEyeAnchor").GetComponent<Camera>();
                c.platform=platform;
            }
            else
            {
                var cameraObject=new GameObject("Preview Camera"); camera=cameraObject.AddComponent<Camera>();
                cameraObject.transform.position=new Vector3(0,1.6f,0);
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.025f,0.045f,0.055f);
                cameraObject.AddComponent<AudioListener>(); c.platform=root.AddComponent<EditorPreviewPlatform>();
            }
            camera.tag="MainCamera";
            var pointer=root.AddComponent<StudioPointer>(); pointer.controller=c; pointer.view=camera;
            var rayObject=new GameObject("Gaze Ray"); pointer.ray=rayObject.AddComponent<LineRenderer>();
            pointer.ray.positionCount=2; pointer.ray.startWidth=0.003f; pointer.ray.endWidth=0.003f;
            pointer.ray.material=Material(new Color(0.35f,0.95f,0.75f));
            c.summary=Text(root.transform,"Completion summary",new Vector3(-1.05f,2.48f,2.8f),"",0.028f);
            c.status=Text(root.transform,"Session",new Vector3(-1.05f,2.15f,2.8f),"FORMA STUDIO",0.032f);
            c.coach=Text(root.transform,"Coach cues",new Vector3(-1.05f,1.76f,2.8f),"Welcome",0.027f);
            Text(root.transform,"Safety",new Vector3(-1.05f,1.48f,2.8f),
                "Stay inside your Quest boundary. Clear people, pets and obstacles.\nNo equipment. Stop for discomfort, dizziness or loss of visibility.\nGaze + trigger to select. Either B/Y button pauses immediately.",0.021f);
            c.marchButton=Button(root.transform,"Gentle march",-0.55f,1.16f,0.92f);
            c.shoulderButton=Button(root.transform,"Shoulder rolls",0.55f,1.16f,0.92f);
            c.confirmButton=Button(root.transform,"Confirm clear space",0,0.96f,2.02f);
            c.startButton=Button(root.transform,"Start guided workout",-0.55f,0.76f,0.92f);
            c.pauseButton=Button(root.transform,"PAUSE / STOP MOVING",0.55f,0.76f,0.92f,true);
            c.endButton=Button(root.transform,"End session",-0.55f,0.56f,0.92f);
            c.exitButton=Button(root.transform,"Exit app",0.55f,0.56f,0.92f);
            // Side panel sits beside, never over, the coach. No room-sized floor or skybox.
            var log=new GameObject("History and manual log"); log.transform.position=new Vector3(2.35f,0,0.6f);
            log.transform.rotation=Quaternion.Euler(0,35,0);
            c.history=Text(log.transform,"History",new Vector3(-1.05f,2.1f,2.8f),"Local history",0.024f);
            c.entryExerciseButton=Button(log.transform,"Manual exercise",0,1.1f,2.02f);
            c.repsButton=Button(log.transform,"Reps +1",0,0.9f,2.02f);
            c.weightButton=Button(log.transform,"Kg +0.5",0,0.7f,2.02f);
            c.deleteButton=Button(log.transform,"Delete local data",0,0.5f,2.02f);
            c.avatar=Avatar(new Vector3(-1.85f,0,3));
            Text(root.transform,"Coach label",new Vector3(-2.3f,1.95f,3),"FORMA COACH\nIllustrative movement",0.024f);
            Directory.CreateDirectory("Assets/Forma/Scenes");
            EditorSceneManager.SaveScene(scene,path);
            AssetDatabase.SaveAssets();
            Debug.Log("Created "+path+" using the Unity Editor. Review XR settings before building.");
        }
        static TextMesh Text(Transform parent,string name,Vector3 position,string text,float size)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            var tm=go.AddComponent<TextMesh>(); tm.text=text; tm.fontSize=64; tm.characterSize=size;
            tm.anchor=TextAnchor.UpperLeft; tm.color=new Color(0.9f,0.98f,0.96f);
            return tm;
        }
        static StudioButton Button(Transform parent,string label,float x,float y,float width,bool stop=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=label; go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(x,y,2.8f); go.transform.localScale=new Vector3(width,0.16f,0.035f);
            go.GetComponent<Renderer>().sharedMaterial=Material(stop?new Color(0.45f,0.08f,0.06f):new Color(0.025f,0.12f,0.13f));
            var b=go.AddComponent<StudioButton>();
            // Parent label to panel rather than scaled collider to keep font proportions.
            b.Label=Text(parent,label+" Label",new Vector3(x-width/2+0.025f,y+0.033f,2.772f),label,width>1?0.023f:0.021f);
            return b;
        }
        static Material Material(Color color)
        {
            var shader=Shader.Find("Unlit/Color");
            if (shader==null) throw new InvalidOperationException("Use the Built-in Render Pipeline template; Unlit/Color shader missing.");
            var material=new Material(shader) {color=color};
            // Save material assets; generated scene must not depend on unsaved transient instances.
            if (!Directory.Exists("Assets/Forma/Art/Generated"))
            { Directory.CreateDirectory("Assets/Forma/Art/Generated"); AssetDatabase.Refresh(); }
            AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath("Assets/Forma/Art/Generated/Studio.mat"));
            return material;
        }
        static CoachAvatar Avatar(Vector3 origin)
        {
            var root=new GameObject("Virtual Coach"); root.transform.position=origin;
            var avatar=root.AddComponent<CoachAvatar>();
            Part(root.transform,"Torso",new Vector3(0,1.15f,0),new Vector3(0.36f,0.5f,0.22f),PrimitiveType.Capsule);
            Part(root.transform,"Head",new Vector3(0,1.72f,0),Vector3.one*0.25f,PrimitiveType.Sphere);
            avatar.leftLeg=Limb(root.transform,"Left leg",new Vector3(-0.12f,0.92f,0),0.65f);
            avatar.rightLeg=Limb(root.transform,"Right leg",new Vector3(0.12f,0.92f,0),0.65f);
            avatar.leftArm=Limb(root.transform,"Left arm",new Vector3(-0.3f,1.48f,0),0.5f);
            avatar.rightArm=Limb(root.transform,"Right arm",new Vector3(0.3f,1.48f,0),0.5f);
            return avatar;
        }
        static Transform Limb(Transform parent,string name,Vector3 p,float length)
        {
            var pivot=new GameObject(name).transform; pivot.SetParent(parent,false); pivot.localPosition=p;
            Part(pivot,name+" mesh",new Vector3(0,-length/2,0),new Vector3(0.1f,length/2,0.1f),PrimitiveType.Capsule);
            return pivot;
        }
        static void Part(Transform parent,string name,Vector3 p,Vector3 scale,PrimitiveType shape)
        {
            var go=GameObject.CreatePrimitive(shape); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=p; go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=Material(new Color(0.26f,0.78f,0.65f));
        }
        [MenuItem("FORMA/Build Quest Development APK")]
        public static void BuildQuest()
        {
            if (EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) throw new InvalidOperationException("Switch to Android and configure OpenXR first.");
            if (!File.Exists(QuestPath)) throw new InvalidOperationException("Generate and configure StudioQuest first.");
            if (Type.GetType("Forma.Meta.MetaStudioPlatform, Forma.Meta")==null) throw new InvalidOperationException("Meta integration is unavailable.");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.companyName="FORMA"; PlayerSettings.productName="FORMA Studio";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.forma.studio.dev");
            Directory.CreateDirectory("Builds");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{QuestPath},locationPathName="Builds/FORMA-Studio.apk",
                target=BuildTarget.Android,options=BuildOptions.Development });
            if (report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Quest build failed: "+report.summary.result);
        }
    }
}
