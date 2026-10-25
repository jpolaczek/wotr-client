using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class MapTests
    {
        [Test]
        public void EveryRegionHasAnInteriorAnchorAndMatchingContentId()
        {
            var d = BoardPresentation.Load();
            Assert.AreEqual(105, d.Geometry.Regions.Count);
            CollectionAssert.AreEquivalent(d.Board.Regions.Select(r => r.Id), d.Geometry.Regions.Select(r => r.Id));
            foreach (var r in d.Geometry.Regions)
                Assert.IsTrue(MapContourMath.Contains(r, new Vector2(r.Anchor[0], r.Anchor[1])), r.Id);
        }
        [Test]
        public void ZoomKeepsPointerOnSameBoardPoint()
        {
            var camera = new CameraState { Zoom = .5f, Pan = new Vector2(30, 50) };
            var point = new Vector2(500, 300);
            var before = camera.ToBoard(point);
            camera.ZoomAt(point, 1.25f);
            Assert.That(Vector2.Distance(before, camera.ToBoard(point)), Is.LessThan(.001f));
        }
        [Test]
        public void RefreshDoesNotChangeCameraAndSiegeForcesStaySeparate()
        {
            var d = BoardPresentation.Load();
            d.UseStressFixture();
            var map = new MapCanvas(d);
            map.Camera.Zoom = 1.2f;
            map.Camera.Pan = new Vector2(-100, 25);
            map.UpdateData(d);
            Assert.AreEqual(1.2f, map.Camera.Zoom);
            Assert.AreEqual(new Vector2(-100, 25), map.Camera.Pan);
            Assert.AreEqual(2, d.Armies.Count(a => a.RegionId == "helms-deep"));
            Assert.AreEqual(1, d.Armies.Count(a => a.RegionId == "helms-deep" && a.Garrison));
        }

        [Test]
        public void HoverIsSubtleAndNeverOverridesSelectionOrLegalTargets()
        {
            var map = new MapCanvas(BoardPresentation.Load());
            map.SetHovered("osgiliath");
            Assert.AreEqual(MapRegionEmphasis.Hover, map.Renderer.CurrentEmphasis("osgiliath"));
            Assert.AreEqual("", map.Selected);
            Assert.IsEmpty(map.MarkerVisibility.InspectedRegion);
            map.Select("osgiliath");
            Assert.AreEqual(MapRegionEmphasis.Selected, map.Renderer.CurrentEmphasis("osgiliath"));
            map.DraftOverlay.Targets.Add("osgiliath");
            map.Redraw();
            Assert.AreEqual(MapRegionEmphasis.Target, map.Renderer.CurrentEmphasis("osgiliath"));
            map.DraftOverlay.AttackTargets.Add("osgiliath");
            map.Redraw();
            Assert.AreEqual(MapRegionEmphasis.Attack, map.Renderer.CurrentEmphasis("osgiliath"));
            map.SetHovered("not-a-region");
            Assert.IsEmpty(map.Hovered);
        }

        // UI-224: enterable and attackable regions glow; the glow layer repaints only when the targets change.
        [Test]
        public void TargetsGlowAndTheGlowFollowsOnlyChangesOfTheTargets()
        {
            var map = new MapCanvas(BoardPresentation.Load());
            map.Redraw();
            Assert.IsFalse(map.Renderer.TargetsGlowing);
            map.DraftOverlay.Targets.Add("osgiliath");
            map.DraftOverlay.AttackTargets.Add("minas-morgul");
            map.Redraw();
            Assert.IsTrue(map.Renderer.TargetsGlowing);
            map.DraftOverlay.Targets.Clear();
            map.DraftOverlay.AttackTargets.Clear();
            map.Redraw();
            Assert.IsFalse(map.Renderer.TargetsGlowing);
        }

        // UI-323 (owner request): the region the selected force stands in has the steady gold source edge; an attack's
        // source is marked, its target is not; without a draft nothing is.
        [Test]
        public void TheSelectedForcesOwnRegionHasTheSourceEdge()
        {
            var fixture = new Wotr.Fixtures.BoardFixture();
            var map = new MapCanvas(BoardPresentation.Load());
            var draft = new TurnDraft();
            map.DraftOverlay.Apply(fixture.View, draft);
            map.Redraw();
            Assert.IsEmpty(map.Renderer.SourceGlowRegions, "No draft, no source.");
            draft.Choose(CommandKind.MoveArmies, "army");
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "osgiliath", To = "" }));
            map.DraftOverlay.Apply(fixture.View, draft);
            map.Redraw();
            CollectionAssert.AreEquivalent(new[] { "osgiliath" }, map.Renderer.SourceGlowRegions, "The march's source.");
            draft.Clear();
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.AttackArmy });
            draft.Choose(CommandKind.AttackArmy, "army");
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "minas-tirith", To = "osgiliath" }));
            map.DraftOverlay.Apply(fixture.View, draft);
            map.Redraw();
            CollectionAssert.AreEquivalent(new[] { "minas-tirith" }, map.Renderer.SourceGlowRegions, "The attack's source, not its target.");
            draft.Clear();
            map.DraftOverlay.Apply(fixture.View, draft);
            map.Redraw();
            Assert.IsEmpty(map.Renderer.SourceGlowRegions);
        }

        [Test]
        public void TheGlowSetChangesOnlyWhenItsRegionsDo()
        {
            var current = new HashSet<string>();
            Assert.IsTrue(MapTargetGlow.Changed(current, new[] { "a", "b" }));
            Assert.IsFalse(MapTargetGlow.Changed(current, new[] { "b", "a" }), "order does not matter");
            Assert.IsTrue(MapTargetGlow.Changed(current, new[] { "a" }));
            CollectionAssert.AreEquivalent(new[] { "a" }, current);
            Assert.IsTrue(MapTargetGlow.Changed(current, new string[0]));
            Assert.IsEmpty(current);
        }
    }
}
