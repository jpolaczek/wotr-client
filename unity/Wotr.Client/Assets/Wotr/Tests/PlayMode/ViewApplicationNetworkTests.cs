using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // QUAL-09: with the real backend, a failing observer neither moves the applied version nor reads as a network
    // loss; once it stops failing, the next update notice applies the same view and the board catches up.
    public sealed class ViewApplicationNetworkTests
    {
        [UnityTest]
        public IEnumerator ADrawingFailureIsReportedAndTheNextNoticeRestoresTheBoard() => Wait(Run());

        private static async Task Run()
        {
            using var room = await Connect("turn-pass", ScenarioKind.BaseAcceptance);
            var shadow = room.Sh;
            var failing = true;
            void Fail(PlayerView view)
            {
                if (failing)
                    throw new InvalidOperationException("injected drawing failure");
            }
            shadow.Connection.Updated += Fail;
            try
            {
                await room.Until(() => room.Fp.Board.Console.Actions.Pass.enabledSelf);
                await room.Settle();
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Select(room.Fp, "Pass");
                await room.Until(() => room.Fp.Board.View.Revision == revision + 1);
                await room.Until(() => shadow.Board.Connection.text.StartsWith("Display error", StringComparison.Ordinal));
                Assert.AreEqual(revision, shadow.Connection.Revision, "The failed view is not recorded as applied.");
                failing = false;
                await room.Until(() => shadow.Connection.Revision == revision + 1);
                await room.Until(() => shadow.Board.Connection.text == "Live");
                Assert.AreEqual(revision + 1, shadow.Board.View.Revision);
                Assert.AreEqual(DecisionKind.Action, shadow.Board.View.Decision.Kind, "The board shows the Shadow's action.");
            }
            finally
            {
                shadow.Connection.Updated -= Fail;
            }
        }
    }
}
