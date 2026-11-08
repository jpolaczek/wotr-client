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
    // UI-186: the map shows its region names inside their regions, clear of the forces, never under the pointer,
    // and more of them as the view zooms in.
    public sealed class MapRegionNamesTests
    {
        [UnityTest]
        public IEnumerator NamesStandInTheirRegionsClearOfTheForcesAndNeverTakeThePointer()
        {
            var fixture = new BoardFixture(true);
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
                    var map = client.Board.Map;
                    client.Board.Hover.Close();
                    map.Fit();
                    yield return Settle();
                    var label = $"{width} × {scale:0.00}";
                    var fit = Check(map, "fit " + label);
                    LayoutTests.Save(texture, $"ui186-names-fit-{width}-{scale:0.00}.png");
                    var edoras = map.Data.Shapes["edoras"].Anchor;
                    map.FocusOn(new Vector2(edoras[0], edoras[1]), map.FitZoom * 2);
                    yield return Settle();
                    var near = Check(map, "zoomed " + label);
                    LayoutTests.Save(texture, $"ui186-names-zoomed-{width}-{scale:0.00}.png");
                    // On the whole map a name smaller than 8 points stays hidden; the strongholds and cities, drawn larger,
                    // still show on the smallest screen.
                    Assert.GreaterOrEqual(fit, 5, "The whole map names its strongholds and cities · " + label);
                    // UI-530 (owner: figures in the middle, names giving way): a name pushed aside by the places may be
                    // smaller and show only closer in (Edoras's and Westemnet's at 1366 × 768 at 125 %), so at least 8.
                    Assert.GreaterOrEqual(near, 8, "The zoomed view names the regions around it · " + label);
                    // UI-363 (owner report): a small region its forces' zone nearly fills names itself up close.
                    var western = map.Data.Shapes["western-mirkwood"].Anchor;
                    map.FocusOn(new Vector2(western[0], western[1]), map.Camera.MaximumZoom);
                    yield return Settle();
                    Check(map, "western mirkwood " + label);
                    LayoutTests.Save(texture, $"ui363-western-mirkwood-{width}-{scale:0.00}.png");
                    Assert.IsTrue(map.Names.Shown.Any(name => name.name == "map-name-western-mirkwood"),
                        "Western Mirkwood shows its name up close · " + label);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // UI-426 (TABLETOP_FIGURES_UX, "As built (UI-426)"): no name lies on its own settlement's stone — on the whole map
        // and at the closest zoom on every settlement, in the four layouts.
        [UnityTest]
        public IEnumerator NoNameLiesOnItsOwnSettlementAtTheWholeMapOrTheClosestZoom()
        {
            var fixture = new BoardFixture(true);
            var findings = new MatrixFindings();
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
                    var map = client.Board.Map;
                    client.Board.Hover.Close();
                    var label = $"{width} × {scale:0.00}";
                    map.Fit();
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui426-names-fit-{width}-{scale:0.00}.png");
                    findings.Check("whole map " + label, () => TabletopFrameChecks.AssertNamesClearOfTheirGems(map, "whole map " + label));
                    var shown = 0;
                    foreach (var region in map.Data.Regions.Values.Where(region => region.Settlement != null))
                    {
                        var anchor = map.Data.Shapes[region.Id].Anchor;
                        map.FocusOn(new Vector2(anchor[0], anchor[1]), map.Camera.MaximumZoom);
                        yield return null;
                        yield return null;
                        var closest = $"closest on {region.Id} · {label}";
                        findings.Check(closest, () => TabletopFrameChecks.AssertNamesClearOfTheirGems(map, closest));
                        if (map.Names.Shown.Any(name => name.name == "map-name-" + region.Id))
                            shown++;
                        if (region.Id == "helms-deep")
                            LayoutTests.Save(texture, $"ui426-helms-deep-closest-{width}-{scale:0.00}.png");
                    }
                    // UI-530 (owner: figures in the middle, names giving way): a name the places pushed toward its region's
                    // edge may lie past the closest view round its gem on the smallest screen, so at least 29.
                    Assert.GreaterOrEqual(shown, 29, "Up close the settlements keep their names · " + label);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
            findings.AssertNone();
        }

        private static int Check(MapCanvas map, string label)
        {
            var shown = map.Names.Shown.ToList();
            var forces = map.Query<Button>(className: "army").ToList()
                .Where(button => button.resolvedStyle.display == DisplayStyle.Flex).Select(button => button.worldBound).ToList();
            foreach (var name in shown)
            {
                var bounds = name.worldBound;
                Assert.AreEqual(PickingMode.Ignore, name.pickingMode, name.name);
                var picked = name.panel.Pick(bounds.center);
                Assert.IsFalse(picked == name, "A name never takes the pointer · " + name.name);
                Assert.IsTrue(map.worldBound.Contains(bounds.min) && map.worldBound.Contains(bounds.max), name.name + " stays on the map · " + label);
                foreach (var force in forces)
                    Assert.IsFalse(force.Overlaps(bounds), name.name + " lies clear of every force · " + label);
                var region = name.name.Substring("map-name-".Length);
                var centre = map.Camera.ToBoard(map.WorldToLocal(bounds.center));
                Assert.IsTrue(MapContourMath.Contains(map.Data.Shapes[region], centre), name.name + " stands in its region · " + label);
            }
            return shown.Count;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
