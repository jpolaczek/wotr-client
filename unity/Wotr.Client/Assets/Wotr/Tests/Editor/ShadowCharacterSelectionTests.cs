using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-566 (owner, 2026-11-22): with the Character die, unmarking every unit of a Shadow army in its panel leaves its
    // Nazgûl (and the Witch-king) to move alone, as the die's Move Nazgûl and Minions. UI-567: the panel's Move Nazgûl
    // selects a force's flyers in one click, only with the Shadow's Character die.
    public sealed class ShadowCharacterSelectionTests
    {
        private const string Die = "sh-character";
        private const string ArmyDie = "sh-army";
        private const string Orc1 = "orc-1";
        private const string Orc2 = "orc-2";
        private const string Nazgul = "nazgul-1";
        private const string WitchKing = "base.character.witch-king";
        private const string Mouth = "base.character.mouth-of-sauron";

        [Test]
        public void UnmarkingEveryUnitLeavesTheNazgulToFlyAlone()
        {
            var view = View();
            var context = Context(view, Die);
            var draft = Draft(Die);
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, "gorgoroth", false));
            ContextArmySelection.Select(draft, view, context, Orc1);
            Assert.IsFalse(ShadowCharacterSelection.Normalize(draft, view, context), "An Orc is still marked.");
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
            ContextArmySelection.Select(draft, view, context, Orc2);
            Assert.IsTrue(ShadowCharacterSelection.Normalize(draft, view, context));
            Assert.AreEqual(CommandKind.MoveShadowCharacters, draft.Kind);
            Assert.AreEqual(Die, draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
            Assert.IsEmpty(draft.Payload.Moves);
            CollectionAssert.AreEquivalent(new[] { Nazgul, WitchKing }, draft.Payload.CompanionMoves.Single().Ids);
        }

        [Test]
        public void AWalkingMinionStaysBehindTheFlyers()
        {
            var view = View(withMouth: true);
            var context = Context(view, Die);
            var draft = Draft(Die);
            ContextArmySelection.SelectAll(draft, view, context, "gorgoroth", false);
            ContextArmySelection.Select(draft, view, context, Orc1);
            ContextArmySelection.Select(draft, view, context, Orc2);
            Assert.IsTrue(ShadowCharacterSelection.Normalize(draft, view, context));
            CollectionAssert.AreEquivalent(new[] { Nazgul, WitchKing }, draft.Payload.CompanionMoves.Single().Ids);
        }

        [Test]
        public void AnArmyDieNeverTurnsItsSelectionIntoAFlight()
        {
            var view = View();
            var context = Context(view, ArmyDie);
            var draft = Draft(ArmyDie);
            ContextArmySelection.SelectAll(draft, view, context, "gorgoroth", false);
            ContextArmySelection.Select(draft, view, context, Orc1);
            ContextArmySelection.Select(draft, view, context, Orc2);
            Assert.IsFalse(ShadowCharacterSelection.Normalize(draft, view, context));
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
        }

        [Test]
        public void MoveNazgulSelectsTheForcesFlyersInOneClick()
        {
            var view = View(withMouth: true);
            var context = Context(view, Die);
            var draft = Draft(Die);
            // The whole army chosen first gives way to the flight.
            ContextArmySelection.SelectAll(draft, view, context, "gorgoroth", false);
            CollectionAssert.AreEquivalent(new[] { Nazgul, WitchKing },
                ShadowCharacterSelection.Flyers(view, draft, context, "gorgoroth", false));
            Assert.IsEmpty(ShadowCharacterSelection.Flyers(view, draft, context, "gorgoroth", true), "No garrison there.");
            Assert.IsTrue(ShadowCharacterSelection.SelectFlyers(draft, view, context, "gorgoroth", false));
            Assert.AreEqual(CommandKind.MoveShadowCharacters, draft.Kind);
            Assert.AreEqual(Die, draft.Payload.DieId);
            CollectionAssert.AreEquivalent(new[] { Nazgul, WitchKing }, draft.Payload.CompanionMoves.Single().Ids);
        }

        [Test]
        public void MoveNazgulStaysDarkWithoutTheCharacterDie()
        {
            var view = View();
            Assert.IsEmpty(ShadowCharacterSelection.Flyers(view, Draft(ArmyDie), Context(view, ArmyDie), "gorgoroth", false));
            Assert.IsEmpty(ShadowCharacterSelection.Flyers(view, new TurnDraft(), Context(view, ""), "gorgoroth", false));
            var draft = Draft(ArmyDie);
            Assert.IsFalse(ShadowCharacterSelection.SelectFlyers(draft, view, Context(view, ArmyDie), "gorgoroth", false));
            Assert.IsNull(draft.Kind);
        }

        private static TurnDraft Draft(string die)
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = die);
            return draft;
        }

        private static ActionDieContext Context(PlayerView view, string die)
        {
            var context = new ActionDieContext();
            context.Sync(view, die);
            return context;
        }

        // Gorgoroth holds two Orcs, a Nazgûl and the Witch-king (and, when asked, the Mouth of Sauron); Nurn is the army's
        // target and Nurn or Osgiliath the flyers'.
        private static PlayerView View(bool withMouth = false)
        {
            var view = new PlayerView
            {
                MatchId = "nazgul", You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
            };
            view.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Character, Location = DieLocation.Available });
            view.Dice.Add(new DieView { Id = ArmyDie, Owner = Side.Shadow, Face = DieFace.Army, Location = DieLocation.Available });
            view.Units.Add(new UnitView { Id = Orc1, Nation = "sauron", Kind = UnitKind.Regular, Location = "gorgoroth", Side = Side.Shadow });
            view.Units.Add(new UnitView { Id = Orc2, Nation = "sauron", Kind = UnitKind.Regular, Location = "gorgoroth", Side = Side.Shadow });
            view.Units.Add(new UnitView { Id = Nazgul, Nation = "sauron", Kind = UnitKind.Nazgul, Location = "gorgoroth", Side = Side.Shadow });
            view.Characters.Add(new CharacterView { Id = WitchKing, DefinitionId = WitchKing, Location = "gorgoroth", Side = Side.Shadow, Level = 5, Leadership = 2 });
            if (withMouth)
                view.Characters.Add(new CharacterView { Id = Mouth, DefinitionId = Mouth, Location = "gorgoroth", Side = Side.Shadow, Level = 3, Leadership = 2 });
            var characters = view.Characters.Select(c => c.Id).ToList();
            var army = new ArmyOption
            {
                RegionId = "gorgoroth", UnitIds = { Orc1, Orc2, Nazgul }, CommanderIds = { Nazgul },
                Targets = { new ArmyTargetOption { RegionId = "nurn", EligibleIds = { Orc1, Orc2, Nazgul } } }
            };
            army.CharacterIds.AddRange(characters);
            army.CommanderIds.AddRange(characters);
            army.Targets[0].EligibleIds.AddRange(characters);
            var character = new MovementCostOption { DieId = Die, Face = DieFace.Character, MaximumMoves = 1, RequiresCommander = true };
            var armyCost = new MovementCostOption { DieId = ArmyDie, Face = DieFace.Army, MaximumMoves = 2 };
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.MoveArmies, Enabled = true, EligibleDice = { Die, ArmyDie }, Maximum = 2,
                MovementCosts = { character, armyCost }, Armies = { army }
            });
            var travel = new ActionOption
            {
                Kind = CommandKind.MoveShadowCharacters, Enabled = true, EligibleDice = { Die }, EligibleIds = { Nazgul, WitchKing }
            };
            travel.Travel.Add(new TravelOption { Id = Nazgul, Origin = "gorgoroth", Flight = true, Targets = { "nurn", "osgiliath" } });
            travel.Travel.Add(new TravelOption { Id = WitchKing, Origin = "gorgoroth", Flight = true, Targets = { "nurn", "osgiliath" } });
            if (withMouth)
            {
                travel.EligibleIds.Add(Mouth);
                travel.Travel.Add(new TravelOption { Id = Mouth, Origin = "gorgoroth", Range = 3, Targets = { "nurn" } });
            }
            view.Options.Add(travel);
            return view;
        }
    }
}
