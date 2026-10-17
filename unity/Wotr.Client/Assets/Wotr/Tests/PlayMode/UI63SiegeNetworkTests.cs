using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class UI63SiegeNetworkTests
    {
        private const string Dain = "kings.sovereign.dain";
        private const string Ruler = "kings.die.fp.ruler";
        private const string Home = "erebor";

        [UnityTest]
        public IEnumerator AwakeningInsideTheGarrisonKeepsItsSideAndPaysEitherExactPhysicalDie() =>
            MatchTestRoom.Wait(Awaken());

        [UnityTest]
        public IEnumerator AwakeningCannotEnterTheBesiegedHomeFromOutsideAndRejectsTheForgedRoute() =>
            MatchTestRoom.Wait(Outside());

        private static async Task Awaken()
        {
            foreach (var source in new[] { "muster", "ruler" })
            {
                using var room = await UI10TestRoom.Connect("ui63-siege-" + source);
                var board = room.Fp.Board;
                var before = board.View;
                var option = before.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(Dain));
                var die = before.Dice.Single(d => option.EligibleDice.Contains(d.Id) &&
                    (source == "ruler" ? d.Id == Ruler : d.Source == "standard" && d.Face == DieFace.Muster));
                CollectionAssert.AreEqual(new[] { Home }, option.Targets);
                Assert.IsTrue(before.Sovereigns.Single(s => s.Id == Dain).Garrison);
                Assert.AreEqual(2, before.Sovereigns.Single(s => s.Id == Dain).Tiles.Count);
                Assert.IsTrue(before.Sovereigns.Single(s => s.Id == Dain).Tiles.Any(t => t.Value == null));
                Assert.AreEqual(0, room.Sh.Board.View.Options.Count);
                if (source == "ruler")
                {
                    Assert.Greater(before.Turn, 1);
                    Assert.AreEqual(DieFace.Character, die.Face, "Ruler must work without substituting a Muster face.");
                    Assert.AreEqual(SovereignStatus.Awakened,
                        before.Sovereigns.Single(s => s.Id == "kings.sovereign.theoden").Status);
                }
                var armies = Armies(before);
                var pan = board.Map.Camera.Pan;
                PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
                await room.Settle();
                PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, Dain));
                await room.Settle();
                Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
                StringAssert.Contains("Garrison", board.Sovereigns.Inspector.Q<Label>("sovereign-state").text);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                Assert.AreEqual(0, room.Boundary.Commands);
                Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
                var stay = board.Tray.Q<Button>("travel-stay");
                Assert.IsNotNull(stay);
                await room.Until(() => stay.enabledInHierarchy);
                LayoutTests.Save(room.Fp.Viewport, "ui63-siege-" + source + "-before.png");
                PointerGestureEvents.ClickAt(stay);
                await room.At(before.Revision + 1);
                Assert.AreEqual(1, room.Boundary.Commands);
                var sent = room.Boundary.LastCommand;
                Assert.AreEqual(CommandKind.AwakenSovereign, sent.Kind);
                Assert.AreEqual(die.Id, sent.Payload.DieId);
                CollectionAssert.AreEqual(new[] { Home }, sent.Payload.Route);
                foreach (var client in new[] { room.Fp, room.Sh })
                    AssertAwakened(client.Board.View, before, armies, die.Id, source == "ruler");
                LayoutTests.Save(room.Fp.Viewport, "ui63-siege-" + source + "-after.png");
            }
        }

        private static void AssertAwakened(PlayerView view, PlayerView before, string armies, string cost, bool ruler)
        {
            var sovereign = view.Sovereigns.Single(s => s.Id == Dain);
            var character = view.Characters.Single(c => c.Id == Dain);
            Assert.AreEqual(SovereignStatus.Awakened, sovereign.Status);
            Assert.AreEqual(Home, character.Location);
            Assert.IsTrue(character.Garrison);
            Assert.IsTrue(sovereign.Garrison);
            Assert.IsFalse(view.Units.Any(u => u.Id == Dain));
            Assert.IsEmpty(sovereign.Tiles);
            Assert.AreEqual("free-peoples", view.RulerTokens.Single(t => t.SovereignId == Dain).Location);
            Assert.AreEqual(before.RulerTokens.Count + 1, view.RulerTokens.Count);
            Assert.AreEqual(DieLocation.Spent, view.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(ruler ? DieLocation.Spent : DieLocation.Removed, view.Dice.Single(d => d.Id == Ruler).Location);
            Assert.AreEqual(armies, Armies(view), "Awakening changes neither army nor siege side.");
            Assert.AreEqual(Side.FreePeoples, view.Control[Home]);
            Assert.IsNull(view.Hunt);
            foreach (var unchanged in before.Dice.Where(d => d.Id != cost))
                Assert.AreEqual(unchanged.Location, view.Dice.Single(d => d.Id == unchanged.Id).Location, unchanged.Id);
        }

        private static async Task Outside()
        {
            using var room = await UI10TestRoom.Connect("ui63-siege-outside");
            var board = room.Fp.Board;
            var before = board.View;
            var fp = JsonConvert.SerializeObject(before);
            var shadow = JsonConvert.SerializeObject(room.Sh.Board.View);
            var cost = before.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Muster &&
                d.Location == DieLocation.Available).Id;
            Assert.IsFalse(before.Options.Any(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(Dain)));
            var availability = before.Availability.Single(a => a.SubjectId == Dain && a.ActionId == "awaken");
            Assert.IsFalse(availability.CanAct);
            Assert.IsEmpty(availability.Targets);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(cost));
            await room.Settle();
            var portrait = await BoardNavigation.Sovereign(board, Dain);
            Assert.IsFalse(portrait.ClassListContains("legal-target"));
            PointerGestureEvents.ClickAt(portrait);
            await room.Settle();
            Assert.IsTrue(board.Sovereigns.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.Sovereigns.DecisionHost.style.display.value);
            Assert.IsNull(board.Sovereigns.DecisionHost.Q<Button>("travel-stay"));
            Assert.AreEqual(0, room.Boundary.Commands);
            LayoutTests.Save(room.Fp.Viewport, "ui63-siege-outside.png");
            var command = new GameCommand
            {
                MatchId = before.MatchId, ExpectedRevision = before.Revision, DecisionId = before.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.AwakenSovereign,
                Payload = new CommandPayload { CharacterId = Dain, DieId = cost, Route = { "iron-hills", Home } }
            };
            var receipt = await room.Fp.Api.Post<CommandReceipt>($"api/matches/{before.MatchId}/commands", command, room.Token);
            Assert.IsFalse(receipt.Accepted);
            Assert.AreEqual(fp, JsonConvert.SerializeObject(await room.Fp.Api.Get<PlayerView>(
                "api/matches/" + before.MatchId, room.Token)));
            Assert.AreEqual(shadow, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                "api/matches/" + before.MatchId, room.Token)));
        }

        private static string Armies(PlayerView view) => JsonConvert.SerializeObject(view.Units
            .Where(u => u.Id != Dain).OrderBy(u => u.Id).ToArray());
    }
}
