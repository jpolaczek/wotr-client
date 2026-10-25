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
        }
    }
}
