"""Repository invariants only; this is not a C# compile or Unity build."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[1]
for path in r.rglob('*.asmdef'):
    json.loads(path.read_text())
json.loads((r/'config/packages.required.json').read_text())
for p in [p for name in ['Domain','Product','Storage'] for p in (r/'Unity/Assets/Forma'/name).glob('*.cs')]:
    text=p.read_text()
    assert 'UnityEngine' not in text and 'OVR' not in text and 'System.Net' not in text,p
for p in ['README.md','AGENTS.md','docs/ARCHITECTURE.md','docs/SETUP.md','docs/VALIDATION.md','THIRD_PARTY_NOTICES.md']:
    assert (r/p).is_file(),p
print('Source invariants passed (not compilation).')

coaches=json.loads((r/'Unity/Assets/Forma/Resources/Forma/Coaches.json').read_text())
assert len(coaches['coaches'])==3
assert {c['id'] for c in coaches['coaches']}=={'sol','mira','atlas'}
print('Phase 2 dependency boundaries and coach resources passed.')
