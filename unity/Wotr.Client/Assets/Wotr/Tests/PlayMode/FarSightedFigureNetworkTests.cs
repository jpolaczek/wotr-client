using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-465 (owner, 2026-11-03, deciding UI-456's open question): "both". kings-far-sighted: Thranduil Awakened in the
    // Woodland Realm, every companion in the Fellowship, the Free Peoples' Ruler die showing a Character. With the Ruler
    // die Far-sighted's Move companions stays greyed and points at his figure, which lights as Far-sighted's mover: its click
    // (or its drag, UI-405) starts MoveCompanions with Far-sighted and lights his Far-sighted range, and a region one beyond
    // his plain move sends it. The catalog's scenario is started anew for each path.
    public sealed class FarSightedFigureNetworkTests
    {
        private const string Scenario = "kings-far-sighted";
        private const string Thranduil = FarSightedMoves.Owner;
        private const string Home = "woodland-realm";

        [UnityTest] public IEnumerator ThranduilsFigureClickMovesHimWithFarSighted() => Wait(Move(drag: false));
        [UnityTest] public IEnumerator ThranduilsFigureIsCarriedWithFarSighted() => Wait(Move(drag: true));

        private static async Task Move(bool drag)
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Fp;
            var board = client.Board;
            var sent = new List<GameCommand>();
            client.Interaction.CommandAnswered += (command, receipt, _) =>
            {
                Assert.IsTrue(receipt.Accepted, receipt.Error);
                sent.Add(command);
            };
            var option = board.View.Options.Single(o => o.Kind == CommandKind.MoveCompanions);
            var plain = option.Travel.Single(t => t.Id == Thranduil).Targets;
            var further = option.RulerTravel.Travel.Single(t => t.Id == Thranduil).Targets.Where(r => !plain.Contains(r)).ToList();
            Assert.IsNotEmpty(further, "Far-sighted reaches one region beyond his plain move.");
            var die = board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
            T22NetworkActions.Die(client, die);
            await room.Settle();
            await VariantGreyed(room, board);
            var target = Nearest(board.Map, further);
            var figure = board.Map.Q<Button>("character-" + Thranduil);
            await Expose(room, client, figure, target);
            Assert.IsTrue(figure.ClassListContains(MapCharacterArtwork.EligibleClass), "The Ruler die lights his figure.");
            StringAssert.Contains(FarSightedFigure.Offer, figure.tooltip, "His tooltip names Far-sighted · Range +1.");
            if (drag)
                await ByDrag(room, board, figure, target);
            else
                await ByClicks(room, client, figure, further, target);
            var move = sent.Single(c => c.Kind == CommandKind.MoveCompanions);
            Assert.AreEqual(FarSightedMoves.Ability, move.Payload.AbilityId, "Far-sighted's move.");
            Assert.AreEqual(die, move.Payload.DieId);
            CollectionAssert.AreEqual(new[] { Thranduil }, move.Payload.CompanionMoves.Single().Ids);
            Assert.AreEqual(target, move.Payload.CompanionMoves.Single().Route.Last());
            Assert.AreEqual(target, room.Sh.Board.View.Characters.Single(c => c.Id == Thranduil).Location,
                "He moved one region beyond his plain range.");
        }

        // UI-405: a press on his figure that crosses the drag threshold lifts him; the drop on the region is its click.
        private static async Task ByDrag(MatchTestRoom room, BoardScreen board, Button figure, string target)
        {
            var revision = board.View.Revision;
            MapContourProbe.DragFigure(board, figure, MapContourProbe.Terrain(board.Map, target));
            await room.At(revision + 1);
        }

        // His figure's click lights his Far-sighted range (once more after Back); the region's click sends the move.
        private static async Task ByClicks(MatchTestRoom room, MountedMatchClient client, Button figure, List<string> further,
            string target)
        {
            var board = client.Board;
            await ShowTooltip(room, client, figure);
            await StartAndUndo(room, board, figure, further);
            PointerGestureEvents.ClickAt(figure);
            await room.Settle();
            CollectionAssert.IsSubsetOf(further, board.Map.DraftOverlay.Targets.ToList(), "His Far-sighted range lights.");
            StringAssert.EndsWith("Click a highlighted region to move there.", board.Console.Status.Hint.text);
            LayoutTests.Save(client.Viewport, "ui465-far-sighted-range.png");
            var revision = board.View.Revision;
            FellowshipGestures.Destination(client, target);
            await room.At(revision + 1);
        }

        // The region one beyond his plain move nearest to his own.
        private static string Nearest(MapCanvas map, List<string> regions) =>
            regions.OrderBy(region => Vector2.Distance(Anchor(map, region), Anchor(map, Home))).First();

        private static Vector2 Anchor(MapCanvas map, string region) =>
            new Vector2(map.Data.Shapes[region].Anchor[0], map.Data.Shapes[region].Anchor[1]);

        // His miniature and the target region in view together, close enough to show the figures, framed without a gesture.
        private static async Task Expose(MatchTestRoom room, MountedMatchClient client, Button figure, string target)
        {
            var map = client.Board.Map;
            map.FocusOn((Anchor(map, Home) + Anchor(map, target)) / 2, map.FitZoom * 2f);
            await room.Settle();
            await room.Settle();
            var picked = figure.panel.Pick(figure.worldBound.center);
            Assert.IsTrue(picked == figure || figure.Contains(picked), "His figure is covered by " + picked?.name + ".");
            MapContourProbe.Terrain(map, target);
        }

        // The tooltip as the player reads it, once the pointer rests on his figure (UI-80's 200 ms).
        private static async Task ShowTooltip(MatchTestRoom room, MountedMatchClient client, Button figure)
        {
            var board = client.Board;
            MapContourProbe.Hover(board.Root, figure.worldBound.center);
            await Task.Delay(350);
            var shown = board.Root.panel.visualTree.Q<Label>("runtime-tooltip");
            Assert.AreEqual(DisplayStyle.Flex, shown.resolvedStyle.display, "His tooltip shows.");
            StringAssert.Contains("Far-sighted · Range +1", shown.text);
            LayoutTests.Save(client.Viewport, "ui465-lit-figure.png");
            MapContourProbe.Hover(board.Root, MapContourProbe.ExposedMap(board.Map));
            await room.Settle();
        }

        // UI-456: Far-sighted's Move companions stays greyed in his region's window; UI-465: its reason points at his figure.
        private static async Task VariantGreyed(MatchTestRoom room, BoardScreen board)
        {
            Assert.AreEqual(Home, board.Hover.Panel.ActionRegion("far-sighted-MoveCompanions"));
            board.Hover.Open(Home);
            await room.Settle();
            var variant = board.Hover.Panel.Components.Root.Q<Button>("component-far-sighted-MoveCompanions");
            Assert.IsFalse(variant.enabledSelf, "No companion is outside the Fellowship.");
            StringAssert.EndsWith(FarSightedMoves.NoCompanion, SkillTooltip.Plain(variant.tooltip));
            StringAssert.Contains(FarSightedFigure.Offer, board.Hover.Panel.Root.Q<Button>("hover-figure-" + Thranduil).tooltip,
                "His chip in the region's panel says the same.");
            board.Hover.Close();
            await room.Settle();
        }

        // The click starts the Far-sighted move; the console's Back leaves him behind and the figure still offers it.
        private static async Task StartAndUndo(MatchTestRoom room, BoardScreen board, Button figure, List<string> further)
        {
            PointerGestureEvents.ClickAt(figure);
            await room.Settle();
            Assert.IsTrue(figure.ClassListContains(MapCharacterArtwork.SelectedClass), "His click chooses him.");
            CollectionAssert.IsSubsetOf(further, board.Map.DraftOverlay.Targets.ToList());
            PointerGestureEvents.ClickAt(board.Console.Actions.Back);
            await room.Settle();
            Assert.IsFalse(figure.ClassListContains(MapCharacterArtwork.SelectedClass), "Back leaves him behind.");
            Assert.IsTrue(board.OwnDice.Q<Button>(board.ActionDie.DieId).ClassListContains("selected"), "The die stays chosen.");
            Assert.IsTrue(figure.ClassListContains(MapCharacterArtwork.EligibleClass));
            StringAssert.Contains(FarSightedFigure.Offer, figure.tooltip);
        }
    }
}
