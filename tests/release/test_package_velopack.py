"""Velopack packaging gates (DIST-06): identity, forbidden files, launcher, architectures and sums."""
import importlib.util
import json
from pathlib import Path
import plistlib
import shutil
import struct
import tempfile
import unittest
from unittest.mock import patch
import zipfile


SPEC = importlib.util.spec_from_file_location(
    "wotr_package_velopack", Path(__file__).resolve().parents[2] / "scripts/package_velopack.py")
PACKAGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PACKAGE)
WINDOWS, MACOS, LINUX = PACKAGE.TARGETS["windows"], PACKAGE.TARGETS["macos"], PACKAGE.TARGETS["linux"]
VERSION = "0.3.0"


def identity(target, **changes):
    info = {"version": VERSION, "development": False, "platform": target.platform, "sourceCommit": "a" * 40,
            "protocolVersion": 2, "publicContentSha256": "c" * 64}
    info.update(changes)
    return info


def pe(machine=PACKAGE.PE_AMD64, extra=b""):
    header = bytearray(0x200)
    header[:2] = b"MZ"
    struct.pack_into("<I", header, 0x3C, 0x80)
    header[0x80:0x84] = b"PE\0\0"
    struct.pack_into("<H", header, 0x84, machine)
    return bytes(header) + extra


def fat(*cpu_types):
    header = struct.pack(">II", 0xCAFEBABE, len(cpu_types))
    for cpu in cpu_types:
        header += struct.pack(">IIIII", cpu, 0, 0, 0, 0)
    return header + b"\0" * 64


UNIVERSAL_BINARY = fat(0x01000007, 0x0100000C)


def elf(appimage=True, machine=0x3E):
    header = bytearray(64)
    header[:4] = b"\x7fELF"
    if appimage:
        header[8:11] = b"AI\x02"
    struct.pack_into("<H", header, 18, machine)
    return bytes(header)


APPIMAGE = elf()
THIN_ARM64 = b"\xcf\xfa\xed\xfe" + struct.pack("<I", 0x0100000C) + b"\0" * 64


def nuspec(version=VERSION, os="win"):
    return (f'<?xml version="1.0"?><package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">'
            f"<metadata><id>{PACKAGE.PACK_ID}</id><version>{version}</version><os>{os}</os></metadata></package>")


def write_platform(folder, target, files, version=VERSION, info=None, installer=None):
    folder.mkdir(parents=True, exist_ok=True)
    suffix = "-full.nupkg" if target.os == "win" else f"-{target.os}-full.nupkg"
    with zipfile.ZipFile(folder / f"{PACKAGE.PACK_ID}-{version}{suffix}", "w") as archive:
        archive.writestr(f"{PACKAGE.PACK_ID}.nuspec", nuspec(version, target.os))
        for name, content in files.items():
            archive.writestr(name, content)
    default = APPIMAGE if target.os == "linux" else b"installer"
    (folder / target.installer_name(version)).write_bytes(installer or default)
    (folder / "build-info.json").write_text(json.dumps(info or identity(target)))


def windows_files(launcher=True):
    return {"lib/app/Wotr.exe": pe(extra=PACKAGE.LAUNCHER_MARKER if launcher else b"unity"),
            "lib/app/UnityPlayer.dll": pe(), "lib/app/Wotr_Data/globalgamemanagers": b"data"}


def linux_files(appimage=APPIMAGE):
    return {"lib/app/WotrOnline.AppImage": appimage}


def macos_files(executable=UNIVERSAL_BINARY):
    app = "lib/app/War of the Ring Online.app/Contents/"
    return {app + "MacOS/War of the Ring Online": executable, app + "MacOS/UpdateMac": UNIVERSAL_BINARY,
            app + "MacOS/sq.version": b"<package/>", app + "Frameworks/UnityPlayer.dylib": UNIVERSAL_BINARY,
            app + "Info.plist": plistlib.dumps({"CFBundleIdentifier": "org.wotr.fixture",
                                               "GCSupportsGameMode": False, "LSSupportsGameMode": False})}


