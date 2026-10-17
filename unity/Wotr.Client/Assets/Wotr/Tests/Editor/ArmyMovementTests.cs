using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-127: whole-army selection, the target ghost and its captions, blocking figures and one undo per gesture.
    public sealed class ArmyMovementTests
    {
        [Test]
        public void OneGestureIsOneUndoStep()
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "army");
            draft.Gesture(() =>
            {
                draft.Choose(CommandKind.MoveArmies, "army");
                draft.Change(p => p.Stepwise = true);
                draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "minas-tirith" }));
            });
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
            draft.Back();
            Assert.IsNull(draft.Kind, "A single Back undoes the whole gesture.");
            Assert.AreEqual("army", draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.Moves);
        }

        [Test]
        public void GhostCaptionsComeOnlyFromTheServerTargets()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var movement = view.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var army = movement.Armies.First(a => a.RegionId == "minas-tirith");
            var everyone = army.UnitIds.Concat(army.CharacterIds).ToList();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.AttackArmy,
                EligibleDice = movement.EligibleDice.ToList(),
                MovementCosts = movement.MovementCosts.ToList(),
                Armies =
                {
                    new ArmyOption
                    {
                        RegionId = army.RegionId, UnitIds = army.UnitIds.ToList(), CharacterIds = army.CharacterIds.ToList(),
                        CommanderIds = army.CommanderIds.ToList(),
                        Targets = { new ArmyTargetOption { RegionId = "minas-morgul", BattleMode = BattleMode.Siege, EligibleIds = everyone } }
                    }
                }
            });
            var context = Context(view);
            var draft = new TurnDraft();
            Assert.IsNull(ArmyGhost.Of(view, draft, context), "No selection, no ghost.");
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, "minas-tirith", false));
            var ghost = ArmyGhost.Of(view, draft, context);
            Assert.NotNull(ghost);
            Assert.AreEqual(everyone.Count, ghost.Army.Total);
            Assert.AreEqual("Move " + everyone.Count + " figures", ghost.Captions["osgiliath"]);
            Assert.AreEqual("Attack · Siege", ghost.Captions["minas-morgul"]);
            CollectionAssert.IsSubsetOf(ghost.Captions.Keys, ArmyActionTargets.For(draft, view, context).ToList(),
                "The ghost never shows where the server offers no target.");
        }

        [Test]
        public void FiguresThatKeepTheSelectionOutOfARegionAreNamed()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var army = view.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies.First(a => a.RegionId == "minas-tirith");
            var blocked = view.Units.First(u => army.UnitIds.Contains(u.Id) && u.Kind == UnitKind.Regular);
            var target = army.Targets.First(t => t.RegionId == "osgiliath");
            target.EligibleIds.Remove(blocked.Id);
            var context = Context(view);
            var draft = new TurnDraft();
            ContextArmySelection.SelectAll(draft, view, context, "minas-tirith", false);
            Assert.IsFalse(ArmyActionTargets.For(draft, view, context).Contains("osgiliath"));
            CollectionAssert.AreEqual(new[] { blocked.Id }, ArmyActionTargets.Blocking(draft, view, context));
            StringAssert.Contains("Leave out " + FigureGrid.Name(blocked.Nation) + " Regular",
                ArmyActionTargets.Hint(draft, view, context));
            StringAssert.DoesNotContain("drag", ArmyActionTargets.Hint(draft, view, context));
        }

        private static ActionDieContext Context(PlayerView view)
        {
            var context = new ActionDieContext();
            context.Sync(view, "army");
            return context;
        }
    }
}
