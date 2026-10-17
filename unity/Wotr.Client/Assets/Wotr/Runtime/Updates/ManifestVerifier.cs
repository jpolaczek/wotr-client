using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    // DS16/D13: the game trusts a release only through its signed manifest. The Ed25519 signature covers the exact
    // manifest bytes; the manifest then has to name this version and platform and a package on the pinned origin.
    // Mirrors the server's ManifestRules and ManifestSignature (wotr-server Wotr.Releases).
    public static class ManifestVerifier
    {
        private const long MaximumPackageSize = 2L * 1024 * 1024 * 1024;
        private static readonly Regex FileName = new Regex(@"\A[A-Za-z0-9][A-Za-z0-9._-]{0,127}\z");
        private static readonly Regex Sha256 = new Regex(@"\A[0-9a-f]{64}\z");
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateParseHandling = DateParseHandling.DateTimeOffset
        };

        // manifest.json.sig as the release tool writes it.
        private sealed class SignatureFile
        {
            public string Algorithm { get; set; } = "";
            public string KeyId { get; set; } = "";
            public string Signature { get; set; } = "";
        }

        public static ClientReleaseManifest Verify(byte[] manifest, byte[] signature, ReleaseTrust trust, string version, string platform)
        {
            CheckSignature(manifest, Parse<SignatureFile>(signature, "signature"), trust);
            var release = Parse<ClientReleaseManifest>(manifest, "manifest");
            CheckManifest(release, trust, version, platform);
            return release;
        }

        private static void CheckSignature(byte[] manifest, SignatureFile signature, ReleaseTrust trust)
        {
            if (signature.Algorithm != "Ed25519" || signature.KeyId != trust.KeyId)
                throw new UpdateRejected("The release is signed by another key.");
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(signature.Signature);
            }
            catch (FormatException)
            {
                throw new UpdateRejected("The release signature is not base64.");
            }
            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(trust.PublicKey, 0));
            verifier.BlockUpdate(manifest, 0, manifest.Length);
            if (bytes.Length != 64 || !verifier.VerifySignature(bytes))
                throw new UpdateRejected("The release signature does not match its manifest.");
        }

        private static void CheckManifest(ClientReleaseManifest release, ReleaseTrust trust, string version, string platform)
        {
            if (release.Schema < 1 || release.Schema > ClientReleaseManifest.CurrentSchema || release.Channel != "stable")
                throw new UpdateRejected("The release manifest has an unknown schema or channel.");
            if (release.Version != version || release.Platform != platform)
                throw new UpdateRejected($"The manifest is for {release.Platform} {release.Version}, not {platform} {version}.");
            var package = release.Package ?? throw new UpdateRejected("The release manifest names no package.");
            var expectedUrl = trust.DownloadsOrigin + $"releases/{version}/{platform}/{package.FileName}";
            if (!FileName.IsMatch(package.FileName ?? "") || package.Url != expectedUrl)
                throw new UpdateRejected("The release package is not on the approved downloads origin.");
            if (package.Size <= 0 || package.Size > MaximumPackageSize || !Sha256.IsMatch(package.Sha256 ?? ""))
                throw new UpdateRejected("The release package has no valid size or SHA-256.");
        }

        private static T Parse<T>(byte[] bytes, string what) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(System.Text.Encoding.UTF8.GetString(bytes), Json)
                    ?? throw new UpdateRejected($"The release {what} is empty.");
            }
            catch (JsonException)
            {
                throw new UpdateRejected($"The release {what} is not valid JSON.");
            }
        }
    }
}
