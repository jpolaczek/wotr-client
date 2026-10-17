using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public class ReturnDraftTests
    {
        [UnityTest]
        public IEnumerator OfflineDraftSurvivesClientRecreationAndReconcilesAnotherDevicesSpentCost() => MatchTestRoom.Wait(Draft());

        private static async Task Draft()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-offline-" + Guid.NewGuid().ToString("N"));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120)))
            using (var server = new RestartTestServer())
            {
                var token = timeout.Token;
                var id = fixture.ReturnMatches["draft"];
                try
                {
                    await server.Start(token);
                    var api = await fixture.Login(Side.FreePeoples, token, address: server.Address);
                    string die, source, selectedUnit;
                    using (var first = new MountedFlowClient(api, storage))
                    {
                        await Open(first, id, token);
                        die = first.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
                        var army = first.Board.View.Options.Single(o => o.Kind == CommandKind.MoveArmies).Armies
                            .First(a => a.UnitIds.Count >= 2 && a.Targets.Count > 0);
                        source = army.RegionId;
                        selectedUnit = army.UnitIds[1];
                        Click(first.Board.OwnDice.Q<Button>(die));
                        first.Board.Hover.Open(source);
                        await Task.Delay(100, token);
                        Click(first.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + army.UnitIds[0]));
                        var card = first.Board.View.Hand.First();
                        first.Board.Cards.Show(card, true);
                        server.Stop();
                        await Until(() => first.Board.Connection.text != "Live", token);
                        first.Board.Map.Camera.Pan = new Vector2(-240, -32);
                        first.Board.Map.Camera.Zoom = 1.3f;
                        first.Board.Map.Refresh();
                        first.Board.Hover.Open(source);
                        await Task.Delay(100, token);
                        Click(first.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + selectedUnit));
                        Assert.IsFalse(first.Root.Q<Button>("confirm-decision")?.enabledInHierarchy == true);
                        Assert.AreEqual(new Vector2(-240, -32), first.Board.Map.Camera.Pan);
                        Assert.IsTrue(first.Board.Cards.Pinned);
                        Assert.AreEqual(card, first.Board.Cards.CardId);
                        await first.LocalWrites;
                        var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, id);
                        Assert.AreEqual(source, saved.Draft.Draft.Payload.Moves.Single().From);
                        Assert.Contains(selectedUnit, saved.Draft.Draft.Payload.Moves.Single().UnitIds);
                        Assert.IsEmpty(saved.Draft.Draft.Payload.Moves.Single().To);
                        Assert.AreEqual(die, saved.Draft.Draft.Payload.DieId);
                        Assert.IsNull(saved.Actions.Pending);
                        first.Flow.Dispose();
                        await first.LocalWrites;
                    }
                    await server.Start(token);
                    var otherApi = await fixture.Login(Side.FreePeoples, token, address: server.Address);
                    using (var other = new MountedFlowClient(otherApi, Path.Combine(storage, "other-device")))
                    {
                        await Open(other, id, token);
                        Assert.IsEmpty(other.Board.ActionDie.DieId, "Another device does not import unsent choices.");
                        Click(other.Board.OwnDice.Q<Button>(die));
                        await ClickReady(other.Root, "console-discard", token);
                        await Until(() => other.Board.View.Revision == 1, token);
                        using (var shadow = await fixture.Login(Side.Shadow, token, address: server.Address))
                        {
                            var view = await shadow.Get<PlayerView>($"api/matches/{id}", token);
                            var receipt = await shadow.Post<CommandReceipt>($"api/matches/{id}/commands", new GameCommand
                            {
                                MatchId = id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
                                DecisionId = view.Decision.Id, Kind = CommandKind.DiscardDie,
                                Payload = new CommandPayload { DieId = view.Options.Single(o => o.Kind == CommandKind.DiscardDie).EligibleDice[0] }
                            }, token);
                            Assert.IsTrue(receipt.Accepted);
                        }
                        other.Flow.Dispose();
                        await other.LocalWrites;
                    }
                    var restoredApi = await fixture.Login(Side.FreePeoples, token, address: server.Address);
                    using (var restored = new MountedFlowClient(restoredApi, storage))
                    {
                        await Open(restored, id, token);
                        Assert.AreEqual(2, restored.Board.View.Revision);
                        await restored.LocalWrites;
                        var saved = await LocalMatchPersistence.Open(storage, restoredApi.Address.AbsoluteUri, restoredApi.AccountId, id);
                        Assert.AreEqual(source, saved.Draft.Draft.Payload.Moves.Single().From);
                        Assert.Contains(selectedUnit, saved.Draft.Draft.Payload.Moves.Single().UnitIds);
                        Assert.IsEmpty(saved.Draft.Draft.Payload.DieId);
                        Assert.IsFalse(restored.Root.Q<Button>("confirm-decision")?.enabledInHierarchy == true);
                        Assert.IsFalse(restored.Board.OwnDice.Query<Button>().ToList().Any(b => b.ClassListContains("selected")),
                            "An obsolete cost must not be replaced strategically.");
                        var available = restored.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
                        Click(restored.Board.OwnDice.Q<Button>(available));
                        await Task.Delay(80, token);
                        var target = restored.Board.Map.DraftOverlay.Targets.First();
                        MapContourProbe.ClickRegion(restored.Board.Map, target);
                        await Until(() => restored.Board.View.Revision == 3, token);
                        Assert.AreEqual(DecisionKind.ActionSequence, restored.Board.View.Decision.Kind);
                        Assert.AreEqual(available, restored.Board.View.ActionSequence.DieId);
                        Assert.AreEqual(target, restored.Board.View.Units.Single(u => u.Id == selectedUnit).Location);
                        restored.Flow.Dispose();
                        await restored.LocalWrites;
                    }
                    var resumedApi = await fixture.Login(Side.FreePeoples, token, address: server.Address);
                    using (var resumed = new MountedFlowClient(resumedApi, storage))
                    {
                        await Open(resumed, id, token);
                        Assert.AreEqual(3, resumed.Board.View.Revision);
                        Assert.AreEqual(1, resumed.Board.View.ActionSequence.Remaining);
                        Assert.AreEqual(resumed.Board.View.ActionSequence.DieId, resumed.Board.ActionDie.DieId);
                        Assert.AreEqual(DieLocation.Resolving, resumed.Board.View.Dice.Single(d => d.Id == resumed.Board.ActionDie.DieId).Location);
                        await Task.Delay(100, token);
                        Assert.AreEqual(3, (await resumedApi.Get<PlayerView>($"api/matches/{id}", token)).Revision,
                            "Restoring a paid continuation does not automatically finish it.");
                        await ClickReady(resumed.Root, "console-confirm", token);
                        await Until(() => resumed.Board.View.Revision == 4, token);
                        Assert.AreEqual(1, resumed.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
                        Assert.AreEqual(DieLocation.Discarded, resumed.Board.View.Dice.Single(d => d.Id == die).Location);
                        resumed.Flow.Dispose();
                        await resumed.LocalWrites;
                    }
                }
                finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
            }
        }
    }
}
