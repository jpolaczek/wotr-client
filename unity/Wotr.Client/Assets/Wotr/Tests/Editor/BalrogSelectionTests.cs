using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-459 (owner, 2026-11-03): with the Balrog die the Balrog's force lights on the map and his region's panel marks who
    // goes; the selection alone tells whether he moves alone or with his army, and the Nazgûl never travels with him.
    public sealed class BalrogSelectionTests
    {
        private const string Balrog = "lords.character.balrog";
        private const string Orc = "orc";
        private const string Nazgul = "nazgul";

        [Test]
        public void TheBalrogDieIsItsAbilitySoHisForceLightsWithNoIcon()
        {
            var view = View();
            var context = Context(view);
            Assert.IsTrue(context.Implied);
            Assert.AreEqual("use-balrog", context.AbilityId);
            Assert.IsTrue(ContextArmySelection.Active(view, new TurnDraft(), context));
            CollectionAssert.AreEquivalent(new[] { Orc, Nazgul, Balrog }, ContextArmySelection.Eligible(view, context));
            Assert.IsEmpty(ComponentActions.For(view, Draft()), "No icon chooses his moves.");
            StringAssert.Contains("alone moves by himself", ArmyActionTargets.Hint(Draft(), view, context));
        }

        [Test]
        public void HisWholeForceMovesAsAnArmyWithTheCharacterCost()
        {
            var view = View();
            var context = Context(view);
            var draft = Draft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, "moria", false));
            Assert.IsFalse(BalrogSelection.Normalize(draft, view, context));
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
            Assert.AreEqual(DieFace.Character, draft.Payload.MovementCost);
            Assert.AreEqual("use-balrog", draft.Payload.AbilityId);
            CollectionAssert.AreEqual(new[] { "dimrill-dale" }, ArmyActionTargets.For(draft, view, context).ToList());
        }

        [Test]
        public void LeavingEveryoneElseBehindMovesHimAlone()
        {
            var view = View();
            var context = Context(view);
            var draft = Draft();
            ContextArmySelection.SelectAll(draft, view, context, "moria", false);
            ContextArmySelection.Select(draft, view, context, Orc);
            Assert.IsFalse(BalrogSelection.Normalize(draft, view, context), "The Nazgûl is still marked.");
            ContextArmySelection.Select(draft, view, context, Nazgul);
            Assert.IsTrue(BalrogSelection.Normalize(draft, view, context));
            Assert.AreEqual(CommandKind.MoveShadowCharacters, draft.Kind);
            Assert.AreEqual("use-balrog", draft.Payload.AbilityId);
            Assert.AreEqual("balrog-die", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { Balrog }, draft.Payload.CompanionMoves.Single().Ids);
        }

        [Test]
        public void TheNazgulNeverTravelsWithHimAndUnitsWithoutHimAskForHim()
        {
            var view = View();
            var context = Context(view);
            var draft = Draft();
            ContextArmySelection.SelectAll(draft, view, context, "moria", false);
            ContextArmySelection.Select(draft, view, context, Orc);
            Assert.IsEmpty(ArmyActionTargets.For(draft, view, context), "The Balrog and a Nazgûl make no army.");
            Assert.AreEqual("Include an army unit", ArmyActionTargets.Hint(draft, view, context));
            Assert.IsFalse(TravelActions.CanSelect(view, draft, context, Nazgul));
            ContextArmySelection.Select(draft, view, context, Orc);
            ContextArmySelection.Select(draft, view, context, Balrog);
            Assert.IsFalse(BalrogSelection.Normalize(draft, view, context));
            Assert.IsEmpty(ArmyActionTargets.For(draft, view, context));
            StringAssert.StartsWith("Include ", ArmyActionTargets.Hint(draft, view, context));
        }

        [Test]
        public void AnInactiveBalrogDieKeepsItsActivation()
        {
            var view = View();
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseCharacterAbility,
                CharacterActions = { new CharacterActionOption { CharacterId = Balrog, AbilityId = "activate-balrog", Dice = { "balrog-die" } } }
            });
            var context = Context(view);
            Assert.IsFalse(context.Implied);
            Assert.AreEqual(DieFace.UseBalrog, context.Face);
            Assert.IsTrue(ComponentActions.For(view, Draft()).Single().Enabled, "His activation is still his figure's use.");
        }

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "balrog-die");
            return draft;
        }

        private static ActionDieContext Context(PlayerView view)
        {
            var context = new ActionDieContext();
            context.Sync(view, "balrog-die");
            return context;
        }

        // Moria holds the active Balrog, an Orc and a Nazgûl; Dimrill Dale is the one target (the server's
        // lords-sh-use-balrog-active, reduced).
        private static PlayerView View()
        {
            var view = new PlayerView
            {
                MatchId = "balrog", You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
            };
            view.Dice.Add(new DieView { Id = "balrog-die", Owner = Side.Shadow, Source = "lords.die.balrog", Face = DieFace.UseBalrog, Location = DieLocation.Available });
            view.Characters.Add(new CharacterView { Id = Balrog, Location = "moria", Side = Side.Shadow, Level = 2 });
            view.Units.Add(new UnitView { Id = Orc, Nation = "sauron", Kind = UnitKind.Regular, Location = "moria", Side = Side.Shadow });
            view.Units.Add(new UnitView { Id = Nazgul, Nation = "sauron", Kind = UnitKind.Nazgul, Location = "moria", Side = Side.Shadow });
            var cost = new MovementCostOption
            {
                DieId = "balrog-die", AbilityId = "use-balrog", Face = DieFace.Character, MaximumMoves = 1,
                RequiresCommander = true, RequiredCharacterId = Balrog
            };
            var army = new ArmyOption
            {
                RegionId = "moria", UnitIds = { Orc, Nazgul }, CharacterIds = { Balrog }, CommanderIds = { Balrog },
                Targets = { new ArmyTargetOption { RegionId = "dimrill-dale", EligibleIds = { Orc, Nazgul, Balrog } } }
            };
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveArmies, EligibleDice = { "balrog-die" }, Maximum = 1, MovementCosts = { cost }, Armies = { army } });
            var travel = new ActionOption
            {
                Kind = CommandKind.MoveShadowCharacters, EligibleDice = { "balrog-die" }, EligibleIds = { Balrog, Nazgul },
                MovementCosts = { cost }
            };
            travel.Travel.Add(new TravelOption { Id = Balrog, Origin = "moria", Range = 2, Targets = { "dimrill-dale", "hollin" } });
            travel.Travel.Add(new TravelOption { Id = Nazgul, Origin = "moria", Flight = true, Targets = { "hollin" } });
            view.Options.Add(travel);
            return view;
        }
    }
}
