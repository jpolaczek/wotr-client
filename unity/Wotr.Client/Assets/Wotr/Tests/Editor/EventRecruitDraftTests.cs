using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class EventRecruitDraftTests
    {
        [Test]
        public void RegionFirstAndFigureFirstProduceOneMixedPlanWithUndo()
        {
            var view = Misty();
            var option = view.Options[0];
            var draft = Draft();
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "moria"));
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "regular-0"));
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "elite-0"));
            Assert.IsFalse(draft.Ready(option));
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "nazgul-0", "mount-gram"));
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "nazgul-0"));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.Ids);
            EventRecruitDraft.Remove(draft, "elite-0");
            Assert.IsFalse(draft.Ready(option));
            draft.Back();
            Assert.IsTrue(draft.Ready(option));
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(option));
            var first = Draft();
            Assert.IsTrue(EventRecruitDraft.Select(first, option, "elite-0"));
            Assert.IsEmpty(first.Payload.Placements);
            Assert.IsTrue(EventRecruitDraft.Region(first, option, "mount-gram"));
            Assert.AreEqual("mount-gram", first.Payload.Placements.Single().RegionId);
        }

        [Test]
        public void RegionAndSharedKindLimitsPreventIllegalPlanWithoutChoosingComposition()
        {
            var option = Misty().Options[0];
            var draft = Draft();
            EventRecruitDraft.Drop(draft, option, "regular-0", "moria");
            EventRecruitDraft.Drop(draft, option, "elite-0", "moria");
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "regular-1", "moria"));
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "regular-0", "moria"));
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "nazgul-0", "mount-gram"));
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "forged", "moria"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "nazgul-0", "moria"));
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void PitsFifthFigureRequiresThirdRegionAndNeverAllowsFourth()
        {
            var option = Pits();
            var draft = Draft();
            EventRecruitDraft.Drop(draft, option, "regular-0", "moria");
            EventRecruitDraft.Drop(draft, option, "regular-1", "moria");
            EventRecruitDraft.Drop(draft, option, "regular-2", "morannon");
            EventRecruitDraft.Drop(draft, option, "regular-3", "barad-dur");
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "regular-4", "mount-gundabad"));
            Assert.IsFalse(draft.Ready(option));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "regular-4", "morannon"));
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEquivalent(new[] { "moria", "morannon", "barad-dur" }, draft.Payload.Placements.Select(p => p.RegionId).Distinct());
        }

        [Test]
        public void SharedShortageDoesNotAllowAPlacementThatPreventsMandatoryMaximum()
        {
            var option = Misty().Options[0];
            option.Minimum = option.Maximum = 4;
            option.EventRecruitment.MaximumRegions = 0;
            option.Targets = new List<string> { "north-dunland", "south-dunland", "orthanc" };
            option.Recruitment.RemoveAll(group => group.Kind == UnitKind.Nazgul);
            foreach (var group in option.Recruitment)
                group.Targets = group.Kind == UnitKind.Elite ? new List<string> { "orthanc" } : option.Targets;
            option.EventRecruitment.Limits = option.Targets.Select(region => new RecruitmentLimit
            {
                Nation = "sauron",
                RegionId = region,
                Maximum = 2,
                Kinds = region == "orthanc" ? new List<UnitKind> { UnitKind.Regular, UnitKind.Elite } : new List<UnitKind> { UnitKind.Regular }
            }).ToList();
            var draft = Draft();
            Assert.IsFalse(EventRecruitDraft.Drop(draft, option, "regular-0", "orthanc"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "elite-0", "orthanc"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "regular-0", "south-dunland"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "elite-1", "orthanc"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "regular-1", "north-dunland"));
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void ReconcileDropsUnavailableFiguresAndGesturesUseTheSamePlacementValidation()
        {
            var view = Misty();
            var option = view.Options[0];
            var draft = Draft();
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Reserve, "elite-0"), new DraftToken(DraftObject.Region, "moria")));
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Reserve, "regular-0"), new DraftToken(DraftObject.Region, "mount-gram")));
            option.Recruitment.Single(g => g.Kind == UnitKind.Elite).ReserveIds.Remove("elite-0");
            draft.Reconcile(view);
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsFalse(draft.Ready(option));
        }

        // UI-213 CC04: one armed token fills three settlements, ↶ removes the last placement and re-arms the token.
        [Test]
        public void OneArmedTokenFillsEverySettlementTheCardAllowsAndUndoRestoresIt()
        {
            var option = Settlements();
            var draft = Draft();
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "regular-0"));
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "dale"));
            Assert.AreEqual("regular-1", draft.ReserveId, "The token stays armed with its next figure.");
            Assert.IsFalse(EventRecruitDraft.Targets(draft, option).Contains("dale"), "One figure per settlement.");
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "erebor"));
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "iron-hills"));
            Assert.AreEqual("", draft.ReserveId, "Every figure of the card is planned: the token is released.");
            Assert.IsTrue(draft.Ready(option));
            draft.Back();
            Assert.AreEqual(2, draft.Payload.Placements.Count);
            Assert.AreEqual("regular-2", draft.ReserveId, "↶ removes the last placement and keeps the token armed.");
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "iron-hills"));
            Assert.IsTrue(draft.Ready(option));
        }

        // UI-213: an armed token is released at a limit, never placed beyond it; a click on another token switches.
        [Test]
        public void TheArmedTokenStopsAtTheCardsLimitsAndAnotherTokenSwitchesIt()
        {
            var option = Misty().Options[0];
            var draft = Draft();
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "regular-0"));
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "moria"));
            Assert.AreEqual("regular-1", draft.ReserveId);
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "elite-0"));
            Assert.AreEqual("elite-0", draft.ReserveId, "Another token switches instead of filling the last region.");
            Assert.AreEqual(1, draft.Payload.Placements.Count);
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "elite-0"));
            Assert.AreEqual("", draft.ReserveId, "A click on the armed token releases it.");
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "regular-1"), "Region first: with nothing armed the token fills Moria.");
            Assert.AreEqual(2, draft.Payload.Placements.Count);
            Assert.AreEqual("", draft.ReserveId, "Moria's limit for Regulars and Elites is reached.");
            Assert.IsFalse(EventRecruitDraft.Select(draft, option, "elite-0"), "No region is left within the limits.");
            Assert.IsFalse(EventRecruitDraft.Region(draft, option, "mount-gram"), "The card allows one region.");
            Assert.IsTrue(EventRecruitDraft.Select(draft, option, "nazgul-0"));
            Assert.IsTrue(draft.Ready(option));
        }

        private static ActionOption Settlements()
        {
            var regions = new List<string> { "dale", "erebor", "iron-hills" };
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 3,
                Maximum = 3,
                Targets = regions,
                EventRecruitment = new EventRecruitmentOption()
            };
            option.Recruitment.Add(new RecruitmentOption
            {
                Nation = "north",
                Kind = UnitKind.Regular,
                Targets = regions,
                ReserveIds = new List<string> { "regular-0", "regular-1", "regular-2", "regular-3" }
            });
            foreach (var region in regions)
                option.EventRecruitment.Limits.Add(new RecruitmentLimit { Nation = "north", RegionId = region, Kinds = new List<UnitKind> { UnitKind.Regular }, Maximum = 1 });
            option.EligibleIds = option.Recruitment.SelectMany(g => g.ReserveIds).ToList();
            return option;
        }

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }

        private static PlayerView Misty()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 3,
                Maximum = 3,
                Targets = new List<string> { "moria", "mount-gram" },
                EventRecruitment = new EventRecruitmentOption { MaximumRegions = 1 }
            };
            foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite, UnitKind.Nazgul })
                option.Recruitment.Add(new RecruitmentOption
                {
                    Nation = "sauron",
                    Kind = kind,
                    Targets = option.Targets,
                    ReserveIds = Enumerable.Range(0, kind == UnitKind.Nazgul ? 1 : 2).Select(i => kind.ToString().ToLowerInvariant() + "-" + i).ToList()
                });
            foreach (var region in option.Targets)
            {
                option.EventRecruitment.Limits.Add(new RecruitmentLimit { Nation = "sauron", RegionId = region, Kinds = new List<UnitKind> { UnitKind.Regular, UnitKind.Elite }, Maximum = 2 });
                option.EventRecruitment.Limits.Add(new RecruitmentLimit { Nation = "sauron", RegionId = region, Kinds = new List<UnitKind> { UnitKind.Nazgul }, Maximum = 1 });
            }
            option.EligibleIds = option.Recruitment.SelectMany(g => g.ReserveIds).ToList();
            return new PlayerView { Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "RecruitmentPlan" }, Options = new List<ActionOption> { option } };
        }

        private static ActionOption Pits()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 5,
                Maximum = 5,
                Targets = new List<string> { "moria", "morannon", "barad-dur", "mount-gundabad" },
                EventRecruitment = new EventRecruitmentOption { MaximumRegions = 3 }
            };
            option.Recruitment.Add(new RecruitmentOption
            {
                Nation = "sauron",
                Kind = UnitKind.Regular,
                Targets = option.Targets,
                ReserveIds = Enumerable.Range(0, 5).Select(i => "regular-" + i).ToList()
            });
            option.EventRecruitment.Limits = option.Targets.Select(region => new RecruitmentLimit
            { Nation = "sauron", RegionId = region, Kinds = new List<UnitKind> { UnitKind.Regular }, Maximum = 2 }).ToList();
            option.EligibleIds = option.Recruitment[0].ReserveIds;
            return option;
        }
    }
}
