using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class BoardOverrideTests
    {
        [Test]
        public void ServerMetadataUpdatesTheExistingMapAndAnEmptyReplacementSetRestoresBase()
        {
            var board = BoardPresentation.Load();
            var regions = board.Regions;
            var geometry = board.Geometry;
            var printed = board.Regions["erebor"];
            var view = new PlayerView();
            view.RegionOverrides.Add(new RegionDefinition
            {
                Id = "erebor", Name = "Erebor", Nation = "sauron", OriginalOwner = Side.Shadow,
                Settlement = "Stronghold", VictoryPoints = 2
            });
            view.RegionOverrides.Add(new RegionDefinition
            {
                Id = "dale", Name = "Dale", Settlement = "Fortification"
            });
            view.Control["erebor"] = Side.Shadow;
            view.Control["dale"] = null;
            board.Apply(view);
            Assert.AreSame(regions, board.Regions);
            Assert.AreSame(geometry, board.Geometry);
            Assert.AreEqual("sauron", board.Regions["erebor"].Nation);
            Assert.AreEqual("Fortification", board.Regions["dale"].Settlement);
            Assert.IsNull(board.Regions["dale"].Nation);
            Assert.AreEqual("dwarves", printed.Nation);
            board.Apply(new PlayerView());
            Assert.AreSame(printed, board.Regions["erebor"]);
            Assert.AreEqual("City", board.Regions["dale"].Settlement);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearingOrShowingSetupRemovesAnOverrideFromThePreviousMatch(bool showSetup)
        {
            var board = BoardPresentation.Load();
            var view = new PlayerView();
            view.RegionOverrides.Add(new RegionDefinition { Id = "ered-luin", Settlement = "City" });
            board.Apply(view);
            Assert.AreEqual("City", board.Regions["ered-luin"].Settlement);
            if (showSetup)
                board.UseSetup();
            else
                board.ClearGameState();
            Assert.AreEqual("Town", board.Regions["ered-luin"].Settlement);
        }
    }
}
