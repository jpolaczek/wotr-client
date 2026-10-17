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
        public void CorruptionSelectionIsPrivateLocalAndNeedsOneExplicitTile()
        {
            var view = new PlayerView { CorruptionAttempt = new CorruptionAttemptView
            {
                DrawLimit = 2, DrawnCount = 2, OwnChoices = new List<CorruptionTileView>
                {
                    new CorruptionTileView { Id = "one", TileId = "one", Value = 1 },
                    new CorruptionTileView { Id = "two", TileId = "two", Value = 2, Reveal = true }
                }
            } };
            var option = new ActionOption { Kind = CommandKind.ChooseCorruptionTile, Minimum = 1, Maximum = 1,
                EligibleIds = new List<string> { "one", "two" } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCorruptionTile);
            var root = new VisualElement();
            var refresh = CorruptionAttemptChoices.Render(root, view, option, draft, edit => edit());
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "two");
            refresh();
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("2 ◉", root.Q<Button>("corruption-choice-two").text);
            Assert.IsTrue(root.Q<Button>("corruption-choice-two").ClassListContains("selected"));
            Assert.AreEqual(2, root.Query<Button>().ToList().Count);
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
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
    }
}
