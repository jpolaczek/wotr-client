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

        // UI-348 (EXP-15 D17): Brand's Heir of Bard is a gilt chip with the North's arms beside the own card, its tooltip
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
                Sovereigns = { new SovereignView { Id = "kings.sovereign.brand", Nation = "north", Status = SovereignStatus.Awakened } },
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
            Assert.AreEqual("north", CombatReplacementChoice.Nation(view, chip));
            StringAssert.StartsWith("Click to use Heir of Bard · Cancels no card at initiative 0", CombatReplacementChoice.Tooltip(view, chip));
            var pass = OptionalDecline.Option(view, new TurnDraft());
            Assert.AreEqual("keep", pass.Id);
            Assert.AreEqual("Pass · Keep your card's effects", pass.Tooltip);

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
            Assert.IsTrue(button.Q("battle-replacement-arms").ClassListContains("nation-emblem-north"));

            view.Decision.Presentation = "Region";
            Assert.IsNull(OptionalDecline.Option(view, new TurnDraft()), "A generic keep counts only in its own step.");
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

        // UI-348: every Kings ability shown as a picture has one of its own, not the generic scroll.
        [Test]
        public void KingsAbilitiesHaveTheirPictures()
        {
            Assert.AreEqual(AbilityPictures.Muster, AbilityPictures.For(CommandKind.UseCharacterAbility, "kings.mustering-of-the-mark"));
            Assert.AreEqual(AbilityPictures.Upgrade, AbilityPictures.For(CommandKind.UseCharacterAbility, "kings.dwarvenking"));
            Assert.AreEqual(AbilityPictures.Draw, AbilityPictures.For(CommandKind.UseCharacterAbility, "kings.palantir-of-anarion"));
            Assert.AreEqual(AbilityPictures.Move, AbilityPictures.For(CommandKind.UseCharacterAbility, "kings.far-sighted"));
            foreach (var ability in new[] { "kings.call-to-arms", "kings.iron-guard", "kings.stewardship", "kings.elvenking" })
                Assert.AreNotEqual(AbilityPictures.Ability, AbilityPictures.For(CommandKind.UseCharacterAbility, ability), ability);
        }
    }
}
