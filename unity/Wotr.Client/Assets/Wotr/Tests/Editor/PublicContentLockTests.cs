using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Wotr.Editor;

namespace Wotr.Tests
{
    public sealed class PublicContentLockTests
    {
        private const string Board = "{\"version\":2}";
        private string root;
        private string resources;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "wotr-content-" + Guid.NewGuid().ToString("N"));
            resources = Path.Combine(root, "unity/Wotr.Client", PublicContentLock.ResourcesPath);
            Directory.CreateDirectory(resources);
            WriteLock("board.json", Sha(Board));
            File.WriteAllText(Path.Combine(resources, "board.json"), Board);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        [Test]
        public void PinnedCopiesPassAndClientOwnedJsonIsIgnored()
        {
            File.WriteAllText(Path.Combine(resources, "asset-manifest.json"), "client owned");
            Assert.DoesNotThrow(() => PublicContentLock.Verify(root));
        }

        [Test]
        public void ChangedOrMissingCopyFailsWithItsName()
        {
            File.WriteAllText(Path.Combine(resources, "board.json"), "edited");
            var changed = Assert.Throws<InvalidDataException>(() => PublicContentLock.Verify(root));
            Assert.That(changed.Message, Does.Contain("board.json").And.Contain("1.0.0"));

            File.Delete(Path.Combine(resources, "board.json"));
            Assert.Throws<InvalidDataException>(() => PublicContentLock.Verify(root));
        }

        [Test]
        public void MissingLockFails()
        {
            File.Delete(Path.Combine(root, PublicContentLock.LockFile));
            Assert.Throws<FileNotFoundException>(() => PublicContentLock.Verify(root));
        }

        [TestCase("../private.json", true)]
        [TestCase("board.json", false)]
        public void RejectsUnsafeNamesAndShortHashes(string name, bool validHash)
        {
            WriteLock(name, validHash ? Sha(Board) : "abc");
            Assert.Throws<InvalidDataException>(() => PublicContentLock.Verify(root));
        }

        [Test]
        public void LockWithoutPublicContentFails()
        {
            File.WriteAllText(Path.Combine(root, PublicContentLock.LockFile),
                "{\"packages\":[{\"name\":\"com.wotr.contracts\",\"version\":\"0.1.0\"}]}");
            Assert.Throws<InvalidDataException>(() => PublicContentLock.Verify(root));
        }

        private void WriteLock(string name, string sha256)
        {
            File.WriteAllText(Path.Combine(root, PublicContentLock.LockFile),
                "{\"schema\":1,\"packages\":[{\"name\":\"com.wotr.public-content\",\"version\":\"1.0.0\",\"public\":"
                + "[{\"name\":\"" + name + "\",\"sha256\":\"" + sha256 + "\"}]}]}");
        }

        private static string Sha(string text)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
    }
}
