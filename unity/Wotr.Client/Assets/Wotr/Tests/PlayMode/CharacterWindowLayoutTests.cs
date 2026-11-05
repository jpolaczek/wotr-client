using System;
using System.Collections;
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
    // same place"): every ruler's window opens with one size at one place beside the Characters tab, whichever portrait or
    // object opened it, in four layouts — the Initial kings' sovereign windows (ui101-sovereigns) and the Awakened kings'
    // character windows (ui66-full) alike, since both are the ruler size at rest; a character's window opened from the
    // roster stands at the same place; the place's picture shows the region on the map as the old text button did.
    // Inspection sends nothing.
    public sealed class CharacterWindowLayoutTests
    {
        private static RenderTexture current;

        [UnityTest]
        public IEnumerator EveryInitialKingsWindowOpensAtOnePlaceBesideTheCharactersTab() => Wait(Kings("ui101-sovereigns", SovereignStatus.Initial));

        [UnityTest]
        public IEnumerator EveryAwakenedKingsWindowHasTheSamePlaceAndSize() => Wait(Kings("ui66-full", SovereignStatus.Awakened));

        private static async Task Kings(string scenario, SovereignStatus status)
        {
            using var room = await Connect(scenario, ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            Assert.AreEqual(5, board.View.Sovereigns.Count, "Every king of the Kings expansion.");
            Assert.IsTrue(board.View.Sovereigns.All(s => s.Status == status), scenario + ": every king is " + status);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    await InLayout(room, room.Fp, width, scale, label => CheckKings(room, board, label));
            await CheckPictures(room, board);
            await InLayout(room, room.Sh, 1366, 1.25f, label => CheckKings(room, room.Sh.Board, label));
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

        // Every king from his portrait; then one king with the panel rolled up, and a character from his roster row.
        private static async Task CheckKings(MatchTestRoom room, BoardScreen board, string label)
        {
            Rect? first = null;
            foreach (var king in board.View.Sovereigns)
            {
                PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, king.Id));
                await Shown(room, board);
                var window = Window(board);
                CheckPlace(board, window, label + " · " + king.Id);
                first ??= window.worldBound;
                Same(first.Value, window.worldBound, label + " · " + king.Id);
                if (king == board.View.Sovereigns[0])
                {
                    LayoutTests.Save(current, $"ui436-{Status(king)}-{label}.png");
                    CheckKingsIcons(board, window, king, label);
                }
            }
            await CloseAll(room, board);
            var other = board.View.Sovereigns[1];
            board.Sovereigns.Show(other.Id);
            await Shown(room, board);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Root.resolvedStyle.display, "The panel stays rolled up: " + label);
            Same(first.Value, Window(board).worldBound, label + " · rolled up");
            LayoutTests.Save(current, $"ui436-rolled-up-{Status(other)}-{label}.png");
            await CloseAll(room, board);
            await CheckCharacter(room, board, first.Value, label);
        }

        private static string Status(SovereignView king) => king.Status.ToString().ToLowerInvariant();

        private static async Task CheckCharacter(MatchTestRoom room, BoardScreen board, Rect ruler, string label)
        {
            await BoardNavigation.OpenCharacters(board);
            var row = board.Characters.Root.Query<Button>(className: "character-roster-row").ToList()
                .FirstOrDefault(b => !b.name.Contains("sovereign") && b.resolvedStyle.display != DisplayStyle.None);
            if (row == null)
                return;
            PointerGestureEvents.ClickAt(row);
            await Shown(room, board);
            var window = board.Characters.Inspector.Root;
            Assert.AreEqual(DisplayStyle.Flex, window.resolvedStyle.display, row.name);
            CheckPlace(board, window, label + " · " + row.name);
            Assert.AreEqual(ruler.xMin, window.worldBound.xMin, 1, "A character's window stands where the rulers' does: " + label);
            Assert.AreEqual(ruler.width, window.worldBound.width, 1, "One width for every character's window: " + label);
            LayoutTests.Save(current, $"ui436-character-{label}.png");
            await CheckAbilityIcons(room, board, window, label);
        }

        // UI-439: every ability shows its ability's own icon (AbilityArt), on the window's card. UI-440 (owner): a bare icon
        // in one row, at least 28 points; a read-only one's click changes nothing and leaves no focus ring; keyboard focus
        // rings it and opens its formatted description at once, whole inside the board.
        private static async Task CheckAbilityIcons(MatchTestRoom room, BoardScreen board, VisualElement window, string label)
        {
            var definition = board.Characters.Inspector.DefinitionId;
            var chips = Icons(window);
            Assert.IsNotEmpty(chips, label);
            foreach (var chip in chips)
                SkillIconChecks.Bare(chip, AbilityArt.For(definition, chip.userData as string), 28f);
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
        }

        // UI-440: the longest printed text (Gandalf the Grey's Emissary from the West, 381 characters) shows whole in its
        // formatted description, on his Grey version's tab, where the window offers it.
        private static async Task CheckLongestTooltip(MatchTestRoom room, BoardScreen board, string label)
        {
            var grey = board.Characters.Inspector.Root.Q<Button>("character-version-base.character.gandalf-grey");
            if (grey == null || grey.resolvedStyle.display == DisplayStyle.None)
                return;
            PointerGestureEvents.ClickAt(grey);
            await room.Settle();
            var icon = board.Characters.Inspector.Root.Q<Button>("character-ability-may-transform-to-white");
            Assert.IsNotNull(icon, label);
            Assert.AreEqual(381, CharacterAbilityLibrary.Find("base.character.gandalf-grey", "may-transform-to-white").Text.Length);
            icon.Focus();
            await room.Settle();
            CheckTooltip(board, icon, label);
            LayoutTests.Save(current, $"ui440-longest-tooltip-{label}.png");
            icon.Blur();
        }

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

        // UI-440: the king's window shows his abilities as the same bare icons, in one row, the window keeping its size.
        private static void CheckKingsIcons(BoardScreen board, VisualElement window, SovereignView king, string label)
        {
            var chips = Icons(window);
            var definition = king.Status == SovereignStatus.Initial ? king.DefinitionId : board.Characters.Inspector.DefinitionId;
            Assert.AreEqual(CharacterAbilityLibrary.For(definition).Count, chips.Count, label);
            foreach (var chip in chips)
            {
                SkillIconChecks.Bare(chip, AbilityArt.For(definition, chip.userData as string), 28f);
                Assert.AreEqual(chips[0].worldBound.yMin, chip.worldBound.yMin, 0.5f, "One row: " + label);
            }
            LayoutTests.Save(current, $"ui440-king-{Status(king)}-{label}.png");
        }

        private static System.Collections.Generic.List<Button> Icons(VisualElement window) =>
            window.Query<Button>(className: CharacterAbilityChips.ChipClass).ToList()
                .Where(chip => chip.resolvedStyle.display == DisplayStyle.Flex).ToList();

        // UI-436: the window stands just right of where the Characters panel unrolls, level with the Characters tab (or
        // raised only to stay whole above the bottom), inside the board, never over the panel; a ruler's window has the
        // ruler size at rest.
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
            // UI-469 (owner): a ruler's window is as tall as its content, with no empty strip at the bottom.
            if (window.ClassListContains(SovereignInspector.RulerWindowClass))
                Assert.AreEqual(ContentHeight(window), window.layout.height, 2, "No empty strip under the content: " + label);
            var place = window.Q<Button>(className: PlacePicture.ClassName);
            Assert.IsNotNull(place, "The place is a picture: " + label);
            Assert.That(place.worldBound.width, Is.GreaterThan(90f), label);
            var picked = place.panel.Pick(place.worldBound.center);
            Assert.IsTrue(picked == place || place.Contains(picked), "The picture takes its click: " + label);
            Assert.IsNotNull(place.Q("place-picture-image").resolvedStyle.backgroundImage.texture, "The place has its picture: " + label);
        }

        // The picture's click shows the region on the map, as the old text button did.
        private static async Task CheckPictures(MatchTestRoom room, BoardScreen board)
        {
            foreach (var king in board.View.Sovereigns.Take(2))
            {
                board.Map.Focus("grey-havens");
                PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, king.Id));
                await Shown(room, board);
                var place = Window(board).Q<Button>(className: PlacePicture.ClassName);
                Assert.IsTrue(place.enabledInHierarchy, king.Id);
                StringAssert.Contains(board.Presentation.Regions[king.RegionId].Name, place.tooltip);
                PointerGestureEvents.ClickAt(place);
                await room.Settle();
                Assert.AreEqual(king.RegionId, board.Map.Selected, "The picture locates " + king.Id);
                await CloseAll(room, board);
            }
        }

        private static VisualElement Window(BoardScreen board) =>
            board.Sovereigns.IsOpen ? board.Sovereigns.Inspector : board.Characters.Inspector.Root;

        private static async Task Shown(MatchTestRoom room, BoardScreen board)
        {
            await room.Until(() => Window(board).resolvedStyle.display == DisplayStyle.Flex && Window(board).layout.height > 0);
            await Task.Delay(250, room.Token);
        }

        // The windows and the panel fade out; the next step waits until they are gone.
        private static async Task CloseAll(MatchTestRoom room, BoardScreen board)
        {
            board.Sovereigns.Close();
            board.Characters.Close();
            await room.Until(() => board.Characters.Root.resolvedStyle.display == DisplayStyle.None &&
                board.Characters.Inspector.Root.resolvedStyle.display == DisplayStyle.None &&
                board.Sovereigns.Inspector.resolvedStyle.display == DisplayStyle.None);
        }

        private static void Same(Rect expected, Rect actual, string label)
        {
            // UI-469: the windows are as tall as their content, so a taller one may be raised to keep its bottom margin
            // (CheckPlace: level with the Characters tab or raised); the column and the width stay.
            Assert.AreEqual(expected.xMin, actual.xMin, 1, "Same place: " + label);
            Assert.AreEqual(expected.width, actual.width, 1, "Same size: " + label);
        }

        // UI-469: the shown rows with their margins, and the window's own padding.
        private static float ContentHeight(VisualElement window) =>
            window.Children().Where(child => child.resolvedStyle.display != DisplayStyle.None)
                .Sum(child => child.layout.height + child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom) +
            window.resolvedStyle.paddingTop + window.resolvedStyle.paddingBottom;
    }
}
