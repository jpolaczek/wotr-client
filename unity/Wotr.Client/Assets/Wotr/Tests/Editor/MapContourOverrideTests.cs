using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class MapContourOverrideTests
    {
        private static readonly string[] FateEdges = { "boundary-0056", "boundary-0066", "boundary-0067",
            "boundary-0069", "boundary-0070", "boundary-0135", "boundary-0140", "boundary-0141", "boundary-0142" };
        private static readonly string[] WhiteHandEdges = { "boundary-0143", "boundary-0149" };

        [Test]
        public void FateAndWhiteHandInvalidateExactlyTheirNationBandsAndResetToPrintedMetadata()
        {
            var board = BoardPresentation.Load();
            using var renderer = new MapRenderer();
            renderer.Prepare(board);
            var original = Bands(board);
            var geometry = board.Geometry;
            var contours = board.Contours;
            var borders = board.Borders;
            var view = Fate(board, false);
            Apply(board, renderer, view, FateEdges.Length);
            CollectionAssert.AreEquivalent(FateEdges, Changed(original, Bands(board)));
            Assert.AreEqual("sauron", board.Regions["erebor"].Nation);
            Assert.IsNull(board.Regions["dale"].Nation);
            Assert.AreEqual("Fortification", board.Regions["dale"].Settlement);
            Assert.AreEqual("City", board.Regions["iron-hills"].Settlement);
            Apply(board, renderer, view, 0);

            var fate = Bands(board);
            view.RegionOverrides.Add(Override(board, "helms-deep", "isengard", "Stronghold", Side.Shadow));
            Apply(board, renderer, view, WhiteHandEdges.Length);
            CollectionAssert.AreEquivalent(WhiteHandEdges, Changed(fate, Bands(board)));
            Apply(board, renderer, new PlayerView(), FateEdges.Length + WhiteHandEdges.Length);
            CollectionAssert.AreEquivalent(original, Bands(board));
            Assert.AreEqual("dwarves", board.Regions["erebor"].Nation);
            Assert.AreEqual("City", board.Regions["dale"].Settlement);
            Assert.AreEqual("Town", board.Regions["iron-hills"].Settlement);
            Assert.AreSame(geometry, board.Geometry);
            Assert.AreSame(contours, board.Contours);
            Assert.AreSame(borders, board.Borders);
        }

        [Test]
        public void NewCitiesAndMilitaryCaptureChangeSettlementMetadataWithoutRebuildingAnyBand()
        {
            var board = BoardPresentation.Load();
            using var renderer = new MapRenderer();
            renderer.Prepare(board);
            Apply(board, renderer, Fate(board, false), FateEdges.Length);
            var view = Fate(board, true);
            Apply(board, renderer, view, 0);
            Assert.AreEqual("City", board.Regions["ered-luin"].Settlement);
            Assert.AreEqual("dwarves", board.Regions["ered-luin"].Nation);
            Assert.AreEqual("City", board.Regions["south-rhun"].Settlement);
            Assert.AreEqual("southrons", board.Regions["south-rhun"].Nation);
            view.Control["erebor"] = Side.FreePeoples;
            view.Control["helms-deep"] = Side.Shadow;
            Apply(board, renderer, view, 0);
            Assert.AreEqual(Side.Shadow, board.Regions["erebor"].OriginalOwner);
            Assert.AreEqual("sauron", board.Regions["erebor"].Nation);
            Assert.AreEqual("rohan", board.Regions["helms-deep"].Nation);
            Apply(board, renderer, new PlayerView(), FateEdges.Length);
            Assert.AreEqual("Town", board.Regions["ered-luin"].Settlement);
            Assert.AreEqual("Town", board.Regions["south-rhun"].Settlement);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResetNeverLeavesAnotherGamesOverridesInTheExistingRenderer(bool setup)
        {
            var board = BoardPresentation.Load();
            using var renderer = new MapRenderer();
            renderer.Prepare(board);
            var original = Bands(board);
            var view = Fate(board, true);
            view.RegionOverrides.Add(Override(board, "helms-deep", "isengard", "Stronghold", Side.Shadow));
            Apply(board, renderer, view, 11);
            if (setup) board.UseSetup();
            else board.ClearGameState();
            var builds = renderer.BorderBuilds;
            renderer.UpdateMetadata(board);
            Assert.AreEqual(builds + 11, renderer.BorderBuilds);
            CollectionAssert.AreEquivalent(original, Bands(board));
            Assert.AreEqual("Town", board.Regions["ered-luin"].Settlement);
            Assert.AreEqual("Town", board.Regions["south-rhun"].Settlement);
        }

        [Test]
        public void AllConfigurationsKeepEveryConnectionBarrierAndContourAndIgnoreUnrelatedPrivateMetadata()
        {
            var board = BoardPresentation.Load();
            using var renderer = new MapRenderer();
            renderer.Prepare(board);
            var graph = Graph(board);
            Assert.AreEqual(232, graph.Count);
            var classification = JsonConvert.SerializeObject(board.Borders.Catalog);
            var geometry = JsonConvert.SerializeObject(board.Geometry);
            var whiteHand = new PlayerView();
            whiteHand.RegionOverrides.Add(Override(board, "helms-deep", "isengard", "Stronghold", Side.Shadow));
            foreach (var view in new[] { new PlayerView(), Fate(board, false), Fate(board, true), whiteHand })
            {
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    view.You = side;
                    board.Apply(view);
                    renderer.UpdateMetadata(board);
                    CollectionAssert.AreEquivalent(graph, Graph(board));
                    Assert.AreEqual(classification, JsonConvert.SerializeObject(board.Borders.Catalog));
                    Assert.AreEqual(geometry, JsonConvert.SerializeObject(board.Geometry));
                    var builds = renderer.BorderBuilds;
                    view.Hand.Add("base.fp.character.01");
                    view.Configuration.Kings = !view.Configuration.Kings;
                    view.Configuration.Council = !view.Configuration.Council;
                    board.Apply(view);
                    renderer.UpdateMetadata(board);
                    Assert.AreEqual(builds, renderer.BorderBuilds,
                        "Only public RegionOverrides can change national bands, never the role, private hand or flags.");
                }
            }
        }

        private static void Apply(BoardPresentation board, MapRenderer renderer, PlayerView view, int expectedBuilds)
        {
            var builds = renderer.BorderBuilds;
            board.Apply(view);
            renderer.UpdateMetadata(board);
            Assert.AreEqual(expectedBuilds, renderer.BorderBuilds - builds);
        }

        private static PlayerView Fate(BoardPresentation board, bool cities)
        {
            var view = new PlayerView();
            view.RegionOverrides.Add(Override(board, "erebor", "sauron", "Stronghold", Side.Shadow));
            view.RegionOverrides.Add(Override(board, "dale", null, "Fortification", null));
            view.RegionOverrides.Add(Override(board, "iron-hills", "dwarves", "City", Side.FreePeoples));
            if (cities)
            {
                view.RegionOverrides.Add(Override(board, "ered-luin", "dwarves", "City", Side.FreePeoples));
                view.RegionOverrides.Add(Override(board, "south-rhun", "southrons", "City", Side.Shadow));
            }
            return view;
        }

        private static RegionDefinition Override(BoardPresentation board, string id, string nation, string settlement, Side? owner)
        {
            var region = JsonConvert.DeserializeObject<RegionDefinition>(JsonConvert.SerializeObject(board.Board.Regions.Single(r => r.Id == id)));
            region.Nation = nation;
            region.Settlement = settlement;
            region.OriginalOwner = owner;
            return region;
        }

        private static Dictionary<string, string> Bands(BoardPresentation board) => board.Borders.Paths.Values
            .Where(path => path.Parts.Any(part => MapBorderData.Passable(part.Kind)))
            .ToDictionary(path => path.Id, path =>
            {
                var bands = MapBorderData.Bands(path, MapBorderKind.Passage, board.Regions);
                return bands.Positive + "/" + bands.Negative;
            });

        private static IEnumerable<string> Changed(Dictionary<string, string> before, Dictionary<string, string> after) =>
            before.Keys.Where(key => before[key] != after[key]);

        private static HashSet<string> Graph(BoardPresentation board) => board.Regions.Values
            .SelectMany(region => region.Neighbors.Select(neighbor => string.CompareOrdinal(region.Id, neighbor) < 0
                ? region.Id + ":" + neighbor : neighbor + ":" + region.Id)).ToHashSet();
    }
}
