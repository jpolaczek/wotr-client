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
    public sealed partial class CompanionSetupPanelNetworkTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string Gimli = "base.character.gimli";
        private const string Strider = "base.character.strider";

        [UnityTest]
        public IEnumerator NormalSetupUsesOnlyServerChoicesAcrossLordsCouncilFateAndKings() => Wait(Variants());

        [UnityTest]
        public IEnumerator ComparisonAndGuideRemainReachableWithoutScrollAtFourLayouts() => Wait(Layouts());

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
                    Assert.IsFalse(board.CompanionSetup.Active);
                    Assert.IsFalse(board.CompanionSetup.IsOpen);
                    continue;
                }
                var option = board.View.Options.Single(o => o.Kind == CommandKind.ChooseCompanionVersions);
                Assert.AreSame(board.CompanionSetup.DecisionHost, board.Tray.parent);
                Assert.IsTrue(board.CompanionSetup.IsOpen);
                Assert.AreEqual(configuration.Council ? 7 : 1, option.CompanionVersions.Count);
                Assert.IsFalse(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
                var revision = board.View.Revision;
                var decision = board.View.Decision.Id;
                var camera = board.Map.Camera.Pan;
                AssertPrivate(room.Sh.Board);
                foreach (var group in option.CompanionVersions)
                {
                    await Press(room, board, "setup-portrait-" + group.FigureId);
                    Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.BaseId));
                    Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.AlternateId));
                }
                Assert.IsFalse(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(camera, board.Map.Camera.Pan);
                await Press(room, board, "setup-portrait-" + Grey);
                await Press(room, board, "version-" + option.CompanionVersions.Single(g => g.FigureId == Grey).BaseId);
                Assert.IsFalse(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
                if (configuration.Council)
                {
                    await Press(room, board, "setup-portrait-" + Gimli);
                    var group = option.CompanionVersions.Single(g => g.FigureId == Gimli);
                    if (configuration.Fate)
                        Assert.AreEqual("fate.character.gimli-dwarven-lord", group.AlternateId);
                    await Press(room, board, "version-" + group.AlternateId);
                }
                await Press(room, board, "starting-guide-" + Strider);
                await room.Until(() => board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
                await CheckKeyboardControls(room, board, revision);
                await Press(room, board, "close-companion-setup");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                Assert.IsFalse(board.CompanionSetup.IsOpen, "Same decision does not reopen a closed private panel.");
                Assert.AreEqual(decision, board.View.Decision.Id);
                board.Notice.Open();
                await room.Settle();
                Assert.IsTrue(board.CompanionSetup.IsOpen);
                Assert.IsTrue(board.Tray.Q<Button>("starting-guide-" + Strider).ClassListContains("selected"));
                var confirm = board.Tray.Q<Button>("confirm-decision");
                Assert.AreEqual("confirm", IconButton.Icon(confirm));
                var confirmPoint = confirm.worldBound.center;
                PointerGestureEvents.ClickAt(confirm);
                RepeatClick(board.Root, confirmPoint);
                await room.At(revision + 1);
                Assert.AreEqual(configuration.Council, board.CompanionSetup.Active);
                Assert.IsNull(board.Tray.Q<VisualElement>("companion-setup-choices"));
                Assert.AreNotEqual(decision, board.View.Decision.Id);
                Assert.AreEqual(configuration.Council ? DecisionKind.CouncilHomelands : DecisionKind.FellowshipPhase,
                    board.View.Decision.Kind);
                Assert.AreEqual(Strider, room.Sh.Board.View.Fellowship.Guide);
                AssertPrivate(room.Sh.Board);
                Assert.AreEqual(revision + 1, (await room.Fp.Api.Get<PlayerView>("api/matches/" + board.View.MatchId, room.Token)).Revision);
            }
        }

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
            var selected = board.CompanionSetup.Root.Query<Button>(className: "selected")
                .ToList().Select(button => button.name).OrderBy(name => name).ToArray();
            var ability = board.CompanionSetup.Root.Q<Label>(className: "setup-version-ability");
            Assert.IsNotNull(ability);
            ability.Focus();
            Key(ability, KeyCode.Return);
            await room.Settle();
            Assert.AreEqual(revision, board.View.Revision, "Reading a focused ability cannot submit the whole setup.");
            Assert.IsTrue(board.CompanionSetup.IsOpen);
            Assert.IsTrue(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
            Key(ability, KeyCode.Escape);
            await room.Settle();
            Assert.IsFalse(board.CompanionSetup.IsOpen);
            Assert.AreSame(board.CompanionSetup.Tab, board.Root.panel.focusController.focusedElement);
            Assert.AreEqual(revision, board.View.Revision);
            await Press(room, board, "companion-setup-tab");
            CollectionAssert.AreEqual(selected, board.CompanionSetup.Root.Query<Button>(className: "selected")
                .ToList().Select(button => button.name).OrderBy(name => name).ToArray());
            Assert.IsTrue(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy,
                "Esc closes only the panel and retains every explicit setup choice.");
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
                        await Press(room, client.Board, "setup-portrait-" + group.FigureId);
                        AssertPanel(client.Board);
                        foreach (var ability in client.Board.CompanionSetup.Root.Query<Label>(className: "setup-version-ability").ToList())
                        {
                            Assert.IsTrue(ability.focusable);
                            Assert.Greater(ability.tooltip.Length, ability.text.Length);
                        }
                    }
                    await Press(room, client.Board, "setup-portrait-" + Gimli);
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
            var panel = board.CompanionSetup.Root;
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

        private static void AssertPrivate(BoardScreen board)
        {
            Assert.IsFalse(board.CompanionSetup.Active);
            Assert.IsFalse(board.CompanionSetup.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.CompanionSetup.Tab.style.display.value);
            Assert.IsEmpty(board.View.OwnCompanionVersions);
            Assert.IsEmpty(board.CompanionSetup.DecisionHost.Children());
            Assert.IsNull(board.Root.Q<Button>("setup-portrait-" + Grey));
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
