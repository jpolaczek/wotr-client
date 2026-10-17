using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Wotr.Editor
{
    // The tracked Resources copies must be the public content pinned in wotr-deps.lock.json.
    // scripts/bootstrap_deps.py checks the archive against the lock; builds check Resources against it.
    public static class PublicContentLock
    {
        public const string ResourcesPath = "Assets/Wotr/Resources";
        public const string LockFile = "wotr-deps.lock.json";
        private const string ContentPackage = "com.wotr.public-content";

        [Serializable]
        private sealed class Lock
        {
            public Package[] packages;
        }

        [Serializable]
        private sealed class Package
        {
            public string name;
            public string version;
            public PublicFile[] @public;
        }

        [Serializable]
        private sealed class PublicFile
        {
            public string name;
            public string sha256;
        }

        public static void Verify(string repositoryRoot)
        {
            var content = PinnedContent(repositoryRoot);
            var resources = Path.Combine(repositoryRoot, "unity/Wotr.Client", ResourcesPath);
            var stale = content.@public.Where(file => !Matches(Path.Combine(resources, file.name), file.sha256))
                .Select(file => file.name).ToArray();
            if (stale.Length > 0)
                throw new InvalidDataException(ResourcesPath + " differs from " + ContentPackage + " " + content.version
                    + " in " + LockFile + ": " + string.Join(", ", stale)
                    + ". Restore the files from Git or pin a new version with scripts/bootstrap_deps.py.");
        }

        public static string PinnedVersion(string repositoryRoot) => PinnedContent(repositoryRoot).version;

        private static Package PinnedContent(string repositoryRoot)
        {
            var path = Path.Combine(repositoryRoot, LockFile);
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing " + LockFile + ". Restore it from the repository.");
            var content = JsonUtility.FromJson<Lock>(File.ReadAllText(path))?.packages?
                .SingleOrDefault(package => package.name == ContentPackage);
            var files = content?.@public;
            if (files == null || files.Length == 0 || files.Select(file => file.name).Distinct().Count() != files.Length)
                throw new InvalidDataException(LockFile + " must pin " + ContentPackage + " with distinct public files.");
            foreach (var file in files)
            {
                if (file.name == null || !Regex.IsMatch(file.name, @"\A[a-z0-9-]+\.json\z")
                    || file.sha256 == null || !Regex.IsMatch(file.sha256, @"\A[0-9a-f]{64}\z"))
                    throw new InvalidDataException(LockFile + " lists an invalid public file.");
            }
            return content;
        }

        private static bool Matches(string path, string sha256)
        {
            if (!File.Exists(path))
                return false;
            using (var hash = SHA256.Create())
            {
                var actual = hash.ComputeHash(File.ReadAllBytes(path));
                return BitConverter.ToString(actual).Replace("-", "").ToLowerInvariant() == sha256;
            }
        }
    }
}
