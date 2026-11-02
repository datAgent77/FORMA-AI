"""Repository invariants only; this is not a C# compile or Unity build."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[1]
for path in r.rglob('*.asmdef'):
    json.loads(path.read_text())
json.loads((r/'config/packages.required.json').read_text())
for p in (r/'Unity/Assets/Forma/Domain').glob('*.cs'):
    text=p.read_text()
    assert 'UnityEngine' not in text and 'OVR' not in text and 'System.Net' not in text,p
for p in ['README.md','AGENTS.md','docs/ARCHITECTURE.md','docs/SETUP.md','docs/VALIDATION.md','THIRD_PARTY_NOTICES.md']:
    assert (r/p).is_file(),p
print('Source invariants passed (not compilation).')
