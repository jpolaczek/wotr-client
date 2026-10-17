using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // DS10: the identity this client declares on every game request and WebSocket handshake. A built player reads
    // the copy its build embedded; the Editor derives a development identity from release/version.json.
    public static class ClientIdentity
    {
        public const string ResourceName = "wotr-build-info";
        public const string UnidentifiedVersion = "0.0.0-unidentified";
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Error
        };
        private static ClientBuildInfo current;

        public static ClientBuildInfo Current => current ?? (current = Load());

        // The embedded text byte for byte, so a probe can compare it with build-info.json.
        public static string EmbeddedText => Resources.Load<TextAsset>(ResourceName)?.text;

        public static string Serialize(ClientBuildInfo info)
        {
            var text = new StringBuilder();
            using (var writer = new StringWriter(text, CultureInfo.InvariantCulture) { NewLine = "\n" })
                JsonSerializer.Create(Json).Serialize(writer, info);
            return text.Append('\n').ToString();
        }

        public static ClientBuildInfo Parse(string text) =>
            JsonConvert.DeserializeObject<ClientBuildInfo>(text, Json) ?? throw new InvalidDataException("Empty client build info.");

        public static void Declare(HttpRequestHeaders headers, ClientBuildInfo identity)
        {
            headers.Add(ClientCompatibility.ClientVersionHeader, identity.Version);
            headers.Add(ClientCompatibility.ProtocolVersionHeader, identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture));
        }

        private static ClientBuildInfo Load()
        {
            var embedded = EmbeddedText;
            return embedded != null ? Parse(embedded) : Development();
        }

        private static ClientBuildInfo Development() => new ClientBuildInfo
        {
            Version = DevelopmentVersion(),
            Platform = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
                ? ClientPlatforms.Windows : ClientPlatforms.MacOS,
            Development = true,
            ProtocolVersion = Protocol.Version,
            RulesVersion = Protocol.RulesVersion,
            ContentVersion = Protocol.ContentVersion,
            UnityVersion = Application.unityVersion
        };

        // A player without an embedded identity was not built through BuildTasks and says so.
        private static string DevelopmentVersion()
        {
#if UNITY_EDITOR
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../release/version.json"));
            var version = File.Exists(path) ? JsonConvert.DeserializeObject<VersionFile>(File.ReadAllText(path))?.Version : null;
            if (ClientVersion.TryParse(version, out var parsed))
                return parsed.Text;
#endif
            return UnidentifiedVersion;
        }

        private sealed class VersionFile
        {
            public string Version { get; set; }
        }
    }
}
