using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-321 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible; the jumps are an option, skip
    // animations"): a panel that comes into view fades in by default and appears at once under Skip animations.
    public sealed class AppearanceTests
    {
        [UnityTest]
        public IEnumerator AnOpeningPanelFadesInByDefault()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var drawer = stage.Board.Nations.Drawer;
            stage.Board.Nations.Open();
            Assert.AreEqual(DisplayStyle.Flex, drawer.style.display.value, "The panel is open at once.");
            yield return null;
            yield return null;
            Assert.Less(drawer.resolvedStyle.opacity, 1f, "It fades in (the left rail's drawer also keeps its own slide).");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(1f, drawer.resolvedStyle.opacity, 0.01f, "It ends fully shown.");
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsAPanelAppearsAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var drawer = stage.Board.Nations.Drawer;
            stage.Board.Nations.Open();
            yield return null;
            yield return null;
            Assert.AreEqual(1f, drawer.resolvedStyle.opacity, "No fade.");
            stage.Board.Nations.Close();
            Assert.AreEqual(DisplayStyle.None, drawer.style.display.value, "UI-322: it also closes at once.");
        }

        // UI-322: closing mirrors opening — the panel fades out where it stands and leaves the layout after it, but it is
        // closed at once for every query and its controls take no click meanwhile.
        [UnityTest]
        public IEnumerator AClosingPanelFadesOutButIsClosedAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var drawer = stage.Board.Nations.Drawer;
            var clicked = false;
            var probe = new Button(() => clicked = true) { name = "closing-probe" };
            probe.style.width = 40;
            probe.style.height = 20;
            drawer.Add(probe);
            stage.Board.Nations.Open();
            yield return new WaitForSecondsRealtime(.4f);
            stage.Board.Nations.Close();
            Assert.IsTrue(Appearance.Closed(drawer), "Closed at once.");
            Assert.AreEqual(DisplayStyle.Flex, drawer.style.display.value, "It still stands while it fades.");
            PointerGestureEvents.ClickAt(probe);
            Assert.IsFalse(clicked, "A closing panel's control takes no click.");
            yield return new WaitForSecondsRealtime(.07f);
            Assert.Less(drawer.resolvedStyle.opacity, .95f, "It fades out…");
            LayoutTests.Save(stage.Texture, "ui322-closing.png");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(DisplayStyle.None, drawer.style.display.value, "…and then leaves.");
            Assert.IsTrue(Appearance.Closed(drawer));
        }

        // A left panel fading out no longer narrows the board's middle: the windows standing there move back as it begins.
        [UnityTest]
        public IEnumerator AClosingLeftPanelNoLongerNarrowsTheMiddle()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var board = stage.Board;
            var drawer = board.Nations.Drawer;
            board.Nations.Open();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(board.Windows.Middle().Value.xMin, Is.EqualTo(drawer.worldBound.xMax).Within(2), "The open panel narrows the middle.");
            var moved = 0;
            board.Windows.MiddleMoved += () => moved++;
            board.Nations.Close();
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, drawer.resolvedStyle.display, "The panel is still fading out…");
            Assert.Greater(moved, 0, "…but the middle moved as the fade began…");
            Assert.That(board.Windows.Middle().Value.xMin, Is.EqualTo(board.Edges.Left.worldBound.xMax).Within(1), "…to the left rail.");
        }
    }
}
