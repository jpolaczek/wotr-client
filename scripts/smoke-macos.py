#!/usr/bin/env python3
"""Start the release app from the macOS installer with clean app data, then restore the previous app profile."""
from contextlib import contextmanager
import json
import os
from pathlib import Path
import plistlib
import subprocess
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]


def run(*args, **kwargs):
    return subprocess.run(args, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, **kwargs)


@contextmanager
def clean_profile(folder, bundle):
    # Unity 6 keeps macOS player data under the bundle identifier; older players used company/product.
    support = Path.home() / "Library/Application Support"
    data_folders = [support / bundle, support / "WoTR Private/War of the Ring Online"]
    domains = [bundle, "unity.WoTR Private.War of the Ring Online"]
    saved_preferences = []
    isolated = []
    try:
        for index, app_data in enumerate(data_folders):
            saved_data = folder / f"original-app-data-{index}"
            if app_data.exists():
                app_data.rename(saved_data)
            isolated.append((app_data, saved_data, folder / f"smoke-app-data-{index}"))
        for index, domain in enumerate(domains):
            path = folder / f"original-preferences-{index}.plist"
            existing = Path.home() / "Library/Preferences" / (domain + ".plist")
            if existing.exists():
                run("defaults", "export", domain, str(path))
                saved_preferences.append((domain, path))
                run("defaults", "delete", domain)
            else:
                saved_preferences.append((domain, None))
        yield
    finally:
        try:
            for domain, path in saved_preferences:
                subprocess.run(["defaults", "delete", domain], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                if path is not None:
                    run("defaults", "import", domain, str(path))
        finally:
            for app_data, saved_data, smoke_data in isolated:
                if app_data.exists():
                    app_data.rename(smoke_data)
                if saved_data.exists():
                    app_data.parent.mkdir(parents=True, exist_ok=True)
                    saved_data.rename(app_data)


def observe(process, folder):
    window = None
    deadline = time.monotonic() + 45
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError("Release player exited before the startup check.")
        output = run("swift", "-module-cache-path", str(ROOT / ".artifacts/swift-cache"),
                     str(ROOT / "tests/release/window.swift"), str(process.pid), timeout=30)
        windows = json.loads(output.stdout)
        # CoreGraphics reports logical points; a 1366-pixel Retina window is only 683 points wide.
        window = next((item for item in windows if item["bounds"].get("Width", 0) > 0
                       and item["bounds"].get("Height", 0) > 0), None)
        if window:
            break
        time.sleep(1)
    if window is None:
        raise RuntimeError("No visible release player window.")
    time.sleep(5)
    if process.poll() is not None:
        raise RuntimeError("Release player stopped during startup.")
    screenshot = folder / "login.png"
    capture = subprocess.run(["screencapture", "-x", "-l", str(window["id"]), str(screenshot)],
                             stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    return {"window": window, "screenshot_captured": capture.returncode == 0}


def installed_app(expanded):
    apps = [path for path in expanded.rglob("*.app") if path.is_dir() and ".app/" not in str(path.relative_to(expanded))]
    if len(apps) != 1:
        raise RuntimeError(f"The installer holds {len(apps)} applications, expected one.")
    return apps[0]


def main():
    os.umask(0o077)
    running = run("ps", "-axo", "comm=").stdout.decode()
    if any("/Wotr.app/Contents/MacOS/" in line or "/War of the Ring Online.app/Contents/MacOS/" in line
           for line in running.splitlines()):
        raise RuntimeError("Close existing WoTR players before the clean-profile smoke test.")
    version = json.loads((ROOT / "release/version.json").read_text())["version"]
    folder = ROOT / ".artifacts" / ("t39-smoke-" + uuid.uuid4().hex[:8])
    folder.mkdir()
    installer = ROOT / f".artifacts/releases/{version}/installers/macos-universal/WotrOnline-{version}-osx-Setup.pkg"
    run("pkgutil", "--expand-full", str(installer), str(folder / "expanded"))
    app = installed_app(folder / "expanded")
    with (app / "Contents/Info.plist").open("rb") as stream:
        info = plistlib.load(stream)
    executable = app / "Contents/MacOS" / info["CFBundleExecutable"]
    log = folder / "player.log"
    with clean_profile(folder, info["CFBundleIdentifier"]):
        with (folder / "process.log").open("wb") as output:
            process = subprocess.Popen([str(executable), "-logFile", str(log), "-screen-width", "1366",
                                        "-screen-height", "768", "-screen-fullscreen", "0"],
                                       stdout=output, stderr=subprocess.STDOUT)
            try:
                result = observe(process, folder)
            finally:
                if process.poll() is None:
                    process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
    text = log.read_text(errors="replace")
    errors = [line for line in text.splitlines() if "Exception:" in line or "Failed to load" in line]
    if errors:
        raise RuntimeError("Release startup logged errors; inspect the local player log.")
    result.update(result="PASS", version=version, clean_app_profile=True, previous_profile_restored=True,
                  login_tested=False, windows_tested=False, source_installer=str(installer))
    (folder / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print("PASS: macOS player from the installer started with clean app data; previous profile restored.")
    print("Report:", folder / "result.json")


if __name__ == "__main__":
    main()
