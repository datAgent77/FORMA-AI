using System;
using System.IO;
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
            public string unityVersion,utc,result,detail,apkSha256;
            public string[] packages;
            public bool buildInvoked,hardwareValidated;
        }
        public static void Build()
        {
            var evidence=new Evidence{unityVersion=Application.unityVersion,utc=DateTime.UtcNow.ToString("O"),result="Blocked"};
            Directory.CreateDirectory("TestResults");
            try
            {
                var packages=UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                evidence.packages=packages.Select(p=>p.name+"@"+p.version).ToArray();
                Require(packages.Any(p=>p.name=="com.meta.xr.sdk.core"),"Meta Core is not resolved.");
                Require(packages.Any(p=>p.name=="com.unity.xr.openxr"),"OpenXR is not resolved.");
                Require(EditorUserBuildSettings.activeBuildTarget==BuildTarget.Android,"Switch active target to Android.");
                Require(File.Exists(Scene),"Generate and save StudioQuest using the Editor.");
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
                var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
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
                // Feature validation and Android manifest permissions still require Editor and device review.
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
                PlayerSettings.companyName="FORMA";PlayerSettings.productName="FORMA Studio";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.forma.studio.dev");
                Directory.CreateDirectory("Builds");
                evidence.buildInvoked=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/FORMA-Studio.apk",target=BuildTarget.Android,options=BuildOptions.Development});
                evidence.result=report.summary.result.ToString();
                evidence.detail="Errors: "+report.summary.totalErrors+"; warnings: "+report.summary.totalWarnings;
                Require(report.summary.result==BuildResult.Succeeded && File.Exists("Builds/FORMA-Studio.apk"),"Unity Android build did not succeed.");
                using(var sha=SHA256.Create())using(var file=File.OpenRead("Builds/FORMA-Studio.apk"))
                    evidence.apkSha256=BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant();
            }
            catch(Exception e){evidence.detail=e.ToString();if(evidence.buildInvoked)evidence.result="Failed";throw;}
            finally{File.WriteAllText("TestResults/native-build.json",JsonUtility.ToJson(evidence,true));}
        }
        static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    }
}
