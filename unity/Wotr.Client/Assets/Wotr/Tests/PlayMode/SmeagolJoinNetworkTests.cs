using System.Collections;
using System.Collections.Generic;
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
    // UI-462 (owner, 2026-11-03: "we need a graphical animation presenting Sméagol joining the Fellowship") with two real
    // clients: in lords-smeagol-joins the Free Peoples move the Fellowship with the Character die and the Hunt draws
    // Sméagol's tile. Both boards — the mover's own too, as the Hunt is a shared resolution — fly his portrait from the
    // tile into the roster, where he stands with the Guide's badge after the release.
    public sealed class SmeagolJoinNetworkTests
    {
        private const string Scenario = "lords-smeagol-joins";
        private const string Smeagol = "lords.character.smeagol";

        [UnityTest] public IEnumerator BothPlayersSeeSmeagolFlyIntoTheRoster() => Wait(Join());

        private static async Task Join()
        {
            var session = await TestScenarioRoom.Start(Scenario);
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var die = fp.Board.View.Options.First(o => o.Kind == CommandKind.MoveFellowship).EligibleDice.First();
                var seen = new HashSet<Side>();
                var saved = false;
                await session.Send(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = die });
                await session.Until(() =>
                {
                    Watch(fp, Side.FreePeoples, seen);
                    Watch(sh, Side.Shadow, seen);
                    if (!saved)
                        saved = SaveInFlight(sh);
                    return seen.Count == 2 && (saved || Flying(sh) == null);
                });
                await session.Until(() => Guiding(sh, "smeagol") || !sh.Playback.Holding);
                LayoutTests.Save(sh.Viewport, "ui462-network-landed.png");
                await session.Until(() => !fp.Playback.Holding && !sh.Playback.Holding);
                await session.Settle();
                Assert.AreEqual(Smeagol, fp.Board.View.Fellowship.Guide);
                AssertGuide(fp, Smeagol);
                AssertGuide(sh, "smeagol");
            }
            finally
            {
                await session.End();
            }
        }

        // The flying portrait on a board.
        private static void Watch(MountedMatchClient client, Side side, ISet<Side> seen)
        {
            if (Flying(client) != null)
                seen.Add(side);
        }

        // A frame of the portrait on its way, out of the tile and fully shown.
        private static bool SaveInFlight(MountedMatchClient client)
        {
            if (Flying(client) is not { } portrait || portrait.resolvedStyle.opacity < 1)
                return false;
            LayoutTests.Save(client.Viewport, "ui462-network-flight.png");
            return true;
        }

        private static VisualElement Flying(MountedMatchClient client) =>
            client.Board.Dissolve.Root.Q(className: FellowshipJoin.PortraitClass);

        private static bool Guiding(MountedMatchClient client, string id) =>
            client.Board.Fellowship.Root.Q("fellowship-portrait-" + id)?.ClassListContains("current-guide") == true;

        private static void AssertGuide(MountedMatchClient client, string id)
        {
            var portrait = client.Board.Fellowship.Root.Q("fellowship-portrait-" + id);
            Assert.IsNotNull(portrait, "He stands in the roster: " + client.Board.View.You);
            Assert.IsTrue(portrait.ClassListContains("current-guide"), "With the Guide's badge.");
            Assert.AreEqual(1f, portrait.resolvedStyle.opacity, "Shown.");
        }
    }
}
