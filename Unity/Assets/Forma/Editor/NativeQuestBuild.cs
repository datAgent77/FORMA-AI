using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Forma.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Forma.Editor
{
    // Only the real Editor may emit a build result. Preflight refusal is not a successful build.
    public static class NativeQuestBuild
    {
        const string Scene="Assets/Forma/Scenes/StudioQuest.unity";
        [Serializable] sealed class Evidence
        {
            public string unityVersion,utc,result,detail,apkSha256,scene;
            public string[] packages;
            public bool buildInvoked,hardwareValidated;
        }
        public static void Build(){BuildInternal(false);}
        [MenuItem("FORMA/Foundation A/Build Quest Foundation APK")]
        public static void BuildFoundation(){BuildInternal(true);}
        static void BuildInternal(bool foundation)
        {
            string scenePath=foundation?FoundationSceneBuilder.QuestScene:Scene;
            string apk=foundation?"Builds/FORMA-Foundation.apk":"Builds/FORMA-Studio.apk";
            string resultPath=foundation?"TestResults/native-foundation-build.json":"TestResults/native-build.json";
            var evidence=new Evidence{unityVersion=Application.unityVersion,utc=DateTime.UtcNow.ToString("O"),result="Blocked",scene=scenePath};
            Directory.CreateDirectory("TestResults");
            try
            {
                var packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                evidence.packages=packages.Select(p=>p.name+"@"+p.version).ToArray();
                Require(packages.Any(p=>p.name=="com.meta.xr.sdk.core"),"Meta Core is not resolved.");
                Require(packages.Any(p=>p.name=="com.unity.xr.openxr"),"OpenXR is not resolved.");
                Require(EditorUserBuildSettings.activeBuildTarget==BuildTarget.Android,"Switch active target to Android.");
                Require(File.Exists(scenePath),"Generate and save the requested native scene using the Editor: "+scenePath);
                var settingsType=Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor",true);
                var general=settingsType.GetMethod("XRGeneralSettingsForBuildTarget",BindingFlags.Public|BindingFlags.Static)
                    .Invoke(null,new object[]{BuildTargetGroup.Android});
                Require(general!=null,"Android XR settings missing.");
                var manager=general.GetType().GetProperty("Manager").GetValue(general);
                Require(manager!=null,"Android XR manager missing.");
                var loaders=(System.Collections.IEnumerable)manager.GetType().GetProperty("activeLoaders").GetValue(manager);
                Require(loaders.Cast<object>().Any(l=>l.GetType().FullName=="UnityEngine.XR.OpenXR.OpenXRLoader"),"Enable the Android OpenXR loader.");
                Require(!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
                    PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[]{GraphicsDeviceType.Vulkan}),"Use Vulkan only and run OpenXR Project Validation.");
                Require((int)PlayerSettings.Android.minSdkVersion>=32,"Set Android minimum API level 32 or higher.");
                if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new InvalidOperationException("Build cancelled; current scene preserved.");
                var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
                if(foundation)
                {
                    var control=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FoundationController>(true)).Single();
                    Require(!control.editorSimulation,"Editor simulation cannot be built as native foundation.");
                    Require(control.platform!=null && control.platform.GetType().FullName=="Forma.Meta.MetaStudioPlatform","Native Meta bridge missing.");
                    var bridge=new SerializedObject(control.platform);
                    var rig=bridge.FindProperty("rig").objectReferenceValue as Component;
                    Require(rig!=null && bridge.FindProperty("layer").objectReferenceValue!=null,"Rig/passthrough references missing.");
                    var eyes=rig.GetComponentsInChildren<Camera>(true);
                    Require(eyes.Length>0 && eyes.All(c=>c.clearFlags==CameraClearFlags.SolidColor && c.backgroundColor.a==0),"Eye cameras must clear alpha to zero.");
                    Require(control.pointer!=null && control.pointer.pauseReceiver==control && control.pointer.trackingSpace!=null,"Foundation pause/input wiring missing.");
                    Require(control.pointer.hands!=null,"Configure the SDK hand adapter.");
                    var handFields=new SerializedObject(control.pointer.hands);
                    var left=handFields.FindProperty("left").objectReferenceValue;var right=handFields.FindProperty("right").objectReferenceValue;
                    Require(left!=null && right!=null && left!=right,"Wire distinct left/right SDK hand prefab components.");
                    var placement=control.placement;
                    Require(placement!=null && placement.floorOrigin!=null && placement.coachAnchor!=null && placement.controlPanel!=null && placement.view!=null && placement.exerciseBoundary!=null,"Manual floor/area placement wiring missing.");
                    Require(control.status!=null && control.floorButton!=null && control.spaceButton!=null && control.pauseButton!=null && control.recenterButton!=null && control.lowerFloorButton!=null && control.raiseFloorButton!=null && control.exitButton!=null,"Foundation controls missing.");
                    Require(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudioController>(true)).Any(),"Foundation A must not silently include the workout milestone.");
                    Require(!scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CoachAvatar>(true)).Any(),"Foundation A must not imply avatar milestone completion.");
                }
                else
                {
                    var studio=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudioController>(true)).Single();
                    Require(studio.platform!=null && studio.platform.GetType().FullName=="Forma.Meta.MetaStudioPlatform","Native Meta bridge missing.");
                    var platform=new SerializedObject(studio.platform);
                    Require(platform.FindProperty("rig").objectReferenceValue!=null && platform.FindProperty("layer").objectReferenceValue!=null,"Rig/passthrough references missing.");
                    var pointer=studio.GetComponent<StudioPointer>();
                    Require(pointer!=null && pointer.trackingSpace!=null && pointer.hands!=null,"Tracking space / hand input adapter missing.");
                    var hands=new SerializedObject(pointer.hands);
                    Require(hands.FindProperty("left").objectReferenceValue!=null && hands.FindProperty("right").objectReferenceValue!=null,"Configure left/right OVRHand prefabs; see native launch guide.");
                    Require(studio.avatar!=null && studio.avatar.GetComponent<CoachAppearance>().floorOrigin!=null,"Coach floor origin missing.");
                    Require(studio.status!=null && studio.pauseButton!=null && studio.exitButton!=null,"Timer/pause/exit missing.");
                }
                // Feature validation and Android manifest permissions still require Editor and device review.
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
                PlayerSettings.companyName="FORMA";PlayerSettings.productName=foundation?"FORMA Foundation A":"FORMA Studio";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,foundation?"com.forma.foundation.dev":"com.forma.studio.dev");
                Directory.CreateDirectory("Builds");
                EditorUserBuildSettings.buildAppBundle=false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
                evidence.buildInvoked=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=apk,target=BuildTarget.Android,options=BuildOptions.Development});
                evidence.result=report.summary.result.ToString();
                evidence.detail="Errors: "+report.summary.totalErrors+"; warnings: "+report.summary.totalWarnings;
                Require(report.summary.result==BuildResult.Succeeded && File.Exists(apk),"Unity Android build did not succeed.");
                using(var archive=ZipFile.OpenRead(apk))
                {
                    Require(archive.GetEntry("AndroidManifest.xml")!=null,"Output is not an APK with a root Android manifest.");
                    Require(archive.GetEntry("lib/arm64-v8a/libil2cpp.so")!=null,"Output lacks the required ARM64 IL2CPP binary.");
                }
                using(var sha=SHA256.Create())using(var file=File.OpenRead(apk))
                    evidence.apkSha256=BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
            }
            catch(Exception e){evidence.detail=e.ToString();if(evidence.buildInvoked)evidence.result="Failed";throw;}
            finally{File.WriteAllText(resultPath,JsonUtility.ToJson(evidence,true));}
        }
        static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    }
}
