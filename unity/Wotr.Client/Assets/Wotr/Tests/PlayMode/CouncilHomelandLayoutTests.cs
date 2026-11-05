using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

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
                                AssertConsoleHint(client.Board, scale);
                                LayoutTests.Save(target, $"ui482-homelands-placed-{width}-{scale:0.00}.png");
                                await CheckHomelandKeyboard(room);
                                StringAssert.Contains("Shadow chooses one Council token", Effect(client.Board));
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
            Assert.AreEqual(before, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                "api/matches/" + room.Sh.Board.View.MatchId, room.Token)));
        }

        // UI-482 (owner, 2026-11-04): the panel keeps no instruction or token line; the console's hint names both, in its two
        // lines at 100 %. At 125 % a Lords and Kings game leaves the hint a narrow line (at 1920×1080 too, the same logical
        // width), so the longest hints end in an ellipsis with the full text in the tooltip.
        private static void AssertConsoleHint(BoardScreen board, float scale)
        {
            var hint = board.Console.Status.Hint;
            StringAssert.Contains("Shadow chooses one Council token · ✓ confirms.", hint.text);
            StringAssert.StartsWith(hint.text + "\n", hint.tooltip);
            StringAssert.Contains(CouncilHomelandPresentation.TokenDetail(1), hint.tooltip);
            StringAssert.Contains(CouncilHomelandPresentation.TravelToken, hint.tooltip);
            foreach (var removed in new[] { "homeland-hint", "homeland-tokens", "homeland-token-effect" })
                Assert.IsNull(board.Root.Q(removed), removed);
            if (scale < 1.2f)
                foreach (var text in LongestHints(board.View))
                    Assert.LessOrEqual(HintHeight(hint, text), hint.contentRect.height + 1, text);
        }

        private static float HintHeight(Label hint, string text) => hint.MeasureTextSize(text, hint.contentRect.width,
            VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y;

        // Every instruction of the step, each with the longest token consequence and the companions of this game.
        private static IEnumerable<string> LongestHints(PlayerView view)
        {
            var option = CompanionSetupPresentation.HomelandOption(view);
            var other = new UnitPlacementChoice { UnitId = "other", RegionId = "the-shire" };
            yield return CouncilHomelandPresentation.Hint(view, HintDraft("", other, other));
            yield return CouncilHomelandPresentation.Hint(view, Waiting(option, other));
            foreach (var choice in option.CharacterActions)
            {
                var placed = new UnitPlacementChoice { UnitId = choice.CharacterId, RegionId = choice.Targets[0] };
                yield return CouncilHomelandPresentation.Hint(view, HintDraft(choice.CharacterId, placed, other));
                yield return CouncilHomelandPresentation.Hint(view, HintDraft(choice.CharacterId, other, other));
            }
        }

        private static TurnDraft HintDraft(string selected, params UnitPlacementChoice[] placements)
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCouncilHomelands);
            draft.Change(payload =>
            {
                payload.CharacterId = selected;
                payload.Placements.AddRange(placements);
            });
            return draft;
        }

        // UI-451: a homeland two companions share waits for a portrait, and the hint names them both.
        private static TurnDraft Waiting(ActionOption option, UnitPlacementChoice other)
        {
            var draft = HintDraft("", other, other);
            var shared = option.CharacterActions.SelectMany(choice => choice.Targets).GroupBy(region => region).First(group => group.Count() > 1).Key;
            Assert.IsTrue(CouncilDraft.Region(draft, option, shared));
            Assert.IsNotEmpty(CouncilDraft.Waiting(draft));
            return draft;
        }

        // Escape is the board's Back, so it undoes the last placement and keeps the held Fellowship panel and the camera.
        private static async Task CheckHomelandKeyboard(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            var camera = board.Map.Camera.Pan;
            using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                board.Fellowship.Root.SendEvent(escape);
            using (var escape = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                board.Fellowship.Root.SendEvent(escape);
            await room.Settle();
            Assert.IsTrue(board.Fellowship.IsOpen, "The Fellowship panel holds the homelands.");
            Assert.AreEqual(camera, board.Map.Camera.Pan);
            Assert.AreEqual("→ Iron Hills", Portrait(board, Gimli).Q<Label>("homeland-location").text, "Escape undid the last placement.");
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static void AssertHomelandPanel(BoardScreen board)
        {
            var panel = board.Fellowship.Root;
            var viewport = board.Map.parent.worldBound;
            Assert.AreEqual(5, panel.Query<Button>(className: "companion-token").ToList().Count(b => b.ClassListContains("legal-target")));
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
