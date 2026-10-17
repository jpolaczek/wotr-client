using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class MapContourBorderTests
    {
        // UI-104: the board's frame meets the canvas paper without a line; every printed map border keeps its ink.
        [Test]
        public void TheFrameIsNotInkedWhileCoastsLakesAndBarriersAre()
        {
            Assert.IsFalse(MapRenderer.Inked(MapBorderKind.Frame));
            Assert.IsTrue(MapRenderer.Inked(MapBorderKind.Coast));
            Assert.IsTrue(MapRenderer.Inked(MapBorderKind.Lake));
            Assert.IsTrue(MapRenderer.Inked(MapBorderKind.Impassable));
            Assert.IsFalse(MapRenderer.Inked(MapBorderKind.Passage), "Passages are drawn by their own layer.");
            Assert.IsFalse(MapRenderer.Inked(MapBorderKind.River), "Rivers are passable bands.");
            var borders = BoardPresentation.Load().Borders;
            var frames = borders.Paths.Values.SelectMany(path => path.Parts).Count(part => part.Kind == MapBorderKind.Frame);
            Assert.AreEqual(13, frames, "North, east and south edges and the north-east cut.");
        }

        // UI-143: a nation band is narrower than the passage beside it and drawn as a two-tone wash of its own hue.
        [Test]
        public void NationBandsAreNarrowTwoToneWashesOfTheirNationsHue()
        {
            var palette = BoardPresentation.Load().Borders.Palette;
            Assert.Less(palette.Style.NationWidth, palette.Style.PassageWidth, "A band is narrower than the passage line.");
            foreach (var nation in palette.Nations)
            {
                var wash = palette.NationWashes[nation.Key];
                var edge = palette.NationEdges[nation.Key];
                Assert.Less(wash.a, 1f, nation.Key + " lets the map show through its wash.");
                Assert.Greater(Luminance(wash), Luminance(nation.Value), nation.Key + " wash is lighter than its hue.");
                Assert.Less(Luminance(edge), Luminance(nation.Value), nation.Key + " edge is darker than its hue.");
                Color.RGBToHSV(nation.Value, out var hue, out var saturation, out _);
                Color.RGBToHSV(edge, out var edgeHue, out _, out _);
                if (saturation > .1f)
                    Assert.AreEqual(hue, edgeHue, .01f, nation.Key + " edge keeps the nation's hue, not grey ink.");
            }
        }

        // UI-143: the printed barrier keeps its width only as a faint shadow; the ink itself is a thin, warm pen line.
        [Test]
        public void BarriersArePenLinesNotMarkerStrokes()
        {
            var palette = BoardPresentation.Load().Borders.Palette;
            Assert.Less(palette.BarrierCoreWidth, palette.Style.PassageWidth, "The ink core is finer than a passage line.");
            Assert.LessOrEqual(palette.BarrierShadowWidth, palette.Style.BarrierWidth);
            Assert.Greater(palette.BarrierShadowWidth, palette.BarrierCoreWidth);
            Assert.Less(palette.BarrierShade.a, .5f, "The shadow lets the mountains show through.");
            Assert.Less(palette.BarrierInk.a, 1f);
            Assert.Greater(Luminance(palette.BarrierInk), Luminance(palette.Barrier), "The ink is softer than the printed black.");
            Assert.Less(Luminance(palette.BarrierInk), Luminance(palette.Keyline), "A barrier still reads darker than a keyline.");
            Assert.GreaterOrEqual(palette.BarrierInk.r, palette.BarrierInk.b, "The ink is warm, like the map's sepia.");
        }

        private static float Luminance(Color color) => .2126f * color.r + .7152f * color.g + .0722f * color.b;

        [Test]
        public void EverySourceSegmentHasExactlyOneClassificationAndSource()
        {
            var board = BoardPresentation.Load();
            var borders = board.Borders;
            Assert.AreEqual(323, borders.Paths.Count);
            Assert.AreEqual(360, borders.Paths.Values.Sum(path => path.Parts.Count));
            Assert.AreEqual(33, borders.Paths.Values.Count(path => path.Parts.Count > 1));
            var segments = 0;
            foreach (var path in borders.Paths.Values)
            {
                var end = 0;
                Assert.AreEqual("MAP-13-BORDER-AUDIT/" + path.Id, path.SourceReference);
                foreach (var part in path.Parts)
                {
                    Assert.AreEqual(end, part.FromSegment);
                    segments += part.ToSegment - part.FromSegment;
                    end = part.ToSegment;
                }
                Assert.AreEqual(borders.Geometry[path.Id].Points.Length - 1, end);
            }
            Assert.AreEqual(14348, segments);
            Assert.AreSame(borders, BoardPresentation.Load().Borders);
        }

        [Test]
        public void EveryLegalPairHasPassageAndAdditionalContactsRemainBlocked()
        {
            var board = BoardPresentation.Load();
            var passagePairs = board.Borders.Paths.Values.Where(p => p.Parts.Any(s => MapBorderData.Passable(s.Kind)))
                .Select(p => Pair(p.PositiveNormalOwner, p.NegativeNormalOwner)).ToHashSet();
            var legalPairs = board.Board.Regions.SelectMany(r => r.Neighbors.Select(n => Pair(r.Id, n))).ToHashSet();
            CollectionAssert.AreEquivalent(legalPairs, passagePairs);
            Assert.AreEqual(232, passagePairs.Count);
            var contacts = board.Geometry.Boundaries.Where(p => p.Owners.All(o => o != null))
                .Select(p => Pair(p.Owners[0], p.Owners[1])).ToHashSet();
            Assert.AreEqual(50, contacts.Except(passagePairs).Count());
            Assert.IsFalse(passagePairs.Contains(Pair("rivendell", "high-pass")));
            Assert.IsFalse(passagePairs.Contains(Pair("high-pass", "old-ford")));
        }

        [TestCase("boundary-0208")]
        [TestCase("boundary-0209")]
        [TestCase("boundary-0211")]
        [TestCase("boundary-0192")]
        [TestCase("boundary-0270")]
        public void ReviewedMountainPassesKeepTheirOpeningAndImpassableRidge(string id)
        {
            var path = BoardPresentation.Load().Borders.Paths[id];
            Assert.IsTrue(path.Parts.Any(part => MapBorderData.Passable(part.Kind)));
            Assert.IsTrue(path.Parts.Any(part => part.Kind == MapBorderKind.Impassable));
        }

        [Test]
        public void EasternNurnContactRemainsBlackWhileWesternContactAllowsPassage()
        {
            var borders = BoardPresentation.Load().Borders;
            Assert.IsTrue(borders.Paths["boundary-0212"].Parts.Any(p => MapBorderData.Passable(p.Kind)));
            Assert.IsTrue(borders.Paths["boundary-0213"].Parts.All(p => p.Kind == MapBorderKind.Impassable));
            foreach (var id in new[] { "boundary-0017", "boundary-0029" })
                CollectionAssert.AreEquivalent(new[] { MapBorderKind.Coast, MapBorderKind.Frame },
                    borders.Paths[id].Parts.Select(part => part.Kind));
            Assert.AreEqual(MapBorderKind.Lake, borders.Paths["boundary-0014"].Parts[0].Kind);
        }

        [Test]
        public void TwoNationBandsFollowActualRingSidesEvenWhenPathDirectionIsReversed()
        {
            var board = BoardPresentation.Load();
            var path = board.Borders.Paths["boundary-0039"];
            var forward = board.Borders.Geometry[path.Id];
            var reversed = new MapBoundaryGeometry { Points = forward.Points.Reverse().ToArray(), Owners = forward.Owners };
            var orientation = new MapBorderOrientation(board.Geometry);
            var reverseSides = orientation.Sides(reversed);
            Assert.AreEqual(path.NegativeNormalOwner, reverseSides.Positive);
            Assert.AreEqual(path.PositiveNormalOwner, reverseSides.Negative);
            var bands = MapBorderData.Bands(path, MapBorderKind.Passage, board.Regions);
            var reverseBands = MapBorderData.Bands(new MapBorderPath
            {
                PositiveNormalOwner = reverseSides.Positive, NegativeNormalOwner = reverseSides.Negative
            }, MapBorderKind.Passage, board.Regions);
            CollectionAssert.AreEquivalent(new[] { "dwarves", "elves" }, new[] { bands.Positive, bands.Negative });
            Assert.AreEqual(bands.Positive, reverseBands.Negative);
            var from = new Vector2(forward.Points[0][0], forward.Points[0][1]);
            var to = new Vector2(forward.Points[1][0], forward.Points[1][1]);
            Assert.That((MapBorderOrientation.PositiveNormal(from, to) + MapBorderOrientation.PositiveNormal(to, from)).sqrMagnitude,
                Is.LessThan(.000001f));
        }

        [Test]
        public void NationBandsDoNotUseMilitaryControlAndDoNotCoverBarriers()
        {
            var board = BoardPresentation.Load();
            var path = board.Borders.Paths["boundary-0039"];
            var before = MapBorderData.Bands(path, MapBorderKind.Passage, board.Regions);
            var view = new PlayerView();
            view.Control["grey-havens"] = Side.Shadow;
            board.Apply(view);
            var after = MapBorderData.Bands(path, MapBorderKind.Passage, board.Regions);
            Assert.AreEqual(before.Positive, after.Positive);
            Assert.AreEqual(before.Negative, after.Negative);
            foreach (var kind in new[] { MapBorderKind.Impassable, MapBorderKind.Coast, MapBorderKind.Lake, MapBorderKind.Frame })
            {
                var bands = MapBorderData.Bands(path, kind, board.Regions);
                Assert.IsNull(bands.Positive);
                Assert.IsNull(bands.Negative);
            }
            var same = new MapBorderPath { PositiveNormalOwner = "lamedon", NegativeNormalOwner = "pelargir" };
            Assert.IsNull(MapBorderData.Bands(same, MapBorderKind.Passage, board.Regions).Positive);
            var neutral = new MapBorderPath { PositiveNormalOwner = "dead-marshes", NegativeNormalOwner = "dagorlad" };
            Assert.IsNull(MapBorderData.Bands(neutral, MapBorderKind.Passage, board.Regions).Negative);
        }

        [Test]
        public void PaletteKeepsEightPrintedNamesAndDistinctColors()
        {
            var palette = BoardPresentation.Load().Borders.Palette;
            CollectionAssert.AreEquivalent(new[] { "Brown", "Light green", "Dark blue", "Light blue", "Dark green", "Yellow", "Red", "Orange" },
                palette.Style.Nations.Select(n => n.ColorName));
            Assert.AreEqual(8, palette.Nations.Values.Distinct().Count());
            Assert.Greater(palette.Style.BarrierWidth, palette.Style.PassageWidth);
        }

        [TestCase("missing")]
        [TestCase("overlap")]
        [TestCase("source")]
        [TestCase("geometry")]
        [TestCase("normal")]
        [TestCase("illegal-passage")]
        [TestCase("palette")]
        public void InvalidCatalogCannotSilentlyLoad(string failure)
        {
            var board = BoardPresentation.Load();
            var catalog = JsonConvert.DeserializeObject<MapBorderCatalog>(Resources.Load<TextAsset>("map-boundaries").text);
            var style = JsonConvert.DeserializeObject<MapStyle>(Resources.Load<TextAsset>("map-style").text);
            switch (failure)
            {
                case "missing": catalog.Paths.RemoveAt(0); break;
                case "overlap": catalog.Paths[208].Parts[1].FromSegment--; break;
                case "source": catalog.Paths[0].SourceReference = ""; break;
                case "geometry": catalog.GeometryHash = new string('0', 64); break;
                case "normal": catalog.Paths[39].PositiveNormalOwner = catalog.Paths[39].NegativeNormalOwner; break;
                case "illegal-passage": catalog.Paths[50].Parts[0].Kind = MapBorderKind.Passage; break;
                case "palette": style.Nations[0].Color = "brown"; break;
            }
            Assert.Throws<InvalidDataException>(() => new MapBorderData(board.Geometry, catalog, style, board.Board,
                Resources.Load<TextAsset>("map-geometry").bytes));
        }

        private static string Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + ":" + b : b + ":" + a;
    }
}
