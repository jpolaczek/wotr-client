using System.Linq;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-106: an action card and a notice keep only what is needed to understand what happened.
    public sealed class ActionCardEffectsTests
    {
        private const string Card = "base.fp.character.18";

        [Test]
        public void EffectsLeaveOutSourceDiceBudgetAndTheActingSide()
        {
            var step = Step();
            var data = BoardPresentation.Load();
            var effects = ActionFactText.Effects(step, data).ToList();
            Assert.IsFalse(effects.Any(line => line.StartsWith("Source ·")), "No source");
            Assert.IsFalse(effects.Any(line => line.Contains("Unrolled") || line.Contains("Will Of The West")), "No die");
            Assert.IsFalse(effects.Any(line => line.Contains("remaining")), "No action budget");
            CollectionAssert.Contains(effects, "Event · " + CardLibrary.Title(Card), "The acting side is not repeated");
            CollectionAssert.Contains(effects, "Shadow · Discard · 1 character card", "The other side is named");

            var record = ActionFactText.Lines(step, data).ToList();
            Assert.IsTrue(record.Any(line => line.StartsWith("Source ·")), "The history list keeps the full record.");
            Assert.IsTrue(record.Any(line => line.Contains("remaining")));
        }

        [Test]
        public void DiceAllocatedToTheHuntRemainAnEffect()
        {
            var step = new ActionLogStep { Id = "allocate", Actor = Side.Shadow, Text = "Turn 3: ActionResolution.",
                Facts = new ActionFacts { Dice =
                {
                    new ActionDieChange { Id = "a", Owner = Side.Shadow, From = DieLocation.Available, To = DieLocation.Hunt },
                    new ActionDieChange { Id = "b", Owner = Side.Shadow, From = DieLocation.Available, To = DieLocation.Hunt }
                } } };
            CollectionAssert.AreEqual(new[] { "Hunt Box +2" }, ActionFactText.Effects(step, BoardPresentation.Load()).ToArray());
        }

        [Test]
        public void TheNoticeSentenceOmitsTheSideItsEnamelShows()
        {
            var step = Step();
            step.Command = CommandKind.PlayEvent;
            var data = BoardPresentation.Load();
            StringAssert.StartsWith("Free Peoples · ", ActionNoticeText.Short(step, data));
            Assert.AreEqual(ActionNoticeText.Short(step, data), "Free Peoples · " + ActionNoticeText.Sentence(step, data));
            StringAssert.DoesNotContain("Free Peoples", ActionNoticeText.Sentence(step, data));
        }

        private static ActionLogStep Step() => new ActionLogStep
        {
            Id = "event", Actor = Side.FreePeoples, Text = "Free Peoples played an Event",
            Facts = new ActionFacts
            {
                SourceId = Card,
                Dice = { new ActionDieChange { Id = "west", Owner = Side.FreePeoples, Before = DieFace.WillOfTheWest, After = null,
                    From = DieLocation.Available, To = DieLocation.Available } },
                Cards =
                {
                    new ActionCardFact { Owner = Side.FreePeoples, Use = ActionCardUse.Event, Id = Card },
                    new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Discard, Deck = "character", Count = 1 }
                },
                Budget = new ActionBudget { Completed = 1, Remaining = 1 }
            }
        };
    }
}
