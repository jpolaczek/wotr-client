using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class CharacterRosterPanelTests
    {
        [Test]
        public void RealRosterKeepsWhiteReturnOutOfRemovedAndDoesNotDuplicateFellowship()
        {
            var view = Read("CharacterRosterWire.json");
            var data = BoardPresentation.Load();
            var panel = new CharacterRosterPanel(new VisualElement(), data, _ => Assert.Fail("Inspection moved the camera"));
            panel.Apply(view);
            var white = panel.Root.Q<Button>("character-roster-base.character.gandalf-grey");
            Assert.IsNotNull(white);
            StringAssert.Contains("White", white.Q<Label>("character-roster-title").text);
            Assert.AreEqual("characters-section-NotInPlay", white.parent.parent.name);
            Assert.IsNull(panel.Root.Q<Button>("character-roster-base.character.gandalf-white"));
            Assert.IsFalse(CharacterRosterPresentation.Rows(view).Any(r => r.Identity is "gollum" or "smeagol"));
            panel.Show("base.character.gandalf-grey", true);
            Assert.IsNull(panel.Inspector.Root.Q<Button>("enter-character"));
            // UI-436: off the map the place's picture keeps its slot, disabled, and says where he is and what it means.
            var place = panel.Inspector.Root.Q<Button>("locate-character");
            Assert.IsFalse(place.enabledSelf);
            Assert.AreEqual("Removed", panel.Inspector.Place.Caption);
            Assert.AreEqual("removed-free-peoples", panel.Inspector.Place.Place.Key);
            Assert.AreEqual("Eliminated; may return as Gandalf the White", panel.Inspector.Root.Q<Label>("character-state").text);
        }

        [Test]
        public void TransformRefreshKeepsTheSameRowInspectorAndPinnedChoice()
        {
            var view = Read("CharacterRosterWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            const string id = "base.character.gandalf-grey";
            var button = panel.Root.Q<Button>("character-roster-" + id);
            panel.Show(id, true);
            var row = view.OwnCharacterRoster.Single(r => r.FigureId == id);
            row.CurrentDefinitionId = "base.character.gandalf-white";
            row.Title = "Gandalf the White";
            row.Lifecycle = CharacterRosterLifecycle.OnBoard;
            row.RegionId = "fangorn";
            row.FutureVersions.Clear();
            panel.Apply(view);
            Assert.AreSame(button, panel.Root.Q<Button>("character-roster-" + id));
            Assert.AreEqual("characters-section-OnBoard", button.parent.parent.name);
            Assert.IsTrue(panel.InspectorPinned);
            Assert.AreEqual(id, panel.Inspector.FigureId);
            Assert.IsTrue(panel.Inspector.Root.Q<Button>("locate-character").enabledSelf, "On the map his place's picture locates it.");
            Assert.AreEqual("Fangorn", panel.Inspector.Place.Caption);
            Assert.AreEqual("fangorn", panel.Inspector.Place.Place.RegionId);
        }

        [Test]
        public void FateStatisticsSeparateMovementFromCombatAndHuntLevel()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            var text = inspector.Root.Q("character-statistics").tooltip;
            StringAssert.Contains("Level 1", text);
            StringAssert.Contains("Move 2", text);
            Assert.IsFalse(text.Contains("Level 2"));
            // UI-420: in words a level equal to Level is not repeated; its symbol still names it.
            StringAssert.DoesNotContain("Combat level", text);
            StringAssert.StartsWith("Combat level 1", inspector.Root.Q("character-fact-combat").tooltip);
        }

        [Test]
        public void CardStatisticsAreSymbolsThatExplainThemselves()
        {
            var nazgul = new CharacterRosterView { Level = 3, MovementLevel = 3, CombatLevel = 3, Leadership = 2,
                UnlimitedMovement = true, NationIcon = "sauron", CurrentDefinitionId = "base.character.witch-king" };
            var facts = CharacterCardFacts.For(nazgul, nazgul.CurrentDefinitionId, true);
            CollectionAssert.AreEqual(new[] { "level", "flight", "combat", "leadership", "nation" }, facts.Select(f => f.Key));
            Assert.AreEqual("∞", facts.Single(f => f.Key == "flight").Value);
            Assert.AreEqual("sauron", facts.Single(f => f.Key == "nation").Nation);
            Assert.IsTrue(facts.All(f => f.Tooltip.Length > 0));
            Assert.AreEqual("Flight · Unlimited movement", facts.Single(f => f.Key == "flight").Tooltip);
            // UI-420: Combat level equal to Level is not repeated in words; the nation is named plainly.
            Assert.AreEqual("Level 3 · Flight · Leadership 2 · Sauron", CharacterCardFacts.Summary(facts));
            var white = CharacterCardFacts.For(new CharacterRosterView(), "base.character.gandalf-white", false);
            Assert.AreEqual("All Free Peoples", white.Single(f => f.Key == "nation").Label);
            var serpent = new CharacterRosterView { Level = 2, MovementLevel = 2, CombatLevel = 3, Leadership = 2, NationIcon = "southrons",
                CurrentDefinitionId = "kings.character.black-serpent" };
            Assert.AreEqual("Level 2 · Combat level 3 · Leadership 2 · Southrons & Easterlings",
                CharacterCardFacts.Summary(CharacterCardFacts.For(serpent, serpent.CurrentDefinitionId, true)));
            Assert.AreEqual("removed", CharacterCardFacts.Status(new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.Eliminated }, "").Key);
            var onBoard = CharacterCardFacts.Status(new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.OnBoard, Garrison = true }, "Erebor");
            Assert.AreEqual(("on-board", "Erebor · Garrison", "Erebor"), (onBoard.Key, onBoard.Text, onBoard.Short));
        }

        // UI-240: an enterable row says what its click does: a change into the next version or an entry.
        [Test]
        public void AnEnterableRowNamesTheChangeItsClickMakes()
        {
            var strider = new CharacterRosterView
            {
                FigureId = "base.character.strider", CurrentDefinitionId = "base.character.strider", Lifecycle = CharacterRosterLifecycle.OnBoard,
                FutureVersions = { new CharacterEntryVersionView { DefinitionId = "base.character.aragorn", Entry = new ActionAvailabilityView { CanAct = true } } }
            };
            StringAssert.StartsWith("Click to turn Strider into Aragorn", CharacterRosterPresentation.EntryOffer(strider));
            var white = new CharacterRosterView
            {
                FigureId = "base.character.gandalf-white", Lifecycle = CharacterRosterLifecycle.NotEntered,
                FutureVersions = { new CharacterEntryVersionView { DefinitionId = "base.character.gandalf-white", Entry = new ActionAvailabilityView { CanAct = true } } }
            };
            Assert.AreEqual("Click to bring Gandalf the White into play", CharacterRosterPresentation.EntryOffer(white));
            Assert.AreEqual("Click to separate Merry from the Fellowship",
                FellowshipRoster.Offer(new PlayerView(), "base.character.merry", CommandKind.SeparateCompanions));
            Assert.AreEqual("", FellowshipRoster.Offer(new PlayerView(), "base.character.merry", null));
        }

        [Test]
        public void InspectorShowsStatusStatisticsAndAbilityChipsWithoutUnrolledText()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            Assert.AreEqual("Gimli", inspector.Root.Q<Label>(className: "title").text);
            Assert.AreEqual("Fate", inspector.Root.Q<Label>("character-edition").text);
            Assert.AreEqual("iron-hills", inspector.Place.Place.Key, "UI-436: his place is the picture of his region.");
            Assert.AreEqual("On the board", inspector.Root.Q<Label>("character-state").text);
            var facts = inspector.Root.Query(className: CharacterFactStrip.ItemClass).ToList()
                .Where(item => item.style.display.value != DisplayStyle.None).ToList();
            Assert.GreaterOrEqual(facts.Count, 4);
            Assert.IsTrue(facts.All(item => item.tooltip.Length > 0));
            var chips = inspector.Abilities.Icons.ToList();
            var printed = CharacterAbilityLibrary.For(inspector.DefinitionId);
            Assert.AreEqual(printed.Count, chips.Count, "One icon per printed ability, in one row.");
            for (var index = 0; index < printed.Count; index++)
            {
                // UI-440 (owner): the chip is the ability's bare icon; its formatted tooltip names the ability, then gives its
                // gist, printed text and source; nothing can be used, so the icon is read only.
                SkillIconChecks.Bare(chips[index], AbilityArt.For(inspector.DefinitionId, printed[index].Id));
                var plain = SkillTooltip.Plain(chips[index].tooltip);
                StringAssert.StartsWith(printed[index].Title + "\n", plain);
                StringAssert.Contains(printed[index].Text, plain);
                StringAssert.Contains(printed[index].Source, plain);
                Assert.IsTrue(chips[index].ClassListContains(RuntimeTooltip.FormattedClass));
                Assert.IsTrue(ReadOnlyControl.Is(chips[index]), "A click does nothing while the ability cannot be used.");
                Assert.IsFalse(inspector.Abilities.Choose(chips[index].name));
            }
            Assert.IsNull(inspector.Root.Q<Label>("character-ability-text"), "UI-440: no text unrolls; the tooltip describes.");
        }

        // UI-440 (owner): a lit icon's click uses its ability at once, as Click to use did (the same action, so the same
        // command); one waiting for its die is read only and says so; an action of none of the card's abilities (an entry)
        // keeps its own button.
        [Test]
        public void AUsableAbilityGlowsAndItsClickUsesIt()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            var printed = CharacterAbilityLibrary.For(inspector.DefinitionId).First();
            var politics = Politics(printed.Id, true);
            var entry = new ComponentAction { Key = "entry", Kind = CommandKind.EnterCharacter, Enabled = true, Character = new CharacterActionOption() };
            var used = new List<ComponentAction>();
            inspector.Abilities.Use += used.Add;
            var buttons = inspector.Abilities.Usable(new[] { politics, entry }, true);
            CollectionAssert.AreEqual(new[] { entry }, buttons, "Only an action without an icon keeps its own button.");
            var chip = inspector.Root.Q<Button>("character-ability-" + printed.Id);
            Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName), "A usable ability's icon glows.");
            Assert.IsFalse(ReadOnlyControl.Is(chip));
            var plain = SkillTooltip.Plain(chip.tooltip);
            StringAssert.StartsWith(printed.Title + "\n", plain);
            StringAssert.EndsWith("\nClick to use · Move the Dwarves one step toward war.", plain);
            Assert.IsTrue(inspector.Abilities.Choose(chip.name));
            CollectionAssert.AreEqual(new[] { politics }, used, "The click uses the very action Click to use used.");
            Assert.IsNull(inspector.Root.Q<Button>("character-ability-use-" + printed.Id), "No Click to use any more.");

            inspector.Abilities.Usable(new[] { Politics(printed.Id, false) }, true);
            Assert.IsFalse(chip.ClassListContains(ChoiceGlow.ClassName), "A disabled action does not light its icon.");
            Assert.IsTrue(ReadOnlyControl.Is(chip));
            StringAssert.EndsWith("\n" + SkillUse.ChooseDie, SkillTooltip.Plain(chip.tooltip));
            Assert.IsFalse(inspector.Abilities.Choose(chip.name), "Without its die the click uses nothing.");
            Assert.AreEqual(1, used.Count);
        }

        // UI-443 (owner, reversing UI-440's decision 7): before any die is chosen, an ability that one die alone could pay
        // waits for that die like any other: read only, unlit, its tooltip asking for the die, its click using nothing.
        [Test]
        public void AnAbilityWithOneEligibleDieWaitsForTheDie()
        {
            var view = Read("CharacterRosterWire.json");
            view.Dice.Clear();
            view.Dice.Add(new DieView { Id = "muster", Owner = Side.FreePeoples, Face = DieFace.Muster, Source = "standard", Location = DieLocation.Available });
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            var printed = CharacterAbilityLibrary.For(inspector.DefinitionId).First();
            var politics = Politics(printed.Id, false);
            politics.Character.Dice.Add("muster");
            var used = new List<ComponentAction>();
            inspector.Abilities.Use += used.Add;
            inspector.Abilities.Usable(new[] { politics }, true, view);
            var chip = inspector.Root.Q<Button>("character-ability-" + printed.Id);
            Assert.IsFalse(chip.ClassListContains(ChoiceGlow.ClassName), "Nothing glows before the player's choice of die.");
            Assert.IsTrue(ReadOnlyControl.Is(chip), "The click waits for the die.");
            StringAssert.EndsWith("\n" + SkillUse.ChooseDie, SkillTooltip.Plain(chip.tooltip));
            Assert.IsFalse(inspector.Abilities.Choose(chip.name), "The click uses nothing.");
            CollectionAssert.IsEmpty(used);

            var paid = Politics(printed.Id, true);
            paid.DieId = "muster";
            inspector.Abilities.Usable(new[] { paid }, true, view);
            Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName), "With its die chosen the icon glows.");
            StringAssert.EndsWith("\nClick to use your Muster die: Move the Dwarves one step toward war.", SkillTooltip.Plain(chip.tooltip));
            Assert.IsTrue(inspector.Abilities.Choose(chip.name));
            CollectionAssert.AreEqual(new[] { paid }, used);
        }

        // UI-447 (owner): the window shows who it is about — the shown version's portrait at the head of its title bar.
        [Test]
        public void TheCharacterWindowShowsThePortraitOfTheVersionItShows()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            var portrait = inspector.Root.Q(CharacterWindowPortrait.ClassName);
            Assert.IsNotNull(portrait, "The medallion stands in the window.");
            Assert.AreSame(inspector.Root.Q(className: "character-header"), portrait.parent, "It heads the title bar.");
            var gimli = PortraitArtwork.Sprite(inspector.DefinitionId);
            Assert.IsNotNull(gimli, "Gimli has a painted portrait.");
            Assert.AreSame(gimli, inspector.Portrait.Shown, "The painted portrait of the version shown.");
            Assert.AreEqual(PickingMode.Ignore, portrait.pickingMode, "It takes no pointer.");
            Assert.IsTrue(inspector.Apply(view, "base.character.strider"));
            Assert.AreSame(PortraitArtwork.Sprite(inspector.DefinitionId), inspector.Portrait.Shown, "Another character, his portrait.");
            Assert.AreNotSame(gimli, inspector.Portrait.Shown);
        }

        private static ComponentAction Politics(string ability, bool enabled) => new ComponentAction
        {
            Key = "gimli-politics", Kind = CommandKind.UseCharacterAbility, Enabled = enabled, Detail = "Move the Dwarves one step toward war.",
            Character = new CharacterActionOption { CharacterId = "base.character.gimli", AbilityId = ability, Title = "Dwarf of Erebor" }
        };

        // UI-440 (owner): Saruman's Voice stands as its two variants side by side in one group, each badged; the one offered
        // now glows, the other stays dimmed in its slot; a command maps to its printed ability's variant.
        [Test]
        public void AVariantAbilityShowsOneBadgedIconPerVariantSideBySide()
        {
            var chips = new CharacterAbilityChips();
            chips.Apply("base.character.saruman");
            const string voice = "muster-each-isengard-settlement-or-upgrade-two";
            var group = chips.Root.Q("character-ability-group-" + voice);
            Assert.IsTrue(group.ClassListContains(CharacterAbilityChips.VariantsClass));
            CollectionAssert.AreEqual(new[] { "character-ability-" + voice + "-voice-recruit", "character-ability-" + voice + "-voice-upgrade" },
                group.Children().Select(icon => icon.name));
            Assert.AreEqual(Resources.Load<Texture2D>(AbilityBadges.Muster), group[0].Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);
            Assert.AreEqual(Resources.Load<Texture2D>(AbilityBadges.Upgrade), group[1].Q(className: IconButton.BadgeClass).style.backgroundImage.value.texture);
            var recruit = new ComponentAction
            {
                Key = "voice-recruit", Kind = CommandKind.UseCharacterAbility, Enabled = true,
                Character = new CharacterActionOption { CharacterId = "base.character.saruman", AbilityId = "voice-recruit", Title = "Voice" }
            };
            Assert.IsEmpty(chips.Usable(new[] { recruit }, true));
            Assert.IsTrue(group[0].ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(group[1].ClassListContains(CharacterAbilityChips.IdleClass), "The variant not offered now is dimmed in its slot.");
            Assert.IsTrue(ReadOnlyControl.Is(group[1]));
            StringAssert.StartsWith(CharacterAbilityLibrary.Title(voice) + " · Recruit\n", SkillTooltip.Plain(((Button)group[0]).tooltip));
            Assert.AreEqual(voice, CharacterAbilitySlots.PrintedOf(new ComponentAction
            {
                Character = new CharacterActionOption { AbilityId = "voice-upgrade" }
            }, new HashSet<string> { voice }), "Saruman's two Voice commands use his printed ability.");
        }

        // UI-440 (owner, 2026-11-02): on every card each ability's icons (its variants) stand together in one group, in
        // printed order, and the most icons a card shows fit the window's one row (380 points, 12 of padding a side).
        [Test]
        public void EveryCardsVariantsStandTogetherInOneRow()
        {
            const float icon = 36f, group = 10f, row = 356f;
            var widest = 0f;
            foreach (var card in CharacterAbilityLibrary.CardIds)
            {
                var chips = new CharacterAbilityChips();
                chips.Apply(card);
                var groups = chips.Root.Children().ToList();
                var abilities = CharacterAbilityLibrary.For(card);
                CollectionAssert.AreEqual(abilities.Select(ability => "character-ability-group-" + ability.Id), groups.Select(g => g.name), card);
                for (var index = 0; index < abilities.Count; index++)
                {
                    Assert.AreEqual(AbilityVariants.For(abilities[index].Id).Count, groups[index].childCount, card + " · " + abilities[index].Id);
                    Assert.IsTrue(groups[index].Children().All(child => (string)child.userData == abilities[index].Id), card);
                }
                widest = Mathf.Max(widest, chips.Icons.Count() * icon + groups.Count * group);
            }
            Assert.LessOrEqual(widest, row, "The card with the most icons fits one row.");
        }

        // UI-366: a die that pays for a character's political ability lights Politics (not Characters), names the nation,
        // and the nation's click becomes the ability's command with that die and the character's region.
        [Test]
        public void APoliticalAbilityIsOfferedOnItsNationsTrack()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
                Dice = { new DieView { Id = "fp-die-1", Owner = Side.FreePeoples, Face = DieFace.Army, Location = DieLocation.Available } },
                Nations = { new NationView { Id = "dwarves", Active = true, StepsToWar = 2 } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.UseCharacterAbility, Enabled = true,
                        CharacterActions =
                        {
                            new CharacterActionOption
                            {
                                CharacterId = "base.character.gimli", AbilityId = "politics-dwarves-at-erebor", Title = "Advance dwarves",
                                Dice = { "fp-die-1" }, Targets = { "erebor" }
                            }
                        }
                    }
                }
            };
            var die = new ActionDieContext();
            die.Sync(view, "fp-die-1");
            var offers = CharacterPolitics.For(view, die);
            CollectionAssert.AreEquivalent(new[] { "dwarves" }, offers.Keys);
            var options = DieOptions.For(view, new TurnDraft(), die);
            Assert.IsTrue(options.Politics, "The die lights Politics.");
            Assert.IsFalse(options.Characters, "A political ability is used on its track, not from Characters.");
            Assert.AreEqual("Gimli in Erebor · Dwarf of Erebor: click to spend this die to move the Dwarves one step toward war.",
                CharacterPolitics.Hint(view, offers["dwarves"], id => id == "erebor" ? "Erebor" : id));
            var draft = new TurnDraft();
            Assert.IsTrue(CharacterPolitics.Use(draft, view, die, "dwarves"));
            Assert.AreEqual(CommandKind.UseCharacterAbility, draft.Kind);
            Assert.AreEqual("base.character.gimli", draft.Payload.CharacterId);
            Assert.AreEqual("politics-dwarves-at-erebor", draft.Payload.AbilityId);
            Assert.AreEqual("fp-die-1", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { "erebor" }, draft.Payload.Ids);
            Assert.IsFalse(CharacterPolitics.Use(new TurnDraft(), view, die, "elves"), "Another nation is not this ability's.");
        }

        // UI-366: on the expansions line Gimli's Dwarf of Renown moves the Dwarves; a Council Messenger also activates a
        // passive nation, which makes the click use the ability even when the die could step the nation.
        [Test]
        public void TheExpansionsPoliticalAbilitiesActOnTheirTracks()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
                Dice = { new DieView { Id = "fp-die-1", Owner = Side.FreePeoples, Face = DieFace.Muster, Location = DieLocation.Available } },
                Nations = { new NationView { Id = "dwarves", Active = true, StepsToWar = 2 }, new NationView { Id = "north", Active = false, StepsToWar = 2 } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.UseCharacterAbility, Enabled = true,
                        CharacterActions =
                        {
                            new CharacterActionOption
                            {
                                CharacterId = "base.character.gimli", AbilityId = "politics-dwarves-at-fate-homelands", Title = "Dwarf of Renown",
                                Dice = { "fp-die-1" }, Targets = { "iron-hills" }
                            },
                            new CharacterActionOption
                            {
                                CharacterId = "base.character.merry", AbilityId = "messenger-north", Title = "Messenger of Elrond",
                                Dice = { "fp-die-1" }, Targets = { "dale" }
                            }
                        }
                    }
                }
            };
            var die = new ActionDieContext();
            die.Sync(view, "fp-die-1");
            var offers = CharacterPolitics.For(view, die);
            CollectionAssert.AreEquivalent(new[] { "dwarves", "north" }, offers.Keys);
            Assert.IsFalse(CharacterPolitics.Preferred(view, die, "dwarves"), "Renown does not activate.");
            Assert.IsTrue(CharacterPolitics.Preferred(view, die, "north"), "A Messenger activates the passive North.");
            StringAssert.EndsWith("click to spend this die to activate the North and move it one step toward war.",
                CharacterPolitics.Hint(view, offers["north"], id => id));
            StringAssert.EndsWith("click to spend this die to move the Dwarves one step toward war.",
                CharacterPolitics.Hint(view, offers["dwarves"], id => id));
        }

        [Test]
        public void RosterRowCarriesTheLevelAndAStatusSymbol()
        {
            var view = Read("CharacterRosterWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var gimli = panel.Root.Q<Button>("character-roster-base.character.gimli");
            Assert.IsTrue(gimli.Q("character-roster-status").ClassListContains("character-status-on-board"));
            Assert.AreEqual(view.OwnCharacterRoster.Single(r => r.FigureId == "base.character.gimli").Level.ToString(),
                gimli.Q<Label>("character-roster-level").text);
            StringAssert.Contains("Level ", gimli.tooltip);
        }

        [Test]
        public void EnemyPublicInspectionDoesNotReadOwnCandidatesOrCreateForeignControls()
        {
            var view = Read("CharacterRosterShadowWire.json");
            view.Characters.Add(new CharacterView { Id = "public-gimli", DefinitionId = "fate.character.gimli-dwarven-lord",
                Side = Side.FreePeoples, Location = "iron-hills", Level = 1, Leadership = 1 });
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "public-gimli"));
            StringAssert.Contains("Move 2", inspector.Root.Q("character-statistics").tooltip);
            Assert.IsEmpty(inspector.Actions.Children());
            Assert.AreEqual(DisplayStyle.None, inspector.Root.Q<Label>("character-entry").style.display.value);
            Assert.IsFalse(CharacterRosterPresentation.Rows(view).Any(r => r.FigureId == "public-gimli"));
        }

        [Test]
        public void EmptyOlderRosterIsHonestAndRemovedStartsCollapsed()
        {
            var view = new PlayerView();
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.Flex, panel.Root.Q<Label>("characters-empty").style.display.value);
            Assert.IsFalse(panel.Root.Q<Foldout>("characters-removed").value);
            Assert.IsNull(CharacterRosterPresentation.Inspect(view, "absent"));
            Assert.AreEqual(CharacterRosterSection.NotInPlay, CharacterRosterPresentation.Section(new CharacterRosterView()));
        }

        [Test]
        public void ShadowFullRosterShowsEightNamedFiguresAndServerEntryConditions()
        {
            var view = Read("CharacterRosterShadowWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var rows = panel.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("character-roster-")).ToList();
            // UI-438 (owner): the Mouth of Sauron and the Witch-king have one row each, their base and Lords versions inside.
            Assert.AreEqual(8, rows.Count);
            Assert.AreEqual(8, rows.Select(b => b.name).Distinct().Count());
            var future = view.OwnCharacterRoster.Single(r => r.Identity == "black-serpent").FutureVersions.Single();
            StringAssert.Contains(future.Entry.Source, CharacterRosterPresentation.Conditions(future.Entry));
            StringAssert.Contains(future.Entry.Cost, CharacterRosterPresentation.Conditions(future.Entry));
            Assert.IsTrue(future.CouncilEntry.CanAct);
            // UI-420: the inspector reads "Cost: X" as the row does; its tooltip neither repeats that cost nor has a blank line.
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "kings.character.black-serpent"));
            var entry = inspector.Root.Q<Label>("character-entry");
            Assert.AreEqual("Cost: " + future.Entry.Cost, entry.text);
            StringAssert.StartsWith(future.Entry.Title + "\n", entry.tooltip);
            StringAssert.DoesNotContain("\n\n", entry.tooltip);
            StringAssert.DoesNotContain("Cost: " + future.Entry.Cost + "\n", entry.tooltip);
            StringAssert.Contains("\nOr with a Council token · Cost: " + future.CouncilEntry.Cost, entry.tooltip);
        }

        // UI-420: a character action's button says what its click chooses and whose ability it is, without a title line.
        [Test]
        public void ACharacterActionButtonNamesItsChoiceAndCharacter()
        {
            const string gimli = "base.character.gimli";
            var ability = new CharacterActionOption { CharacterId = gimli, Title = "Dwarf of Erebor", Detail = "Move the Dwarves one step toward war." };
            Assert.AreEqual("Click to choose Dwarf of Erebor · " + CharacterLibrary.Name(gimli) + "\nMove the Dwarves one step toward war.",
                CharacterChoices.Tooltip(ability));
            var entry = new CharacterActionOption { CharacterId = gimli, Title = CharacterLibrary.Name(gimli) };
            Assert.AreEqual("Click to choose " + CharacterLibrary.Name(gimli), CharacterChoices.Tooltip(entry));
        }

        [Test]
        public void EveryPublicCharacterAbilityHasReadableTextAndItsReviewedSource()
        {
            foreach (var catalog in new[] { "base-characters", "lords-characters", "kings-characters", "fate-characters" })
            {
                var cards = JsonConvert.DeserializeObject<CharacterText[]>(Resources.Load<TextAsset>(catalog).text);
                foreach (var card in cards)
                {
                    var descriptions = CharacterAbilityLibrary.For(card.Id);
                    CollectionAssert.AreEquivalent(card.Abilities, descriptions.Select(a => a.Id), card.Id);
                    foreach (var ability in descriptions)
                    {
                        Assert.Greater(ability.Text.Length, 30, card.Id + " / " + ability.Id);
                        Assert.IsNotEmpty(ability.Title);
                        Assert.IsNotEmpty(ability.Source);
                        Assert.IsFalse(ability.Source.Contains(".json"), "Repository paths do not belong in the player's tooltip.");
                    }
                }
            }
            Assert.IsEmpty(CharacterAbilityLibrary.For("unprovided-version"));
        }

        [Test]
        public void PublicAbilityInspectionExplainsParticipationAndTheFiveDiceLimit()
        {
            var view = Read("CharacterRosterShadowWire.json");
            view.Characters.Add(new CharacterView { Id = "public-gimli", DefinitionId = "base.character.gimli",
                Side = Side.FreePeoples, Location = "iron-hills", Level = 2, Leadership = 1 });
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "public-gimli"));
            var ability = inspector.Root.Q<Button>("character-ability-combat-strength-plus-one");
            // UI-440: the icon's formatted tooltip names the ability and explains participation and the limit.
            var text = SkillTooltip.Plain(ability.tooltip).ToLowerInvariant();
            StringAssert.StartsWith("captain of the west\n", text);
            StringAssert.Contains("participat", text);
            StringAssert.Contains("5 combat dice", text);
            Assert.IsTrue(ability.focusable);
        }

        private static PlayerView Read(string file) => JsonConvert.DeserializeObject<PlayerView>(
            File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor", file)));
    }
}
