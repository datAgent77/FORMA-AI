"""Tooling checks only. Temp malformed data is a fixture, not an Editor project or build."""
import importlib.util,tempfile,unittest,subprocess,os,shutil
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
    def test_partial_mac_editor_cannot_pass_as_a_usable_install(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);binary=root/'Editor/Unity.app/Contents/MacOS/Unity';binary.parent.mkdir(parents=True);binary.write_text('fixture, not an executable Unity installation');binary.chmod(0o755)
            report=module.inspect(root,editor=str(binary))
            self.assertTrue(report['editorStructureErrors']);self.assertFalse(report['editorLaunchVerified'])
            self.assertIsNone(report['androidBuildSupport']);self.assertTrue(any('Info.plist' in b for b in report['blockers']))
            self.assertFalse(report['buildInvoked'])
    def test_bootstrap_refuses_incomplete_mac_bundle_before_invoking_editor(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);tools=root/'tools';tools.mkdir()
            script=tools/'bootstrap-unity.sh';shutil.copyfile(Path(__file__).resolve().parents[1]/'tools/bootstrap-unity.sh',script)
            binary=root/'Unity.app/Contents/MacOS/Unity';binary.parent.mkdir(parents=True);binary.write_text('#!/bin/sh\necho EDITOR_WAS_INVOKED\n');binary.chmod(0o755)
            result=subprocess.run(['bash',str(script)],env={**os.environ,'UNITY_EDITOR':str(binary)},capture_output=True,text=True)
            self.assertEqual(result.returncode,2);self.assertIn('Info.plist is missing',result.stderr)
            self.assertNotIn('EDITOR_WAS_INVOKED',result.stdout);self.assertFalse((root/'Unity').exists())
if __name__=='__main__':unittest.main()
