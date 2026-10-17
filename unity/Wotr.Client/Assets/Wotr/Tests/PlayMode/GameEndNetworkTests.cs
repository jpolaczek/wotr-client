using System;
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
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-169 (SIGNALS_UX GE01–GE03) on the TEST-07 Game end scenarios with the real server: one command ends the
    // game, both players get the end screen with their own title; View board closes it and the console's GAME OVER
    // opens it again, Game log opens the Log, Main menu leaves through the board menu; a finished game opened again
    // shows the screen at once; the window fits four layouts without scrolling.
    public sealed class GameEndNetworkTests
    {
        [UnityTest]
        public IEnumerator TheRingDestroyedEndsTheGameOnBothScreensAndItsButtonsWork() => MatchTestRoom.Wait(RingDestroyed());

        [UnityTest]
        public IEnumerator AShadowMilitaryVictoryShowsBothSidesTheirTitleAndFitsFourLayouts() => MatchTestRoom.Wait(ShadowMilitary());

        [UnityTest]
        public IEnumerator AFreePeoplesMilitaryVictoryReachesBothWithTheOpponentsNotice() => MatchTestRoom.Wait(FreePeoplesMilitary());

        // UI-170 (SG08): the last scenario of TEST-07 end to end — the Free Peoples' last action reaches the Shadow as
        // a notice with its result, the Victory check ends the game, and no ribbon follows a finished game.
        private static async Task FreePeoplesMilitary()
        {
            var session = await TestScenarioRoom.Start("fp-military");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var die = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
                await session.Send(Side.FreePeoples, CommandKind.DiscardDie, new CommandPayload { DieId = die.Id });
                await session.Until(() => fp.Board.GameEnd.IsOpen && sh.Board.GameEnd.IsOpen);
                AssertScreen(fp.Board, "VICTORY", "Free Peoples win · Military victory", "free-victory");
                AssertScreen(sh.Board, "DEFEAT", "Free Peoples win · Military victory", "shadow-defeat");
                StringAssert.Contains("Free Peoples 4/4", fp.Board.GameEnd.State.Summary[1]);
                await session.Until(() => sh.Activity.Model.Groups.Any(group => group.Result.Header.StartsWith("Die discarded")));
                Assert.AreEqual(0, fp.Board.Ribbon.Shown + sh.Board.Ribbon.Shown, "A finished game gives no turn ribbon.");
                StringAssert.StartsWith("GAME OVER", sh.Board.Console.Status.Headline.text);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task RingDestroyed()
        {
            var session = await TestScenarioRoom.Start("fp-ring-destroyed");
            try
            {
                await RingDestroyed(session);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task RingDestroyed(TestScenarioRoom session)
        {
            var fp = session.Client(Side.FreePeoples);
            var sh = session.Client(Side.Shadow);
            // AUDIO-07 (AU16): the live end sounds each player's own chord once.
            var fpCues = new List<SoundCue>();
            var shCues = new List<SoundCue>();
            fp.Interface.Sounds.Policy.Played += fpCues.Add;
            sh.Interface.Sounds.Policy.Played += shCues.Add;
            var die = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
            await session.Send(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = die.Id });
            await session.Until(() => fp.Board.GameEnd.IsOpen && sh.Board.GameEnd.IsOpen);
            await session.Settle();
            CollectionAssert.AreEqual(new[] { SoundCue.Victory }, fpCues, "The winner hears the victory once, nothing else.");
            CollectionAssert.AreEqual(new[] { SoundCue.Defeat }, shCues, "The other player hears the defeat once.");
            AssertScreen(fp.Board, "VICTORY", "Free Peoples win · The Ring is destroyed", "free-victory");
            AssertScreen(sh.Board, "DEFEAT", "Free Peoples win · The Ring is destroyed", "shadow-defeat");
            StringAssert.StartsWith("Fellowship · Crack of Doom", fp.Board.GameEnd.State.Summary[2]);

            var board = fp.Board;
            CardNetworkTests.Click(board.GameEnd.Root.Q<Button>("game-end-view-board"));
            await session.Settle();
            Assert.IsFalse(board.GameEnd.IsOpen, "View board leaves the board to read.");
            StringAssert.StartsWith("GAME OVER", board.Console.Status.Headline.text);
            CardNetworkTests.Click(board.Console.Status.Plaque);
            await session.Settle();
            Assert.IsTrue(board.GameEnd.IsOpen, "The console's GAME OVER opens the end again.");
            CardNetworkTests.Click(board.GameEnd.Root.Q<Button>("game-end-view-board"));
            await session.Settle();
            // UI-233: the console's own button brings back the result, the log and Save replay.
            var reopen = board.Console.Status.Options.Q<Button>(GameEndConsole.Name);
            Assert.AreEqual(DisplayStyle.Flex, board.Console.Status.Options.resolvedStyle.display);
            Assert.AreEqual("Results & replay", reopen.text);
            CardNetworkTests.Click(reopen);
            await session.Settle();
            Assert.IsTrue(board.GameEnd.IsOpen, "Results & replay opens the end again.");
            CardNetworkTests.Click(board.GameEnd.Root.Q<Button>("game-end-log"));
            await session.Settle();
            Assert.IsTrue(board.Log.IsOpen, "Game log opens the Log.");
            Assert.IsFalse(board.GameEnd.IsOpen);
            var menus = 0;
            sh.Board.Menu.GamesRequested += _ => menus++;
            CardNetworkTests.Click(sh.Board.GameEnd.Root.Q<Button>("game-end-menu"));
            await session.Settle();
            Assert.AreEqual(1, menus, "Main menu leaves through the board menu's Games.");
            Assert.AreEqual(1, fpCues.Count(c => c == SoundCue.Victory), "View board and GAME OVER do not repeat the chord.");

            // GE03: opening the finished game again shows the end at once, without its chord.
            using var again = await session.Reopen(Side.FreePeoples);
            var againCues = new List<SoundCue>();
            again.Interface.Sounds.Policy.Played += againCues.Add;
            await session.Until(() => again.Board.GameEnd.IsOpen);
            await session.Settle();
            AssertScreen(again.Board, "VICTORY", "Free Peoples win · The Ring is destroyed", "free-victory");
            Assert.IsFalse(againCues.Contains(SoundCue.Victory), "A finished game opened again is silent.");
        }

        private static async Task ShadowMilitary()
        {
            var session = await TestScenarioRoom.Start("sh-military");
            try
            {
                await ShadowMilitary(session);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task ShadowMilitary(TestScenarioRoom session)
        {
            var fp = session.Client(Side.FreePeoples);
            var sh = session.Client(Side.Shadow);
            var die = sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
            await session.Send(Side.Shadow, CommandKind.DiscardDie, new CommandPayload { DieId = die.Id });
            await session.Until(() => fp.Board.GameEnd.IsOpen && sh.Board.GameEnd.IsOpen);
            AssertScreen(sh.Board, "VICTORY", "Shadow win · Military victory", "shadow-victory");
            AssertScreen(fp.Board, "DEFEAT", "Shadow win · Military victory", "free-defeat");
            StringAssert.Contains("Shadow 10/10", sh.Board.GameEnd.State.Summary[1]);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        fp.Panel.targetTexture = texture;
                        fp.Panel.scale = scale;
                        await session.Settle();
                        AssertFits(fp.Board, $"{width}-{scale:0.00}");
                    }
                    finally
                    {
                        fp.Panel.targetTexture = fp.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
        }

        private static void AssertScreen(BoardScreen board, string title, string subtitle, string scene)
        {
            var root = board.GameEnd.Root;
            Assert.AreEqual(title, root.Q<Label>("game-end-title").text);
            Assert.AreEqual(subtitle, root.Q<Label>("game-end-subtitle").text);
            Assert.IsTrue(root.Q("game-end-scene").ClassListContains("game-end-scene-" + scene), scene);
            Assert.AreEqual(4, board.GameEnd.State.Summary.Count);
        }

        // The window, its scene and every button stand inside the board, with nothing to scroll.
        private static void AssertFits(BoardScreen board, string label)
        {
            var root = board.GameEnd.Root;
            Assert.IsEmpty(root.Query<ScrollView>().ToList(), label);
            var bounds = board.Root.worldBound;
            foreach (var element in new[] { root.Q("game-end-window"), root.Q("game-end-scene"), root.Q("game-end-title-plate") }
                .Concat(root.Query<Button>().ToList()).Concat(root.Query<Label>().ToList()))
            {
                Assert.Greater(element.worldBound.width, 0, element.name + " · " + label);
                Assert.GreaterOrEqual(element.worldBound.xMin + 1, bounds.xMin, element.name + " · " + label);
                Assert.LessOrEqual(element.worldBound.xMax, bounds.xMax + 1, element.name + " · " + label);
                Assert.GreaterOrEqual(element.worldBound.yMin + 1, bounds.yMin, element.name + " · " + label);
                Assert.LessOrEqual(element.worldBound.yMax, bounds.yMax + 1, element.name + " · " + label);
            }
            // UI-233: the buttons stand inside the window's frame, and the title clear of the plate's ornaments.
            var frame = root.Q("game-end-window").worldBound;
            foreach (var button in root.Query<Button>().ToList())
            {
                Assert.GreaterOrEqual(button.worldBound.xMin + 1, frame.xMin + 8, button.name + " inside the frame · " + label);
                Assert.LessOrEqual(button.worldBound.xMax, frame.xMax - 8 + 1, button.name + " inside the frame · " + label);
            }
            var title = root.Q<Label>("game-end-title");
            var text = title.MeasureTextSize(title.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(text.x, root.Q("game-end-title-plate").contentRect.width + 1, "The title keeps clear of the ornaments · " + label);
        }
    }
}
