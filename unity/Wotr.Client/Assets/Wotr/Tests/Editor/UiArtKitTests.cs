using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Wotr.EditorTests
{
    // Contract of graphicalassets/ui (assets.json): every rendered piece the stylesheets reference.
    public sealed class UiArtKitTests
    {
        private const string ArtRoot = "Wotr/Art/UI/";

        // Logical sizes in points from the SVG sources; textures are rendered at twice that size.
        private static readonly (string Key, int Width, int Height, bool Alpha)[] Artwork =
        {
            ("surfaces/topbar-plate", 168, 56, false), ("surfaces/cast-shadow", 16, 8, true),
            ("frames/topbar-frame", 192, 44, true), ("frames/cartouche", 64, 44, true),
            ("frames/pennant-free", 22, 28, true), ("frames/pennant-free-hover", 22, 28, true),
            ("frames/pennant-shadow", 22, 28, true), ("frames/pennant-shadow-hover", 22, 28, true), ("ornaments/wax-seal", 22, 22, true),
            ("ornaments/move-glow", 44, 44, true),
            ("frames/button", 96, 40, true), ("frames/button-hover", 96, 40, true), ("frames/button-selected", 96, 40, true),
            ("frames/button-pressed", 96, 40, true), ("frames/button-square-pressed", 28, 28, true),
            ("frames/glow", 48, 48, true),
            ("characters/level", 16, 16, true), ("characters/movement", 16, 16, true), ("characters/flight", 16, 16, true),
            ("characters/combat", 16, 16, true), ("characters/ability", 16, 16, true), ("characters/free-peoples", 16, 16, true),
            ("characters/on-board", 16, 16, true), ("characters/waiting", 16, 16, true), ("characters/removed", 16, 16, true),
            ("characters/may-return", 16, 16, true),
            ("mordor/slab", 24, 18, true), ("mordor/slab-passed", 24, 18, true), ("mordor/slab-next", 24, 18, true),
            ("mordor/crack-of-doom", 34, 28, true), ("mordor/crack-of-doom-next", 34, 28, true),
            ("mordor/gate", 16, 16, true), ("mordor/gate-next", 16, 16, true),
            ("hunt/tile-standard", 28, 28, true), ("hunt/tile-special-fp", 28, 28, true), ("hunt/tile-special-sh", 28, 28, true),
            ("hunt/tile-smeagol", 28, 28, true), ("hunt/eye", 16, 16, true), ("hunt/die", 16, 16, true), ("hunt/smeagol", 16, 16, true),
            ("hunt/reveal", 10, 10, true), ("hunt/stop", 10, 10, true), ("hunt/kings", 10, 10, true), ("actions/hunt-pool", 20, 20, true),
            ("hunt/stage-tile", 16, 16, true), ("hunt/stage-damage", 16, 16, true), ("hunt/stage-result", 16, 16, true),
            ("hunt/stage-step", 16, 16, true), ("hunt/scene-hunt", 168, 44, false), ("hunt/scene-mordor", 168, 44, false),
            ("hunt/choice-corruption", 32, 32, true), ("hunt/choice-random", 32, 32, true),
            ("console/frame", 96, 96, false), ("console/status-plaque-free", 120, 30, true), ("console/status-plaque-shadow", 120, 30, true),
            ("console/hunt-box", 64, 40, true), ("console/hunt-box-eligible", 64, 40, true),
            ("console/picker", 64, 44, true), ("console/picker-tail", 18, 9, true),
            ("rail/edge-tab", 94, 66, true), ("rail/edge-tab-hover", 94, 66, true), ("rail/edge-tab-selected", 94, 66, true),
            ("rail/edge-tab-pressed", 94, 66, true), ("rail/edge-tab-available", 94, 66, true),
            ("rail/edge-tab-available-hover", 94, 66, true),
            // UI-263: the waiting room's rules strip and the expansions' crests.
            ("rules/medallion", 56, 56, true), ("rules/medallion-hover", 56, 56, true), ("rules/medallion-pressed", 56, 56, true),
            ("rules/medallion-fate-on", 56, 56, true), ("rules/medallion-lords-on", 56, 56, true),
            ("rules/medallion-kings-on", 56, 56, true), ("rules/crest-fate", 32, 32, true), ("rules/crest-lords", 32, 32, true),
            ("rules/crest-kings", 32, 32, true), ("rules/check-off", 14, 14, true), ("rules/check-on", 14, 14, true),
            ("console/ribbon-free", 300, 40, true), ("console/ribbon-shadow", 300, 40, true), ("console/status-glow", 48, 48, true),
            ("end/title-victory", 300, 56, true), ("end/title-defeat", 300, 56, true),
            ("actions/back", 20, 20, true), ("actions/battle", 20, 20, true), ("actions/close", 20, 20, true),
            ("actions/collapse", 20, 20, true), ("actions/discard", 20, 20, true), ("actions/pass", 20, 20, true),
            ("actions/confirm", 20, 20, true), ("actions/deselect-all", 20, 20, true), ("actions/expand", 20, 20, true),
            ("actions/help", 20, 20, true), ("actions/chat", 20, 20, true), ("actions/locate", 20, 20, true), ("actions/next", 20, 20, true),
            ("actions/pin", 20, 20, true), ("actions/pinned", 20, 20, true), ("actions/political", 20, 20, true), ("actions/refresh", 20, 20, true),
            ("actions/reinforcements", 20, 20, true), ("actions/select-all", 20, 20, true), ("actions/why", 20, 20, true),
            ("actions/skip", 20, 20, true),
            ("frames/panel", 256, 256, true), ("frames/panel-header", 120, 32, true),
            ("frames/button-square", 28, 28, true), ("frames/button-square-hover", 28, 28, true),
            ("frames/entry", 96, 40, true), ("frames/entry-hover", 96, 40, true),
            ("frames/entry-shadow", 96, 40, true), ("frames/entry-shadow-hover", 96, 40, true),
            ("ornaments/divider", 12, 44, true), ("ornaments/divider-small", 8, 30, true),
            ("dice/medallion", 40, 40, true), ("dice/medallion-hover", 40, 40, true), ("dice/medallion-pressed", 40, 40, true),
            ("dice/medallion-free-pressed", 40, 40, true), ("dice/medallion-shadow-pressed", 40, 40, true),
            ("dice/medallion-free", 40, 40, true), ("dice/medallion-free-hover", 40, 40, true), ("dice/medallion-free-selected", 40, 40, true),
            ("dice/medallion-shadow", 40, 40, true), ("dice/medallion-shadow-hover", 40, 40, true), ("dice/medallion-shadow-selected", 40, 40, true),
            ("dice/faces/character", 24, 24, true), ("dice/faces/army", 24, 24, true), ("dice/faces/muster", 24, 24, true),
            ("dice/faces/army-muster", 24, 24, true), ("dice/faces/event", 24, 24, true), ("dice/faces/will", 24, 24, true),
            ("dice/faces/eye", 24, 24, true), ("dice/faces/card-draw", 24, 24, true), ("dice/faces/use-balrog", 24, 24, true),
            ("dice/tile", 44, 36, true), ("dice/tile-hover", 44, 36, true),
            ("dice/special-vilya", 40, 40, true), ("dice/special-vilya-hover", 40, 40, true), ("dice/special-vilya-lit", 40, 40, true),
            ("dice/special-vilya-pressed", 40, 40, true), ("dice/origins/vilya", 16, 16, true), ("dice/ruler-crown", 18, 12, true),
            ("kings/attempt", 22, 22, true), ("kings/status-initial", 12, 12, true), ("kings/status-awakened", 12, 12, true),
            ("kings/status-corrupted", 12, 12, true), ("kings/status-removed", 12, 12, true), ("kings/tile-back", 18, 18, true),
            ("kings/marker-corrupted", 26, 26, true),
            ("dice/special-nenya", 40, 40, true), ("dice/special-nenya-hover", 40, 40, true), ("dice/special-nenya-lit", 40, 40, true),
            ("dice/special-nenya-pressed", 40, 40, true), ("dice/origins/nenya", 16, 16, true),
            ("dice/special-narya", 40, 40, true), ("dice/special-narya-hover", 40, 40, true), ("dice/special-narya-lit", 40, 40, true),
            ("dice/special-narya-pressed", 40, 40, true), ("dice/origins/narya", 16, 16, true),
            ("dice/special-balrog", 40, 40, true), ("dice/special-balrog-hover", 40, 40, true), ("dice/special-balrog-lit", 40, 40, true),
            ("dice/special-balrog-pressed", 40, 40, true), ("dice/origins/balrog", 16, 16, true),
            ("dice/special-gothmog", 40, 40, true), ("dice/special-gothmog-hover", 40, 40, true), ("dice/special-gothmog-lit", 40, 40, true),
            ("dice/special-gothmog-pressed", 40, 40, true), ("dice/origins/gothmog", 16, 16, true),
            ("dice/remove-star", 14, 14, true),
            ("console/ring-vilya", 32, 32, true), ("console/ring-vilya-keeper-lit", 32, 32, true),
            ("console/ring-vilya-keeper-captured", 32, 32, true), ("console/ring-nenya", 32, 32, true),
            ("console/ring-nenya-keeper-lit", 32, 32, true), ("console/ring-nenya-keeper-captured", 32, 32, true),
            ("console/ring-narya", 32, 32, true), ("console/ring-narya-keeper-lit", 32, 32, true),
            ("console/ring-narya-keeper-captured", 32, 32, true),
            ("fellowship/versions", 18, 18, true), ("fellowship/guide-crown", 20, 18, true),
            ("fellowship/guide-crown-crown-on", 20, 18, true), ("fellowship/guide-crown-crown-hover", 20, 18, true),
            ("fellowship/alternate", 18, 18, true), ("icons/fellowship-box", 17, 17, true),
            ("tokens/council-politics-council", 32, 32, true), ("tokens/council-politics-council-lit", 32, 32, true),
            ("tokens/council-politics-council-hover", 32, 32, true), ("tokens/council-politics-council-spent", 32, 32, true),
            ("tokens/council-travel-council", 32, 32, true), ("tokens/council-travel-council-lit", 32, 32, true),
            ("tokens/council-travel-council-hover", 32, 32, true), ("tokens/council-travel-council-spent", 32, 32, true),
            ("tokens/politics-token-free", 28, 28, true), ("tokens/politics-token-shadow", 28, 28, true),
            ("icons/cards", 26, 26, true), ("icons/ring", 26, 26, true), ("icons/ring-captured", 26, 26, true), ("icons/spent", 26, 26, true),
            ("icons/hunt", 26, 26, true), ("icons/out", 26, 26, true), ("icons/log", 20, 20, true),
            ("icons/politics", 28, 28, true), ("icons/effects", 28, 28, true), ("icons/characters", 28, 28, true),
            ("icons/fellowship", 28, 28, true),
            ("cards/parchment", 96, 96, true), ("cards/parchment-playable", 96, 96, true), ("cards/parchment-armed", 96, 96, true), ("cards/parchment-discard", 96, 96, true),
            ("cards/back", 96, 96, true), ("cards/back-hover", 96, 96, true),
            ("icons/nations/dwarves", 20, 20, true), ("icons/nations/elves", 20, 20, true),
            ("icons/nations/gondor", 20, 20, true), ("icons/nations/north", 20, 20, true),
            ("icons/nations/rohan", 20, 20, true), ("icons/nations/isengard", 20, 20, true),
            ("icons/nations/sauron", 20, 20, true), ("icons/nations/southrons", 20, 20, true),
            // UI-115: the battle window.
            ("battle/scene-field", 168, 44, false), ("battle/scene-siege", 168, 44, false),
            ("battle/scene-sortie", 168, 44, false), ("battle/scene-relief", 168, 44, false),
            ("battle/force-frame-free", 96, 96, true), ("battle/force-frame-shadow", 96, 96, true),
            ("battle/portrait-medallion-free", 36, 36, true), ("battle/portrait-medallion-shadow", 36, 36, true),
            ("battle/portrait-medallion-free-cancelled", 36, 36, true), ("battle/portrait-medallion-shadow-cancelled", 36, 36, true),
            ("battle/card-slot", 96, 36, true), ("battle/garrison-shelf", 96, 28, true),
            ("battle/strength", 20, 20, true), ("battle/leadership", 20, 20, true), ("battle/dice", 20, 20, true),
            ("battle/hit", 20, 20, true), ("battle/garrison", 20, 20, true),
            ("battle/stage-ribbon", 120, 22, true), ("battle/stage-marker", 24, 24, true),
            ("battle/stage-cards", 16, 16, true), ("battle/stage-roll", 16, 16, true), ("battle/stage-reroll", 16, 16, true),
            ("battle/stage-losses", 16, 16, true), ("battle/stage-next", 16, 16, true),
            ("battle/no-card", 20, 20, true), ("battle/skip-cards", 20, 20, true),
            ("battle/reroll-arrow", 10, 14, true),
            ("battle/loss-downgrade", 18, 18, true), ("battle/loss-remove", 18, 18, true),
            ("battle/hit-seal", 16, 16, true), ("battle/hit-seal-spent", 16, 16, true),
            ("battle/continue", 20, 20, true), ("battle/stop", 20, 20, true), ("battle/hold", 20, 20, true),
            ("battle/retreat", 20, 20, true), ("battle/advance", 20, 20, true), ("battle/extend-siege", 20, 20, true), ("battle/evil-knowledge", 20, 20, true),
            ("battle/round-tab", 96, 24, true),
            ("battle/die-1", 22, 22, true), ("battle/die-1-hit", 22, 22, true), ("battle/die-1-rerolled", 22, 22, true),
            ("battle/die-1-die-selected", 22, 22, true), ("battle/die-1-die-pressed", 22, 22, true),
            ("battle/die-2", 22, 22, true), ("battle/die-2-hit", 22, 22, true), ("battle/die-2-rerolled", 22, 22, true),
            ("battle/die-2-die-selected", 22, 22, true), ("battle/die-2-die-pressed", 22, 22, true),
            ("battle/die-3", 22, 22, true), ("battle/die-3-hit", 22, 22, true), ("battle/die-3-rerolled", 22, 22, true),
            ("battle/die-3-die-selected", 22, 22, true), ("battle/die-3-die-pressed", 22, 22, true),
            ("battle/die-4", 22, 22, true), ("battle/die-4-hit", 22, 22, true), ("battle/die-4-rerolled", 22, 22, true),
            ("battle/die-4-die-selected", 22, 22, true), ("battle/die-4-die-pressed", 22, 22, true),
            ("battle/die-5", 22, 22, true), ("battle/die-5-hit", 22, 22, true), ("battle/die-5-rerolled", 22, 22, true),
            ("battle/die-5-die-selected", 22, 22, true), ("battle/die-5-die-pressed", 22, 22, true),
            ("battle/die-6", 22, 22, true), ("battle/die-6-hit", 22, 22, true), ("battle/die-6-rerolled", 22, 22, true),
            ("battle/die-6-die-selected", 22, 22, true), ("battle/die-6-die-pressed", 22, 22, true),
            // UI-125: army bases and tokens on the map, reserve slots and budget points of the Reinforcements panel.
            ("armies/base-field-free", 76, 22, true), ("armies/base-field-shadow", 76, 22, true),
            ("armies/base-field-free-selected", 76, 22, true), ("armies/base-field-shadow-selected", 76, 22, true),
            ("armies/base-field-free-eligible", 76, 22, true), ("armies/base-field-shadow-eligible", 76, 22, true),
            ("armies/base-field-free-pressed", 76, 22, true), ("armies/base-field-shadow-pressed", 76, 22, true),
            ("armies/base-field-free-moved", 76, 22, true), ("armies/base-field-shadow-moved", 76, 22, true),
            ("armies/base-garrison-free", 76, 28, true), ("armies/base-garrison-shadow", 76, 28, true),
            ("armies/base-garrison-free-selected", 76, 28, true), ("armies/base-garrison-shadow-selected", 76, 28, true),
            ("armies/base-garrison-free-eligible", 76, 28, true), ("armies/base-garrison-shadow-eligible", 76, 28, true),
            ("armies/base-garrison-free-pressed", 76, 28, true), ("armies/base-garrison-shadow-pressed", 76, 28, true),
            ("armies/base-garrison-free-moved", 76, 28, true), ("armies/base-garrison-shadow-moved", 76, 28, true),
            ("armies/token-field-free", 30, 30, true), ("armies/token-field-shadow", 30, 30, true),
            ("armies/token-field-free-selected", 30, 30, true), ("armies/token-field-shadow-selected", 30, 30, true),
            ("armies/token-field-free-eligible", 30, 30, true), ("armies/token-field-shadow-eligible", 30, 30, true),
            ("armies/token-field-free-pressed", 30, 30, true), ("armies/token-field-shadow-pressed", 30, 30, true),
            ("armies/token-field-free-moved", 30, 30, true), ("armies/token-field-shadow-moved", 30, 30, true),
            ("armies/token-garrison-free", 30, 34, true), ("armies/token-garrison-shadow", 30, 34, true),
            ("armies/token-garrison-free-selected", 30, 34, true), ("armies/token-garrison-shadow-selected", 30, 34, true),
            ("armies/token-garrison-free-eligible", 30, 34, true), ("armies/token-garrison-shadow-eligible", 30, 34, true),
            ("armies/token-garrison-free-pressed", 30, 34, true), ("armies/token-garrison-shadow-pressed", 30, 34, true),
            ("armies/token-garrison-free-moved", 30, 34, true), ("armies/token-garrison-shadow-moved", 30, 34, true),
            ("armies/moved-check", 14, 14, true), ("armies/ghost-outline", 48, 48, true),
            ("armies/reserve-slot", 48, 48, true), ("armies/reserve-slot-selected", 48, 48, true), ("armies/reserve-slot-pressed", 48, 48, true),
            ("armies/budget-slot", 18, 18, true), ("armies/budget-slot-filled", 18, 18, true)
        };

        // Surfaces that tile horizontally.
        private static readonly string[] Repeating = { "surfaces/topbar-plate", "battle/scene-field", "battle/scene-siege", "battle/scene-sortie", "battle/scene-relief",
            "hunt/scene-hunt", "hunt/scene-mordor" };

        [Test]
        public void ArtworkIsRenderedAtTwiceItsLogicalSizeWithUiImportSettings()
        {
            foreach (var art in Artwork)
            {
                var texture = Resources.Load<Texture2D>(ArtRoot + art.Key);
                Assert.IsNotNull(texture, art.Key);
                Assert.AreEqual(art.Width * 2, texture.width, art.Key);
                Assert.AreEqual(art.Height * 2, texture.height, art.Key);
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, art.Key);
                Assert.IsFalse(importer.mipmapEnabled, art.Key);
                Assert.IsFalse(importer.isReadable, art.Key);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, art.Key);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, art.Key);
                Assert.AreEqual(Repeating.Contains(art.Key) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                    importer.wrapModeU, art.Key);
                Assert.AreEqual(art.Alpha, importer.DoesSourceTextureHaveAlpha(), art.Key);
                // The cast shadow is a translucent gradient without opaque or fully clear texels.
                if (art.Alpha && art.Key != "surfaces/cast-shadow")
                    CheckTransparentEdgeAndVisibleArt(path, art.Key);
            }
        }

        [Test]
        public void EveryTextureInTheKitIsCoveredByTheContract()
        {
            var listed = Artwork.Select(art => "Assets/Wotr/Resources/" + ArtRoot + art.Key + ".png").ToHashSet();
            var present = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Wotr/Resources/Wotr/Art/UI" })
                .Select(AssetDatabase.GUIDToAssetPath).ToList();
            CollectionAssert.IsEmpty(present.Where(path => !listed.Contains(path)), "Unlisted kit textures");
            Assert.AreEqual(listed.Count, present.Count);
        }

        private static void CheckTransparentEdgeAndVisibleArt(string path, string key)
        {
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(path)), key);
                var pixels = decoded.GetPixels32();
                Assert.Greater(pixels.Count(pixel => pixel.a == 0), 0, key + " has transparent pixels");
                Assert.Greater(pixels.Count(pixel => pixel.a >= 200), 0, key + " has visible artwork");
            }
            finally { Object.DestroyImmediate(decoded); }
        }
    }
}
