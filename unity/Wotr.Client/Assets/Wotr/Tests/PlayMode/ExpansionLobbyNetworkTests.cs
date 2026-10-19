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
            // UI-263: the expansions' crests after the phase and in the Victory header.
            var modules = RulesChoices.ModuleIds.Where(module => RulesChoices.IsOn(configuration, module)).Select(m => "crest-" + m);
            CollectionAssert.AreEqual(modules, client.Board.TopBar.Crests.Children().Select(c => c.name));
            CollectionAssert.AreEqual(modules, client.Board.Victory.Root.Q("victory-crests").Children().Select(c => c.name));
            await Layout(room, client, "rules-" + flags + "-" + client.Board.View.You, "t34");
            Assert.AreEqual(revision, client.Board.View.Revision, "Reading match rules must not perform a game action.");
        }

        private static async Task<RoomView> StartFromLobby(GameApi fp, GameApi sh, RoomView room, CancellationToken token)
        {
            using (var mounted = new MountedMatchClient(fp.Address.ToString(), "lobby", api: fp))
            {
                // UI-263: the waiting room's rules strip, Ready and Start, over the same HTTP calls as WaitingRoomController.
                var screen = new WaitingRoomScreen();
                var root = mounted.Board.Root.parent;
                mounted.Board.Root.RemoveFromHierarchy();
                root.Add(screen.Root);
                screen.Update(room);
                Task<RoomView> request = null;
                screen.Configure += config => request = fp.Put<RoomView>($"api/rooms/{room.Id}/configuration",
                    new ConfigureRoomRequest { ExpectedVersion = screen.Room.Version, Configuration = config }, token);
                screen.Ready += ready => request = fp.Put<RoomView>($"api/rooms/{room.Id}/ready",
                    new RoomReadyRequest { ExpectedVersion = screen.Room.Version, Ready = ready }, token);
                screen.Start += () => request = fp.Post<RoomView>($"api/rooms/{room.Id}/start",
                    new RoomRevisionRequest { ExpectedVersion = screen.Room.Version }, token);
                // A reversible edit: Fate's medallion twice, then New Cities back to the wanted rules.
                for (var click = 0; click < 2; click++)
                {
                    await Task.Delay(80, token);
                    CardNetworkTests.Click(screen.Rules.Medallion("fate"));
                    screen.Update(await request);
                }
                if (room.Configuration.NewCities != screen.Room.Configuration.NewCities)
                {
                    await Task.Delay(80, token);
                    CardNetworkTests.Click(screen.Rules.VariantChip("new-cities"));
                    screen.Update(await request);
                }
                Assert.AreEqual(room.Configuration.Fate, screen.Room.Configuration.Fate);
                Assert.AreEqual(room.Configuration.NewCities, screen.Room.Configuration.NewCities);
                Assert.IsFalse(screen.Room.FreePeoplesAccepted == true || screen.Room.ShadowAccepted == true,
                    "Every change of the rules makes both players get ready again.");
                Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
                await Task.Delay(80, token);
                CardNetworkTests.Click(screen.Root.Q<Button>("room-ready"));
                screen.Update(await request);
                await sh.Put<RoomView>($"api/rooms/{room.Id}/ready",
                    new RoomReadyRequest { ExpectedVersion = screen.Room.Version, Ready = true }, token);
                screen.Update(await fp.Get<RoomView>($"api/rooms/{room.Id}", token));
                Assert.IsTrue(screen.Root.Q<Button>("start-game").enabledSelf);
                await Task.Delay(80, token);
                CardNetworkTests.Click(screen.Root.Q<Button>("start-game"));
                return await request;
            }
        }
    }
}
