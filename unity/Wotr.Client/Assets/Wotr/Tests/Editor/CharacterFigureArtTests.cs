using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // ART-02: every base character has its own figure and painted portrait; expansion characters keep UI-120's.
    public sealed class CharacterFigureArtTests
    {
        private static readonly string[] BaseCharacters =
        {
            "gandalf-grey", "strider", "boromir", "legolas", "gimli", "merry", "pippin", "gandalf-white", "aragorn",
            "gollum", "ring-bearers", "saruman", "mouth-of-sauron", "witch-king"
        };

        [Test]
        public void EveryBaseCharacterHasItsOwnFigure()
        {
            // ART-05: the expansions' own characters join the atlas as their art is generated (Fate's Dwarven Lord now).
            CollectionAssert.AreEquivalent(BaseCharacters.Append("gimli-dwarven-lord"), FigureArtwork.Characters.Names);
            Assert.AreEqual("gimli-dwarven-lord", FigureAppearances.Character("fate.character.gimli-dwarven-lord", Side.FreePeoples).Key);
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
        public void BaseCharactersHavePaintedPortraitsAndExpansionsKeepTheirOwn()
        {
            var painted = Resources.Load<Texture2D>(PortraitArtwork.BasePath);
            Assert.IsNotNull(painted);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(painted)) as TextureImporter;
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsFalse(importer.isReadable);
            foreach (var key in BaseCharacters)
                Assert.AreSame(painted, PortraitArtwork.Sprite("base.character." + key).texture, key);
            Assert.AreSame(painted, PortraitArtwork.Sprite("lords.character.gimli").texture, "Versions share the portrait.");
            Assert.AreNotSame(painted, PortraitArtwork.Sprite("base.character.galadriel").texture, "Expansions keep UI-120.");
        }
    }
}
