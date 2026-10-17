using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    public class ExpansionLobbyNetworkTests
    {
        [UnityTest] public IEnumerator AllEightModuleCombinationsStartThroughAcceptedLobbyControls() => Wait(StartAll());

        private static async Task StartAll()
        {
            for (var flags = 0; flags < 8; flags++)
            {
                var config = new MatchConfiguration { Lords = (flags & 1) != 0, Kings = (flags & 2) != 0, Fate = (flags & 4) != 0,
                    Council = (flags & 1) != 0, VisibleCorruption = (flags & 2) != 0, SequentialCorruption = (flags & 2) != 0,
                    RemovingRulerTokens = (flags & 3) == 3, NewCities = (flags & 4) != 0 };
                using (var room = await CreateNormalGame(config, StartFromLobby))
                {
                    Assert.IsNull(room.Sh.Board.View.Decision);
                    foreach (var client in new[] { room.Fp, room.Sh })
                        await InspectRules(room, client, config, flags);
                    if (config.Lords)
                    {
                        Click(room.Fp, "version-base.character.gandalf-grey");
                        Click(room.Fp, "starting-guide-base.character.strider");
                        await room.Confirm(room.Fp);
                        if (room.Fp.Board.View.Decision?.Kind == DecisionKind.CouncilHomelands)
                            await room.Confirm(room.Fp);
                    }
                    var revision = room.Fp.Board.View.Revision;
                    FellowshipGestures.Action(room.Fp, "fellowship-continue");
                    await room.At(revision + 1);
                    await room.Confirm(room.Sh);
                    Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
                    await Reconnect(room, room.Sh);
                    Assert.AreEqual(config.Lords, room.Sh.Board.View.Configuration.Lords);
                    Assert.AreEqual(config.NewCities, room.Sh.Board.View.Configuration.NewCities);
                }
            }
        }

        // UI-173: without Help the match rules are read from the board itself: the sovereigns of Kings and the layout.
        private static async Task InspectRules(MatchTestRoom room, MountedMatchClient client,
            MatchConfiguration configuration, int flags)
        {
            var revision = client.Board.View.Revision;
            Assert.AreEqual(configuration.Kings ? configuration.Fate ? 3 : 5 : 0, client.Board.View.Sovereigns.Count);
            await Layout(room, client, "rules-" + flags + "-" + client.Board.View.You, "t34");
            Assert.AreEqual(revision, client.Board.View.Revision, "Reading match rules must not perform a game action.");
        }

        private static async Task<RoomView> StartFromLobby(GameApi fp, GameApi sh, RoomView room, CancellationToken token)
        {
            using (var mounted = new MountedMatchClient(fp.Address.ToString(), "lobby", api: fp))
            {
                var screen = new RoomScreen();
                var root = mounted.Board.Root.parent;
                mounted.Board.Root.RemoveFromHierarchy();
                root.Add(screen.Root);
                screen.Update(room);
                Task<RoomView> request = null;
                screen.Configure += config => request = fp.Put<RoomView>($"api/rooms/{room.Id}/configuration",
                    new ConfigureRoomRequest { ExpectedVersion = screen.Room.Version, Configuration = config }, token);
                screen.Confirm += () => request = fp.Post<RoomView>($"api/rooms/{room.Id}/confirm",
                    new RoomRevisionRequest { ExpectedVersion = screen.Room.Version }, token);
                screen.Start += () => request = fp.Post<RoomView>($"api/rooms/{room.Id}/start",
                    new RoomRevisionRequest { ExpectedVersion = screen.Room.Version }, token);
                // Exercise a reversible configuration edit through the same UI event/HTTP contract as ClientFlow.
                foreach (var enabled in new[] { !room.Configuration.Fate, room.Configuration.Fate })
                {
                    screen.Root.Q<Toggle>("config-fate").value = enabled;
                    screen.Update(await request);
                }
                if (room.Configuration.NewCities != screen.Room.Configuration.NewCities)
                {
                    screen.Root.Q<Toggle>("config-new-cities").value = room.Configuration.NewCities;
                    screen.Update(await request);
                }
                Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
                await Task.Delay(80, token);
                CardNetworkTests.Click(screen.Root.Q<Button>("confirm-configuration"));
                screen.Update(await request);
                var version = new RoomRevisionRequest { ExpectedVersion = screen.Room.Version };
                await sh.Post<RoomView>($"api/rooms/{room.Id}/confirm", version, token);
                screen.Update(await fp.Get<RoomView>($"api/rooms/{room.Id}", token));
                Assert.IsTrue(screen.Root.Q<Button>("start-game").enabledSelf);
                await Task.Delay(80, token);
                CardNetworkTests.Click(screen.Root.Q<Button>("start-game"));
                return await request;
            }
        }
    }
}
