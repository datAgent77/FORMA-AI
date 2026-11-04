"""Tooling checks only. Temp malformed data is a fixture, not an Editor project or build."""
import importlib.util,tempfile,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('native_preflight',Path(__file__).resolve().parents[1]/'tools/native-preflight.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class PreflightTests(unittest.TestCase):
    def test_empty_checkout_never_becomes_a_project(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);before=list(root.rglob('*'));report=module.inspect(root,scene='Unity/Assets/Forma/Scenes/FoundationQuest.unity')
            self.assertFalse(report['editorProject']);self.assertFalse(report['packageLock']);self.assertFalse(report['buildInvoked']);self.assertFalse(report['hardwareValidated'])
            self.assertTrue(any('FoundationQuest.unity' in b for b in report['blockers']));self.assertEqual(before,list(root.rglob('*')))
    def test_invalid_manifest_is_reported_without_fabricating_a_lock(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);packages=root/'Unity/Packages';packages.mkdir(parents=True);(packages/'manifest.json').write_text('{not-json')
            report=module.inspect(root)
            self.assertTrue(report['configurationErrors']);self.assertTrue(any('Invalid project configuration' in b for b in report['blockers']))
            self.assertFalse((packages/'packages-lock.json').exists());self.assertFalse(report['buildInvoked'])
    def test_bad_lock_dependency_record_cannot_count_as_sdk_resolution(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);packages=root/'Unity/Packages';packages.mkdir(parents=True);(packages/'packages-lock.json').write_text('{"dependencies":{"com.meta.xr.sdk.core":1}}')
            report=module.inspect(root)
            self.assertTrue(report['configurationErrors']);self.assertEqual(report['resolvedDependencies'],{});self.assertTrue(report['blockers'])
if __name__=='__main__':unittest.main()
