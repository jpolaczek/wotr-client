using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class CharacterRosterActionNetworkTests
    {
        [UnityTest] public IEnumerator RegionPanelGroupsKeepTheExactCostAndPaidReconnect() => Wait(Companions());
        [UnityTest] public IEnumerator FlightAndStationarySarumanKeepTheirDifferentRules() => Wait(Shadow());
        [UnityTest] public IEnumerator WhiteEntryRecoversTheLostReceiptWithoutPayingAgain() => Wait(Entry());
        [UnityTest] public IEnumerator BalrogTravelRequiresHisExplicitAbilityAndCannotTakeANazgul() => Wait(Balrog());
        [UnityTest] public IEnumerator RulerTokenRecruitmentUsesOneCharacterHostAndOneCommit() => Wait(Ruler());
        [UnityTest] public IEnumerator CouncilEntryAndTravelKeepTheChosenTokenAndCost() => Wait(Council());
        [UnityTest] public IEnumerator FateAlternateAbilityBelongsToItsStableFigure() => Wait(Fate());

        private static async Task Companions()
        {
            using var room = await Connect("ui67-travel-fp", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            FellowshipGestures.Die(room.Fp);
            var die = board.ActionDie.DieId;
            // UI-105: the Characters list only inspects; figures are picked in their region's panel on the map.
            await Inspect(room, room.Fp, "base.character.gimli");
            Assert.IsFalse(Picked(room.Fp, "base.character.gimli"), "Inspecting a companion selects nothing.");
            board.Characters.Close();
            await Pick(room, room.Fp, "base.character.gimli");
            await Pick(room, room.Fp, "base.character.legolas");
            Assert.IsFalse(Picked(room.Fp, "base.character.gimli"));
            Assert.IsTrue(Picked(room.Fp, "base.character.legolas"));
            await Pick(room, room.Fp, "base.character.gimli");
            var revision = board.View.Revision;
            FellowshipGestures.FinishRoute(room.Fp, "moria");
            await room.At(revision + 1);
            Assert.AreEqual(die, board.View.ActionSequence.DieId);
            Assert.AreEqual(1, board.View.ActionSequence.Remaining);
            Assert.AreEqual("moria", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.IsFalse(Row(board, "base.character.gimli").ClassListContains("legal-target"));
            await Pick(room, room.Fp, "base.character.legolas");
            FellowshipGestures.FinishRoute(room.Fp, "erebor");
            await room.At(revision + 2);
            Assert.IsNull(board.View.ActionSequence);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
            Assert.AreEqual("erebor", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.legolas").Location);
        }

        private static async Task Shadow()
        {
            using var room = await Connect("ui67-travel-sh", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            FellowshipGestures.Die(room.Sh);
            var die = board.ActionDie.DieId;
            var revision = board.View.Revision;
            await Pick(room, room.Sh, "base.character.saruman");
            Assert.IsFalse(Picked(room.Sh, "base.character.saruman"), "Saruman never leaves Orthanc.");
            Assert.AreEqual(revision, board.View.Revision);
            await Pick(room, room.Sh, "base.character.witch-king");
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("hollin"));
            FellowshipGestures.FinishRoute(room.Sh, "hollin");
            await room.At(revision + 1);
            Assert.AreEqual("hollin", room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.witch-king").Location);
            Assert.AreEqual("orthanc", room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.saruman").Location);
            Assert.AreEqual(die, board.View.ActionSequence.DieId);
            await ArmyGestures.Finish(room, room.Sh);
            T22NetworkActions.Spent(room, Side.Shadow, die);
        }

        private static async Task Entry()
        {
            using var boundary = new UI10HttpBoundary();
            using var room = await Connect("ui67-entry", ScenarioKind.Interaction, fpHandler: boundary, openPanels: false);
            var board = room.Fp.Board;
            boundary.MatchPath = "/api/matches/" + board.View.MatchId;
            var revision = board.View.Revision;
            var dice = board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
            var choice = board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                .Single(c => c.CharacterId == "base.character.gandalf-white");
            await ComponentGestures.Cost(room, room.Fp, choice.Dice[0]);
            // UI-108/UI-142: a click on Gandalf's row starts the transformation; no window and no button.
            await ComponentGestures.Enter(room, room.Fp, choice);
            AssertNoEntryWindow(board);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("fangorn"));
            board.Map.Select("gorgoroth");
            Assert.AreEqual(revision, board.View.Revision);
            board.Characters.Close();
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            AssertNoEntryWindow(board);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("fangorn"), "The chosen entry survives the reconnect.");
            boundary.ArmCommandLoss();
            board.Map.Select("fangorn");
            await room.Until(() => boundary.CommandHeld);
            await room.At(revision + 1);
            boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            Assert.Greater(boundary.ReceiptLookups, 0);
            Assert.AreEqual(1, boundary.Commands);
            await VerifyEntry(room, boundary.LastCommand, choice.Dice[0], dice, revision);
        }

        private static async Task VerifyEntry(MatchTestRoom room, GameCommand command, string die, int dice, long revision)
        {
            var board = room.Fp.Board;
            var duplicate = await room.Fp.Api.Post<CommandReceipt>($"api/matches/{board.View.MatchId}/commands", command, room.Token);
            Assert.IsTrue(duplicate.Accepted);
            Assert.AreEqual(revision + 1, duplicate.Revision);
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.AreEqual(revision + 1, board.View.Revision);
            Assert.AreEqual(dice - 1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available));
            var white = board.View.Characters.Single(c => c.DefinitionId == "base.character.gandalf-white");
            Assert.AreEqual("base.character.gandalf-grey", white.Id);
            Assert.AreEqual("fangorn", white.Location);
            Assert.AreEqual(1, room.Sh.Board.View.Characters.Count(c => c.Id == white.Id));
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Balrog()
        {
            using var room = await Connect("ui67-balrog", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            var cost = board.View.Options.Single(o => o.Kind == CommandKind.MoveShadowCharacters).MovementCosts.Single(c => c.AbilityId == "use-balrog");
            await ComponentGestures.Cost(room, room.Sh, cost.DieId);
            await Inspect(room, room.Sh, cost.RequiredCharacterId);
            Assert.IsFalse(Picked(room.Sh, cost.RequiredCharacterId), "The portrait must not choose Use Balrog automatically.");
            // UI-459 (owner, 2026-11-03): no icon in the window moves him: Balrog Activation keeps its one slot, dark while he
            // is active, and his figure on the map starts his move alone; the row stays one row in every layout.
            await Widest(room, room.Sh);
            var inspector = board.Characters.Inspector;
            Assert.IsNull(inspector.Root.Q<Button>("character-ability-balrog-activation-MoveShadowCharacters"));
            Assert.IsFalse(inspector.Root.Q<Button>("character-ability-balrog-activation").ClassListContains(ChoiceGlow.ClassName),
                "Nothing in the window moves the Balrog.");
            board.Characters.Close();
            await Pick(room, room.Sh, cost.RequiredCharacterId);
            var nazgul = board.View.Units.Single(u => u.Location == "moria" && u.Kind == UnitKind.Nazgul).Id;
            var revision = board.View.Revision;
            await KingsCardNetworkChecks.Reconnect(room, room.Sh);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("dimrill-dale"), "The Balrog's selection survives the reconnect.");
            FellowshipGestures.FinishRoute(room.Sh, "dimrill-dale");
            await room.At(revision + 1);
            Assert.AreEqual("dimrill-dale", room.Fp.Board.View.Characters.Single(c => c.Id == cost.RequiredCharacterId).Location);
            Assert.AreEqual("moria", room.Fp.Board.View.Units.Single(u => u.Id == nazgul).Location);
            Assert.IsNull(board.View.ActionSequence);
            T22NetworkActions.Spent(room, Side.Shadow, cost.DieId);
        }

        private static async Task Ruler()
        {
            using var room = await Connect("ui67-ruler", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            const string token = "kings.token.theoden";
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("ruler-token-" + token));
            var die = board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
            await ComponentGestures.Cost(room, room.Fp, die);
            await Inspect(room, room.Fp, "kings.sovereign.theoden");
            // UI-380: the token's ability stands on its chip (the command's "kings." prefix matches the printed id).
            await UseChip(room, room.Fp, "mustering-of-the-mark", "character-kings.sovereign.theoden-kings.mustering-of-the-mark", "theoden");
            // UI-450 (owner): the window keeps its form and the Reserve opens at once; each figure stands with its click and
            // the last one sends the recruitment, its token and its die in one commit (no ✓).
            Assert.AreNotSame(board.Characters.DecisionHost, board.Tray.parent, "The window keeps its form.");
            Assert.IsTrue(board.Reinforcements.IsOpen, "The Reserve opens at once.");
            var revision = board.View.Revision;
            var regions = new[] { "edoras", "helms-deep", "edoras" };
            for (var index = 0; index < regions.Length; index++)
            {
                if (index == regions.Length - 1)
                {
                    Assert.AreEqual(revision, room.Fp.Board.View.Revision, "Nothing is sent before the last figure.");
                    await KingsCardNetworkChecks.Layout(room, room.Fp, "ruler", "ui67");
                    await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                }
                room.Fp.Board.Map.Select(regions[index]);
                await room.Settle();
                PointerGestureEvents.ClickAt(room.Fp.Board.Reinforcements.Root.Q<Button>("character-reserve-rohan-Regular"));
            }
            await room.At(revision + 1);
            Assert.AreEqual("dismay", room.Sh.Board.View.RulerTokens.Single(t => t.Id == token).Location);
            Assert.AreEqual(DieFace.Muster, room.Sh.Board.View.Dice.Single(d => d.Id == die).Face);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Council()
        {
            await CouncilEntry();
            using var room = await Connect("ui67-council-travel", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            const string token = ExpansionDraft.CouncilTravel;
            var dice = board.View.Dice.Count(d => d.Location == DieLocation.Available);
            // UI-347 (D13): the Black Serpent can enter with the token. UI-388: its one click lights his entry and the
            // Nazgûl of its Travel; choosing a figure starts the Travel.
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("pool-" + token));
            await room.Settle();
            await Pick(room, room.Sh, "base.character.witch-king");
            var revision = board.View.Revision;
            FellowshipGestures.FinishRoute(room.Sh, "hollin");
            await room.Settle();
            // UI-379: the planned move waits for Finish unless it was the token's last movable figure.
            if (board.View.Revision == revision)
            {
                await room.Until(() => board.Console.Actions.Confirm.enabledInHierarchy);
                PointerGestureEvents.ClickAt(board.Console.Actions.Confirm);
            }
            await room.At(revision + 1);
            Assert.IsTrue(room.Fp.Board.View.CouncilTokens.Single(t => t.Id == token).Spent);
            Assert.AreEqual(dice, board.View.Dice.Count(d => d.Location == DieLocation.Available));
            Assert.AreEqual("hollin", room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.witch-king").Location);
        }

        private static async Task CouncilEntry()
        {
            using var room = await Connect("ui67-council-entry", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            const string token = ExpansionDraft.CouncilPolitics;
            var choice = board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                .Single(c => c.CharacterId == "kings.character.black-serpent");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("pool-" + token));
            await room.Settle();
            await ComponentGestures.Enter(room, room.Sh, choice);
            AssertNoEntryWindow(board);
            var revision = board.View.Revision;
            board.Map.Select("near-harad");
            await room.Settle();
            Assert.AreEqual(revision, board.View.Revision, "A token does not replace the missing die.");
            await KingsCardNetworkChecks.Reconnect(room, room.Sh);
            await ComponentGestures.Cost(room, room.Sh, choice.Dice[0]);
            await room.At(revision + 1);
            Assert.IsTrue(room.Fp.Board.View.CouncilTokens.Single(t => t.Id == token).Spent);
            Assert.AreEqual("near-harad", room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Location);
            T22NetworkActions.Spent(room, Side.Shadow, choice.Dice[0]);
        }

        private static async Task Fate()
        {
            using var room = await Connect("ui67-fate", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            const string figure = "base.character.gimli";
            var choice = board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility).CharacterActions
                .Single(c => c.CharacterId == figure && c.AbilityId == "messenger-dwarves");
            await ComponentGestures.Cost(room, room.Fp, choice.Dice[0]);
            board.Characters.Show(figure, true);
            await room.Settle();
            Assert.AreEqual(figure, board.Characters.Inspector.FigureId);
            Assert.AreEqual("fate.character.gimli-dwarven-lord", board.Characters.Inspector.DefinitionId);
            Assert.AreEqual("Gimli", board.Characters.Inspector.Root.Q<Label>(className: "title").text);
            Assert.AreEqual("Fate", board.Characters.Inspector.Root.Q<Label>("character-edition").text);
            var revision = board.View.Revision;
            // UI-440: the icon's one click sends what Click to use sent (the same component path on the same die).
            var expected = OldPayload(board.View, choice.Dice[0], ComponentGestures.Character(choice));
            var sent = new List<GameCommand>();
            room.Fp.Interaction.CommandAnswered += (command, receipt, _) => sent.Add(command);
            await UseChip(room, room.Fp, choice.AbilityId, ComponentGestures.Character(choice), "gimli");
            await room.At(revision + 1);
            Assert.AreEqual(CommandKind.UseCharacterAbility, sent.Single().Kind);
            Assert.AreEqual(expected, JsonConvert.SerializeObject(sent.Single().Payload), "The icon sends the old payload.");
            var nation = room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves");
            Assert.IsTrue(nation.Active);
            Assert.AreEqual(1, nation.StepsToWar);
            Assert.AreEqual("", room.Sh.Board.View.Characters.Single(c => c.Id == figure).NationIcon);
            T22NetworkActions.Spent(room, Side.FreePeoples, choice.Dice[0]);
        }

        private static void AssertNoEntryWindow(BoardScreen board)
        {
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display);
            Assert.AreNotSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.IsNull(board.Components.Root.Query<Button>().ToList().FirstOrDefault(b => b.name.StartsWith("component-character-") &&
                b.resolvedStyle.display != DisplayStyle.None), "No entry button in the decision dock.");
        }

        private static async Task Inspect(MatchTestRoom room, MountedMatchClient client, string id)
        {
            client.Board.Characters.Open();
            await room.Settle();
            PointerGestureEvents.ClickAt(Row(client.Board, id));
            await room.Settle();
            Assert.AreEqual(id, client.Board.Characters.Inspector.FigureId);
        }

        private static async Task Pick(MatchTestRoom room, MountedMatchClient client, string id)
        {
            FellowshipGestures.Figure(client, Location(client, id), id);
            await room.Settle();
        }

        private static bool Picked(MountedMatchClient client, string id)
        {
            client.Board.Hover.Open(Location(client, id));
            return client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id).ClassListContains("selected");
        }

        private static string Location(MountedMatchClient client, string id) =>
            client.Board.View.Characters.Single(c => c.Id == id).Location;

        // UI-369: an ability with a chip has no separate button: its chip glows, and once selected its Click to use uses it.
        // UI-440 (owner): the ability's bare icon glows while it can be used, its formatted tooltip names it and says what
        // the click does; one click uses it (no unrolled text, no Click to use): a one-click ability sends, a click-then-choose
        // one starts its choice on the board. shots: the four layouts' screenshots' name.
        private static async Task UseChip(MatchTestRoom room, MountedMatchClient client, string ability, string key, string shots)
        {
            await room.Settle();
            var inspector = client.Board.Characters.Inspector;
            Assert.IsNull(inspector.Actions.Q<Button>("component-" + key), "An ability with an icon has no separate button.");
            var chip = inspector.Root.Q<Button>("character-ability-" + ability);
            Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName), ability + " glows while it can be used.");
            var title = CharacterAbilityLibrary.For(inspector.DefinitionId).Single(item => item.Id == ability).Title;
            var plain = SkillTooltip.Plain(chip.tooltip);
            StringAssert.StartsWith(title + "\n", plain);
            StringAssert.Contains("\nClick to use your ", plain);
            SkillIconChecks.Bare(chip, AbilityArt.For(inspector.DefinitionId, ability), 28f);
            Assert.IsFalse(ReadOnlyControl.Is(chip));
            await SkillIconLayouts.Capture(room, client, "usable-" + shots);
            PointerGestureEvents.ClickAt(chip);
            await room.Settle();
            Assert.IsFalse(chip.ClassListContains(KeyboardFocusRing.ClassName), "A click leaves no focus ring round the icon.");
            Assert.IsNull(inspector.Root.Q("character-ability-sheet"), "No text unrolls; the tooltip describes.");
            LayoutTests.Save(client.Viewport, "ui440-used-" + shots + ".png");
        }

        // UI-440 (owner, 2026-11-02): the Balrog's icons stand in one row, each ability's icons adjacent in its group, in four
        // layouts; UI-459 (owner, 2026-11-03): Balrog Activation keeps one slot beside his three other abilities.
        private static async Task Widest(MatchTestRoom room, MountedMatchClient client)
        {
            var inspector = client.Board.Characters.Inspector;
            Assert.AreEqual(4, inspector.Abilities.Icons.Count(), "Balrog Activation's one slot and his three other abilities.");
            await SkillIconLayouts.Capture(room, client, "widest-balrog", () => OneRow(inspector));
        }

        private static void OneRow(CharacterInspector inspector)
        {
            var icons = inspector.Abilities.Icons.ToList();
            foreach (var icon in icons)
            {
                Assert.AreEqual(icons[0].worldBound.yMin, icon.worldBound.yMin, 0.5f, "One row: " + icon.name);
                Assert.LessOrEqual(icon.worldBound.xMax, inspector.Root.worldBound.xMax, icon.name + " stands inside the window.");
            }
            for (var index = 1; index < icons.Count; index++)
                Assert.Greater(icons[index].worldBound.xMin, icons[index - 1].worldBound.xMax, "In printed order, side by side.");
        }

        // UI-440: the payload the old Click to use sent for this action — the same component path (ComponentActions.Select)
        // on the chosen die — to compare with what the icon's click sends.
        private static string OldPayload(PlayerView view, string die, string key)
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = die);
            var action = ComponentActions.For(view, draft).Single(item => item.Key == key);
            Assert.IsTrue(ComponentActions.Select(view, draft, action), key);
            return JsonConvert.SerializeObject(draft.Payload);
        }

        private static Button Row(BoardScreen board, string id) => board.Characters.Root.Q<Button>("character-roster-" + id);
        private static Button Recovery(BoardScreen board) => PendingRecovery.Control(board);
    }
}
