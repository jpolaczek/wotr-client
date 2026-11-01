using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ComponentActionTests
    {
        [Test]
        public void AbilityIsAnchoredAtItsOwnerAndCannotSubstituteAnUnselectedCost()
        {
            var view = View();
            view.Characters.Add(new CharacterView { Id = "owner", Location = "moria" });
            var choice = new CharacterActionOption { CharacterId = "owner", AbilityId = "ability", Dice = { "cost" }, Targets = { "moria" }, TargetCount = 1 };
            view.Options.Add(new ActionOption { Kind = CommandKind.UseCharacterAbility, EligibleDice = { "cost" }, CharacterActions = { choice } });
            var draft = new TurnDraft();
            var unavailable = ComponentActions.For(view, draft).Single();
            Assert.AreEqual("moria", unavailable.Region);
            Assert.IsFalse(unavailable.Enabled);
            Assert.IsFalse(ComponentActions.Select(view, draft, unavailable));
            draft.Change(p => p.DieId = "cost");
            var selected = ComponentActions.For(view, draft).Single();
            choice.Targets[0] = "hollin";
            Assert.IsTrue(ComponentActions.Select(view, draft, selected));
            Assert.AreEqual("cost", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { "hollin" }, draft.Payload.Ids);
        }

        [Test]
        public void PassAndDiscardWithoutAnOwnerShowAnActionIcon()
        {
            var view = View();
            view.Options.Add(new ActionOption { Kind = CommandKind.Pass });
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardDie, EligibleDice = { "cost" } });
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "cost");
            var actions = ComponentActions.For(view, draft).ToDictionary(a => a.Key);
            Assert.AreEqual("pass", actions["Pass"].Icon);
            Assert.AreEqual("discard", actions["DiscardDie"].Icon);
            var buttons = new ComponentActionView();
            buttons.Apply(actions.Values, true);
            foreach (var key in new[] { "Pass", "DiscardDie" })
            {
                var button = buttons.Root.Q<Button>("component-" + key);
                Assert.IsTrue(button.ClassListContains(IconButton.ClassName), key);
                Assert.AreEqual("", button.text, key);
                Assert.AreEqual(actions[key].Icon, IconButton.Icon(button), key);
                StringAssert.StartsWith(actions[key].Detail, button.tooltip, key);
            }
        }

        // UI-286: a character's ability is a small picture of what it does; it glows only while a click can use it.
        [Test]
        public void SarumansVoiceIsTwoPicturesThatGlowOnlyWhileTheirDieIsChosen()
        {
            var view = View();
            view.Characters.Add(new CharacterView { Id = "base.character.saruman", Location = "orthanc", Side = Side.Shadow });
            var option = new ActionOption { Kind = CommandKind.UseCharacterAbility, EligibleDice = { "muster" } };
            foreach (var ability in new[] { "voice-recruit", "voice-upgrade" })
                option.CharacterActions.Add(new CharacterActionOption
                {
                    CharacterId = "base.character.saruman", AbilityId = ability, Title = "Voice", Detail = ability, Dice = { "muster" },
                    Targets = { "orthanc" }, TargetCount = 1
                });
            view.Options.Add(option);
            var draft = new TurnDraft();
            var buttons = new ComponentActionView();
            buttons.Apply(ComponentActions.For(view, draft), true);
            var recruit = buttons.Root.Q<Button>("component-character-base.character.saruman-voice-recruit");
            var upgrade = buttons.Root.Q<Button>("component-character-base.character.saruman-voice-upgrade");
            foreach (var button in new[] { recruit, upgrade })
            {
                Assert.IsTrue(button.ClassListContains("ability-icon"), button.name);
                Assert.AreEqual("", button.text, "A picture, not a text button.");
                Assert.IsFalse(button.enabledSelf, "Without its die the ability cannot be used.");
                Assert.IsFalse(button.ClassListContains(ChoiceGlow.ClassName), "It glows only while a click can use it.");
                StringAssert.StartsWith("Saruman · ", button.tooltip);
                StringAssert.EndsWith("Select an eligible die to use it", button.tooltip);
            }
            // UI-439: both show the Voice's own icon; a badge tells the recruit from the upgrade.
            var voice = Picture(AbilityArt.Folder + "muster-each-isengard-settlement-or-upgrade-two");
            Assert.IsNotNull(voice);
            foreach (var button in new[] { recruit, upgrade })
                Assert.AreEqual(voice, button.Q("icon-glyph").style.backgroundImage.value.texture, button.name);
            Assert.AreEqual(Picture(AbilityBadges.Muster), recruit.Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);
            Assert.AreEqual(Picture(AbilityBadges.Upgrade), upgrade.Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);

            draft.Change(p => p.DieId = "muster");
            buttons.Apply(ComponentActions.For(view, draft), true);
            Assert.AreSame(recruit, buttons.Root.Q<Button>(recruit.name), "The same control stays in its place.");
            foreach (var button in new[] { recruit, upgrade })
            {
                Assert.IsTrue(button.enabledSelf, button.name);
                Assert.IsTrue(button.ClassListContains(ChoiceGlow.ClassName), button.name);
                StringAssert.EndsWith("Click to use", button.tooltip);
            }
            buttons.Apply(ComponentActions.For(view, draft), false);
            Assert.IsFalse(recruit.ClassListContains(ChoiceGlow.ClassName), "A busy board invites no click.");
        }

        // UI-439: every component button that uses an ability shows the ability's own icon (AbilityArt) — a character's
        // action, the Mouth of Sauron's movement cost, the Balrog's — with a badge where one ability has several commands;
        // an entry keeps the board pin, and Narya shows Narya's icon.
        [Test]
        public void AbilityButtonsShowTheAbilitysOwnIcon()
        {
            var view = View();
            view.Characters.Add(new CharacterView { Id = "base.character.mouth-of-sauron", Location = "barad-dur", Side = Side.Shadow });
            view.Characters.Add(new CharacterView { Id = "balrog", DefinitionId = "lords.character.balrog", Location = "moria", Side = Side.Shadow });
            foreach (var kind in new[] { CommandKind.MoveArmies, CommandKind.AttackArmy })
            {
                var move = new ActionOption { Kind = kind, EligibleDice = { "muster" } };
                move.MovementCosts.Add(new MovementCostOption { DieId = "muster", Face = DieFace.Army, AbilityId = "muster-as-army-once-per-turn" });
                view.Options.Add(move);
            }
            var ability = new ActionOption { Kind = CommandKind.UseCharacterAbility, EligibleDice = { "muster" } };
            ability.CharacterActions.Add(new CharacterActionOption { CharacterId = "balrog", AbilityId = "activate-balrog", Title = "Activate", Dice = { "muster" } });
            view.Options.Add(ability);
            var entry = new ActionOption { Kind = CommandKind.EnterCharacter, EligibleDice = { "muster" } };
            entry.CharacterActions.Add(new CharacterActionOption { CharacterId = "lords.character.gothmog", Title = "Gothmog", Dice = { "muster" } });
            view.Options.Add(entry);
            view.Options.Add(new ActionOption { Kind = CommandKind.UseNarya, EligibleDice = { "muster" } });
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            var actions = ComponentActions.For(view, draft).ToDictionary(a => a.Key);
            var mouth = AbilityArt.Folder + "muster-as-army-once-per-turn";
            Assert.AreEqual(mouth, actions["MoveArmies-muster-as-army-once-per-turn"].Picture);
            Assert.AreEqual(AbilityBadges.Move, actions["MoveArmies-muster-as-army-once-per-turn"].Badge);
            Assert.AreEqual(mouth, actions["AttackArmy-muster-as-army-once-per-turn"].Picture);
            Assert.AreEqual(AbilityBadges.Attack, actions["AttackArmy-muster-as-army-once-per-turn"].Badge);
            var balrog = actions["character-balrog-activate-balrog"];
            Assert.AreEqual(AbilityArt.Folder + "balrog-activation", balrog.Picture, "The Balrog's command names his printed ability.");
            Assert.AreEqual("", balrog.Badge, "One command of its ability at a time needs no badge.");
            Assert.AreEqual(ComponentActions.EntryPicture, actions["character-lords.character.gothmog-"].Picture, "An entry uses no ability.");
            Assert.AreEqual(AbilityArt.Folder + "narya", actions["narya"].Picture);
            Assert.AreEqual("Narya · Activate the nation of Gandalf’s settlement\nClick to use",
                TooltipOf(actions["narya"]), "Narya's picture names the ring once.");
            var buttons = new ComponentActionView();
            buttons.Apply(actions.Values, true);
            foreach (var item in actions.Values)
            {
                var button = buttons.Root.Q<Button>("component-" + item.Key);
                Assert.AreEqual(Picture(item.Picture), button.Q("icon-glyph").style.backgroundImage.value.texture, item.Key);
                Assert.AreEqual(item.Badge.Length > 0, button.Q(className: IconButton.BadgeClass) != null, item.Key);
            }
        }

        private static string TooltipOf(ComponentAction item)
        {
            var buttons = new ComponentActionView();
            buttons.Apply(new[] { item }, true);
            return buttons.Root.Q<Button>("component-" + item.Key).tooltip;
        }

        private static UnityEngine.Texture2D Picture(string resource) => UnityEngine.Resources.Load<UnityEngine.Texture2D>(resource);

        [Test]
        public void SpecialTravelBindsItsSymbolAndKeepsUnrelatedFiguresUnavailable()
        {
            var view = View();
            view.Dice.Add(new DieView { Id = "cost", Owner = Side.Shadow, Face = DieFace.UseBalrog, Location = DieLocation.Available });
            view.Characters.Add(new CharacterView { Id = "balrog", Location = "moria", Side = Side.Shadow });
            var option = new ActionOption { Kind = CommandKind.MoveShadowCharacters, EligibleDice = { "cost" }, EligibleIds = { "balrog", "nazgul" } };
            option.MovementCosts.Add(new MovementCostOption { DieId = "cost", Face = DieFace.Character, AbilityId = "use-balrog", RequiredCharacterId = "balrog" });
            option.Travel.Add(new TravelOption { Id = "balrog", Origin = "moria", Range = 2, Targets = { "moria", "hollin" } });
            option.Travel.Add(new TravelOption { Id = "nazgul", Origin = "moria", Flight = true, Targets = { "moria", "hollin" } });
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "cost");
            Assert.IsTrue(ComponentActions.Select(view, draft, ComponentActions.For(view, draft).Single()));
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId);
            context.BindCost(view, draft);
            Assert.AreEqual(DieFace.Character, context.Face);
            Assert.IsFalse(TravelActions.CanSelect(view, draft, context, "nazgul"));
            Assert.IsTrue(TravelActions.Select(view, draft, context, "balrog"));
            Assert.AreEqual("use-balrog", draft.Payload.AbilityId);
            Assert.AreEqual("cost", draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
        }

        [Test]
        public void CouncilDropChecksThatPortraitsOwnHomelandsAndKeepsTheOtherPlacements()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.CouncilHomelands;
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.ChooseCouncilHomelands,
                CharacterActions = { new CharacterActionOption { CharacterId = "gimli", Targets = { "ered-luin" } } }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCouncilHomelands);
            var portrait = new DraftToken(DraftObject.CouncilPortrait, "gimli");
            Assert.IsFalse(DraftGesture.Drop(draft, view, portrait, new DraftToken(DraftObject.Region, "moria")));
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsTrue(DraftGesture.Drop(draft, view, portrait, new DraftToken(DraftObject.Region, "ered-luin")));
            Assert.AreEqual("ered-luin", draft.Payload.Placements.Single().RegionId);
            Assert.IsFalse(ComponentActions.Immediate(view, draft), "Council setup is one reviewed set.");
        }

        [Test]
        public void FarSightedSeparationRetainsTheTokenCostWithoutARegularFellowshipOption()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Dice.Add(new DieView { Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Face = DieFace.Muster, Location = DieLocation.Available });
            var option = new ActionOption
            {
                Kind = CommandKind.SeparateCompanions, EligibleIds = { "legolas" }, EligibleDice = { "ruler" },
                RulerTravel = new RulerTravelOption
                {
                    AbilityId = "far-sighted", Dice = { "ruler" },
                    Travel = { new TravelOption { Id = "legolas", Origin = "rivendell", Range = 3, Targets = { "rivendell", "hollin" } } }
                }
            };
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = "token", AbilityId = "far-sighted", Action = option });
            var draft = new TurnDraft();
            draft.Change(p => { p.DieId = "ruler"; p.RulerTokenId = "token"; });
            Assert.IsTrue(ComponentActions.Select(view, draft, ComponentActions.For(view, draft).Single()));
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId);
            context.BindCost(view, draft);
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "legolas"));
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.AreEqual("token", draft.Payload.RulerTokenId);
            Assert.AreEqual("far-sighted", draft.Payload.AbilityId);
            CollectionAssert.AreEqual(new[] { "legolas" }, draft.Payload.Ids);
        }

        // UI-439: Far-sighted is Thranduil's ability picture, offered with an eligible die.
        [Test]
        public void FarSightedIsThranduilsPicture()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.MoveCompanions, Enabled = true, EligibleDice = { "ruler" },
                RulerTravel = new RulerTravelOption { AbilityId = "kings.far-sighted", Dice = { "ruler" } }
            });
            var draft = new TurnDraft();
            Assert.IsEmpty(ComponentActions.For(view, draft).Where(c => c.Key.StartsWith("far-sighted")), "No die, no picture.");
            draft.Change(p => p.DieId = "ruler");
            var picture = ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions");
            Assert.AreEqual(AbilityArt.Folder + "far-sighted", picture.Picture, "UI-439: Far-sighted's own icon, prefix dropped.");
            Assert.AreEqual("", picture.Badge, "Far-sighted's buttons all move; their tooltips tell them apart.");
            Assert.AreEqual("kings.sovereign.thranduil", picture.OwnerId);
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "component", You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
        };
    }
}
