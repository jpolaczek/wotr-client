"""Client bootstrap of pinned wotr-server packages: exact bytes or a clear refusal (DIST-02/03)."""
import hashlib
import http.server
import importlib.util
import io
import json
from pathlib import Path
import shutil
import tarfile
import tempfile
import threading
import unittest
from unittest.mock import patch


SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"


def load(name):
    spec = importlib.util.spec_from_file_location("wotr_" + name, SCRIPTS / (name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


BOOTSTRAP = load("bootstrap_deps")
CONTRACTS = "com.wotr.contracts"
CONTENT = "com.wotr.public-content"
COMMIT = "0123456789ab" + "c" * 28
BASE_FILES = {"board.json": '{"regions": 1}', "cards.json": '{"cards": 2}'}


def write(root, files):
    for path, text in files.items():
        (root / path).parent.mkdir(parents=True, exist_ok=True)
        (root / path).write_text(text)


def archive(entries):
    raw = io.BytesIO()
    with tarfile.open(fileobj=raw, mode="w:gz") as tar:
        for name in sorted(entries):
            info = tarfile.TarInfo(name)
            info.size = len(entries[name])
            tar.addfile(info, io.BytesIO(entries[name]))
    return raw.getvalue()


def publish(channel, name, version, entries, extra):
    """An archive and manifest in the format of wotr-server's scripts/package_deps.py."""
    channel.mkdir(parents=True, exist_ok=True)
    data = archive(entries)
    stem = f"{name}-{version}"
    manifest = dict(extra, schema=1, name=name, version=version, commit=COMMIT, archive=stem + ".tgz",
                    size=len(data), sha256=hashlib.sha256(data).hexdigest())
    (channel / (stem + ".tgz")).write_bytes(data)
    (channel / (stem + ".manifest.json")).write_text(json.dumps(manifest))


def publish_contracts(channel, version="0.1.0"):
    package = json.dumps({"name": CONTRACTS, "version": version}).encode()
    publish(channel, CONTRACTS, version, {"package/package.json": package}, {})


def publish_content(channel, version, files, contracts="0.1.0"):
    hashes = {name: hashlib.sha256(text.encode()).hexdigest() for name, text in files.items()}
    entries = {"package/package.json": json.dumps({"name": CONTENT, "version": version}).encode()}
    entries.update({"package/Resources/" + name: text.encode() for name, text in files.items()})
    publish(channel, CONTENT, version, entries, {
        "requires": {CONTRACTS: contracts}, "contentVersion": "content-1", "fingerprint": BOOTSTRAP.fingerprint(hashes),
        "public": [{"name": name, "sha256": hashes[name]} for name in sorted(files)]})


def publish_backend(channel, name, entries):
    extra = {"entry": f"{name}/{name}.dll", "framework": "net10.0", "testOnly": True}
    publish(channel, name, COMMIT[:12], entries, extra)


def client_root(folder):
    root = folder / "client"
    write(root, {
        "unity/Wotr.Client/Packages/manifest.json":
            '{\n  "dependencies": {\n    "com.wotr.contracts": "file:../../../src/Wotr.Contracts",\n'
            '    "com.unity.test-framework": "1.6.0"\n  },\n  "testables": ["com.wotr.contracts"]\n}\n',
        "unity/Wotr.Client/Assets/Wotr/Resources/board.json": "old",
        "unity/Wotr.Client/Assets/Wotr/Resources/board.json.meta": "guid: board",
        "unity/Wotr.Client/Assets/Wotr/Resources/asset-manifest.json": "client owned",
    })
    return root


class BootstrapTests(unittest.TestCase):
    def setUp(self):
        self.folder = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.folder)
        self.channel = self.folder / "channel"
        publish_contracts(self.channel)
        publish_content(self.channel, "1.0.0", BASE_FILES)
        self.client = client_root(self.folder)
        self.environ = {}
        # No GitHub CLI: a real login must never reach these tests.
        no_cli = patch.object(BOOTSTRAP, "gh_token", return_value=None)
        no_cli.start()
        self.addCleanup(no_cli.stop)
        BOOTSTRAP.pin(self.client, self.environ, [f"{CONTRACTS}@0.1.0", f"{CONTENT}@1.0.0"], "file:" + str(self.channel))

    def lock(self):
        return json.loads((self.client / BOOTSTRAP.LOCK).read_text())

    def resource(self, name):
        return self.client / BOOTSTRAP.RESOURCES / name

    def test_pin_writes_lock_unity_reference_and_resources(self):
        manifest = (self.client / BOOTSTRAP.UNITY_MANIFEST).read_text()
        self.assertIn(f'"com.wotr.contracts": "file:../../../.deps/{CONTRACTS}-0.1.0.tgz"', manifest)
        self.assertIn('"testables": ["com.wotr.contracts"]', manifest)
        self.assertEqual('{"regions": 1}', self.resource("board.json").read_text())
        self.assertEqual("guid: board", self.resource("board.json.meta").read_text())
        self.assertEqual("client owned", self.resource("asset-manifest.json").read_text())
        packages = BOOTSTRAP.fetch(self.client, self.environ)
        self.assertEqual("0.1.0", packages[CONTRACTS]["version"])

    def test_fresh_checkout_fetches_only_the_locked_bytes(self):
        for path in (self.client / BOOTSTRAP.CACHE).iterdir():
            path.unlink()
        BOOTSTRAP.fetch(self.client, self.environ)
        self.assertTrue((self.client / BOOTSTRAP.CACHE / f"{CONTRACTS}-0.1.0.tgz").is_file())

    def test_changed_archive_is_rejected_before_use(self):
        archive = f"{CONTENT}-1.0.0.tgz"
        (self.client / BOOTSTRAP.CACHE / archive).unlink()
        tampered = bytearray((self.channel / archive).read_bytes())
        tampered[-5] ^= 1
        (self.channel / archive).write_bytes(bytes(tampered))
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "does not match the lock"):
            BOOTSTRAP.fetch(self.client, self.environ)
        self.assertFalse((self.client / BOOTSTRAP.CACHE / archive).exists())

    def test_corrupt_cache_is_replaced_by_a_verified_download(self):
        cached = self.client / BOOTSTRAP.CACHE / f"{CONTRACTS}-0.1.0.tgz"
        cached.write_bytes(b"corrupt")
        BOOTSTRAP.fetch(self.client, self.environ)
        self.assertEqual((self.channel / cached.name).read_bytes(), cached.read_bytes())

    def test_unknown_version_is_refused_without_changing_the_lock(self):
        before = (self.client / BOOTSTRAP.LOCK).read_bytes()
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "not in the channel"):
            BOOTSTRAP.pin(self.client, self.environ, [f"{CONTRACTS}@9.9.9"])
        self.assertEqual(before, (self.client / BOOTSTRAP.LOCK).read_bytes())

    def test_lock_version_without_its_archive_is_refused(self):
        lock = self.lock()
        contracts = next(p for p in lock["packages"] if p["name"] == CONTRACTS)
        contracts["version"] = "0.2.0"
        (self.client / BOOTSTRAP.LOCK).write_text(json.dumps(lock))
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "must name the archive"):
            BOOTSTRAP.fetch(self.client, self.environ)

    def test_content_must_match_the_locked_contracts(self):
        lock = self.lock()
        content = next(p for p in lock["packages"] if p["name"] == CONTENT)
        content["requires"] = {CONTRACTS: "0.0.9"}
        (self.client / BOOTSTRAP.LOCK).write_text(json.dumps(lock))
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "Pin both from the same server commit"):
            BOOTSTRAP.fetch(self.client, self.environ)

    def test_resource_drift_and_unity_manifest_drift_are_reported(self):
        self.resource("cards.json").write_text("edited")
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "cards.json"):
            BOOTSTRAP.fetch(self.client, self.environ)
        self.resource("cards.json").write_text('{"cards": 2}')
        path = self.client / BOOTSTRAP.UNITY_MANIFEST
        path.write_text(path.read_text().replace(".deps/", "elsewhere/"))
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "must reference"):
            BOOTSTRAP.fetch(self.client, self.environ)

    def test_new_content_removes_only_files_the_previous_pin_managed(self):
        publish_content(self.channel, "1.1.0", {"board.json": BASE_FILES["board.json"]})
        BOOTSTRAP.pin(self.client, self.environ, [f"{CONTENT}@1.1.0"])
        self.assertFalse(self.resource("cards.json").exists())
        self.assertTrue(self.resource("board.json.meta").exists())
        self.assertEqual("client owned", self.resource("asset-manifest.json").read_text())
        BOOTSTRAP.fetch(self.client, self.environ)

    def test_an_older_pinned_version_can_be_restored(self):
        original = (self.client / BOOTSTRAP.LOCK).read_bytes()
        publish_content(self.channel, "1.1.0", dict(BASE_FILES, **{"cards.json": '{"cards": 3}'}))
        BOOTSTRAP.pin(self.client, self.environ, [f"{CONTENT}@1.1.0"])
        self.assertEqual('{"cards": 3}', self.resource("cards.json").read_text())
        BOOTSTRAP.pin(self.client, self.environ, [f"{CONTENT}@1.0.0"])
        self.assertEqual(original, (self.client / BOOTSTRAP.LOCK).read_bytes())
        self.assertEqual('{"cards": 2}', self.resource("cards.json").read_text())
        BOOTSTRAP.fetch(self.client, self.environ)


