import sys
import xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
tests = list(r.iter('test-case'))
failed = [t for t in tests if t.get('result') not in ('Passed', 'Success')]
if not tests or failed:
    raise SystemExit(f'Unity tests missing, failed or skipped: {len(tests)} total / {len(failed)} nonpassing')
print(f'{len(tests)} Unity tests passed')
