#!/usr/bin/env python3
"""Run the development Player presentation benchmark without accounts or a server."""
import argparse
import hashlib
import json
import math
import plistlib
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
KINDS = {
    "legacy": ("map-idle", "map-zoom", "hover", "army", "battle", "card", "tooltip",
               "hunt-allocation", "hunt-reroll", "hunt-response", "hunt-report", "mordor-refresh",
               "activity-scroll", "activity-card"),
    "in-game": ("effects", "resources-fp", "resources-sh", "sovereigns", "victory", "help",
                "dense-fp", "dense-sh"),
    "board": ("characters-fp", "characters-sh", "setup-lords-fp", "setup-council-fp", "setup-fate-fp",
              "setup-waiting-sh", "homelands-fp", "homelands-sh", "menu-battle", "menu-hunt"),
    "figure-motion": ("motion-army-fp", "motion-army-sh", "motion-character-fp", "motion-character-sh",
                      "motion-hidden-fp", "motion-hidden-sh", "motion-separation-fp", "motion-separation-sh"),
}


def validate_report(data, suite):
    """Reject incomplete evidence before evaluating its measured performance."""
    expected = {(kind, width, height, scale) for kind in KINDS[suite]
                for width, height in ((1366, 768), (1920, 1080)) for scale in (1, 1.25)}
    results = data.get("Results", [])
    actual = {(item["Kind"], item["Width"], item["Height"], item["Scale"]) for item in results}
    if data.get("Editor") is not False or data.get("Development") is not True:
        raise ValueError("A Development Player report is required.")
    if data.get("Suite", "legacy") != suite or actual != expected or len(results) != len(expected):
        raise ValueError("Incomplete, duplicated or mismatched profile workloads.")
    for item in results:
        metrics = item["Metrics"]
        if metrics.get("WarmupFrames") != 120 or metrics.get("SampleFrames") != 360:
            raise ValueError("Each workload requires 120 warmup and 360 measured frames.")
        if item.get("ReducedMotion") != (item["Scale"] == 1.25):
            raise ValueError("Missing reduced-motion coverage.")
        fps = metrics["Fps"]
        if not math.isfinite(fps) or fps < 60:
            raise ValueError("Presentation frame budget missed.")
        for key in ("FrameMilliseconds", "MainThreadMilliseconds", "GcBytesPerFrame"):
            validate_summary(metrics.get(key), key, required=key == "FrameMilliseconds")
        if metrics["FrameMilliseconds"]["Count"] != 360:
            raise ValueError("Incomplete frame timing samples.")
        for key in ("HandlerMilliseconds", "ToRenderEndMilliseconds"):
            summary = metrics.get(key)
            validate_summary(summary, key, required=item["Kind"] != "map-idle")
            if suite in ("in-game", "board", "figure-motion") and summary["Count"] != 24:
                raise ValueError("Incomplete presentation interaction samples.")
            if summary and summary["Max"] > 100:
                raise ValueError("Presentation input budget missed.")
        if suite == "figure-motion":
            validate_motion_evidence(item)


def validate_motion_evidence(item):
    evidence = item.get("MotionEvidence")
    if not evidence or evidence["AfterRevision"] != evidence["BeforeRevision"] + 1:
        raise ValueError("Motion profiling requires consecutive recorded projections.")
    hidden = item["Kind"].startswith("motion-hidden")
    translating = not item["ReducedMotion"] and not hidden and not item["Kind"].startswith("motion-separation")
    if evidence["ExpectedTranslation"] != translating or (evidence["ExpectedFigures"] == 0) != hidden:
        raise ValueError("Motion evidence does not match the workload or reduced-motion setting.")
    frames = evidence["Frames"]
    if [frame["Phase"] for frame in frames] != ["start", "middle", "finished"]:
        raise ValueError("Motion evidence requires start, middle and finished frames.")
    if any(not math.isfinite(frame[key]) for frame in frames for key in ("Elapsed", "X", "Y")):
        raise ValueError("Invalid motion coordinates or timestamps.")
    if not frames[0]["Elapsed"] < frames[1]["Elapsed"] < frames[2]["Elapsed"]:
        raise ValueError("Motion capture frames are not chronological.")
    if frames[2]["ActiveGroups"] != 0 or any((frame["ActiveGroups"] > 0) == hidden for frame in frames[:2]):
        raise ValueError("Motion capture has an incorrect animation lifetime.")
    distance = math.hypot(frames[1]["X"] - frames[0]["X"], frames[1]["Y"] - frames[0]["Y"])
    if translating and distance <= 1 or not translating and distance > .1:
        raise ValueError("Motion capture does not demonstrate the expected intermediate position.")


