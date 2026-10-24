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
    // UI-310 with two real clients: the Free Peoples move the Fellowship with a Character die and the Shadow's board
    // presents the Hunt in its window before the progress moves, then shows the latest tracks once.
    public sealed class OpponentHuntPresentationNetworkTests
    {
        [UnityTest]
        public IEnumerator TheShadowWatchesTheHuntBeforeTheFellowshipsProgress() => Wait(Move());

        private static async Task Move()
        {
            var session = await TestScenarioRoom.Start("fp-character-move-fellowship");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var progress = sh.Board.Fellowship.Status.Q<Label>("fellowship-status-progress");
                var before = progress.text;
                var start = sh.Board.View.Fellowship.Progress;
                var die = fp.Board.View.Options.First(o => o.Kind == CommandKind.MoveFellowship).EligibleDice.First();
                await session.Send(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = die });
                await session.Until(() => sh.Playback.Holding && sh.Board.Hunt.Root.style.display.value == DisplayStyle.Flex);
                Assert.AreEqual(before, progress.text, "The Hunt opens before the progress moves.");
                await session.Until(() => sh.Board.Hunt.Root.Q(className: "hunt-die") != null || !sh.Playback.Holding);
                await session.Until(() => !sh.Playback.Holding);
                var fellowship = sh.Board.View.Fellowship;
                if (fellowship.Hidden)
                    Assert.AreEqual(start + 1, fellowship.Progress, "A hidden move adds one progress.");
                Assert.IsFalse(sh.Board.TracksStaged, "The released view does not stage the tracks again.");
                Assert.IsNotNull(sh.Board.View.HuntReport, "The Hunt is recorded.");
            }
            finally
            {
                await session.End();
            }
        }
    }
}
