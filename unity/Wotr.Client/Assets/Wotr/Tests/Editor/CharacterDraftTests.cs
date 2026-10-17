using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CharacterDraftTests
    {
        [Test]
        public void EntryRequiresExplicitCharacterAndNonEquivalentCostAndUndoPreservesVersion()
        {
            var view = View(CommandKind.EnterCharacter, DieFace.WillOfTheWest);
            var option = view.Options[0];
            var choice = new CharacterActionOption { CharacterId = "base.character.gandalf-white", Dice = { "die" }, Targets = { "fangorn", "lorien" } };
            option.CharacterActions.Add(choice);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsFalse(draft.Ready(option));
            CharacterDraft.Select(draft, view, choice);
            Assert.AreEqual("die", draft.Payload.DieId, "UI-239: the only die is taken for the player.");
            CharacterDraft.Region(draft, option, "fangorn");
            draft.Change(p => p.DieId = "die");
            Assert.IsTrue(draft.Ready(option));
            var command = draft.Command(view);
            CharacterDraft.Region(draft, option, "lorien");
            Assert.AreEqual("fangorn", command.Payload.RegionId);
            draft.Back();
            Assert.AreEqual("fangorn", draft.Payload.RegionId);
            Assert.AreEqual(choice.CharacterId, command.Payload.CharacterId);
            view.Characters.Add(new CharacterView { Id = "base.character.gandalf-grey", DefinitionId = choice.CharacterId });
            Assert.AreEqual("Gandalf the White", CharacterLibrary.Name(view, "base.character.gandalf-grey"));
        }

        [Test]
        public void FlyingGroupSelectsOneDestinationAndDoesNotCreateAnArtificialLongRoute()
        {
            var view = View(CommandKind.MoveShadowCharacters, DieFace.Character);
            var option = view.Options[0];
            option.EligibleIds.Add("nazgul");
            option.Travel.Add(new TravelOption { Id = "nazgul", Origin = "barad-dur", Flight = true, Targets = { "barad-dur", "hollin" } });
            view.Units.Add(new UnitView { Id = "nazgul", Kind = UnitKind.Nazgul, Location = "barad-dur", Side = Side.Shadow });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            TravelDraft.ToggleCompanion(draft, view, option, "nazgul");
            Assert.IsFalse(draft.Ready(option));
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "hollin"));
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEqual(new[] { "barad-dur", "hollin" }, draft.Command(view).Payload.CompanionMoves[0].Route);
            Assert.IsFalse(TravelDraft.Step(draft, view, BoardPresentation.Load(), "rivendell"));
            TravelDraft.Stay(draft, option);
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void ShadowfaxBonusDependsOnAdvertisedSmallGroupPartnersAndFixedHobbitsCannotSplit()
        {
            var view = View(CommandKind.MoveCompanions, DieFace.Character);
            var option = view.Options[0];
            option.Travel.Add(new TravelOption { Id = "white", Origin = "hollin", Range = 3, SmallGroupRange = 4, SmallGroupPartners = { "merry", "pippin" } });
            option.Travel.Add(new TravelOption { Id = "merry", Origin = "hollin", Range = 1 });
            option.Travel.Add(new TravelOption { Id = "pippin", Origin = "hollin", Range = 1 });
            Assert.AreEqual(4, TravelDraft.GroupTravel(new[] { "white", "merry" }, option).Range);
            Assert.AreEqual(3, TravelDraft.GroupTravel(new[] { "white", "merry", "pippin" }, option).Range);
            option.Kind = CommandKind.SeparateCompanions;
            option.EligibleIds.AddRange(new[] { "merry", "pippin" });
            option.Travel.RemoveAt(0);
            option.Travel.ForEach(t => t.RequiredGroup.AddRange(option.EligibleIds));
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            CollectionAssert.AreEquivalent(option.EligibleIds, draft.Payload.Ids);
            Assert.IsFalse(TravelDraft.CanToggle(draft, view, option, "merry"));
        }

        [Test]
        public void MessengerIsNeverAutoSpentAndCostModeIsCopiedIntoArmyAndEventCommands()
        {
            const string ability = "muster-as-army-once-per-turn";
            var view = View(CommandKind.MoveArmies, DieFace.Muster);
            var option = view.Options[0];
            option.MovementCosts.Add(new MovementCostOption { DieId = "die", Face = DieFace.Army, AbilityId = ability, MaximumMoves = 2 });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsEmpty(draft.Payload.DieId);
            draft.Change(p => p.DieId = "die");
            ArmyDraft.MatchCost(draft, option);
            Assert.AreEqual(ability, draft.Command(view).Payload.AbilityId);
            option.Kind = CommandKind.PlayEvent;
            option.Events.Add(new EventPlayOption { CardId = "card", Costs = { new EventCostOption { DieId = "die", Face = DieFace.Army, AbilityId = ability } } });
            draft.Clear();
            CardDraft.Select(draft, view, "card");
            Assert.IsEmpty(draft.Payload.DieId);
            CardDraft.SelectDie(draft, option, "die");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual(ability, draft.Command(view).Payload.AbilityId);
        }

        private static PlayerView View(CommandKind kind, DieFace face) => new PlayerView
        {
            MatchId = "character-draft",
            You = Side.FreePeoples,
            Decision = new DecisionView { Id = "choice", Kind = DecisionKind.Action },
            Options = { new ActionOption { Kind = kind, EligibleDice = { "die" } } },
            Dice = { new DieView { Id = "die", Face = face, Source = "standard", Owner = Side.FreePeoples, Location = DieLocation.Available } }
        };
    }
}
