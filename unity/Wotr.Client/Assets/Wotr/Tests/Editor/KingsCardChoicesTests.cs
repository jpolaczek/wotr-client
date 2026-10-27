using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-278 (EXP-15 D15-D16): the Kings cards' choices on their objects — the nation tokens, the kings' portraits, the
    // Reserve and a king who takes part in the battle; a branch's object carries into the card's next step.
    public sealed class KingsCardChoicesTests
    {
        private const string Wisdom = "kings.fp.strategy.08";
        private const string Stewards = "kings.fp.character.23";
        private const string Dain = "kings.sovereign.dain";
        private const string Denethor = "kings.sovereign.denethor";

        [Test]
        public void TheNationStepIsTheNationTokensAndLeavesTheConsoleEmpty()
        {
            var view = Step(KingsCardChoices.NationStep, "kings.fp.character.26");
            foreach (var (id, title) in new[] { ("north", "The North"), ("gondor", "Gondor") })
                Choice(view, new EffectChoiceView { Id = id, Title = title, Detail = "Advance one step, if possible." });
            var nations = KingsCardChoices.Nations(view);
            CollectionAssert.AreEquivalent(new[] { "north", "gondor" }, nations.Keys);
            StringAssert.StartsWith("Click to choose Gondor for It Is Not So Dark Here · Advance one step", nations["gondor"]);
            Assert.AreEqual("gondor", KingsCardChoices.NationEffect(view, "gondor"));
            Assert.AreEqual("Click a glowing nation token in Politics", KingsCardChoices.Hint(view));
            var draft = Effect();
            Assert.IsTrue(CardConsoleOptions.Owns(view, draft), "The tray builds no nation buttons.");
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
        }

        [Test]
        public void WisdomsBranchesAreTheNationTokensAndTheKingsWithTiles()
        {
            var view = Step(KingsCardChoices.Branch, Wisdom);
            Choice(view, new EffectChoiceView { Id = KingsCardChoices.Politics, Title = "Activate and advance" });
            Choice(view, new EffectChoiceView { Id = KingsCardChoices.Inspect, Title = "Inspect corruption" });
            Kings(view);
            view.Sovereigns.Single(s => s.Id == Dain).Tiles.Add(new CorruptionTileView { Id = "slot" });
            var nations = KingsCardChoices.Nations(view);
            CollectionAssert.AreEquivalent(new[] { "dwarves", "gondor" }, nations.Keys, "Each king's nation, as the Nation step offers.");
            Assert.AreEqual(KingsCardChoices.Politics, KingsCardChoices.NationEffect(view, "gondor"));
            var kings = SovereignChoices.Portraits(view);
            CollectionAssert.AreEqual(new[] { Dain }, kings.Keys, "Only an Initial king with tiles can be inspected.");
            Assert.AreEqual(KingsCardChoices.Inspect, kings[Dain].EffectId);
            StringAssert.StartsWith("Click to look at Dáin Ironfoot's Corruption tiles", kings[Dain].Tooltip);
            Assert.IsEmpty(CardConsoleOptions.For(view, Effect()), "Both branches stand on their objects.");
            StringAssert.Contains("nation token", KingsCardChoices.Hint(view));
            StringAssert.Contains("king to look at his tiles", KingsCardChoices.Hint(view));

            view.Sovereigns.Single(s => s.Id == Dain).Tiles.Clear();
            Assert.AreEqual(new[] { "effect-choice-" + KingsCardChoices.Inspect },
                CardConsoleOptions.For(view, Effect()).Select(o => o.Name).ToArray(), "A branch without its object stays in the console.");
        }

        [Test]
        public void ARecruitOrCleanseCardIsItsNationsReserveAndItsKing()
        {
            var view = Step(KingsCardChoices.Branch, Stewards);
            Choice(view, new EffectChoiceView { Id = KingsCardChoices.Recruit, TargetId = "gondor", Title = "Recruit", Detail = "Recruit the available figures specified by this card." });
            Choice(view, new EffectChoiceView { Id = KingsCardChoices.Cleanse, TargetId = Denethor, Title = "Remove corruption", Detail = "Return one randomly chosen hidden Corruption tile, if possible." });
            Kings(view);
            view.Units.Add(new UnitView { Id = "gondor-elite", Side = Side.FreePeoples, Nation = "gondor", Kind = UnitKind.Elite, Location = "reserve" });
            Assert.AreEqual("gondor", KingsCardChoices.RecruitNation(view));
            var kings = SovereignChoices.Portraits(view);
            CollectionAssert.AreEqual(new[] { Denethor }, kings.Keys);
            StringAssert.StartsWith("Click to remove corruption from Denethor", kings[Denethor].Tooltip);
            Assert.IsEmpty(CardConsoleOptions.For(view, Effect()));
            var state = ReinforcementsState.Branch(view, "gondor", true);
            var row = state.Rows.Single();
            Assert.AreEqual("gondor-elite", row.Figures.Single(f => f.Kind == UnitKind.Elite).ReserveId);
            Assert.IsTrue(row.CanRecruit);
            StringAssert.StartsWith("Click to recruit with House of the Stewards", state.TokenHint);
            StringAssert.Contains("Gondor in the Reserve to recruit", KingsCardChoices.Hint(view));
            StringAssert.Contains("the glowing Denethor to remove his corruption", KingsCardChoices.Hint(view));

            view.Units.Clear();
            Assert.AreEqual("", KingsCardChoices.RecruitNation(view));
            Assert.AreEqual(new[] { "effect-choice-" + KingsCardChoices.Recruit },
                CardConsoleOptions.For(view, Effect()).Select(o => o.Name).ToArray(), "An empty Reserve leaves the recruit in the console.");
        }

        [Test]
        public void ABranchsObjectWaitsForItsNextStepAndEndsWithAnyOther()
        {
            var carry = new BranchCarry();
            var branch = Step(KingsCardChoices.Branch, Wisdom);
            carry.Set(branch, KingsCardChoices.NationStep, "gondor");
            carry.Expire(branch);
            Assert.AreEqual("", carry.Take(branch, KingsCardChoices.NationStep), "The branch's own decision is still open.");
            var nation = Step(KingsCardChoices.NationStep, Wisdom);
            nation.Decision.Id = "next";
            carry.Expire(nation);
            Assert.AreEqual("", carry.Take(nation, SovereignChoices.Sovereign));
            Assert.AreEqual("gondor", carry.Take(nation, KingsCardChoices.NationStep));
            Assert.AreEqual("", carry.Take(nation, KingsCardChoices.NationStep), "Taking it ends the carry.");

            carry.Set(branch, BranchCarry.Reserve, "gondor-elite");
            var plan = Step("ReserveFigures", Stewards);
            plan.Decision.Id = "recruit";
            Assert.AreEqual("gondor-elite", carry.Take(plan, BranchCarry.Reserve));

            carry.Set(branch, SovereignChoices.Sovereign, Dain);
            var other = Step(SovereignChoices.Inspection, Wisdom);
            other.Decision.Id = "inspection";
            carry.Expire(other);
            var sovereign = Step(SovereignChoices.Sovereign, Wisdom);
            sovereign.Decision.Id = "later";
            Assert.AreEqual("", carry.Take(sovereign, SovereignChoices.Sovereign), "A step in between ended the carry.");
        }

        [Test]
        public void AKingWhoTakesPartInTheBattleIsChosenInItsArmy()
        {
            var view = Step(SovereignChoices.Sovereign, "kings.sh.strategy.25");
            Kings(view);
            Choice(view, new EffectChoiceView { Id = Denethor, SourceId = Denethor, Title = "Denethor" });
            Choice(view, new EffectChoiceView { Id = Dain, SourceId = Dain, Title = "Dáin Ironfoot" });
            Assert.AreEqual(2, SovereignChoices.Portraits(view).Count);
            Assert.AreEqual("Click a glowing king, by his portrait or on the map", SovereignPresentation.Hint(view, Effect()));
            view.Battle = new BattleView
            {
                RegionId = "edoras",
                Defender = new BattleForceView { Side = Side.FreePeoples, Units = { new UnitView { Id = Denethor, Nation = "gondor", Kind = UnitKind.Leader } } }
            };
            CollectionAssert.AreEqual(new[] { Dain }, SovereignChoices.Portraits(view).Keys, "Denethor glows in the battle window instead.");
            Assert.IsTrue(BattleObjectChoices.Offered(view, Denethor));
            Assert.IsTrue(SovereignChoices.InBattleWindow(view));
            Assert.AreEqual(DecisionContext.Battle, DecisionAttention.For(view).Context, "The notice opens the battle, not the Characters panel.");
            Assert.AreEqual("Click a glowing king in the battle window's army or by his portrait", SovereignPresentation.Hint(view, Effect()));
        }

        [Test]
        public void KeepTilesAfterAnInspectionIsThePass()
        {
            var view = Step(SovereignChoices.Inspection, Wisdom);
            Choice(view, new EffectChoiceView { Id = SovereignChoices.Keep, TargetId = Dain, Title = "Keep tiles" });
            Choice(view, new EffectChoiceView { Id = "tile", TargetId = Dain, Title = "1 Corruption" });
            var pass = OptionalDecline.Option(view, Effect());
            Assert.AreEqual(SovereignChoices.Keep, pass.Id);
            Assert.AreEqual("Pass · Finish looking without returning a tile", pass.Tooltip);
            Assert.IsEmpty(CardConsoleOptions.For(view, Effect()));
            Assert.AreEqual("Click a tile beside the king to return it to the Hunt Pool", SovereignPresentation.Hint(view, Effect()));
        }

        [Test]
        public void ThePalantirsCardConvertsTheSelectedMusterDieOrWaitsForOne()
        {
            var view = new PlayerView
            {
                MatchId = "kings", You = Side.Shadow, DecisionOwner = Side.Shadow, Phase = GamePhase.ActionResolution,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
                Dice =
                {
                    new DieView { Id = "muster", Owner = Side.Shadow, Face = DieFace.Muster, Source = "standard", Location = DieLocation.Available },
                    new DieView { Id = "army", Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available }
                },
                Options =
                {
                    new ActionOption { Kind = CommandKind.DrawCard, Enabled = true, EligibleDice = { "muster" }, Targets = { "strategy" },
                        DrawCosts = { new EventCostOption { DieId = "muster", Face = DieFace.Event, AbilityId = CardDrawDraft.Palantir } } }
                }
            };
            var draft = new TurnDraft();
            StringAssert.StartsWith("Click, then a glowing Muster die", PalantirConversion.CardUses(view, draft)[PalantirConversion.Card]);
            PalantirConversion.Use(view, draft);
            Assert.IsTrue(PalantirConversion.Armed(draft));
            CollectionAssert.AreEqual(new[] { "muster" }, CostDice.Offered(view, draft), "The die it converts glows.");
            StringAssert.Contains("spend this Muster die as an Event die", PalantirConversion.DieHint(view, draft, "muster"));
            Assert.AreEqual("", PalantirConversion.DieHint(view, draft, "army"));
            Assert.IsEmpty(PalantirConversion.CardUses(view, draft), "The card waits for its die.");
            draft.Change(p => p.DieId = "muster");
            Assert.IsTrue(PalantirConversion.Converted(draft));
            Assert.AreEqual(DieFace.Event, draft.Payload.Face);

            var selected = new TurnDraft();
            selected.Change(p => p.DieId = "muster");
            StringAssert.StartsWith("Click to spend this Muster die", PalantirConversion.CardUses(view, selected)[PalantirConversion.Card]);
            PalantirConversion.Use(view, selected);
            Assert.IsTrue(PalantirConversion.Converted(selected), "The selected die converts at once.");

            var context = new ActionDieContext();
            context.Sync(view, "muster");
            var picker = new CardCostPicker();
            picker.Refresh(view, new TurnDraft(), context, false);
            Assert.AreEqual(DisplayStyle.None, picker.Root.style.display.value, "The conversion is the card's, not the picker's.");
        }

        [Test]
        public void TheKingsRemovalsListNoDiceAndBaradDurTakesItsRingOnItsSocket()
        {
            foreach (var kind in new[] { CommandKind.DiscardWelcome, CommandKind.DiscardBaradPalantir })
            {
                var view = new PlayerView
                {
                    You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Phase = GamePhase.ActionResolution,
                    Decision = new DecisionView { Id = "pay", Kind = DecisionKind.Action },
                    Dice =
                    {
                        new DieView { Id = "will", Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available },
                        new DieView { Id = "character", Owner = Side.FreePeoples, Face = DieFace.Character, Source = "standard", Location = DieLocation.Available }
                    },
                    Options = { new ActionOption { Kind = kind, Enabled = true, EligibleDice = { "will" }, DiceWithoutRing = { "will" } } }
                };
                if (kind == CommandKind.DiscardBaradPalantir)
                {
                    view.Options[0].EligibleDice.Add("character");
                    view.Options[0].EligibleIds.Add("base.ring.narya");
                }
                var draft = new TurnDraft();
                draft.Choose(kind, kind == CommandKind.DiscardBaradPalantir ? "character" : "will");
                var host = new VisualElement();
                new DecisionTray(host).Render(view, draft, false, false);
                Assert.IsNull(host.Q<Button>("cost-will"), kind + ": the pool holds the dice.");
                Assert.IsNull(host.Q<Button>("cost-character"));
                Assert.IsNull(host.Q<Button>("palantir-no-ring"));
                Assert.IsNull(host.Q<Button>("ring-cost-base.ring.narya"));
                if (kind != CommandKind.DiscardBaradPalantir)
                    continue;
                StringAssert.StartsWith("Click a lit Elven Ring", CostDice.RingHint(view, draft));
                Assert.IsTrue(CostDice.DropsRing(view, draft, "will"), "A Will die pays alone.");
            }
        }

        private static PlayerView Step(string presentation, string card) => new PlayerView
        {
            MatchId = "kings", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Phase = GamePhase.ActionResolution,
            Decision = new DecisionView { Id = "branch", Kind = DecisionKind.EffectChoice, Presentation = presentation, SourceId = card },
            Nations = { new NationView { Id = "dwarves" }, new NationView { Id = "gondor" }, new NationView { Id = "sauron" } },
            Options = { new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 } }
        };

        private static void Choice(PlayerView view, EffectChoiceView choice)
        {
            view.Options[0].EligibleIds.Add(choice.Id);
            view.Options[0].EffectChoices.Add(choice);
        }

        private static void Kings(PlayerView view)
        {
            view.Sovereigns.Add(new SovereignView { Id = Dain, Title = "Dáin Ironfoot", Nation = "dwarves", Status = SovereignStatus.Initial });
            view.Sovereigns.Add(new SovereignView { Id = Denethor, Title = "Denethor", Nation = "gondor", Status = SovereignStatus.Initial });
        }

        private static TurnDraft Effect()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }
    }
}
