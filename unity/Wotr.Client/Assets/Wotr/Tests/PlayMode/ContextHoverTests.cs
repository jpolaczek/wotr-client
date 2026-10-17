using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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
    public sealed class ContextHoverTests
    {
        [UnityTest]
        public IEnumerator ClickedInspectionIsInteractiveStableAndFitsDenseRegionsAtBothScales()
        {
            using var client = new MountedMatchClient("http://localhost", "hover-layout");
            var fixture = new BoardFixture(true);
            fixture.Put("sauron", UnitKind.Nazgul, "minas-tirith", 2);
            fixture.View.Characters.Add(new CharacterView { Id = "shadow-portrait", DefinitionId = "base.character.saruman", Side = Side.Shadow, Location = "minas-tirith" });
            fixture.View.Dice[0].Face = DieFace.ArmyMuster;
            fixture.Refresh();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Apply(fixture.View);
                    var board = client.Board;
                    for (var i = 0; i < 6; i++)
                        yield return null;
                    Assert.IsEmpty(board.ActionDie.DieId);
                    // UI-160: the bottom row keeps one height whatever is chosen.
                    var consoleHeight = board.Console.Root.layout.height;
                    ZoomToArmy(board.Map, "minas-tirith");
                    CardNetworkTests.Click(board.OwnDice.Q<Button>("army"));
                    yield return null;
                    Assert.IsTrue(board.ActionDie.Supports(DieFace.Army));
                    Assert.IsTrue(board.ActionDie.Supports(DieFace.Muster));
                    Assert.IsNull(board.OwnDice.Q("action-face-picker"));
                    var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
                    Assert.IsTrue(marker.ClassListContains("eligible-army"));
                    Move(marker);
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert.AreEqual(consoleHeight, board.Console.Root.layout.height, 0.01f, "UI-160: the console keeps its height.");
                    Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
                    CardNetworkTests.Click(marker);
                    yield return new WaitForSecondsRealtime(.1f);
                    var panel = board.Hover.Panel.Root;
                    Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
                    Assert.AreEqual(fixture.View.Units.Count(u => u.Location == "minas-tirith") + fixture.View.Characters.Count(c => c.Location == "minas-tirith"),
                        Figures(panel).Count());
                    Assert.IsEmpty(panel.Query<ScrollView>().ToList());
                    Assert.IsNull(panel.Q<Button>("inspect-characters"));
                    var figure = panel.Query<Button>().ToList().First(b => b.ClassListContains("eligible-figure"));
                    Assert.IsTrue(figure.ClassListContains("selected"), "UI-127: the marker click selects the whole army.");
                    Move(figure);
                    yield return new WaitForSecondsRealtime(.2f);
                    var before = figure.worldBound;
                    var stageBefore = panel.parent.worldBound;
                    var dock = board.Root.Q<VisualElement>(className: "bottom");
                    var dockBefore = dock.worldBound;
                    var started = Time.realtimeSinceStartupAsDouble;
                    CardNetworkTests.Click(figure);
                    Assert.Less(Time.realtimeSinceStartupAsDouble - started, .1);
                    Assert.IsFalse(figure.ClassListContains("selected"), "A figure click leaves that figure behind.");
                    Assert.IsTrue(marker.ClassListContains("selected-force"));
                    Assert.AreSame(figure, panel.Q<Button>(figure.name));
                    yield return null;
                    Assert.AreEqual(consoleHeight, board.Console.Root.layout.height, 0.01f, "UI-160: the console keeps its height.");
                    Assert.AreEqual(stageBefore, panel.parent.worldBound,
                        "Choosing the first figure must not shrink the inspected region's viewport.");
                    Assert.AreEqual(dockBefore, dock.worldBound);
                    Assert.AreEqual(before, figure.worldBound);
                    LayoutTests.Save(target, $"ui28-hover-{width}-{scale:0.00}.png");
                    AssertInside(panel.worldBound, board.Map.worldBound);
                    foreach (var button in Figures(panel))
                        AssertInside(button.worldBound, panel.worldBound);
                    LayoutTests.Save(target, $"ui15-hover-{width}-{scale:0.00}.png");
                    // UI-189 (user decision): away from the region and the window, the window closes after 0.3 s.
                    Move(board.Phase);
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "Not at once.");
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "UI-189: the window closes once the pointer has left.");
                    board.Hover.Close();
                    Assert.IsTrue(marker.ClassListContains("selected-force"), "The selected source remains visible after inspection closes.");
                    CardNetworkTests.Click(marker);
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert.IsFalse(panel.Q<Button>(figure.name).ClassListContains("selected"),
                        "Another click on the same force keeps its split.");
                    Assert.AreEqual(0, fixture.View.Revision);
                    var pinnedCard = fixture.View.Hand[0];
                    board.Cards.Show(pinnedCard, true);
                    yield return null;
                    Move(board.Phase);
                    yield return new WaitForSecondsRealtime(.22f);
                    // A pinned card now occludes the map instead of pushing its markers away.
                    board.Map.Camera.Pan += new Vector2(-150, 0);
                    board.Map.Refresh();
                    yield return null;
                    CardNetworkTests.Click(marker);
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
                    Assert.IsTrue(board.Cards.Pinned);
                    Assert.AreEqual(pinnedCard, board.Cards.CardId);
                    Assert.AreEqual(DisplayStyle.Flex, board.Cards.Root.resolvedStyle.display);
                    board.Cards.Close();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        [UnityTest]
        public IEnumerator InspectionOnRealProjectionsNeverChangesTheGame() => MatchTestRoom.Wait(InspectServer());

        [UnityTest]
        public IEnumerator ClickedCardSurvivesPointerAndFocusExitAndClosesExplicitly()
        {
            using var client = new MountedMatchClient("http://localhost", "card-click-lifecycle");
            var view = new BoardFixture().View;
            view.Hand.Add("base.fp.strategy.01");
            client.Apply(view);
            var board = client.Board;
            var card = view.Hand[0];
            board.HandDrawer.Open();
            yield return new WaitForSecondsRealtime(.1f);
            var source = board.Hand.Q<Button>(card);
            Move(source);
            source.Focus();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsEmpty(board.Cards.CardId);
            Assert.AreEqual(card, board.ExpandedCard, "UI-91: hover and focus unroll the hand card.");
            PointerGestureEvents.ClickAt(source);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsEmpty(board.Cards.CardId, "UI-91: a hand card click never opens the separate preview.");
            // The preview lifecycle below starts from another source, e.g. Effects or the map.
            board.Cards.Show(card, true);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(card, board.Cards.CardId);
            Assert.IsTrue(board.Cards.Pinned);
            var detail = board.Cards.Root.Q<Label>("card-text");
            var picked = detail.panel.Pick(detail.worldBound.center);
            Assert.IsTrue(picked == detail || detail.Contains(picked), "The card preview must be reachable.");
            Move(detail);
            board.Cards.Show(card, true);
            Move(board.Phase);
            board.Tray.Focus();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(card, board.Cards.CardId);
            Assert.AreEqual(DisplayStyle.Flex, board.Cards.Root.resolvedStyle.display);
            PointerGestureEvents.ClickAt(board.Cards.Root.Q<Button>("close-card"));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsEmpty(board.Cards.CardId);
            source.Focus();
            yield return null;
            Assert.IsEmpty(board.Cards.CardId);
            using (var submit = NavigationSubmitEvent.GetPooled())
                source.SendEvent(submit);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsEmpty(board.Cards.CardId, "UI-91: Enter on a hand card does not open the separate preview either.");
            board.Cards.Show(card, true);
            yield return new WaitForSecondsRealtime(.1f);
            var pin = board.Cards.Root.Q<Button>("pin-card");
            pin.Focus();
            yield return null;
            Assert.AreSame(pin, pin.panel.focusController.focusedElement);
            Assert.AreEqual(view.Revision, board.View.Revision);
        }

        [UnityTest]
        // UI-142: no double-click; a fast second click is a click within its decision and suppressed across decisions.
        public IEnumerator AFastSecondClickCannotCrossDecisionBoundary()
        {
            var bootstrap = Object.FindAnyObjectByType<ClientBootstrap>() ?? new GameObject("Gesture scope test").AddComponent<ClientBootstrap>();
            bootstrap.ShowFixture();
            // UI-160: the tray lives in the (closed) decision window; the gesture scope is tested on a visible host.
            var root = new VisualElement { name = "gesture-scope-host" };
            root.style.position = Position.Absolute;
            bootstrap.BoardScreen.Root.Add(root);
            var clicks = 0;
            var scope = "decision-1/5";
            var button = BoardScreen.Button("Object", () => clicks++);
            button.userData = new DraftToken(DraftObject.Army, "army");
            root.Add(button);
            using var input = new DraftInput(root, () => { }, () => { }, (_, _) => false, () => scope);
            yield return null;
            Click(button, 1);
            Click(button, 2);
            Assert.AreEqual(2, clicks, "Within one decision both clicks reach the control.");
            Click(button, 1);
            scope = "decision-2/6";
            Click(button, 2);
            Assert.AreEqual(3, clicks, "A second click cannot operate on the new decision.");
            root.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator PressingAnInspectedArmyMarkerKeepsItVisibleUntilItsSingleClickCompletes()
        {
            using var client = new MountedMatchClient("http://localhost", "inspected-army-click");
            client.Apply(new BoardFixture().View);
            yield return null;
            yield return null;
            var board = client.Board;
            board.Map.Focus("minas-tirith");
            var anchor = board.Map.Data.Shapes["minas-tirith"].Anchor;
            board.Map.Camera.Zoom = board.Map.FitZoom;
            board.Map.Camera.Pan = board.Map.contentRect.center - new Vector2(anchor[0], anchor[1]) * board.Map.FitZoom;
            board.Map.Refresh();
            var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display, "UI-126: the whole map shows every army.");
            Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.TokenClass));
            board.Hover.Open("minas-tirith");
            yield return null;
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display);
            Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.FiguresClass), "The inspected region shows its figures.");
            var bounds = marker.worldBound;
            var picked = marker.panel.Pick(bounds.center);
            Assert.IsTrue(picked == marker || marker.Contains(picked), "The inspected army remains reachable.");
            var activations = 0;
            board.Map.ArmySelected += _ => activations++;
            PointerGestureEvents.Press(marker);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display, "Pointer-down cannot hide its own source.");
            Assert.AreEqual(bounds, marker.worldBound, "Pointer-down cannot move its own source.");
            PointerGestureEvents.Release(marker);
            yield return null;
            Assert.AreEqual(1, activations);
            Assert.AreEqual("minas-tirith", board.Hover.Panel.Region);
            Assert.AreEqual(0, board.View.Revision);
        }

        [UnityTest]
        public IEnumerator MapKeyboardActivationDoesNotAlsoConfirmTheNextDraftStep()
        {
            using var client = new MountedMatchClient("http://localhost", "map-keyboard-step");
            var surface = new VisualElement();
            surface.style.position = Position.Absolute;
            surface.style.width = 600;
            surface.style.height = 400;
            client.Board.Root.parent.Add(surface);
            var map = new MapCanvas(BoardPresentation.Load());
            map.style.width = 600;
            map.style.height = 400;
            surface.Add(map);
            var inspection = new RegionHover(surface, surface, map);
            var selections = 0;
            var confirmations = 0;
            map.RegionSelected += _ => selections++;
            using var input = new DraftInput(surface, () => confirmations++, () => { }, (_, _) => false);
            yield return null;
            map.Focus("old-forest");
            ((VisualElement)map).Focus();
            yield return null;
            Assert.AreSame(map, map.panel.focusController.focusedElement);
            foreach (var code in new[] { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space })
            {
                var before = selections;
                using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = code }))
                    map.SendEvent(down);
                using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = code }))
                    map.SendEvent(up);
                Assert.AreEqual(before + 1, selections, "One activation selects one map destination.");
                Assert.AreEqual(0, confirmations, "The same key cannot also confirm the next draft step.");
            }
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator KeyboardActivationShowsInformationAndPanningDoesNotOpenCrossedRegions()
        {
            using var client = new MountedMatchClient("http://localhost", "hover-focus");
            client.Apply(new BoardFixture().View);
            yield return null;
            yield return null;
            var board = client.Board;
            ZoomToArmy(board.Map, "minas-tirith");
            yield return null;
            board.Root.Focus();
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab }))
                board.Root.SendEvent(key);
            var focused = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            Assert.AreEqual(DisplayStyle.Flex, focused.resolvedStyle.display);
            focused.Focus();
            Assert.AreSame(focused, focused.panel.focusController.focusedElement);
            yield return null;
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            using (var submit = NavigationSubmitEvent.GetPooled())
                focused.SendEvent(submit);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            var origin = board.Map.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = origin }))
                board.Map.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = origin + Vector2.one * 24 }))
                board.Map.SendEvent(move);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 1, mousePosition = origin + Vector2.one * 24 }))
                board.Map.SendEvent(up);
        }

        private static async Task InspectServer()
        {
            using var room = await MatchTestRoom.Connect("information-readonly", ScenarioKind.BaseAcceptance, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                ZoomToArmy(board.Map, "minas-tirith");
                await room.Settle();
                var pan = board.Map.Camera.Pan;
                var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
                Move(marker);
                await Task.Delay(320, room.Token);
                Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
                PointerGestureEvents.ClickAt(marker);
                await room.Settle();
                Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                CollectionAssert.IsSubsetOf(Figures(board.Hover.Panel.Root)
                    .Select(b => ((DraftToken)b.userData).Id), board.View.Units.Select(u => u.Id).Concat(board.View.Characters.Select(c => c.Id)));
                Move(board.Phase);
                await Task.Delay(220, room.Token);
                Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
                board.Hover.Close();
                var contents = new RegionViewIndex(board.View);
                var emptyRegion = board.Map.Data.Regions.Values.First(r => r.Settlement == null && !contents.Occupied(r.Id)).Id;
                board.Map.Focus(emptyRegion);
                await room.Settle();
                var empty = RegionInspectionTests.Terrain(board, emptyRegion);
                RegionInspectionTests.Move(board, empty);
                await Task.Delay(300, room.Token);
                Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
                RegionInspectionTests.ClickTerrain(board, empty);
                await room.Settle();
                Assert.AreEqual(emptyRegion, board.Hover.Panel.Region);
                Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);

                await ComponentGestures.Hover(board.Edges.Corner);
                Assert.IsTrue(board.Fellowship.IsOpen, "The corner hover unfolds Fellowship without choosing an action.");
                await room.Settle();
                var controls = board.Fellowship.Root.Q("fellowship-controls");
                Assert.AreEqual(board.View.You == Side.FreePeoples ? DisplayStyle.Flex : DisplayStyle.None,
                    controls.resolvedStyle.display);
                Assert.AreEqual(DisplayStyle.Flex, board.Fellowship.Root.resolvedStyle.display);
                Assert.IsTrue(board.Fellowship.Root.Q<Button>("focus-fellowship").enabledInHierarchy);
                Assert.IsEmpty(board.Fellowship.Root.Query<Button>(className: "legal-target").ToList(),
                    "Inspection without a chosen die must not create an action.");
                LayoutTests.Save(client.Viewport, "ui47-context-fellowship-" + board.View.You + ".png");
                await client.Connection.Refresh();
                await room.Settle();
                Assert.AreEqual(revision, board.View.Revision);
            }
        }

        private static void ZoomToArmy(MapCanvas map, string region)
        {
            map.Focus(region);
            var anchor = map.Data.Shapes[region].Anchor;
            map.Camera.Zoom = map.FitZoom * 1.75f;
            map.Camera.Pan = map.contentRect.center - new Vector2(anchor[0], anchor[1]) * map.Camera.Zoom;
            map.Refresh();
            Assert.IsTrue(map.MarkerVisibility.Detail);
        }

        private static System.Collections.Generic.IEnumerable<Button> Figures(VisualElement panel) =>
            panel.Query<Button>().ToList().Where(b => b.userData is DraftToken token && token.Object == DraftObject.Figure);

        private static void Move(VisualElement target)
        {
            Assert.NotNull(target);
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }

        private static void Click(VisualElement target, int count)
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(up);
        }

        private static void AssertInside(Rect inner, Rect outer)
        {
            Assert.GreaterOrEqual(inner.xMin, outer.xMin - 1);
            Assert.GreaterOrEqual(inner.yMin, outer.yMin - 1);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, $"Inner {inner}; outer {outer}");
        }
    }
}
