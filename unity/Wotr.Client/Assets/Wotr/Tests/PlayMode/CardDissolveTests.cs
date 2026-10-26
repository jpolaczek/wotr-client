using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-326: a discard's two card backs dissolve once, where the cards left; a card the server kept is forgotten, a quiet
    // view plays nothing, and Skip animations draws none.
    public sealed class CardDissolveTests
    {
        [UnityTest]
        public IEnumerator AMarkedCardDissolvesOnceWhenItLeavesAndIsForgottenWhenItStays()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var dissolve = stage.Board.Dissolve;
            var place = stage.Board.Phase;
            dissolve.Mark("kept", place, 10);
            dissolve.Mark("discarded", place, 10);
            dissolve.Settle(View(10, "kept", "discarded"), false);
            Assert.AreEqual(0, dissolve.Shown, "Nothing left the hand yet.");
            dissolve.Settle(View(11, "kept"), false);
            Assert.AreEqual(2, dissolve.Shown, "The discarded card's two backs.");
            yield return new WaitForSecondsRealtime((CardDissolve.Milliseconds + 200) / 1000f);
            Assert.AreEqual(0, dissolve.Shown, "They are gone.");
            dissolve.Settle(View(12), false);
            Assert.AreEqual(0, dissolve.Shown, "The card the server kept at revision 11 was forgotten.");
            dissolve.Mark("quiet", place, 12);
            dissolve.Settle(View(13), true);
            Assert.AreEqual(0, dissolve.Shown, "A quiet view plays nothing.");
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsNothingIsDrawn()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            stage.Board.Dissolve.Play(stage.Board.Phase.worldBound);
            Assert.AreEqual(0, stage.Board.Dissolve.Shown);
        }

        private static PlayerView View(long revision, params string[] hand) =>
            new PlayerView { Revision = revision, Hand = new System.Collections.Generic.List<string>(hand) };
    }
}
