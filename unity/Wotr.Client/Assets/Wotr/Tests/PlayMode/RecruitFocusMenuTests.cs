using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // UI-412 (owner, 2026-10-29): with a Muster die chosen, a clicked settlement lights only the reserves it can take. In
    // the Corrupted kings scenario Erebor takes Dwarven Regulars but no Elite, the Iron Hills take the Elite, Dale no North
    // Elite, and a region without a settlement lights every recruit again.
    public sealed class RecruitFocusMenuTests
    {
        [UnityTest]
        public IEnumerator AClickedSettlementLightsOnlyTheReservesItCanTake() => MatchTestRoom.Wait(Focus());

        private static async Task Focus()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui412-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-corrupted-recruitment", token);
            await Task.Delay(500, token);
            var board = client.Board;
            var revision = board.View.Revision;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(600, token);
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "Before a click, a Dwarven Elite may go to the Iron Hills.");
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular));
            await Inspect(board, "erebor", token);
            Assert.AreEqual("Reserve · Erebor", Title(board));
            // UI-538 (owner): the settlement in focus stands out on the map from the other lit settlements.
            AssertChosen(board, "erebor", "iron-hills");
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Regular), "Erebor still takes Regulars.");
            Assert.IsFalse(Lit(board, "dwarves", UnitKind.Elite), "Corrupted Dáin forbids Elites in Erebor.");
            Assert.IsTrue(Token(board, "dwarves", UnitKind.Elite).enabledSelf, "An unlit Elite stays choosable for another settlement.");
            StringAssert.Contains("Erebor", Token(board, "dwarves", UnitKind.Elite).tooltip);
            Assert.IsFalse(board.Reinforcements.Root.Q("reinforcement-elves").ClassListContains("recruitable"), "The Elves' row is not lit.");
            Assert.IsFalse(Lit(board, "elves", UnitKind.Regular), "No Elf goes to Erebor.");
            LayoutTests.Save(client.Panel.targetTexture, "ui412-erebor.png");
            await Inspect(board, "iron-hills", token);
            Assert.AreEqual("Reserve · Iron Hills", Title(board));
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "The Iron Hills take a Dwarven Elite.");
            AssertChosen(board, "iron-hills", "erebor");
            LayoutTests.Save(client.Panel.targetTexture, "ui538-iron-hills.png");
            await Inspect(board, "dale", token);
            Assert.IsTrue(Lit(board, "north", UnitKind.Regular));
            Assert.IsFalse(Lit(board, "north", UnitKind.Elite), "Corrupted Brand forbids North Elites everywhere.");
            await Inspect(board, "old-forest", token);
            Assert.AreEqual("Reserve", Title(board));
            CollectionAssert.IsEmpty(board.Map.DraftOverlay.ChosenPlacements, "A region without a settlement clears the mark.");
            Assert.AreEqual(MapRegionEmphasis.PlacementTarget, board.Map.Renderer.CurrentEmphasis("iron-hills"));
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular), "A region without a settlement lights every recruit again.");
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite));
            Assert.AreEqual(revision, board.View.Revision, "Clicking settlements sends nothing.");
            await TakeUp(board, client, token);
            Assert.AreEqual(revision, board.View.Revision, "Taking a figure up sends nothing.");
        }

        // UI-538 (owner, 2026-11-16): a figure taken up keeps its own settlements green; the settlements only the other
        // figures may take turn pale grey, still and no targets. The figure has two settlements or more, so its click
        // only takes it up (UI-142 would send a figure with one).
        private static async Task TakeUp(BoardScreen board, MountedFlowClient client, CancellationToken token)
        {
            var open = board.Map.DraftOverlay.PlacementTargets.ToList();
            var recruitment = board.View.Options.First(o => o.Kind == CommandKind.Recruit && o.Enabled).Recruitment;
            var group = recruitment.First(g => g.Targets.Count >= 2 && recruitment.Any(other => other.Targets.Any(t => !g.Targets.Contains(t))));
            PointerGestureEvents.ClickAt(Token(board, group.Nation, group.Kind));
            await Task.Delay(600, token);
            var own = board.Map.DraftOverlay.PlacementTargets.ToList();
            CollectionAssert.IsNotEmpty(own, "The taken figure lights its own settlements.");
            CollectionAssert.IsSubsetOf(own, open, "Among the settlements lit before.");
            var pale = open.Where(region => !own.Contains(region)).ToList();
            CollectionAssert.IsNotEmpty(pale, "Other figures recruit elsewhere.");
            foreach (var region in own)
                Assert.AreEqual(MapRegionEmphasis.PlacementTarget, board.Map.Renderer.CurrentEmphasis(region), region);
            CollectionAssert.AreEquivalent(pale, board.Map.DraftOverlay.OtherPlacements);
            foreach (var region in pale)
            {
                Assert.AreEqual(MapRegionEmphasis.OtherPlacement, board.Map.Renderer.CurrentEmphasis(region), region);
                Assert.IsFalse(board.Map.Renderer.TargetPulses(region), region + " stands still.");
            }
            LayoutTests.Save(client.Panel.targetTexture, "ui538-taken-figure.png");
        }

        private static async Task Inspect(BoardScreen board, string region, CancellationToken token)
        {
            MapContourProbe.ClickRegion(board.Map, region);
            await Task.Delay(500, token);
            Assert.AreEqual(region, board.Hover.Panel.Region);
        }

        private static string Title(BoardScreen board) => board.Reinforcements.Root.Q<Label>(className: "title").text;

        // UI-538: the chosen settlement is the bright, steady green with its own edge; another lit settlement stays lit.
        private static void AssertChosen(BoardScreen board, string chosen, string other)
        {
            CollectionAssert.AreEquivalent(new[] { chosen }, board.Map.DraftOverlay.ChosenPlacements);
            Assert.AreEqual(MapRegionEmphasis.ChosenPlacement, board.Map.Renderer.CurrentEmphasis(chosen), chosen);
            Assert.IsTrue(board.Map.Renderer.PlacementChosen(chosen), chosen + " has the bright edge.");
            Assert.IsFalse(board.Map.Renderer.TargetPulses(chosen), chosen + " stands still.");
            Assert.AreEqual(MapRegionEmphasis.PlacementTarget, board.Map.Renderer.CurrentEmphasis(other), other + " stays lit.");
        }

        private static Button Token(BoardScreen board, string nation, UnitKind kind)
        {
            var token = board.Reinforcements.Root.Q<Button>("reserve-token-" + nation + "-" + kind);
            Assert.IsNotNull(token, nation + " " + kind);
            return token;
        }

        private static bool Lit(BoardScreen board, string nation, UnitKind kind)
        {
            var token = Token(board, nation, kind);
            return token.enabledSelf && token.ClassListContains("eligible-token");
        }
    }
}
