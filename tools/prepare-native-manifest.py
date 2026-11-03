"""Merge developer-authored dependency requests into a REAL Editor-created project; never create metadata/lock files."""
import argparse,json,os
from pathlib import Path
repo=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--include-meta',action='store_true');args=p.parse_args()
if not (repo/'Unity/ProjectSettings/ProjectVersion.txt').is_file():raise SystemExit('Create the project with the real Unity Editor first; no metadata will be fabricated.')
target=repo/'Unity/Packages/manifest.json'
if not target.is_file():raise SystemExit('Unity-generated manifest missing; open the actual project first.')
if args.include_meta and os.environ.get('FORMA_META_LICENSE_REVIEWED')!='1':raise SystemExit('Read the Meta SDK license for your use; then set FORMA_META_LICENSE_REVIEWED=1 to request Meta package restoration. This script does not accept an EULA on your behalf.')
current=json.loads(target.read_text());recipe=json.loads((repo/'config/unity-manifest.native.json').read_text())
for name,version in recipe['dependencies'].items():
    if name.startswith('com.meta.') and not args.include_meta:continue
    old=current.setdefault('dependencies',{}).get(name)
    if old and old!=version:raise SystemExit(f'{name} already requests {old}; review the version difference before editing it explicitly to {version}.')
    current['dependencies'][name]=version
if args.include_meta:
    regs=current.setdefault('scopedRegistries',[])
    for r in regs:
        if any(s.startswith('com.meta.xr') for s in r.get('scopes',[])) and r.get('url')!='https://npm.developer.oculus.com':raise SystemExit('A different Meta registry is configured; review it instead of silently replacing it.')
    if not any(r.get('url')=='https://npm.developer.oculus.com' for r in regs):regs.extend(recipe['scopedRegistries'])
backup=repo/'TestResults/manifest-before-native.json';backup.parent.mkdir(exist_ok=True)
if not backup.exists():backup.write_text(target.read_text())
target.write_text(json.dumps(current,indent=2)+'\n')
print('Dependency requests merged. Reopen Unity to resolve packages and generate the actual lock; no package lock was created by this script.')
