#!/usr/bin/env python3
"""DIST-06: pack the release players into Velopack installers and update packages, then verify them.

  pack --platform windows|macos|linux   stage a BuildTasks.BuildRelease* player, add the Windows launcher and run vpk
  pack ... --smoke-output DIR     the same player under the next version, only for installer smoke tests
  sums                            write installers/SHA256SUMS over all platforms
  verify                          check identities, package contents and sums before the release is signed
  assets --output DIR             flat files for the draft GitHub Release (DIST-10), read by the release command

Windows packs on any system with mingw-w64 and icoutils; macOS packs only on macOS (codesign, pkgbuild); Linux packs
an AppImage on Linux with squashfs-tools (LNX-02). Output:
.artifacts/releases/<version>/installers/<platform>/ with the full package, the installer and build-info.json.
"""
import argparse
from dataclasses import dataclass
import json
import platform
import plistlib
import re
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path
from xml.etree import ElementTree
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
import release  # noqa: E402  (shared release inputs: version folder, source gate, content hash)

ROOT = release.ROOT
VPK_VERSION = "1.2.161"
PACK_ID = "WotrOnline"
TITLE = "War of the Ring Online"
AUTHORS = "WoTR Private"
# Unity writes debug symbols beside the player; build-info.json travels beside the package, not inside it.
STAGING_IGNORED = shutil.ignore_patterns("build-info.json", "*_DoNotShip", "*_s.debug")
# Names that must never reach a player: test assemblies, fixtures, debug symbols and secrets.
FORBIDDEN = ("Wotr.Tests", "Wotr.PlayTests", "Wotr.EditorTests", "Wotr.Fixtures", "nunit.framework",
             "UnityEngine.TestRunner", "client-integration", "DoNotShip", "/secrets/", "/.env", "build-info.json")
# Only release/windows/WotrLauncher.c carries this message; Unity's own Wotr.exe does not.
LAUNCHER_MARKER = "UnityPlayer.dll is missing or damaged".encode("utf-16-le")
PE_AMD64 = 0x8664
MACH_O_ARCHITECTURES = {0x01000007: "x86_64", 0x0100000C: "arm64", 7: "i386", 12: "arm"}
UNIVERSAL = {"x86_64", "arm64"}


@dataclass(frozen=True)
class Target:
    folder: str  # BuildTasks.BuildRelease* output beside build-info.json
    platform: str  # ClientBuildInfo platform (DIST-05) and installers/<platform>
    os: str  # vpk directive and <os> in the package's nuspec

    def installer_name(self, version):
        if self.os == "linux":
            return f"{PACK_ID}-{version}-linux.AppImage"
        return f"{PACK_ID}-{version}-{self.os}-Setup" + (".exe" if self.os == "win" else ".pkg")


TARGETS = {"windows": Target("windows", "windows-x64", "win"),
           "macos": Target("macos", "macos-universal", "osx"),
           "linux": Target("linux", "linux-x64", "linux")}
LINUX_EXECUTABLE = "Wotr.x86_64"
ICON_PNG = ROOT / "release/icons/wotr.png"


def run(*args, **kwargs):
    return subprocess.run(args, cwd=ROOT, check=True, **kwargs)


def installers(folder):
    return folder / "installers"


def next_version(version):
    """A version above the release for the second package of a smoke test: patch + 1 with a smoke pre-release."""
    major, minor, patch = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)(?:-[0-9A-Za-z.-]+)?", version).groups()
    return f"{major}.{minor}.{int(patch) + 1}-smoke"


def check_identity(info, target, version):
    if info.get("version") != version or info.get("development") is not False:
        raise ValueError(f"{target.folder}: build-info.json is not a release build of {version}.")
    if info.get("platform") != target.platform:
        raise ValueError(f"{target.folder}: build-info.json names {info.get('platform')}, not {target.platform}.")
    if not re.fullmatch(r"[0-9a-f]{40}", info.get("sourceCommit", "")) or not info.get("protocolVersion"):
        raise ValueError(f"{target.folder}: build-info.json lacks its source commit or protocol.")
    return info


