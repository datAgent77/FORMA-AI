"""Read-only native Unity inventory. Exit 2 means blocked, never a build failure/success claim."""
import argparse, json, os, shutil
from pathlib import Path

def inspect(repo, editor=None):
    repo=Path(repo).resolve(); unity=repo/'Unity'
    candidates=[]
    if editor or os.environ.get('UNITY_EDITOR'):candidates.append(Path(editor or os.environ['UNITY_EDITOR']).expanduser())
    found=shutil.which('Unity')
    if found:candidates.append(Path(found))
    for parent in (Path('/Applications/Unity/Hub/Editor'),Path.home()/'Applications/Unity/Hub/Editor'):
        if parent.exists():candidates.extend(parent.glob('*/Unity.app/Contents/MacOS/Unity'))
    installed=next((p for p in candidates if p.is_file() and os.access(p,os.X_OK)),None)
    project=unity/'ProjectSettings/ProjectVersion.txt';manifest=unity/'Packages/manifest.json';lock=unity/'Packages/packages-lock.json'
    dependencies={};resolved={}
    if manifest.is_file():dependencies=json.loads(manifest.read_text()).get('dependencies',{})
    if lock.is_file():resolved={k:v.get('version') for k,v in json.loads(lock.read_text()).get('dependencies',{}).items()}
    sdk=Path(os.environ.get('ANDROID_SDK_ROOT',str(Path.home()/'Library/Android/sdk')))
    scenes=sorted(str(p.relative_to(repo)) for p in unity.rglob('*.unity'))
    prefabs=sorted(str(p.relative_to(repo)) for p in unity.rglob('*.prefab'))
    apks=sorted(str(p.relative_to(repo)) for p in unity.rglob('*.apk'))
    blockers=[]
    if installed is None:blockers.append('Unity Editor executable not found')
    if not project.is_file():blockers.append('No Editor-created ProjectSettings/ProjectVersion.txt')
    if not manifest.is_file():blockers.append('No actual Unity Packages/manifest.json')
    if not lock.is_file():blockers.append('No Unity-resolved Packages/packages-lock.json')
    for name in ('com.meta.xr.sdk.core','com.unity.xr.openxr'):
        if name not in resolved:blockers.append('Not resolved in project: '+name)
    if 'Unity/Assets/Forma/Scenes/StudioQuest.unity' not in scenes:blockers.append('Native StudioQuest scene has not been generated/saved by Unity')
    return dict(repo=str(repo),editor=str(installed) if installed else None,editorProject=project.is_file(),
        editorVersionFile=project.read_text().strip() if project.is_file() else None,manifest=manifest.is_file(),packageLock=lock.is_file(),
        requestedDependencies=dependencies,resolvedDependencies=resolved,scenes=scenes,prefabs=prefabs,metaCount=sum(1 for _ in unity.rglob('*.meta')),
        apkFiles=apks,androidSdk=str(sdk) if sdk.is_dir() else None,adb=str(sdk/'platform-tools/adb') if (sdk/'platform-tools/adb').is_file() else shutil.which('adb'),
        androidPlatforms=sorted(p.name for p in (sdk/'platforms').glob('*')) if sdk.is_dir() else [],
        ndkVersions=sorted(p.name for p in (sdk/'ndk').glob('*')) if sdk.is_dir() else [],
        buildInvoked=False,hardwareValidated=False,blockers=blockers)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--repo',default=str(Path(__file__).resolve().parents[1]));parser.add_argument('--editor');parser.add_argument('--output');args=parser.parse_args()
    report=inspect(args.repo,args.editor);text=json.dumps(report,indent=2)
    if args.output:Path(args.output).write_text(text+'\n')
    print(text);raise SystemExit(2 if report['blockers'] else 0)
