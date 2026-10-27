using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class PoliticalPanelTests
    {
        [UnityTest]
        public IEnumerator AllTracksAlignAndRemainClickableWithoutScrollingAtBothScales()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-layout");
            var fixture = Fixture();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                    {
                        fixture.View.You = side;
                        client.Apply(fixture.View);
                        yield return Settle();
                        var panel = client.Board.Nations;
                        CardNetworkTests.Click(panel.Root);
                        yield return Settle();
                        Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.resolvedStyle.display);
                        Inside(panel.Drawer.worldBound, client.Board.Map.worldBound);
                        Assert.IsEmpty(panel.Drawer.Query<ScrollView>().ToList());
                        var axis = panel.Drawer.Q(className: "politics-axis");
                        var headings = axis.Query<Label>(className: "politics-axis-step").ToList();
                        for (var step = 0; step < headings.Count; step++)
                        {
                            Inside(headings[step].worldBound, axis.worldBound);
                            Assert.That(headings[step].worldBound.center.x, Is.EqualTo(panel.Drawer
                                .Q<Button>("political-slot-north-" + (3 - step)).worldBound.center.x).Within(1));
                        }
                        Inside(panel.ShowOpponent.worldBound, panel.Drawer.Q(className: "politics-header").worldBound);
                        // UI-197: in the narrower panel the title and the toggle still stand apart.
                        var title = panel.Drawer.Q(className: "politics-header").Q<Label>();
                        Assert.IsFalse(title.worldBound.Overlaps(panel.ShowOpponent.worldBound), "Title and toggle overlap.");
                        var first = panel.Drawer.Q("politics-tracks").ElementAt(0);
                        var expected = fixture.Data.Board.Nations.First(n => n.Side == side).Id;
                        Assert.AreEqual("nation-track-" + expected, first.name);
                        foreach (var nation in fixture.View.Nations)
                        {
                            var token = Token(panel, nation.Id);
                            var slot = panel.Drawer.Q<Button>("political-slot-" + nation.Id + "-" + nation.StepsToWar);
                            Inside(token.worldBound, panel.Drawer.worldBound);
                            Assert.That(token.worldBound.width, Is.EqualTo(token.worldBound.height).Within(1));
                            Assert.That(token.worldBound.center.x, Is.EqualTo(slot.worldBound.center.x).Within(1));
                            // UI-197: an active nation's crest is lit, a passive one's dimmed.
                            Assert.AreEqual(!nation.Active, token.ClassListContains("passive-nation"));
                            Assert.IsNotNull(token.Q("nation-emblem-" + nation.Id));
                            Hit(token);
                        }
                        Hit(panel.ShowOpponent);
                        var stable = Token(panel, "north");
                        CardNetworkTests.Click(panel.ShowOpponent);
                        yield return Settle();
                        Assert.IsFalse(panel.ShowOpponent.value);
                        foreach (var nation in fixture.Data.Board.Nations)
                            Assert.AreEqual(nation.Side == side ? DisplayStyle.Flex : DisplayStyle.None,
                                panel.Drawer.Q("nation-track-" + nation.Id).resolvedStyle.display);
                        CardNetworkTests.Click(panel.ShowOpponent);
                        yield return Settle();
                        Assert.AreSame(stable, Token(panel, "north"));
                        AssertTrackRows(panel, label: $"{side}-{width}-{scale:0.00}");
                        if (side == Side.FreePeoples)
                        {
                            // UI-144: without an activation option no nation shows Activate; the token explains activation.
                            Assert.AreEqual(Visibility.Hidden, panel.Drawer.Q<Button>("nation-activate-north").resolvedStyle.visibility);
                            Assert.That(stable.tooltip, Does.Contain("Muster advances"));
                        }
                        LayoutTests.Save(target, $"ui25-politics-{side}-{width}-{scale:0.00}.png");
                    }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        [UnityTest]
        public IEnumerator HoverTabTokenAndButtonShareSafeAreasAndNeverSelectOrMoveAnything()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-hover");
            client.Apply(Fixture().View);
            yield return Settle();
            var board = client.Board;
            var panel = board.Nations;
            var camera = board.Map.Camera.Pan;
            Move(panel.Root);
            yield return new WaitForSecondsRealtime(.28f);
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.resolvedStyle.display);
            Assert.IsTrue(panel.Drawer.ClassListContains(LeftRailPreview.PreviewClass));
            Assert.IsEmpty(board.ActionDie.DieId);
            PointerGestureEvents.ClickAt(panel.Root);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.resolvedStyle.display);
            PointerGestureEvents.ClickAt(Token(panel, "north"));
            yield return new WaitForSecondsRealtime(.28f);
            var activate = panel.Drawer.Q<Button>("nation-activate-north");
            Assert.AreEqual(Visibility.Hidden, activate.resolvedStyle.visibility, "A token click shows no activation that is not offered.");
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.resolvedStyle.display);
            PointerGestureEvents.ClickAt(Token(panel, "elves"));
            yield return new WaitForSecondsRealtime(.22f);
            Assert.AreEqual(Visibility.Hidden, activate.resolvedStyle.visibility);
            Move(board.Phase);
            yield return new WaitForSecondsRealtime(.22f);
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.resolvedStyle.display, "Leaving preserves explicit inspection.");
            Assert.IsEmpty(board.ActionDie.DieId);
            Assert.IsFalse(Token(panel, "north").ClassListContains("selected"));
            Assert.AreEqual(0, board.View.Revision);
            Assert.AreEqual(camera, board.Map.Camera.Pan);
        }

        [UnityTest]
        public IEnumerator KeyboardFocusAndEscapeWorkAndRevisionsReuseTokens()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-focus");
            var fixture = Fixture();
            client.Apply(fixture.View);
            yield return Settle();
            var board = client.Board;
            board.Root.Focus();
            Key(board.Root, KeyCode.Tab);
            board.Nations.Root.Focus();
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display);
            Assert.IsTrue(board.Nations.Drawer.ClassListContains(LeftRailPreview.PreviewClass));
            using (var submit = NavigationSubmitEvent.GetPooled())
                board.Nations.Root.SendEvent(submit);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display);
            var token = Token(board.Nations, "north");
            token.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
                token.SendEvent(submit);
            yield return Settle();
            Assert.AreEqual(Visibility.Hidden, board.Nations.Drawer.Q<Button>("nation-activate-north").resolvedStyle.visibility);
            var nation = fixture.View.Nations.Single(n => n.Id == "north");
            nation.StepsToWar = 1;
            nation.Active = true;
            fixture.View.Revision++;
            board.Apply(fixture.View);
            yield return Settle();
            // UI-327: the activation turns the token over; its active side shows from the middle of the turn.
            yield return new WaitForSecondsRealtime((float)TokenFlip.HalfSeconds * 2 + .1f);
            Assert.AreSame(token, Token(board.Nations, "north"));
            Assert.IsFalse(token.ClassListContains("passive-nation"));
            Assert.IsTrue(token.ClassListContains("active-nation"));
            Assert.That(token.worldBound.center.x, Is.EqualTo(board.Nations.Drawer.Q<Button>("political-slot-north-1").worldBound.center.x).Within(1));
            Assert.AreEqual(Visibility.Hidden, board.Nations.Drawer.Q<Button>("nation-activate-north").resolvedStyle.visibility);
            token.Blur();
            token.Focus();
            yield return Settle();
            var description = client.Interface.Content.parent.Q<Label>("runtime-tooltip");
            Assert.AreEqual(DisplayStyle.Flex, description.resolvedStyle.display);
            var die = board.ActionDie.DieId;
            BoardMenuLayoutTests.Key(token, true);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, description.resolvedStyle.display);
            Assert.IsTrue(Appearance.Closed(board.Nations.Drawer), "A passive description must not take the first Esc from Politics.");
            Assert.AreSame(board.Nations.Root, board.Root.panel.focusController.focusedElement);
            BoardMenuLayoutTests.Key(board.Nations.Root, true);
            yield return Settle();
            Assert.IsTrue(Appearance.Closed(board.Nations.Drawer));
            Assert.IsFalse(board.Menu.IsOpen, "A held Esc cannot reach the next owner after closing Politics.");
            Assert.AreEqual(DisplayStyle.None, description.resolvedStyle.display,
                "Returning focus during Esc cannot reopen the passive description.");
            BoardMenuLayoutTests.Key(board.Nations.Root, false);
            Assert.AreEqual(die, board.ActionDie.DieId);
            Assert.AreEqual(1, board.View.Revision);
        }

        [UnityTest]
        public IEnumerator EveryLegalStepAndActivationShowsWithoutSelectingANation()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-always");
            var fixture = Fixture();
            var isengard = fixture.View.Nations.Single(n => n.Id == "isengard");
            isengard.StepsToWar = 1;
            fixture.View.Options.Single(o => o.Kind == CommandKind.AdvancePolitics).Targets.Add("isengard");
            fixture.View.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseNarya, Enabled = true,
                EligibleDice = new List<string> { "muster" }, Targets = new List<string> { "north", "dwarves", "elves" }
            });
            client.Apply(fixture.View);
            yield return Settle();
            var board = client.Board;
            var panel = board.Nations;
            panel.Open();
            yield return Settle();
            Assert.IsEmpty(Arrows(panel), "Without a Muster die no nation can move.");
            var north = panel.Drawer.Q<Button>("nation-activate-north");
            Assert.AreEqual(Visibility.Visible, north.resolvedStyle.visibility, "Narya's activation shows without a click.");
            Assert.IsFalse(north.enabledInHierarchy);
            StringAssert.Contains("Select an available action die", north.tooltip);
            Assert.AreEqual(Visibility.Hidden, panel.Drawer.Q<Button>("nation-activate-elves").resolvedStyle.visibility,
                "An active nation cannot be activated.");

            ArmyGestures.Die(client, "muster", DieFace.Muster);
            panel.Open();
            yield return Settle();
            CollectionAssert.AreEquivalent(new[] { "political-slot-elves-1", "political-slot-north-2" }, Arrows(panel).Select(b => b.name),
                "Every own nation the die can move shows its arrow; an opponent's never does.");
            foreach (var arrow in Arrows(panel))
            {
                Assert.IsTrue(arrow.enabledInHierarchy);
                Hit(arrow);
                // UI-178: the station is lit without a sign of its own; the row's arrow carries the step.
                Assert.IsEmpty(arrow.text);
                var advance = panel.Drawer.Q<Button>("nation-advance-" + arrow.name.Split('-')[2]);
                Assert.IsTrue(advance.enabledInHierarchy && advance.ClassListContains("legal-target"), advance.name);
            }
            Assert.IsEmpty(panel.Drawer.Query<Button>(className: "destination-selected").ToList());
            Assert.IsFalse(Token(panel, "isengard").ClassListContains("legal-target"));
            foreach (var id in new[] { "north", "dwarves" })
            {
                var activate = panel.Drawer.Q<Button>("nation-activate-" + id);
                Assert.AreEqual(Visibility.Visible, activate.resolvedStyle.visibility);
                Assert.IsTrue(activate.enabledInHierarchy, id);
                StringAssert.Contains("Click to use Narya with the selected die", activate.tooltip);
                Inside(activate.worldBound, panel.Drawer.worldBound);
                Assert.That(activate.worldBound.xMin, Is.GreaterThanOrEqualTo(Token(panel, id).worldBound.xMax));
                Hit(activate);
            }
            Assert.IsTrue(panel.Root.ClassListContains("legal-target"));
            LayoutTests.Save(client.Viewport, "ui144-politics-actions.png");
            Assert.AreEqual(0, board.View.Revision, "Showing the actions sends nothing.");
        }

        private static List<Button> Arrows(PoliticalPanel panel) => panel.Drawer.Query<Button>(className: "nation-slot").ToList()
            .Where(slot => slot.ClassListContains("legal-target")).ToList();

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture(true);
            var north = fixture.View.Nations.Single(n => n.Id == "north");
            north.Active = false;
            north.StepsToWar = 3;
            var dwarves = fixture.View.Nations.Single(n => n.Id == "dwarves");
            dwarves.Active = false;
            dwarves.StepsToWar = 1;
            fixture.Refresh();
            fixture.View.Options.Single(o => o.Kind == CommandKind.AdvancePolitics).Targets.Remove("dwarves");
            return fixture;
        }

        // UI-178: a row is the name, the track and the arrow, and nothing else: no station is a framed button, every
        // row keeps a greyed arrow in one column and the panel has no empty column after it.
        private static void AssertTrackRows(PoliticalPanel panel, string label)
        {
            var arrows = panel.Drawer.Query<Button>(className: "nation-advance").ToList()
                .Where(button => button.resolvedStyle.display != DisplayStyle.None && button.parent.parent.resolvedStyle.display != DisplayStyle.None).ToList();
            Assert.IsNotEmpty(arrows, label);
            foreach (var arrow in arrows)
            {
                Assert.AreEqual(Visibility.Visible, arrow.resolvedStyle.visibility, arrow.name + " · " + label);
                Assert.IsFalse(arrow.enabledInHierarchy, "Without a die every arrow is greyed · " + arrow.name + " · " + label);
                Assert.That(arrow.worldBound.center.x, Is.EqualTo(arrows[0].worldBound.center.x).Within(1), "One arrow column · " + label);
                Inside(arrow.worldBound, panel.Drawer.worldBound);
            }
            Assert.Less(panel.Drawer.worldBound.xMax - arrows[0].worldBound.xMax, 24, "No empty column after the arrows · " + label);
            foreach (var slot in panel.Drawer.Query<Button>(className: "nation-slot").ToList())
            {
                Assert.IsFalse(slot.ClassListContains("action"), slot.name + " is a station, not a framed button");
                Assert.IsNull(slot.resolvedStyle.backgroundImage.texture, slot.name + " draws no button frame · " + label);
            }
        }

        private static Button Token(PoliticalPanel panel, string id) => panel.Drawer.Q<Button>("nation-status-" + id);

        private static void Move(VisualElement target)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }

        private static void Key(VisualElement target, KeyCode key)
        {
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = key }))
                target.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = key }))
                target.SendEvent(up);
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.06f);
        }

        private static void Hit(VisualElement element)
        {
            var hit = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(hit == element || element.Contains(hit),
                $"Occluded control: {element.name} by {hit?.name} ({string.Join(" ", hit?.GetClasses() ?? new string[0])}) under {hit?.parent?.name}");
        }

        private static void Inside(Rect inner, Rect outer)
        {
            Assert.GreaterOrEqual(inner.xMin, outer.xMin - 1);
            Assert.GreaterOrEqual(inner.yMin, outer.yMin - 1);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1);
        }
    }
}
