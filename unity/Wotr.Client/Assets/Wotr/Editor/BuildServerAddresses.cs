using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEditor.Build;
using Wotr.Client;

namespace Wotr.Editor
{
    // MENU-28: a build talks to the hosts named outside git — the environment or wotr-client/.env (see .env.example).
    // They are written next to the build's identity for the duration of one build (Generated, removed afterwards).
    internal static class BuildServerAddresses
    {
        public const string ResourcePath = ClientBuildIdentity.GeneratedFolder + "/Resources/" + ServerConfiguration.Resource + ".json";

        public static void Embed()
        {
            var addresses = ClientEnvironment.Read(ClientEnvironment.FilePath, Environment.GetEnvironmentVariable);
            if (addresses.Player.Length == 0 || addresses.Downloads.Length == 0)
                throw new BuildFailedException($"Set {ClientEnvironment.ServerKey} and {ClientEnvironment.DownloadsKey} in wotr-client/.env " +
                    "(copy .env.example) or in the environment before building.");
            ServerConfiguration.Parse(Serialize(addresses));
            Directory.CreateDirectory(Path.GetDirectoryName(ResourcePath));
            File.WriteAllText(ResourcePath, Serialize(addresses), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ResourcePath, ImportAssetOptions.ForceSynchronousImport);
        }

        // A build started outside BuildTasks would have no addresses; it stops here with the reason.
        public static void RequireEmbedded()
        {
            if (!File.Exists(ResourcePath))
                throw new BuildFailedException("Build through WoTR → Build or BuildTasks so the player carries its server addresses (MENU-28).");
        }

        private static string Serialize(ServerConfiguration.Addresses addresses) => JsonConvert.SerializeObject(addresses,
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented });
    }
}
