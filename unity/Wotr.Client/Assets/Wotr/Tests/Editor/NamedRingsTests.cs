using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-268: the named Elven Rings of Lords keep fixed sockets in their own art; Nenya's and Vilya's sockets glow and
    // answer their Keeper abilities; spending Vilya's die is a console button.
    public sealed class NamedRingsTests
    {
        [Test]
        public void TheNamedRingsKeepFixedSocketsInTheirOwnArt()
        {
            var rings = new ConsoleRings();
            rings.Apply(Lords(), "", true, null, CommandKind.UseRing);
            var classes = Enumerable.Range(0, ConsoleRings.Count)
                .Select(index => NamedRings.Keys.Single(key => rings.Socket(index).ClassListContains("ring-" + key)));
            CollectionAssert.AreEqual(new[] { "vilya", "nenya", "narya" }, classes);
            Assert.AreSame(rings.Socket(1), rings.For(NamedRings.Nenya));
            StringAssert.Contains("Keeper · in the Hunt Galadriel", rings.For(NamedRings.Nenya).tooltip);
            StringAssert.Contains("on its row in the Politics panel", rings.For(NamedRings.Narya).tooltip);
            Assert.IsFalse(rings.Socket(0).ClassListContains(ConsoleRings.KeeperReadyClass));
        }

        [Test]
        public void TheNenyaSocketAnswersTheTileStepAndTheHuntListLeavesItOut()
        {
            var view = Lords();
            view.Hunt = new HuntView();
            view.Decision = new DecisionView { Id = "d", Kind = DecisionKind.Reaction };
            view.Options.Add(new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { NamedRings.NenyaReaction } });
            Assert.IsTrue(NamedRings.OnSocket(view, NamedRings.NenyaReaction));
            Assert.IsFalse(NamedRings.KeeperReady(view, NamedRings.Vilya));
            var rings = new ConsoleRings();
            rings.Apply(view, "", true, null, CommandKind.UseRing);
            var nenya = rings.For(NamedRings.Nenya);
            Assert.IsTrue(nenya.ClassListContains(ConsoleRings.KeeperReadyClass));
            Assert.IsTrue(nenya.enabledSelf);
            StringAssert.Contains("Click to remove this Eye tile", nenya.tooltip);
            rings.Apply(view, "", false, null, CommandKind.UseRing);
            Assert.IsFalse(rings.For(NamedRings.Nenya).enabledSelf, "A step being sent lights nothing.");
            view.Rings.Single(r => r.Id == NamedRings.Nenya).Owner = Side.Shadow;
            rings.Apply(view, "", true, null, CommandKind.UseRing);
            Assert.IsFalse(rings.For(NamedRings.Nenya).ClassListContains(ConsoleRings.KeeperReadyClass), "A captured Nenya has no Keeper.");
            Assert.IsTrue(rings.For(NamedRings.Nenya).ClassListContains("ring-captured"));
        }

        [Test]
        public void VilyaIsKeptOnItsSocketAndSpendingTheDieIsAConsoleButton()
        {
            var view = Lords();
            view.Decision = new DecisionView { Id = "d", Kind = DecisionKind.KeepActionDie };
            view.Options.Add(new ActionOption
                { Kind = CommandKind.UseVilya, Enabled = true, EligibleIds = { NamedRings.Vilya }, EligibleDice = { "paid" }, Minimum = 1, Maximum = 1 });
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclineVilya, Enabled = true });
            Assert.AreEqual(DecisionHost.Rings, DecisionRoutes.Of(view));
            Assert.IsTrue(NamedRings.KeeperReady(view, NamedRings.Vilya));
            var draft = new TurnDraft();
            Assert.IsTrue(ExpansionActions.Ring(view, draft, NamedRings.Vilya));
            Assert.AreEqual(CommandKind.UseVilya, draft.Kind);
            Assert.AreEqual("paid", draft.Payload.DieId, "One paid die makes the socket's click complete.");
            Assert.AreEqual(NamedRings.Vilya, draft.Payload.RingId);
            var spend = CardConsoleOptions.For(view, draft).Single();
            Assert.AreEqual("action-DeclineVilya", spend.Name);
            Assert.AreEqual("Spend the die", spend.Label);
            Assert.AreEqual(CommandKind.DeclineVilya, spend.Command);
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, draft), "The resource panel does not host Vilya.");
        }

        private static PlayerView Lords() => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Configuration = new MatchConfiguration { Lords = true },
            Rings =
            {
                new RingView { Id = NamedRings.Narya, Owner = Side.FreePeoples },
                new RingView { Id = NamedRings.Nenya, Owner = Side.FreePeoples },
                new RingView { Id = NamedRings.Vilya, Owner = Side.FreePeoples }
            }
        };
    }
}
