using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-120: the vector portrait atlas and the four scene strips; unknown characters keep the UI-115 placeholder.
    public sealed class PortraitArtworkTests
    {
        private static readonly string[] Keys =
        {
            "gimli", "boromir", "legolas", "gandalf-grey", "gandalf-white", "strider", "aragorn", "pippin", "merry", "gollum",
            "smeagol", "ring-bearers", "elrond", "galadriel", "balrog", "gothmog", "saruman", "mouth-of-sauron", "witch-king"
        };

        [Test]
        public void PortraitKeysShareOneCellAcrossExpansions()
        {
            Assert.AreEqual("gandalf-grey", PortraitArtwork.Key("base.character.gandalf-grey"));
            Assert.AreEqual("gandalf-grey", PortraitArtwork.Key("lords.character.gandalf-grey"));
            Assert.AreEqual("witch-king", PortraitArtwork.Key("lords.character.witch-king"));
            Assert.AreEqual("", PortraitArtwork.Key(""));
        }

        // ART-04: every character card resolves its own cell: versions of one figure share it, while each sovereign
        // state, each Dark Chieftain and the Dwarven Lord have cells of their own (no more "awakened" for all five).
        [Test]
        public void EveryCharacterCardResolvesItsOwnPortraitKey()
        {
            Assert.AreEqual("brand-awakened", PortraitArtwork.Key("kings.character.brand.awakened"));
            Assert.AreEqual("dain-corrupted", PortraitArtwork.Key("kings.character.dain.corrupted"));
            Assert.AreEqual("gimli-dwarven-lord", PortraitArtwork.Key("fate.character.gimli-dwarven-lord"));
            Assert.AreEqual("gimli", PortraitArtwork.Key("fate.character.gimli"));
            var cards = Newtonsoft.Json.Linq.JObject.Parse(Resources.Load<TextAsset>("character-ability-text").text)["cards"]
                .Select(card => (string)card["id"]).ToList();
            var expansions = cards.Where(id => !id.StartsWith("base.")).ToList();
            CollectionAssert.IsNotEmpty(expansions);
            foreach (var id in expansions)
            {
                var key = PortraitArtwork.Key(id);
                Assert.AreEqual(id.Substring(id.IndexOf(".character.", System.StringComparison.Ordinal) + 11).Replace('.', '-'), key, id);
                var sharing = cards.Where(other => other != id && PortraitArtwork.Key(other) == key).ToList();
                Assert.IsTrue(sharing.All(other => other.EndsWith(".character." + key)), id + " shares its cell only with versions of itself");
            }
            Assert.AreEqual(10, expansions.Where(id => id.StartsWith("kings.character.") && id.Count(c => c == '.') == 3)
                .Select(PortraitArtwork.Key).Distinct().Count(), "Five sovereigns, two states each.");
            // ART-05: Fate's Dwarven Lord has his painted portrait.
            Assert.IsNotNull(PortraitArtwork.Sprite("fate.character.gimli-dwarven-lord"));
        }

        [Test]
        public void AtlasSuppliesEveryNamedCharacterWithDisjointCellsAndUiImportSettings()
        {
            Assert.IsTrue(PortraitArtwork.Available);
            var atlas = Resources.Load<Texture2D>(PortraitArtwork.ResourcePath);
            Assert.AreEqual(1000, atlas.width);
            Assert.AreEqual(800, atlas.height);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas)) as TextureImporter;
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsFalse(importer.isReadable);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapModeU);
            var rects = Keys.Select(key => PortraitArtwork.Sprite("base.character." + key)).ToList();
            Assert.IsTrue(rects.All(sprite => sprite != null), "Every named character has a portrait.");
            // ART-02 base portraits and UI-120 expansion portraits sit in two atlases; cells overlap only within one.
            for (var i = 0; i < rects.Count; i++)
                for (var j = i + 1; j < rects.Count; j++)
                    Assert.IsFalse(rects[i].texture == rects[j].texture && rects[i].rect.Overlaps(rects[j].rect),
                        Keys[i] + " overlaps " + Keys[j]);
            Assert.IsNull(PortraitArtwork.Sprite("base.character.unknown-hero"), "An unknown character keeps the placeholder.");
        }

        [Test]
        public void MedallionShowsThePortraitAndHidesTheInitialOnlyWhenTheAtlasHasTheCharacter()
        {
            var gimli = new CharacterView { Id = "gimli", DefinitionId = "lords.character.gimli", Side = Side.FreePeoples };
            var medallion = new PortraitMedallion(gimli, "Gimli", "Gimli", false);
            Assert.IsTrue(medallion.ClassListContains(PortraitMedallion.IllustratedClass));
            Assert.AreSame(PortraitArtwork.Sprite(gimli.DefinitionId), medallion.Q<Image>("portrait-figure").sprite);
            StringAssert.EndsWith("-gimli", medallion.Q<Image>("portrait-figure").sprite.name);
            var unknown = new CharacterView { Id = "x", DefinitionId = "base.character.unknown-hero", Side = Side.Shadow };
            var fallback = new PortraitMedallion(unknown, "Unknown Hero", "Unknown", false);
            Assert.IsFalse(fallback.ClassListContains(PortraitMedallion.IllustratedClass));
            Assert.IsNotNull(fallback.Q<Image>("portrait-figure").sprite, "The figure silhouette stands in.");
            Assert.AreEqual("U", fallback.Q<Label>("portrait-initial").text);
        }

        [Test]
        public void EveryBattleModeHasARepeatingSceneStrip()
        {
            foreach (BattleMode mode in System.Enum.GetValues(typeof(BattleMode)))
            {
                var texture = BattleSceneArtwork.Texture(mode);
                Assert.IsNotNull(texture, mode.ToString());
                Assert.AreEqual(336, texture.width, mode.ToString());
                Assert.AreEqual(88, texture.height, mode.ToString());
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapModeU, mode + " tiles horizontally");
                Assert.IsFalse(importer.mipmapEnabled);
            }
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Wotr/Resources/Wotr/Art/battle-portraits-v1.json")));
        }
    }
}
