using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-245: a combat card's targets are the Battle window's own dice and figures; an exclusive outcome without an
    // object keeps its button, and the command carries the exact chosen ids.
    public sealed class BattleObjectChoicesTests
    {
        [Test]
        public void AMissIsChosenOnItsDieAndKeepMissesStaysAButton()
        {
            var view = View();
            Assert.IsTrue(BattleObjectChoices.Offered(view, "miss-1"));
            Assert.IsFalse(BattleObjectChoices.Offered(view, "keep-misses"), "An exclusive outcome without an object stays a button.");
            Assert.IsFalse(BattleObjectChoices.Offered(view, "hit-1"), "A die the card cannot choose is not a target.");
            Assert.IsTrue(BattleObjectChoices.Owns(view, "nazgul-1"));
            var draft = new TurnDraft();
            BattleObjectChoices.Choose(view, draft, "miss-1");
            Assert.AreEqual(CommandKind.ChooseEffect, draft.Kind);
            CollectionAssert.AreEqual(new[] { "miss-1" }, draft.Payload.Ids);

            var root = new VisualElement();
            var strip = new BattleDiceStrip(root, view.Battle.Attacker, view.Battle, view, new TurnDraft(), _ => { });
            strip.Refresh();
            Assert.IsTrue(root.Q<Button>("battle-die-miss-1").ClassListContains(ChoiceGlow.ClassName), "The miss glows as the target.");
            Assert.IsFalse(root.Q<Button>("battle-die-hit-1").ClassListContains(ChoiceGlow.ClassName));
            StringAssert.Contains("Convert this miss", root.Q<Button>("battle-die-miss-1").tooltip);
        }

        [Test]
        public void AFigureOfAStackIsTakenFromTheStackAndOutsideABattleNothingIsATarget()
        {
            var view = View();
            view.Decision.Presentation = "NazgulOrMinion";
            view.Options[0].EligibleIds.Clear();
            view.Options[0].EligibleIds.AddRange(new[] { "nazgul-1", "nazgul-2" });
            view.Options[0].EffectChoices.Clear();
            var draft = new TurnDraft();
            var stack = view.Battle.Defender.Units;
            Assert.AreEqual("nazgul-1", BattleObjectChoices.NextInStack(view, draft, stack));
            view.Battle = null;
            Assert.IsFalse(BattleObjectChoices.Offered(view, "nazgul-1"));
        }

        // UI-246: Cruel as Death's fixed cost of two points from different figures; each point stays on its own figure,
        // the Witch-king's two points stay his, a third point cannot be added and the source can change before sending.
        [Test]
        public void LeadershipPointsStandOnTheirFiguresAndTheSourceCanChangeBeforeSending()
        {
            var view = LeadershipView(2, 2);
            CollectionAssert.AreEqual(new[] { "nazgul-a:leadership:1" }, BattleObjectChoices.PointsOf(view, new[] { "nazgul-a", "nazgul-b" }));
            CollectionAssert.AreEqual(new[] { "witch-king:leadership:1", "witch-king:leadership:2" },
                BattleObjectChoices.PointsOf(view, new[] { "witch-king" }));
            var draft = new TurnDraft();
            BattleObjectChoices.Choose(view, draft, "witch-king:leadership:1");
            BattleObjectChoices.Choose(view, draft, "nazgul-a:leadership:1");
            Assert.IsFalse(BattleObjectChoices.CanChoose(view, draft, "witch-king:leadership:2"), "A third point exceeds the fixed cost.");
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]));

            var root = new VisualElement();
            var context = new BattleContext(root, view.Battle, view, draft, edit => edit(), _ => { });
            context.Refresh();
            var witchKing = root.Q("portrait-witch-king");
            Assert.AreEqual("◆ 1/2", witchKing.Q<Label>("battle-leadership-points").text, "The portrait counts its own points.");
            Assert.IsTrue(witchKing.ClassListContains("selected"));
            Assert.IsFalse(witchKing.ClassListContains(ChoiceGlow.ClassName), "The full cost leaves nothing to add.");
            var stack = root.Q("battle-figure-sauron-Nazgul");
            Assert.AreEqual("◆ 1/1", stack.Q<Label>("battle-leadership-points").text, "Only the offered point of the stack counts.");
            Assert.IsEmpty(root.Query<Button>(className: BattleChoicePip.ClassName).ToList(), "No small point buttons.");

            var stackPoints = BattleObjectChoices.PointsOf(view, new[] { "nazgul-a", "nazgul-b" });
            BattleObjectChoices.ClickPoints(view, draft, stackPoints);
            CollectionAssert.AreEqual(new[] { "witch-king:leadership:1" }, draft.Payload.Ids, "A source that can add nothing gives its points back.");
            context.Refresh();
            Assert.IsTrue(witchKing.ClassListContains(ChoiceGlow.ClassName), "Taking a point back opens the others again.");
            BattleObjectChoices.ClickPoints(view, draft, BattleObjectChoices.PointsOf(view, new[] { "witch-king" }));
            CollectionAssert.AreEquivalent(new[] { "witch-king:leadership:1", "witch-king:leadership:2" }, draft.Payload.Ids,
                "The Witch-king's click takes his next point.");
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]));
        }

        // UI-246: a variable cost (They Are Terrible) accepts any positive set; no points means no badge.
        [Test]
        public void AVariableLeadershipCostAcceptsAnyPositiveSetAndExhaustedPointsShowNothing()
        {
            var view = LeadershipView(1, 3);
            var draft = new TurnDraft();
            BattleObjectChoices.Choose(view, draft, "witch-king:leadership:2");
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]));
            BattleObjectChoices.Choose(view, draft, "nazgul-a:leadership:1");
            BattleObjectChoices.Choose(view, draft, "witch-king:leadership:1");
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]));
            Assert.AreEqual(3, draft.Payload.Ids.Count);
            Assert.IsFalse(BattleObjectChoices.Offered(view, "nazgul-b"), "A figure is not itself a point.");

            view.Options[0].EligibleIds.Clear();
            view.Options[0].EffectChoices.Clear();
            Assert.IsEmpty(BattleObjectChoices.PointsOf(view, new[] { "witch-king", "nazgul-a" }));
        }

        // UI-246: Heroic Death takes the figure in its column and the cancelled hits on the incoming hits; zero hits and
        // Keep all stay buttons, and the amount is open only once a figure that can cancel it is chosen.
        [Test]
        public void HeroicDeathTakesTheFigureAndTheCancelledHitsOnTheirIndicators()
        {
            var view = SacrificeView();
            Assert.IsTrue(BattleObjectChoices.Offered(view, "gimli"));
            Assert.IsTrue(BattleObjectChoices.Offered(view, "heroic-death.hits.2"));
            Assert.IsFalse(BattleObjectChoices.Offered(view, "heroic-death.hits.0"), "Keep hits stays a button.");
            Assert.IsFalse(BattleObjectChoices.Offered(view, "heroic-death.decline"), "Keep all stays a button.");
            var draft = new TurnDraft();
            Assert.IsFalse(BattleObjectChoices.CanChoose(view, draft, "heroic-death.hits.1"), "The hits wait for the figure.");

            var root = new VisualElement();
            var context = new BattleContext(root, view.Battle, view, draft, edit => edit(), _ => { });
            context.Refresh();
            var pips = root.Q("battle-defender").Q("battle-hit-cancels");
            Assert.IsNotNull(pips, "The pips sit on the opponent's hits.");
            Assert.IsNull(root.Q("battle-attacker").Q("battle-hit-cancels"));
            Assert.AreEqual(2, pips.childCount);
            Assert.IsFalse(pips.Q<Button>("battle-hit-cancel-1").enabledSelf);

            BattleObjectChoices.Choose(view, draft, "gimli");
            context.Refresh();
            Assert.IsTrue(pips.Q<Button>("battle-hit-cancel-2").ClassListContains(ChoiceGlow.ClassName));
            BattleObjectChoices.Choose(view, draft, "heroic-death.hits.2");
            context.Refresh();
            CollectionAssert.AreEquivalent(new[] { "gimli", "heroic-death.hits.2" }, draft.Payload.Ids);
            Assert.AreEqual(2, BattleObjectChoices.ChosenHits(view, draft));
            Assert.IsTrue(pips.Q<Button>("battle-hit-cancel-2").ClassListContains("selected"), "The chosen amount is selected.");
            Assert.IsFalse(pips.Q<Button>("battle-hit-cancel-1").ClassListContains("selected"));
            Assert.AreEqual("−2", pips.Q<Button>("battle-hit-cancel-2").text);
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]));

            BattleObjectChoices.Choose(view, draft, "heroic-death.hits.1");
            CollectionAssert.AreEquivalent(new[] { "gimli", "heroic-death.hits.1" }, draft.Payload.Ids, "A smaller amount replaces the larger.");
            BattleObjectChoices.Choose(view, draft, "boromir");
            CollectionAssert.AreEquivalent(new[] { "boromir", "heroic-death.hits.1" }, draft.Payload.Ids, "Another figure replaces the first.");
            EffectChoiceDraft.Choose(draft, view.Options[0], "heroic-death.decline");
            CollectionAssert.AreEqual(new[] { "heroic-death.decline" }, draft.Payload.Ids, "Keep all clears the plan.");
        }

        // UI-347 (EXP-15 D10): Orderly Withdrawal's cancelled hit is a "−1" pip on the opponent's hits; Keep the hit is the
        // console's Pass, so no list repeats either.
        [Test]
        public void OrderlyWithdrawalCancelsItsHitOnThePipAndKeepsItThroughPass()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = new DecisionView { Id = "withdrawal", Kind = DecisionKind.EffectChoice, Presentation = "CombatEffect" };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            option.EffectChoices.Add(new EffectChoiceView { Id = BattleObjectChoices.WithdrawalCancel, Title = "Cancel hit · withdraw",
                Detail = "Resolve both armies' remaining casualties, then end the attack or retreat if possible." });
            option.EffectChoices.Add(new EffectChoiceView { Id = OptionalDecline.WithdrawalKeep, Title = "Keep the hit" });
            option.EligibleIds.AddRange(option.EffectChoices.Select(c => c.Id));
            view.Options.Clear();
            view.Options.Add(option);

            Assert.IsTrue(BattleObjectChoices.Offered(view, BattleObjectChoices.WithdrawalCancel));
            Assert.AreEqual(OptionalDecline.WithdrawalKeep, OptionalDecline.Option(view, new TurnDraft()).Id);
            Assert.AreEqual("Pass · Keep the hit and fight on", OptionalDecline.Option(view, new TurnDraft()).Tooltip);
            var draft = new TurnDraft();
            var root = new VisualElement();
            var context = new BattleContext(root, view.Battle, view, draft, edit => edit(), _ => { });
            context.Refresh();
            var pip = root.Q("battle-defender").Q<Button>("battle-hit-cancel-1");
            Assert.AreEqual("−1", pip.text);
            Assert.IsTrue(pip.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Cancel hit · withdraw", pip.tooltip);
            BattleObjectChoices.Choose(view, draft, BattleObjectChoices.WithdrawalCancel);
            CollectionAssert.AreEqual(new[] { BattleObjectChoices.WithdrawalCancel }, draft.Payload.Ids);
            Assert.IsTrue(CardActions.SingleEffect(view, draft), "The pip's click completes the step, so it is sent.");
        }

        private static PlayerView LeadershipView(int minimum, int maximum)
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Defender;
            force.Units.Add(new UnitView { Id = "nazgul-a", Kind = UnitKind.Nazgul, Nation = "sauron", Side = Side.Shadow, Location = force.RegionId });
            force.Units.Add(new UnitView { Id = "nazgul-b", Kind = UnitKind.Nazgul, Nation = "sauron", Side = Side.Shadow, Location = force.RegionId });
            var witchKing = new CharacterView { Id = "witch-king", DefinitionId = "base.character.witch-king", Side = Side.Shadow, Location = force.RegionId };
            view.Characters.Add(witchKing);
            force.Characters.Add(witchKing);
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Decision = new DecisionView { Id = "cruel", Kind = DecisionKind.EffectChoice, Presentation = BattleObjectChoices.LeadershipPresentation };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = minimum, Maximum = maximum };
            foreach (var (id, source) in new[] { ("nazgul-a:leadership:1", "nazgul-a"), ("witch-king:leadership:1", "witch-king"), ("witch-king:leadership:2", "witch-king") })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView
                {
                    Id = id, SourceId = source, Title = "Nazgûl ◆", Detail = "Forfeit one Leadership for this round.",
                    SelectionGroup = "Leadership", GroupMinimum = minimum, GroupMaximum = maximum
                });
            }
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }

        private static PlayerView SacrificeView()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Attacker;
            foreach (var id in new[] { "gimli", "boromir" })
            {
                var character = new CharacterView { Id = id, DefinitionId = "base.character." + id, Side = Side.FreePeoples, Location = force.RegionId };
                view.Characters.Add(character);
                force.Characters.Add(character);
            }
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = new DecisionView { Id = "heroic", Kind = DecisionKind.EffectChoice, Presentation = BattleObjectChoices.SacrificePresentation };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 2 };
            option.EffectChoices.Add(new EffectChoiceView { Id = "gimli", SelectionGroup = "Sacrifice", Detail = "Eliminate this figure to cancel the selected hits." });
            option.EffectChoices.Add(new EffectChoiceView { Id = "boromir", SelectionGroup = "Sacrifice", Detail = "Eliminate this figure to cancel the selected hits." });
            option.EffectChoices.Add(new EffectChoiceView { Id = "heroic-death.decline", SelectionGroup = "Sacrifice", Title = "Keep all", Exclusive = true });
            option.EffectChoices.Add(Hits(0, "gimli", "boromir"));
            option.EffectChoices.Add(Hits(1, "gimli", "boromir"));
            option.EffectChoices.Add(Hits(2, "gimli", "boromir"));
            option.EligibleIds.AddRange(option.EffectChoices.Select(c => c.Id));
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }

        private static EffectChoiceView Hits(int amount, params string[] figures)
        {
            var choice = new EffectChoiceView
            {
                Id = "heroic-death.hits." + amount, SelectionGroup = "Hits",
                Title = amount == 0 ? "Keep hits" : "− " + new string('●', amount), Detail = $"Cancel {amount} incoming hits."
            };
            choice.RequiresAny.AddRange(figures);
            return choice;
        }

        private static PlayerView View()
        {
            var attacker = new BattleForceView
            {
                Side = Side.FreePeoples,
                Dice = { new BattleDieView { Id = "miss-1", Result = 2 }, new BattleDieView { Id = "hit-1", Result = 6, Hit = true } },
                Characters = { new CharacterView { Id = "base.character.strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples } }
            };
            var defender = new BattleForceView
            {
                Side = Side.Shadow,
                Units = { new UnitView { Id = "nazgul-1", Kind = UnitKind.Nazgul, Nation = "sauron" }, new UnitView { Id = "nazgul-2", Kind = UnitKind.Nazgul, Nation = "sauron" } }
            };
            return new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "anduril", Kind = DecisionKind.EffectChoice, Presentation = "ConvertMisses" },
                Battle = new BattleView { Id = "b", Attacker = attacker, Defender = defender },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { "miss-1", "keep-misses" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "miss-1", SelectionGroup = "Miss", Title = "2 → Hit", Detail = "Convert this miss to a hit." },
                            new EffectChoiceView { Id = "keep-misses", Title = "Keep misses", Exclusive = true, SelectionGroup = "Miss" }
                        }
                    }
                }
            };
        }
    }
}
