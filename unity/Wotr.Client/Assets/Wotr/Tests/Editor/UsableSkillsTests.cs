using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-441 (owner): "make sure the character always lights up whenever its skill can be used at that moment". The rule
    // (UsableSkills) names the characters whose skill icon lights now — and only those — for every class of skill.
    public sealed class UsableSkillsTests
    {
        private const string Gimli = "base.character.gimli";
        private const string Saruman = "base.character.saruman";
        private const string Gothmog = "lords.character.gothmog";
        private const string Gandalf = "base.character.gandalf-grey";

        [Test]
        public void AOneClickSkillIsUsableWithItsChosenDie()
        {
            var view = ActionView(Side.FreePeoples, Row(Gimli, Side.FreePeoples, "gimli"));
            Die(view, "army", DieFace.Army);
            Die(view, "muster", DieFace.Muster);
            Ability(view, Gimli, "politics-dwarves-at-erebor", "army");
            var skills = Skills(view, "army");
            CollectionAssert.AreEqual(new[] { Gimli }, skills.Keys);
            CollectionAssert.AreEqual(new[] { "Dwarf of Erebor" }, skills[Gimli].Names);
            Assert.AreEqual("", skills[Gimli].Place, "Its icon uses it.");
            Assert.AreEqual("Usable now: Dwarf of Erebor · Click its glowing icon", UsableSkills.Line(skills[Gimli], "Click its glowing icon"));
            CollectionAssert.IsEmpty(Skills(view, "muster"), "A die that cannot pay lights nothing.");
        }

        [Test]
        public void BeforeAnyDieIsChosenOnlyTheOneDieThatCanPayMakesASkillUsable()
        {
            var view = ActionView(Side.FreePeoples, Row(Gimli, Side.FreePeoples, "gimli"));
            Die(view, "army", DieFace.Army);
            Ability(view, Gimli, "politics-dwarves-at-erebor", "army");
            Assert.IsTrue(Skills(view, "").ContainsKey(Gimli), "The one die that can pay is the icon's click.");
            Die(view, "muster", DieFace.Muster);
            view.Options[0].CharacterActions[0].Dice.Add("muster");
            CollectionAssert.IsEmpty(Skills(view, ""), "Two different dice wait for the player's choice: no icon lights.");
        }

        // The variants of one skill (Saruman's Voice: recruit and upgrade) light their character once, by the skill's name.
        [Test]
        public void AnyVariantOfASkillLightsItsCharacterOnce()
        {
            var view = ActionView(Side.Shadow, Row(Saruman, Side.Shadow, "saruman"));
            Die(view, "muster", DieFace.Muster);
            Ability(view, Saruman, "voice-recruit", "muster");
            view.Options[0].CharacterActions.Add(new CharacterActionOption { CharacterId = Saruman, AbilityId = "voice-upgrade", Dice = { "muster" } });
            var skills = Skills(view, "muster");
            CollectionAssert.AreEqual(new[] { "The Voice of Saruman" }, skills[Saruman].Names);
            view.Options[0].CharacterActions.RemoveAt(0);
            Assert.IsTrue(Skills(view, "muster").ContainsKey(Saruman), "The upgrade alone still lights him.");
        }

        // Gothmog's Fling Them into the Fray (a card of the catalogue): either variant lights him once, by the skill's name.
        [Test]
        public void ASkillUsedOnItsOwnObjectLightsItsCharacter()
        {
            var view = ActionView(Side.Shadow, Row(Gothmog, Side.Shadow, "gothmog"));
            Die(view, "muster", DieFace.Muster);
            Ability(view, Gothmog, "gothmog-recruit", "muster");
            view.Options[0].CharacterActions.Add(new CharacterActionOption { CharacterId = Gothmog, AbilityId = "gothmog-upgrade", Dice = { "muster" } });
            var components = Components(view, "muster");
            var skills = UsableSkills.Characters(view, Draft("muster"), components, true);
            CollectionAssert.AreEqual(new[] { "Fling Them into the Fray" }, skills[Gothmog].Names);
        }

        // The window's icons light exactly for the rule's characters: the same condition, the one-die icon included.
        [Test]
        public void TheWindowsIconsLightExactlyWhileTheRuleNamesTheCharacter()
        {
            var view = ActionView(Side.Shadow, Row(Gothmog, Side.Shadow, "gothmog"));
            Die(view, "muster", DieFace.Muster);
            Ability(view, Gothmog, "gothmog-recruit", "muster");
            foreach (var die in new[] { "muster", "" })
            {
                var chips = new CharacterAbilityChips();
                chips.Apply(Gothmog);
                var components = Components(view, die);
                chips.Usable(components, true, view);
                var icon = chips.Root.Q<Button>("character-ability-fling-them-into-the-fray-gothmog-recruit");
                Assert.IsNotNull(icon, string.Join(", ", chips.Icons.Select(item => item.name)));
                Assert.IsTrue(icon.ClassListContains(ChoiceGlow.ClassName), "Die '" + die + "': the icon lights.");
                Assert.IsTrue(UsableSkills.Characters(view, Draft(die), components, true).ContainsKey(Gothmog), "Die '" + die + "'.");
            }
        }

        [Test]
        public void NothingIsUsableWhileAnotherActionIsDraftedOrNothingMayBeSent()
        {
            var view = ActionView(Side.FreePeoples, Row(Gimli, Side.FreePeoples, "gimli"));
            Die(view, "army", DieFace.Army);
            Ability(view, Gimli, "politics-dwarves-at-erebor", "army");
            var draft = Draft("army");
            var components = ComponentActions.For(view, draft).ToList();
            CollectionAssert.IsEmpty(UsableSkills.Characters(view, draft, components, false), "Nothing while a command is sent.");
            draft.Choose(CommandKind.MoveArmies, "army");
            CollectionAssert.IsEmpty(UsableSkills.Characters(view, draft, ComponentActions.For(view, draft), true),
                "Nothing while another action is drafted.");
        }

        // An entry, another version's ability and the opponent's characters are no usable skill of the player's.
        [Test]
        public void EntriesOtherVersionsAndTheOpponentsCharactersAreNotUsableSkills()
        {
            var view = ActionView(Side.FreePeoples, Row(Gimli, Side.FreePeoples, "gimli"));
            view.OwnCharacterRoster.Add(Row(Saruman, Side.Shadow, "saruman"));
            Die(view, "army", DieFace.Army);
            Ability(view, Gimli, "messenger-dwarves", "army");
            view.Options[0].CharacterActions.Add(new CharacterActionOption { CharacterId = Saruman, AbilityId = "voice-recruit", Dice = { "army" } });
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.EnterCharacter, EligibleDice = { "army" },
                CharacterActions = { new CharacterActionOption { CharacterId = Gimli, AbilityId = "enter", Dice = { "army" } } }
            });
            CollectionAssert.IsEmpty(Skills(view, "army"));
        }

        // A reaction offered to the player now lights its character, and the line names where it is used.
        [Test]
        public void AReactionOfferedNowLightsItsCharacter()
        {
            var view = ReactionView(Row(Gandalf, Side.FreePeoples, "gandalf"));
            view.Options.Add(Respond("character.grey-draw", Gandalf));
            var skills = UsableSkills.Characters(view, new TurnDraft(), Enumerable.Empty<ComponentAction>(), true);
            CollectionAssert.AreEqual(new[] { "Guide" }, skills[Gandalf].Names, "The printed ability the reaction uses.");
            Assert.AreEqual(SkillReactionPlaces.Decision, skills[Gandalf].Place);
            StringAssert.EndsWith(" · " + SkillReactionPlaces.Decision, UsableSkills.Line(skills[Gandalf], "Click its glowing icon"));
            view.Options[0].Enabled = false;
            CollectionAssert.IsEmpty(UsableSkills.Characters(view, new TurnDraft(), Enumerable.Empty<ComponentAction>(), true),
                "A reaction no longer offered lights nothing.");
        }

        // A Guide's Hunt response without a source belongs to the Guide; a card's reaction lights no character.
        [Test]
        public void AGuidesHuntResponseLightsTheGuideAndACardsReactionNoCharacter()
        {
            var merry = "base.character.merry";
            var view = ReactionView(Row(merry, Side.FreePeoples, "merry"));
            view.Hunt = new HuntView();
            view.Decision.Window = HuntGuideResponses.Window;
            view.Fellowship = new FellowshipView { Guide = merry };
            view.Options.Add(Respond("character.merry-guide", ""));
            view.Options[0].EffectChoices.Add(new EffectChoiceView { Id = "card.reaction", SourceId = "base.fp.strategy.01", Title = "A card" });
            view.Options[0].EligibleIds.Add("card.reaction");
            var skills = UsableSkills.Characters(view, new TurnDraft(), Enumerable.Empty<ComponentAction>(), true);
            CollectionAssert.AreEqual(new[] { merry }, skills.Keys);
            Assert.AreEqual(SkillReactionPlaces.HuntResponse, skills[merry].Place);
        }

        private static IReadOnlyDictionary<string, UsableSkills.Skill> Skills(PlayerView view, string die) =>
            UsableSkills.Characters(view, Draft(die), Components(view, die), true);

        private static List<ComponentAction> Components(PlayerView view, string die) => ComponentActions.For(view, Draft(die)).ToList();

        private static TurnDraft Draft(string die)
        {
            var draft = new TurnDraft();
            if (die.Length > 0)
                draft.Change(p => p.DieId = die);
            return draft;
        }

        private static PlayerView ActionView(Side side, CharacterRosterView row)
        {
            var view = new PlayerView
            {
                MatchId = "m", You = side, DecisionOwner = side,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
            };
            view.OwnCharacterRoster.Add(row);
            return view;
        }

        private static PlayerView ReactionView(CharacterRosterView row)
        {
            var view = new PlayerView
            {
                MatchId = "m", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction }
            };
            view.OwnCharacterRoster.Add(row);
            return view;
        }

        private static CharacterRosterView Row(string id, Side side, string identity) => new CharacterRosterView
        {
            FigureId = id, Side = side, Identity = identity, CurrentDefinitionId = id, Lifecycle = CharacterRosterLifecycle.OnBoard
        };

        private static void Die(PlayerView view, string id, DieFace face) => view.Dice.Add(new DieView
        {
            Id = id, Owner = view.You, Face = face, Source = "standard", Location = DieLocation.Available
        });

        private static void Ability(PlayerView view, string character, string ability, string die)
        {
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.UseCharacterAbility);
            if (option == null)
                view.Options.Add(option = new ActionOption { Kind = CommandKind.UseCharacterAbility, EligibleDice = { die } });
            option.CharacterActions.Add(new CharacterActionOption { CharacterId = character, AbilityId = ability, Title = ability, Dice = { die } });
        }

        private static ActionOption Respond(string reaction, string source) => new ActionOption
        {
            Kind = CommandKind.RespondEffect, EligibleIds = { reaction },
            EffectChoices = { new EffectChoiceView { Id = reaction, SourceId = source, Title = "Response " + reaction } }
        };
    }
}
