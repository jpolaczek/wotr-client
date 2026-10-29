using System;
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
    public sealed class CharacterRosterPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator WhiteReturnsThroughOneStableInspectedFigureAndReconnect() => Wait(Transformation());

        [UnityTest]
        public IEnumerator ClickAndKeyboardInspectionPreserveTheDieWhileHoverAndPanStaySilent() => Wait(Inspection());

        [UnityTest]
        public IEnumerator AwakenedPortraitAndOwnRosterShareOnePublicInspector() => Wait(Sovereigns());

        private static async Task Transformation()
        {
            using var room = await Connect("ui66-entry", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var before = board.View;
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await room.Settle();
            const string id = "base.character.gandalf-grey";
            var row = board.Characters.Root.Q<Button>("character-roster-" + id);
            StringAssert.Contains("White", row.Q<Label>("character-roster-title").text);
            // UI-187: the row's tooltip ends with the short abilities of the card it shows.
            StringAssert.EndsWith("\n" + string.Join("\n", CharacterAbilitySummaries.Lines("base.character.gandalf-white")), CharacterRowTooltip.Plain(row.tooltip));
            // UI-408 (owner): the base roster stands two a line, its ability icons larger, beside the text and inside the row.
            Assert.IsTrue(board.Characters.Wide, "Two characters a line fit this roster.");
            var icon = row.Query<Button>(className: CharacterAbilityStrip.IconClass).Where(b => b.resolvedStyle.display == DisplayStyle.Flex).First();
            Assert.IsNotNull(icon, "Gandalf's row shows an ability icon.");
            Assert.That(icon.worldBound.width, Is.GreaterThanOrEqualTo(17f), "The icons are larger two a line.");
            Assert.That(icon.worldBound.yMax, Is.LessThanOrEqualTo(row.worldBound.yMax + 0.5f), "The icons fit inside the row.");
            Assert.That(icon.worldBound.xMax, Is.LessThanOrEqualTo(row.worldBound.xMax + 0.5f), "The icons fit inside the row.");
            PointerGestureEvents.ClickAt(row);
            await room.Settle();
            Assert.IsTrue(board.Characters.InspectorPinned);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.Q<Button>("locate-character").resolvedStyle.display);
            // UI-387 (owner): the versions are tabs right under the title, low and named by the version.
            var white = board.Characters.Inspector.Root.Q<Button>("character-version-base.character.gandalf-white");
            var tabs = white.parent;
            Assert.IsTrue(tabs.ClassListContains("character-version-tabs"));
            Assert.AreEqual(1, board.Characters.Inspector.Root.IndexOf(tabs), "The tabs follow the title.");
            Assert.LessOrEqual(white.resolvedStyle.height, 30.5f, "A tab, not a large button.");
            Assert.AreEqual("White", white.text);
            LayoutTests.Save(room.Fp.Viewport, "ui387-version-tabs.png");
            white.Focus();
            var choice = before.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                .Single(c => c.CharacterId == "base.character.gandalf-white");
            var command = T22NetworkActions.Command(before, CommandKind.EnterCharacter,
                new CommandPayload { CharacterId = choice.CharacterId, DieId = choice.Dice[0], RegionId = "fangorn" });
            var receipt = await room.Fp.Api.Post<CommandReceipt>($"api/matches/{before.MatchId}/commands", command, room.Token);
            Assert.IsTrue(receipt.Accepted);
            await room.At(before.Revision + 1);
            Assert.AreSame(row, board.Characters.Root.Q<Button>("character-roster-" + id));
            Assert.AreEqual("characters-section-OnBoard", row.parent.parent.name);
            Assert.AreEqual(id, board.Characters.Inspector.FigureId);
            Assert.IsTrue(board.Characters.InspectorPinned);
            Assert.AreSame(board.Characters.Inspector.Root.Q<Button>("locate-character"),
                board.Root.panel.focusController.focusedElement, "A disappearing version control keeps focus in the inspector.");
            StringAssert.Contains("Fangorn", board.Characters.Inspector.Root.Q<Label>("character-state").text);
            Assert.AreEqual(1, room.Sh.Board.View.Characters.Count(c => c.Id == id));
            Assert.IsFalse(room.Sh.Board.View.OwnCharacterRoster.Any(c => c.FigureId == id));
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.AreSame(row, board.Characters.Root.Q<Button>("character-roster-" + id));
            Assert.IsTrue(board.Characters.InspectorPinned);
            var pan = board.Map.Camera.Pan;
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Characters.Inspector.Root.Q<Button>("locate-character"));
            await room.Settle();
            Assert.AreNotEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(before.Revision + 1, board.View.Revision);
            await InspectRemovedMinion(room);
        }

        private static async Task InspectRemovedMinion(MatchTestRoom room)
        {
            var board = room.Sh.Board;
            const string id = "base.character.saruman";
            var revision = board.View.Revision;
            Assert.AreEqual(CharacterRosterLifecycle.Eliminated,
                board.View.OwnCharacterRoster.Single(c => c.FigureId == id).Lifecycle);
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await room.Settle();
            var removed = board.Characters.Root.Q<Foldout>("characters-removed");
            Assert.IsFalse(removed.value);
            PointerGestureEvents.ClickAt(removed.Q<Toggle>());
            await room.Settle();
            Assert.IsTrue(removed.value);
            var row = removed.Q<Button>("character-roster-" + id);
            PointerGestureEvents.ClickAt(row);
            await room.Settle();
            Assert.AreEqual(id, board.Characters.Inspector.FigureId);
            StringAssert.Contains("Removed", board.Characters.Inspector.Root.Q<Label>("character-state").text);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.Q<Button>("locate-character").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.Q<Label>("character-entry").resolvedStyle.display);
            Assert.AreEqual(revision, board.View.Revision);
        }

        private static async Task Inspection()
        {
            using var room = await Connect("ui66-hover", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            var originalView = JsonConvert.SerializeObject(board.View);
            var die = board.View.Options.Single(o => o.Kind == CommandKind.MoveCompanions).EligibleDice.First();
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            await ComponentGestures.Hover(board.Characters.Tab);
            Assert.IsTrue(board.Characters.Root.ClassListContains(LeftRailPreview.PreviewClass), "UI-92: hover previews Characters read-only.");
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            var row = board.Characters.Root.Q<Button>("character-roster-base.character.gimli");
            await ComponentGestures.Hover(row);
            Assert.IsFalse(row.ClassListContains("selected"));
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display);
            PointerGestureEvents.ClickAt(row);
            await room.Settle();
            Assert.AreEqual("base.character.gimli", board.Characters.Inspector.FigureId);
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Inspector.Root.resolvedStyle.display);
            await ComponentGestures.Hover(board.Characters.Inspector.Root.Q("character-statistics"));
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            var ability = board.Characters.Inspector.Root.Q<Button>("character-ability-combat-strength-plus-one");
            await ComponentGestures.Hover(ability);
            var tooltip = room.Fp.Interface.Content.parent.Q<Label>("runtime-tooltip");
            Assert.AreEqual(ability.tooltip, tooltip.text);
            Assert.AreEqual(DisplayStyle.Flex, tooltip.resolvedStyle.display);
            StringAssert.Contains("5 combat dice", tooltip.text.ToLowerInvariant());
            // UI-139: a click unrolls the ability's full text under the chips; UI-369: a second click keeps it unrolled.
            var text = board.Characters.Inspector.Root.Q<Label>("character-ability-text");
            Assert.AreEqual(DisplayStyle.None, text.resolvedStyle.display);
            PointerGestureEvents.ClickAt(ability);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, text.resolvedStyle.display);
            StringAssert.Contains("5 combat dice", text.text.ToLowerInvariant());
            Assert.IsTrue(ability.ClassListContains("selected"));
            Assert.Greater(text.resolvedStyle.height, 20, "The unrolled label measures its text.");
            PointerGestureEvents.ClickAt(ability);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, text.resolvedStyle.display, "A second click on the selected chip changes nothing.");
            // UI-371: another ability's text changes the window's content, not its place.
            var placed = board.Characters.Inspector.Root.worldBound.position;
            PointerGestureEvents.ClickAt(board.Characters.Inspector.Root.Q<Button>("character-ability-politics-dwarves-at-erebor"));
            await room.Settle();
            Assert.That(Vector2.Distance(placed, board.Characters.Inspector.Root.worldBound.position), Is.LessThan(1f),
                "The window stays where it stood.");
            // UI-372: no choice highlight is the old pale blue.
            var mint = new Color(144 / 255f, 203 / 255f, 172 / 255f);
            foreach (var target in board.Root.Query(className: "legal-target").ToList())
                Assert.Greater(Vector4.Distance(target.resolvedStyle.borderTopColor, mint), 0.05f, target.name + " is still pale blue.");
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            await ComponentGestures.Hover(board.Phase);
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Inspector.Root.resolvedStyle.display);
            board.Characters.Close();
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab }))
                board.Root.SendEvent(key);
            board.Characters.Tab.Focus();
            await room.Settle();
            Assert.IsTrue(board.Characters.Root.ClassListContains(LeftRailPreview.PreviewClass),
                "UI-92: keyboard focus previews the panel read-only, like hover.");
            using (var submit = NavigationSubmitEvent.GetPooled())
                board.Characters.Tab.SendEvent(submit);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await ComponentGestures.Hover(board.Phase);
            Assert.IsTrue(board.Characters.Pinned);
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            await ComponentGestures.Hover(row);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display);
            row.Focus();
            await room.Settle();
            Assert.AreSame(row, board.Root.panel.focusController.focusedElement);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display,
                "Keyboard focus alone must leave the character inspector closed.");
            using (var submit = NavigationSubmitEvent.GetPooled())
                row.SendEvent(submit);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Inspector.Root.resolvedStyle.display,
                "Activating the focused portrait must inspect the selected figure.");
            Assert.AreEqual("base.character.gimli", board.Characters.Inspector.FigureId);
            PointerGestureEvents.ClickAt(board.Characters.Inspector.Root.Q<Button>("pin-character"));
            await room.Settle();
            Assert.IsTrue(board.Characters.InspectorPinned);
            Assert.IsFalse(row.ClassListContains("selected"), "Keyboard activation toggles the selected figure through the same action path.");
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            var versionControl = board.Characters.Inspector.Root.Q<Button>("pin-character");
            versionControl.Focus();
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }))
                versionControl.SendEvent(key);
            await room.Settle();
            board.Characters.Close();
            var origin = board.Map.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = origin }))
                board.Map.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = board.Characters.Tab.worldBound.center }))
                board.Root.SendEvent(move);
            await Task.Delay(300, room.Token);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Root.resolvedStyle.display);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 1, mousePosition = origin }))
                board.Map.SendEvent(up);
            var enemy = room.Sh.Board;
            enemy.Map.Focus("hollin");
            enemy.Hover.Open("hollin");
            await room.Settle();
            var portrait = enemy.Hover.Panel.Root.Q<Button>("hover-figure-base.character.gimli");
            PointerGestureEvents.ClickAt(portrait);
            await room.Settle();
            Assert.AreEqual("base.character.gimli", enemy.Characters.Inspector.FigureId);
            Assert.IsTrue(enemy.Characters.InspectorPinned);
            Assert.IsEmpty(enemy.Characters.Inspector.Actions.Children());
            Assert.IsFalse(enemy.Characters.Root.Query<Button>().ToList().Any(b => b.name == "character-roster-base.character.gimli"));
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(revision, enemy.View.Revision);
            var actual = await room.Fp.Api.Get<PlayerView>($"api/matches/{board.View.MatchId}", room.Token);
            Assert.AreEqual(originalView, JsonConvert.SerializeObject(actual), "Inspection and keyboard navigation must not mutate the server view.");
        }

        private static async Task Sovereigns()
        {
            using var room = await Connect("ui66-full", ScenarioKind.Interaction, openPanels: false);
            const string id = "kings.sovereign.dain";
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                var pan = board.Map.Camera.Pan;
                var pinnedId = board.View.You == Side.FreePeoples ? "base.character.gandalf-grey" : "base.character.saruman";
                board.Characters.Show(pinnedId, true);
                await room.Settle();
                await ComponentGestures.Hover(await BoardNavigation.Sovereign(board, id));
                Assert.AreEqual(pinnedId, board.Characters.Inspector.FigureId,
                    "A sovereign hover must preserve the explicitly pinned character.");
                Assert.IsTrue(board.Characters.InspectorPinned);
                board.Sovereigns.Show(id);
                await room.Settle();
                Assert.IsFalse(board.Sovereigns.IsOpen);
                Assert.AreEqual(id, board.Characters.Inspector.FigureId);
                Assert.AreEqual(DisplayStyle.Flex, board.Characters.Inspector.Root.resolvedStyle.display);
                Assert.AreEqual(1, board.Root.Query<VisualElement>("character-inspector").ToList().Count);
                if (board.View.You == Side.FreePeoples)
                    Assert.IsFalse(board.Characters.Inspector.Actions.Query<Button>().ToList().Any(button => button.enabledSelf),
                        "Own abilities cannot run before choosing their exact cost.");
                else
                    Assert.IsEmpty(board.Characters.Inspector.Actions.Children(), "The opponent cannot use the character's actions.");
                if (board.View.You == Side.FreePeoples)
                {
                    board.Characters.Close();
                    PointerGestureEvents.ClickAt(board.Characters.Tab);
                    await room.Settle();
                    var portrait = board.Characters.Root.Q<Button>("character-roster-" + id);
                    Assert.IsNotNull(portrait);
                    PointerGestureEvents.ClickAt(portrait);
                    await room.Settle();
                    Assert.AreEqual(id, board.Characters.Inspector.FigureId);
                }
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                Assert.AreEqual(revision, board.View.Revision);
            }
        }
    }
}
