using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ComponentActionTests
    {
        // UI-385 (owner): a king's ability names him and the printed ability it uses, never his id.
        [Test]
        public void AKingsAbilityNamesTheKingAndThePrintedAbility()
        {
            var view = new PlayerView
            {
                MatchId = "m", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
            };
            view.Sovereigns.Add(new SovereignView { Id = "kings.sovereign.dain", Title = "Dáin II Ironfoot", Status = SovereignStatus.Awakened });
            view.Dice.Add(new DieView { Id = "muster", Owner = Side.FreePeoples, Face = DieFace.Muster, Location = DieLocation.Available });
            var option = new ActionOption { Kind = CommandKind.UseCharacterAbility, Enabled = true, EligibleDice = { "muster" } };
            option.CharacterActions.Add(new CharacterActionOption
            {
                CharacterId = "kings.sovereign.dain", AbilityId = "kings.dwarvenking", Title = "Upgrade a Regular",
                Detail = "Return one Regular from this army to the reserves and recruit an Elite.", Dice = { "muster" }
            });
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            var action = ComponentActions.For(view, draft).Single(a => a.OwnerId == "kings.sovereign.dain");
            Assert.AreEqual("Dáin", action.Owner);
            Assert.AreEqual("Dwarvenking", ComponentActions.AbilityTitle(action.Character));
        }

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
                // UI-440 (owner): the skill's formatted description: its name and variant first, its owner, its text.
                var plain = SkillTooltip.Plain(button.tooltip);
                StringAssert.StartsWith(CharacterAbilityLibrary.Title("muster-each-isengard-settlement-or-upgrade-two") +
                    (button == recruit ? " · Recruit · Saruman\n" : " · Upgrade · Saruman\n"), plain);
                Assert.IsTrue(button.ClassListContains(RuntimeTooltip.FormattedClass));
                StringAssert.EndsWith("\nSelect an eligible die to use it", plain);
            }
            // UI-439: both show the Voice's own icon; a badge tells the recruit from the upgrade.
            var voice = Picture(AbilityArt.Folder + "muster-each-isengard-settlement-or-upgrade-two");
            Assert.IsNotNull(voice);
            foreach (var button in new[] { recruit, upgrade })
            {
                Assert.AreEqual(voice, button.Q("icon-glyph").style.backgroundImage.value.texture, button.name);
                // UI-440 (owner): the bare skill icon, no frame; the badge stays.
                SkillIconChecks.Bare(button, AbilityArt.Folder + "muster-each-isengard-settlement-or-upgrade-two");
            }
            Assert.AreEqual(Picture(AbilityBadges.Muster), recruit.Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);
            Assert.AreEqual(Picture(AbilityBadges.Upgrade), upgrade.Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);

            draft.Change(p => p.DieId = "muster");
            buttons.Apply(ComponentActions.For(view, draft), true);
            Assert.AreSame(recruit, buttons.Root.Q<Button>(recruit.name), "The same control stays in its place.");
            foreach (var button in new[] { recruit, upgrade })
            {
                Assert.IsTrue(button.enabledSelf, button.name);
                Assert.IsTrue(button.ClassListContains(ChoiceGlow.ClassName), button.name);
                StringAssert.Contains("\nClick to use · Voice · voice-", SkillTooltip.Plain(button.tooltip));
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
            // UI-440 (owner): Narya's formatted description names the ring once, first, and ends with what a click does.
            var narya = SkillTooltip.Plain(TooltipOf(actions["narya"]));
            StringAssert.StartsWith("Narya\n", narya);
            StringAssert.EndsWith("\nClick to use · Put the nation of Gandalf’s Settlement at war", narya);
            var buttons = new ComponentActionView();
            buttons.Apply(actions.Values, true);
            foreach (var item in actions.Values)
            {
                var button = buttons.Root.Q<Button>("component-" + item.Key);
                Assert.AreEqual(Picture(item.Picture), button.Q("icon-glyph").style.backgroundImage.value.texture, item.Key);
                Assert.AreEqual(item.Badge.Length > 0, button.Q(className: IconButton.BadgeClass) != null, item.Key);
                // UI-440 (owner): a button that uses an ability is the bare skill icon; an entry keeps its framed pin.
                if (item.Picture == ComponentActions.EntryPicture)
                    Assert.IsTrue(button.ClassListContains(IconButton.ClassName) && !button.ClassListContains(SkillIcon.ClassName), item.Key);
                else
                    SkillIconChecks.Bare(button, item.Picture);
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

        // UI-276 (EXP-15 D18): Far-sighted is Thranduil's ability picture, offered with an eligible die; there is no mode chip.
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
            // UI-440 (owner): Far-sighted's three moves are badged apart (the Fellowship, companions, a separation).
            Assert.AreEqual(AbilityBadges.Companions, picture.Badge);
            Assert.AreEqual("kings.sovereign.thranduil", picture.OwnerId);
        }

        // UI-456 (owner, 2026-11-03, kings-far-sighted): Far-sighted's Move companions waits for a companion outside the
        // Fellowship. Thranduil alone, the king who uses it, is none: the variant is greyed in its slot and says why, so its
        // click no longer starts a move whose only lit objects are the Fellowship's portraits (their click separates).
        [Test]
        public void FarSightedMoveCompanionsWaitsForACompanionOutsideTheFellowship()
        {
            const string thranduil = FarSightedMoves.Owner;
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            var option = new ActionOption
            {
                Kind = CommandKind.MoveCompanions, Enabled = true, EligibleIds = { thranduil }, EligibleDice = { "ruler" },
                Travel = { new TravelOption { Id = thranduil, Origin = "woodland-realm", Range = 2 } },
                RulerTravel = new RulerTravelOption
                {
                    AbilityId = "kings.far-sighted", Dice = { "ruler" }, RangeBonus = 1,
                    Travel = { new TravelOption { Id = thranduil, Origin = "woodland-realm", Range = 3 } }
                }
            };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "ruler");
            var move = ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions");
            Assert.IsFalse(move.Enabled, "Only Thranduil stands outside the Fellowship.");
            Assert.AreEqual(FarSightedMoves.NoCompanion, move.Reason);
            Assert.IsFalse(ComponentActions.Select(view, draft, move), "Its click starts nothing.");
            Assert.IsNull(draft.Kind);
            var actions = new ComponentActionView();
            actions.Apply(new[] { move }, true);
            var button = actions.Root.Q<Button>("component-far-sighted-MoveCompanions");
            Assert.IsFalse(button.enabledSelf, "Greyed in the region's window.");
            StringAssert.EndsWith(FarSightedMoves.NoCompanion, SkillTooltip.Plain(button.tooltip), "Its tooltip says why.");
            var chips = new CharacterAbilityChips();
            chips.Apply("kings.character.thranduil.awakened");
            chips.Usable(new[] { move }, true, view);
            var icon = chips.Icons.Single(b => b.name.EndsWith("-" + CommandKind.MoveCompanions));
            Assert.IsTrue(icon.ClassListContains(CharacterAbilityChips.IdleClass), "Dimmed in Thranduil's window.");
            StringAssert.EndsWith(FarSightedMoves.NoCompanion, SkillTooltip.Plain(icon.tooltip));

            const string legolas = "base.character.legolas";
            option.EligibleIds.Add(legolas);
            option.RulerTravel.Travel.Add(new TravelOption { Id = legolas, Origin = "lorien", Range = 4 });
            move = ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions");
            Assert.IsTrue(move.Enabled, "Legolas has left the Fellowship.");
            Assert.AreEqual("", move.Reason);
            Assert.IsTrue(ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions").Enabled);
        }

        // UI-443 (owner, reversing UI-440's decision 7): before any die is chosen an ability is not selected, even when one
        // die alone could pay for it; with that die chosen it is.
        [Test]
        public void AnAbilityIsNotSelectedBeforeItsDieEvenWhenOneDieCouldPay()
        {
            var view = View();
            view.Dice.Add(new DieView { Id = "muster", Owner = Side.Shadow, Face = DieFace.Muster, Source = "standard", Location = DieLocation.Available });
            view.Characters.Add(new CharacterView { Id = "base.character.saruman", Location = "orthanc", Side = Side.Shadow });
            var option = new ActionOption { Kind = CommandKind.UseCharacterAbility, Enabled = true, EligibleDice = { "muster" } };
            option.CharacterActions.Add(new CharacterActionOption
            {
                CharacterId = "base.character.saruman", AbilityId = "voice-upgrade", Title = "Voice", Dice = { "muster" },
                Targets = { "orthanc" }, TargetCount = 1
            });
            view.Options.Add(option);
            var draft = new TurnDraft();
            var waiting = ComponentActions.For(view, draft).Single();
            Assert.IsFalse(waiting.Enabled, "No die is chosen yet.");
            Assert.IsFalse(ComponentActions.Select(view, draft, waiting), "The one die that could pay is not taken.");
            Assert.AreEqual("", draft.Payload.DieId);
            draft.Change(p => p.DieId = "muster");
            var paid = ComponentActions.For(view, draft).Single();
            Assert.IsTrue(paid.Enabled);
            Assert.IsTrue(ComponentActions.Select(view, draft, paid));
            Assert.AreEqual("voice-upgrade", draft.Payload.AbilityId);
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "component", You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
        };
    }
}
