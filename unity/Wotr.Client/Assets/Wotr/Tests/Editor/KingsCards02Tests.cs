using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsCards02Tests
    {
        private static ActionOption Villages()
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 4, Maximum = 4,
                EventRecruitment = new EventRecruitmentOption { DistinctNations = 4 } };
            foreach (var nation in new[] { "gondor", "rohan", "elves", "north", "dwarves" })
            {
                var region = nation + "-settlement";
                option.Targets.Add(region);
                option.EventRecruitment.Limits.Add(new RecruitmentLimit { Nation = nation, RegionId = region,
                    Maximum = 1, Kinds = new List<UnitKind> { UnitKind.Regular, UnitKind.Leader } });
                foreach (var kind in new[] { UnitKind.Regular, UnitKind.Leader })
                {
                    var id = nation + kind;
                    option.EligibleIds.Add(id);
                    option.Recruitment.Add(new RecruitmentOption { Nation = nation, Kind = kind,
                        ReserveIds = new List<string> { id }, Targets = new List<string> { region } });
                }
            }
            return option;
        }

        [Test]
        public void VillagesRequiresFourDifferentNationsAndUndoReopensOnlyThatNation()
        {
            var option = Villages();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            foreach (var group in option.Recruitment.Where(g => g.Kind == UnitKind.Regular).Take(4))
            {
                Assert.IsTrue(EventRecruitDraft.Select(draft, option, group.ReserveIds[0]));
                Assert.IsTrue(EventRecruitDraft.Region(draft, option, group.Targets[0]));
                var other = option.Recruitment.Single(g => g.Nation == group.Nation && g.Kind == UnitKind.Leader);
                Assert.IsFalse(EventRecruitDraft.Select(draft, option, other.ReserveIds[0]));
            }
            Assert.IsTrue(draft.Ready(option));
            EventRecruitDraft.Remove(draft, "gondorRegular");
            Assert.IsFalse(draft.Ready(option));
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "gondorLeader"));
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "gondor-settlement"));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsFalse(EventRecruitDraft.Select(draft, option, "dwarvesRegular"));
        }

        // UI-131: the card's figures appear in Reinforcements, one row per nation, without scrolling or typing.
        [Test]
        public void ReinforcementsGroupTenFiguresUnderFiveNationRows()
        {
            var option = Villages();
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.EffectChoice },
                Options = new List<ActionOption> { option },
                Units = option.Recruitment.Select(g => new UnitView { Id = g.ReserveIds[0], Nation = g.Nation, Kind = g.Kind,
                    Side = Side.FreePeoples, Location = "reserve" }).ToList()
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var plan = ReinforcementPlans.For(view, draft, BoardPresentation.Load());
            Assert.AreEqual(5, plan.Rows.Count);
            Assert.AreEqual(4, plan.Slots);
            var panel = new ReinforcementsPanel();
            panel.Apply(ReinforcementsState.For(view, plan, "", true));
            var root = panel.Root;
            Assert.AreEqual(5, root.Query<VisualElement>(className: "reinforcement-row").ToList().Count);
            Assert.AreEqual(10, root.Query<Button>().ToList().Count(b => b.name.StartsWith("event-reserve-")));
            Assert.AreEqual(4, root.Q("reinforcement-budget").childCount);
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
            Assert.IsEmpty(root.Query<TextField>().ToList());
        }

        [Test]
        public void WelcomeRequiresEligibleDieAndVainAllowsZeroWithAccuratePreview()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardWelcome);
            var option = new ActionOption { Kind = CommandKind.DiscardWelcome, EligibleDice = new List<string> { "will" } };
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.DieId = "will");
            Assert.IsTrue(draft.Ready(option));
            draft.Choose(CommandKind.ChooseEffect);
            option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 0, Maximum = 5,
                EligibleIds = new List<string> { "nazgul" } };
            Assert.IsTrue(draft.Ready(option));
            var view = new PlayerView { Decision = new DecisionView { SourceId = "kings.sh.character.26" }, Options = new List<ActionOption> { option } };
            StringAssert.Contains("Reduce opposing Leadership by 0", NazgulLeadershipSummary.Describe(draft, view));
        }

        [Test]
        public void SixCardsHaveBothHalvesAndTheirSourceInitiatives()
        {
            var ids = new[] { "fp.strategy.22", "fp.strategy.24", "fp.strategy.25", "fp.strategy.26", "sh.character.25", "sh.character.26" };
            var initiatives = new[] { 3, 3, 4, 4, 3, 1 };
            for (var i = 0; i < ids.Length; i++)
            {
                var card = CardLibrary.Cards["kings." + ids[i]];
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
                Assert.AreEqual(initiatives[i], card.Initiative.Single());
            }
        }

        // UI-348: a corruption attempt's tiles and the inspection of a king's tiles are made on the kings' portraits,
        // attempt buttons and tiles (UI-274), so the tray keeps no fallback for them.
        [Test]
        public void CorruptionChoicesAreHostedByTheKingsObjects()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "tile", Kind = DecisionKind.CorruptionTile },
                CorruptionAttempt = new CorruptionAttemptView { SovereignId = "kings.sovereign.theoden" },
                Options = { new ActionOption { Kind = CommandKind.ChooseCorruptionTile, Enabled = true, EligibleIds = { "one" } } }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCorruptionTile);
            Assert.IsTrue(SovereignPresentation.OwnsDecision(view, draft), "The attempt's tiles stand beside the king.");
            view.CorruptionAttempt = null;
            view.Decision = new DecisionView { Id = "inspect", Kind = DecisionKind.EffectChoice, Presentation = SovereignChoices.Inspection };
            view.Options[0] = new ActionOption
            {
                Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { "tile" },
                EffectChoices = { new EffectChoiceView { Id = "tile", TargetId = "kings.sovereign.theoden" } }
            };
            Assert.IsTrue(SovereignPresentation.OwnsDecision(view, new TurnDraft()), "The inspected tiles stand beside the king.");
        }

        // UI-348 (EXP-15 D17): Brand's Heir of Bard is a gilt chip (UI-439: its own icon) beside the own card, its tooltip
        // naming the Shadow card it cancels; Keep card effects is the console's Pass. A Chieftain's substitution names
        // his effect instead.
        [Test]
        public void BrandsReplacementIsAChipAndKeepingIsThePass()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "brand", Kind = DecisionKind.EffectChoice, Presentation = CombatReplacementChoice.Presentation },
                Battle = new BattleView
                {
                    Attacker = new BattleForceView { Side = Side.Shadow, CardId = "" },
                    Defender = new BattleForceView { Side = Side.FreePeoples, CardId = "" }
                },
                Sovereigns = { new SovereignView { Id = "kings.sovereign.brand", Title = "Brand", Nation = "north", Status = SovereignStatus.Awakened,
                    DefinitionId = "kings.character.brand.awakened" } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { "replace", "keep" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "replace", SourceId = "kings.sovereign.brand", Title = "Use Brand" },
                            new EffectChoiceView { Id = "keep", Title = "Keep card effects" }
                        }
                    }
                }
            };
            var chip = CombatReplacementChoice.Chips(view).Single();
            Assert.AreEqual("replace", chip.Id);
            Assert.IsTrue(CombatReplacementChoice.OnChip(view, "replace"));
            Assert.IsFalse(CombatReplacementChoice.OnChip(view, "keep"));
            // UI-439: the chip shows Heir of Bard's own icon, where the North's arms stood in before.
            Assert.AreEqual(AbilityArt.Folder + "heir-of-bard", CombatReplacementChoice.Icon(view, chip.SourceId));
            // UI-420 (owner): without cards on the table the chip names neither as "no card".
            StringAssert.StartsWith("Click to use Heir of Bard · Cancels the Shadow Combat card at initiative 0\n", CombatReplacementChoice.Tooltip(view, chip));
            // UI-397 (owner): the chip ends with the ability's printed text. UI-420 (owner): it names the ability once.
            StringAssert.EndsWith("\nOnce per battle, when Brand and a North Army unit take part, you may replace your Combat " +
                "card’s effect after the reveal: cancel the Shadow Combat card at initiative 0.", CombatReplacementChoice.Tooltip(view, chip));
            var tooltip = CombatReplacementChoice.Tooltip(view, chip);
            Assert.AreEqual(tooltip.IndexOf("Heir of Bard", System.StringComparison.Ordinal),
                tooltip.LastIndexOf("Heir of Bard", System.StringComparison.Ordinal), "The ability is named once.");
            StringAssert.DoesNotContain("no card", tooltip);
            // UI-396 (owner): the battle window names the chip and its skip.
            Assert.IsTrue(CombatReplacementChoice.Skippable(view));
            Assert.AreEqual("Click Brand's Heir of Bard beside your Combat card's place to use the ability, or Skip effect to fight on as usual.",
                CombatReplacementChoice.Hint(view));
            var pass = OptionalDecline.Option(view, new TurnDraft());
            Assert.AreEqual("keep", pass.Id);
            Assert.AreEqual("Pass · Skip Heir of Bard · Keep your card's effects", pass.Tooltip);
            Assert.AreEqual("Skip Heir of Bard · Keep your card's effects", OptionalDecline.SkipTooltip(view, new TurnDraft()));

            var root = new VisualElement();
            var draft = new TurnDraft();
            var sent = 0;
            var context = new BattleContext(root, view.Battle, view, draft, edit => edit(), _ => { }, complete: action =>
            {
                action();
                sent++;
            });
            context.Refresh();
            var button = root.Q("battle-defender").Q<Button>("battle-replacement-replace");
            Assert.IsNotNull(button, "The chip stands in the own column.");
            Assert.IsNull(root.Q("battle-attacker").Q<Button>("battle-replacement-replace"));
            Assert.IsTrue(button.ClassListContains(ChoiceGlow.ClassName));
            Assert.AreEqual(Texture("heir-of-bard"), Icon(button));
            // UI-440 (owner): the replacement is Heir of Bard's bare skill icon, no gilt chip.
            SkillIconChecks.Bare(button, AbilityArt.Folder + "heir-of-bard");

            view.Decision.Presentation = "Region";
            Assert.IsNull(OptionalDecline.Option(view, new TurnDraft()), "A generic keep counts only in its own step.");

            // UI-277: once made, the replacement is a gilt marker beside the replaced card, for both players.
            view.Decision = null;
            view.Options.Clear();
            view.CombatReplacements.Add(new CombatReplacementView { Side = Side.FreePeoples, SourceId = "kings.sovereign.brand" });
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                view.You = side;
                root = new VisualElement();
                new BattleContext(root, view.Battle, view, new TurnDraft(), edit => edit(), _ => { }, complete: action => action()).Refresh();
                var marker = root.Q("battle-defender").Q("battle-replacement-applied");
                Assert.IsNotNull(marker, side + " sees the replacement beside the Free Peoples' card.");
                Assert.AreEqual(Texture("heir-of-bard"), Icon(marker), "The marker shows the ability too.");
                // UI-440: the same bare icon, marked as the chosen one.
                Assert.IsTrue(marker.ClassListContains(SkillIcon.ClassName) && marker.ClassListContains(SkillIcon.SelectedClass));
                // UI-397 (owner): the marker names the ability and gives its printed text.
                StringAssert.StartsWith("Brand · Heir of Bard\nOnce per battle", marker.tooltip);
                StringAssert.EndsWith("\nThis round it replaces the Combat card's effects.", marker.tooltip);
                Assert.IsNull(root.Q("battle-attacker").Q("battle-replacement-applied"));
            }
        }

        // UI-277 (D17): a Dark Chieftain's substitution names his effect instead of the card's; UI-439: its chip shows his
        // ability's own icon (his nation's arms before).
        [Test]
        public void AChieftainsSubstitutionIsAChipWithHisAbility()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "chieftain", Kind = DecisionKind.EffectChoice, Presentation = CombatReplacementChoice.Presentation },
                Battle = new BattleView
                {
                    Attacker = new BattleForceView { Side = Side.FreePeoples },
                    Defender = new BattleForceView { Side = Side.Shadow, CardId = "kings.sh.strategy.01" }
                },
                Characters = { new CharacterView { Id = "kings.character.black-serpent", DefinitionId = "kings.character.black-serpent", Side = Side.Shadow } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1,
                        EligibleIds = { "kings.character.black-serpent", "keep" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "kings.character.black-serpent", SourceId = "kings.character.black-serpent",
                                Title = "The Black Serpent", Detail = "Take one hit before reveal. Combat +1 at initiative 3." },
                            new EffectChoiceView { Id = "keep", Title = "Keep card effects" }
                        }
                    }
                }
            };
            var chip = CombatReplacementChoice.Chips(view).Single();
            Assert.AreEqual(AbilityArt.Folder + "red-wrath", CombatReplacementChoice.Icon(view, chip.SourceId));
            StringAssert.StartsWith("Click to use The Black Serpent instead of ", CombatReplacementChoice.Tooltip(view, chip));
            StringAssert.Contains("Take one hit before reveal", CombatReplacementChoice.Tooltip(view, chip));
            // UI-397 (owner): the Black Serpent's chip and marker name Red Wrath and give its printed text.
            StringAssert.Contains("\nRed Wrath: When the Black Serpent is in a battle", CombatReplacementChoice.Tooltip(view, chip));
            var applied = new CombatReplacementView { Side = Side.Shadow, SourceId = "kings.character.black-serpent" };
            StringAssert.Contains(" · Red Wrath\nWhen the Black Serpent is in a battle, before the reveal", CombatReplacementChoice.AppliedTooltip(view, applied));
            Assert.AreEqual("keep", OptionalDecline.Option(view, new TurnDraft()).Id);
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), edit => edit(), _ => { }, complete: action => action()).Refresh();
            var serpentChip = root.Q("battle-defender").Q<Button>("battle-replacement-kings.character.black-serpent");
            Assert.IsNotNull(serpentChip);
            Assert.AreEqual(Texture("red-wrath"), Icon(serpentChip), "UI-439: Red Wrath's own icon.");
        }

        private static UnityEngine.Texture2D Texture(string ability) =>
            UnityEngine.Resources.Load<UnityEngine.Texture2D>(AbilityArt.Folder + ability);

        private static UnityEngine.Texture2D Icon(VisualElement chip) =>
            SkillIcon.Glyph(chip).style.backgroundImage.value.texture;

        // UI-423 (owner's playtest pattern, like UI-416): with two Chieftains in one battle the footer, the skip and Pass name
        // both chips, not only the first.
        [Test]
        public void TwoChieftainsChipsAreBothNamed()
        {
            const string serpent = "kings.character.black-serpent";
            const string bats = "kings.character.shadow-of-mirkwood";
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "chieftains", Kind = DecisionKind.EffectChoice, Presentation = CombatReplacementChoice.Presentation },
                Battle = new BattleView
                {
                    Attacker = new BattleForceView { Side = Side.FreePeoples },
                    Defender = new BattleForceView { Side = Side.Shadow, CardId = "" }
                },
                Characters =
                {
                    new CharacterView { Id = serpent, DefinitionId = serpent, Side = Side.Shadow },
                    new CharacterView { Id = bats, DefinitionId = bats, Side = Side.Shadow }
                },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { serpent, bats, "keep" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = serpent, SourceId = serpent, Title = "The Black Serpent" },
                            new EffectChoiceView { Id = bats, SourceId = bats, Title = "The Shadow of Mirkwood" },
                            new EffectChoiceView { Id = "keep", Title = "Keep card effects" }
                        }
                    }
                }
            };
            Assert.AreEqual(2, CombatReplacementChoice.Chips(view).Count);
            Assert.AreEqual("Click The Black Serpent's Red Wrath or The Shadow of Mirkwood's Lord of the Bats beside your Combat card's " +
                "place to use an ability, or Skip effect to fight on as usual.", CombatReplacementChoice.Hint(view));
            Assert.AreEqual("Red Wrath or Lord of the Bats", CombatReplacementChoice.AbilityNames(view));
            Assert.AreEqual("Skip Red Wrath or Lord of the Bats · Keep your card's effects", OptionalDecline.SkipTooltip(view, new TurnDraft()));
            Assert.AreEqual("Pass · Skip Red Wrath or Lord of the Bats · Keep your card's effects", OptionalDecline.Option(view, new TurnDraft()).Tooltip);
        }

        // UI-348 (D7): Palantír of Anárion's Skip extra card is the console's Pass; the decks are the draw.
        [Test]
        public void AnarionsSkipIsThePass()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "anarion", Kind = DecisionKind.EffectChoice, Presentation = "Deck" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { "character", "decline" },
                        EffectChoices = { new EffectChoiceView { Id = "character" }, new EffectChoiceView { Id = "decline", Title = "Skip extra card" } }
                    }
                }
            };
            Assert.AreEqual("decline", OptionalDecline.Option(view, new TurnDraft()).Id);
            Assert.AreEqual("Pass · Complete the action without an extra card", OptionalDecline.Option(view, new TurnDraft()).Tooltip);
            Assert.IsFalse(OptionalDecline.Declines(view, "character"));
        }

        // UI-348: every Kings ability shown as a picture has one of its own, not the generic scroll; UI-439: the printed
        // ability's own icon, the command's prefix dropped.
        [Test]
        public void KingsAbilitiesHaveTheirPictures()
        {
            foreach (var ability in new[] { "mustering-of-the-mark", "dwarvenking", "palantir-of-anarion", "far-sighted", "call-to-arms",
                         "iron-guard", "stewardship", "elvenking" })
                Assert.AreEqual(AbilityArt.Folder + ability, AbilityArt.For("kings." + ability), ability);
        }
    }
}
