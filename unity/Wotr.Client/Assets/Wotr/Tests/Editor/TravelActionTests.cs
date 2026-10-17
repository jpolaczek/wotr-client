using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class TravelActionTests
    {
        [Test]
        public void GroupedTravelCanPrepareAnExplicitStayAtTheSelectedOrigin()
        {
            var view = View();
            var context = new ActionDieContext();
            context.Sync(view, "character");
            var draft = new TurnDraft();
            Assert.IsTrue(TravelActions.Select(view, draft, context, "gimli"));
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "hollin"));
            CollectionAssert.AreEqual(new[] { "hollin" }, TravelDraft.Route(draft));
            CollectionAssert.AreEqual(new[] { "gimli" }, TravelActions.Selected(draft));
            Assert.IsTrue(TravelActions.End(draft, view, "hollin"));
            Assert.AreEqual("character", draft.Payload.DieId);
            Assert.AreEqual(0, view.Revision);
        }

        [Test]
        public void ANewSourceReplacesOnlyTheUnsentGroupAndPreservesTheChosenDie()
        {
            var view = View();
            var context = new ActionDieContext();
            context.Sync(view, "character");
            var draft = new TurnDraft();
            Assert.IsTrue(TravelActions.Select(view, draft, context, "gimli"));
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "moria"));
            Assert.IsTrue(TravelActions.Select(view, draft, context, "legolas"));
            Assert.AreEqual("character", draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
            Assert.AreEqual(1, draft.Payload.CompanionMoves.Count);
            CollectionAssert.AreEqual(new[] { "legolas" }, TravelActions.Selected(draft));
            Assert.IsEmpty(TravelDraft.Route(draft));
            Assert.AreEqual("hollin", view.Characters.Single(c => c.Id == "gimli").Location);
            Assert.IsFalse(TravelActions.End(draft, view, "moria"), "No automatic route to a distant destination.");
            Assert.IsTrue(TravelActions.End(draft, view, "dale"), "Explicit endpoint at the origin means stay.");
        }

        [Test]
        public void AFlightHasOnlyTwoEndpointsAndCannotReuseASavedFigure()
        {
            var view = View();
            view.You = Side.Shadow;
            view.DecisionOwner = view.You;
            view.Decision.Kind = DecisionKind.ActionSequence;
            view.Dice[0].Owner = view.You;
            view.Dice[0].Location = DieLocation.Resolving;
            view.Options[0].Kind = CommandKind.MoveShadowCharacters;
            view.Options[0].EligibleIds.Remove("legolas");
            view.Options[0].Travel.RemoveAll(t => t.Id == "legolas");
            view.Options[0].Travel[0].Flight = true;
            view.ActionSequence = new ActionSequenceView { Kind = CommandKind.MoveShadowCharacters, Owner = view.You,
                DieId = "character", Face = DieFace.Character, Remaining = 1, UsedIds = { "legolas" } };
            var context = new ActionDieContext();
            context.Sync(view, "");
            var draft = new TurnDraft();
            Assert.IsFalse(TravelActions.Select(view, draft, context, "legolas"));
            Assert.IsTrue(TravelActions.Select(view, draft, context, "gimli"));
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "moria"));
            Assert.IsTrue(TravelActions.End(draft, view, "moria"));
            CollectionAssert.AreEqual(new[] { "hollin", "moria" }, TravelDraft.Route(draft));
            Assert.AreEqual("character", draft.Command(view).Payload.DieId);
        }

        [Test]
        public void CardJourneyUsesNoExtraDieAndPreservesItsSingleGroupLimit()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.CompanionSeparation;
            view.Options[0].EligibleDice.Clear();
            view.Options[0].MaximumGroups = 1;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions);
            var context = new ActionDieContext();
            Assert.IsTrue(TravelActions.Select(view, draft, context, "gimli"));
            Assert.IsTrue(TravelActions.End(draft, view, "hollin"));
            TravelDraft.NewGroup(draft);
            Assert.IsFalse(TravelActions.Select(view, draft, context, "gimli"), "A figure cannot belong to two groups.");
            Assert.IsFalse(TravelActions.Select(view, draft, context, "legolas"), "The card permits only one group.");
            TravelDraft.SelectGroup(draft, 0);
            Assert.IsTrue(TravelActions.Select(view, draft, context, "legolas"));
            Assert.AreEqual(1, draft.Payload.CompanionMoves.Count);
            CollectionAssert.AreEqual(new[] { "legolas" }, draft.Payload.CompanionMoves[0].Ids);
            Assert.IsEmpty(draft.Payload.CompanionMoves[0].Route);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsFalse(draft.Payload.Stepwise, "The card retains its existing grouped decision.");
        }

        [Test]
        public void FellowshipRequiresAnExplicitCostButMandatoryChoicesDoNotInventOne()
        {
            var view = View();
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveFellowship, EligibleDice = { "character", "wild" } });
            view.Dice.Add(new DieView { Id = "wild", Owner = view.You, Face = DieFace.WillOfTheWest, Location = DieLocation.Available });
            var context = new ActionDieContext();
            var draft = new TurnDraft();
            Assert.IsFalse(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
            context.Sync(view, "wild");
            Assert.IsTrue(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
            Assert.AreEqual("wild", draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.Route);
            view.Decision.Kind = DecisionKind.Guide;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.SelectGuide, EligibleIds = { "gimli" } });
            context.Clear();
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "gimli"));
            Assert.AreEqual("gimli", draft.Payload.GuideId);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            view.DecisionOwner = Side.Shadow;
            Assert.IsNull(FellowshipActions.PortraitKind(view, context, "gimli"));
        }

        // UI-105: a click on a destination takes the shortest legal route within range; stops end a route,
        // blocked regions are avoided and ties go to the first neighbour by id.
        [Test]
        public void ADestinationClickTakesTheShortestLegalRouteWithinRange()
        {
            var view = View();
            var gimli = view.Options[0].Travel[0];
            gimli.Targets.AddRange(new[] { "rivendell", "dimrill-dale" });
            var data = BoardPresentation.Load();
            var context = new ActionDieContext();
            context.Sync(view, "character");
            var draft = new TurnDraft();
            Assert.IsTrue(TravelActions.Select(view, draft, context, "gimli"));
            Assert.IsTrue(TravelDraft.TargetRoute(draft.Kind));
            CollectionAssert.AreEquivalent(new[] { "hollin", "moria", "rivendell" }, TravelDraft.Reachable(draft, view, data),
                "Dimrill Dale lies beyond the stop at Moria.");
            Assert.IsFalse(TravelDraft.RouteTo(draft, view, data, "dimrill-dale"));
            Assert.IsFalse(TravelDraft.RouteTo(draft, view, data, "trollshaws"), "Only the option's targets are destinations.");
            Assert.IsTrue(TravelDraft.RouteTo(draft, view, data, "rivendell"));
            CollectionAssert.AreEqual(new[] { "hollin", "fords-of-bruinen", "rivendell" }, TravelDraft.Route(draft));
            Assert.IsTrue(TravelActions.End(draft, view, "rivendell"));
            gimli.BlockedRegions.Add("fords-of-bruinen");
            Assert.IsTrue(TravelDraft.RouteTo(draft, view, data, "rivendell"));
            CollectionAssert.AreEqual(new[] { "hollin", "trollshaws", "rivendell" }, TravelDraft.Route(draft), "A blocked region is avoided.");
            Assert.IsTrue(TravelDraft.RouteTo(draft, view, data, "hollin"));
            CollectionAssert.AreEqual(new[] { "hollin" }, TravelDraft.Route(draft), "The origin means stay.");
            gimli.Flight = true;
            Assert.IsTrue(TravelDraft.RouteTo(draft, view, data, "moria"));
            CollectionAssert.AreEqual(new[] { "hollin", "moria" }, TravelDraft.Route(draft), "A flight has two endpoints.");
            Assert.IsFalse(TravelDraft.TargetRoute(CommandKind.DeclareFellowship), "The Fellowship still draws its route.");
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "travel-context",
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "travel", Kind = DecisionKind.Action },
            Dice = { new DieView { Id = "character", Owner = Side.FreePeoples, Face = DieFace.Character, Location = DieLocation.Available } },
            Characters = { new CharacterView { Id = "gimli", Location = "hollin" }, new CharacterView { Id = "legolas", Location = "dale" } },
            Options = { new ActionOption { Kind = CommandKind.MoveCompanions, EligibleDice = { "character" }, EligibleIds = { "gimli", "legolas" }, Travel =
            {
                new TravelOption { Id = "gimli", Origin = "hollin", Range = 2, Targets = { "hollin", "moria" }, StopRegions = { "moria" } },
                new TravelOption { Id = "legolas", Origin = "dale", Range = 2, Targets = { "dale", "erebor" } }
            } } }
        };
    }
}
