using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class FellowshipArtworkTests
    {
        [Test]
        public void NamedSpritesAreCachedDisjointAndContainOpaqueSymbolsWithTransparentSurrounds()
        {
            Assert.AreEqual(8, FellowshipArtwork.Keys.Count);
            var cells = FellowshipArtwork.Keys.Select(FellowshipArtwork.NormalizedCell).ToArray();
            var sprites = FellowshipArtwork.Keys.Select(FellowshipArtwork.Sprite).ToArray();
            for (var i = 0; i < cells.Length; i++)
            {
                Assert.IsNotNull(sprites[i], FellowshipArtwork.Keys[i]);
                Assert.AreSame(sprites[i], FellowshipArtwork.Sprite(FellowshipArtwork.Keys[i]));
                Assert.Greater(sprites[i].rect.width, 32);
                Assert.Greater(sprites[i].rect.height, 32);
                Assert.GreaterOrEqual(cells[i].xMin, 0);
                Assert.GreaterOrEqual(cells[i].yMin, 0);
                Assert.LessOrEqual(cells[i].xMax, 1);
                Assert.LessOrEqual(cells[i].yMax, 1);
                for (var j = i + 1; j < cells.Length; j++)
                    Assert.IsFalse(cells[i].Overlaps(cells[j]), FellowshipArtwork.Keys[i] + " / " + FellowshipArtwork.Keys[j]);
            }
            var path = AssetDatabase.GetAssetPath(sprites[0].texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.isReadable);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            CheckPixels(path, sprites);
        }

        private static void CheckPixels(string path, Sprite[] sprites)
        {
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(decoded.LoadImage(File.ReadAllBytes(path)));
                var pixels = decoded.GetPixels32();
                foreach (var sprite in sprites)
                {
                    var bounds = sprite.rect;
                    var transparent = 0;
                    var opaque = 0;
                    for (var y = (int)bounds.yMin; y < (int)bounds.yMax; y++)
                        for (var x = (int)bounds.xMin; x < (int)bounds.xMax; x++)
                        {
                            var alpha = pixels[y * decoded.width + x].a;
                            if (alpha == 0) transparent++;
                            if (alpha >= 200) opaque++;
                        }
                    Assert.Greater(transparent, 100, sprite.name + " transparent surround");
                    Assert.Greater(opaque, 100, sprite.name + " visible symbol");
                }
            }
            finally { Object.DestroyImmediate(decoded); }
        }
    }
}
