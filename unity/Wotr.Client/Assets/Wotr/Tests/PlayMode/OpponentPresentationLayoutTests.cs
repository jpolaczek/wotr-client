using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
    // UI-305 on a mounted board: the Shadow's recorded march (an Army die and three Regulars from Dol Guldur to the
    // Eastern Brown Lands) presented to the Free Peoples by the production presenters — the die's mark over the pool, the
    // one Skip target, the camera — at the four supported layouts and under Reduced motion, with Skip, Esc, a card read
    // during the presentation and a manual camera takeover. Frames go to .artifacts/screenshots (ui305-*).
    public sealed class OpponentPresentationLayoutTests
    {
        private const string Die = "shadow-army";
        private const string Source = "dol-guldur";
        private const string Destination = "eastern-brown-lands";
        private const string FarAway = "grey-havens";
        private const string Between = "between";
        private const string Card = "base.fp.character.18";

        [UnityTest]
        public IEnumerator TheCostSkipTargetAndCameraHoldTheirPlacesAtFourLayoutsAndLeaveNothingBehind()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    yield return Scene(width, scale, false, FarAway, "far");
        }

        [UnityTest]
        public IEnumerator ReducedMotionJumpsTheCameraAndKeepsTheSameMarks() => Scene(1366, 1f, true, FarAway, "reduced");

        [UnityTest]
        public IEnumerator AMarchAlreadyInViewDoesNotMoveTheCamera() => Scene(1920, 1f, false, Between, "legible");

        [UnityTest]
        public IEnumerator SkipEndsThePresentationOnTheNextFrameWithoutMovingTheCamera()
        {
            using var stage = new Stage(1366, 1f, false);
            yield return stage.Ready();
            stage.Start(FarAway);
            var camera = stage.CameraNow;
            yield return stage.Until(() => stage.Mark != null);
            PointerGestureEvents.ClickAt(stage.SkipButton);
            Assert.AreEqual(0, stage.Board.View.Revision, "The click itself releases nothing.");
            Assert.IsTrue(stage.Playback.Holding);
            yield return stage.Until(() => !stage.Playback.Holding);
            Assert.AreEqual(1, stage.Board.View.Revision);
            Assert.AreEqual(camera, stage.CameraNow, "Skip leaves the camera where it was.");
            Assert.IsNull(stage.SkipButton.parent);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNull(stage.Mark, "The die's mark fades with the presentation.");
        }

        [UnityTest]
        public IEnumerator EscClosesAReadCardFirstAndThenSkips()
        {
            using var stage = new Stage(1366, 1f, false);
            yield return stage.Ready();
            stage.Start(FarAway);
            yield return stage.Until(() => stage.Mark != null);
            stage.Board.Cards.Show(Card, true, allowPlay: false);
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsTrue(stage.Playback.Holding, "A card opened during the presentation holds it.");
            Assert.AreEqual(PresentationCueKind.Cost, stage.Playback.Current?.Kind, "Nothing advanced while the card was read.");
            // The player clicked the card, so the key comes from inside it.
            Escape(stage.Board.Cards.Root.Q(className: "card-header"));
            yield return null;
            Assert.AreEqual(DisplayStyle.None, stage.Board.Cards.Root.resolvedStyle.display, "Esc closes the card only.");
            Assert.IsTrue(stage.Playback.Holding);
            yield return stage.Until(() => stage.Playback.Current?.Kind == PresentationCueKind.March);
            Assert.IsEmpty(OpenWindows(stage.Board).Where(name => name != "hand"), "No window owns Esc now.");
            Escape(Focused(stage.Board));
            yield return stage.Until(() => !stage.Playback.Holding);
            Assert.AreEqual(1, stage.Board.View.Revision, "Esc with nothing else open skips to the latest state.");
            Assert.IsFalse(stage.Board.Menu.IsOpen, "The skip does not open the menu.");
        }

        [UnityTest]
        public IEnumerator AWheelZoomDuringTheTravelStopsTheCameraForTheRestOfThePresentation()
        {
            using var stage = new Stage(1366, 1f, false);
            yield return stage.Ready();
            stage.Start(FarAway);
            yield return stage.Until(() => stage.Playback.Current?.Kind == PresentationCueKind.March);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsTrue(stage.Board.Map.EdgePan.Suspended, "Edge pan does not fight the travel.");
            var map = stage.Board.Map;
            using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, mousePosition = map.worldBound.center, delta = new Vector2(0, -1) }))
                map.SendEvent(wheel);
            var taken = stage.CameraNow;
            yield return stage.Until(() => !stage.Playback.Holding);
            Assert.AreEqual(taken, stage.CameraNow, "Manual navigation wins.");
            Assert.IsFalse(map.EdgePan.Suspended);
        }

        private static IEnumerator Scene(int width, float scale, bool reduced, string start, string name)
        {
            var file = "ui305-" + name + "-" + width + "-" + scale.ToString("0.00");
            using var stage = new Stage(width, scale, reduced);
            yield return stage.Ready();
            stage.Start(start);
            var before = stage.Buttons();
            var pool = stage.Board.OpponentDice.Q<Button>(Die).worldBound;
            var camera = stage.CameraNow;
            yield return stage.Until(() => stage.Mark != null);
            yield return new WaitForSecondsRealtime(.25f);
            AssertCost(stage, pool);
            AssertSkip(stage, before);
            LayoutTests.Save(stage.Texture, file + "-1-cost.png");
            yield return stage.Until(() => stage.Playback.Current?.Kind == PresentationCueKind.March || !stage.Playback.Holding);
            yield return new WaitForSecondsRealtime(reduced ? .05f : .3f);
            LayoutTests.Save(stage.Texture, file + "-2-camera.png");
            yield return stage.Until(() => !stage.Playback.Holding);
            yield return new WaitForSecondsRealtime(.5f);
            LayoutTests.Save(stage.Texture, file + "-3-after.png");
            Assert.AreEqual(before, stage.Buttons(), "No control remains after the presentation.");
            Assert.IsNull(stage.Mark);
            Assert.IsNull(stage.Board.Root.Q(className: "presentation-caption"), "These beats need no caption.");
            Assert.AreEqual(1, stage.Board.View.Revision);
            var safe = stage.Presenters.Camera.SafeArea();
            foreach (var region in new[] { Source, Destination })
                Assert.IsTrue(safe.Contains(stage.Board.Map.Camera.ToScreen(Anchor(stage.Board, region))),
                    region + " ends in the safe map area (" + file + ").");
            if (start == Between)
                Assert.AreEqual(camera, stage.CameraNow, "The march was already legible; the camera did not move.");
            else
                Assert.AreNotEqual(camera, stage.CameraNow, "The camera brought the march into view.");
        }

        private static void AssertCost(Stage stage, Rect pool)
        {
            var mark = stage.Mark;
            var die = stage.Board.OpponentDice.Q<Button>(Die);
            Assert.AreEqual(pool, die.worldBound, "The live pool is not reflowed.");
            Assert.IsTrue(mark.worldBound.Contains(die.worldBound.center), "The mark stands on its die.");
            Assert.IsTrue(mark.Query<VisualElement>().ToList().All(e => e.pickingMode == PickingMode.Ignore), "Marks ignore the pointer.");
            Assert.AreEqual(1f, mark.resolvedStyle.opacity, .01f);
        }

        private static void AssertSkip(Stage stage, int before)
        {
            var skip = stage.SkipButton;
            Assert.AreEqual(before + 1, stage.Buttons(), "Exactly one control is added while the presentation plays.");
            Assert.AreEqual(SkipControl.Size, skip.worldBound.width, .6f);
            Assert.AreEqual(SkipControl.Size, skip.worldBound.height, .6f);
            Assert.AreEqual("Skip presentation", skip.tooltip);
            var board = stage.Board;
            Assert.AreEqual(board.Edges.Right.worldBound.xMin - SkipControl.Gap, skip.worldBound.xMax, 1f);
            Assert.AreEqual(board.Console.Root.parent.worldBound.yMin - SkipControl.Gap, skip.worldBound.yMax, 1f);
            var local = new Rect(board.Map.WorldToLocal(skip.worldBound.position), skip.worldBound.size);
            Assert.IsFalse(board.Map.Occlusions().Any(rect => rect.Overlaps(local)), "The Skip target covers no HUD.");
        }

        // Esc as a keyboard sends it: to the focused element.
        private static void Escape(VisualElement focused)
        {
            focused.Focus();
            using var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape });
            focused.SendEvent(key);
            using var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape });
            focused.SendEvent(up);
        }

        // Closing the card returned the focus to its fallback (the hand's tab).
        private static VisualElement Focused(BoardScreen board) =>
            board.Root.panel.focusController.focusedElement as VisualElement is { } element && board.Root.Contains(element)
                ? element : board.HandDrawer.Tab;

        private static List<string> OpenWindows(BoardScreen board) => board.Escape.Roots
            .Where(root => !root.ClassListContains("edge-permanent-panel") && root.resolvedStyle.display != DisplayStyle.None &&
                root.resolvedStyle.visibility == Visibility.Visible && root.worldBound.width > 0 && root.worldBound.height > 0)
            .Select(root => root.name).ToList();

        private static Vector2 Anchor(BoardScreen board, string region) =>
            new Vector2(board.Map.Data.Shapes[region].Anchor[0], board.Map.Data.Shapes[region].Anchor[1]);

        // One mounted board at one layout, with the Shadow's march received while the camera looks elsewhere.
        private sealed class Stage : IDisposable
        {
            private readonly MountedMatchClient client;
            private readonly List<string> moved;
            private readonly PlayerView before;
            public RenderTexture Texture { get; }
            public BoardScreen Board => client.Board;
            public PresentationPlayback Playback => client.Playback;
            public OpponentPresenters Presenters => client.Presenters;
            public Button SkipButton => client.Presenters.Skip.Button;
            public VisualElement Mark => Board.Root.Q(CostEmphasis.MarkName);
            public (float, Vector2) CameraNow => (Board.Map.Camera.Zoom, Board.Map.Camera.Pan);

            public Stage(int width, float scale, bool reduced)
            {
                client = new MountedMatchClient("http://localhost", "ui305-presentation");
                Texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                Texture.Create();
                client.Panel.targetTexture = Texture;
                client.Interface.Preferences.SetScale(scale);
                client.Interface.Preferences.SetReducedMotion(reduced);
                var fixture = new BoardFixture();
                before = fixture.View;
                before.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
                moved = Enumerable.Range(0, 3).Select(_ => fixture.Put("sauron", UnitKind.Regular, Source).Id).ToList();
                client.Apply(before);
            }

            // The board's first layout: the pool, the console and the map have their places.
            public IEnumerator Ready()
            {
                yield return new WaitForSecondsRealtime(.3f);
            }

            public void Start(string focus)
            {
                if (focus == Between)
                    Board.Map.FocusOn((Anchor(Board, Source) + Anchor(Board, Destination)) / 2, Board.Map.FitZoom * 1.4f);
                else
                    Board.Map.FocusOn(Anchor(Board, focus), Board.Map.FitZoom * 2.2f);
                Playback.SetConnection("Connected");
                var after = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(before));
                after.Revision = 1;
                after.Dice.Single(d => d.Id == Die).Location = DieLocation.Spent;
                foreach (var unit in after.Units.Where(u => moved.Contains(u.Id)))
                    unit.Location = Destination;
                client.Refresh(after);
                Playback.Enqueue(Cues());
                Playback.HistoryRead(1);
            }

            // The board's controls, without the opponent's pool, whose spent die leaves with the release.
            public int Buttons() => Board.Root.Query<Button>().ToList().Count(button => !Board.OpponentDice.Contains(button));

            public IEnumerator Until(Func<bool> condition)
            {
                var deadline = Time.realtimeSinceStartup + 10;
                while (!condition() && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.IsTrue(condition(), "The presentation did not reach the expected state.");
            }

            // The recorded step as the UI-303 model makes cues of it: the die's cost, then the march.
            private IReadOnlyList<PresentationCue> Cues()
            {
                var step = new ActionLogStep
                {
                    Id = "ui305-march", ActionId = "ui305-action", Revision = 1, Order = 1, Actor = Side.Shadow, Kind = "ArmiesMoved"
                };
                step.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Use = DieFace.Army });
                step.Movements.Add(new ActionMovement
                {
                    Kind = ActionMovementKind.March, From = Source, To = Destination,
                    Participants = moved.Select(id => new ActionParticipant { Id = id, Side = Side.Shadow, UnitKind = UnitKind.Regular }).ToList()
                });
                var model = new OpponentPresentationModel(Side.FreePeoples, Board.Presentation);
                model.Baseline(new ActionLogPage[0], 0);
                model.Advance(1);
                return model.Merge(new[] { new ActionLogPage { Steps = { step } } });
            }

            public void Dispose()
            {
                client.Dispose();
                Texture.Release();
                Object.Destroy(Texture);
            }
        }
    }
}