def check_sources(info):
    """The player was built from the committed sources of this checkout, with the same public content."""
    release.source_commit()
    try:
        run("git", "diff", "--quiet", info["sourceCommit"], "HEAD", "--", *release.SOURCE_PATHS)
    except subprocess.CalledProcessError as error:
        raise ValueError("Client sources differ from the build commit, or the commit is missing here.") from error
    if info["publicContentSha256"] != release.public_content_hash():
        raise ValueError("Public content changed since the client build.")


def check_names(names):
    found = sorted(name for name in names if any(token in name for token in FORBIDDEN))
    if found:
        raise ValueError("Package contains test, private or development files: " + ", ".join(found[:5]))


def nuspec(archive):
    specs = [name for name in archive.namelist() if "/" not in name and name.endswith(".nuspec")]
    if len(specs) != 1:
        raise ValueError("Package has no single .nuspec.")
    metadata = ElementTree.fromstring(archive.read(specs[0])).find("{*}metadata")
    return tuple(metadata.findtext("{*}" + field) for field in ("id", "version", "os"))


def pe_machine(header):
    if header[:2] != b"MZ" or len(header) < 0x40:
        return None
    offset = struct.unpack_from("<I", header, 0x3C)[0]
    if header[offset:offset + 4] != b"PE\0\0":
        return None
    return struct.unpack_from("<H", header, offset + 4)[0]


def mach_o_architectures(header):
    """The architectures of a Mach-O file (fat or thin), or None for any other file."""
    magic = header[:4]
    if magic in (b"\xca\xfe\xba\xbe", b"\xca\xfe\xba\xbf") and len(header) >= 8:
        count = struct.unpack_from(">I", header, 4)[0]
        size = 20 if magic[3] == 0xBE else 32
        # Java class files share the fat magic; their "count" is a class file version far above any fat binary.
        if count == 0 or count > 16 or len(header) < 8 + count * size:
            return None
        return {MACH_O_ARCHITECTURES.get(struct.unpack_from(">I", header, 8 + index * size)[0], "unknown")
                for index in range(count)}
    if magic in (b"\xfe\xed\xfa\xce", b"\xfe\xed\xfa\xcf"):
        return {MACH_O_ARCHITECTURES.get(struct.unpack_from(">I", header, 4)[0], "unknown")}
    if magic in (b"\xce\xfa\xed\xfe", b"\xcf\xfa\xed\xfe"):
        return {MACH_O_ARCHITECTURES.get(struct.unpack_from("<I", header, 4)[0], "unknown")}
    return None


def check_windows_package(archive):
    names = set(archive.namelist())
    if "lib/app/Wotr.exe" not in names or not any(name.startswith("lib/app/Wotr_Data/") for name in names):
        raise ValueError("Windows package lacks lib/app/Wotr.exe or Wotr_Data.")
    launcher = archive.read("lib/app/Wotr.exe")
    if LAUNCHER_MARKER not in launcher:
        raise ValueError("Windows package starts Unity's Wotr.exe instead of the launcher (release/windows).")
    for name in ("lib/app/Wotr.exe", "lib/app/UnityPlayer.dll"):
        with archive.open(name) as stream:
            if pe_machine(stream.read(4096)) != PE_AMD64:
                raise ValueError(f"{name} is not a Windows x64 binary.")


def macos_binaries(archive):
    binaries = {}
    for entry in archive.infolist():
        if entry.is_dir():
            continue
        with archive.open(entry) as stream:
            architectures = mach_o_architectures(stream.read(4096))
        if architectures is not None:
            binaries[entry.filename] = architectures
    return binaries


