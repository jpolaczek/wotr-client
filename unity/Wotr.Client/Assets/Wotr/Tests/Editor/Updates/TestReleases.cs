using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // A throwaway Ed25519 publisher for updater tests: manifests and signatures in the release tool's format.
    internal sealed class TestReleases
    {
        public const string Origin = "https://downloads.example:50443/";
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private readonly Ed25519PrivateKeyParameters privateKey;

        public TestReleases()
        {
            var generator = new Ed25519KeyPairGenerator();
            generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
            var pair = generator.GenerateKeyPair();
            privateKey = (Ed25519PrivateKeyParameters)pair.Private;
            var publicKey = ((Ed25519PublicKeyParameters)pair.Public).GetEncoded();
            Trust = new ReleaseTrust(Convert.ToBase64String(publicKey), "testkey0000000001", Origin);
        }

        public ReleaseTrust Trust { get; }

        public static ClientReleaseManifest Manifest(string version = "0.9.1", string platform = ClientPlatforms.MacOS,
            byte[] package = null)
        {
            var bytes = package ?? Encoding.UTF8.GetBytes("package");
            var fileName = $"WotrOnline-{version}-osx-full.nupkg";
            return new ClientReleaseManifest
            {
                Version = version,
                Platform = platform,
                ProtocolVersion = 2,
                SourceCommit = new string('a', 40),
                Package = new ClientReleaseFile
                {
                    FileName = fileName,
                    Url = Origin + $"releases/{version}/{platform}/{fileName}",
                    Size = bytes.Length,
                    Sha256 = Sha256(bytes)
                },
                Notes = "Notes.",
                CreatedAt = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero)
            };
        }

        public static byte[] Bytes(ClientReleaseManifest manifest) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest, Formatting.Indented, Json));

        public byte[] Sign(byte[] manifest, string keyId = null, string algorithm = "Ed25519")
        {
            var signer = new Ed25519Signer();
            signer.Init(true, privateKey);
            signer.BlockUpdate(manifest, 0, manifest.Length);
            var signature = new { algorithm, keyId = keyId ?? Trust.KeyId, signature = Convert.ToBase64String(signer.GenerateSignature()) };
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(signature));
        }

        public static string Sha256(byte[] bytes)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
