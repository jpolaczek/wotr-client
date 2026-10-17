using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class VictoryPanelLayoutTests
    {
        [UnityTest]
        public IEnumerator BothScoresThresholdPagesAndFinalBannerFitAllFourLayouts()
        {
            using var client = new MountedMatchClient("http://localhost", "victory-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    client.Interface.Windows.Clear();
                    var view = Screen();
                    view.MatchId = $"victory-layout-{width}-{scale:0.00}";
                    client.Apply(view);
                    client.SetOnline();
                    yield return Frames();
                    var board = client.Board;
                    yield return MatchTestRoom.Wait(BoardNavigation.Victory(board));
                    yield return Frames();
                    LayoutTests.Save(target, $"ui60-threshold-layout-{width}-{scale:0.00}.png");
                    Check(board);
                    Assert.AreEqual("Threshold reached · Checked at turn end", board.Victory.Root.Q<Label>("victory-timing").text);
                    var pageSize = board.Map.contentRect.height < 360 ? 2 : 3;
                    Assert.AreEqual(pageSize, VisibleRegions(board, Side.Shadow));
                    PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("victory-next-Shadow"));
                    yield return Frames();
                    Assert.AreEqual(2, VisibleRegions(board, Side.Shadow));
                    Check(board);
                    if (pageSize == 2)
                    {
                        PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("victory-next-Shadow"));
                        yield return Frames();
                        Assert.AreEqual(1, VisibleRegions(board, Side.Shadow));
                        Check(board);
                    }
                    var locate = board.Victory.Root.Q<Button>("victory-locate-moria");
                    locate.Focus();
                    view.Revision++;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.AreSame(locate, board.Root.panel.focusController.focusedElement);
                    var heading = board.Victory.Root.Q(className: FloatingWindow.HandleClass).Q<Label>();
                    var point = heading.worldBound.center;
                    PointerGestureEvents.Press(heading);
                    yield return null;
                    using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0,
                        mousePosition = point + new Vector2(-80, -10) }))
                        board.Victory.Root.SendEvent(move);
                    yield return null;
                    PointerGestureEvents.Release(board.Victory.Root);
                    yield return Frames();
                    var position = board.Victory.Root.layout.position;
                    board.Victory.Close();
                    client.Refresh(view);
                    yield return Frames();
                    Assert.IsFalse(board.Victory.IsOpen);
                    yield return MatchTestRoom.Wait(BoardNavigation.Victory(board));
                    yield return Frames();
                    Assert.AreEqual(position, board.Victory.Root.layout.position);
                    PointerGestureEvents.ClickAt(locate);
                    yield return Frames();
                    Assert.IsTrue(board.Victory.Pinned);
                    Assert.AreEqual("moria", board.Map.Selected);
                    Assert.IsFalse(board.Victory.Root.worldBound.Contains(board.Map.LocalToWorld(board.Map.contentRect.center)));
                    view.Winner = Side.Shadow;
                    view.Victory = VictoryCondition.ShadowMilitary;
                    view.Phase = GamePhase.Finished;
                    view.Revision++;
                    client.Refresh(view);
                    yield return Frames();
                    // UI-169: the first view with a winner opens the end-of-game screen over the board; View board returns
                    // to the board, where the inspector, the banner and the Menu shortcut are checked.
                    Assert.IsTrue(board.GameEnd.IsOpen);
                    PointerGestureEvents.ClickAt(board.GameEnd.Root.Q<Button>("game-end-view-board"));
                    yield return Frames();
                    Check(board);
                    Assert.AreEqual("Shadow wins · Military victory", board.Victory.Banner.text);
                    Assert.AreEqual(DisplayStyle.Flex, board.Victory.Banner.resolvedStyle.display);
                    Inside(board.Victory.Banner.worldBound, board.Map.worldBound);
                    Assert.IsFalse(board.Victory.Root.worldBound.Overlaps(board.Victory.Banner.worldBound),
                        "The moved inspector must not cover the final result.");
                    LayoutTests.Save(target, $"ui60-result-layout-{width}-{scale:0.00}.png");
                    board.Victory.Close();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        [UnityTest]
        public IEnumerator PermanentShortcutKeepsEveryHandCardReachableWithoutMovingASavedWindow()
        {
            using var client = new MountedMatchClient("http://localhost", "victory-hand-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Interface.Preferences.SetScale(scale);
                        client.Interface.Windows.Clear();
                        var view = Screen();
                        view.MatchId = $"victory-hand-{width}-{scale:0.00}";
                        view.Hand = Enumerable.Range(1, 9).Select(number => "base.fp.character." + number.ToString("00")).ToList();
                        client.Apply(view);
                        yield return Frames();
                        var board = client.Board;
                        Assert.IsTrue(board.HandDrawer.Docked && board.HandDrawer.IsOpen);
                        Assert.IsNull(board.HandDrawer.Window);
                        CheckHandReachable(board);
                        var position = board.Hand.worldBound;
                        board.Cards.Show("base.fp.character.01", true);
                        yield return Frames();
                        var cardPosition = board.Cards.Root.worldBound;
                        yield return MatchTestRoom.Wait(BoardNavigation.Victory(board));
                        Assert.AreEqual(position, board.Hand.worldBound);
                        Assert.AreEqual(cardPosition, board.Cards.Root.worldBound);
                        Assert.IsTrue(board.Cards.Pinned);
                        board.Victory.Close();
                        board.Cards.Close();
                        view.Revision++;
                        client.Refresh(view);
                        yield return Frames();
                        Assert.AreEqual(position, board.Hand.worldBound);
                        CheckHandReachable(board);
                        Assert.IsFalse(client.Interface.Windows.TryGet(board.Hand.name, out _));
                        LayoutTests.Save(target, $"ui54-victory-hand-{width}-{scale:0.00}.png");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        private static void CheckHandReachable(BoardScreen board)
        {
            Assert.IsFalse(board.Menu.Toggle.worldBound.Overlaps(board.Hand.worldBound));
            foreach (var card in board.Hand.Query<Button>(className: "hand-card").ToList())
            {
                var picked = card.panel.Pick(card.worldBound.center);
                Assert.IsTrue(picked == card || card.Contains(picked), "Hand card must remain reachable: " + card.name);
            }
        }

        [UnityTest]
        public IEnumerator ClickAndKeyboardActivationInspectVictoryWithoutChangingCameraOrSelection()
        {
            using var client = new MountedMatchClient("http://localhost", "victory-hover");
            client.Apply(Screen());
            yield return Frames();
            var board = client.Board;
            var pan = board.Map.Camera.Pan;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = board.Menu.Toggle.worldBound.center }))
                board.Root.panel.visualTree.SendEvent(move);
            yield return new WaitForSeconds(.3f);
            Assert.IsFalse(board.Victory.IsOpen);
            yield return MatchTestRoom.Wait(BoardNavigation.Victory(board));
            yield return Frames();
            Assert.IsTrue(board.Victory.IsOpen);
            Assert.IsTrue(board.Victory.Pinned);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            var detail = board.Victory.Root.Q<Button>("victory-locate-moria");
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = detail.worldBound.center }))
                board.Root.panel.visualTree.SendEvent(move);
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(board.Victory.IsOpen);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = board.Map.worldBound.min + new Vector2(400, 40) }))
                board.Root.panel.visualTree.SendEvent(move);
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(board.Victory.IsOpen, "Pointer exit preserves explicit inspection.");
            PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("close-victory"));
            yield return Frames();
            Assert.IsFalse(board.Victory.IsOpen);
            board.Root.Focus();
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab }))
                board.Root.SendEvent(key);
            yield return MatchTestRoom.Wait(BoardNavigation.Victory(board, keyboard: true));
            Assert.IsTrue(board.Victory.IsOpen);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(0, board.View.Revision);
        }

        private static PlayerView Screen()
        {
            var view = new PlayerView
            {
                MatchId = "victory-layout", Phase = GamePhase.ActionResolution, You = Side.FreePeoples,
                Players = { new PlayerSummary { Side = Side.FreePeoples, VictoryPoints = 4 },
                    new PlayerSummary { Side = Side.Shadow, VictoryPoints = 10 } }
            };
            var fp = new MilitaryVictoryView { Side = Side.FreePeoples, Points = 4, Threshold = 4, ThresholdReached = true };
            var sh = new MilitaryVictoryView { Side = Side.Shadow, Points = 10, Threshold = 10, ThresholdReached = true };
            var data = BoardPresentation.Load();
            foreach (var id in new[] { "moria", "dol-guldur" })
                fp.Regions.Add(new VictoryRegionView { RegionId = id, Name = data.Regions[id].Name, Settlement = "Stronghold", Points = 2 });
            foreach (var id in new[] { "helms-deep", "minas-tirith", "dol-amroth", "lorien", "woodland-realm" })
                sh.Regions.Add(new VictoryRegionView { RegionId = id, Name = data.Regions[id].Name, Settlement = "Stronghold", Points = 2 });
            view.MilitaryVictory.Add(fp);
            view.MilitaryVictory.Add(sh);
            return view;
        }

        private static int VisibleRegions(BoardScreen board, Side side) => board.Victory.Root.Q("victory-score-" + side)
            .Query<VisualElement>(className: "victory-region").ToList().Count(row => row.resolvedStyle.display != DisplayStyle.None);

        private static void Check(BoardScreen board)
        {
            Inside(board.Victory.Root.worldBound, board.Map.worldBound);
            Inside(board.Menu.Toggle.worldBound, board.Map.worldBound);
            Assert.IsFalse(board.Victory.Root.worldBound.Overlaps(board.Menu.Toggle.worldBound),
                $"The inspector {board.Victory.Root.worldBound} leaves shortcut {board.Menu.Toggle.worldBound} accessible in {board.Map.worldBound}.");
            var picked = board.Root.panel.Pick(board.Menu.Toggle.worldBound.center);
            Assert.IsTrue(picked == board.Menu.Toggle || board.Menu.Toggle.Contains(picked),
                "The shortcut remains an actual pointer target, not " + picked?.name + " (" + picked?.GetType().Name + ").");
            Assert.IsEmpty(board.Victory.Root.Query<ScrollView>().ToList());
            Assert.IsEmpty(board.Victory.Root.Query<TextField>().ToList());
            foreach (var element in board.Victory.Root.Query<VisualElement>().ToList())
                if (element.visible && element.worldBound.width > 0 && element.worldBound.height > 0)
                    Inside(element.worldBound, board.Victory.Root.worldBound);
        }

        private static void Inside(Rect inner, Rect outer)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), $"{inner} in {outer}");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), $"{inner} in {outer}");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), $"{inner} in {outer}");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), $"{inner} in {outer}");
        }

        private static IEnumerator Frames()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