class TestBackendTests(unittest.TestCase):
    def setUp(self):
        self.folder = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.folder)
        self.channel = self.folder / "channel"
        publish_contracts(self.channel)
        publish_content(self.channel, "1.0.0", BASE_FILES)
        self.client = client_root(self.folder)
        self.environ = {}
        no_cli = patch.object(BOOTSTRAP, "gh_token", return_value=None)
        no_cli.start()
        self.addCleanup(no_cli.stop)
        BOOTSTRAP.pin(self.client, self.environ, [f"{CONTRACTS}@0.1.0", f"{CONTENT}@1.0.0"], "file:" + str(self.channel))

    def publish(self, name, entries):
        publish_backend(self.channel, name, entries)

    def pin_backend(self):
        version = COMMIT[:12]
        BOOTSTRAP.pin(self.client, self.environ, [f"wotr-server@{version}", f"wotr-seeder@{version}"])

    def test_backend_is_pinned_and_unpacked_once(self):
        for name in BOOTSTRAP.TEST_PACKAGES:
            self.publish(name, {f"{name}/{name}.dll": b"MZ" + name.encode(), f"{name}/fixtures/a.json": b"{}"})
        self.pin_backend()
        BOOTSTRAP.fetch(self.client, self.environ, test=True)
        dll = self.client / BOOTSTRAP.TEST_CACHE / "wotr-server/wotr-server.dll"
        self.assertEqual(b"MZwotr-server", dll.read_bytes())
        self.assertTrue((self.client / BOOTSTRAP.TEST_CACHE / "wotr-seeder/fixtures/a.json").is_file())
        before = dll.stat().st_mtime_ns
        BOOTSTRAP.fetch(self.client, self.environ, test=True)
        self.assertEqual(before, dll.stat().st_mtime_ns)

    def test_unsafe_test_archive_entry_is_refused(self):
        self.publish("wotr-server", {"wotr-server/wotr-server.dll": b"MZ", "wotr-server/../escape.txt": b"x"})
        self.publish("wotr-seeder", {"wotr-seeder/wotr-seeder.dll": b"MZ"})
        self.pin_backend()
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "unsafe entry"):
            BOOTSTRAP.fetch(self.client, self.environ, test=True)
        self.assertFalse((self.client / BOOTSTRAP.TEST_CACHE / "escape.txt").exists())

    def test_integration_needs_the_whole_pinned_backend(self):
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "must pin the test backend"):
            BOOTSTRAP.fetch(self.client, self.environ, test=True)
        BOOTSTRAP.fetch(self.client, self.environ)