def check_macos_package(archive):
    binaries = macos_binaries(archive)
    partial = sorted(f"{name} ({' '.join(sorted(found))})" for name, found in binaries.items() if not UNIVERSAL <= found)
    if partial:
        raise ValueError("macOS package has binaries without both x86_64 and arm64: " + ", ".join(partial[:5]))
    updater = next((name for name in binaries if name.endswith("/Contents/MacOS/UpdateMac")), None)
    if updater is None:
        raise ValueError("macOS package lacks Contents/MacOS/UpdateMac.")
    plist = updater.removesuffix("MacOS/UpdateMac") + "Info.plist"
    if plist not in archive.namelist():
        raise ValueError("macOS package lacks the main app's Contents/Info.plist.")
    check_macos_game_mode(plistlib.loads(archive.read(plist)))


def check_macos_game_mode(info):
    # AUDIO-09: the installer/update must retain Unity's opt-out, including the legacy macOS 14/15 key.
    for key in ("GCSupportsGameMode", "LSSupportsGameMode"):
        if info.get(key) is not False:
            raise ValueError(f"macOS app must disable {key} with a boolean false; rebuild the player.")


def is_appimage(header):
    """An x86_64 ELF with the AppImage type-2 magic ("AI" 2 at offset 8)."""
    return header[:4] == b"\x7fELF" and header[8:11] == b"AI\x02" and header[18:20] == b"\x3e\x00"


def check_linux_package(archive, installer):
    # Velopack's Linux package carries the whole AppImage; an update replaces the file in place.
    if "lib/app/WotrOnline.AppImage" not in archive.namelist():
        raise ValueError("Linux package lacks lib/app/WotrOnline.AppImage.")
    with archive.open("lib/app/WotrOnline.AppImage") as stream:
        if not is_appimage(stream.read(64)):
            raise ValueError("Linux package's WotrOnline.AppImage is not an x86_64 AppImage.")
    with installer.open("rb") as stream:
        if not is_appimage(stream.read(64)):
            raise ValueError(f"{installer.name} is not an x86_64 AppImage.")


def check_package(folder, target, version):
    """The packed folder holds exactly the package, the installer and (for a release) build-info.json."""
    packages = sorted(folder.glob("*-full.nupkg"))
    installer = folder / target.installer_name(version)
    if len(packages) != 1 or not installer.is_file():
        raise ValueError(f"{folder}: expected one *-full.nupkg and {installer.name}.")
    with zipfile.ZipFile(packages[0]) as archive:
        if archive.testzip() is not None:
            raise ValueError(f"{packages[0].name} is corrupt.")
        check_names(archive.namelist())
        if nuspec(archive) != (PACK_ID, version, target.os):
            raise ValueError(f"{packages[0].name} is not {PACK_ID} {version} for {target.os}: {nuspec(archive)}.")
        if target.os == "win":
            check_windows_package(archive)
        elif target.os == "linux":
            check_linux_package(archive, installer)
        else:
            check_macos_package(archive)
    return packages[0], installer


def vpk(*args):
    try:
        help_text = subprocess.run(["vpk", "--help"], capture_output=True, text=True).stdout
    except FileNotFoundError:
        help_text = ""
    if f"Velopack CLI {VPK_VERSION}" not in help_text:
        raise ValueError(f"Install vpk {VPK_VERSION}: dotnet tool update --global vpk --version {VPK_VERSION}")
    run("vpk", *args)


def pack_windows(source, work, version):
    stage, launcher, output = work / "player", work / "launcher", work / "out"
    shutil.copytree(source, stage, symlinks=True, ignore=STAGING_IGNORED)
    run("bash", "release/windows/build-launcher.sh", str(stage), str(launcher))
    vpk("[win]", "pack", "--packId", PACK_ID, "--packVersion", version, "--packDir", str(stage),
        "--mainExe", "Wotr.exe", "--packTitle", TITLE, "--packAuthors", AUTHORS, "--runtime", "win-x64",
        "--icon", str(launcher / "app.ico"), "--skipVeloAppCheck", "--delta", "None", "--noPortable",
        "--outputDir", str(output))
    return output


