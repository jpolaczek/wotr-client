using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-131: card and ability recruitment as plans of the Reinforcements panel; the draft rules stay the sources'.
    public sealed class ReinforcementPlansTests
    {
        private const string Card = "kings.fp.strategy.22";
        private static readonly BoardPresentation Data = BoardPresentation.Load();

        [Test]
        public void ACardPlanShowsItsLimitsAndClosesTokensOnceEveryFigureIsPlanned()
        {
            var view = EventView(out var option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual(CardLibrary.Title(Card), plan.Title);
            Assert.AreEqual("event", plan.Prefix);
            Assert.AreEqual(2, plan.Slots);
            var gondor = plan.Rows.Single();
            StringAssert.Contains("Minas Tirith 2", gondor.Status);
            Assert.IsTrue(gondor.Figures.All(f => f.ReserveId != null));
            var regular = gondor.Figures.Single(f => f.Kind == UnitKind.Regular);

            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, regular.ReserveId));
            Assert.AreEqual(regular.ReserveId, draft.ReserveId, "A token waits for its region.");
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "dol-amroth"));
            var elite = ReinforcementPlans.For(view, draft, Data).Rows[0].Figures.Single(f => f.Kind == UnitKind.Elite);
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, elite.ReserveId));
            Assert.IsTrue(EventRecruitDraft.Region(draft, option, "minas-tirith"));

            plan = ReinforcementPlans.For(view, draft, Data);
            Assert.IsTrue(plan.Ready);
            CollectionAssert.AreEqual(new[] { "Dol Amroth", "Minas Tirith" }, plan.Placements.Select(p => p.RegionName));
            Assert.AreEqual(ReinforcementRows.InReserve(view, "gondor", UnitKind.Regular) - 1,
                plan.Rows[0].Figures.Single(f => f.Kind == UnitKind.Regular).Reserve, "A planned figure leaves the count.");
            Assert.IsFalse(plan.Rows[0].CanRecruit);
            Assert.AreEqual("All reinforcements of this card are planned", plan.Rows[0].Reason);
        }

        [Test]
        public void TheMapGhostsCrossReturnsARegionsFiguresInOneUndoStep()
        {
            var view = EventView(out var option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var ids = option.Recruitment.Single(g => g.Kind == UnitKind.Regular).ReserveIds;
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, ids[0], "minas-tirith"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, ids[1], "minas-tirith"));
            Assert.IsTrue(ReinforcementPlans.RemoveRegion(draft, view, Data, "minas-tirith"));
            Assert.IsEmpty(draft.Payload.Placements);
            draft.Back();
            Assert.AreEqual(2, draft.Payload.Placements.Count, "One undo brings both figures back.");
            Assert.IsFalse(ReinforcementPlans.RemoveRegion(draft, view, Data, "dol-amroth"));
        }

        [Test]
        public void ReserveFiguresArePlannedAtOnceInTheirRegionWithinTheCardsGroups()
        {
            var view = EffectView();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual("effect", plan.Prefix);
            CollectionAssert.AreEqual(new[] { "dwarves", "north" }, plan.Rows.Select(r => r.Nation));
            StringAssert.Contains("The Shire", plan.Rows.Single(r => r.Nation == "north").Status);
            var north = plan.Rows.Single(r => r.Nation == "north").Figures.Single(f => f.Kind == UnitKind.Regular);

            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, north.ReserveId));
            plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual("the-shire", plan.Placements.Single().RegionId, "No region click is needed.");
            Assert.IsFalse(plan.Ready);
            Assert.IsNull(plan.Rows.Single(r => r.Nation == "north").Figures.Single(f => f.Kind == UnitKind.Regular).ReserveId,
                "The planned figure was the only Regular offered.");

            // The North's group takes one figure: its Elite replaces the planned Regular, as in the card's choice.
            var elite = plan.Rows.Single(r => r.Nation == "north").Figures.Single(f => f.Kind == UnitKind.Elite).ReserveId;
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, elite));
            Assert.AreEqual(UnitKind.Elite, ReinforcementPlans.For(view, draft, Data).Placements.Single().Kind);
            var dwarves = plan.Rows.Single(r => r.Nation == "dwarves").Figures.Single(f => f.Kind == UnitKind.Regular).ReserveId;
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, dwarves));
            Assert.IsTrue(ReinforcementPlans.For(view, draft, Data).Ready);

            Assert.IsTrue(ReinforcementPlans.Remove(draft, view, elite));
            CollectionAssert.AreEqual(new[] { dwarves }, draft.Payload.Ids);
            Assert.IsFalse(ReinforcementPlans.Remove(draft, view, elite));
        }

        [Test]
        public void TheBalrogsCompanionIsOptionalAndARulerFillsItsSlots()
        {
            var view = new BoardFixture().View;
            view.You = Side.Shadow;
            var elites = view.Units.Where(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "reserve")
                .Select(u => u.Id).ToList();
            var balrog = new CharacterActionOption
            {
                CharacterId = "lords.character.balrog", Title = "The Balrog of Moria", Targets = { "moria" }, TargetCount = 1,
                Recruitment = { new RecruitmentOption { Nation = "sauron", Kind = UnitKind.Elite, ReserveIds = elites, Targets = { "moria" } } }
            };
            view.Options = new List<ActionOption> { new ActionOption { Kind = CommandKind.EnterCharacter, CharacterActions = { balrog } } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            CharacterDraft.Select(draft, view, balrog);
            var plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual("character", plan.Prefix);
            Assert.AreEqual("The Balrog of Moria", plan.Title);
            Assert.IsTrue(plan.Optional);
            Assert.AreEqual(1, plan.Slots);
            Assert.IsTrue(plan.Ready, "Entering without a companion is legal.");
            Assert.IsFalse(plan.RegionFirst, "UI-392: the Balrog's one figure keeps its panel.");
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, elites[0]));
            Assert.AreEqual("moria", ReinforcementPlans.For(view, draft, Data).Placements.Single().RegionId,
                "The only target takes the figure at once.");

            var rohan = view.Units.Where(u => u.Nation == "rohan" && u.Kind == UnitKind.Regular && u.Location == "reserve")
                .Select(u => u.Id).ToList();
            var ruler = new CharacterActionOption
            {
                CharacterId = "kings.sovereign.theoden", AbilityId = "muster", Title = "Mustering of the Mark", TargetCount = 2,
                RulerRecruitment = new RulerRecruitmentOption { LocalRegion = "edoras", SettlementCount = 2, Settlements = { "edoras", "helms-deep" } },
                Recruitment = { new RecruitmentOption { Nation = "rohan", Kind = UnitKind.Regular, ReserveIds = rohan, Targets = { "edoras", "helms-deep" } } }
            };
            view.You = Side.FreePeoples;
            view.Options = new List<ActionOption> { new ActionOption { Kind = CommandKind.UseCharacterAbility, CharacterActions = { ruler } } };
            draft = new TurnDraft();
            draft.Choose(CommandKind.UseCharacterAbility);
            CharacterDraft.Select(draft, view, ruler);
            plan = ReinforcementPlans.For(view, draft, Data);
            Assert.IsFalse(plan.Optional);
            Assert.AreEqual(2, plan.Slots);
            StringAssert.Contains("Edoras", plan.Rows.Single().Status);
            Assert.IsFalse(plan.Ready);
            // UI-392 (owner): a king's recruitment starts on the map; the settlement clicked first takes the next figure.
            Assert.IsTrue(plan.RegionFirst);
            Assert.IsTrue(ReinforcementsState.For(view, plan, "", true).RegionFirst, "Its panel waits for the settlement's click.");
            Assert.IsTrue(CharacterRecruitDraft.Region(draft, ruler, "helms-deep"));
            Assert.AreEqual("helms-deep", draft.Payload.RegionId);
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, rohan[0]));
            Assert.AreEqual("helms-deep", ReinforcementPlans.For(view, draft, Data).Placements.Single().RegionId);
        }

        // UI-135: a Muster paid with a ruler token or an ability is planned in the panel, not in a tray palette.
        [Test]
        public void ARulerOrAbilityMusterIsPlannedWithTwoPointsAndOneElite()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.Recruit, "muster");
            Assert.IsTrue(ReinforcementPlans.Active(view, draft), "Without a stepwise sequence the whole plan is one command.");
            var plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual("muster", plan.Prefix);
            Assert.AreEqual(2, plan.Slots);
            Assert.IsFalse(plan.Ready);
            var gondor = plan.Rows.Single(r => r.Nation == "gondor");
            var regular = gondor.Figures.Single(f => f.Kind == UnitKind.Regular).ReserveId;
            Assert.IsTrue(ReinforcementPlans.Choose(draft, view, regular));
            Assert.IsTrue(RecruitDraft.Place(draft, view, "pelargir"));
            plan = ReinforcementPlans.For(view, draft, Data);
            Assert.AreEqual("pelargir", plan.Placements.Single().RegionId);
            var elite = plan.Rows.Single(r => r.Nation == "gondor").Figures.Single(f => f.Kind == UnitKind.Elite);
            Assert.IsNull(elite.ReserveId);
            Assert.AreEqual("An Elite needs both recruitment points", elite.Reason);
            Assert.IsTrue(plan.Ready, "One figure is a complete plan.");
            Assert.IsTrue(ReinforcementPlans.Remove(draft, view, regular));
            Assert.IsEmpty(draft.Payload.Placements);
            draft.Change(p => p.Stepwise = true);
            Assert.IsFalse(ReinforcementPlans.Active(view, draft), "A Muster step keeps the panel's Muster mode.");
        }

        [Test]
        public void NoPlanWithoutACardOrAbilityRecruitment()
        {
            var fixture = new BoardFixture();
            var draft = new TurnDraft();
            Assert.IsNull(ReinforcementPlans.For(fixture.View, draft, Data));
            draft.Choose(CommandKind.Recruit);
            draft.Change(p => p.Stepwise = true);
            Assert.IsFalse(ReinforcementPlans.Active(fixture.View, draft), "A Muster step keeps its own panel mode.");
        }

        private static PlayerView EventView(out ActionOption option)
        {
            var view = new BoardFixture().View;
            view.Decision = new DecisionView { Id = "card", Kind = DecisionKind.EffectChoice, SourceId = Card };
            option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2,
                EventRecruitment = new EventRecruitmentOption
                {
                    Limits =
                    {
                        new RecruitmentLimit { Nation = "gondor", RegionId = "minas-tirith", Maximum = 2, Kinds = { UnitKind.Regular, UnitKind.Elite } },
                        new RecruitmentLimit { Nation = "gondor", RegionId = "dol-amroth", Maximum = 1, Kinds = { UnitKind.Regular } }
                    }
                },
                Targets = { "minas-tirith", "dol-amroth" }
            };
            foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite })
                option.Recruitment.Add(new RecruitmentOption
                {
                    Nation = "gondor", Kind = kind,
                    ReserveIds = view.Units.Where(u => u.Nation == "gondor" && u.Kind == kind && u.Location == "reserve").Select(u => u.Id).ToList(),
                    Targets = kind == UnitKind.Regular ? new List<string> { "minas-tirith", "dol-amroth" } : new List<string> { "minas-tirith" }
                });
            option.EligibleIds.AddRange(option.Recruitment.SelectMany(g => g.ReserveIds));
            view.Options = new List<ActionOption> { option };
            return view;
        }

        private static PlayerView EffectView()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            view.Decision = new DecisionView { Id = "swords", Kind = DecisionKind.EffectChoice, SourceId = "base.fp.strategy.20", Presentation = "ReserveFigures" };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var nation in new[] { "north", "dwarves" })
                foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite })
                {
                    var unit = view.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
                    var target = nation == "north" ? "the-shire" : "ered-luin";
                    option.EligibleIds.Add(unit.Id);
                    option.EffectChoices.Add(new EffectChoiceView
                    {
                        Id = unit.Id, TargetId = target, SelectionGroup = fixture.Data.Regions[target].Name, GroupMinimum = 1, GroupMaximum = 1
                    });
                }
            view.Options = new List<ActionOption> { option };
            return view;
        }
    }
}
