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
    // map as the old Locate button did. UI-439: each ability line shows its ability's own icon. Inspection sends nothing.
    // On ability-chips the kings' own window keeps its base form and place (it hosts their decisions).
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
                    shownIcons = CheckAbilityIcons(board, window, label);
                await CloseAll(room, board);
            }
            Assert.IsNotNull(first, "The roster has rows: " + label);
        }

        private static async Task<IReadOnlyList<string>> RowNames(BoardScreen board)
        {
            await BoardNavigation.OpenCharacters(board);
            return board.Characters.Root.Query<Button>(className: "character-roster-row").ToList()
                .Where(row => row.resolvedStyle.display != DisplayStyle.None && row.worldBound.height > 0)
                .Select(row => row.name).Take(Rows).ToList();
        }

        // UI-439: every ability line shows its ability's own icon (AbilityArt), on the window's card.
        private static bool CheckAbilityIcons(BoardScreen board, VisualElement window, string label)
        {
            var definition = board.Characters.Inspector.DefinitionId;
            var chips = window.Query<Button>(className: CharacterAbilityChips.ChipClass).ToList()
                .Where(chip => chip.resolvedStyle.display == DisplayStyle.Flex).ToList();
            if (chips.Count == 0)
                return false;
            foreach (var chip in chips)
                Assert.AreEqual(Resources.Load<Texture2D>(AbilityArt.For(definition, chip.userData as string)),
                    chip.Q(className: CharacterAbilityChips.IconClass).style.backgroundImage.value.texture, chip.name + " · " + label);
            LayoutTests.Save(current, $"ui439-character-{label}.png");
            return true;
        }

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
