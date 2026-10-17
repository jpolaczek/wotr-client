using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using Wotr.Contracts;
using Wotr.Editor;

namespace Wotr.Client.Tests
{
    // DIST-05: one build identity, declared on every game request, and version refusals that stop retrying.
    public class ClientIdentityTests
    {
        [Test]
        public async Task EveryGameRequestDeclaresTheBuildVersionAndProtocol()
        {
            using var handler = new RecordingHandler(HttpStatusCode.OK, "{\"token\":\"t\",\"accountId\":\"a\",\"username\":\"u\"}");
            using var api = new GameApi("https://localhost", handler, Identity("0.3.0-rc.2"));
            await api.Login("user", "a long enough password", CancellationToken.None);
            await api.Get<object>("api/rooms", CancellationToken.None);
            await api.Post<object>("api/rooms", new CreateRoomRequest(), CancellationToken.None);
            Assert.AreEqual(3, handler.Versions.Count);
            CollectionAssert.AreEqual(Enumerable.Repeat("0.3.0-rc.2/" + Protocol.Version, 3), handler.Versions);
        }

        [TestCase(ClientCompatibility.ClientUpdateRequired)]
        [TestCase(ClientCompatibility.ServerUpdateRequired)]
        public async Task VersionRefusalStopsAutomaticReconnectAndKeepsItsCode(string code)
        {
            using var handler = new RecordingHandler(HttpStatusCode.Conflict, "{\"error\":\"" + code + "\"}");
            using var api = new GameApi("https://localhost", handler, Identity("0.2.0"));
            using var connection = new MatchConnection(api, "test");
            var refusals = 0;
            connection.StatusChanged += status => { if (status == code) refusals++; };
            connection.Start();
            await Task.Delay(50);
            Assert.AreEqual(1, refusals);
            Assert.AreEqual(1, handler.Versions.Count);
        }

        // DIST-07: whichever screen sent the request, the updater hears a version refusal; other conflicts stay local.
        [TestCase(ClientCompatibility.ClientUpdateRequired, true)]
        [TestCase(ClientCompatibility.ServerUpdateRequired, true)]
        [TestCase("StaleConfiguration", false)]
        public async Task EveryVersionRefusalReachesTheUpdater(string code, bool refusal)
        {
            using var handler = new RecordingHandler(HttpStatusCode.Conflict, "{\"error\":\"" + code + "\"}");
            using var api = new GameApi("https://localhost", handler, Identity("0.2.0"));
            var heard = new List<string>();
            Action<string> listen = heard.Add;
            GameApi.VersionRefused += listen;
            try
            {
                var error = await Expect.Throws<ApiException>(() => api.Get<object>("api/rooms", CancellationToken.None));
                Assert.AreEqual(code, error.Message);
                Assert.AreEqual(refusal, VersionRefusal.Is(error));
                CollectionAssert.AreEqual(refusal ? new[] { code } : new string[0], heard);
            }
            finally
            {
                GameApi.VersionRefused -= listen;
            }
        }

        [Test]
        public void IdentityTextUsesTheContractFieldNamesAndRoundTrips()
        {
            var text = ClientIdentity.Serialize(Identity("0.3.0"));
            StringAssert.Contains("\"protocolVersion\": " + Protocol.Version, text);
            StringAssert.Contains("\"publicContentSha256\": \"" + new string('c', 64) + "\"", text);
            Assert.IsFalse(text.Contains("\r"));
            StringAssert.EndsWith("}\n", text);
            Assert.AreEqual(text, ClientIdentity.Serialize(ClientIdentity.Parse(text)));
            Assert.Throws<JsonSerializationException>(() => ClientIdentity.Parse("{\"version\":\"0.3.0\",\"channel\":\"beta\"}"));
        }