# vpk adds UpdateMac and sq.version to the bundle, so the ad-hoc signature (identity "-") must come from vpk itself:
# it signs after adding them, with hardened runtime and Velopack's default entitlements (JIT for Mono), spike S3b.
def pack_macos(source, work, version):
    if platform.system() != "Darwin":
        raise ValueError("vpk packs macOS only on macOS (codesign, pkgbuild).")
    app, output = work / "player/Wotr.app", work / "out"
    app.parent.mkdir()
    run("ditto", str(source / "Wotr.app"), str(app))
    info = plistlib.loads((app / "Contents/Info.plist").read_bytes())
    check_macos_game_mode(info)
    executable = info["CFBundleExecutable"]
    vpk("[osx]", "pack", "--packId", PACK_ID, "--packVersion", version, "--packDir", str(app),
        "--mainExe", executable, "--packTitle", TITLE, "--packAuthors", AUTHORS, "--signAppIdentity", "-",
        "--delta", "None", "--noPortable", "--outputDir", str(output))
    check_macos_signature(next(output.glob("*-Setup.pkg")), work / "expanded")
    return output


# LNX-02: the Linux player (Wotr.x86_64, UnityPlayer.so, Wotr_Data) becomes one AppImage; vpk needs mksquashfs.
def pack_linux(source, work, version):
    if platform.system() != "Linux":
        raise ValueError("vpk packs the Linux AppImage only on Linux (mksquashfs).")
    stage, output = work / "player", work / "out"
    shutil.copytree(source, stage, symlinks=True, ignore=STAGING_IGNORED)
    if not (stage / LINUX_EXECUTABLE).is_file():
        raise ValueError(f"The Linux player lacks {LINUX_EXECUTABLE}.")
    check_names(path.relative_to(stage).as_posix() for path in stage.rglob("*"))
    vpk("[linux]", "pack", "--packId", PACK_ID, "--packVersion", version, "--packDir", str(stage),
        "--mainExe", LINUX_EXECUTABLE, "--packTitle", TITLE, "--packAuthors", AUTHORS, "--runtime", "linux-x64",
        "--icon", str(ICON_PNG), "--delta", "None", "--outputDir", str(output))
    return output


def check_macos_signature(installer, expanded):
    run("pkgutil", "--expand-full", str(installer), str(expanded), stdout=subprocess.DEVNULL)
    apps = [path for path in expanded.rglob("*.app") if path.is_dir() and ".app/" not in str(path.relative_to(expanded))]
    if len(apps) != 1:
        raise ValueError(f"{installer.name} holds {len(apps)} applications, expected one.")
    check_macos_game_mode(plistlib.loads((apps[0] / "Contents/Info.plist").read_bytes()))
    run("codesign", "--verify", "--deep", "--strict", str(apps[0]))


def collect(output, destination, target, version, identity):
    packages = sorted(output.glob("*-full.nupkg"))
    setups = sorted(output.glob("*.AppImage" if target.os == "linux" else "*-Setup.*"))
    if len(packages) != 1 or len(setups) != 1:
        raise ValueError(f"vpk left {len(packages)} packages and {len(setups)} installers in {output}.")
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    shutil.copy2(packages[0], destination / packages[0].name)
    shutil.copy2(setups[0], destination / target.installer_name(version))
    if identity is not None:
        shutil.copy2(identity, destination / "build-info.json")
    return check_package(destination, target, version)


