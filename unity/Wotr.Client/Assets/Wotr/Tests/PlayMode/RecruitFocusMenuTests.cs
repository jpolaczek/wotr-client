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
    // UI-539 (owner, 2026-11-16; UI-412's narrowing retired): with a Muster die chosen in the Corrupted kings scenario the
    // Reserve lights every figure the die may recruit and the map lights nothing; a settlement clicked first changes
    // nothing; a figure's click lights its own settlements and greys the settlements only the others take (UI-538); a lit
    // settlement's click recruits it there.
    public sealed class RecruitFocusMenuTests
    {
        [UnityTest]
        public IEnumerator ARecruitmentIsFigureFirst() => MatchTestRoom.Wait(FigureFirst());

        private static async Task FigureFirst()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui539-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-corrupted-recruitment", token);
            await Task.Delay(500, token);
            var board = client.Board;
            var revision = board.View.Revision;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(600, token);
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "A Dwarven Elite may be recruited (in the Iron Hills).");
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular));
            AssertNothingLit(board, "Before a figure");
            LayoutTests.Save(client.Panel.targetTexture, "ui539-muster-before-figure.png");
            await Inspect(board, "erebor", token);
            Assert.AreEqual("Reserve", Title(board), "A settlement clicked first narrows nothing.");
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "Every figure stays lit.");
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular));
            AssertNothingLit(board, "After a settlement's click");
            Assert.AreEqual(revision, board.View.Revision, "Clicking a settlement sends nothing.");
            var group = await TakeUp(board, client, token);
            Assert.AreEqual(revision, board.View.Revision, "Taking a figure up sends nothing.");
            var target = board.Map.DraftOverlay.PlacementTargets.First();
            MapContourProbe.ClickRegion(board.Map, target);
            for (var wait = 0; wait < 50 && board.View.Revision == revision; wait++)
                await Task.Delay(100, token);
            Assert.Greater(board.View.Revision, revision, "The lit settlement's click recruits there.");
            for (var wait = 0; wait < 20 && !board.View.Units.Any(u => u.Nation == group.Nation && u.Kind == group.Kind && u.Location == target); wait++)
                await Task.Delay(100, token);
            Assert.IsTrue(board.View.Units.Any(u => u.Nation == group.Nation && u.Kind == group.Kind && u.Location == target),
                group.Nation + " " + group.Kind + " in " + target);
        }

        private static void AssertNothingLit(BoardScreen board, string when)
        {
            CollectionAssert.IsEmpty(board.Map.DraftOverlay.PlacementTargets, when + ": no settlement is lit.");
            CollectionAssert.IsEmpty(board.Map.DraftOverlay.ChosenPlacements, when + ": no settlement is chosen.");
            CollectionAssert.IsEmpty(board.Map.DraftOverlay.OtherPlacements, when + ": nothing is pale.");
            Assert.AreNotEqual(MapRegionEmphasis.PlacementTarget, board.Map.Renderer.CurrentEmphasis("iron-hills"), when);
        }

        // UI-538 (owner, 2026-11-16): a figure taken up lights its own settlements; the settlements only the other figures
        // may take turn pale grey, still and no targets. The figure has two settlements or more, so its click only takes it
        // up (UI-142 would send a figure with one).
        private static async Task<RecruitmentOption> TakeUp(BoardScreen board, MountedFlowClient client, CancellationToken token)
        {
            var recruitment = board.View.Options.First(o => o.Kind == CommandKind.Recruit && o.Enabled).Recruitment;
            var open = recruitment.SelectMany(g => g.Targets).Distinct().ToList();
            var group = recruitment.First(g => g.Targets.Count >= 2 && recruitment.Any(other => other.Targets.Any(t => !g.Targets.Contains(t))));
            PointerGestureEvents.ClickAt(Token(board, group.Nation, group.Kind));
            await Task.Delay(600, token);
            var own = board.Map.DraftOverlay.PlacementTargets.ToList();
            CollectionAssert.AreEquivalent(group.Targets.Distinct(), own, "The taken figure lights its own settlements.");
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
            LayoutTests.Save(client.Panel.targetTexture, "ui539-muster-figure-taken.png");
            return group;
        }

        private static async Task Inspect(BoardScreen board, string region, CancellationToken token)
        {
            MapContourProbe.ClickRegion(board.Map, region);
            await Task.Delay(500, token);
            Assert.AreEqual(region, board.Hover.Panel.Region);
        }

        private static string Title(BoardScreen board) => board.Reinforcements.Root.Q<Label>(className: "title").text;

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