class FakeGitHub:
    """Releases API on one port; asset bytes behind a redirect on another, like GitHub's storage host."""

    def __init__(self, assets, secret):
        self.assets = assets
        self.secret = secret
        self.storage_headers = []
        self.api = self.serve(self.api_handler())
        self.storage = self.serve(self.storage_handler())

    def serve(self, handler):
        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler)
        threading.Thread(target=server.serve_forever, daemon=True).start()
        return server

    def url(self, server):
        return f"http://127.0.0.1:{server.server_address[1]}"

    def close(self):
        for server in (self.api, self.storage):
            server.shutdown()
            server.server_close()

    def api_handler(self):
        fake = self

        class Handler(http.server.BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                if self.headers.get("Authorization") != "Bearer " + fake.secret:
                    self.send_error(404)
                    return
                ids = sorted(fake.assets)
                if self.path.startswith("/repos/owner/server/releases/tags/"):
                    tag = self.path.rsplit("/", 1)[1]
                    assets = [{"id": ids.index(name), "name": name} for name in ids if name.startswith(tag + ".")]
                    body = json.dumps({"assets": assets}).encode()
                    self.send_response(200 if assets else 404)
                    self.end_headers()
                    self.wfile.write(body)
                elif self.path.startswith("/repos/owner/server/releases/assets/"):
                    self.send_response(302)
                    self.send_header("Location", fake.url(fake.storage) + "/blob/" + self.path.rsplit("/", 1)[1])
                    self.end_headers()
                else:
                    self.send_error(404)
        return Handler

    def storage_handler(self):
        fake = self

        class Handler(http.server.BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                fake.storage_headers.append(dict(self.headers))
                data = fake.assets[sorted(fake.assets)[int(self.path.rsplit("/", 1)[1])]]
                self.send_response(200)
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
        return Handler


class GitHubChannelTests(unittest.TestCase):
    SECRET = "ghp_test_secret_value_0123456789"

    def setUp(self):
        self.folder = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.folder)
        channel = self.folder / "channel"
        publish_contracts(channel)
        publish_content(channel, "1.0.0", BASE_FILES)
        self.github = FakeGitHub({path.name: path.read_bytes() for path in channel.iterdir()}, self.SECRET)
        self.addCleanup(self.github.close)
        self.client = client_root(self.folder)
        no_cli = patch.object(BOOTSTRAP, "gh_token", return_value=None)
        no_cli.start()
        self.addCleanup(no_cli.stop)
        self.base = {"WOTR_DEPS_GITHUB_API": self.github.url(self.github.api)}
        BOOTSTRAP.pin(self.client, dict(self.base, WOTR_DEPS_TOKEN=self.SECRET),
                      [f"{CONTRACTS}@0.1.0", f"{CONTENT}@1.0.0"], "github:owner/server")
        for path in (self.client / BOOTSTRAP.CACHE).iterdir():
            path.unlink()
        self.github.storage_headers.clear()

    def written_text(self):
        return "".join(path.read_bytes().decode(errors="replace")
                       for path in self.client.rglob("*") if path.is_file())

    def test_missing_private_access_is_explained_without_another_version(self):
        with self.assertRaisesRegex(BOOTSTRAP.DependencyError, "not available anonymously"):
            BOOTSTRAP.fetch(self.client, self.base)
        self.assertEqual([], list((self.client / BOOTSTRAP.CACHE).iterdir()))

    def test_token_reaches_only_the_api_and_is_never_written_or_reported(self):
        BOOTSTRAP.fetch(self.client, dict(self.base, WOTR_DEPS_TOKEN=self.SECRET))
        self.assertEqual(2, len(self.github.storage_headers))
        self.assertFalse([h for h in self.github.storage_headers if "Authorization" in h])
        self.assertNotIn(self.SECRET, self.written_text())
        with self.assertRaises(BOOTSTRAP.DependencyError) as refused:
            BOOTSTRAP.pin(self.client, dict(self.base, WOTR_DEPS_TOKEN=self.SECRET), [f"{CONTRACTS}@9.9.9"])
        self.assertIn("is missing from owner/server", str(refused.exception))
        self.assertNotIn(self.SECRET, str(refused.exception))
        for path in (self.client / BOOTSTRAP.CACHE).iterdir():
            path.unlink()
        wrong = dict(self.base, WOTR_DEPS_TOKEN="wrong-" + self.SECRET)
        with self.assertRaises(BOOTSTRAP.DependencyError) as denied:
            BOOTSTRAP.fetch(self.client, wrong)
        self.assertNotIn(self.SECRET, str(denied.exception))


if __name__ == "__main__":
    unittest.main()
