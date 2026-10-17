using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-166 (SIGNALS_UX A): the compact card and the pennant's tooltip tell the result of an action.
    public sealed class ActionResultTextTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();

        [Test]
        public void ArmyMoveNamesTheDestinationAndTheMovedForceInOneLine()
        {
            var step = Step(CommandKind.MoveArmies, Side.Shadow);
            step.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.March, From = "minas-tirith", To = "osgiliath",
                Participants = { Unit(UnitKind.Regular), Unit(UnitKind.Regular), Unit(UnitKind.Elite), Character("base.character.witch-king") }
            });
            var result = ActionResultText.For(new[] { step }, Data);
            Assert.AreEqual("Army move · Osgiliath", result.Header);
            Assert.AreEqual("2 Regular + 1 Elite + " + CharacterLibrary.Name("base.character.witch-king") + " · Minas Tirith → Osgiliath",
                result.Lines.Single());
            Assert.AreEqual(result.Header + "\n" + result.Lines[0], result.Tooltip);
            Assert.AreEqual("", result.CardId);
        }

        [Test]
        public void ResultsFollowTheirImportanceAndTheLastLineCountsTheRest()
        {
            var attack = Step(CommandKind.AttackArmy, Side.Shadow);
            attack.Context.BattleId = "battle";
            attack.Facts.BattlePlace = new BattlePlaceFact { RegionId = "edoras" };
            var retreat = Step(CommandKind.RetreatBattle, Side.FreePeoples, order: 1);
            retreat.Context.BattleId = "battle";
            retreat.Movements.Add(new ActionMovement { Kind = ActionMovementKind.Retreat, From = "edoras", To = "westemnet",
                Participants = { Unit(UnitKind.Regular, Side.FreePeoples) } });
            var after = Step(null, Side.Shadow, order: 2);
            after.Facts.Control.Add(new ActionControlChange { RegionId = "edoras", Before = Side.FreePeoples, After = Side.Shadow });
            after.Facts.Nations.Add(new ActionNationChange { Id = "rohan", PreviousStepsToWar = 1, StepsToWar = 0, Active = true, WasActive = true });
            after.Movements.Add(new ActionMovement { Kind = ActionMovementKind.March, From = "fords-of-isen", To = "helms-deep",
                Participants = { Unit(UnitKind.Regular) } });
            var result = ActionResultText.For(new[] { after, retreat, attack }, Data);
            Assert.AreEqual("Attack · Edoras", result.Header);
            Assert.AreEqual(ActionResultText.MaxLines, result.Lines.Count);
            Assert.AreEqual("Edoras captured · +" + Data.Regions["edoras"].VictoryPoints + " VP", result.Lines[0]);
            Assert.AreEqual("Battle at Edoras · Free Peoples retreats", result.Lines[1]);
            Assert.AreEqual("+2 more", result.Lines[2], "The march and the politics wait in the Log.");
            var fewer = ActionResultText.For(new[] { retreat, attack }, Data);
            CollectionAssert.AreEqual(new[] { "Battle at Edoras · Free Peoples retreats" }, fewer.Lines);
        }

        [Test]
        public void MusterPoliticsAndCharactersTellWhatChangedOnTheBoard()
        {
            var muster = Step(CommandKind.Recruit, Side.FreePeoples);
            muster.Facts.Figures.Add(new ActionFigureChange { After = Unit(UnitKind.Regular, Side.FreePeoples), From = "reserve", To = "edoras" });
            muster.Facts.Figures.Add(new ActionFigureChange { After = Unit(UnitKind.Regular, Side.FreePeoples), From = "reserve", To = "edoras" });
            var result = ActionResultText.For(new[] { muster }, Data);
            Assert.AreEqual("Muster · Edoras", result.Header);
            CollectionAssert.AreEqual(new[] { "+2 Regular · Edoras" }, result.Lines);

            var politics = Step(CommandKind.AdvancePolitics, Side.FreePeoples);
            politics.Facts.Nations.Add(new ActionNationChange { Id = "rohan", WasActive = false, Active = true, PreviousStepsToWar = 2, StepsToWar = 1 });
            result = ActionResultText.For(new[] { politics }, Data);
            Assert.AreEqual("Politics · Rohan", result.Header);
            CollectionAssert.AreEqual(new[] { "Rohan · Active · 1 step to war" }, result.Lines);

            var white = Step(CommandKind.EnterCharacter, Side.FreePeoples);
            white.Facts.Figures.Add(new ActionFigureChange { After = Character("base.character.gandalf-white", Side.FreePeoples), From = "reserve", To = "fangorn" });
            result = ActionResultText.For(new[] { white }, Data);
            CollectionAssert.AreEqual(new[] { CharacterLibrary.Name("base.character.gandalf-white") + " enters · Fangorn" }, result.Lines);
        }

        [Test]
        public void FellowshipMoveTellsTheHuntResultAndTheHuntBoxCountsAddedDice()
        {
            var move = Step(CommandKind.MoveFellowship, Side.FreePeoples);
            move.Facts.FellowshipBefore = new HuntOutcomeView { Hidden = true, Corruption = 3, Progress = 1, RegionId = "hollin" };
            var hunt = Step(null, Side.Shadow, order: 1);
            hunt.Context.HuntChainId = "hunt";
            hunt.Facts.Hunt = new ActionHuntFact { ChainId = "hunt" };
            hunt.Facts.FellowshipAfter = new HuntOutcomeView { Hidden = false, Corruption = 5, Progress = 0, RegionId = "moria" };
            var result = ActionResultText.For(new[] { move, hunt }, Data);
            Assert.AreEqual("Fellowship moved · Hunt", result.Header);
            CollectionAssert.AreEqual(new[] { "Corruption 3 → 5 · Revealed in Moria" }, result.Lines);

            var allocate = Step(CommandKind.AllocateHunt, Side.Shadow);
            allocate.Facts.Dice.Add(new ActionDieChange { Owner = Side.Shadow, From = DieLocation.Available, To = DieLocation.Hunt });
            allocate.Facts.Dice.Add(new ActionDieChange { Owner = Side.Shadow, From = DieLocation.Available, To = DieLocation.Hunt });
            CollectionAssert.AreEqual(new[] { "Hunt Box +2" }, ActionResultText.For(new[] { allocate }, Data).Lines);
        }

        [Test]
        public void OnlyARevealedCardIsShownAndPrivateCardsNeverReachTheResult()
        {
            var revealed = CardLibrary.Cards.Keys.First();
            var secret = CardLibrary.Cards.Keys.First(id => CardLibrary.Title(id) != CardLibrary.Title(revealed));
            var play = Step(CommandKind.PlayEvent, Side.Shadow);
            play.Text = "Shadow played an Event card.";
            play.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.CombatCommit, Id = secret });
            play.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Draw, Id = secret });
            play.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Event, Id = revealed });
            var result = ActionResultText.For(new[] { play }, Data);
            Assert.AreEqual(revealed, result.CardId);
            Assert.AreEqual("Event · " + CardLibrary.Title(revealed), result.Header);
            Assert.AreEqual(CardLibrary.Title(revealed), result.CardTitle);
            CollectionAssert.AreEqual(new[] { "Played an Event card." }, result.Lines, "Without a recorded effect the action's own text remains.");
            StringAssert.DoesNotContain(CardLibrary.Title(secret), result.Header + string.Join(" ", result.Lines));

            play.Facts.Cards.RemoveAt(2);
            Assert.AreEqual("", ActionResultText.For(new[] { play }, Data).CardId, "A committed or drawn card is not revealed.");
        }

        [Test]
        public void AnActionWithoutEffectKeepsItsRecordWithoutTheSide()
        {
            var discard = Step(CommandKind.DiscardDie, Side.Shadow);
            discard.Text = "Shadow discarded a die.";
            discard.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Face = DieFace.Muster });
            var result = ActionResultText.For(new[] { discard }, Data);
            Assert.AreEqual("Die discarded", result.Header);
            CollectionAssert.AreEqual(new[] { "Discarded a die." }, result.Lines);
            Assert.AreEqual(DieFace.Muster, result.Die);
        }

        [Test]
        public void ThePennantTooltipReadsTheResultOfTheUnreadSteps()
        {
            var model = new ActionNoticeModel(new NotificationReadStore(), Data) { You = Side.FreePeoples };
            var move = Step(CommandKind.MoveArmies, Side.Shadow);
            move.Movements.Add(new ActionMovement { Kind = ActionMovementKind.March, From = "minas-tirith", To = "osgiliath",
                Participants = { Unit(UnitKind.Regular) } });
            model.Merge(new ActionLogPage { Steps = { move } });
            var group = model.Groups.Single();
            Assert.AreEqual("Army move · Osgiliath\n1 Regular · Minas Tirith → Osgiliath", group.Result.Tooltip);
            Assert.IsTrue(group.Latest.Request.Compact, "A notice opens the compact card.");
            Assert.IsFalse(group.Latest.Request.Full().Compact);
            Assert.AreEqual(group.Latest.Request.ActionId, group.Latest.Request.Full().ActionId);
        }

        private static ActionLogStep Step(CommandKind? command, Side actor, int order = 0) => new ActionLogStep
        {
            Id = "step-" + order + "-" + command, ActionId = "action", Revision = 10, Order = order, Actor = actor, Command = command,
            Text = command.HasValue ? ActionCommandText.Describe(command.Value) : ""
        };

        private static ActionParticipant Unit(UnitKind kind, Side side = Side.Shadow) =>
            new ActionParticipant { Side = side, NationId = side == Side.Shadow ? "sauron" : "rohan", UnitKind = kind };

        private static ActionParticipant Character(string id, Side side = Side.Shadow) =>
            new ActionParticipant { Side = side, DefinitionId = id };
    }
}
