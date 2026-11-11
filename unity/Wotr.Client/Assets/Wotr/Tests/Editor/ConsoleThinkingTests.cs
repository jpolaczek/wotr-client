using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // Owner (2026-11-11): the console shows that the CPU thinks only while the CPU opponent holds the decision of a live,
    // connected match, and gives the hint its place back afterwards.
    public sealed class ConsoleThinkingTests
    {
        private static PlayerView View(Side you, Side? owner, Side? winner = null) =>
            new PlayerView { You = you, DecisionOwner = owner, Winner = winner };

        [Test]
        public void TheCpuThinksOnlyOverItsOwnDecisionInALiveCpuMatch()
        {
            var waiting = View(Side.FreePeoples, Side.Shadow);
            Assert.IsTrue(ConsoleThinking.Thinking(waiting, true, true, false));
            Assert.IsFalse(ConsoleThinking.Thinking(waiting, false, true, false), "A human opponent is never called the CPU.");
            Assert.IsFalse(ConsoleThinking.Thinking(waiting, true, false, false), "Offline, the connection speaks instead.");
            Assert.IsFalse(ConsoleThinking.Thinking(waiting, true, true, true), "Sending or presenting has its own words.");
            Assert.IsFalse(ConsoleThinking.Thinking(View(Side.FreePeoples, Side.FreePeoples), true, true, false), "The player's turn.");
            Assert.IsFalse(ConsoleThinking.Thinking(View(Side.Shadow, Side.Shadow), true, true, false), "The player's turn.");
            Assert.IsTrue(ConsoleThinking.Thinking(View(Side.Shadow, Side.FreePeoples), true, true, false));
            Assert.IsFalse(ConsoleThinking.Thinking(View(Side.FreePeoples, null), true, true, false), "Nobody decides.");
            Assert.IsFalse(ConsoleThinking.Thinking(View(Side.FreePeoples, Side.Shadow, Side.Shadow), true, true, false), "Game over.");
            Assert.IsFalse(ConsoleThinking.Thinking(null, true, true, false));
        }

        [Test]
        public void TheIndicatorTakesTheHintsPlaceAndGivesItBack()
        {
            var status = new ConsoleStatus();
            status.Hint.text = "Choose a die";
            Assert.IsFalse(status.Thinking.Shown);
            status.SetThinking(true);
            Assert.IsTrue(status.Thinking.Shown);
            Assert.AreEqual(DisplayStyle.None, status.Hint.style.display.value);
            Assert.AreEqual(ConsoleThinking.Words + "…", status.Thinking.Text.text);
            Assert.AreEqual(PickingMode.Ignore, status.Thinking.Root.pickingMode, "The indicator never takes a click.");
            status.SetThinking(false);
            Assert.IsFalse(status.Thinking.Shown);
            Assert.AreEqual(DisplayStyle.Flex, status.Hint.style.display.value);
        }
    }
}