def pack(name, smoke_output=None):
    target = TARGETS[name]
    folder = release.destination()
    identity = folder / target.folder / "build-info.json"
    check_sources(check_identity(json.loads(identity.read_text()), target, folder.name))
    version, destination = folder.name, installers(folder) / target.platform
    if smoke_output is not None:
        version, destination, identity = next_version(folder.name), Path(smoke_output).resolve(), None
        if destination.is_relative_to(installers(folder).resolve()):
            raise ValueError("A smoke package never goes beside the release installers.")
    with tempfile.TemporaryDirectory(prefix="wotr-pack-") as temporary:
        packer = {"windows": pack_windows, "macos": pack_macos, "linux": pack_linux}[name]
        output = packer(folder / target.folder, Path(temporary), version)
        package, installer = collect(output, destination, target, version, identity)
    kind = "smoke package (not releasable)" if smoke_output is not None else "release package"
    print(f"Packed {name} {version} {kind}: {package.name}, {installer.name} in {destination}")


def platform_files(base):
    files = []
    for target in TARGETS.values():
        folder = base / target.platform
        if not folder.is_dir():
            raise ValueError(f"Missing {folder}; pack every platform first.")
        files.extend(path for path in folder.iterdir() if path.is_file())
    return sorted(files)


def sums():
    base = installers(release.destination())
    lines = [f"{release.digest(path)}  {path.relative_to(base).as_posix()}\n" for path in platform_files(base)]
    (base / "SHA256SUMS").write_text("".join(lines))
    print(f"Wrote {base / 'SHA256SUMS'} for {len(lines)} files.")


def check_sums(base):
    listed = {}
    for line in (base / "SHA256SUMS").read_text().splitlines():
        expected, name = line.split("  ", 1)
        listed[name] = expected
    present = {path.relative_to(base).as_posix(): path for path in platform_files(base)}
    if set(listed) != set(present):
        raise ValueError("SHA256SUMS does not list exactly the packed files.")
    for name, path in present.items():
        if release.digest(path) != listed[name]:
            raise ValueError(f"Checksum mismatch: {name}.")


def verify():
    folder = release.destination()
    base = installers(folder)
    identities = {}
    for name, target in TARGETS.items():
        info = json.loads((base / target.platform / "build-info.json").read_text())
        identities[name] = check_identity(info, target, folder.name)
        check_package(base / target.platform, target, folder.name)
    shared = ("sourceCommit", "protocolVersion", "publicContentSha256")
    if len({tuple(info[field] for field in shared) for info in identities.values()}) != 1:
        raise ValueError("All platforms must come from one commit, protocol and public content.")
    check_sources(identities["windows"])
    check_sums(base)
    print(f"PASS: {folder.name} installers for Windows x64, macOS Universal and Linux x64, one commit, sums match.")


def release_assets(output):
    """The draft GitHub Release holds flat files: packages and installers of all platforms, build-info.json named
    by platform, the release notes and SHA256SUMS over all of them."""
    folder = release.destination()
    base, output = installers(folder), Path(output)
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)
    for target in TARGETS.values():
        for path in sorted((base / target.platform).iterdir()):
            name = f"build-info-{target.platform}.json" if path.name == "build-info.json" else path.name
            shutil.copy2(path, output / name)
    shutil.copy2(release.notes_path(folder.name), output / "RELEASE-NOTES.md")
    files = sorted(output.iterdir())
    (output / "SHA256SUMS").write_text("".join(f"{release.digest(path)}  {path.name}\n" for path in files))
    print(f"Release assets for {folder.name}: {', '.join(path.name for path in files)}, SHA256SUMS")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    packer = commands.add_parser("pack")
    packer.add_argument("--platform", choices=TARGETS, required=True)
    packer.add_argument("--smoke-output", help="pack the same player as the next version into this folder")
    commands.add_parser("sums")
    commands.add_parser("verify")
    assets = commands.add_parser("assets")
    assets.add_argument("--output", required=True)
    args = parser.parse_args()
    try:
        if args.command == "pack":
            pack(args.platform, args.smoke_output)
        elif args.command == "sums":
            sums()
        elif args.command == "assets":
            release_assets(args.output)
        else:
            verify()
    except ValueError as error:
        sys.exit("ERROR: " + str(error))


if __name__ == "__main__":
    main()
