using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class T23FullGameNetworkTests
    {
        [UnityTest]
        public IEnumerator NormalLobbySetupReachesAFinalResultOnTwoClientsWithProductionRandomness()
        {
            var task = Play();
            var deadline = Time.realtimeSinceStartup + 610;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "The complete game exceeded its time bound.");
            task.GetAwaiter().GetResult();
        }

        // EXP-04: onObjects checks that every decision has its object host and the generic window never opens;
        // finished runs on the finished game before the room closes.
        internal static async Task Play(MatchConfiguration configuration = null, bool largeInterface = false,
            string artifact = "t23-full-game.json", bool onObjects = false, Func<MatchTestRoom, Task> finished = null)
        {
            var trace = new List<object>();
            using (var room = await MatchTestRoom.CreateNormalGame(configuration))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.AreEqual(0, room.Fp.Board.View.Revision);
                Assert.AreEqual(configuration?.Council == true ? 0 : 1, room.Fp.Board.View.Turn);
                Assert.AreEqual("rivendell", room.Fp.Board.View.Fellowship.RegionId);
                if (configuration?.Lords != true)
                {
                    Assert.AreEqual(2, room.Fp.Board.View.Hand.Count);
                    Assert.AreEqual(2, room.Sh.Board.View.Hand.Count);
                }
                if (largeInterface)
                    await FullGameDesktopChecks.Configure(room);
                var lastReconnectTurn = 0;
                try
                {
                    while (!room.Fp.Board.View.Winner.HasValue)
                    {
                        var actor = room.Actor;
                        var view = actor.Board.View;
                        Assert.Less(view.Revision, 600, "Bound the scenario without forcing a result or changing RNG.");
                        Assert.Less(view.Turn, 60);
                        Assert.IsNotNull(view.Decision);
                        if (largeInterface)
                            await FullGameDesktopChecks.FinishInspection(room, actor);
                        var waiting = actor == room.Fp ? room.Sh : room.Fp;
                        var observer = largeInterface ? new FullGameDesktopChecks.Observer(waiting) : null;
                        if (largeInterface)
                        {
                            FullGameDesktopChecks.Layout(actor);
                            FullGameDesktopChecks.Layout(waiting);
                        }
                        if (onObjects)
                            watch.OnItsObject(actor);
                        var kind = FullGameChoices.Select(view);
                        trace.Add(new { view.Revision, view.Turn, view.You, view.Phase, Decision = view.Decision.Kind,
                            Command = kind, view.Fellowship });
                        await ExecuteDecision(room, actor, kind, largeInterface);
                        observer?.Check();
                        T22NetworkActions.SameDice(room);
                        if (view.Turn >= lastReconnectTurn + 3)
                        {
                            await Reconnect(room);
                            lastReconnectTurn = view.Turn;
                        }
                    }
                    await CheckFinished(room);
                    if (onObjects)
                        watch.NeverOpened();
                    if (finished != null)
                        await finished(room);
                    var result = room.Fp.Board.View;
                    trace.Add(new { result.Revision, result.Turn, result.Winner, result.Victory, result.Fellowship });
                    TestContext.WriteLine($"Complete game {result.MatchId}: {result.Turn} turns, {result.Revision} decisions, {result.Victory}.");
                }
                catch
                {
                    LayoutTests.Save(room.Fp.Viewport, artifact + "-fp.png");
                    LayoutTests.Save(room.Sh.Viewport, artifact + "-sh.png");
                    throw;
                }
                finally
                {
                    var directory = Path.GetDirectoryName(Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG"));
                    File.WriteAllText(Path.Combine(directory, artifact), JsonConvert.SerializeObject(new
                    {
                        room.Fp.Board.View.MatchId,
                        RecordedAt = DateTime.UtcNow,
                        room.Fp.Board.View.Configuration,
                        Scale = room.Fp.Panel.scale,
                        ReducedMotion = room.Fp.Interface.Preferences.ReducedMotion,
                        Decisions = trace
                    }, Formatting.Indented));
                }
            }
        }

        private static async Task ExecuteDecision(MatchTestRoom room, MountedMatchClient actor, CommandKind kind,
            bool inspectLayout)
        {
            var task = FullGameChoices.Execute(room, actor, kind, inspectLayout ? FullGameDesktopChecks.Layout : null);
            await Task.WhenAny(task, Task.Delay(15000, room.Token));
            var confirm = actor.Board.Tray.Q<Button>("confirm-decision");
            Assert.IsTrue(task.IsCompleted,
                $"Decision {kind} stalled at revision {actor.Board.View.Revision}; confirm enabled: {confirm?.enabledInHierarchy}.");
            await task;
        }

        private static async Task Reconnect(MatchTestRoom room)
        {
            var fp = room.Fp.Connected;
            var sh = room.Sh.Connected;
            var revision = room.Fp.Board.View.Revision;
            room.Fp.Connection.Reconnect();
            room.Sh.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > fp && room.Sh.Connected > sh);
            await room.At(revision);
        }

        private static async Task CheckFinished(MatchTestRoom room)
        {
            await Reconnect(room);
            var result = room.Fp.Board.View;
            Assert.AreEqual(result.Winner, room.Sh.Board.View.Winner);
            Assert.AreEqual(result.Victory, room.Sh.Board.View.Victory);
            Assert.IsTrue(result.Victory is VictoryCondition.RingCorrupted or VictoryCondition.RingDestroyed);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual(GamePhase.Finished, client.Board.View.Phase);
                Assert.IsNull(client.Board.View.Decision);
                Assert.IsEmpty(client.Board.View.Options);
                var receipt = await client.Api.Post<CommandReceipt>($"api/matches/{result.MatchId}/commands", new GameCommand
                {
                    MatchId = result.MatchId,
                    CommandId = Guid.NewGuid().ToString("N"),
                    ExpectedRevision = result.Revision,
                    Kind = CommandKind.DiscardDie
                }, room.Token);
                Assert.IsFalse(receipt.Accepted);
                Assert.AreEqual("GameFinished", receipt.Error);
                Assert.AreEqual(result.Revision, receipt.Revision);
            }
        }
    }
}
