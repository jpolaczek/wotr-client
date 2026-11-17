using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-540 (owner, 2026-11-16/17): AlphaWOTR is named and credited on the CPU seat, a room it plays keeps to the base game,
    // a saved game names it, and while it cannot answer the console says the game waits for it.
    public sealed class AlphaWotrSeatTests
    {
        private static BotSeatView Alpha(Side side) =>
            new BotSeatView { Side = side, Status = BotSeatStatus.Ready, Engine = CpuNames.AlphaWotrEngine };

        [Test]
        public void TheSeatNamesAndCreditsAlphaWotrAndTheScriptedCpuKeepsItsName()
        {
            var room = PveRoomTests.Room(Side.FreePeoples);
            room.Shadow = "CPU";
            room.Bot = Alpha(Side.Shadow);
            var screen = new WaitingRoomScreen();
            screen.Update(room);
            var name = screen.Root.Q<Label>("seat-shadow-player");
            Assert.AreEqual("AlphaWOTR", name.text);
            StringAssert.Contains("Sarvesh Sadana", name.tooltip);
            StringAssert.Contains("MIT", name.tooltip);
            room.Bot = new BotSeatView { Side = Side.Shadow, Status = BotSeatStatus.Ready };
            screen.Update(room);
            Assert.AreEqual("CPU", name.text, "A game begun with the scripted CPU is named as before.");
            Assert.AreEqual("", name.tooltip);
        }

        [Test]
        public void ARoomWithAlphaWotrKeepsToTheBaseGame()
        {
            var room = PveRoomTests.Room(Side.FreePeoples);
            room.Bot = Alpha(Side.Shadow);
            foreach (var choice in RulesChoices.Modules(room, true).Concat(RulesChoices.Variants(room, true)))
            {
                Assert.IsFalse(choice.Enabled, choice.Id);
                StringAssert.Contains(CpuNames.BaseGameOnly, choice.Tooltip, choice.Id);
            }
            room.Bot = null;
            Assert.IsFalse(RulesChoices.CpuBaseGame(room));
        }

        [Test]
        public void ASavedGameNamesAlphaWotr()
        {
            var match = new MatchSummary
            {
                MatchId = "m", IsPve = true, You = Side.FreePeoples, Turn = 3, Status = MatchStatus.Waiting,
                Bot = new BotSeatView { Side = Side.Shadow, Status = BotSeatStatus.Unavailable, Engine = CpuNames.AlphaWotrEngine }
            };
            var row = new SavedMatchRow(match, _ => { });
            row.Apply(match);
            StringAssert.Contains("AlphaWOTR", row.Root.Q<Label>(className: "saved-match-heading").text);
            StringAssert.Contains("CPU unavailable", Text(row.Root));
        }

        [Test]
        public void TheConsoleSaysTheGameWaitsForAnUnavailableCpuAndGoesBackToThinking()
        {
            var status = new ConsoleStatus();
            var since = new DateTimeOffset(2026, 11, 17, 9, 30, 0, TimeSpan.Zero);
            status.SetThinking(true, new CpuSeatState { Waiting = true, Since = since, Problem = "Unavailable" });
            Assert.IsTrue(status.Thinking.Shown);
            Assert.IsTrue(status.Thinking.Waiting);
            Assert.AreEqual(CpuNames.Unavailable, status.Thinking.Text.text);
            Assert.IsTrue(status.Thinking.Root.ClassListContains("waiting"));
            StringAssert.Contains("No other CPU plays in its place", status.Thinking.Root.tooltip);
            status.SetThinking(true, null);
            Assert.IsFalse(status.Thinking.Waiting);
            Assert.AreEqual(ConsoleThinking.Words + "…", status.Thinking.Text.text);
            Assert.AreEqual(ConsoleThinking.Tooltip, status.Thinking.Root.tooltip);
            status.SetThinking(false, new CpuSeatState { Waiting = true });
            Assert.IsFalse(status.Thinking.Shown, "The player's own turn shows no CPU state.");
        }

        private static string Text(VisualElement root) => string.Join(" | ", root.Query<Label>().ToList().Select(l => l.text));
    }
}
