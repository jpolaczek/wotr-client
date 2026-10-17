using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    // UI-98: a small corner tab at the top right of the map shows the Fellowship; hovering it unrolls the full
    // Fellowship panel over it, without a pin, and a decision it hosts holds it. UI-160/UI-162: the Hunt plaque sits
    // in the turn console and opens the Hunt window.
    public sealed class EdgePlaqueTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public IEnumerator CornerTabShowsTheFellowshipAndTheEyesInTheMapsTopRightCornerInFourLayouts()
        {
            var views = Fixture();
            using var client = new MountedMatchClient("http://localhost", "edge-plaques");
            foreach (var (width, scale) in Layouts)
            {
                var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                target.Create();
                try
                {
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    foreach (var key in new[] { "unrolled-FreePeoples", "unrolled-Shadow" })
                        foreach (var free in new[] { 0, 1 })
                        {
                            var view = WithHunt(views[key], 2, free);
                            client.Apply(view);
                            for (var frame = 0; frame < 6; frame++)
                                yield return null;
                            var label = $"{key} free {free} {width} {scale:0.00}";
                            LayoutTests.Save(target, $"ui98-plaques-{key}-{free}-{width}-{scale:0.00}.png");
                            CheckPlaques(client.Board, label);
                            CheckFellowship(client.Board, view.Fellowship, label);
                            CheckHunt(client.Board, 2, free, label);
                        }
                }
                finally
                {
                    client.Panel.targetTexture = client.Viewport;
                    client.Interface.Preferences.SetScale(1);
                    target.Release();
                    Object.Destroy(target);
                }
            }
        }

        [UnityTest]
        public IEnumerator PanelsHostingTheDecisionStayOpenAndRollBackAfterIt()
        {
            var views = Fixture();
            using var client = new MountedMatchClient("http://localhost", "edge-plaques-held");
            var board = client.Board;
            var fellowshipPhase = Copy(views["unrolled-FreePeoples"]);
            Assert.AreEqual(DecisionKind.FellowshipPhase, fellowshipPhase.Decision.Kind);
            client.Apply(fellowshipPhase);
            for (var frame = 0; frame < 6; frame++)
                yield return null;
            // The corner panels unroll in 140 ms; measure where they rest, not mid-slide.
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(board.Fellowship.Held && Visible(board.Fellowship.Root), "The Fellowship phase holds its panel open.");
            Assert.IsTrue(board.Fellowship.Root.ClassListContains("edge-permanent-panel"), "Esc passes a held panel by.");
            Assert.IsTrue(board.Edges.Corner.ClassListContains(EdgePlaque.OpenClass), "The corner tab is lit while a panel is unrolled.");
            Assert.IsFalse(Visible(board.Hunt.Root));
            CheckDropdown(board, board.Fellowship.Root, "fellowship phase");

            // Later projections carry later revisions, as from the server.
            var allocation = Copy(fellowshipPhase);
            allocation.Revision = fellowshipPhase.Revision + 1;
            allocation.Phase = GamePhase.HuntAllocation;
            allocation.Decision = new DecisionView { Id = "hunt-allocation", Kind = DecisionKind.AllocateHunt };
            allocation.DecisionOwner = Side.Shadow;
            allocation.Options.Clear();
            client.Refresh(allocation);
            for (var frame = 0; frame < 6; frame++)
                yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            // UI-162: the allocation belongs to the turn console; no Hunt window opens for it.
            Assert.IsFalse(board.Hunt.Held || Visible(board.Hunt.Root), "The Hunt allocation opens no window.");
            Assert.IsFalse(Visible(board.Fellowship.Root), "The Fellowship panel rolls back after its decision.");

            var action = Copy(allocation);
            action.Revision = allocation.Revision + 1;
            action.Phase = GamePhase.ActionResolution;
            action.Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action };
            action.DecisionOwner = Side.FreePeoples;
            client.Refresh(action);
            for (var frame = 0; frame < 6; frame++)
                yield return null;
            Assert.IsFalse(board.Hunt.Held);
            Assert.IsFalse(Visible(board.Hunt.Root));
            Assert.IsFalse(Visible(board.Fellowship.Root));
        }

        [UnityTest]
        public IEnumerator HoveringTheCornerTabUnrollsTheFullPanelsOverTheHandWithoutAPin() => MatchTestRoom.Wait(Hover());

        [UnityTest]
        public IEnumerator EscapeRestoresPlaqueFocusWithoutReopeningAndFreshHoverStillWorks() => MatchTestRoom.Wait(EscapeFocus());

        private static async Task EscapeFocus()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var snapshot = JsonConvert.SerializeObject(board.View);
                foreach (var fellowship in new[] { true, false })
                {
                    CardGestures.Move(board.Phase);
                    board.Fellowship.Close();
                    board.Hunt.Close();
                    await Task.Delay(420, room.Token);
                    Assert.IsFalse(board.Fellowship.Held || board.Hunt.Held);
                    if (fellowship)
                        board.Fellowship.Open();
                    else
                        board.Hunt.Open();
                    await room.Settle();
                    var panel = fellowship ? board.Fellowship.Root : board.Hunt.Root;
                    var fallback = fellowship ? board.Fellowship.Status : board.Hunt.Tab;
                    var close = panel.Q<Button>(fellowship ? "close-fellowship" : "close-hunt");
                    close.Focus();
                    BoardMenuLayoutTests.Key(close, true);
                    BoardMenuLayoutTests.Key(close, false);
                    await room.Settle();
                    Assert.IsFalse(board.Fellowship.IsOpen || board.Hunt.IsOpen,
                        $"Esc must not unfold the corner on restored focus: {board.View.You}, Fellowship={fellowship}, preview={board.Edges.Preview.Previewing}.");
                    Assert.AreSame(fallback, board.Root.panel.focusController.focusedElement);
                    var nextPlaque = fellowship ? board.Hunt.Tab : board.Fellowship.Status;
                    nextPlaque.Focus();
                    await room.Settle();
                    Assert.AreSame(nextPlaque, board.Root.panel.focusController.focusedElement);
                    // UI-160: only the Fellowship plaque unfolds on focus; the console's Hunt plaque opens by a click.
                    if (fellowship)
                        Assert.IsFalse(board.Hunt.IsOpen || board.Fellowship.IsOpen, "Focus on the Hunt plaque opens nothing.");
                    else
                        Assert.IsTrue(board.Fellowship.IsOpen && !board.Hunt.IsOpen, "Fresh keyboard focus on the Fellowship plaque unfolds the corner.");
                    nextPlaque.Blur();
                    board.Fellowship.Close();
                    board.Hunt.Close();
                    CardGestures.Move(board.Phase);
                    await Task.Delay(300, room.Token);
                    await ComponentGestures.Hover(board.Edges.Corner);
                    Assert.IsTrue(board.Fellowship.IsOpen && !board.Hunt.IsOpen, "Fresh hover must still unfold the Fellowship panel.");
                    CardGestures.Move(board.Phase);
                    await Task.Delay(420, room.Token);
                    Assert.IsFalse(board.Fellowship.IsOpen || board.Hunt.IsOpen);
                }
                Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
            }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task Hover()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            var client = room.Fp;
            var board = client.Board;
            var revision = board.View.Revision;
            foreach (var (width, scale) in Layouts)
            {
                var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                texture.Create();
                try
                {
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    await room.Settle();
                    await room.Settle();
                    await CheckHover(board, texture, $"{width}-{scale:0.00}");
                }
                finally
                {
                    client.Panel.targetTexture = client.Viewport;
                    client.Interface.Preferences.SetScale(1);
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(0, room.Boundary.Commands, "Unrolling a plaque's panel never sends a game command.");
        }

        private static async Task CheckHover(BoardScreen board, RenderTexture texture, string label)
        {
            var fellowship = board.Fellowship.Root;
            var hunt = board.Hunt.Root;
            var corner = board.Edges.Corner;
            Assert.IsFalse(board.Fellowship.Held || board.Hunt.Held, label);
            Assert.IsFalse(Visible(fellowship) || Visible(hunt), "No decision holds the panels · " + label);
            var hand = board.Hand.worldBound;
            CardGestures.Move(corner);
            await Task.Delay(300);
            Assert.IsTrue(Visible(fellowship), "Hovering the corner tab unrolls the Fellowship panel · " + label);
            Assert.IsFalse(Visible(hunt), "UI-160: the corner holds only the Fellowship · " + label);
            Assert.AreEqual("corner", board.Edges.Preview.Previewing);
            Assert.IsTrue(board.Edges.CornerPanels.ClassListContains(LeftRailPreview.PreviewClass));
            Assert.IsTrue(corner.ClassListContains(EdgePlaque.OpenClass), label);
            Assert.IsFalse(Visible(fellowship.Q<Button>("close-fellowship")), "A hovered panel closes by itself · " + label);
            Assert.LessOrEqual(fellowship.worldBound.yMin, corner.worldBound.yMin + 1, "The full form covers the corner tab · " + label);
            CheckDropdown(board, fellowship, label);
            Assert.AreEqual(hand, board.Hand.worldBound, "The panel lies over the hand without moving it · " + label);
            var portrait = fellowship.Query<Button>(className: "companion-token").ToList().First(Visible);
            var picked = fellowship.panel.Pick(portrait.worldBound.center);
            Assert.IsTrue(picked == portrait || portrait.Contains(picked), "The panel is interactive at once · " + label);
            LayoutTests.Save(texture, $"ui98-fellowship-unrolled-{label}.png");
            CardGestures.Move(portrait);
            await Task.Delay(300);
            Assert.IsTrue(Visible(fellowship), "Reading the panel keeps it open · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            Assert.IsFalse(Visible(fellowship) || Visible(hunt), "Leaving rolls the panels back · " + label);
            Assert.IsFalse(corner.ClassListContains(EdgePlaque.OpenClass), label);
            Assert.IsTrue(Visible(corner), "The corner tab returns · " + label);

            // UI-160/UI-162: the Hunt plaque in the console is a button; hovering it opens nothing, a click opens the
            // Hunt window, which stands on the map and not in the corner.
            CardGestures.Move(board.Hunt.Tab);
            await Task.Delay(300);
            Assert.IsFalse(Visible(hunt) || Visible(fellowship), "Hovering the console's Hunt plaque opens nothing · " + label);
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await Task.Delay(120);
            Assert.IsTrue(Visible(hunt), "A click on the Hunt plaque opens the Hunt window · " + label);
            Assert.IsTrue(Visible(hunt.Q("hunt-box")), "An idle Hunt lists the Hunt Box · " + label);
            Assert.IsFalse(board.Edges.CornerPanels.Contains(hunt), "The Hunt window is not a corner panel · " + label);
            Assert.LessOrEqual(hunt.worldBound.yMax, board.Console.Root.worldBound.yMin + 1, "The window stays above the console · " + label);
            PointerGestureEvents.ClickAt(hunt.Q<Button>("close-hunt"));
            await Task.Delay(120);
            Assert.IsFalse(Visible(hunt), "The Hunt window closes with its ✕ · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            board.Fellowship.Status.Focus();
            await Task.Delay(260);
            Assert.IsTrue(Visible(fellowship), "Keyboard focus on the Fellowship plaque unrolls the full form · " + label);
            board.Fellowship.Status.Blur();
            await Task.Delay(420);
            Assert.IsFalse(Visible(fellowship), label);
        }

        private static void CheckPlaques(BoardScreen board, string label)
        {
            var bar = board.TopBar.Root;
            Assert.That(bar.resolvedStyle.height, Is.EqualTo(44).Within(.5f), "The bar is slimmer than UI-86 · " + label);
            var corner = board.Edges.Corner;
            Assert.IsTrue(Visible(corner), label);
            Assert.AreEqual("panel", Texture(corner), "The corner tab wears the panels' gilt frame · " + label);
            Assert.That(corner.worldBound.yMin - bar.worldBound.yMax, Is.InRange(-1f, 2f), "The tab hangs from the bar · " + label);
            Assert.That(board.Root.worldBound.xMax - corner.worldBound.xMax, Is.InRange(-.5f, 1f), "In the map's top right corner · " + label);
            Assert.LessOrEqual(corner.worldBound.height, 48, "A small basic tab · " + label);
            Assert.IsEmpty(corner.tooltip, "Hovering unrolls the panels, so no tooltip covers them · " + label);
            var fellowship = board.Fellowship.Status;
            var hunt = board.Hunt.Tab;
            // UI-160: the corner holds the Fellowship; the Hunt plaque sits in the turn console's Hunt Box.
            foreach (var (plaque, frame) in new[] { (fellowship, corner), (hunt, board.Console.Hunt.Well) })
            {
                Assert.IsTrue(Visible(plaque), plaque.name + " · " + label);
                Assert.AreSame(frame, plaque.parent, label);
                Inside(plaque.worldBound, frame.worldBound, plaque.name + " · " + label);
                Assert.GreaterOrEqual(plaque.worldBound.height + .5f, 32, "Click target · " + label);
                foreach (var text in plaque.Query<Label>().ToList().Where(Visible))
                    Assert.LessOrEqual(Width(text), text.contentRect.width + 1, text.name + " fits · " + label);
            }
            Assert.IsEmpty(fellowship.tooltip, "Hovering unrolls the Fellowship, so no tooltip covers it · " + label);
            StringAssert.Contains("Click to open the Hunt", hunt.tooltip, label);
            var notices = board.Notifications.Root;
            if (Visible(notices))
                Assert.LessOrEqual(notices.worldBound.xMax, corner.worldBound.xMin + 1, "The notification bar ends before the tab · " + label);
        }

        private static void CheckFellowship(BoardScreen board, FellowshipView fellowship, string label)
        {
            var plaque = board.Fellowship.Status;
            var region = board.Presentation.Regions.TryGetValue(fellowship.RegionId, out var shape) ? shape.Name : fellowship.RegionId;
            Assert.AreEqual(fellowship.MordorStep.HasValue ? "Mordor" : region, plaque.Q<Label>("fellowship-status-location").text, label);
            Assert.AreEqual(fellowship.Hidden ? "Hidden" : "Revealed", plaque.Q<Label>("fellowship-status-condition").text, label);
            Assert.AreEqual(fellowship.Progress.ToString(), plaque.Q<Label>("fellowship-status-progress").text, label);
            Assert.AreEqual(fellowship.Corruption.ToString(), plaque.Q<Label>("fellowship-status-corruption").text, label);
            foreach (var symbol in plaque.Query<Image>(className: "fellowship-symbol").ToList())
            {
                Assert.IsNotNull(symbol.sprite, label);
                Assert.Greater(symbol.worldBound.width, 12, "Symbols stay readable · " + label);
            }
            Assert.GreaterOrEqual(plaque.Q(className: EdgePlaque.EmblemClass).worldBound.width + .5f, 28, "The condition emblem · " + label);
        }

        private static void CheckHunt(BoardScreen board, int eyes, int free, string label)
        {
            var plaque = board.Hunt.Tab;
            Assert.AreEqual("Hunt", plaque.text, label);
            Assert.AreEqual(eyes.ToString(), plaque.Q<Label>("hunt-eyes").text, "The Eyes in the Hunt Box · " + label);
            // UI-160: both counters keep their place in the console, zero included.
            Assert.IsTrue(Visible(plaque.Q("hunt-free-count")), "The Free Peoples dice counter keeps its place · " + label);
            Assert.AreEqual(free.ToString(), plaque.Q<Label>("hunt-free-dice").text, label);
            Inside(plaque.Q("hunt-box-counts").worldBound, plaque.worldBound, "hunt-box-counts · " + label);
            Assert.AreEqual("eye", Texture(plaque.Q(className: DieFaceIcon.ClassName)), label);
            Assert.AreEqual("medallion-shadow", Texture(plaque.Q(className: "hunt-eye-medallion")), label);
        }

        // An unrolled panel hangs under the bar at the right edge and stays on screen.
        private static void CheckDropdown(BoardScreen board, VisualElement panel, string label)
        {
            var bar = board.TopBar.Root.worldBound;
            var form = board.Edges.CornerPanels.worldBound;
            Assert.That(form.yMin - bar.yMax, Is.InRange(-1f, 4f), "The full form hangs from the bar · " + label);
            Assert.GreaterOrEqual(panel.worldBound.yMin + 1, form.yMin, label);
            var notices = board.Notifications.Root;
            if (Visible(notices))
                Assert.LessOrEqual(notices.worldBound.xMax, panel.worldBound.xMin + 1, "The notification bar ends before the panel · " + label);
            Assert.That(board.Root.worldBound.xMax - panel.worldBound.xMax, Is.InRange(-.5f, 4f), "At the right edge · " + label);
            Assert.LessOrEqual(panel.worldBound.yMax, board.Root.worldBound.yMax + 1, "On screen · " + label);
            Assert.AreEqual("panel", Texture(panel), label);
        }

        private static Dictionary<string, PlayerView> Fixture()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-resources.json");
            return JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
        }

        private static PlayerView Copy(PlayerView view) => JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));

        // Moves Shadow dice and Free Peoples dice into the Hunt Box, as Eyes and movement dice.
        private static PlayerView WithHunt(PlayerView source, int eyes, int free)
        {
            var view = Copy(source);
            foreach (var die in view.Dice.Where(d => d.Owner == Side.Shadow).Take(eyes))
            {
                die.Location = DieLocation.Hunt;
                die.Face = DieFace.Eye;
            }
            foreach (var die in view.Dice.Where(d => d.Owner == Side.FreePeoples).Take(free))
            {
                die.Location = DieLocation.Hunt;
                die.Face = DieFace.Character;
            }
            return view;
        }

        private static float Width(TextElement text) =>
            text.MeasureTextSize(text.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;

        private static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, label);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1, label);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, label);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, label);
        }

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }
    }
}
