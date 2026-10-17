using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class UI10DraftSessionTests
    {
        [Test]
        public void DieSelectedBeforeActionSurvivesWaitingWithoutChoosingAction()
        {
            var session = new TurnDraftSession();
            var initial = Action("original");
            session.Update(initial);
            session.Draft.Change(payload => payload.DieId = "exact-die");
            session.Edited(initial);
            session.Update(Waiting());
            var next = Action("next");
            next.Dice.Add(new DieView
            {
                Id = "exact-die", Owner = Side.FreePeoples, Location = DieLocation.Available, Face = DieFace.Event
            });
            session.Update(next);
            Assert.IsNull(session.Draft.Kind);
            Assert.AreEqual("exact-die", session.Draft.Payload.DieId);
        }

        [Test]
        public void OpponentTurnParksIntentThenPreservesDeckWithoutReplacingSpentDie()
        {
            var session = Prepared();
            session.Update(Waiting());
            Assert.IsNull(session.Draft.Kind, "Waiting does not display obsolete action targets.");
            var updated = Action("next", "other-die");
            var notice = session.Update(updated);
            Assert.AreEqual(CommandKind.DrawCard, session.Draft.Kind);
            Assert.AreEqual("character", session.Draft.Payload.Deck);
            Assert.AreEqual("", session.Draft.Payload.DieId);
            Assert.That(notice, Does.Contain("Choices changed"));
        }

        [Test]
        public void LateReceiptClearsCarriedOriginalButKeepsNewMandatoryWindow()
        {
            var session = Prepared();
            session.Update(Action("next"));
            session.Complete(new GameCommand { DecisionId = "original" });
            Assert.IsNull(session.Draft.Kind);

            var mandatory = Action("discard");
            mandatory.Decision.Kind = DecisionKind.DiscardCards;
            session.Update(mandatory);
            session.Draft.Choose(CommandKind.DiscardCards);
            session.Edited(mandatory);
            session.Draft.Toggle("card");
            session.Complete(new GameCommand { DecisionId = "original" });
            Assert.AreEqual(CommandKind.DiscardCards, session.Draft.Kind);
            CollectionAssert.AreEqual(new[] { "card" }, session.Draft.Payload.Ids);
        }

        [Test]
        public void AcceptedReceiptDuringWaitingPreventsRestoringCompletedIntent()
        {
            var session = Prepared();
            session.Update(Waiting());
            session.Complete(new GameCommand { DecisionId = "original" });
            session.Update(Action("next"));
            Assert.IsNull(session.Draft.Kind);
        }

        [Test]
        public void NewTurnOrNestedBattleDiscardsParkedIntent()
        {
            foreach (var battle in new[] { false, true })
            {
                var session = Prepared();
                var waiting = Waiting();
                if (battle)
                    waiting.Battle = new BattleView { Id = "new-battle" };
                else
                    waiting.Turn++;
                session.Update(waiting);
                session.Update(Action("next"));
                Assert.IsNull(session.Draft.Kind);
            }
        }

        private static TurnDraftSession Prepared()
        {
            var session = new TurnDraftSession();
            var initial = Action("original");
            session.Update(initial);
            session.Draft.Choose(CommandKind.DrawCard, "exact-die");
            session.Draft.Change(payload => payload.Deck = "character");
            session.Edited(initial);
            return session;
        }

        private static PlayerView Waiting()
        {
            var view = Action("opponent");
            view.Decision = null;
            view.DecisionOwner = Side.Shadow;
            view.Options.Clear();
            return view;
        }

        private static PlayerView Action(string decision, string die = "exact-die") => new PlayerView
        {
            MatchId = "match", Turn = 1, Phase = GamePhase.ActionResolution,
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = decision, Kind = DecisionKind.Action },
            Options = { new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { die }, Targets = { "character" } } }
        };
    }
}
