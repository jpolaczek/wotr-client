using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // QUAL-22 (owner: the client sometimes hangs on Waiting for server): the board held while what just happened is
    // presented (UI-304's playback gate) says so and points to Skip; "Waiting for the server…" means the server only.
    public sealed class PresentationHoldWaitTests
    {
        [UnityTest]
        public IEnumerator APresentationHoldIsNotCalledAServerWait()
        {
            var view = new BoardFixture().View;
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            client.SetOnline();
            yield return Settle();
            Assert.IsFalse(Shows(client, DecisionTray.PresentationWait), "Nothing is presented yet.");
            client.Interaction.Hold(true);
            yield return Settle();
            Assert.IsTrue(Shows(client, DecisionTray.PresentationWait), "The hold names the presentation.");
            Assert.IsFalse(Shows(client, DecisionTray.ServerWait), "It is no wait for the server.");
            client.Interaction.Hold(false);
            yield return Settle();
            Assert.IsFalse(Shows(client, DecisionTray.PresentationWait), "The words go with the hold.");
        }

        private static bool Shows(MountedMatchClient client, string text) =>
            client.Board.Root.Query<TextElement>().ToList().Any(element => element.text == text && element.resolvedStyle.display != DisplayStyle.None);

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 4; i++)
                yield return null;
        }
    }
}