        [Test]
        public void GeneratorNamesThisCheckoutAndOnlyReleasesNeedAnExplicitCommit()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../.."));
            var version = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "release/version.json")))["version"];
            var local = ClientBuildIdentity.Create(BuildTarget.StandaloneWindows64, release: false);
            Assert.AreEqual(version, local.Version);
            Assert.AreEqual(ClientPlatforms.Windows, local.Platform);
            Assert.IsTrue(local.Development);
            Assert.AreEqual(PublicContentLock.PinnedVersion(root), local.PublicContentVersion);
            Assert.AreEqual(ClientPlatforms.MacOS, ClientBuildIdentity.Create(BuildTarget.StandaloneOSX, release: false).Platform);
            if (Environment.GetEnvironmentVariable("WOTR_BUILD_COMMIT") == null)
                Assert.Throws<InvalidOperationException>(() => ClientBuildIdentity.Create(BuildTarget.StandaloneOSX, release: true));
        }

        // DS10: the runtime reads exactly the bytes BuildTasks embeds; the folder is removed after the build.
        [Test]
        public void RuntimeReadsTheEmbeddedIdentityByteForByte()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(ClientBuildIdentity.GeneratedFolder), "A previous build left its identity behind.");
            try
            {
                var embedded = ClientBuildIdentity.Embed(ClientBuildIdentity.Create(BuildTarget.StandaloneOSX, release: false));
                Assert.AreEqual(embedded, ClientIdentity.EmbeddedText);
                Assert.AreEqual(ClientPlatforms.MacOS, ClientIdentity.Parse(ClientIdentity.EmbeddedText).Platform);
                Assert.DoesNotThrow(() => ClientBuildIdentity.RequireEmbedded(BuildTarget.StandaloneOSX));
                Assert.Throws<UnityEditor.Build.BuildFailedException>(() => ClientBuildIdentity.RequireEmbedded(BuildTarget.StandaloneWindows64));
            }
            finally
            {
                ClientBuildIdentity.RemoveEmbedded();
            }
            Assert.IsNull(ClientIdentity.EmbeddedText);
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => ClientBuildIdentity.RequireEmbedded(BuildTarget.StandaloneOSX));
        }

        [Test]
        public void EditorDeclaresADevelopmentBuildOfTheRepositoryVersion()
        {
            var identity = ClientIdentity.Current;
            Assert.IsTrue(identity.Development);
            Assert.IsTrue(ClientVersion.TryParse(identity.Version, out _));
            Assert.AreNotEqual(ClientIdentity.UnidentifiedVersion, identity.Version);
            Assert.AreEqual(Protocol.Version, identity.ProtocolVersion);
        }

        [Test]
        public void PinnedPublisherKeyIsEd25519SizedAndMatchesItsId()
        {
            var key = Convert.FromBase64String(ReleasePublisher.PublicKey);
            Assert.AreEqual(32, key.Length);
            using (var sha = SHA256.Create())
                Assert.AreEqual(ReleasePublisher.KeyId, BitConverter.ToString(sha.ComputeHash(key), 0, 8).Replace("-", "").ToLowerInvariant());
        }

        private static ClientBuildInfo Identity(string version) => new ClientBuildInfo
        {
            Version = version,
            SourceCommit = new string('a', 40),
            Platform = ClientPlatforms.MacOS,
            ProtocolVersion = Protocol.Version,
            RulesVersion = Protocol.RulesVersion,
            ContentVersion = Protocol.ContentVersion,
            PublicContentVersion = "1.0.1",
            PublicContentSha256 = new string('c', 64),
            UnityVersion = "6000.6.0f1"
        };

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode status;
            private readonly string body;
            public List<string> Versions { get; } = new List<string>();

            public RecordingHandler(HttpStatusCode status, string body)
            {
                this.status = status;
                this.body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Versions.Add(request.Headers.GetValues(ClientCompatibility.ClientVersionHeader).Single() + "/"
                    + request.Headers.GetValues(ClientCompatibility.ProtocolVersionHeader).Single());
                return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }
        }
    }
}
