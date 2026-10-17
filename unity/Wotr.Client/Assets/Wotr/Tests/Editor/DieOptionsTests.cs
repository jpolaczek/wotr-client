using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-136: the chosen die lights only what the server's options let it do.
    public sealed class DieOptionsTests
    {
        [Test]
        public void MusterLightsPoliticsAndRecruitmentButNotTheFellowship()
        {
            var view = Fixture();
            var options = DieOptions.For(view, new TurnDraft(), Die(view, "muster"));
            Assert.IsTrue(options.Politics);
            Assert.IsTrue(options.Recruit);
            Assert.IsFalse(options.Fellowship);
            Assert.IsFalse(options.Armies);
            Assert.IsFalse(options.Characters);
        }

        [Test]
        public void WillOfTheWestLightsTheFellowshipMoveAndACharacterEntry()
        {
            var view = Fixture();
            var options = DieOptions.For(view, new TurnDraft(), Die(view, "will"));
            Assert.IsTrue(options.Fellowship, "Will of the West can move the Fellowship.");
            Assert.IsTrue(options.Characters, "Will of the West can bring in the listed character.");
            Assert.IsTrue(options.Armies);
            var army = DieOptions.For(view, new TurnDraft(), Die(view, "army"));
            Assert.IsFalse(army.Fellowship, "An Army die cannot move the Fellowship.");
            Assert.IsFalse(army.Characters);
        }

        // UI-196: a Character die that can only move companions (on the map) leaves the Characters tab dark.
        [Test]
        public void ACharacterDieLightsTheCharactersTabOnlyForWhatTheTabOffers()
        {
            var view = Fixture();
            view.Dice.Add(new DieView { Id = "character", Face = DieFace.Character, Source = "standard", Location = DieLocation.Available });
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveCompanions, EligibleDice = { "character" } });
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveShadowCharacters, EligibleDice = { "character" } });
            Assert.IsFalse(DieOptions.For(view, new TurnDraft(), Die(view, "character")).Characters,
                "A plain move of companions is made on the map.");

            var ability = new ActionOption { Kind = CommandKind.MoveCompanions, EligibleDice = { "character" } };
            ability.MovementCosts.Add(new MovementCostOption { DieId = "character", AbilityId = "strider-guide", Face = DieFace.Character });
            view.Options.Add(ability);
            Assert.IsTrue(DieOptions.For(view, new TurnDraft(), Die(view, "character")).Characters,
                "A move paid by a character's ability is offered in the Characters tab.");
            ability.Enabled = false;
            Assert.IsFalse(DieOptions.For(view, new TurnDraft(), Die(view, "character")).Characters);

            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseCharacterAbility,
                CharacterActions = { new CharacterActionOption { CharacterId = "base.character.strider", AbilityId = "heir", Dice = { "character" } } }
            });
            Assert.IsTrue(DieOptions.For(view, new TurnDraft(), Die(view, "character")).Characters,
                "A character's ability that takes the die lights the tab.");
        }

        [Test]
        public void NothingGlowsWithoutAChosenDieOrOutsideTheViewersAction()
        {
            var view = Fixture();
            Assert.IsFalse(DieOptions.For(view, new TurnDraft(), Die(view, "")).Any);
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(DieOptions.For(view, new TurnDraft(), Die(view, "muster")).Any);
        }

        internal static PlayerView Fixture()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            view.Dice.Add(new DieView { Id = "will", Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available });
            fixture.Refresh();
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveFellowship, EligibleDice = new List<string> { "will" } });
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.EnterCharacter,
                CharacterActions = { new CharacterActionOption { CharacterId = "base.character.gandalf-white", Dice = { "will" } } }
            });
            return view;
        }

        private static ActionDieContext Die(PlayerView view, string id)
        {
            var context = new ActionDieContext();
            context.Sync(view, id);
            return context;
        }
    }
}