class IdentityTests(unittest.TestCase):
    def test_only_a_release_build_of_this_version_and_platform_is_packed(self):
        self.assertEqual(identity(WINDOWS), PACKAGE.check_identity(identity(WINDOWS), WINDOWS, VERSION))
        for changes in [{"version": "0.2.9"}, {"development": True}, {"platform": "StandaloneWindows64"},
                        {"sourceCommit": ""}, {"protocolVersion": 0}]:
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                PACKAGE.check_identity(identity(WINDOWS, **changes), WINDOWS, VERSION)

    def test_smoke_version_is_above_every_release_version(self):
        self.assertEqual("0.3.1-smoke", PACKAGE.next_version("0.3.0"))
        self.assertEqual("0.2.1-smoke", PACKAGE.next_version("0.2.0-rc1"))


class ContentTests(unittest.TestCase):
    def test_every_actual_unity_test_assembly_is_refused(self):
        definitions = (PACKAGE.ROOT / "unity/Wotr.Client/Assets/Wotr/Tests").rglob("*.asmdef")
        names = [json.loads(definition.read_text())["name"] for definition in definitions]
        self.assertTrue(names)
        for name in names:
            with self.subTest(assembly=name), self.assertRaises(ValueError):
                PACKAGE.check_names(["lib/app/Wotr_Data/Managed/" + name + ".dll"])

    def test_debug_symbols_and_build_info_are_refused(self):
        for name in ["lib/app/Wotr_BurstDebugInformation_DoNotShip/a.pdb", "lib/app/build-info.json"]:
            with self.subTest(name=name), self.assertRaises(ValueError):
                PACKAGE.check_names([name])

    def test_architectures_of_mach_o_and_pe_headers(self):
        self.assertEqual({"x86_64", "arm64"}, PACKAGE.mach_o_architectures(UNIVERSAL_BINARY))
        self.assertEqual({"arm64"}, PACKAGE.mach_o_architectures(THIN_ARM64))
        java_class = struct.pack(">II", 0xCAFEBABE, 0x00000041) + b"\0" * 64
        self.assertIsNone(PACKAGE.mach_o_architectures(java_class))
        self.assertIsNone(PACKAGE.mach_o_architectures(b"<package/>"))
        self.assertEqual(PACKAGE.PE_AMD64, PACKAGE.pe_machine(pe()))
        self.assertEqual(0x14C, PACKAGE.pe_machine(pe(0x14C)))
        self.assertIsNone(PACKAGE.pe_machine(b"#!/bin/sh"))


class PackageTests(unittest.TestCase):
    def check(self, target, files, version=VERSION, installer=None):
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary) / target.platform
            write_platform(folder, target, files, version, installer=installer)
            return PACKAGE.check_package(folder, target, VERSION)

    def test_linux_package_carries_an_x86_64_appimage(self):
        self.assertEqual(f"{PACKAGE.PACK_ID}-{VERSION}-linux.AppImage", LINUX.installer_name(VERSION))
        self.check(LINUX, linux_files())
        for files, installer in [({}, None), (linux_files(elf(appimage=False)), None),
                                 (linux_files(elf(machine=0xB7)), None), (linux_files(), b"#!/bin/sh"),
                                 (linux_files(), elf(machine=0xB7))]:
            with self.subTest(files=files, installer=installer), self.assertRaises(ValueError):
                self.check(LINUX, files, installer=installer)

    def test_windows_package_starts_the_launcher_on_x64(self):
        self.check(WINDOWS, windows_files())
        unity_exe = windows_files(launcher=False)
        x86 = dict(windows_files(), **{"lib/app/UnityPlayer.dll": pe(0x14C)})
        for files in [unity_exe, x86, {"lib/app/Wotr.exe": pe(extra=PACKAGE.LAUNCHER_MARKER)}]:
            with self.subTest(files=sorted(files)), self.assertRaises(ValueError):
                self.check(WINDOWS, files)

    def test_package_must_name_this_version(self):
        with self.assertRaises(ValueError):
            self.check(WINDOWS, windows_files(), version="0.3.1")

    def test_macos_package_is_universal_with_the_updater(self):
        self.check(MACOS, macos_files())
        no_updater = {name: content for name, content in macos_files().items() if not name.endswith("UpdateMac")}
        for files in [macos_files(executable=THIN_ARM64), no_updater]:
            with self.subTest(files=sorted(files)), self.assertRaises(ValueError):
                self.check(MACOS, files)

    def test_macos_package_keeps_both_game_mode_opt_outs_as_booleans(self):
        path = "lib/app/War of the Ring Online.app/Contents/Info.plist"
        for key in ("GCSupportsGameMode", "LSSupportsGameMode"):
            for value in (None, True, "false", 0):
                files = macos_files()
                info = plistlib.loads(files[path])
                if value is None:
                    del info[key]
                else:
                    info[key] = value
                files[path] = plistlib.dumps(info)
                with self.subTest(key=key, value=value), self.assertRaisesRegex(ValueError, key):
                    self.check(MACOS, files)
        files = macos_files()
        del files[path]
        with self.assertRaisesRegex(ValueError, "Info.plist"):
            self.check(MACOS, files)


class ReleaseFolderTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.folder = Path(self.temporary.name) / VERSION
        self.base = PACKAGE.installers(self.folder)
        write_platform(self.base / WINDOWS.platform, WINDOWS, windows_files())
        write_platform(self.base / MACOS.platform, MACOS, macos_files())
        write_platform(self.base / LINUX.platform, LINUX, linux_files())
        patches = [patch.object(PACKAGE.release, "destination", return_value=self.folder),
                   patch.object(PACKAGE, "check_sources")]
        for active in patches:
            active.start()
            self.addCleanup(active.stop)

    def tearDown(self):
        self.temporary.cleanup()

    def test_sums_then_verify_pass_and_catch_changed_or_extra_files(self):
        PACKAGE.sums()
        PACKAGE.verify()
        extra = self.base / WINDOWS.platform / "notes.txt"
        extra.write_text("unlisted")
        with self.assertRaises(ValueError):
            PACKAGE.verify()
        extra.unlink()
        (self.base / MACOS.platform / MACOS.installer_name(VERSION)).write_bytes(b"changed")
        with self.assertRaises(ValueError):
            PACKAGE.verify()

    def test_platforms_from_different_commits_are_refused(self):
        for target in (MACOS, LINUX):
            original = (self.base / target.platform / "build-info.json").read_text()
            (self.base / target.platform / "build-info.json").write_text(json.dumps(identity(target, sourceCommit="b" * 40)))
            PACKAGE.sums()
            with self.subTest(platform=target.platform), self.assertRaises(ValueError):
                PACKAGE.verify()
            (self.base / target.platform / "build-info.json").write_text(original)

    def test_a_missing_linux_platform_fails_the_release(self):
        shutil.rmtree(self.base / LINUX.platform)
        with self.assertRaisesRegex(ValueError, "linux-x64"):
            PACKAGE.sums()

    def test_release_assets_are_flat_with_their_own_sums(self):
        notes = Path(self.temporary.name) / "notes.md"
        notes.write_text("Notes.")
        output = Path(self.temporary.name) / "assets"
        with patch.object(PACKAGE.release, "notes_path", return_value=notes):
            PACKAGE.release_assets(output)
        names = sorted(path.name for path in output.iterdir())
        self.assertEqual(sorted([f"{PACKAGE.PACK_ID}-{VERSION}-full.nupkg", WINDOWS.installer_name(VERSION),
                                 f"{PACKAGE.PACK_ID}-{VERSION}-osx-full.nupkg", MACOS.installer_name(VERSION),
                                 f"{PACKAGE.PACK_ID}-{VERSION}-linux-full.nupkg", LINUX.installer_name(VERSION),
                                 "build-info-windows-x64.json", "build-info-macos-universal.json",
                                 "build-info-linux-x64.json",
                                 "RELEASE-NOTES.md", "SHA256SUMS"]), names)
        sums = dict(reversed(line.split("  ")) for line in (output / "SHA256SUMS").read_text().splitlines())
        self.assertEqual(set(names) - {"SHA256SUMS"}, set(sums))
        for name, expected in sums.items():
            self.assertEqual(expected, PACKAGE.release.digest(output / name))

    def test_smoke_package_never_lands_beside_the_release(self):
        build = self.folder / WINDOWS.folder
        build.mkdir(parents=True)
        (build / "build-info.json").write_text(json.dumps(identity(WINDOWS)))
        with patch.object(PACKAGE, "pack_windows") as packer, self.assertRaises(ValueError):
            PACKAGE.pack("windows", smoke_output=str(self.base / "smoke"))
        packer.assert_not_called()


if __name__ == "__main__":
    unittest.main()
