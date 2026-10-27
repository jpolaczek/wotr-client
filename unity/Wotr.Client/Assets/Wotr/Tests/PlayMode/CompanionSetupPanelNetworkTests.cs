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
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-269: the private companion versions and the starting Guide on the Fellowship panel (formerly the setup panel).
    public sealed partial class CompanionSetupPanelNetworkTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string Gimli = "base.character.gimli";
        private const string Strider = "base.character.strider";

        [UnityTest]
        public IEnumerator NormalSetupUsesOnlyServerChoicesAcrossLordsCouncilFateAndKings() => Wait(Variants());

        [UnityTest]
        public IEnumerator ComparisonAndGuideRemainReachableWithoutScrollAtFourLayouts() => Wait(Layouts());

        // UI-269: the private versions and the starting Guide are chosen on the Fellowship panel's portraits — the
        // two-faces badge compares a companion's two version cards, a crown makes the starting Guide, the ✓ sends.
        private static async Task Variants()
        {
            var configurations = new[]
            {
                new MatchConfiguration(), new MatchConfiguration { Lords = true },
                new MatchConfiguration { Lords = true, Council = true },
                new MatchConfiguration { Lords = true, Council = true, Kings = true },
                new MatchConfiguration { Lords = true, Council = true, Kings = true, Fate = true, NewCities = true }
            };
            foreach (var configuration in configurations)
            {
                using var room = await CreateNormalGame(configuration);
                var board = room.Fp.Board;
                if (!configuration.Lords)
                {
                    Assert.IsFalse(CompanionSetupPresentation.Setup(board.View));
                    continue;
                }
                await room.Settle();
                var option = board.View.Options.Single(o => o.Kind == CommandKind.ChooseCompanionVersions);
                Assert.AreSame(board.Fellowship.Root, board.Tray.parent, "The versions are chosen on the Fellowship panel.");
                Assert.IsTrue(board.Fellowship.IsOpen);
                Assert.AreEqual(configuration.Council ? 7 : 1, option.CompanionVersions.Count);
                // UI-370: Gandalf is the starting Guide by default, so Confirm is ready at once, beside Finish phase.
                var ready = board.Fellowship.Root.Q<Button>("confirm-decision");
                Assert.IsTrue(ready.enabledInHierarchy, "The default setup can be confirmed at once.");
                Assert.AreSame(board.Fellowship.ActionRow, ready.parent, "Confirm stands in the row of Finish phase.");
                Assert.IsTrue(board.Root.Q<Button>("starting-guide-" + Grey).ClassListContains("selected"), "Gandalf wears the crown.");
                LayoutTests.Save(room.Fp.Viewport, "ui370-council-setup-" + (configuration.Council ? "council" : "lords") + ".png");
                var revision = board.View.Revision;
                var decision = board.View.Decision.Id;
                var camera = board.Map.Camera.Pan;
                AssertPrivate(room.Sh.Board);
                foreach (var group in option.CompanionVersions)
                {
                    Assert.AreEqual(DisplayStyle.Flex, Portrait(board, group.FigureId).Q("versions-badge").resolvedStyle.display);
                    await Press(room, board, "fellowship-portrait-" + group.FigureId);
                    Assert.IsTrue(Portrait(board, group.FigureId).ClassListContains("selected"));
                    Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.BaseId));
                    Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.AlternateId));
                }
                foreach (var candidate in option.Targets)
                    Assert.AreEqual(DisplayStyle.Flex, board.Root.Q<Button>("starting-guide-" + candidate).resolvedStyle.display);
                Assert.IsTrue(board.Fellowship.Root.Q<Button>("confirm-decision").enabledInHierarchy);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(camera, board.Map.Camera.Pan);
                await Press(room, board, "fellowship-portrait-" + Grey);
                await Press(room, board, "version-" + option.CompanionVersions.Single(g => g.FigureId == Grey).BaseId);
                Assert.IsTrue(board.Fellowship.Root.Q<Button>("confirm-decision").enabledInHierarchy);
                if (configuration.Council)
                {
                    await Press(room, board, "fellowship-portrait-" + Gimli);
                    var group = option.CompanionVersions.Single(g => g.FigureId == Gimli);
                    if (configuration.Fate)
                        Assert.AreEqual("fate.character.gimli-dwarven-lord", group.AlternateId);
                    await Press(room, board, "version-" + group.AlternateId);
                    StringAssert.Contains("Alternate", Portrait(board, Gimli).tooltip, "The portrait shows the chosen version.");
                }
                await Press(room, board, "starting-guide-" + Strider);
                await room.Until(() => board.Fellowship.Root.Q<Button>("confirm-decision").enabledInHierarchy);
                Assert.IsTrue(board.Root.Q<Button>("starting-guide-" + Strider).ClassListContains("selected"));
                await CheckKeyboardControls(room, board, revision);
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                Assert.AreEqual(decision, board.View.Decision.Id);
                Assert.IsTrue(board.Fellowship.IsOpen, "The panel holds its decision across a reconnect.");
                Assert.IsTrue(board.Root.Q<Button>("starting-guide-" + Strider).ClassListContains("selected"), "The draft survives.");
                var confirm = board.Fellowship.Root.Q<Button>("confirm-decision");
                Assert.AreEqual("confirm", IconButton.Icon(confirm));
                var confirmPoint = confirm.worldBound.center;
                PointerGestureEvents.ClickAt(confirm);
                RepeatClick(board.Root, confirmPoint);
                await room.At(revision + 1);
                Assert.AreEqual(configuration.Council, CompanionSetupPresentation.Homelands(board.View));
                Assert.IsNull(board.Tray.Q<VisualElement>("companion-setup-choices"));
                Assert.AreNotEqual(decision, board.View.Decision.Id);
                Assert.AreEqual(configuration.Council ? DecisionKind.CouncilHomelands : DecisionKind.FellowshipPhase,
                    board.View.Decision.Kind);
                Assert.AreEqual(Strider, room.Sh.Board.View.Fellowship.Guide);
                Assert.IsTrue(board.Root.Query(className: "versions-badge").ToList().All(b => b.style.display.value == DisplayStyle.None));
                AssertPrivate(room.Sh.Board);
                Assert.AreEqual(revision + 1, (await room.Fp.Api.Get<PlayerView>("api/matches/" + board.View.MatchId, room.Token)).Revision);
            }
        }

        private static Button Portrait(BoardScreen board, string figure) => board.Fellowship.Root.Q<Button>("fellowship-portrait-" + figure);

        private static void RepeatClick(VisualElement root, Vector2 point)
        {
            var target = root.panel.Pick(point);
            Assert.IsNotNull(target, "The second click must target the current layout at the same screen position.");
            using (var down = PointerDownEvent.GetPooled(new Event
                { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 2 }))
                target.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event
                { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 2 }))
                target.SendEvent(up);
        }

        private static async Task CheckKeyboardControls(MatchTestRoom room, BoardScreen board, long revision)
        {
            var ability = board.Tray.Q<Label>(className: "setup-version-ability");
            Assert.IsNotNull(ability);
            ability.Focus();
            Key(ability, KeyCode.Return);
            await room.Settle();
            Assert.AreEqual(revision, board.View.Revision, "Reading a focused ability cannot submit the whole setup.");
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.IsTrue(board.Fellowship.Root.Q<Button>("confirm-decision").enabledInHierarchy);
        }

        private static void Key(VisualElement target, KeyCode key)
        {
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = key }))
                target.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = key }))
                target.SendEvent(up);
        }

        private static async Task Layouts()
        {
            using var room = await CreateNormalGame(new MatchConfiguration { Lords = true, Council = true, Kings = true, Fate = true });
            var client = room.Fp;
            var option = client.Board.View.Options.Single();
            var before = JsonConvert.SerializeObject(client.Board.View);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    await room.Settle();
                    foreach (var group in option.CompanionVersions)
                    {
                        await Press(room, client.Board, "fellowship-portrait-" + group.FigureId);
                        AssertPanel(client.Board);
                        foreach (var ability in client.Board.Tray.Query<Label>(className: "setup-version-ability").ToList())
                        {
                            Assert.IsTrue(ability.focusable);
                            Assert.Greater(ability.tooltip.Length, ability.text.Length);
                        }
                    }
                    await Press(room, client.Board, "fellowship-portrait-" + Gimli);
                    LayoutTests.Save(target, $"ui68-versions-{width}-{scale:0.00}.png");
                    AssertPrivate(room.Sh.Board);
                    Assert.AreEqual(before, JsonConvert.SerializeObject(client.Board.View), "Comparison cannot mutate any projected state.");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Interface.Preferences.SetScale(1f);
        }

        private static void AssertPanel(BoardScreen board)
        {
            var panel = board.Fellowship.Root;
            var viewport = board.Map.parent.worldBound;
            Assert.GreaterOrEqual(panel.worldBound.xMin, viewport.xMin);
            Assert.GreaterOrEqual(panel.worldBound.yMin, viewport.yMin);
            Assert.LessOrEqual(panel.worldBound.xMax, viewport.xMax + 1);
            Assert.LessOrEqual(panel.worldBound.yMax, viewport.yMax + 1);
            ExpansionMatrixLayoutTests.ResourcesFit(board);
            LayoutTests.AssertNoDecisionScroll(board);
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
                    var card = label.GetFirstAncestorOfType<Button>();
                    if (card != null)
                    {
                        Assert.GreaterOrEqual(label.worldBound.yMin, card.worldBound.yMin - 1, label.text);
                        Assert.LessOrEqual(label.worldBound.yMax, card.worldBound.yMax + 1, label.text);
                    }
                }
                if (element is Button button && button.enabledInHierarchy)
                {
                    var picked = button.panel.Pick(button.worldBound.center);
                    Assert.IsTrue(picked == button || button.Contains(picked), "Covered: " + button.name);
                }
            }
        }

        // The Shadow's board shows none of the private setup: no badges, crowns or cards, only that the Free Peoples
        // prepare the Fellowship.
        private static void AssertPrivate(BoardScreen board)
        {
            Assert.IsEmpty(board.View.OwnCompanionVersions);
            Assert.IsFalse(CompanionSetupPresentation.Setup(board.View));
            Assert.AreNotSame(board.Fellowship.Root, board.Tray.parent);
            Assert.IsTrue(board.Root.Query(className: "versions-badge").ToList().All(b => b.style.display.value == DisplayStyle.None));
            Assert.IsTrue(board.Root.Query<Button>(className: "guide-crown").ToList().All(c => c.style.display.value == DisplayStyle.None));
            if (board.View.Phase == GamePhase.Setup && board.View.DecisionOwner == Side.FreePeoples)
                Assert.AreEqual("FREE PEOPLES PREPARE THE FELLOWSHIP", board.Console.Status.Headline.text);
        }

        private static async Task Press(MatchTestRoom room, BoardScreen board, string name)
        {
            var button = board.Root.Q<Button>(name);
            try
            {
                PointerGestureEvents.ClickAt(button);
            }
            catch
            {
                var client = board == room.Fp.Board ? room.Fp : room.Sh;
                LayoutTests.Save(client.Panel.targetTexture, "ui68-blocked-" + name + ".png");
                TestContext.WriteLine("Expected " + name + " at " + button?.worldBound);
                for (var element = button?.panel.Pick(button.worldBound.center); element != null; element = element.parent)
                    TestContext.WriteLine(element.GetType().Name + "#" + element.name + " " + element.worldBound +
                        " classes=" + string.Join(",", element.GetClasses()) + " text=" + (element as Label)?.text);
                throw;
            }
            await room.Settle();
        }
    }
}
