using System.Collections.Generic;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-120: optional painted strips for the four battle modes. Without a texture the scene keeps the
    // vector strip from the UI kit (Styles/Battle.uss). Same size and horizontal repeat as the kit piece.
    public static class BattleSceneArtwork
    {
        public const string ResourceFolder = "Wotr/Art/battle-scenes/";
        private static readonly Texture2D[] textures = new Texture2D[4];
        private static readonly bool[] probed = new bool[4];

        // UI-206: painted battle illustrations by place (BattleSceneArt), shown whole instead of the mode strip.
        public const string IllustrationFolder = "Wotr/Art/battle-illustrations/";
        private static readonly Dictionary<string, Texture2D> illustrations = new Dictionary<string, Texture2D>();

        public static string Key(BattleMode mode) => mode.ToString().ToLowerInvariant();

        public static Texture2D Illustration(string key)
        {
            if (!illustrations.TryGetValue(key, out var texture))
            {
                texture = Resources.Load<Texture2D>(IllustrationFolder + key);
                illustrations[key] = texture;
            }
            return texture;
        }

        public static Texture2D Texture(BattleMode mode)
        {
            var index = (int)mode;
            if (index < 0 || index >= textures.Length)
                return null;
            if (!probed[index])
            {
                probed[index] = true;
                textures[index] = Resources.Load<Texture2D>(ResourceFolder + Key(mode));
            }
            return textures[index];
        }
    }
}
