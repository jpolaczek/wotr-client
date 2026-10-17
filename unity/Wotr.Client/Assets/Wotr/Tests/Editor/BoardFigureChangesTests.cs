using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class BoardFigureChangesTests
    {
        private static bool Region(string id) => id is "minas-tirith" or "osgiliath" or "pelargir" or "rivendell" or "hollin";

        [Test]
        public void SnapshotCopiesPublicPhysicalIdsAndIgnoresPrivateVersionsAndReservePieces()
        {
            var view = View();
            view.Units.Add(Unit("same-id", "reserve"));
            view.Characters.Add(Character("same-id", "rivendell"));
            view.OwnCompanions.Add(Character("private", "rivendell"));
            view.OwnCompanionVersions.Add("lords.character.gimli");
            var snapshot = new BoardFigureSnapshot(view, Region);
            view.Units[0].Location = "osgiliath";
            Assert.AreEqual("minas-tirith", snapshot.Figures["unit:first"].Region);
            Assert.IsTrue(snapshot.Figures.ContainsKey("character:same-id"));
            Assert.IsFalse(snapshot.Figures.ContainsKey("unit:same-id"));
            Assert.IsFalse(snapshot.Figures.ContainsKey("character:private"));
            Assert.AreEqual(3, snapshot.Figures.Count, "One public unit, one public character, one public fellowship marker.");
        }

        [Test]
        public void SplitAndMergeTrackIndividualIdsAndNeverMoveTheArmyLeftBehind()
        {
            var view = View();
            view.Units.Add(Unit("stays", "minas-tirith"));
            view.Units.Add(Unit("merges", "pelargir"));
            var changes = Online(view);
            view.Revision++;
            view.Units[0].Location = "osgiliath";
            var split = changes.Apply(view, Region);
            Assert.AreEqual(1, split.Count);
            Assert.AreEqual("unit:first", split[0].To.Key);
            Assert.AreEqual("minas-tirith", split[0].From.Region);
            view.Revision++;
            view.Units[2].Location = "osgiliath";
            var merge = changes.Apply(view, Region);
            Assert.AreEqual(1, merge.Count);
            Assert.AreEqual("unit:merges", merge[0].To.Key);
            Assert.AreEqual("pelargir", merge[0].From.Region);
            Assert.AreEqual("osgiliath", merge[0].To.Region);
        }

        [Test]
        public void FirstDuplicateStaleGapDifferentViewerAndNewMatchDoNotReplay()
        {
            var view = View();
            var changes = Online(view);
            view.Units[0].Location = "osgiliath";
            Assert.IsEmpty(changes.Apply(view, Region), "Duplicate revision does not animate.");
            view.Revision = -1;
            Assert.IsEmpty(changes.Apply(view, Region));
            Assert.AreEqual(0, changes.Current.Revision, "Old responses cannot replace the baseline.");
            view.Revision = 4;
            Assert.IsEmpty(changes.Apply(view, Region), "Missing revisions are not reconstructed.");
            view.Revision++;
            view.You = Side.Shadow;
            view.Units[0].Location = "pelargir";
            Assert.IsEmpty(changes.Apply(view, Region));
            view.Revision++;
            view.MatchId = "another-match";
            view.Units[0].Location = "minas-tirith";
            Assert.IsEmpty(changes.Apply(view, Region));
        }

        [Test]
        public void DisconnectResetsBaselineAndLaterLiveChangesResume()
        {
            var view = View();
            var changes = Online(view);
            changes.SetConnected(false);
            view.Revision++;
            view.Units[0].Location = "osgiliath";
            Assert.IsEmpty(changes.Apply(view, Region));
            changes.SetConnected(true);
            Assert.IsEmpty(changes.Apply(view, Region));
            view.Revision++;
            view.Units[0].Location = "pelargir";
            Assert.AreEqual(1, changes.Apply(view, Region).Count);
            changes.Reset();
            view.Revision++;
            Assert.IsEmpty(changes.Apply(view, Region));
        }

        [Test]
        public void HiddenProgressAndMordorDoNotInventRegionalTravel()
        {
            var view = View();
            var changes = Online(view);
            view.Revision++;
            view.Fellowship.Progress = 3;
            Assert.IsEmpty(changes.Apply(view, Region));
            view.Revision++;
            view.Fellowship.Hidden = false;
            Assert.IsEmpty(changes.Apply(view, Region));
            view.Revision++;
            view.Fellowship.MordorStep = 0;
            Assert.IsEmpty(changes.Apply(view, Region));
            Assert.IsFalse(changes.Current.Figures.ContainsKey("fellowship"));
        }

        [TestCase(0, false)]
        [TestCase(2, true)]
        public void SeparationUsesPublicIdentityAndOnlyAKnownOrigin(int progress, bool arrivalOnly)
        {
            var view = View();
            view.Fellowship.Progress = progress;
            view.Fellowship.CompanionIdentities.Add("gimli");
            var changes = Online(view);
            view.Revision++;
            view.Fellowship.CompanionIdentities.Clear();
            view.Characters.Add(Character("gimli-physical", "hollin"));
            var moved = changes.Apply(view, Region).Single();
            Assert.AreEqual(arrivalOnly, moved.IsArrival);
            Assert.AreEqual(arrivalOnly ? "hollin" : "rivendell", moved.From.Region);
            Assert.AreEqual("hollin", moved.To.Region);
        }

        [Test]
        public void TransformationKeepsThePhysicalIdAndOnlyLaterLocationChangesTravel()
        {
            var view = View();
            var character = Character("wizard", "rivendell");
            character.DefinitionId = "base.character.gandalf-grey";
            view.Characters.Add(character);
            var changes = Online(view);
            view.Revision++;
            character.DefinitionId = "base.character.gandalf-white";
            Assert.IsEmpty(changes.Apply(view, Region));
            view.Revision++;
            character.Location = "hollin";
            var moved = changes.Apply(view, Region).Single();
            Assert.AreEqual("character:wizard", moved.To.Key);
            Assert.AreEqual(FigureAppearance.Wizard, moved.To.Appearance.Fallback);
            Assert.AreEqual("gandalf-white", moved.To.Appearance.Key, "ART-02: the figure follows the current definition.");
        }

        [Test]
        public void AllAtlasSpritesHaveDistinctNonOverlappingCellsAndATransparentTexture()
        {
            var cells = System.Enum.GetValues(typeof(FigureAppearance)).Cast<FigureAppearance>()
                .Select(FigureArtwork.NormalizedCell).ToArray();
            Assert.AreEqual(12, cells.Length);
            for (var i = 0; i < cells.Length; i++)
            {
                Assert.IsTrue(new Rect(0, 0, 1, 1).Contains(cells[i].center));
                for (var j = i + 1; j < cells.Length; j++)
                    Assert.IsFalse(cells[i].Overlaps(cells[j]), $"Atlas slots {i} and {j} overlap.");
                var sprite = FigureArtwork.Sprite((FigureAppearance)i);
                Assert.IsNotNull(sprite);
                Assert.Greater(sprite.rect.width, 32);
                Assert.Greater(sprite.rect.height, 32);
            }
            var texture = FigureArtwork.Sprite(FigureAppearance.FreeRegular).texture;
            var importer = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(texture)) as UnityEditor.TextureImporter;
            Assert.IsNotNull(importer);
            Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.isReadable, "The runtime atlas needs no CPU copy.");
            Assert.IsFalse(importer.mipmapEnabled);
            AssertPixelsHaveTransparentBackgroundAndVisibleFigures(UnityEditor.AssetDatabase.GetAssetPath(texture), cells);
        }

        private static void AssertPixelsHaveTransparentBackgroundAndVisibleFigures(string path, Rect[] cells)
        {
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(decoded.LoadImage(System.IO.File.ReadAllBytes(path)));
                var pixels = decoded.GetPixels32();
                Assert.IsTrue(pixels.Any(pixel => pixel.a == 0));
                foreach (var cell in cells)
                {
                    var transparent = 0;
                    var opaque = 0;
                    var left = Mathf.RoundToInt(cell.x * decoded.width);
                    var bottom = Mathf.RoundToInt(cell.y * decoded.height);
                    var width = Mathf.RoundToInt(cell.width * decoded.width);
                    var height = Mathf.RoundToInt(cell.height * decoded.height);
                    for (var y = bottom; y < bottom + height; y++)
                        for (var x = left; x < left + width; x++)
                        {
                            var alpha = pixels[y * decoded.width + x].a;
                            if (alpha == 0) transparent++;
                            if (alpha >= 200) opaque++;
                        }
                    Assert.Greater(transparent, 100, "Each sprite keeps a transparent surround.");
                    Assert.Greater(opaque, 100, "Each named cell contains a visible figurine.");
                }
            }
            finally { Object.DestroyImmediate(decoded); }
        }

        private static BoardFigureChanges Online(PlayerView view)
        {
            var changes = new BoardFigureChanges();
            changes.SetConnected(true);
            Assert.IsEmpty(changes.Apply(view, Region));
            return changes;
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "figures", Units = { Unit("first", "minas-tirith") },
            Fellowship = new FellowshipView { RegionId = "rivendell", Hidden = true }
        };

        private static UnitView Unit(string id, string region) => new UnitView
        {
            Id = id, Location = region, Nation = "gondor", Side = Side.FreePeoples, Kind = UnitKind.Regular
        };

        private static CharacterView Character(string id, string region) => new CharacterView
        {
            Id = id, Location = region, DefinitionId = "base.character.gimli", Side = Side.FreePeoples
        };
    }
}
