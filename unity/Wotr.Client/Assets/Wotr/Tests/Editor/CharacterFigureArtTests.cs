using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // ART-02: every base character has its own figure and painted portrait; expansion characters keep UI-120's until
    // their art is generated (ART-05, ART-06).
    public sealed class CharacterFigureArtTests
    {
        private static readonly string[] BaseCharacters =
        {
            "gandalf-grey", "strider", "boromir", "legolas", "gimli", "merry", "pippin", "gandalf-white", "aragorn",
            "gollum", "ring-bearers", "saruman", "mouth-of-sauron", "witch-king"
        };

        // ART-06: the Lords of Middle-earth characters with their own art (the Mouth's and the Witch-king's versions keep
        // the base figures, as every version does).
        private static readonly string[] LordsCharacters = { "elrond", "galadriel", "balrog", "gothmog", "smeagol" };

        [Test]
        public void EveryBaseCharacterHasItsOwnFigure()
        {
            // ART-05/ART-06: the expansions' own characters join the atlas as their art is generated (Fate's Dwarven
            // Lord, the Lords characters).
            CollectionAssert.AreEquivalent(BaseCharacters.Append("gimli-dwarven-lord").Concat(LordsCharacters), FigureArtwork.Characters.Names);
            Assert.AreEqual("gimli-dwarven-lord", FigureAppearances.Character("fate.character.gimli-dwarven-lord", Side.FreePeoples).Key);
            foreach (var key in LordsCharacters)
            {
                var side = key is "balrog" or "gothmog" ? Side.Shadow : Side.FreePeoples;
                var art = FigureAppearances.Character("lords.character." + key, side);
                Assert.AreEqual(key, art.Key);
                Assert.AreSame(FigureArtwork.Characters.Sprite(key), FigureArtwork.Sprite(art), key);
            }
            foreach (var key in BaseCharacters)
            {
                var side = key is "saruman" or "mouth-of-sauron" or "witch-king" ? Side.Shadow : Side.FreePeoples;
                var art = FigureAppearances.Character("base.character." + key, side);
                Assert.AreEqual(key, art.Key);
                Assert.AreSame(FigureArtwork.Characters.Sprite(key), FigureArtwork.Sprite(art), key);
            }
            Assert.AreSame(FigureArtwork.Characters.Sprite("ring-bearers"), FigureArtwork.Sprite(FigureAppearances.Fellowship),
                "The Fellowship marker shows the Ring-bearers.");
        }

        [Test]
        public void BaseAndLordsCharactersHavePaintedPortraits()
        {
            var painted = Resources.Load<Texture2D>(PortraitArtwork.BasePath);
            Assert.IsNotNull(painted);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(painted)) as TextureImporter;
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsFalse(importer.isReadable);
            foreach (var key in BaseCharacters)
                Assert.AreSame(painted, PortraitArtwork.Sprite("base.character." + key).texture, key);
            Assert.AreSame(painted, PortraitArtwork.Sprite("lords.character.gimli").texture, "Versions share the portrait.");
            foreach (var key in LordsCharacters)
                Assert.AreSame(painted, PortraitArtwork.Sprite("lords.character." + key).texture, key + " has its painted portrait.");
            // Every character with a UI-120 portrait is painted now; one whose art is not generated yet has none.
            Assert.IsNull(PortraitArtwork.Sprite("kings.character.black-serpent"));
        }
    }
}
