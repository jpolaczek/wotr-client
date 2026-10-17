using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-101: the sovereign portraits leave the map for a Sovereigns section of the Characters panel. The tab tells
    // a portrait is a legal target, and an inspector opened while the panel is rolled up stands beside the tab.
    public sealed class SovereignSectionLayoutTests
    {
        private const string Dain = "kings.sovereign.dain";

        [UnityTest]
        public IEnumerator SovereignsLiveInTheCharactersPanelAndTheTabMarksTheirTargets() => Wait(Section());

        private static async Task Section()
        {
            using var room = await Connect("ui101-sovereigns", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            Assert.IsTrue(board.Characters.Root.Contains(board.Sovereigns.Root), "The portraits are part of the Characters panel.");
            Assert.IsFalse(Visible(board.Sovereigns.Root), "No sovereign buttons hang over the map.");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        room.Fp.Panel.targetTexture = texture;
                        room.Fp.Panel.scale = scale;
                        board.Windows.Clear();
                        await room.Settle();
                        await CheckSection(room, board, texture, $"{width}-{scale:0.00}");
                    }
                    finally
                    {
                        board.Characters.Close();
                        room.Fp.Panel.targetTexture = room.Fp.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
            await CheckTarget(room, board);
            Assert.AreEqual(revision, board.View.Revision, "Opening portraits and choosing a target sends no command.");
        }

        private static async Task CheckSection(MatchTestRoom room, BoardScreen board, RenderTexture texture, string label)
        {
            await BoardNavigation.OpenCharacters(board);
            var section = board.Characters.Root.Q("characters-section-Sovereigns");
            Assert.IsTrue(Visible(section), label);
            Assert.AreEqual("Sovereigns", section.Q<Label>().text, label);
            var portraits = board.Sovereigns.Root.Query<Button>().ToList();
            Assert.AreEqual(board.View.Sovereigns.Count, portraits.Count, label);
            Inside(board.Characters.Root.worldBound, board.Root.worldBound, label);
            foreach (var portrait in portraits)
            {
                Assert.IsTrue(Visible(portrait), portrait.name + " · " + label);
                Inside(portrait.worldBound, board.Characters.Root.worldBound, portrait.name + " · " + label);
                var picked = portrait.panel.Pick(portrait.worldBound.center);
                Assert.IsTrue(picked == portrait || portrait.Contains(picked), portrait.name + " covered by " + picked?.name + " · " + label);
            }
            Assert.AreEqual(DisplayStyle.None, board.Characters.Root.Q<Label>("characters-empty").resolvedStyle.display, label);
            LayoutTests.Save(texture, $"ui101-sovereigns-{label}.png");
            board.Characters.Close();
            await room.Settle();
        }

        private static async Task CheckTarget(MatchTestRoom room, BoardScreen board)
        {
            var option = board.View.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(Dain));
            Assert.IsFalse(board.Characters.Tab.ClassListContains("legal-target"), "No target before the die is chosen.");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(option.EligibleDice.First()));
            await room.Settle();
            Assert.IsTrue(board.Characters.Tab.ClassListContains("legal-target"), "The rolled-up tab tells that a sovereign is a target.");
            var portrait = await BoardNavigation.Sovereign(board, Dain);
            Assert.IsTrue(portrait.ClassListContains("legal-target"));
            PointerGestureEvents.ClickAt(portrait);
            await room.Settle();
            Assert.IsTrue(board.Sovereigns.IsOpen);
            Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
            Assert.AreNotEqual(DisplayStyle.None, board.Characters.Root.resolvedStyle.display,
                "The panel stays unrolled while the sovereign it opened is inspected.");
            board.Characters.Close();
            board.Sovereigns.Position();
            await room.Settle();
            var inspector = board.Sovereigns.Inspector.worldBound;
            var tab = board.Characters.Tab.worldBound;
            Inside(inspector, board.Root.worldBound, "inspector");
            Assert.IsFalse(inspector.Overlaps(tab), "The inspector leaves the tab readable.");
            Assert.Less(Mathf.Max(0, inspector.xMin - tab.xMax), 160, "The inspector stands beside the Characters tab.");
        }

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }

        private static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, label);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1, label);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, label);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, label);
        }
    }
}
