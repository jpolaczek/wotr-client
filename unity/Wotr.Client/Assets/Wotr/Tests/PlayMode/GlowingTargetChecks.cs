using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-434 (owner, after UI-433): an object a hint asks the player to click glows visibly where it stands. It is
    // displayed, inside the screen and uncovered — the panel picks it or its child at its centre, or the map under a map
    // object drawn without a pointer — and it wears a strong glow whose host pulses: the shared gold ChoiceGlow (a rim of
    // at least 2 points), the map's choice glow, the Reserve's, or the red of a card to discard or a figure to eliminate.
    // An object inside a closed panel is reached through its lit tab (AssertTabLit), then checked in the opened panel.
    internal static class GlowingTargetChecks
    {
        private const int PulseWindowMilliseconds = 1700;
        private static readonly string[] PulseClasses = { ChoiceGlow.PulseClass, BoardEdgeLayout.TabPulseClass };

        public static async Task AssertVisiblyOffered(BoardScreen board, VisualElement element, string label)
        {
            AssertShownUncovered(board, element, label);
            var glow = Glow(element);
            Assert.IsNotEmpty(glow, label + " wears no strong glow; its classes: " + string.Join(" ", element.GetClasses()));
            if (Static(element))
                return;
            Assert.IsTrue(await Pulses(element), label + " (" + glow + ") does not pulse with its host.");
        }

        // A closed host's tab leads to the object: it is shown, uncovered, lit and pulsing.
        public static async Task AssertTabLit(BoardScreen board, VisualElement tab, string label)
        {
            AssertShownUncovered(board, tab, label);
            Assert.IsTrue(tab.ClassListContains("legal-target") || tab.ClassListContains(BoardEdgeLayout.UsableClass),
                label + " is not lit; its classes: " + string.Join(" ", tab.GetClasses()));
            Assert.IsTrue(await Pulses(tab), label + " does not pulse.");
        }

        // A lit region: in the map's lit targets, with uncovered terrain of its own on screen.
        public static void AssertRegionOffered(BoardScreen board, string region)
        {
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(region), region + " is not lit.");
            var point = MapContourProbe.Terrain(board.Map, region);
            Assert.IsTrue(board.Root.worldBound.Contains(point), region + "'s terrain is off the screen at " + point + ".");
        }

        public static void AssertShownUncovered(BoardScreen board, VisualElement element, string label)
        {
            Assert.IsNotNull(element, label + " is not drawn.");
            Assert.IsTrue(Displayed(element), label + " is hidden (itself or a closed host).");
            var screen = board.Root.worldBound;
            var bounds = element.worldBound;
            Assert.IsTrue(bounds.xMin >= screen.xMin - 1 && bounds.yMin >= screen.yMin - 1 && bounds.xMax <= screen.xMax + 1 &&
                bounds.yMax <= screen.yMax + 1, label + " leaves the screen: " + bounds + " of " + screen + ".");
            var picked = element.panel.Pick(bounds.center);
            Assert.IsTrue(Uncovered(board, element, picked), label + " is covered at its centre by " + Describe(picked) + ".");
        }

        // The strong glow the element wears, or "".
        public static string Glow(VisualElement element)
        {
            if (element.ClassListContains(ChoiceGlow.ClassName) && Rim(element, Gold))
                return "gold ChoiceGlow";
            if (element.ClassListContains("legal-target") && Rim(element, Gold))
                return "gold legal-target rim";
            if ((element.ClassListContains("eligible-army") || element.ClassListContains(MapCanvas.SkillOwnerClass)) &&
                Shown(element.Q(className: "army-choice-glow")))
                return "map choice glow";
            if (element.ClassListContains("eligible-token") && Shown(element.Q(className: "reserve-choice-glow")))
                return "Reserve choice glow";
            if (element.ClassListContains("eligible-figure") && Rim(element, Gold))
                return "region panel's gold rim";
            if (element.ClassListContains("eliminate-target") && Rim(element, Red))
                return "red rim";
            if (element.ClassListContains(TableEffectsView.DiscardClass) || element.ClassListContains(HandCard.DiscardClass))
                return "red discard look";
            return "";
        }

        // The red parchment of a card to discard is UI-222's still look; everything else breathes with its host.
        private static bool Static(VisualElement element) =>
            element.ClassListContains(HandCard.DiscardClass) || element.ClassListContains("effect-card-tile");

        // The element or one of its hosts toggles the shared pulse within the window.
        public static async Task<bool> Pulses(VisualElement element)
        {
            var hosts = Hosts(element).ToList();
            var before = hosts.Select(PulseState).ToList();
            for (var waited = 0; waited < PulseWindowMilliseconds; waited += 50)
            {
                await Task.Delay(50);
                if (hosts.Select(PulseState).Where((state, index) => state != before[index]).Any())
                    return true;
            }
            return false;
        }

        private static IEnumerable<VisualElement> Hosts(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                yield return current;
        }

        private static string PulseState(VisualElement host) => string.Join(",", PulseClasses.Where(host.ClassListContains));

        private static bool Uncovered(BoardScreen board, VisualElement element, VisualElement picked)
        {
            if (picked == element || element.Contains(picked))
                return true;
            // A map object drawn without a pointer of its own lies on the canvas, whose click reaches it.
            return board.Map.Contains(element) && (picked == board.Map || board.Map.Contains(picked) && !Windowed(board, picked));
        }

        private static bool Windowed(BoardScreen board, VisualElement picked) =>
            Hosts(picked).Any(host => host != board.Map && host.ClassListContains("region-hover"));

        public static bool Displayed(VisualElement element)
        {
            if (element?.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            return Hosts(element).All(host => host.resolvedStyle.display != DisplayStyle.None &&
                host.resolvedStyle.visibility != Visibility.Hidden && host.resolvedStyle.opacity > 0.05f);
        }

        private static bool Shown(VisualElement element) => element != null && Displayed(element);

        private static bool Rim(VisualElement element, System.Func<Color, bool> colour)
        {
            var style = element.resolvedStyle;
            return style.borderTopWidth >= 2 && style.borderBottomWidth >= 2 && colour(style.borderTopColor) && colour(style.borderBottomColor);
        }

        private static bool Gold(Color colour) => colour.r >= 0.85f && colour.g >= 0.65f && colour.b <= 0.6f && colour.a > 0.3f;

        private static bool Red(Color colour) => colour.r >= 0.75f && colour.g <= 0.45f && colour.b <= 0.4f && colour.a > 0.3f;

        private static string Describe(VisualElement element) =>
            element == null ? "nothing" : element.name + " (" + string.Join(" ", element.GetClasses()) + ") in " + element.parent?.name;
    }
}