def validate_motion_images(data, directory):
    for item in data["Results"]:
        for phase in ("start", "middle", "finished"):
            path = directory / f"ui77-{item['Kind']}-{phase}-{item['Width']}-{item['Scale']:.2f}.png"
            if not path.is_file() or path.stat().st_size < 1000 or path.read_bytes()[:8] != b"\x89PNG\r\n\x1a\n":
                raise ValueError("Missing motion screenshot: " + str(path))


def validate_summary(summary, name, required):
    if summary is None and not required:
        return
    if not summary or summary.get("Count", 0) <= 0:
        raise ValueError("Missing timing samples: " + name)
    if any(not math.isfinite(summary[key]) or summary[key] < 0 for key in ("Mean", "P95", "P99", "Max")):
        raise ValueError("Invalid timing samples: " + name)


def validate_input(data, before, after):
    if before != after:
        raise ValueError("Profile input changed while the Player was running.")
    if data.get("InputSha256") != before:
        raise ValueError("Player report does not match the supplied profile input.")


def validate_player_log(log):
    """Timing thresholds cannot accept a Player with a recursive UI layout failure."""
    if "Layout update failed to stabilize" in log:
        raise ValueError("Player UI layout failed to stabilize; inspect its log before accepting timings.")


def default_player():
    builds = ROOT / ".artifacts/builds"
    if sys.platform == "darwin":
        app = builds / "mac/Wotr.app/Contents"
        with (app / "Info.plist").open("rb") as stream:
            executable = plistlib.load(stream)["CFBundleExecutable"]
        return app / "MacOS" / executable
    return builds / "windows/Wotr.exe"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path)
    parser.add_argument("--views", type=Path, default=ROOT / ".artifacts/ui13-profile-views.json")
    parser.add_argument("--report", type=Path, default=ROOT / ".artifacts/ui13-player-profile.json")
    parser.add_argument("--timeout", type=int, default=600)
    parser.add_argument("--suite", choices=KINDS, default="legacy")
    args = parser.parse_args()
    player = (args.player or default_player()).resolve(strict=True)
    views = args.views.resolve(strict=True)
    input_sha256 = hashlib.sha256(views.read_bytes()).hexdigest()
    report = args.report.resolve()
    report.parent.mkdir(parents=True, exist_ok=True)
    report.unlink(missing_ok=True)
    command = [str(player), "-screen-fullscreen", "0", "-screen-width", "1366", "-screen-height", "768",
               "-logFile", str(report.with_suffix(".log")), "--wotr-profile", str(views), str(report),
               "--wotr-profile-suite", args.suite]
    with subprocess.Popen(command, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) as process:
        try:
            code = process.wait(timeout=args.timeout)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            raise SystemExit("Player benchmark timed out; inspect its log.")
    if code or not report.is_file():
        raise SystemExit(f"Player benchmark failed ({code}); inspect {report.with_suffix('.log')}")
    data = json.loads(report.read_text())
    try:
        validate_player_log(report.with_suffix(".log").read_text(encoding="utf-8", errors="replace"))
        validate_input(data, input_sha256, hashlib.sha256(views.read_bytes()).hexdigest())
        validate_report(data, args.suite)
        if args.suite == "figure-motion":
            validate_motion_images(data, report.parent)
    except (KeyError, TypeError, ValueError, OSError) as error:
        raise SystemExit(str(error)) from error
    print(f"{data['Platform']}, {data['CPU']}, {data['GPU']}, Unity {data['Unity']}")
    for result in data["Results"]:
        metrics = result["Metrics"]
        feedback = metrics["ToRenderEndMilliseconds"]
        elapsed = f"{feedback['Max']:.2f}ms max to render end" if feedback else "no input (idle)"
        print(f"{result['Kind']:10} {result['Width']}x{result['Height']} {result['Scale']:.2f}: "
              f"{metrics['Fps']:.2f} FPS, {elapsed}")
    print(f"Report: {report}")


if __name__ == "__main__":
    main()
