using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-522: a force's row stand narrows on the side a stand stands on now: a character on its line keeps UI-422's gap
    // from the row's round end and clear of its figures, one just behind keeps clear of the row's figures, one far behind
    // does not narrow it, one straight behind narrows both sides, and nothing narrows a side below a round base's half or
    // widens it past its place's reach.
    public sealed class MapArmyRowRoomTests
    {
        private static readonly Vector2 Feet = new Vector2(500, 500);
        private static readonly Vector2 Widest = Vector2.one * StandFootprint.RowReach;

        [Test]
        public void ACharacterOnTheRowsLineNarrowsItsSideToItsGap()
        {
            var reach = MapArmyRowRoom.Narrow(Feet, Widest, new[] { (Feet + new Vector2(40, 0), StandFootprint.Character) });
            // Bases: the row's round end (20) and the character's (7) a gap (1.5) apart: 40 - 28.5 + 20 = 31.5.
            Assert.AreEqual(31.5f, reach.y, .01f);
            Assert.AreEqual(StandFootprint.RowReach, reach.x, .01f, "The other side keeps its reach.");
        }

        [Test]
        public void ACharacterJustBehindKeepsClearOfTheRowsFigures()
        {
            var reach = MapArmyRowRoom.Narrow(Feet, Widest, new[] { (Feet + new Vector2(-30, -10), StandFootprint.Character) });
            // Its figure reaches down into the row's: the row ends where its figure (16 wide) begins, 30 - 8 = 22 out.
            Assert.AreEqual(22, reach.x, .01f);
            Assert.AreEqual(StandFootprint.RowReach, reach.y, .01f);
        }

        [Test]
        public void AStandFarBehindLeavesTheReach()
        {
            var reach = MapArmyRowRoom.Narrow(Feet, Widest, new[] { (Feet + new Vector2(10, -60), StandFootprint.Fellowship) });
            Assert.AreEqual(Widest, reach);
        }

        // UI-526: past its place a row reaches as far as the widest row goes where nothing stands, but stops short of a
        // force behind it whose plaque it would cover.
        [Test]
        public void PastItsPlaceARowStopsShortOfAPlaqueBehindIt()
        {
            var nothing = new (Vector2, StandFootprint)[0];
            Assert.AreEqual(Widest, MapArmyRowRoom.Limit(Feet, nothing, new Vector2[0]));
            var limit = MapArmyRowRoom.Limit(Feet, nothing, new[] { Feet + new Vector2(30, -5) });
            Assert.AreEqual(StandFootprint.RowReach, limit.x, .01f, "The open side reaches the widest");
            Assert.AreEqual(StandFootprint.Army.BaseWidth / 2, limit.y, .01f, "The force's side keeps a round base's half");
        }

        // UI-526 (release check): a Minas Morgul row reached over its Mordor gate and took the gate's click. A mark across
        // the row's band (its figures down to its front) stops the row a gap short of it; one above the figures does not.
        [Test]
        public void PastItsPlaceARowStopsShortOfAMarkOnTheBoard()
        {
            var nothing = new (Vector2, StandFootprint)[0];
            var gate = MapArmyRowRoom.Mark(Feet + new Vector2(50, -10), new Vector2(40, 40), 1);
            var limit = MapArmyRowRoom.Limit(Feet, nothing, new Vector2[0], new[] { gate });
            // The gate spans 30 to 70 units right of the feet: 30 - 1.5.
            Assert.AreEqual(28.5f, limit.y, .01f);
            Assert.AreEqual(StandFootprint.RowReach, limit.x, .01f, "The other side keeps the widest reach.");
            var above = MapArmyRowRoom.Mark(Feet + new Vector2(50, -90), new Vector2(40, 40), 1);
            Assert.AreEqual(Widest, MapArmyRowRoom.Limit(Feet, nothing, new Vector2[0], new[] { above }));
            Assert.AreEqual(57.5f, MapArmyRowRoom.Mark(Feet, new Vector2(46, 46), .8f).width, .01f,
                "A mark of 46 points at .8 points a unit covers 57.5 units.");
        }

        [Test]
        public void EachSideStaysBetweenARoundBaseAndItsReach()
        {
            var round = StandFootprint.Army.BaseWidth / 2;
            var behind = MapArmyRowRoom.Narrow(Feet, Widest, new[] { (Feet + new Vector2(0, -12), StandFootprint.Character) });
            Assert.AreEqual(new Vector2(round, round), behind, "A stand straight behind narrows both sides.");
            var place = new Vector2(24, 30);
            Assert.AreEqual(place, MapArmyRowRoom.Narrow(Feet, place, new (Vector2, StandFootprint)[0]));
        }
    }
}
