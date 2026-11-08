using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-532 (owner, 2026-11-08: "more symbols, a small graphic as in Characters, less text, more useful information,
    // less empty space"; the owner chose a picture of the place with emblems): Minas Tirith's window shows the place's
    // picture beside its name and a line of symbols — the settlement's gem, Gondor's emblem, its Victory points — the
    // force's header its strength and leadership as icons and numbers, each nation's units after its emblem, and nothing
    // runs past the window, at 1366 × 768 and 1920 × 1080, 100 and 125 %.
    public sealed class RegionWindowDesignTests
    {
        [UnityTest]
        public IEnumerator MinasTirithsWindowSpeaksInSymbolsAtFourLayouts()
        {
            var fixture = new BoardFixture(true);
            var units = fixture.View.Units.Where(u => u.Location == "minas-tirith" && u.Side == Side.FreePeoples && !u.Garrison).ToList();
            var characters = fixture.View.Characters.Where(c => c.Location == "minas-tirith" && c.Side == Side.FreePeoples && !c.Garrison).ToList();
            var strength = units.Count(u => u.Kind == UnitKind.Regular || u.Kind == UnitKind.Elite);
            var leadership = units.Count(u => u.Kind == UnitKind.Leader || u.Kind == UnitKind.Nazgul) + characters.Sum(c => c.Leadership);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    client.Apply(fixture.View);
                    yield return Settle();
                    var label = $"{width} × {scale:0.00}";
                    client.Board.Hover.Open("minas-tirith");
                    yield return Settle();
                    var panel = client.Board.Hover.Panel.Root;
                    LayoutTests.Save(texture, $"ui532-minas-tirith-{width}-{scale:0.00}.png");
                    var place = panel.Q("hover-place");
                    Assert.IsNotNull(place, "The place's picture · " + label);
                    Assert.IsNotNull(place.resolvedStyle.backgroundImage.texture, "The picture shows · " + label);
                    Assert.IsNotNull(panel.Q("hover-region-gem"), "The settlement's gem · " + label);
                    Assert.IsNotNull(panel.Q(className: "nation-emblem-gondor"), "Gondor's emblem · " + label);
                    Assert.AreEqual("2", panel.Q("hover-region-points").Q<Label>().text, "Minas Tirith's Victory points · " + label);
                    StringAssert.Contains("Stronghold", panel.Q("hover-region-facts").tooltip, label);
                    var force = panel.Q("hover-force-FreePeoples-field");
                    Assert.AreEqual(strength.ToString(), force.Q("hover-stat-strength").Q<Label>().text, "Strength · " + label);
                    Assert.AreEqual(leadership.ToString(), force.Q("hover-stat-leadership").Q<Label>().text, "Leadership · " + label);
                    Assert.AreEqual("Field", force.Q<Label>("hover-force-place").text, label);
                    foreach (var nation in units.Select(u => u.Nation).Distinct())
                        Assert.IsNotNull(force.Q("hover-nation-" + nation), nation + "'s emblem leads its units · " + label);
                    foreach (var element in panel.Query<VisualElement>().ToList().Where(e => e.resolvedStyle.display == DisplayStyle.Flex &&
                                 e.worldBound.width > 0 && e.panel != null && e.visible))
                    {
                        Assert.GreaterOrEqual(element.worldBound.xMin + 1, panel.worldBound.xMin, element.name + " · " + label);
                        Assert.LessOrEqual(element.worldBound.xMax, panel.worldBound.xMax + 1, element.name + " · " + label);
                    }
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
