using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-461 (owner, 2026-11-03): a Will of the West die is Will of the West itself until the console's face menu turns it
    // into another result; the result is a local, never-sent part of the draft that belongs to its die.
    public sealed class WillFaceTests
    {
        [Test]
        public void AWillDieAsItselfLightsOnlyWhatItDoesAsItself()
        {
            var view = DieOptionsTests.Fixture();
            var die = Context(view, "will", null);
            Assert.IsTrue(die.Will);
            Assert.AreEqual(DieFace.WillOfTheWest, die.Face);
            Assert.IsNull(die.WillFace);
            var options = DieOptions.For(view, new TurnDraft(), die);
            Assert.IsTrue(options.Characters, "Gandalf the White enters with Will of the West itself.");
            Assert.IsFalse(options.Fellowship, "Moving the Fellowship takes a Character result.");
            Assert.IsFalse(options.Armies);
            Assert.IsFalse(options.Recruit);
            Assert.IsFalse(options.Politics);
            Assert.IsEmpty(ContextArmySelection.Eligible(view, die));
        }

        [Test]
        public void ATurnedWillDieLightsExactlyWhatThatResultDoes()
        {
            var view = DieOptionsTests.Fixture();
            var character = DieOptions.For(view, new TurnDraft(), Context(view, "will", DieFace.Character));
            Assert.IsTrue(character.Fellowship);
            Assert.IsFalse(character.Characters, "An entry that takes Will of the West itself goes dark once it is turned.");
            Assert.IsFalse(character.Recruit);
            var muster = DieOptions.For(view, new TurnDraft(), Context(view, "will", DieFace.Muster));
            Assert.IsTrue(muster.Recruit);
            Assert.IsTrue(muster.Politics);
            Assert.IsFalse(muster.Fellowship);
            Assert.IsFalse(muster.Armies);
        }

        [TestCase(DieFace.Army, 3, DieFace.Army)]
        [TestCase(DieFace.Character, 1, DieFace.Character)]
        public void AnArmyMoveSendsTheResultTheDieWasTurnedInto(DieFace face, int sources, DieFace cost)
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = DieFace.WillOfTheWest;
            fixture.Refresh();
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "army");
            draft.TurnWill(face);
            var die = Context(fixture.View, "army", draft.WillFace);
            Assert.AreEqual(sources, ContextArmySelection.Sources(fixture.View, die).Count(),
                "A Character result moves only a force with a commander.");
            var unit = fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular);
            Assert.IsTrue(ContextArmySelection.Select(draft, fixture.View, die, unit.Id));
            Assert.AreEqual(cost, draft.Payload.MovementCost);
            Assert.AreEqual(face, draft.WillFace, "Starting the move keeps the die's result.");
        }

        [Test]
        public void TheResultBelongsToTheDraftsDieAndBackUndoesIt()
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "will");
            WillFaceChoices.Choose(draft, DieFace.Army);
            Assert.AreEqual(DieFace.Army, draft.WillFace);
            WillFaceChoices.Choose(draft, DieFace.Event);
            Assert.AreEqual(DieFace.Event, draft.WillFace);
            draft.Back();
            Assert.AreEqual(DieFace.Army, draft.WillFace, "Back undoes one change of the result.");
            WillFaceChoices.Choose(draft, DieFace.Army);
            Assert.IsNull(draft.WillFace, "A second click on the chosen result makes it Will of the West itself.");
            WillFaceChoices.Choose(draft, DieFace.Muster);
            draft.Choose(CommandKind.Recruit, "will");
            Assert.AreEqual(DieFace.Muster, draft.WillFace, "An action of the same die keeps its result.");
            draft.Change(p => p.DieId = "other");
            Assert.IsNull(draft.WillFace, "Another die has no result of its own.");
            draft.Change(p => p.DieId = "will");
            Assert.AreEqual(DieFace.Muster, draft.WillFace);
            draft.Clear();
            draft.Change(p => p.DieId = "will");
            Assert.IsNull(draft.WillFace, "A released die comes back as itself.");
        }

        [Test]
        public void ForksCheckpointsAndSnapshotsKeepTheResult()
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "will");
            draft.TurnWill(DieFace.Character);
            Assert.AreEqual(DieFace.Character, draft.Fork().WillFace);
            var adopted = new TurnDraft();
            adopted.Adopt(draft);
            Assert.AreEqual(DieFace.Character, adopted.WillFace);
            var checkpoint = draft.Checkpoint();
            draft.TurnWill(null);
            draft.Rewind(checkpoint);
            Assert.AreEqual(DieFace.Character, draft.WillFace);
            var restored = new TurnDraft();
            restored.Restore(draft.Snapshot());
            Assert.AreEqual(DieFace.Character, restored.WillFace);
        }

        [Test]
        public void TheMenuOffersFourFixedResultsWhileTheDieWaitsForItsUse()
        {
            var view = DieOptionsTests.Fixture();
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "will");
            var slots = WillFaceChoices.For(view, draft, Context(view, "will", null));
            CollectionAssert.AreEqual(new[] { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event }, slots.Select(s => s.Face));
            Assert.IsTrue(slots.Single(s => s.Face == DieFace.Character).Enabled, "The Fellowship can move with a Character result.");
            Assert.IsTrue(slots.Single(s => s.Face == DieFace.Muster).Enabled);
            Assert.IsFalse(slots.Single(s => s.Face == DieFace.Event).Enabled, "No card or deck takes an Event result here.");
            Assert.IsFalse(slots.Any(s => s.Chosen));
            draft.TurnWill(DieFace.Muster);
            slots = WillFaceChoices.For(view, draft, Context(view, "will", draft.WillFace));
            Assert.IsTrue(slots.Single(s => s.Face == DieFace.Muster).Chosen);
            StringAssert.Contains("Click again", slots.Single(s => s.Face == DieFace.Muster).Tooltip);
            draft.Choose(CommandKind.Recruit, "will");
            Assert.IsEmpty(WillFaceChoices.For(view, draft, Context(view, "will", draft.WillFace)), "A started action hides the menu.");
            Assert.IsEmpty(WillFaceChoices.For(view, new TurnDraft(), Context(view, "army", null)), "Only a Will die has the menu.");
        }

        [Test]
        public void AStartedActionTellsTheResultSoAReloadedDraftNeedsNoMenu()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = DieFace.WillOfTheWest;
            fixture.Refresh();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies, "army");
            draft.Change(p =>
            {
                p.MovementCost = DieFace.Army;
                p.Moves.Add(new ArmyMoveChoice { From = "minas-tirith" });
            });
            var die = Context(fixture.View, "army", null);
            die.BindCost(fixture.View, draft);
            Assert.AreEqual(DieFace.Army, die.WillFace);
        }

        [Test]
        public void RemovalsAndAsItselfAbilitiesTakeOnlyTheUnturnedDie()
        {
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.DiscardDenethor, null));
            Assert.IsFalse(WillOfTheWestUses.Pays(CommandKind.DiscardDenethor, DieFace.Character));
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.UseNarya, DieFace.Army), "Narya takes any die.");
            Assert.IsFalse(WillOfTheWestUses.Pays(CommandKind.SeparateCompanions, null));
            var deactivate = new CharacterActionOption { CharacterId = "lords.character.balrog", AbilityId = "deactivate-balrog" };
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.UseCharacterAbility, deactivate, null));
            Assert.IsFalse(WillOfTheWestUses.Pays(CommandKind.UseCharacterAbility, deactivate, DieFace.Character));
            var politics = new CharacterActionOption { CharacterId = "base.character.gimli", AbilityId = "politics-dwarves-at-erebor" };
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.UseCharacterAbility, politics, null));
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.UseCharacterAbility, politics, DieFace.Event), "A political ability takes any die.");
            var aragorn = new CharacterActionOption { CharacterId = "base.character.aragorn" };
            Assert.IsTrue(WillOfTheWestUses.Pays(CommandKind.EnterCharacter, aragorn, null));
            Assert.IsFalse(WillOfTheWestUses.Pays(CommandKind.EnterCharacter, aragorn, DieFace.Muster));
        }

        private static ActionDieContext Context(PlayerView view, string id, DieFace? face)
        {
            var context = new ActionDieContext();
            context.Sync(view, id, "", face);
            return context;
        }
    }
}
