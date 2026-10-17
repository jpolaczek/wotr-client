using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    public sealed partial class CouncilHomelandPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator FivePortraitsAndTokenPreviewFitBothRolesAtFourLayoutsWithoutScrolling() => MatchTestRoom.Wait(Layouts());

        private static async Task Layouts()
        {
            using var room = await UI10TestRoom.Connect("ui69-layout");
            var before = JsonConvert.SerializeObject(room.Sh.Board.View);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    foreach (var client in new[] { room.Fp, room.Sh })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        try
                        {
                            client.Panel.targetTexture = target;
                            client.Interface.Preferences.SetScale(scale);
                            client.Board.Windows.Clear();
                            await room.Settle();
                            if (client == room.Fp)
                            {
                                await Place(room, Gimli, "iron-hills", false);
                                await Place(room, Gimli, "ered-luin", true);
                                LayoutTests.Save(target, $"ui69-homelands-{client.Board.View.You}-{width}-{scale:0.00}.png");
                                AssertHomelandPanel(client.Board);
                                await CheckHomelandKeyboard(room);
                                Assert.AreEqual("Shadow chooses one", Effect(client.Board));
                            }
                            else
                                AssertPrivate(client.Board);
                            ExpansionMatrixLayoutTests.ResourcesFit(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            LayoutTests.Save(target, $"ui69-homelands-{client.Board.View.You}-{width}-{scale:0.00}.png");
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            client.Interface.Preferences.SetScale(1f);
                            target.Release();
                            Object.Destroy(target);
                        }
                    }
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.AreEqual(before, JsonConvert.SerializeObject(await room.Sh.Api.Get<Wotr.Contracts.PlayerView>(
                "api/matches/" + room.Sh.Board.View.MatchId, room.Token)));
        }

        private static async Task CheckHomelandKeyboard(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            board.CompanionSetup.Root.Focus();
            UI10TestRoom.Key(board.CompanionSetup.Root, true);
            UI10TestRoom.Key(board.CompanionSetup.Root, false);
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands, "Enter on information cannot confirm a strategic choice.");
            using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                board.CompanionSetup.Root.SendEvent(escape);
            await room.Settle();
            Assert.IsFalse(board.CompanionSetup.IsOpen);
            using (var escape = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                board.CompanionSetup.Tab.SendEvent(escape);
            var camera = board.Map.Camera.Pan;
            PointerGestureEvents.ClickAt(board.CompanionSetup.Tab);
            await room.Settle();
            Assert.IsTrue(board.CompanionSetup.IsOpen);
            Assert.AreEqual(camera, board.Map.Camera.Pan);
            Assert.AreEqual("→ Ered Luin", Portrait(board, Gimli).Q<Label>("homeland-location").text);
        }

        private static void AssertHomelandPanel(BoardScreen board)
        {
            var panel = board.CompanionSetup.Root;
            var viewport = board.Map.parent.worldBound;
            Assert.AreEqual(5, panel.Query<Button>(className: "homeland-portrait").ToList().Count);
            Assert.GreaterOrEqual(panel.worldBound.xMin, viewport.xMin - 1);
            Assert.GreaterOrEqual(panel.worldBound.yMin, viewport.yMin - 1);
            Assert.LessOrEqual(panel.worldBound.xMax, viewport.xMax + 1);
            Assert.LessOrEqual(panel.worldBound.yMax, viewport.yMax + 1);
            Assert.IsEmpty(panel.Query<TextField>().ToList());
            foreach (var element in panel.Query<VisualElement>().ToList().Where(e => e.worldBound.height > 0 &&
                e.resolvedStyle.display != DisplayStyle.None && (e is Button || e is Label)))
            {
                Assert.GreaterOrEqual(element.worldBound.xMin, panel.worldBound.xMin - 1, element.name);
                Assert.LessOrEqual(element.worldBound.xMax, panel.worldBound.xMax + 1, element.name);
                Assert.LessOrEqual(element.worldBound.yMax, panel.worldBound.yMax + 1, element.name);
                if (element is Label label)
                {
                    var measured = label.MeasureTextSize(label.text, label.contentRect.width,
                        VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(measured.y, label.contentRect.height + 1, label.text);
                    var portrait = label.GetFirstAncestorOfType<Button>();
                    if (portrait != null)
                    {
                        Assert.GreaterOrEqual(label.worldBound.yMin, portrait.worldBound.yMin - 1, label.text);
                        Assert.LessOrEqual(label.worldBound.yMax, portrait.worldBound.yMax + 1, label.text);
                    }
                }
                if (element is Button button && button.enabledInHierarchy)
                {
                    var picked = button.panel.Pick(button.worldBound.center);
                    Assert.IsTrue(picked == button || button.Contains(picked), "Covered: " + button.name);
                }
            }
        }
    }
}
