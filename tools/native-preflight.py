"""Read-only native Unity inventory. Exit 2 means blocked, never a build failure/success claim."""
import argparse, json, os, shutil
from pathlib import Path

def inspect(repo, editor=None, scene="Unity/Assets/Forma/Scenes/StudioQuest.unity"):
    repo=Path(repo).resolve(); unity=repo/'Unity'
    candidates=[]
    if editor or os.environ.get('UNITY_EDITOR'):candidates.append(Path(editor or os.environ['UNITY_EDITOR']).expanduser())
    found=shutil.which('Unity')
    if found:candidates.append(Path(found))
    for parent in (Path('/Applications/Unity/Hub/Editor'),Path.home()/'Applications/Unity/Hub/Editor'):
        if parent.exists():candidates.extend(parent.glob('*/Unity.app/Contents/MacOS/Unity'))
    installed=next((p for p in candidates if p.is_file() and os.access(p,os.X_OK)),None)
    editor_bundle=None;editor_structure_errors=[];android_player=None
    if installed is not None:
        editor_bundle=next((parent for parent in installed.parents if parent.suffix=='.app'),None)
        if editor_bundle is not None:
            if not (editor_bundle/'Contents/Info.plist').is_file():editor_structure_errors.append('Unity macOS app bundle is incomplete: Contents/Info.plist is missing')
            module_candidates=[editor_bundle.parent/'PlaybackEngines/AndroidPlayer',editor_bundle/'Contents/PlaybackEngines/AndroidPlayer']
        else:module_candidates=[installed.parent/'Data/PlaybackEngines/AndroidPlayer',installed.parent/'PlaybackEngines/AndroidPlayer']
        android_player=next((candidate for candidate in module_candidates if candidate.is_dir()),None)
    project=unity/'ProjectSettings/ProjectVersion.txt';manifest=unity/'Packages/manifest.json';lock=unity/'Packages/packages-lock.json'
    dependencies={};resolved={};configuration_errors=[]
    for path,is_lock in ((manifest,False),(lock,True)):
        if not path.is_file():continue
        try:
            entries=json.loads(path.read_text()).get('dependencies',{})
            if not isinstance(entries,dict):raise ValueError('dependencies is not an object')
            if is_lock:resolved={k:v.get('version') for k,v in entries.items()}
            else:dependencies=entries
        except (ValueError,AttributeError,TypeError,OSError) as error:configuration_errors.append(str(path.relative_to(repo))+': '+str(error))
    sdk=Path(os.environ.get('ANDROID_SDK_ROOT',str(Path.home()/'Library/Android/sdk')))
    assets=unity/'Assets'
    scenes=sorted(str(p.relative_to(repo)) for p in assets.rglob('*.unity'))
    prefabs=sorted(str(p.relative_to(repo)) for p in assets.rglob('*.prefab'))
    apks=sorted(str(p.relative_to(repo)) for p in (unity/'Builds').rglob('*.apk'))
    blockers=['Invalid project configuration: '+e for e in configuration_errors]
    if installed is None:blockers.append('Unity Editor executable not found')
    blockers.extend(editor_structure_errors)
    if installed is not None and android_player is None:blockers.append('Android Build Support module is missing from the selected Unity Editor')
    if not project.is_file():blockers.append('No Editor-created ProjectSettings/ProjectVersion.txt')
    if not manifest.is_file():blockers.append('No actual Unity Packages/manifest.json')
    if not lock.is_file():blockers.append('No Unity-resolved Packages/packages-lock.json')
    for name in ('com.meta.xr.sdk.core','com.unity.xr.openxr'):
        if name not in resolved:blockers.append('Not resolved in project: '+name)
    if scene not in scenes:blockers.append('Requested native scene has not been generated/saved by Unity: '+scene)
    return dict(repo=str(repo),editor=str(installed) if installed else None,
        editorBundle=str(editor_bundle) if editor_bundle else None,editorStructureErrors=editor_structure_errors,
        editorLaunchVerified=False,androidBuildSupport=str(android_player) if android_player else None,diskFreeBytes=shutil.disk_usage(repo).free,
        editorProject=project.is_file(),
        editorVersionFile=project.read_text().strip() if project.is_file() else None,manifest=manifest.is_file(),packageLock=lock.is_file(),
        requestedDependencies=dependencies,resolvedDependencies=resolved,scenes=scenes,prefabs=prefabs,metaCount=sum(1 for _ in assets.rglob('*.meta')),
        apkFiles=apks,androidSdk=str(sdk) if sdk.is_dir() else None,adb=str(sdk/'platform-tools/adb') if (sdk/'platform-tools/adb').is_file() else shutil.which('adb'),
        androidPlatforms=sorted(p.name for p in (sdk/'platforms').glob('*')) if sdk.is_dir() else [],
        ndkVersions=sorted(p.name for p in (sdk/'ndk').glob('*')) if sdk.is_dir() else [],
        requestedScene=scene,configurationErrors=configuration_errors,buildInvoked=False,hardwareValidated=False,blockers=blockers)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--repo',default=str(Path(__file__).resolve().parents[1]));parser.add_argument('--editor');parser.add_argument('--output');parser.add_argument('--scene',default='Unity/Assets/Forma/Scenes/StudioQuest.unity');args=parser.parse_args()
    report=inspect(args.repo,args.editor,args.scene);text=json.dumps(report,indent=2)
    if args.output:Path(args.output).write_text(text+'\n')
    print(text);raise SystemExit(2 if report['blockers'] else 0)
