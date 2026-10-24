using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-312 with two real clients on `fp-event-draw`: the Shadow's board shows the Free Peoples' draw as a card back at
    // their hand count, and neither the full playback nor a Skip marks the action's pennant read.
    public sealed class OpponentResourcePresentationNetworkTests
    {
        [UnityTest]
        public IEnumerator PlaybackAndSkipLeaveTheOpponentsPennantsUnread() => Wait(Draw());

        private static async Task Draw()
        {
            var session = await TestScenarioRoom.Start("fp-event-draw");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var cost = fp.Board.View.Options.First(o => o.Kind == CommandKind.DrawCard).DrawCosts.First();
                await session.Send(Side.FreePeoples, CommandKind.DrawCard,
                    new CommandPayload { DieId = cost.DieId, Deck = "character", CardCost = cost.Face, AbilityId = cost.AbilityId });
                await Seen(session, () => sh.Playback.Holding && sh.Board.Root.Q(ResourcePresenter.PlateName) != null,
                    "The draw is a card back at the Free Peoples' hand count.");
                await session.Until(() => !sh.Playback.Holding);
                var drawn = Unread(sh);
                Assert.Greater(drawn, 0, "The full playback marks no pennant read.");

                await session.Until(() => sh.Board.View.DecisionOwner == Side.Shadow && sh.Board.View.Decision != null);
                var shadowDie = sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id;
                await session.Send(Side.Shadow, CommandKind.DiscardDie, new CommandPayload { DieId = shadowDie });
                await session.Until(() => fp.Board.View.DecisionOwner == Side.FreePeoples && fp.Board.View.Decision != null && !fp.Playback.Holding);
                var freeDie = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
                await session.Send(Side.FreePeoples, CommandKind.DiscardDie, new CommandPayload { DieId = freeDie });
                // The Skip medallion joins the layer on the next tick of the presentation.
                await Seen(session, () => sh.Playback.Presenting && sh.Presenters.Skip.Button.panel != null,
                    "The forfeit is presented with its Skip control.");
                PointerGestureEvents.ClickAt(sh.Presenters.Skip.Button);
                await session.Until(() => !sh.Playback.Holding);
                Assert.Greater(Unread(sh), drawn, "Skip marks no pennant read either.");
            }
            finally
            {
                await session.End();
            }
        }

        private static int Unread(MountedMatchClient client) => client.Activity.Model.Groups.Count;

        private static async Task Seen(TestScenarioRoom session, Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
            Assert.IsTrue(condition(), what);
        }
    }
}
