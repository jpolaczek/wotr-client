using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-169 (SIGNALS_UX D, GE01–GE03): the end of the game from each player's view and the screen's own rules.
    public sealed class GameEndStateTests
    {
        [Test]
        public void TheTitleIsThePlayersOwnResultAndTheSubtitleNamesWinnerAndReason()
        {
            Assert.IsNull(GameEndState.For(new PlayerView { You = Side.FreePeoples }));
            var free = GameEndState.For(Ended(Side.FreePeoples, Side.FreePeoples, VictoryCondition.RingDestroyed));
            Assert.AreEqual("VICTORY", free.Title);
            Assert.AreEqual("Free Peoples win · The Ring is destroyed", free.Subtitle);
            Assert.AreEqual("ring-destroyed", free.Scene);
            var shadow = GameEndState.For(Ended(Side.Shadow, Side.FreePeoples, VictoryCondition.RingDestroyed));
            Assert.AreEqual("DEFEAT", shadow.Title);
            Assert.AreEqual(free.Subtitle, shadow.Subtitle, "Both players read the same result.");
            Assert.AreEqual("Shadow win · The Ring-bearers are corrupted",
                GameEndState.For(Ended(Side.Shadow, Side.Shadow, VictoryCondition.RingCorrupted)).Subtitle);
            Assert.AreEqual("ring-corrupted", GameEndState.For(Ended(Side.FreePeoples, Side.Shadow, VictoryCondition.RingCorrupted)).Scene);
            Assert.AreEqual("military-shadow", GameEndState.For(Ended(Side.FreePeoples, Side.Shadow, VictoryCondition.ShadowMilitary)).Scene);
            Assert.AreEqual("military-free", GameEndState.For(Ended(Side.Shadow, Side.FreePeoples, VictoryCondition.FreePeoplesMilitary)).Scene);
        }

        [Test]
        public void TheSummaryTellsTheTurnThePointsTheFellowshipAndItsCompanions()
        {
            var view = Ended(Side.FreePeoples, Side.FreePeoples, VictoryCondition.RingDestroyed);
            view.Turn = 7;
            view.Fellowship = new FellowshipView { MordorStep = 5, Corruption = 6, CompanionCount = 2 };
            view.MilitaryVictory.Add(new MilitaryVictoryView { Side = Side.FreePeoples, Points = 1, Threshold = 4 });
            view.MilitaryVictory.Add(new MilitaryVictoryView { Side = Side.Shadow, Points = 6, Threshold = 10 });
            CollectionAssert.AreEqual(new[]
            {
                "Turn 7",
                "Victory points · Free Peoples 1/4 · Shadow 6/10",
                "Fellowship · Crack of Doom · Corruption 6/12",
                "Companions in the Fellowship · 2"
            }, GameEndState.For(view).Summary);
            view.Fellowship = new FellowshipView { Progress = 3, Corruption = 12 };
            Assert.AreEqual("Fellowship · Progress 3 · Corruption 12/12", GameEndState.For(view).Summary[2]);
            view.Fellowship.MordorStep = 1;
            Assert.AreEqual("Fellowship · Mordor step 1 · Corruption 12/12", GameEndState.For(view).Summary[2]);
        }

        [Test]
        public void TheScreenOpensOncePerMatchAndStaysClosedUntilAskedAgain()
        {
            var screen = new GameEndScreen();
            screen.Apply(new PlayerView { MatchId = "m", You = Side.Shadow });
            Assert.IsFalse(screen.IsOpen, "No winner, no screen.");
            var ended = Ended(Side.Shadow, Side.FreePeoples, VictoryCondition.FreePeoplesMilitary);
            screen.Apply(ended);
            Assert.IsTrue(screen.IsOpen);
            Assert.AreEqual("DEFEAT", screen.Root.Q<Label>("game-end-title").text);
            Assert.IsTrue(screen.Root.ClassListContains("lost"));
            Assert.IsTrue(screen.Root.Q("game-end-emblem").ClassListContains("side-free"));
            Assert.IsTrue(screen.Root.Q("game-end-scene").ClassListContains("game-end-scene-military-free"));
            foreach (var name in new[] { "game-end-view-board", "game-end-log", "game-end-menu" })
                Assert.IsNotNull(screen.Root.Q<Button>(name), name);
            screen.Close();
            screen.Apply(ended);
            Assert.IsFalse(screen.IsOpen, "View board keeps the board in view until the console asks again.");
            screen.Open();
            Assert.IsTrue(screen.IsOpen);
            var other = Ended(Side.Shadow, Side.Shadow, VictoryCondition.ShadowMilitary);
            other.MatchId = "another";
            screen.Close();
            screen.Apply(other);
            Assert.IsTrue(screen.IsOpen, "Another finished match opens it again.");
            Assert.AreEqual("VICTORY", screen.Root.Q<Label>("game-end-title").text);
        }

        [Test]
        public void TheConsoleSaysGameOverAndTheStatusOpensTheEnd()
        {
            var state = TurnConsoleState.For(Ended(Side.FreePeoples, Side.Shadow, VictoryCondition.ShadowMilitary), new TurnDraft(),
                new ActionDieContext(), id => id);
            StringAssert.StartsWith("GAME OVER · Shadow win", state.Headline);
            StringAssert.Contains("Click the status", state.Guidance);
        }

        private static PlayerView Ended(Side you, Side winner, VictoryCondition victory) => new PlayerView
        {
            MatchId = "m", You = you, Turn = 4, Winner = winner, Victory = victory
        };
    }
}
