using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class FellowshipTrackLayoutTests
    {
        [UnityTest]
        public IEnumerator PublicTracksFitBothSidesAndFourLayoutsWithoutChangingEitherProjection() =>
            MatchTestRoom.Wait(PublicLayouts());

        [UnityTest]
        public IEnumerator BeyondPrintedProgressAndFinalMordorKeepExactValuesAndSpritesInBounds()
        {
            // Explicit presentation fixture: no connection or command sender is simulated.
            using var client = new MountedMatchClient("http://localhost", "track-layout");
            var panel = new VisualElement();
            panel.AddToClassList("fellowship-panel");
            panel.style.left = 30;
            panel.style.top = 100;
            client.Board.Root.Add(panel);
            var status = new FellowshipStatus(null);
            panel.Add(status.Root);
            var data = BoardPresentation.Load();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = Target(width);
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    client.Interface.Preferences.SetReducedMotion(true);
                    foreach (var mordor in new[] { false, true })
                    {
                        var view = new FellowshipView
                        {
                            RegionId = mordor ? "mordor-track" : "rivendell", Progress = 20,
                            MordorStep = mordor ? 5 : (int?)null, Hidden = !mordor, Corruption = 12,
                            Guide = "base.character.gandalf-grey"
                        };
                        view.RevealedCompanionVersions.Add(view.Guide, view.Guide);
                        var before = JsonConvert.SerializeObject(view);
                        status.Apply(view, data, finished: true);
                        for (var frame = 0; frame < 4; frame++)
                            yield return null;
                        CheckTracks(panel, view);
                        Assert.AreEqual(mordor ? "5" : "20", panel.Q<Label>(mordor ? "mordor-label" : "journey-label").text);
                        Assert.AreEqual("12", panel.Q<Label>("corruption-label").text);
                        Assert.AreEqual(before, JsonConvert.SerializeObject(view));
                        Assert.AreEqual(DisplayStyle.None, panel.Q<Label>("mordor-activity").resolvedStyle.display);
                        LayoutTests.Save(texture, $"ui79-boundary-{(mordor ? "mordor" : "progress")}-{width}-{scale:0.00}.png");
                    }
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        private static async Task PublicLayouts()
        {
            using var room = await UI10TestRoom.Connect("ui34-panel-move");
            var snapshots = new[] { JsonConvert.SerializeObject(room.Fp.Board.View), JsonConvert.SerializeObject(room.Sh.Board.View) };
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = Target(width);
                        var previous = client.Panel.targetTexture;
                        try
                        {
                            client.Panel.targetTexture = texture;
                            client.Interface.Preferences.SetScale(scale);
                            client.Board.Windows.Clear();
                            await room.Settle();
                            await ComponentGestures.Hover(client.Board.Edges.Corner);
                            Assert.IsTrue(client.Board.Fellowship.IsOpen, "Hover unfolds the public tracks.");
                            await room.Settle();
                            var board = client.Board;
                            CheckTracks(board.Fellowship.Root, board.View.Fellowship);
                            Assert.AreSame(board.Edges.Corner, board.Fellowship.Status.parent,
                                "The summary remains in the corner tab while the full tracks unfold over it.");
                            LayoutTests.AssertNoDecisionScroll(board);
                            AssertInside(board.Fellowship.Root, board.Root);
                            if (board.View.You == Side.Shadow)
                                Assert.AreEqual(DisplayStyle.None, board.Fellowship.Root.Q("fellowship-controls").resolvedStyle.display);
                            LayoutTests.Save(texture, $"ui79-tracks-{board.View.You}-{width}-{scale:0.00}.png");
                            var before = board.Map.Camera.Pan;
                            var track = board.Fellowship.Root.Q("journey-track");
                            await ComponentGestures.Hover(track);
                            var tooltip = client.Interface.Content.parent.Q<Label>("runtime-tooltip");
                            Assert.AreEqual(track.tooltip, tooltip.text);
                            Assert.AreEqual(DisplayStyle.Flex, tooltip.resolvedStyle.display);
                            PointerGestureEvents.ClickAt(track);
                            await room.Settle();
                            Assert.AreEqual(before, board.Map.Camera.Pan);
                            Assert.AreEqual(DisplayStyle.None, tooltip.resolvedStyle.display,
                                "A click dismisses the passive description without changing the tracks.");
                            track.Blur();
                            track.Focus();
                            await room.Settle();
                            Assert.AreEqual(track.tooltip, tooltip.text);
                            // Esc dismisses the tooltip and closes its focused panel in the same press.
                            using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                                track.SendEvent(escape);
                            using (var release = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                                board.Root.SendEvent(release);
                            await room.Settle();
                            Assert.IsFalse(board.Fellowship.IsOpen);
                            Assert.AreEqual(DisplayStyle.None, tooltip.resolvedStyle.display);
                            // UI-162: the Hunt is a window opened from the console; Esc closes it from its focused control.
                            board.Hunt.Open();
                            await room.Settle();
                            var huntControl = board.Hunt.Root.Q<Button>("close-hunt");
                            Assert.Greater(huntControl.worldBound.width, 0, "The Hunt window's visible control receives keyboard focus.");
                            huntControl.Focus();
                            Assert.AreSame(huntControl, board.Root.panel.focusController.focusedElement);
                            BoardMenuLayoutTests.Key(huntControl, true);
                            BoardMenuLayoutTests.Key(huntControl, false);
                            await room.Settle();
                            Assert.IsFalse(board.Hunt.IsOpen,
                                $"Esc closes the Hunt window and keeps it closed under a stationary pointer: {board.View.You} {width} {scale}.");
                            Assert.IsFalse(board.Fellowship.IsOpen, "Restoring focus to the plaque must not unfold the panels again.");
                            Assert.IsFalse(board.Menu.IsOpen);
                        }
                        finally
                        {
                            client.Panel.targetTexture = previous;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.AreEqual(snapshots[0], JsonConvert.SerializeObject(room.Fp.Board.View));
            Assert.AreEqual(snapshots[1], JsonConvert.SerializeObject(room.Sh.Board.View));
        }

        private static RenderTexture Target(int width)
        {
            var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            texture.Create();
            return texture;
        }

        private static void CheckTracks(VisualElement panel, FellowshipView view)
        {
            var travelName = view.MordorStep.HasValue ? "mordor-track" : "journey-track";
            var upper = panel.Q(travelName);
            var lower = panel.Q("corruption-track");
            Assert.IsTrue(Visible(upper) && Visible(lower));
            Assert.Less(upper.worldBound.yMax, lower.worldBound.yMax);
            Assert.IsFalse(upper.worldBound.Overlaps(lower.worldBound));
            var topLane = upper.Q(travelName + "-lane");
            var bottomLane = lower.Q("corruption-track-lane");
            Assert.That(Mathf.Abs(topLane.worldBound.xMin - bottomLane.worldBound.xMin), Is.LessThan(1));
            Assert.That(Mathf.Abs(topLane.worldBound.xMax - bottomLane.worldBound.xMax), Is.LessThan(1));
            foreach (var track in new[] { upper, lower })
            {
                AssertInside(track, panel);
                var cells = track.Query<Label>(className: "fellowship-track-cell").ToList();
                Assert.AreEqual(track == upper && view.MordorStep.HasValue ? 6 : 13, cells.Count);
                Assert.AreEqual(1, cells.Count(cell => cell.ClassListContains("selected")));
                foreach (var cell in cells)
                    AssertInside(cell, track);
                var value = track.Q<Label>(className: "fellowship-track-value");
                TextFits(value);
                var marker = track.Q<Image>(className: "fellowship-track-marker");
                AssertInside(marker, cells.Single(cell => cell.ClassListContains("selected")));
                Assert.AreSame(FellowshipArtwork.Sprite(track == lower ? "corruption" : view.Hidden ? "hidden" : "revealed"), marker.sprite);
                var picked = track.panel.Pick(marker.worldBound.center);
                Assert.IsTrue(picked == track || track.Contains(picked), "Covered track: " + track.name + " by " + picked?.name);
            }
            foreach (var image in panel.Query<Image>().ToList().Where(Visible))
            {
                Assert.IsNotNull(image.sprite, image.name);
                AssertInside(image, panel);
                Assert.AreEqual(PickingMode.Ignore, image.pickingMode);
            }
            var guide = panel.Q<Label>("fellowship-guide");
            if (Visible(guide))
                TextFits(guide);
        }

        private static void CheckSummary(Button summary, FellowshipView view)
        {
            Assert.AreEqual((view.MordorStep ?? view.Progress).ToString() + (view.MordorStep.HasValue ? "/5" : ""),
                summary.Q<Label>("fellowship-status-progress").text);
            Assert.AreEqual(view.Corruption.ToString(), summary.Q<Label>("fellowship-status-corruption").text);
            var children = summary.Children().ToArray();
            for (var i = 0; i < children.Length; i++)
            {
                AssertInside(children[i], summary);
                for (var j = i + 1; j < children.Length; j++)
                    Assert.IsFalse(children[i].worldBound.Overlaps(children[j].worldBound), "Shortcut children overlap.");
                if (children[i] is Label label)
                {
                    TextFits(label);
                    var complete = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined,
                        0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(complete.x, label.contentRect.width + 1, "Truncated shortcut: " + label.text);
                }
            }
        }

        private static bool Visible(VisualElement element) => element != null && element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0 &&
            (element.parent == null || Visible(element.parent));

        private static void TextFits(Label label)
        {
            Assert.IsFalse(label.isElided, label.text);
            var size = label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.Exactly,
                0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(size.y, label.contentRect.height + 1, "Clipped: " + label.text);
        }

        private static void AssertInside(VisualElement child, VisualElement parent)
        {
            var where = child.name + " " + child.worldBound + " in " + parent.name + " " + parent.worldBound;
            Assert.GreaterOrEqual(child.worldBound.xMin, parent.worldBound.xMin - .5f, where);
            Assert.GreaterOrEqual(child.worldBound.yMin, parent.worldBound.yMin - .5f, where);
            Assert.LessOrEqual(child.worldBound.xMax, parent.worldBound.xMax + .5f, where);
            Assert.LessOrEqual(child.worldBound.yMax, parent.worldBound.yMax + .5f, where);
        }
    }
}
