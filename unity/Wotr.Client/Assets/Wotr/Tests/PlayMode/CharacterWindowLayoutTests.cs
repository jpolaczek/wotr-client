using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-436 (owner: "the window opens in a different place for each ruler; always show it beside the rulers' tab, in the
    // same place"): every character's window opened from the roster opens at one place beside where the Characters panel
    // unrolls, level with the Characters tab, in four layouts; its place is a picture whose click shows the region on the
    // map as the old Locate button did. UI-439: each ability line shows its ability's own icon; UI-440: each ability is its
    // bare icon, described in its formatted tooltip. Inspection sends nothing. On ability-chips the kings' own window keeps
    // its base form and place (it hosts their decisions).
    public sealed class CharacterWindowLayoutTests
    {
        private const int Rows = 3;
        private static RenderTexture current;

        [UnityTest]
        public IEnumerator EveryCharactersWindowOpensAtOnePlaceBesideTheCharactersTab() => Wait(Characters("ui66-full"));

        private static async Task Characters(string scenario)
        {
            using var room = await Connect(scenario, ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    await InLayout(room, room.Fp, width, scale, label => CheckRows(room, room.Fp.Board, label));
            await CheckPicture(room, board);
            await InLayout(room, room.Sh, 1366, 1.25f, label => CheckRows(room, room.Sh.Board, label));
            Assert.AreEqual(revision, board.View.Revision, "Opening the windows and their pictures sends no command.");
        }

        private static async Task InLayout(MatchTestRoom room, MountedMatchClient client, int width, float scale, Func<string, Task> check)
        {
            var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            texture.Create();
            try
            {
                client.Panel.targetTexture = texture;
                client.Panel.scale = scale;
                client.Board.Windows.Clear();
                await room.Settle();
                current = texture;
                await check($"{client.Board.View.You}-{width}-{scale:0.00}");
            }
            finally
            {
                await CloseAll(room, client.Board);
                client.Panel.targetTexture = client.Viewport;
                client.Panel.scale = 1;
                texture.Release();
                Object.Destroy(texture);
            }
        }

        // The first rows of the roster, each from its row: one place and one width for every window.
        private static async Task CheckRows(MatchTestRoom room, BoardScreen board, string label)
        {
            Rect? first = null;
            var shownIcons = false;
            foreach (var name in await RowNames(board))
            {
                await BoardNavigation.OpenCharacters(board);
                PointerGestureEvents.ClickAt(board.Characters.Root.Q<Button>(name));
                await Shown(room, board);
                var window = board.Characters.Inspector.Root;
                CheckPlace(board, window, label + " · " + name);
                if (first == null)
                    LayoutTests.Save(current, $"ui436-character-{label}.png");
                first ??= window.worldBound;
                Assert.AreEqual(first.Value.xMin, window.worldBound.xMin, 1, "Same place: " + label + " · " + name);
                Assert.AreEqual(first.Value.width, window.worldBound.width, 1, "One width for every character's window: " + label + " · " + name);
                if (!shownIcons)
                    shownIcons = await CheckAbilityIcons(room, board, window, label);
                await CloseAll(room, board);
            }
            Assert.IsNotNull(first, "The roster has rows: " + label);
            if (board.View.You == Side.FreePeoples)
                Assert.IsTrue(await CheckGandalf(room, board, label), "Gandalf's window shows the Grey's card: " + label);
        }

        // UI-440: Gandalf's window, opened from his row, where the Grey's card (the base's or the Lords') shows the longest
        // printed text.
        private static async Task<bool> CheckGandalf(MatchTestRoom room, BoardScreen board, string label)
        {
            var gandalf = board.View.OwnCharacterRoster.FirstOrDefault(row => PrintsLongest(row.CurrentDefinitionId) ||
                row.FutureVersions.Any(version => PrintsLongest(version.DefinitionId)));
            if (gandalf == null)
                return false;
            await BoardNavigation.OpenCharacters(board);
            PointerGestureEvents.ClickAt(board.Characters.Root.Q<Button>("character-roster-" + gandalf.FigureId));
            await Shown(room, board);
            var shown = await CheckLongestTooltip(room, board, label);
            await CloseAll(room, board);
            return shown;
        }

        private static async Task<IReadOnlyList<string>> RowNames(BoardScreen board)
        {
            await BoardNavigation.OpenCharacters(board);
            return board.Characters.Root.Query<Button>(className: "character-roster-row").ToList()
                .Where(row => row.resolvedStyle.display != DisplayStyle.None && row.worldBound.height > 0)
                .Select(row => row.name).Take(Rows).ToList();
        }

        // UI-439: every ability shows its ability's own icon (AbilityArt), on the window's card. UI-440 (owner): a bare icon
        // in one row, at least 28 points; a read-only one's click changes nothing and leaves no focus ring; keyboard focus
        // rings it and opens its formatted description at once, whole inside the board.
        private static async Task<bool> CheckAbilityIcons(MatchTestRoom room, BoardScreen board, VisualElement window, string label)
        {
            var definition = board.Characters.Inspector.DefinitionId;
            var chips = Icons(window);
            if (chips.Count == 0)
                return false;
            foreach (var chip in chips)
            {
                SkillIconChecks.Bare(chip, AbilityArt.For(definition, chip.userData as string), 28f);
                Assert.AreEqual(chips[0].worldBound.yMin, chip.worldBound.yMin, 0.5f, "One row: " + chip.name + " · " + label);
            }
            LayoutTests.Save(current, $"ui439-character-{label}.png");
            var passive = chips.FirstOrDefault(ReadOnlyControl.Is);
            Assert.IsNotNull(passive, "Without a chosen die some ability waits: " + label);
            var placed = window.worldBound;
            var revision = board.View.Revision;
            PointerGestureEvents.ClickAt(passive);
            await room.Settle();
            Assert.IsFalse(passive.ClassListContains(KeyboardFocusRing.ClassName), "A click leaves no focus ring: " + label);
            Assert.AreEqual(placed.position, window.worldBound.position, "The window keeps its content and place: " + label);
            Assert.AreEqual(placed.height, window.worldBound.height, 0.5f, label);
            Assert.AreEqual(revision, board.View.Revision, "A read-only icon's click sends nothing: " + label);
            passive.Blur();
            await room.Settle();
            passive.Focus();
            await room.Settle();
            Assert.IsTrue(passive.ClassListContains(KeyboardFocusRing.ClassName), "Focus from the keyboard rings the icon: " + label);
            CheckTooltip(board, passive, label);
            LayoutTests.Save(current, $"ui440-character-{label}.png");
            passive.Blur();
            await CheckLongestTooltip(room, board, label);
            return true;
        }

        // UI-440: the longest printed text (Gandalf the Grey's Emissary from the West, 381 characters) shows whole in its
        // formatted description, on his Grey version's tab, where the window offers it.
        private static async Task<bool> CheckLongestTooltip(MatchTestRoom room, BoardScreen board, string label)
        {
            var inspector = board.Characters.Inspector;
            if (!PrintsLongest(inspector.DefinitionId))
            {
                var grey = inspector.Root.Query<Button>(className: "character-version-tab").ToList()
                    .FirstOrDefault(tab => PrintsLongest(tab.userData as string) && tab.resolvedStyle.display != DisplayStyle.None);
                if (grey == null || grey.worldBound.width <= 0)
                    return false;
                PointerGestureEvents.ClickAt(grey);
                await room.Settle();
            }
            var icon = inspector.Root.Q<Button>("character-ability-" + Longest);
            Assert.IsNotNull(icon, label);
            Assert.AreEqual(381, CharacterAbilityLibrary.Find(inspector.DefinitionId, Longest).Text.Length, inspector.DefinitionId);
            icon.Focus();
            await room.Settle();
            CheckTooltip(board, icon, label);
            LayoutTests.Save(current, $"ui440-longest-tooltip-{label}.png");
            icon.Blur();
            return true;
        }

        private const string Longest = "may-transform-to-white";

        private static bool PrintsLongest(string definition) =>
            CharacterAbilityLibrary.For(definition ?? "").Any(ability => ability.Id == Longest);

        // The formatted description shows whole, inside the board, its words the icon's own.
        private static void CheckTooltip(BoardScreen board, Button icon, string label)
        {
            var tooltip = board.Root.panel.visualTree.Q<Label>("runtime-tooltip");
            Assert.AreEqual(DisplayStyle.Flex, tooltip.resolvedStyle.display, label);
            Assert.IsTrue(tooltip.enableRichText, "The skill's description is formatted: " + label);
            Assert.AreEqual(icon.tooltip, tooltip.text, label);
            var root = tooltip.parent.worldBound;
            Assert.GreaterOrEqual(tooltip.worldBound.yMin, root.yMin - 1, label);
            Assert.LessOrEqual(tooltip.worldBound.yMax, root.yMax + 1, label);
            Assert.LessOrEqual(tooltip.worldBound.width, 361f, label);
        }

        private static List<Button> Icons(VisualElement window) =>
            window.Query<Button>(className: CharacterAbilityChips.ChipClass).ToList()
                .Where(chip => chip.resolvedStyle.display == DisplayStyle.Flex).ToList();

        // UI-436: the window stands just right of where the Characters panel unrolls, level with the Characters tab (or
        // raised only to stay whole above the bottom), inside the board, never over the panel.
        private static void CheckPlace(BoardScreen board, VisualElement window, string label)
        {
            var bounds = window.worldBound;
            var slot = board.Root.Q("characters-panel-slot").worldBound;
            var tab = board.Characters.Tab.worldBound;
            var stage = window.parent.worldBound;
            Assert.That(bounds.xMin, Is.InRange(slot.xMax, slot.xMax + 10), "Beside the Characters panel's place: " + label);
            var level = Mathf.Abs(bounds.yMin - tab.yMin) <= 1;
            var raised = bounds.yMin < tab.yMin && stage.yMax - bounds.yMax <= CharacterWindowPlace.BottomMargin + 2;
            Assert.IsTrue(level || raised, "Level with the Characters tab: " + bounds + " / " + tab + " / " + stage + " · " + label);
            Assert.GreaterOrEqual(bounds.yMin, stage.yMin - 1, label);
            Assert.LessOrEqual(bounds.yMax, stage.yMax + 1, label);
            Assert.LessOrEqual(bounds.xMax, stage.xMax + 1, label);
            if (board.Characters.Root.resolvedStyle.display != DisplayStyle.None)
            {
                Assert.LessOrEqual(board.Characters.Root.worldBound.xMax, slot.xMax + 1, "The slot is the panel's place: " + label);
                Assert.IsFalse(board.Characters.Root.worldBound.Overlaps(bounds), "The panel stays readable: " + label);
            }
            Assert.IsFalse(bounds.Overlaps(tab), label);
            Assert.IsEmpty(window.Query<ScrollView>().ToList(), label);
            var place = window.Q<Button>(className: PlacePicture.ClassName);
            Assert.IsNotNull(place, "The place is a picture: " + label);
            Assert.That(place.worldBound.width, Is.GreaterThan(90f), label);
            var picked = place.panel.Pick(place.worldBound.center);
            Assert.IsTrue(picked == place || place.Contains(picked), "The picture takes its click: " + label);
            Assert.IsNotNull(place.Q("place-picture-image").resolvedStyle.backgroundImage.texture, "The place has its picture: " + label);
        }

        // The picture's click shows the region on the map, as the old Locate button did.
        private static async Task CheckPicture(MatchTestRoom room, BoardScreen board)
        {
            var onBoard = board.View.OwnCharacterRoster.FirstOrDefault(row => row.Lifecycle == CharacterRosterLifecycle.OnBoard &&
                board.Presentation.Regions.ContainsKey(row.RegionId));
            Assert.IsNotNull(onBoard, "A character of the Free Peoples stands on the map.");
            board.Map.Focus("grey-havens");
            await BoardNavigation.OpenCharacters(board);
            PointerGestureEvents.ClickAt(board.Characters.Root.Q<Button>("character-roster-" + onBoard.FigureId));
            await Shown(room, board);
            var place = board.Characters.Inspector.Root.Q<Button>(className: PlacePicture.ClassName);
            Assert.IsTrue(place.enabledInHierarchy, onBoard.FigureId);
            StringAssert.Contains(board.Presentation.Regions[onBoard.RegionId].Name, place.tooltip);
            PointerGestureEvents.ClickAt(place);
            await room.Settle();
            Assert.AreEqual(onBoard.RegionId, board.Map.Selected, "The picture locates " + onBoard.FigureId);
            await CloseAll(room, board);
        }

        private static async Task Shown(MatchTestRoom room, BoardScreen board)
        {
            var window = board.Characters.Inspector.Root;
            await room.Until(() => window.resolvedStyle.display == DisplayStyle.Flex && window.layout.height > 0);
            await Task.Delay(250, room.Token);
        }

        // The window and the panel fade out; the next step waits until they are gone.
        private static async Task CloseAll(MatchTestRoom room, BoardScreen board)
        {
            board.Characters.Close();
            await room.Until(() => board.Characters.Root.resolvedStyle.display == DisplayStyle.None &&
                board.Characters.Inspector.Root.resolvedStyle.display == DisplayStyle.None);
        }
    }
}
