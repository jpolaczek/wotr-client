using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class InGameContextNetworkTests
    {
        [UnityTest]
        public IEnumerator RestartBeforeRingCommitRestoresTheExactUnsentCostAndWaitsForTheFinalFace() =>
            MatchTestRoom.Wait(RingDraft());

        private static async Task RingDraft()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-ui62-draft-" + Guid.NewGuid().ToString("N"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var id = fixture.ReturnMatches["ui62-ring-draft"];
            try
            {
                await server.Start(token);
                var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                var api = await fixture.Login(Side.FreePeoples, token, boundary, server.Address);
                string ring, die;
                long revision;
                using (var first = new MountedFlowClient(api, storage))
                {
                    await Open(first, id, token);
                    revision = first.Board.View.Revision;
                    ring = first.Board.View.Rings.Last(value => value.Owner == Side.FreePeoples).Id;
                    die = first.Board.View.Dice.Last(value => value.Owner == Side.FreePeoples && value.Location == DieLocation.Available).Id;
                    // UI-161: the ring is armed at its console socket, then the die is chosen.
                    Click(first.Board.Console.Rings.For(ring));
                    await ClickReady(first.Root, die, token);
                    Assert.IsTrue(first.Board.RingPicker.IsOpen);
                    await first.LocalWrites;
                    var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, id);
                    Assert.AreEqual(ring, saved.Draft.Draft.Payload.RingId);
                    Assert.AreEqual(die, saved.Draft.Draft.Payload.DieId);
                    Assert.IsNull(saved.Actions.Pending);
                    Assert.AreEqual(0, boundary.Commands);
                    first.Flow.Dispose();
                    await first.LocalWrites;
                }
                server.Stop();
                await server.Start(token);
                var nextBoundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
                var nextApi = await fixture.Login(Side.FreePeoples, token, nextBoundary, server.Address);
                using (var next = new MountedFlowClient(nextApi, storage))
                {
                    await Open(next, id, token);
                    Assert.AreEqual(revision, next.Board.View.Revision);
                    Assert.AreEqual(0, nextBoundary.Commands);
                    Assert.IsTrue(next.Board.Console.Rings.For(ring).ClassListContains("selected"));
                    Assert.IsTrue(next.Board.OwnDice.Q<Button>(die).ClassListContains("selected"));
                    // The restored draft brings the result popover back above the die; no resource panel opens.
                    await Until(() => next.Board.RingPicker.IsOpen, token);
                    var face = next.Board.RingPicker.Root.Q<Button>("ring-face-muster");
                    await Until(() => face.enabledInHierarchy, token);
                    PointerGestureEvents.ClickAt(face);
                    await Until(() => next.Board.View.Revision == revision + 1, token);
                    Assert.AreEqual(1, nextBoundary.Commands);
                    Assert.AreEqual(ring, nextBoundary.LastCommand.Payload.RingId);
                    Assert.AreEqual(die, nextBoundary.LastCommand.Payload.DieId);
                    Assert.AreEqual(DieLocation.Available, next.Board.View.Dice.Single(value => value.Id == die).Location);
                    next.Flow.Dispose();
                    await next.LocalWrites;
                }
            }
            finally
            {
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }
    }
}
