using System;
using NUnit.Framework;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // DIST-07: a release is trusted only through its manifest signed with the pinned key, for this version and
    // platform, with the package on the approved downloads origin.
    public class ManifestVerifierTests
    {
        private readonly TestReleases releases = new TestReleases();

        [Test]
        public void SignedManifestOfThisReleaseIsAccepted()
        {
            var bytes = TestReleases.Bytes(TestReleases.Manifest());
            var manifest = ManifestVerifier.Verify(bytes, releases.Sign(bytes), releases.Trust, "0.9.1", ClientPlatforms.MacOS);
            Assert.AreEqual("WotrOnline-0.9.1-osx-full.nupkg", manifest.Package.FileName);
        }

        [Test]
        public void ChangedBytesOrAnotherKeyAreRefused()
        {
            var bytes = TestReleases.Bytes(TestReleases.Manifest());
            var signature = releases.Sign(bytes);
            var changed = (byte[])bytes.Clone();
            changed[changed.Length - 3] ^= 1;
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(changed, signature, releases.Trust, "0.9.1", ClientPlatforms.MacOS));
            var other = new TestReleases();
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, other.Sign(bytes, releases.Trust.KeyId), releases.Trust, "0.9.1", ClientPlatforms.MacOS));
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, releases.Sign(bytes, "otherkey"), releases.Trust, "0.9.1", ClientPlatforms.MacOS));
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, releases.Sign(bytes, algorithm: "RSA"), releases.Trust, "0.9.1", ClientPlatforms.MacOS));
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, System.Text.Encoding.UTF8.GetBytes("not json"), releases.Trust, "0.9.1", ClientPlatforms.MacOS));
        }

        [Test]
        public void ManifestOfAnotherReleaseOrPlatformIsRefused()
        {
            var bytes = TestReleases.Bytes(TestReleases.Manifest());
            var signature = releases.Sign(bytes);
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, signature, releases.Trust, "0.9.2", ClientPlatforms.MacOS));
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, signature, releases.Trust, "0.9.1", ClientPlatforms.Windows));
        }

        [TestCase("url")]
        [TestCase("file")]
        [TestCase("size")]
        [TestCase("sha")]
        [TestCase("channel")]
        public void SignedManifestWithAnUnsafePackageIsStillRefused(string damage)
        {
            var manifest = TestReleases.Manifest();
            if (damage == "url")
                manifest.Package.Url = "https://elsewhere.example/releases/0.9.1/macos-universal/" + manifest.Package.FileName;
            if (damage == "file")
                manifest.Package.FileName = "../WotrOnline.nupkg";
            if (damage == "size")
                manifest.Package.Size = 0;
            if (damage == "sha")
                manifest.Package.Sha256 = "ABC";
            if (damage == "channel")
                manifest.Channel = "beta";
            var bytes = TestReleases.Bytes(manifest);
            Assert.Throws<UpdateRejected>(() => ManifestVerifier.Verify(bytes, releases.Sign(bytes), releases.Trust, "0.9.1", ClientPlatforms.MacOS));
        }
    }
}
