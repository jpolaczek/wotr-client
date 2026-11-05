using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-407: one layout of the carried-figure frames on a fresh server test scene. The side chooses its Army die and
    // presses its largest army that can march: carried over its target at both levels (the miniature under the pointer,
    // the target lit, the landing mark on the place it would take, the caption above it), held at the map's edge until
    // the map pans under it, and dropped on the target, so the real server takes the march: its own seat flies it on from
    // where it was dropped, the opponent's from its force's place, each between the two stand points.
    internal sealed class TabletopCarryScene : IDisposable
    {
        // Longer than the lift pose's and the glide home's transitions.
        private const int Transition = 450;
        private const float DragStep = 9;
        private readonly TestScenarioRoom room;
        private readonly Side side;
        private readonly (int Width, float Scale) layout;
        private readonly MatrixFindings findings;
        private readonly MountedMatchClient client;
        private readonly MountedMatchClient observer;
        private readonly MatrixViewport own;
        private readonly MatrixViewport other;
        private readonly ArmyOption army;
        private readonly ArmyTargetOption target;
        private readonly string die;

        public TabletopCarryScene(TestScenarioRoom room, Side side, (int Width, float Scale) layout, MatrixFindings findings)
        {
            this.room = room;
            this.side = side;
            this.layout = layout;
            this.findings = findings;
            client = room.Client(side);
            observer = room.Client(side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples);
            var view = client.Board.View;
            (army, target) = Movable(view);
            die = ArmyDie(view);
            own = new MatrixViewport(client, layout);
            other = new MatrixViewport(observer, layout);
        }

        private BoardScreen Board => client.Board;
        private MapCanvas Map => client.Board.Map;
        private string SourceName => "army-" + army.RegionId + "-" + side;
        private Button Source => Map.Q<Button>(SourceName);

        public async Task Run(bool export)
        {
            if (export)
                DragProfileInput.Export(side, Board.View);
            TabletopAcceptanceMatrixTests.Quiet(Board);
            TabletopAcceptanceMatrixTests.Quiet(observer.Board);
            await Settle();
            ArmyGestures.Die(client, die);
            await Settle();
            Assert.AreEqual(die, Board.ActionDie.DieId, "The Army die is in hand.");
            await Carried(true);
            await Carried(false);
            await EdgePan();
            await Flight();
        }

        // The army carried over its target: under the pointer, the target lit, the landing mark and the caption. UI-428:
        // its feet (its shadow) on the place it would take, where the landing mark lay under it (UI-407's defect 5).
        private async Task Carried(bool near)
        {
            FrameMarch(Map, near);
            await Settle();
            var label = TabletopAcceptanceMatrixTests.Label("carried", side, layout, near);
            var feet = FigureHandTests.World(Map, FigureMotionTests.StandFeet(Map, SourceName));
            var (piece, press, pointer) = await Lift(label);
            var lifted = piece.Q("hand-lift").worldBound.center;
            var to = press + FigureHandTests.World(Map, LandingPlace(Map)) - feet;
            MapContourProbe.Carry(Board, to);
            await Settle();
            own.Save(TabletopAcceptanceMatrixTests.Name("carried", side, layout, near));
            findings.Check(label, () => AssertCarried(piece, lifted + to - pointer, label));
            await PutBack(to, label);
        }

        private void AssertCarried(VisualElement piece, Vector2 expected, string label)
        {
            BoardInteractionTests.AssertUnder(piece, expected, "The carried miniature stays under the pointer · " + label);
            Assert.AreEqual(target.RegionId, Map.DragTarget, "The region under it is the drop target · " + label);
            Assert.AreEqual(MapRegionEmphasis.CarriedTarget, Map.Renderer.CurrentEmphasis(target.RegionId), "It lights · " + label);
            var ring = Map.Q("landing-mark");
            Assert.AreEqual(DisplayStyle.Flex, ring.resolvedStyle.display, "The landing mark shows · " + label);
            Assert.IsTrue(ring.ClassListContains("landing-move"), "Green for a move · " + label);
            FigureHandTests.AssertOnPlace(Map, ring, LandingPlace(Map), "It lies on the place the army would take · " + label);
            TabletopFrameChecks.AssertLandingMarkClear(Map, label);
            var caption = Map.Q<Label>("landing-caption");
            Assert.AreEqual(DisplayStyle.Flex, caption.resolvedStyle.display, label);
            Assert.LessOrEqual(caption.worldBound.yMax, FigureHandTests.RaisedTop(piece) + .5f,
                "The caption hangs above the carried figure · " + label);
            Assert.IsTrue(Map.worldBound.Overlaps(piece.Q("hand-lift").worldBound), "The carried figure is on the map · " + label);
            Assert.IsNull(Map.Q("army-ghost"), "No translucent copy · " + label);
            Assert.AreEqual(DisplayStyle.None, Board.Root.Q(className: "drag-ghost").resolvedStyle.display, "No text label · " + label);
            if (piece.Q<Label>("army-count")?.text == army.UnitIds.Count.ToString())
                Assert.IsTrue(Source.ClassListContains("lifted-away"), "The whole army is lifted: its base is empty · " + label);
        }

        // Held in an edge zone, the map pans under the carried army after the dwell; UI-444: the army stays whole on the
        // screen, above the HUD, as near the pointer as the edge lets it.
        private async Task EdgePan()
        {
            FrameMarch(Map, true);
            await Settle();
            var label = $"edge-pan {TabletopAcceptanceMatrixTests.Seat(side)} {layout.Width} × {layout.Scale:0.00}";
            var (piece, _, pointer) = await Lift(label);
            var lifted = piece.Q("hand-lift").worldBound.center;
            var edge = PanningEdge(label);
            var clock = Stopwatch.StartNew();
            MapContourProbe.Carry(Board, edge);
            var pan = Map.Camera.Pan;
            await Task.Delay(120);
            var resting = Map.Camera.Pan;
            var dwelt = clock.Elapsed.TotalSeconds;
            await Task.Delay(600);
            own.Save(TabletopAcceptanceMatrixTests.Name("edge-pan", side, layout));
            findings.Check(label, () =>
            {
                if (dwelt < MapEdgePan.DragDwellSeconds - .05)
                    Assert.AreEqual(pan, resting, "Within the dwell the map stays · " + label);
                Assert.Greater(Vector2.Distance(pan, Map.Camera.Pan), 20, "After the dwell the map pans · " + label);
                TabletopFrameChecks.AssertCarriedWhole(Board.Root, piece, lifted + edge - pointer, label);
                Assert.AreEqual(BoardInteractionTests.MapRegion(DraftInput.At(Map.panel, edge)), Map.DragTarget ?? "",
                    "The region under it is read again as the map moves · " + label);
            });
            await PutBack(edge, label);
        }

        // Dropped on its target, the real server takes the march: the own seat flies it on from the drop point, the
        // opponent's seat from the force's place, both along the ground between the two stand points.
        private async Task Flight()
        {
            FrameMarch(Map, true);
            FrameMarch(observer.Board.Map, true);
            await Settle();
            var label = $"flight {TabletopAcceptanceMatrixTests.Seat(side)} {layout.Width} × {layout.Scale:0.00}";
            var sourceFeet = FigureMotionTests.StandFeet(Map, SourceName);
            var observedFeet = FigureMotionTests.StandFeet(observer.Board.Map, SourceName);
            var revision = Board.View.Revision;
            var (_, press, _) = await Lift(label);
            var to = MapContourProbe.Terrain(Map, target.RegionId);
            MapContourProbe.Carry(Board, to);
            await room.Settle();
            var dropped = Map.Camera.ToBoard(Map.Camera.ToScreen(sourceFeet) + to - press);
            MapContourProbe.Drop(Board, to);
            await room.Until(() => Board.View.Revision > revision);
            var flown = await MidAir(Map, label);
            own.Save(TabletopAcceptanceMatrixTests.Name("flight", side, layout));
            findings.Check(label, () =>
            {
                Assert.Less(Vector2.Distance(dropped, flown.From), 1, "It flies on from where it was dropped · " + label);
                AssertBetweenStands(Map, flown, label);
            });
            var observed = await MidAir(observer.Board.Map, "observed " + label);
            other.Save(TabletopAcceptanceMatrixTests.Name("flight-observed", observer.Board.View.You, layout));
            findings.Check("observed " + label, () =>
            {
                Assert.Less(Vector2.Distance(observedFeet, observed.From), 1, "The opponent's march leaves its force's place · " + label);
                AssertBetweenStands(observer.Board.Map, observed, "observed " + label);
            });
        }

        // A flight half way: its ground on the straight line between its two stand points, the miniature above it, and its
        // landing point the destination force's own place.
        private void AssertBetweenStands(MapCanvas map, FlightSnapshot flight, string label)
        {
            var from = map.Camera.ToScreen(flight.From);
            var to = map.Camera.ToScreen(flight.To);
            Assert.Less(DistanceToSegment(flight.Ground, from, to), 1, "The ground runs between the stand points · " + label);
            Assert.Greater(flight.Height, 0, "The miniature flies above its shadow · " + label);
            Assert.Greater(Vector2.Distance(flight.Ground, from), 2, "It has left its place · " + label);
            Assert.Greater(Vector2.Distance(flight.Ground, to), 2, "It has not landed yet · " + label);
            var destination = "army-" + target.RegionId + "-" + side;
            if (map.Q<Button>(destination) != null)
                Assert.Less(Vector2.Distance(FigureMotionTests.StandFeet(map, destination), flight.To), .01f,
                    "It lands on the destination force's place · " + label);
        }

        // Waits for the army's flight on a map and returns it once it is about half way.
        private async Task<FlightSnapshot> MidAir(MapCanvas map, string label)
        {
            var keys = army.UnitIds.Select(id => "unit:" + id).ToList();
            string key = null;
            await room.Until(() => (key = keys.FirstOrDefault(k => map.Figures.Flight(k).HasValue)) != null);
            await room.Until(() => !(map.Figures.Flight(key) is { } flight) || flight.Frame.Progress >= .4f);
            return map.Figures.Flight(key) ?? throw new AssertionException("The flight landed before it was seen in the air · " + label);
        }

        // A press on the army that crosses the drag threshold lifts it into the hand; the pointer then rests beside the
        // press until the lift pose has risen. Returns the piece, the press and the pointer.
        private async Task<(VisualElement Piece, Vector2 Press, Vector2 Pointer)> Lift(string label)
        {
            Board.Hover.Close();
            var source = Source;
            Assert.IsNotNull(source, "The army's stand · " + label);
            var press = source.worldBound.center;
            MapContourProbe.LiftFigure(Board, source);
            var pointer = press + new Vector2(DragStep, 0);
            MapContourProbe.Carry(Board, pointer);
            await Task.Delay(Transition);
            // UI-444: carried, the piece stands in the carry layer in the board's root, above the HUD.
            var piece = Board.Root.Q("hand-" + SourceName);
            Assert.IsNotNull(piece, "The press lifts the army itself into the hand · " + label);
            return (piece, press, pointer);
        }

        // Esc puts the carried army back onto its base, and the pointer is released.
        private async Task PutBack(Vector2 point, string label)
        {
            BoardMenuLayoutTests.Key(Board.Root, true);
            BoardMenuLayoutTests.Key(Board.Root, false);
            MapContourProbe.Release(Map, point);
            await Task.Delay(Transition);
            Assert.IsNull(Board.Root.Q("hand-" + SourceName), "Esc puts the army back onto its base · " + label);
            Assert.IsNull(Map.DragTarget, label);
        }

        // An edge zone on exposed terrain toward which the map can still pan.
        private Vector2 PanningEdge(string label)
        {
            foreach (var direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                if (MapContourProbe.EdgeTerrain(Map, direction) is Vector2 point &&
                    Map.Camera.CanPan(MapEdgePan.VelocityAt(point, Map.worldBound) * .02f))
                    return point;
            throw new AssertionException("No edge of the map can pan · " + label);
        }

        // Zoomed in on the march (miniatures), or the whole map's zoom centred on it (tokens).
        private void FrameMarch(MapCanvas map, bool near)
        {
            var from = FigureMotionTests.StandFeet(map, SourceName);
            var middle = (from + LandingPlace(map)) / 2;
            map.FocusOn(middle, map.FitZoom * (near ? TabletopAcceptanceMatrixTests.Near : 1));
        }

        // UI-401: an army lands on the region's field, or on its planned place when a field force stands there.
        private Vector2 LandingPlace(MapCanvas map)
        {
            var stands = map.Stands.Stands(target.RegionId);
            return map.Data.Armies.Any(force => force.RegionId == target.RegionId && !force.Garrison) ? stands.Planned : stands.Field;
        }

        private async Task Settle()
        {
            await room.Settle();
            await room.Settle();
        }

        // The largest army that can march (not attack), and its first such target: the Player profile carries the same.
        private static (ArmyOption Army, ArmyTargetOption Target) Movable(PlayerView view)
        {
            var option = view.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var chosen = option.Armies
                .Where(a => !a.Garrison && a.UnitIds.Count > 0 && a.Targets.Any(t => t.BattleMode == null))
                .OrderByDescending(a => a.UnitIds.Count).ThenBy(a => a.RegionId, StringComparer.Ordinal).First();
            return (chosen, chosen.Targets.Where(t => t.BattleMode == null).OrderBy(t => t.RegionId, StringComparer.Ordinal).First());
        }

        private static string ArmyDie(PlayerView view)
        {
            var eligible = view.Options.First(o => o.Kind == CommandKind.MoveArmies).EligibleDice;
            return view.Dice.First(d => d.Owner == view.You && d.Location == DieLocation.Available && eligible.Contains(d.Id) &&
                d.Face is DieFace.Army or DieFace.ArmyMuster).Id;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var along = b - a;
            var t = along.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, along) / along.sqrMagnitude);
            return Vector2.Distance(point, a + along * t);
        }

        public void Dispose()
        {
            own.Dispose();
            other.Dispose();
        }
    }

    // UI-407: the drag workloads of map-profile-2 carry an army on each side's own decision as the real server projected
    // it; the first layout of each side's carried frames records that projection, before anything is chosen.
    internal static class DragProfileInput
    {
        public static void Export(Side side, PlayerView view)
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/ui407-map-drag.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var views = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
            views[side == Side.FreePeoples ? "drag-fp" : "drag-sh"] = JToken.Parse(JsonConvert.SerializeObject(view));
            File.WriteAllText(path, views.ToString(Formatting.Indented));
        }
    }
}
