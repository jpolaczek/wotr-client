using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-188: a region with nothing but its name and state opens a small window sized to its text; a region with
    // forces keeps its fixed window.
    public sealed class RegionNameOnlyTests
    {
        [UnityTest]
        public IEnumerator AQuietRegionOpensASmallWindowAndAnOccupiedOneKeepsItsLayout()
        {
            var fixture = new BoardFixture();
            var quiet = fixture.Data.Regions.Values.First(r => r.Settlement == null &&
                fixture.View.Units.All(u => u.Location != r.Id) && fixture.View.Characters.All(c => c.Location != r.Id) &&
                fixture.View.Fellowship.RegionId != r.Id).Id;
            foreach (var scale in new[] { 1f, 1.25f })
            {
                using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                client.Panel.scale = scale;
                client.Apply(fixture.View);
                yield return Settle();
                var hover = client.Board.Hover;
                hover.Open(quiet);
                yield return Settle();
                var panel = hover.Panel.Root;
                var label = quiet + " · " + scale;
                Assert.IsTrue(panel.ClassListContains(RegionHoverPanel.NameOnlyClass), label);
                var title = panel.Q<Label>(className: "title");
                var state = panel.Q<Label>("hover-region-state");
                var text = Mathf.Max(title.layout.width, state.layout.width);
                Assert.LessOrEqual(panel.layout.width, text + 24, "The window is as wide as its text · " + label);
                Assert.Less(panel.layout.width, 240, "Smaller than the old fixed window · " + label);
                var parts = string.Join(", ", panel.Children().Where(c => c.resolvedStyle.display == DisplayStyle.Flex)
                    .Select(c => (c.name.Length > 0 ? c.name : string.Join(".", c.GetClasses())) + ":" + c.layout.height + "+" +
                        (c.resolvedStyle.marginTop + c.resolvedStyle.marginBottom)));
                Assert.Less(panel.layout.height, 60, "Two short lines, no empty space · " + label + " · " + parts);
                Assert.AreEqual(DisplayStyle.None, panel.Q<Label>("hover-empty").resolvedStyle.display, label);
                foreach (var line in new[] { title, state })
                {
                    var measured = line.MeasureTextSize(line.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(measured.x, line.contentRect.width + 1, "Not cut · " + line.text);
                }
                LayoutTests.Save(client.Viewport, $"ui188-name-only-{scale:0.00}.png");
                hover.Open("minas-tirith");
                yield return Settle();
                Assert.IsFalse(panel.ClassListContains(RegionHoverPanel.NameOnlyClass), "A region with forces keeps its window · " + label);
                Assert.GreaterOrEqual(panel.layout.width, 240, label);
                hover.Close();
            }
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
