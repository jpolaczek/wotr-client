#!/usr/bin/env python3
"""Fetch and verify the client's pinned server dependencies before Unity runs (DIST-02).

wotr-deps.lock.json names the exact Contracts and public content archives and their
SHA-256. `fetch` (the default) downloads missing archives into .deps/, rejects any
other bytes, and checks that Unity's manifest and the tracked Resources copies match
the lock. `pin` is the only way to change the lock. Tokens come from the environment
or the GitHub CLI and are never written or printed.

`fetch --test` also unpacks the pinned test backend (server and PlayMode seeder,
DIST-03) into .deps/test/<name>/ for client integration.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
LOCK = "wotr-deps.lock.json"
CACHE = ".deps"
UNITY_MANIFEST = "unity/Wotr.Client/Packages/manifest.json"
RESOURCES = "unity/Wotr.Client/Assets/Wotr/Resources"
CONTRACTS = "com.wotr.contracts"
CONTENT = "com.wotr.public-content"
TEST_PACKAGES = ("wotr-server", "wotr-seeder")
TEST_CACHE = CACHE + "/test"
DEFAULT_CHANNEL = "github:jpolaczek/wotr-server"
GITHUB_API = "https://api.github.com"
SEMVER = re.compile(r"\d+\.\d+\.\d+")
SHA256 = re.compile(r"[0-9a-f]{64}")
COMMIT = re.compile(r"[0-9a-f]{40}")
PUBLIC_NAME = re.compile(r"[a-z0-9-]+\.json")
CONTRACTS_REFERENCE = re.compile(r'("com\.wotr\.contracts"\s*:\s*)"[^"]*"')


class DependencyError(Exception):
    pass


def digest(data):
    return hashlib.sha256(data).hexdigest()


def fingerprint(files):
    lines = "".join(f"{name}:{files[name]}\n" for name in sorted(files))
    return digest(lines.encode())


def write_atomic(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".partial")
    temporary.write_bytes(data)
    os.replace(temporary, path)


def unity_reference(package):
    return "file:../../../" + CACHE + "/" + package["archive"]


# --- Lock -----------------------------------------------------------------------------

def check_package(package):
    name, version = package.get("name"), package.get("version")
    if name not in (CONTRACTS, CONTENT) or not SEMVER.fullmatch(str(version)):
        raise DependencyError(f"Lock entry {name!r} needs a known name and a plain x.y.z version.")
    if package.get("archive") != f"{name}-{version}.tgz":
        raise DependencyError(f"Lock entry {name} must name the archive {name}-{version}.tgz.")
    if not SHA256.fullmatch(str(package.get("sha256"))) or not COMMIT.fullmatch(str(package.get("commit"))):
        raise DependencyError(f"Lock entry {name} needs a SHA-256 and the server commit.")
    if not isinstance(package.get("size"), int) or package["size"] <= 0:
        raise DependencyError(f"Lock entry {name} needs the archive size.")


def check_content(content, contracts):
    files = content.get("public")
    names = [entry.get("name") for entry in files or []]
    if not names or len(names) != len(set(names)) or not all(PUBLIC_NAME.fullmatch(str(n)) for n in names):
        raise DependencyError(f"Lock entry {CONTENT} must list distinct public JSON files.")
    if not all(SHA256.fullmatch(str(entry.get("sha256"))) for entry in files):
        raise DependencyError(f"Lock entry {CONTENT} needs a SHA-256 for every public file.")
    if content.get("fingerprint") != fingerprint({e["name"]: e["sha256"] for e in files}):
        raise DependencyError(f"Lock entry {CONTENT} has a fingerprint that does not match its files.")
    required = (content.get("requires") or {}).get(CONTRACTS)
    if required != contracts["version"]:
        raise DependencyError(f"{CONTENT} {content['version']} was built with {CONTRACTS} {required}, "
                              f"but the lock pins {contracts['version']}. Pin both from the same server commit.")


def check_test_package(package):
    name, version = package.get("name"), str(package.get("version"))
    if name not in TEST_PACKAGES or not re.fullmatch(r"[0-9a-f]{12}", version):
        raise DependencyError(f"Test entry {name!r} needs a known name and a 12-character server commit version.")
    if package.get("archive") != f"{name}-{version}.tgz" or not str(package.get("commit")).startswith(version):
        raise DependencyError(f"Test entry {name} must name {name}-{version}.tgz from commit {version}.")
    if not SHA256.fullmatch(str(package.get("sha256"))) or not isinstance(package.get("size"), int):
        raise DependencyError(f"Test entry {name} needs a SHA-256 and the archive size.")
    if not str(package.get("entry", "")).startswith(name + "/"):
        raise DependencyError(f"Test entry {name} needs the path of its entry assembly.")


def test_packages(lock):
    packages = {package.get("name"): package for package in lock.get("testPackages", [])}
    if len(packages) != len(lock.get("testPackages", [])):
        raise DependencyError(LOCK + " pins a test package twice.")
    for package in packages.values():
        check_test_package(package)
    return packages


def check_lock(lock):
    if lock.get("schema") != 1 or not isinstance(lock.get("channel"), str):
        raise DependencyError(LOCK + " needs schema 1 and a channel.")
    packages = {package.get("name"): package for package in lock.get("packages", [])}
    if sorted(packages) != sorted([CONTRACTS, CONTENT]) or len(packages) != len(lock["packages"]):
        raise DependencyError(LOCK + f" must pin exactly {CONTRACTS} and {CONTENT}.")
    for package in packages.values():
        check_package(package)
    check_content(packages[CONTENT], packages[CONTRACTS])
    test_packages(lock)
    return packages


def load_lock(root):
    path = root / LOCK
    if not path.is_file():
        raise DependencyError(f"Missing {LOCK}. Restore it from the repository.")
    lock = json.loads(path.read_text())
    return lock, check_lock(lock)


def save_lock(root, lock):
    lock["packages"] = sorted(lock["packages"], key=lambda package: package["name"])
    write_atomic(root / LOCK, (json.dumps(lock, indent=2) + "\n").encode())


# --- Channels -------------------------------------------------------------------------

def gh_token():
    try:
        result = subprocess.run(["gh", "auth", "token"], stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
    except FileNotFoundError:
        return None
    return result.stdout.strip() or None


def token(environ):
    for key in ("WOTR_DEPS_TOKEN", "GITHUB_TOKEN"):
        if environ.get(key):
            return environ[key]
    return gh_token()


class FileChannel:
    def __init__(self, folder):
        self.folder = folder

    def fetch(self, name, version, filename):
        path = self.folder / filename
        if not path.is_file():
            raise DependencyError(f"{filename} is not in the channel {self.folder}. "
                                  "The pinned version must be published there; no other version is used.")
        return path.read_bytes()


class GitHubChannel:
    """Release `<name>-<version>` of a repository holds the archive and its manifest; public releases need no token."""

    def __init__(self, repository, secret, api):
        self.repository = repository
        self.secret = secret
        self.api = api.rstrip("/")

    def fetch(self, name, version, filename):
        tag = f"{name}-{version}"
        path = f"/repos/{self.repository}/releases/tags/{urllib.parse.quote(tag)}"
        release = json.loads(self.request(path, "application/vnd.github+json", tag))
        asset = next((item for item in release.get("assets", []) if item.get("name") == filename), None)
        if asset is None:
            raise DependencyError(f"Release {tag} in {self.repository} has no asset {filename}.")
        return self.request(f"/repos/{self.repository}/releases/assets/{asset['id']}", "application/octet-stream", tag)

    def request(self, path, accept, tag):
        request = urllib.request.Request(self.api + path, headers={
            "Accept": accept, "User-Agent": "wotr-bootstrap", "X-GitHub-Api-Version": "2022-11-28"})
        if self.secret:
            # Unredirected: the asset redirect goes to another host, which must not receive the token.
            request.add_unredirected_header("Authorization", "Bearer " + self.secret)
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            raise DependencyError(self.refusal(error.code, tag)) from None
        except urllib.error.URLError as error:
            raise DependencyError(f"Cannot reach the dependency channel {self.api}: {error.reason}.") from None

    def refusal(self, status, tag):
        if status in (401, 403, 404) and not self.secret:
            return (f"Release {tag} is not available anonymously from {self.repository} (HTTP {status}). "
                    "The public channel needs no token; for a private channel set WOTR_DEPS_TOKEN or run `gh auth login`.")
        if status in (401, 403, 404):
            return (f"Release {tag} is missing from {self.repository}, or the token cannot read it (HTTP {status}). "
                    "The pinned version must be published; no other version is used.")
        return f"The dependency channel returned HTTP {status} for {tag}."


def open_channel(spec, root, environ):
    kind, _, location = spec.partition(":")
    if kind == "file" and location:
        folder = Path(location)
        return FileChannel(folder if folder.is_absolute() else root / folder)
    if kind == "github" and re.fullmatch(r"[\w.-]+/[\w.-]+", location):
        return GitHubChannel(location, token(environ), environ.get("WOTR_DEPS_GITHUB_API", GITHUB_API))
    raise DependencyError(f"Unknown dependency channel {spec!r}; use file:<folder> or github:<owner>/<repo>.")


def channel_for(lock, root, environ):
    return open_channel(environ.get("WOTR_DEPS_SOURCE") or lock["channel"], root, environ)


# --- Archives -------------------------------------------------------------------------

def check_bytes(package, data, origin):
    if len(data) != package["size"] or digest(data) != package["sha256"]:
        raise DependencyError(f"{package['archive']} from {origin} does not match the lock "
                              f"(sha256 {digest(data)}, expected {package['sha256']}). It was not used.")


def ensure(package, root, channel):
    """Return the verified archive bytes, fetching them only when the cache lacks those exact bytes."""
    path = root / CACHE / package["archive"]
    if path.is_file():
        data = path.read_bytes()
        if len(data) == package["size"] and digest(data) == package["sha256"]:
            return data
    data = channel.fetch(package["name"], package["version"], package["archive"])
    check_bytes(package, data, "the channel")
    write_atomic(path, data)
    return data


def public_files(package, data):
    """Read the content archive, accepting only its package.json and the locked public files."""
    expected = {entry["name"]: entry["sha256"] for entry in package["public"]}
    files, metadata = {}, None
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        for member in archive.getmembers():
            name = member.name
            if not member.isreg():
                raise DependencyError(f"{package['archive']} contains a non-file entry {name}.")
            if name == "package/package.json":
                metadata = json.loads(archive.extractfile(member).read())
            elif name.startswith("package/Resources/") and name[len("package/Resources/"):] in expected:
                files[name[len("package/Resources/"):]] = archive.extractfile(member).read()
            else:
                raise DependencyError(f"{package['archive']} contains {name}, which the lock does not allow.")
    if metadata is None or metadata.get("name") != CONTENT or metadata.get("version") != package["version"]:
        raise DependencyError(f"{package['archive']} does not describe {CONTENT} {package['version']}.")
    if sorted(files) != sorted(expected) or any(digest(files[n]) != expected[n] for n in expected):
        raise DependencyError(f"{package['archive']} files differ from the locked public files.")
    return files


# --- Client checks --------------------------------------------------------------------

def check_unity_manifest(root, contracts):
    dependencies = json.loads((root / UNITY_MANIFEST).read_text())["dependencies"]
    if dependencies.get(CONTRACTS) != unity_reference(contracts):
        raise DependencyError(f"{UNITY_MANIFEST} must reference {unity_reference(contracts)} for {CONTRACTS}.")
    if CONTENT in dependencies:
        raise DependencyError(f"{UNITY_MANIFEST} must not import {CONTENT}; Resources keep its tracked copies.")


def stale_resources(root, files):
    resources = root / RESOURCES
    return sorted(name for name, data in files.items()
                  if not (resources / name).is_file() or (resources / name).read_bytes() != data)


def unpack(root, package, data):
    """Extract a verified test archive once; its marker names the archive bytes it holds."""
    target = root / TEST_CACHE / package["name"]
    marker = target / ".archive-sha256"
    if marker.is_file() and marker.read_text() == package["sha256"]:
        return target
    shutil.rmtree(target, ignore_errors=True)
    prefix = package["name"] + "/"
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        for member in archive.getmembers():
            relative = Path(member.name[len(prefix):]) if member.name.startswith(prefix) else None
            if not member.isreg() or relative is None or relative.is_absolute() or ".." in relative.parts:
                raise DependencyError(f"{package['archive']} contains an unsafe entry {member.name}.")
            write_atomic(target / relative, archive.extractfile(member).read())
    if not (root / TEST_CACHE / package["entry"]).is_file():
        raise DependencyError(f"{package['archive']} lacks its entry {package['entry']}.")
    marker.write_text(package["sha256"])
    return target


def fetch_test_backend(root, lock, channel):
    packages = test_packages(lock)
    if sorted(packages) != sorted(TEST_PACKAGES):
        raise DependencyError(LOCK + " must pin the test backend: " + ", ".join(TEST_PACKAGES) + ".")
    for package in packages.values():
        unpack(root, package, ensure(package, root, channel))
    return packages


def fetch(root, environ, test=False):
    lock, packages = load_lock(root)
    channel = channel_for(lock, root, environ)
    data = {name: ensure(package, root, channel) for name, package in packages.items()}
    files = public_files(packages[CONTENT], data[CONTENT])
    check_unity_manifest(root, packages[CONTRACTS])
    stale = stale_resources(root, files)
    if stale:
        raise DependencyError(f"{RESOURCES} differs from the pinned {CONTENT} {packages[CONTENT]['version']}: "
                              + ", ".join(stale) + ". Restore them from Git or pin a new version.")
    if test:
        return dict(packages, **fetch_test_backend(root, lock, channel))
    return packages


# --- Pinning --------------------------------------------------------------------------

def entry_from_manifest(manifest):
    entry = {key: manifest[key] for key in ("name", "version", "archive", "sha256", "size", "commit")}
    if manifest["name"] == CONTENT:
        entry.update({key: manifest[key] for key in ("requires", "contentVersion", "fingerprint", "public")})
    if manifest["name"] in TEST_PACKAGES:
        entry["entry"] = manifest["entry"]
    return entry


def fetch_pinned(channel, name, version):
    stem = f"{name}-{version}"
    manifest = json.loads(channel.fetch(name, version, stem + ".manifest.json"))
    entry = entry_from_manifest(manifest)
    (check_test_package if name in TEST_PACKAGES else check_package)(entry)
    if entry["name"] != name or entry["version"] != version:
        raise DependencyError(f"{stem}.manifest.json describes {entry['name']} {entry['version']}.")
    data = channel.fetch(name, version, entry["archive"])
    check_bytes(entry, data, "the channel")
    return entry, data


def apply_content(root, old, files):
    resources = root / RESOURCES
    for name, data in files.items():
        write_atomic(resources / name, data)
    # Remove only files the previous pin managed and the new one no longer lists, with their .meta.
    previous = {entry["name"] for entry in (old or {}).get("public", [])}
    for name in sorted(previous - set(files)):
        for path in (resources / name, resources / (name + ".meta")):
            path.unlink(missing_ok=True)


def apply_contracts(root, entry):
    path = root / UNITY_MANIFEST
    text, count = CONTRACTS_REFERENCE.subn(r'\g<1>"' + unity_reference(entry) + '"', path.read_text())
    if count != 1:
        raise DependencyError(f"{UNITY_MANIFEST} must contain exactly one {CONTRACTS} dependency.")
    write_atomic(path, text.encode())


def pin(root, environ, requests, channel_spec=None):
    path = root / LOCK
    lock = json.loads(path.read_text()) if path.is_file() else {"schema": 1, "channel": DEFAULT_CHANNEL, "packages": []}
    if channel_spec:
        lock["channel"] = channel_spec
    channel = channel_for(lock, root, environ)
    current = {package["name"]: package for package in lock["packages"]}
    fetched = {}
    for request in requests:
        name, _, version = request.partition("@")
        valid = SEMVER.fullmatch(version) if name in (CONTRACTS, CONTENT) else re.fullmatch(r"[0-9a-f]{12}", version)
        if name not in (CONTRACTS, CONTENT) + TEST_PACKAGES or not valid or name in fetched:
            raise DependencyError(f"Pin {request!r} as {CONTRACTS}@x.y.z, {CONTENT}@x.y.z "
                                  "or wotr-server/wotr-seeder@<12-character commit>, once each.")
        fetched[name] = fetch_pinned(channel, name, version)
    proposed = dict(current, **{n: entry for n, (entry, _) in fetched.items() if n not in TEST_PACKAGES})
    lock["packages"] = list(proposed.values())
    tests = {package["name"]: package for package in lock.get("testPackages", [])}
    tests.update({n: entry for n, (entry, _) in fetched.items() if n in TEST_PACKAGES})
    if tests:
        lock["testPackages"] = sorted(tests.values(), key=lambda package: package["name"])
    packages = check_lock(lock)
    files = public_files(packages[CONTENT], fetched[CONTENT][1]) if CONTENT in fetched else None
    for entry, data in fetched.values():
        write_atomic(root / CACHE / entry["archive"], data)
    if files is not None:
        apply_content(root, current.get(CONTENT), files)
    if CONTRACTS in fetched:
        apply_contracts(root, packages[CONTRACTS])
    save_lock(root, lock)
    return packages


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest="command")
    fetching = commands.add_parser("fetch", help="Fetch and verify the pinned archives (default).")
    fetching.add_argument("--test", action="store_true", help="Also unpack the pinned test backend for PlayMode.")
    pinning = commands.add_parser("pin", help="Pin exact versions, e.g. com.wotr.contracts@0.2.0.")
    pinning.add_argument("packages", nargs="+")
    pinning.add_argument("--channel", help="Store this channel in the lock (file:<folder> or github:<owner>/<repo>).")
    args = parser.parse_args()
    try:
        if args.command == "pin":
            packages = pin(ROOT, os.environ, args.packages, args.channel)
        else:
            packages = fetch(ROOT, os.environ, getattr(args, "test", False))
    except DependencyError as error:
        raise SystemExit("bootstrap_deps: " + str(error))
    for name in sorted(packages):
        print(f"{name} {packages[name]['version']} {packages[name]['sha256']}")


if __name__ == "__main__":
    main()
