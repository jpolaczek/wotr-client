using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-127: whole-army selection, the targets' captions, blocking figures and one undo per gesture. UI-403: the captions
    // hang above the lifted figure (no ghost is drawn), the attacks make the landing mark red, and travellers have captions.
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
            Assert.IsNull(ArmyGhost.Of(view, draft, context), "No selection, no captions.");
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, "minas-tirith", false));
            var ghost = ArmyGhost.Of(view, draft, context);
            Assert.NotNull(ghost);
            Assert.AreEqual("Move " + everyone.Count + " figures", ghost.Captions["osgiliath"]);
            Assert.AreEqual("Attack · Siege", ghost.Captions["minas-morgul"]);
            // UI-403: the landing mark is red where the click attacks.
            CollectionAssert.AreEquivalent(new[] { "minas-morgul" }, ghost.Attacks);
            CollectionAssert.IsSubsetOf(ghost.Captions.Keys, ArmyActionTargets.For(draft, view, context).ToList(),
                "No caption where the server offers no target.");
            Assert.IsNull(ArmyGhost.Of(view, draft, null), "Without the army context the army's targets have no captions.");
            ContextArmySelection.Target(draft, view, context, "osgiliath");
            Assert.IsNull(ArmyGhost.Of(view, draft, context), "A move given its destination has no captions left.");
        }

        // UI-403: a travelling figure's targets have captions too — a card's flight names the armed figure, a companion's
        // travel names him — from the same destinations the map lights.
        [Test]
        public void TravellingFiguresHaveCaptionsOnTheirDestinations()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "search", Kind = DecisionKind.CharacterEvent, SourceId = "base.sh.character.09" },
                Units = { new UnitView { Id = "nazgul-1", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "mordor" } },
                Characters = { new CharacterView { Id = "witch-king", DefinitionId = "base.character.witch-king", Side = Side.Shadow, Location = "mordor" } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.MoveShadowCharacters, Enabled = true, AllowEmptySelection = true,
                        EligibleIds = { "nazgul-1", "witch-king" },
                        Travel =
                        {
                            new TravelOption { Id = "nazgul-1", Origin = "mordor", Flight = true, Targets = { "moria", "rohan" } },
                            new TravelOption { Id = "witch-king", Origin = "mordor", Flight = true, Targets = { "moria", "rohan" } }
                        }
                    }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            Assert.IsNull(ArmyGhost.Of(view, draft, null), "Nothing armed, no captions.");
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "mordor"));
            var flight = ArmyGhost.Of(view, draft, null);
            Assert.AreEqual("Fly a Nazgûl", flight.Captions["moria"]);
            CollectionAssert.AreEquivalent(new[] { "moria", "rohan" }, flight.Captions.Keys);
            Assert.IsEmpty(flight.Attacks);
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "witch-king"));
            Assert.AreEqual("Fly the Witch-king", ArmyGhost.Of(view, draft, null).Captions["rohan"]);
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
