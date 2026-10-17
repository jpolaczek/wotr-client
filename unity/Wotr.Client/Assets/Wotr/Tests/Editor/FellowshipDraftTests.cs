using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class FellowshipDraftTests
    {
        [TestCase(CommandKind.DeclareFellowship)]
        [TestCase(CommandKind.RevealFellowship)]
        public void LocationRouteUndoCancelAndStayPreserveTheRequiredDecision(CommandKind kind)
        {
            var view = View(kind);
            var option = view.Options[0];
            option.EligibleDice.Clear();
            option.Travel.Add(new TravelOption { Id = "fellowship", Origin = "hollin", Range = 2, Targets = { "hollin", "moria" } });
            var draft = new TurnDraft();
            draft.Choose(kind);
            var board = BoardPresentation.Load();
            Assert.IsFalse(TravelDraft.Step(draft, view, board, "minas-tirith"));
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "moria"));
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"), "A repeated region spends range, not a hidden shortcut.");
            Assert.IsFalse(TravelDraft.Step(draft, view, board, "moria"));
            FellowshipDraft.UndoRoute(draft);
            CollectionAssert.AreEqual(new[] { "hollin", "moria" }, draft.Payload.Route);
            FellowshipDraft.CancelRoute(draft);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.AreEqual(kind == CommandKind.RevealFellowship ? kind : (CommandKind?)null, draft.Kind);
            Assert.IsNotNull(view.Decision);
            draft.Choose(kind);
            option.Travel[0].Range = 0;
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"));
            CollectionAssert.AreEqual(new[] { "hollin" }, draft.Payload.Route);
            Assert.IsTrue(draft.Ready(option));
            Assert.IsFalse(TravelDraft.Step(draft, view, board, "moria"));
        }

        // UI-219: the step that uses all of the Progress completes the route and sends it; a shorter legal route and
        // a stay with Progress left wait for ✓, and an end that is not a legal destination never completes.
        [TestCase(CommandKind.DeclareFellowship)]
        [TestCase(CommandKind.RevealFellowship)]
        public void TheRouteIsCompleteOnlyWhenItUsesAllOfTheProgress(CommandKind kind)
        {
            var view = View(kind);
            var option = view.Options[0];
            option.EligibleDice.Clear();
            option.Travel.Add(new TravelOption { Id = "fellowship", Origin = "hollin", Range = 2, Targets = { "hollin", "moria" } });
            var board = BoardPresentation.Load();
            var draft = new TurnDraft();
            draft.Choose(kind);
            Assert.IsFalse(TravelActions.Complete(draft, view), "nothing drawn yet");
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"));
            Assert.IsFalse(TravelActions.Complete(draft, view), "a stay with Progress left waits for ✓");
            FellowshipDraft.CancelRoute(draft);
            draft.Choose(kind);
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "moria"));
            Assert.IsTrue(TravelDraft.EndAllowed(draft.Payload.Route, option.Travel[0]));
            Assert.IsFalse(TravelActions.Complete(draft, view), "a shorter legal route waits for ✓");
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"));
            Assert.IsTrue(TravelActions.Complete(draft, view), "all of the Progress is used");
            option.Travel[0].Targets.Remove("hollin");
            Assert.IsFalse(TravelActions.Complete(draft, view), "an end that is not a destination (a sanctuary) never sends");
            option.Travel[0].Targets.Add("hollin");
            option.Travel[0].Range = 0;
            FellowshipDraft.CancelRoute(draft);
            draft.Choose(kind);
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"));
            Assert.IsTrue(TravelActions.Complete(draft, view), "with no Progress the stay sends at once");
            Assert.IsFalse(TravelActions.Complete(new TurnDraft(), view));
        }

        [Test]
        public void PanelNeverGuessesTheCostAndKeepsTheExactCharacterOrWillDie()
        {
            var view = View(CommandKind.MoveFellowship);
            view.DecisionOwner = Side.FreePeoples;
            view.Fellowship.Hidden = true;
            view.Dice.Add(new DieView { Id = "will", Face = DieFace.WillOfTheWest, Owner = Side.FreePeoples, Location = DieLocation.Available });
            view.Options[0].EligibleDice.Add("will");
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            Assert.IsFalse(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
            Assert.AreEqual("Select a Character die", FellowshipActions.ActionHint(view, draft, context));
            Assert.IsEmpty(draft.Payload.DieId);
            foreach (var id in new[] { "character", "will" })
            {
                context.Sync(view, id);
                Assert.IsTrue(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
                Assert.AreEqual(id, draft.Payload.DieId);
                Assert.IsEmpty(draft.Payload.Route);
            }
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
            Assert.AreEqual("will", draft.Payload.DieId, "Rejected input cannot silently replace a cost.");
        }

        [Test]
        public void RevealedFellowshipUsesOnlyAdvertisedHideAndExplicitSpecialCost()
        {
            var view = View(CommandKind.HideFellowship);
            view.DecisionOwner = Side.FreePeoples;
            view.Fellowship.Hidden = false;
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            StringAssert.Contains("Hide first", FellowshipActions.ActionHint(view, draft, context));
            context.Sync(view, "character");
            Assert.IsNull(FellowshipActions.Option(view, context, CommandKind.MoveFellowship));
            Assert.IsTrue(FellowshipActions.Choose(view, draft, context, CommandKind.HideFellowship));
            Assert.AreEqual("character", draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.AbilityId);
            view.Decision = null;
            Assert.IsFalse(FellowshipActions.Choose(view, draft, context, CommandKind.HideFellowship));
        }

        [Test]
        public void MovementNeedsNoRouteAndAValuableDieIsNeverChosenAutomatically()
        {
            var view = View(CommandKind.MoveFellowship);
            var option = view.Options[0];
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.Route);
            view.Dice.Add(new DieView { Id = "wild", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.Available, Face = DieFace.WillOfTheWest });
            option.EligibleDice.Add("wild");
            draft.Clear();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsFalse(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.DieId);
        }

        [Test]
        public void RouteChoicesPreserveIntermediateSanctuariesAndNeverChooseAPathOrStay()
        {
            var view = View(CommandKind.RevealFellowship);
            var option = view.Options[0];
            option.EligibleDice.Clear();
            option.Travel.Add(new TravelOption { Id = "fellowship", Origin = "fords-of-bruinen", Range = 3, Targets = { "hollin", "moria" } });
            var draft = new TurnDraft();
            var board = BoardPresentation.Load();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsFalse(draft.Ready(option));
            Assert.IsTrue(TravelDraft.Next(draft, view, board).Contains("rivendell"), "Sanctuary may be traversed even when it is an illegal reveal destination.");
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "hollin"));
            Assert.IsTrue(TravelDraft.Step(draft, view, board, "moria"));
            Assert.IsTrue(draft.Ready(option));
            var command = draft.Command(view);
            CollectionAssert.AreEqual(new[] { "fords-of-bruinen", "hollin", "moria" }, command.Payload.Route);
            draft.Back();
            CollectionAssert.AreEqual(new[] { "fords-of-bruinen", "hollin" }, draft.Payload.Route);
            Assert.AreEqual(3, command.Payload.Route.Count, "Command owns an independent copy.");
            Assert.IsFalse(TravelDraft.Step(draft, view, board, "minas-tirith"));
            option.Travel[0].Range = 0;
            draft.Reconcile(view);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsEmpty(TravelDraft.Next(draft, view, board));
        }

        [Test]
        public void GroupsCannotReuseFiguresOrExtendTravelPastAStopAndUndoOwnsItsLists()
        {
            var view = View(CommandKind.MoveCompanions);
            var option = view.Options[0];
            foreach (var id in new[] { "gimli", "legolas" })
            {
                option.EligibleIds.Add(id);
                option.Travel.Add(new TravelOption { Id = id, Origin = "hollin", Range = 2, Targets = { "hollin", "moria", "fords-of-bruinen" }, StopRegions = { "moria" } });
                view.Characters.Add(new CharacterView { Id = id, Location = "hollin", Level = 2 });
            }
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            TravelDraft.ToggleCompanion(draft, view, option, "gimli");
            TravelDraft.Step(draft, view, BoardPresentation.Load(), "moria");
            Assert.IsEmpty(TravelDraft.Next(draft, view, BoardPresentation.Load()));
            TravelDraft.NewGroup(draft);
            Assert.IsFalse(TravelDraft.CanToggle(draft, view, option, "gimli"));
            TravelDraft.ToggleCompanion(draft, view, option, "legolas");
            Assert.IsFalse(draft.Ready(option));
            TravelDraft.Stay(draft, option);
            Assert.IsTrue(draft.Ready(option));
            var snapshot = JsonConvert.SerializeObject(draft.Command(view).Payload);
            var fork = draft.Fork();
            fork.Payload.CompanionMoves[0].Ids.Clear();
            Assert.AreEqual(snapshot, JsonConvert.SerializeObject(draft.Command(view).Payload));
            draft.Back();
            Assert.IsFalse(draft.Ready(option));
            Assert.IsEmpty(TravelDraft.Group(draft).Route);
        }

        [Test]
        public void DamageHasNoDefaultChoiceAndRandomNeverSendsAPortraitId()
        {
            var view = View(CommandKind.ResolveHuntDamage);
            var option = view.Options[0];
            option.EligibleDice.Clear();
            option.HuntResponses.AddRange(new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsFalse(draft.Ready(option));
            FellowshipDraft.Respond(draft, HuntResponse.RandomCompanion);
            Assert.IsTrue(draft.Ready(option));
            var command = draft.Command(view);
            Assert.AreEqual(HuntResponse.RandomCompanion, command.Payload.HuntResponse);
            Assert.IsEmpty(command.Payload.Ids);
            draft.Back();
            Assert.IsFalse(draft.Ready(option));
            FellowshipDraft.Respond(draft, HuntResponse.Guide);
            option.HuntResponses.Remove(HuntResponse.Guide);
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
            Assert.IsNull(draft.HuntChoice);
        }

        [Test]
        public void RerollsAreOptionalAndOnlyAllowTheAdvertisedFailuresAndLimit()
        {
            var view = View(CommandKind.RerollHunt);
            var option = view.Options[0];
            option.EligibleDice.Clear();
            option.EligibleIds.AddRange(new[] { "failed-one", "failed-two" });
            option.Maximum = 1;
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsTrue(draft.Ready(option));
            FellowshipDraft.ToggleReroll(draft, option, "success");
            Assert.IsEmpty(draft.Payload.Ids);
            FellowshipDraft.ToggleReroll(draft, option, "failed-one");
            FellowshipDraft.ToggleReroll(draft, option, "failed-two");
            CollectionAssert.AreEqual(new[] { "failed-one" }, draft.Payload.Ids);
        }

        private static PlayerView View(CommandKind kind) => new PlayerView
        {
            MatchId = "fixture",
            You = Side.FreePeoples,
            Decision = new DecisionView { Id = "decision", Kind = DecisionKind.Action },
            Options = { new ActionOption { Kind = kind, EligibleDice = { "character" } } },
            Dice = { new DieView { Id = "character", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.Available, Face = DieFace.Character } }
        };
    }
}
