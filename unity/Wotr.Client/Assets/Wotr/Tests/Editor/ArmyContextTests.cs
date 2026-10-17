using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class ArmyContextTests
    {
        [Test]
        public void WholeStackCorrectionAndOtherSourceReplaceOnlyTheLocalChoice()
        {
            var fixture = new BoardFixture();
            var context = Context(fixture.View);
            var draft = new TurnDraft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, fixture.View, context, "minas-tirith", false));
            var first = fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular);
            Assert.IsTrue(ContextArmySelection.Select(draft, fixture.View, context, first.Id));
            Assert.IsFalse(ArmyDraft.Current(draft).UnitIds.Contains(first.Id));
            Assert.Contains("osgiliath", ArmyActionTargets.For(draft, fixture.View, context).ToList());
            var other = fixture.View.Units.First(u => u.Location == "dale");
            ContextArmySelection.Select(draft, fixture.View, context, other.Id);
            Assert.AreEqual("dale", ArmyDraft.Current(draft).From);
            CollectionAssert.AreEqual(new[] { other.Id }, ArmyDraft.Current(draft).UnitIds);
            Assert.AreEqual(1, draft.Payload.Moves.Count);
            Assert.IsTrue(ContextArmySelection.Target(draft, fixture.View, context, "erebor"));
            Assert.IsTrue(draft.Command(fixture.View).Payload.Stepwise);
            Assert.IsTrue(draft.Fork().Payload.Stepwise);
            var restored = new TurnDraft();
            restored.Restore(draft.Snapshot());
            Assert.IsTrue(restored.Payload.Stepwise);
            Assert.AreEqual("minas-tirith", first.Location);
        }

        [Test]
        public void CommanderAndRearGuardAreCheckedBeforeHighlightingDestinations()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = DieFace.Character;
            fixture.Refresh();
            var context = Context(fixture.View);
            var draft = new TurnDraft();
            var units = fixture.View.Units.Where(u => u.Location == "minas-tirith").ToArray();
            var first = units.First(u => u.Kind == UnitKind.Regular);
            ContextArmySelection.Select(draft, fixture.View, context, first.Id);
            Assert.IsEmpty(ArmyActionTargets.For(draft, fixture.View, context));
            ContextArmySelection.Select(draft, fixture.View, context, units.Single(u => u.Kind == UnitKind.Leader).Id);
            Assert.IsNotEmpty(ArmyActionTargets.For(draft, fixture.View, context));
            Assert.IsTrue(ContextArmySelection.Target(draft, fixture.View, context, "osgiliath"));
            Assert.AreEqual(DieFace.Character, draft.Payload.MovementCost);
        }

        // UI-145: the server's option lists every army; a Character die commands only a force with a commander.
        [Test]
        public void CharacterDieSourcesOnlyForcesWithACommander()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = DieFace.Character;
            fixture.Refresh();
            var context = Context(fixture.View);
            var sources = ContextArmySelection.Sources(fixture.View, context).Select(a => a.RegionId).ToList();
            Assert.Contains("minas-tirith", sources);
            CollectionAssert.DoesNotContain(sources, "dale", "Dale's soldiers have no leader or character.");
            var soldier = fixture.View.Units.First(u => u.Location == "dale");
            Assert.IsFalse(ContextArmySelection.Eligible(fixture.View, context).Contains(soldier.Id));
            var draft = new TurnDraft();
            Assert.IsFalse(ContextArmySelection.SelectAll(draft, fixture.View, context, "dale", false));
            Assert.IsFalse(ContextArmySelection.Select(draft, fixture.View, context, soldier.Id));
            Assert.IsNull(ArmyDraft.Current(draft));
            Assert.AreEqual("Click an army with a commander to select all of it", ArmyActionTargets.Hint(draft, fixture.View, context));
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, fixture.View, context, "minas-tirith", false));

            fixture.View.Options[0].MovementCosts.Single(c => c.Face == DieFace.Character).RequiredCharacterId = "absent";
            Assert.IsEmpty(ContextArmySelection.Sources(fixture.View, context), "An ability's cost needs its own character in the force.");

            fixture.View.Dice[0].Face = DieFace.Army;
            fixture.Refresh();
            context = Context(fixture.View);
            sources = ContextArmySelection.Sources(fixture.View, context).Select(a => a.RegionId).ToList();
            Assert.Contains("dale", sources, "An Army die moves any army.");
            Assert.AreEqual("Click your army to select all of it", ArmyActionTargets.Hint(new TurnDraft(), fixture.View, context));
        }

        [Test]
        public void EnemyTargetUsesAttackWithIdenticalParticipantsAndNoOptimisticRelocation()
        {
            var fixture = new BoardFixture();
            var movement = fixture.View.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var army = movement.Armies.First(a => a.RegionId == "minas-tirith");
            army.Targets.RemoveAll(t => t.RegionId == "lossarnach");
            fixture.View.Options.Add(new ActionOption
            {
                Kind = CommandKind.AttackArmy, Maximum = 1,
                EligibleDice = { "army" },
                MovementCosts = { new MovementCostOption { DieId = "army", Face = DieFace.Army } },
                Armies = { new ArmyOption { RegionId = army.RegionId, UnitIds = army.UnitIds.ToList(),
                    CommanderIds = army.CommanderIds.ToList(), Targets = { new ArmyTargetOption
                    { RegionId = "lossarnach", EligibleIds = army.UnitIds.ToList() } } } }
            });
            fixture.View.Characters.Add(new CharacterView { Id = "stationary", Side = Side.FreePeoples, Location = army.RegionId, Level = 0 });
            var attacking = fixture.View.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies[0];
            attacking.CharacterIds.Add("stationary");
            attacking.Targets[0].EligibleIds.Add("stationary");
            var context = Context(fixture.View);
            var draft = new TurnDraft();
            Assert.IsFalse(ContextArmySelection.Target(draft, fixture.View, context, "lossarnach"));
            ContextArmySelection.SelectAll(draft, fixture.View, context, "minas-tirith", false);
            ContextArmySelection.Select(draft, fixture.View, context, army.UnitIds[0]);
            ContextArmySelection.Select(draft, fixture.View, context, army.UnitIds[0]);
            CollectionAssert.AreEqual(new[] { "stationary" }, draft.Payload.Moves[0].CharacterIds,
                "Toggling a mobile unit preserves a stationary participant available only for the attack.");
            var ids = draft.Payload.Moves[0].UnitIds.ToArray();
            Assert.IsFalse(ContextArmySelection.Target(draft, fixture.View, context, "rivendell"));
            Assert.IsTrue(ContextArmySelection.Target(draft, fixture.View, context, "lossarnach"));
            Assert.AreEqual(CommandKind.AttackArmy, draft.Kind);
            Assert.IsFalse(draft.Payload.Stepwise);
            CollectionAssert.AreEqual(ids, draft.Payload.Moves[0].UnitIds);
            Assert.IsTrue(fixture.View.Units.Where(u => ids.Contains(u.Id)).All(u => u.Location == "minas-tirith"));
        }

        [Test]
        public void PaidSequenceRestoresItsExactCostAndUsedFiguresStayUnavailable()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var session = new TurnDraftSession();
            session.Update(view);
            var context = Context(view);
            ContextArmySelection.SelectAll(session.Draft, view, context, "minas-tirith", false);
            var used = session.Draft.Payload.Moves[0].UnitIds.ToList();
            ContextArmySelection.Target(session.Draft, view, context, "osgiliath");
            session.Edited(view);
            view = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerView>(Newtonsoft.Json.JsonConvert.SerializeObject(view));
            view.Decision = new DecisionView { Id = "second-step", Kind = DecisionKind.ActionSequence };
            view.Revision++;
            view.Dice[0].Location = DieLocation.Resolving;
            view.ActionSequence = new ActionSequenceView { Owner = Side.FreePeoples, Kind = CommandKind.MoveArmies,
                DieId = "army", Face = DieFace.Army, CompletedSteps = 1, Remaining = 1, UsedIds = used };
            foreach (var option in view.Options.Where(o => o.Kind == CommandKind.MoveArmies))
                foreach (var army in option.Armies)
                    army.UnitIds.RemoveAll(used.Contains);
            session.Update(view);
            session.Draft.Back();
            Assert.IsEmpty(session.Draft.Payload.Moves, "Undo cannot recover the previous decision's draft.");
            context.Sync(view, "muster");
            Assert.AreEqual("army", context.DieId);
            Assert.AreEqual(DieFace.Army, context.Face);
            Assert.IsFalse(ContextArmySelection.Eligible(view, context).Overlaps(used));
            Assert.IsFalse(ContextArmySelection.Select(session.Draft, view, context, used[0]));
        }

        [Test]
        public void ClickOpenedPanelClosesAfterPointerLeaves()
        {
            var hover = new HoverState();
            hover.Open("dale");
            hover.Blur(0, true);
            Assert.IsFalse(hover.Tick(2), "Clicking the hovered object does not start a close timer.");
            hover.Observe("", 2.1);
            Assert.IsFalse(hover.Tick(2.24));
            Assert.IsTrue(hover.Tick(2.26));
            Assert.IsEmpty(hover.Visible);
        }

        private static ActionDieContext Context(PlayerView view)
        {
            var context = new ActionDieContext();
            context.Sync(view, "army");
            return context;
        }
    }
}
