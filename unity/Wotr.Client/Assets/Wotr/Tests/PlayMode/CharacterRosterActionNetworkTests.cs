using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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
            await Ability(room, room.Sh, "MoveShadowCharacters-use-balrog");
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
            await Ability(room, room.Fp, "character-kings.sovereign.theoden-kings.mustering-of-the-mark");
            Assert.AreSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.Q<Label>("character-limits").resolvedStyle.display);
            var revision = board.View.Revision;
            foreach (var region in new[] { "edoras", "helms-deep", "edoras" })
            {
                board.Map.Select(region);
                await room.Settle();
                PointerGestureEvents.ClickAt(board.Reinforcements.Root.Q<Button>("character-reserve-rohan-Regular"));
            }
            Assert.AreEqual(revision, board.View.Revision, "Collective recruitment has one commit.");
            await CharacterRosterActionLayoutChecks.Capture(room, room.Fp, "ruler");
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            await room.Confirm(room.Fp);
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
            // UI-347 (D13): the Black Serpent can enter with the token, so the first click marks it and the second
            // starts Travel.
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("pool-" + token));
            await room.Settle();
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("pool-" + token));
            await room.Settle();
            await Pick(room, room.Sh, "base.character.witch-king");
            var revision = board.View.Revision;
            FellowshipGestures.FinishRoute(room.Sh, "hollin");
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
            await UseChip(room, room.Fp, choice.AbilityId, ComponentGestures.Character(choice));
            await room.At(revision + 1);
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

        private static async Task Ability(MatchTestRoom room, MountedMatchClient client, string key)
        {
            await room.Settle();
            var button = client.Board.Characters.Inspector.Actions.Q<Button>("component-" + key);
            Assert.IsNotNull(button, key);
            Assert.IsTrue(button.enabledInHierarchy, key);
            PointerGestureEvents.ClickAt(button);
            await room.Settle();
        }

        // UI-369: an ability with a chip has no separate button: its chip glows, and once selected its Click to use uses it.
        private static async Task UseChip(MatchTestRoom room, MountedMatchClient client, string ability, string key)
        {
            await room.Settle();
            var inspector = client.Board.Characters.Inspector;
            Assert.IsNull(inspector.Actions.Q<Button>("component-" + key), "An ability with a chip has no separate button.");
            var chip = inspector.Root.Q<Button>("character-ability-" + ability);
            Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName), ability + " glows while it can be used.");
            Assert.IsNull(inspector.Root.Q<Button>("character-ability-use-" + ability), "Click to use waits for the chip to be selected.");
            PointerGestureEvents.ClickAt(chip);
            await room.Settle();
            var use = inspector.Root.Q<Button>("character-ability-use-" + ability);
            Assert.IsNotNull(use, ability);
            Assert.IsTrue(use.enabledInHierarchy, ability);
            Assert.IsTrue(use.worldBound.Overlaps(inspector.Root.Q("character-ability-text").worldBound), "Click to use stands on the unrolled text.");
            LayoutTests.Save(client.Viewport, "ui369-click-to-use.png");
            PointerGestureEvents.ClickAt(use);
            await room.Settle();
        }

        private static Button Row(BoardScreen board, string id) => board.Characters.Root.Q<Button>("character-roster-" + id);
        private static Button Recovery(BoardScreen board) => PendingRecovery.Control(board);
    }
}
