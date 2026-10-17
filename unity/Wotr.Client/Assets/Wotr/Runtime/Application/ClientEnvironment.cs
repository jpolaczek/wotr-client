using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wotr.Client
{
    // MENU-28: the addresses of the game's hosts come from outside git — the environment first, then the client's
    // .env file (wotr-client/.env; wotr-client/.env.example lists the keys). The editor reads them directly; a build
    // embeds them for the duration of the build (Editor/BuildServerAddresses).
    public static class ClientEnvironment
    {
        public const string ServerKey = "WOTR_SERVER_URL";
        public const string EditorServerKey = "WOTR_EDITOR_SERVER_URL";
        public const string DownloadsKey = "WOTR_DOWNLOADS_URL";
        public const string LocalServer = "https://localhost:7443";

        // wotr-client/.env, beside unity/Wotr.Client.
        public static string FilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.env"));

        public static ServerConfiguration.Addresses Read(string path, Func<string, string> environment)
        {
            var file = File.Exists(path) ? Parse(File.ReadAllText(path)) : new Dictionary<string, string>();
            string Value(string key)
            {
                var set = environment(key);
                return !string.IsNullOrWhiteSpace(set) ? set.Trim() : file.TryGetValue(key, out var value) ? value : "";
            }
            var editor = Value(EditorServerKey);
            return new ServerConfiguration.Addresses
            {
                Player = Value(ServerKey),
                Editor = editor.Length > 0 ? editor : LocalServer,
                Downloads = Value(DownloadsKey)
            };
        }

        // KEY=VALUE lines; blank lines and # comments are skipped, a value may be quoted.
        public static Dictionary<string, string> Parse(string text)
        {
            var values = new Dictionary<string, string>();
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                var equals = line.IndexOf('=');
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || equals <= 0)
                    continue;
                var key = line.Substring(0, equals).Trim();
                if (key.StartsWith("export ", StringComparison.Ordinal))
                    key = key.Substring(7).Trim();
                values[key] = Unquote(line.Substring(equals + 1).Trim());
            }
            return values;
        }

        private static string Unquote(string value) =>
            value.Length >= 2 && (value[0] == '"' && value[value.Length - 1] == '"' || value[0] == '\'' && value[value.Length - 1] == '\'')
                ? value.Substring(1, value.Length - 2) : value;
    }
}
