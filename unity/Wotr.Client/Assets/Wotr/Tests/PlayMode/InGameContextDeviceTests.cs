using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed partial class InGameContextNetworkTests
    {
        [UnityTest]
        public IEnumerator PublicNoticeNeverRevealsPrivateTileAndAnotherDeviceInvalidatesItsPressedChoice() => Wait(PrivateTile());

        private static async Task PrivateTile()
        {
            var storage = Path.Combine(Path.GetTempPath(), "wotr-ui62-device-" + Guid.NewGuid().ToString("N"));
            try
            {
                var boundary = new UI10HttpBoundary();
                using var room = await Connect("ui62-private-tile", ScenarioKind.Interaction, readRoot: storage,
                    shHandler: boundary, openPanels: false);
                boundary.MatchPath = "/api/matches/" + room.Sh.Board.View.MatchId;
                await ActionNoticeNetworkTests.Loaded(room);
                var shadow = room.Sh.Board;
                var publicBoard = room.Fp.Board;
                var tile = SelectableCorruptionTile(shadow.View);
                var sovereign = shadow.View.CorruptionAttempt.SovereignId;
                var revision = shadow.View.Revision;
                Assert.IsEmpty(publicBoard.View.CorruptionAttempt.OwnChoices);
                Assert.IsNull(publicBoard.Root.Q<Button>("corruption-choice-" + tile));
                await ActionNoticeNetworkTests.OpenLatest(room, room.Fp);
                Assert.AreEqual(InGameActionHost.Sovereign, publicBoard.ActionHistory.Context.InGame.Host);
                var publicRecord = JToken.FromObject(publicBoard.Log.Card.Displayed).ToString();
                foreach (var privateTile in shadow.View.CorruptionAttempt.OwnChoices)
                    StringAssert.DoesNotContain(privateTile.TileId, publicRecord);
                Assert.IsFalse(publicBoard.Log.IsOpen);
                publicBoard.Log.CloseCard();
                var read = room.Fp.Activity.Model.StepCount;
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                Assert.AreEqual(read, room.Fp.Activity.Model.StepCount, "GET refresh cannot manufacture or acknowledge an update.");
                using var other = await room.AnotherDevice(room.Sh, Path.Combine(storage, "second"));
                shadow.Notice.Open();
                other.Board.Notice.Open();
                await room.Settle();
                await room.Until(() => shadow.Tray.Q<Button>("corruption-choice-" + tile)?.enabledInHierarchy == true &&
                    other.Board.Tray.Q<Button>("corruption-choice-" + tile)?.enabledInHierarchy == true);
                var choice = shadow.Tray.Q<Button>("corruption-choice-" + tile);
                PointerGestureEvents.Press(choice);
                PointerGestureEvents.ClickAt(other.Board.Tray.Q<Button>("corruption-choice-" + tile));
                await room.At(revision + 1);
                await room.Until(() => other.Board.View.Revision == revision + 1);
                PointerGestureEvents.Release(choice);
                await room.Settle();
                Assert.AreEqual(1, boundary.Commands, "Only the second device's actual choice may reach the server.");
                Assert.AreEqual(CommandKind.ChooseCorruptionTile, boundary.LastCommand.Kind);
                CollectionAssert.AreEqual(new[] { tile }, boundary.LastCommand.Payload.Ids);
                Assert.IsNull(shadow.View.CorruptionAttempt);
                var publicSovereign = publicBoard.View.Sovereigns.Single(value => value.Id == sovereign);
                Assert.AreEqual(1, publicSovereign.Tiles.Count);
                Assert.IsNull(publicSovereign.Corruption);
                Assert.AreEqual("", publicSovereign.Tiles[0].TileId);
                Assert.IsNull(publicSovereign.Tiles[0].Value);
                var page = await Page(room, room.Fp);
                StringAssert.DoesNotContain(tile, JToken.FromObject(page).ToString());
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                Assert.AreEqual(revision + 1, shadow.View.Revision);
                Assert.AreEqual(1, boundary.Commands);
                LayoutTests.Save(room.Fp.Viewport, "ui62-private-tile-public.png");
            }
            finally
            {
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }

        private static string SelectableCorruptionTile(PlayerView view)
        {
            Assert.AreEqual(DecisionKind.CorruptionTile, view.Decision.Kind);
            Assert.AreEqual("CorruptionChoice", view.Decision.Presentation);
            Assert.IsFalse(view.CorruptionAttempt.Sequential);
            Assert.AreEqual(2, view.CorruptionAttempt.DrawLimit);
            Assert.AreEqual(2, view.CorruptionAttempt.DrawnCount);
            Assert.AreEqual(2, view.CorruptionAttempt.OwnChoices.Count);
            var option = view.Options.Single(value => value.Kind == CommandKind.ChooseCorruptionTile);
            Assert.IsTrue(option.Enabled);
            CollectionAssert.AreEquivalent(view.CorruptionAttempt.OwnChoices.Select(value => value.TileId), option.EligibleIds);
            return option.EligibleIds.Last();
        }
    }
}
