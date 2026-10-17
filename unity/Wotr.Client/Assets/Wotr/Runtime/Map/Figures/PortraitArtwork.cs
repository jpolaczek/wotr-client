using UnityEngine;

namespace Wotr.Client
{
    // UI-120: optional portrait atlas for named characters. Without the atlas (or without a character's cell)
    // the battle medallion keeps its figure silhouette and initial from UI-115. Cells are presentation only.
    public static class PortraitArtwork
    {
        // UI-120: every character, the expansions' included.
        public const string ResourcePath = "Wotr/Art/battle-portraits-v1";
        // ART-02: painted portraits of the base characters, preferred where they exist.
        public const string BasePath = "Wotr/Art/battle-portraits-v2";
        private static readonly NamedAtlas Current = new NamedAtlas(BasePath);
        private static readonly NamedAtlas Previous = new NamedAtlas(ResourcePath);

        // "base.character.gandalf-grey" and "lords.character.gandalf-grey" share one portrait.
        public static string Key(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return "";
            var dot = definitionId.LastIndexOf('.');
            return dot < 0 ? definitionId : definitionId.Substring(dot + 1);
        }

        public static bool Available => Current.Available || Previous.Available;

        public static Sprite Sprite(string definitionId)
        {
            var key = Key(definitionId);
            return key.Length == 0 ? null : Current.Sprite(key) ?? Previous.Sprite(key);
        }
    }
}
