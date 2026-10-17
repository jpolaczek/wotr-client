using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Wotr.Client
{
    // MENU-20/MENU-28: the server the game talks to and the origin of its update packages. Nobody types them in the
    // game and git never holds them: the editor reads the environment and wotr-client/.env (ClientEnvironment); a build
    // carries them in Resources/Wotr/server-config.json, written for the duration of the build from the same source.
    // A test points the client at its own server with Use.
    public static class ServerConfiguration
    {
        public const string Resource = "Wotr/server-config";
        private static Addresses file;
        private static string chosen;

        public static string Address => chosen ?? For(Application.isEditor);

        // The https origin update packages may come from, ending with /; empty when the editor has none set.
        public static string Downloads => WithSlash(Current.Downloads);

        public static string For(bool editor) => editor ? Current.Editor : Current.Player;

        // A test's own server, or null to return to the configured one.
        public static void Use(string address) => chosen = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        public static Addresses Parse(string json)
        {
            var addresses = JsonConvert.DeserializeObject<Addresses>(json);
            if (addresses == null || string.IsNullOrWhiteSpace(addresses.Player) || string.IsNullOrWhiteSpace(addresses.Downloads))
                throw new InvalidOperationException(Resource + ".json needs the server and the downloads address.");
            if (string.IsNullOrWhiteSpace(addresses.Editor))
                addresses.Editor = ClientEnvironment.LocalServer;
            return addresses;
        }

        private static Addresses Current => file ??= Load();

        private static Addresses Load()
        {
#if UNITY_EDITOR
            return ClientEnvironment.Read(ClientEnvironment.FilePath, Environment.GetEnvironmentVariable);
#else
            var asset = Resources.Load<TextAsset>(Resource);
            if (asset == null)
                throw new InvalidOperationException("The build has no " + Resource + ".json.");
            return Parse(asset.text);
#endif
        }

        private static string WithSlash(string address) =>
            string.IsNullOrWhiteSpace(address) ? "" : address.Trim().TrimEnd('/') + "/";

        public sealed class Addresses
        {
            public string Player { get; set; } = "";
            public string Editor { get; set; } = "";
            public string Downloads { get; set; } = "";
        }
    }
}
