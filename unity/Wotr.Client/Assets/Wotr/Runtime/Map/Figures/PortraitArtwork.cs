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

        private const string CharacterMarker = ".character.";

        // ART-04: a character's cell is its definition id after "<module>.character.", dots as dashes. Versions of one
        // figure share it ("base.character.gandalf-grey" and "lords.character.gandalf-grey" → "gandalf-grey"), and
        // each sovereign state has its own ("kings.character.brand.awakened" → "brand-awakened"; before ART-04 every
        // state collided on "awakened"). Other ids keep their last segment.
        public static string Key(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return "";
            var marker = definitionId.IndexOf(CharacterMarker, System.StringComparison.Ordinal);
            if (marker >= 0)
                return definitionId.Substring(marker + CharacterMarker.Length).Replace('.', '-');
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
