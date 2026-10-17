#!/usr/bin/env python3
"""Build desktop clients; smoke macOS after other tests stop writing the app profile."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / '.artifacts/menu08'
SOURCE_PROJECT = ROOT / 'unity/Wotr.Client'
PROJECT = ROOT / '.artifacts/menu-build-client'
SMOKE_ID = 'com.wotr.menu08.smoke'
SMOKE_TITLE = 'Menu08'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def runtime_files(project=SOURCE_PROJECT):
    return {str(p.relative_to(project)): digest(p)
            for directory in ['Runtime', 'Resources']
            for p in (project / 'Assets/Wotr' / directory).rglob('*') if p.is_file()}


def build(method, log, smoke=False):
    env = dict(os.environ)
    if smoke:
        env['WOTR_MENU_SMOKE'] = '1'
    editor = env.get('WOTR_UNITY_EDITOR', '/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity')
    subprocess.run([editor, '-batchmode', '-nographics', '-projectPath', str(PROJECT), '-quit', '-executeMethod',
                    'Wotr.Editor.BuildTasks.' + method, '-logFile', str(log)],
                   cwd=ROOT, env=env, check=True)
    assert 'WOTR_BUILD_SUCCESS' in log.read_text(), 'BuildTasks did not report success.'


def profile_hashes(paths):
    files = [p for path in paths for p in ([path] if path.is_file() else path.rglob('*')) if p.is_file()]
    return {str(p): digest(p) for p in files}


def smoke(app, original_app):
    spec = importlib.util.spec_from_file_location('menu_smoke', ROOT / 'scripts/smoke-macos.py')
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    info = plistlib.loads((app / 'Contents/Info.plist').read_bytes())
    original_info = plistlib.loads((original_app / 'Contents/Info.plist').read_bytes())
    assert info['CFBundleIdentifier'] == SMOKE_ID
    library = Path.home() / 'Library'
    protected = [library / 'Application Support/WoTR Private',
                 library / 'Preferences/unity.WoTR Private.War of the Ring Online.plist',
                 library / ('Preferences/' + original_info['CFBundleIdentifier'] + '.plist')]
    folder = OUTPUT / 'smoke/run'
    folder.mkdir(parents=True, exist_ok=False)
    profile = folder / 'profile.sb'
    profile.write_text('(version 1)\n(allow default)\n' + ''.join(
        '(deny file-read* file-write* (subpath ' + json.dumps(str(p)) + '))\n' for p in protected))
    before = profile_hashes(protected)
    executable = app / 'Contents/MacOS' / info['CFBundleExecutable']
    with (folder / 'process.log').open('wb') as stream:
        process = subprocess.Popen(['sandbox-exec', '-f', str(profile), str(executable),
                                    '-logFile', str(folder / 'player.log'), '-screen-width', '1366',
                                    '-screen-height', '768', '-screen-fullscreen', '0'],
                                   cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT)
        try:
            result = helper.observe(process, folder)
            assert result['screenshot_captured'], 'Startup screenshot missing.'
            (folder / 'login.png').rename(folder / 'startup.png')
            time.sleep(12)
            assert process.poll() is None, 'Player stopped after the splash screen.'
            subprocess.run(['screencapture', '-x', '-l', str(result['window']['id']),
                            str(folder / 'login.png')], check=True)
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
    errors = [line for line in (folder / 'player.log').read_text(errors='replace').splitlines()
              if 'Exception:' in line or 'Failed to load' in line]
    assert not errors, 'Inspect smoke player.log.'
    assert before == profile_hashes(protected), 'Existing application profile changed.'
    result.update(result='PASS', application_identity=info['CFBundleIdentifier'], development=False,
                  original_profile_byte_identical=True, original_profile_files=len(before),
                  login_tested=False, windows_runtime_tested=False, intel_runtime_tested=False,
                  executable_sha256=digest(executable), same_binary_as_normal=False)
    return result


def main():
    OUTPUT.mkdir(exist_ok=False)
    source = runtime_files()
    PROJECT.mkdir(exist_ok=False)
    for directory in ['Assets', 'Packages', 'ProjectSettings']:
        shutil.copytree(SOURCE_PROJECT / directory, PROJECT / directory)
    assert source == runtime_files(PROJECT), 'Build snapshot differs from tested input.'
    build('BuildLocalMac', OUTPUT / 'mac-build.log')
    normal_app = ROOT / '.artifacts/builds/local/macos/Wotr.app'
    shutil.copytree(normal_app, OUTPUT / 'macos/Wotr.app')
    build('BuildLocalWindows', OUTPUT / 'windows-build.log')
    shutil.copytree(ROOT / '.artifacts/builds/local/windows', OUTPUT / 'windows')
    assert source == runtime_files(PROJECT) == runtime_files(), 'Runtime inputs changed during builds.'
    settings = PROJECT / 'ProjectSettings/ProjectSettings.asset'
    original = settings.read_bytes()
    hook = PROJECT / 'Assets/Wotr/Editor/MenuSmokeIdentity.cs'
    assert not hook.exists()
    hook.write_text('''using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace Wotr.Editor
{
    public sealed class MenuSmokeIdentity : IPreprocessBuildWithReport
    {
        public int callbackOrder => 10000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (System.Environment.GetEnvironmentVariable("WOTR_MENU_SMOKE") != "1") return;
            PlayerSettings.companyName = "WoTR Menu08 Smoke";
            PlayerSettings.productName = "War of the Ring Menu08 Smoke";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.wotr.menu08.smoke");
        }
    }
}
'''.replace('Menu08', SMOKE_TITLE).replace('com.wotr.menu08.smoke', SMOKE_ID))
    try:
        build('BuildLocalMac', OUTPUT / 'smoke-build.log', smoke=True)
        shutil.copytree(normal_app, OUTPUT / 'smoke/macos/Wotr.app')
    finally:
        hook.unlink(missing_ok=True)
        hook.with_suffix('.cs.meta').unlink(missing_ok=True)
        settings.write_bytes(original)
        # Keep the ordinary local build at its documented location.
        if normal_app.exists():
            shutil.rmtree(normal_app)
        shutil.copytree(OUTPUT / 'macos/Wotr.app', normal_app)
    assert source == runtime_files(PROJECT) == runtime_files(), 'Smoke runtime differs from the tested input.'
    result = smoke(OUTPUT / 'smoke/macos/Wotr.app', OUTPUT / 'macos/Wotr.app')
    result.update(runtime_matches_normal_build=True, runtime_files_checked=len(source))
    (OUTPUT / 'runtime-source.json').write_text(json.dumps(source, indent=2) + '\n')
    (OUTPUT / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS: macOS Universal / Windows x64 nonDevelopment builds and isolated macOS startup.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--task', help='Separate artifact directory and smoke identity, e.g. MENU-09.')
    args = parser.parse_args()
    if args.task:
        if not re.fullmatch(r'MENU-[0-9]{2,}', args.task):
            parser.error('--task must use MENU-NN format.')
        namespace = args.task.lower().replace('-', '')
        OUTPUT = ROOT / '.artifacts' / namespace
        PROJECT = ROOT / '.artifacts' / (namespace + '-build-client')
        SMOKE_ID = 'com.wotr.' + namespace + '.smoke'
        SMOKE_TITLE = namespace.capitalize()
    main()
