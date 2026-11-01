using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-437: both columns of the Battle window keep their rows level — the force rows start together, and the statistics,
    // the ledge of figures outside the fight, the Combat cards, the roll headings and the dice rows stand at one height.
    internal static class BattleRowChecks
    {
        public const float Tolerance = 1f;

        public static void Level(VisualElement tray, string label, bool rolled)
        {
            var attacker = tray.Q("battle-attacker");
            var defender = tray.Q("battle-defender");
            Assert.IsNotNull(attacker, label);
            Assert.IsNotNull(defender, label);
            Assert.IsFalse(attacker.ClassListContains("battle-force-choosing") || defender.ClassListContains("battle-force-choosing"),
                label + ": no column chooses figures");
            SameTop(attacker.Q("battle-force-row"), defender.Q("battle-force-row"), label + ": force rows");
            SameTop(attacker.Q("battle-statistics"), defender.Q("battle-statistics"), label + ": statistics");
            SameTop(Shelf(attacker), Shelf(defender), label + ": the ledge of figures outside the fight", optional: true);
            SameTop(attacker.Q("battle-card-attacker"), defender.Q("battle-card-defender"), label + ": cards");
            if (!rolled)
                return;
            SameTop(attacker.Q(className: "battle-roll-heading"), defender.Q(className: "battle-roll-heading"), label + ": roll headings");
            SameTop(attacker.Q(className: "battle-dice"), defender.Q(className: "battle-dice"), label + ": dice rows");
        }

        private static VisualElement Shelf(VisualElement column) =>
            column.Q("battle-garrison-shelf") ?? column.Q("battle-garrison-shelf-reserved");

        public static void SameTop(VisualElement first, VisualElement second, string label, bool optional = false)
        {
            if (optional && first == null && second == null)
                return;
            Assert.IsNotNull(first, label);
            Assert.IsNotNull(second, label);
            Assert.Greater(first.worldBound.height, 0, label);
            Assert.That(second.worldBound.yMin, Is.EqualTo(first.worldBound.yMin).Within(Tolerance), label);
        }
    }
}
