using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class EffectsNetworkTests
    {
        private const string Protection = "base.fp.strategy.02";
        private const string Palantir = "base.sh.character.21";
        private const string Wormtongue = "base.sh.character.22";

        [UnityTest]
        public IEnumerator EnemyProtectionRequiresItsWholeCostAndOneCommitAfterReconnect() => Wait(RemoveProtection());

        [UnityTest]
        public IEnumerator PalantirCompletesOnTheChosenRingWithoutAnotherConfirmation() => Wait(RemovePalantir());

        [UnityTest]
        public IEnumerator ExpiringSourceKeepsItsPinnedTextAndRemovesControlsAndScopeAfterReconnect() => Wait(Expire());

        [UnityTest]
        public IEnumerator PublicEffectsRequireClickAndNeverMoveTheCameraDuringInspection() => Wait(Inspect());

        [UnityTest]
        public IEnumerator EmptyRegionEffectDoesNotInterceptTheFinalArmyTarget() => Wait(MoveIntoEffect());

        private static async Task RemoveProtection()
        {
            using var room = await Connect("effects-protection", ScenarioKind.BaseAcceptance);
            var actor = room.Sh;
            var before = actor.Board.View;
            var die = before.Options.Single(o => o.Kind == CommandKind.DiscardProtection).EligibleDice[0];
            Assert.AreEqual("Effects · 2", actor.Board.TableEffects.Tab.text);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.TableEffects.Root.Q<Button>("remove-table-" + Protection).style.display.value,
                "The effect owner's view has no opponent removal control.");
            await ComponentGestures.Cost(room, actor, die);
            await Open(room, actor);
            PointerGestureEvents.ClickAt(actor.Board.TableEffects.Root.Q<Button>("remove-table-" + Protection));
            await room.Settle();
            Assert.AreSame(actor.Board.TableEffects.DecisionHost, actor.Board.Tray.parent);
            Assert.AreEqual(DisplayStyle.None, actor.Board.TableEffects.Root.Q("effect-card").resolvedStyle.display,
                "UI-94: paying for a removal leaves the room to the decision.");
            StringAssert.Contains("one Army Event and one Character Event", actor.Board.TableEffects.Root.Q<Label>("effect-removal-cost").text);
            Assert.IsFalse(actor.Board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
            await CardGestures.Open(actor);
            PointerGestureEvents.ClickAt(actor.Board.Hand.Q<Button>("base.sh.strategy.01"));
            await room.Settle();
            Assert.IsFalse(actor.Board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
            await RejectIncomplete(room, actor, die);
            Assert.AreEqual(before.Revision, actor.Board.View.Revision);
            Assert.AreEqual(DieLocation.Available, actor.Board.View.Dice.Single(d => d.Id == die).Location);
            // UI-91: an unrolled card keeps the pointer over the cards it covers; leave it before the next card.
            CardGestures.Move(actor.Board.Map);
            await room.Settle();
            // UI-222: the second card completes the cost, so its click removes the protection without ✓.
            Assert.IsTrue(actor.Board.Hand.Q<Button>("base.sh.character.01").ClassListContains(HandCard.DiscardClass), "A cost card is marked red.");
            PointerGestureEvents.ClickAt(actor.Board.Hand.Q<Button>("base.sh.character.01"));
            await room.At(before.Revision + 1);
            Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == Protection));
            Assert.AreEqual("Effects · 1", actor.Board.TableEffects.Tab.text);
            Assert.IsNull(actor.Board.TableEffects.Root.Q<Button>("remove-table-" + Protection));
            Assert.IsNull(actor.Board.Map.Q("map-effects-lorien"));
            Assert.IsNotNull(actor.Board.Map.Q("map-effects-the-shire"));
            Assert.AreEqual(DieLocation.Spent, actor.Board.View.Dice.Single(d => d.Id == die).Location);
            Assert.AreEqual(1, actor.Board.View.Hand.Count);
            await Reconnect(room, actor);
            Assert.AreEqual("Effects · 1", actor.Board.TableEffects.Tab.text);
            Assert.AreEqual("Effect ended", actor.Board.TableEffects.Root.Q<Label>("effect-detail-state").text);
            LayoutTests.Save(actor.Viewport, "ui57-protection-removed-shadow.png");
        }

        private static async Task RejectIncomplete(MatchTestRoom room, MountedMatchClient actor, string die)
        {
            var before = actor.Board.View;
            var receipt = await actor.Api.Post<CommandReceipt>($"api/matches/{before.MatchId}/commands", new GameCommand
            {
                MatchId = before.MatchId, DecisionId = before.Decision.Id, ExpectedRevision = before.Revision,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.DiscardProtection,
                Payload = new CommandPayload { DieId = die, CardId = Protection, Ids = { "base.sh.strategy.01" } }
            }, room.Token);
            Assert.IsFalse(receipt.Accepted);
            var after = await actor.Api.Get<PlayerView>($"api/matches/{before.MatchId}", room.Token);
            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)),
                "An incomplete payment cannot alter any part of the public projection.");
        }

        private static async Task RemovePalantir()
        {
            using var room = await Connect("effects-palantir", ScenarioKind.BaseAcceptance);
            var actor = room.Fp;
            var before = actor.Board.View;
            var die = before.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army && d.Location == DieLocation.Available);
            var ring = before.Options.Single(o => o.Kind == CommandKind.DiscardPalantir).EligibleIds[0];
            await ComponentGestures.Cost(room, actor, die.Id);
            await Open(room, actor);
            PointerGestureEvents.ClickAt(actor.Board.TableEffects.Root.Q<Button>("remove-table-" + Palantir));
            await room.Settle();
            Assert.AreSame(actor.Board.TableEffects.DecisionHost, actor.Board.Tray.parent);
            StringAssert.Contains("chosen Elven Ring", actor.Board.TableEffects.Root.Q<Label>("effect-removal-cost").text);
            Assert.IsNull(actor.Board.Tray.Q<Button>("confirm-decision"));
            Assert.AreEqual(before.Revision, actor.Board.View.Revision);
            PointerGestureEvents.ClickAt(actor.Board.Console.Rings.For(ring));
            await room.At(before.Revision + 1);
            Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == Palantir));
            Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
            Assert.AreEqual(DieLocation.Spent, actor.Board.View.Dice.Single(d => d.Id == die.Id).Location);
            Assert.IsNull(actor.Board.TableEffects.Root.Q<Button>("remove-table-" + Palantir));
            Assert.AreEqual("Effects · 0", actor.Board.TableEffects.Tab.text);
        }

        private static async Task Expire()
        {
            using var room = await Connect("effects-expiry", ScenarioKind.BaseAcceptance);
            var observer = room.Sh;
            await Open(room, observer);
            PointerGestureEvents.ClickAt(observer.Board.TableEffects.Root.Q<Button>("effect-source-" + Wormtongue));
            await room.Settle();
            AssertCardInPanel(observer.Board, Wormtongue);
            Assert.AreEqual("Shadow · In play", observer.Board.TableEffects.Root.Q<Label>("effect-detail-state").text);
            // UI-413 (owner): the Politics row keeps its name clear; the Effects tab lists Rohan's effect.
            Assert.IsNull(observer.Board.Nations.Drawer.Q(className: "effect-scope-badge"), "No effect badge covers a nation's name.");
            Assert.IsNull(observer.Board.Map.Q("map-effects-orthanc"), "Saruman is an expiry condition, not the effect's region.");
            FellowshipGestures.Die(room.Fp);
            await room.Settle();
            var origin = room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.gandalf-grey").Location;
            FellowshipGestures.Figure(room.Fp, origin, "base.character.gandalf-grey");
            await room.MoveTo(room.Fp, "helms-deep");
            Assert.IsFalse(observer.Board.View.Effects.Any(e => e.SourceId == Wormtongue));
            Assert.AreEqual("Effect ended", observer.Board.TableEffects.Root.Q<Label>("effect-detail-state").text);
            Assert.IsNull(observer.Board.TableEffects.Root.Q<Button>("effect-source-" + Wormtongue));
            AssertCardInPanel(observer.Board, Wormtongue);
            Assert.IsTrue(observer.Board.TableEffects.Root.Q("effect-card").ClassListContains("effect-card-ended"));
            await Reconnect(room, observer);
            Assert.AreEqual("Effect ended", observer.Board.TableEffects.Root.Q<Label>("effect-detail-state").text);
            AssertCardInPanel(observer.Board, Wormtongue);
            LayoutTests.Save(observer.Viewport, "ui94-expired-pinned-shadow.png");
        }

        private static async Task Inspect()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance);
            foreach (var actor in new[] { room.Fp, room.Sh })
            {
                var board = actor.Board;
                var camera = board.Map.Camera;
                var pan = camera.Pan;
                var zoom = camera.Zoom;
                var revision = board.View.Revision;
                await ComponentGestures.Hover(board.TableEffects.Tab);
                Assert.IsTrue(board.TableEffects.Root.ClassListContains(Wotr.Client.LeftRailPreview.PreviewClass), "UI-92: hover previews Effects read-only.");
                PointerGestureEvents.ClickAt(board.TableEffects.Tab);
                await room.Settle();
                Assert.IsTrue(board.TableEffects.IsOpen);
                var source = board.TableEffects.Root.Q<Button>("effect-source-" + Protection);
                await ComponentGestures.Hover(source);
                Assert.IsEmpty(board.Cards.CardId);
                PointerGestureEvents.ClickAt(source);
                await room.Settle();
                Assert.AreEqual(Protection, board.TableEffects.SelectedSource);
                AssertCardInPanel(board, Protection);
                Assert.IsFalse(board.Family.Watching, "The source opens no sub-window.");
                Assert.AreEqual(pan, camera.Pan);
                Assert.AreEqual(zoom, camera.Zoom);
                Assert.AreEqual(revision, board.View.Revision);
                LayoutTests.Save(actor.Viewport, "ui94-inspect-" + board.View.You + ".png");
                board.TableEffects.Close();
                board.Map.Focus("lorien");
                await room.Settle();
                var marker = board.Map.Q<Label>("map-effects-lorien");
                Assert.AreEqual(PickingMode.Ignore, marker.pickingMode);
                await ComponentGestures.Hover(marker);
                Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
                FellowshipGestures.Region(actor, "lorien");
                await room.Settle();
                Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
                var effect = board.Hover.Panel.Root.Q<Button>("hover-effect-" + Protection);
                Assert.IsNotNull(effect);
                LayoutTests.Save(actor.Viewport, "ui57-empty-region-" + board.View.You + ".png");
                PointerGestureEvents.ClickAt(effect);
                await room.Settle();
                Assert.IsTrue(board.TableEffects.IsOpen);
                Assert.AreEqual(Protection, board.TableEffects.SelectedSource);
                Assert.AreEqual(revision, board.View.Revision);
            }
        }

        private static async Task MoveIntoEffect()
        {
            using var room = await Connect("effects-movement", ScenarioKind.BaseAcceptance);
            var actor = room.Fp;
            actor.Board.Map.Focus("dimrill-dale");
            ArmyGestures.Die(actor, ArmyGestures.ArmyDie(actor));
            await ArmyGestures.All(actor, "dimrill-dale");
            await room.Settle();
            Assert.Contains("lorien", actor.Board.Map.DraftOverlay.Targets.ToList());
            Assert.IsNull(actor.Board.Map.Q<Button>("target-lorien"), "Targets are terrain highlights.");
            Assert.AreEqual(PickingMode.Ignore, actor.Board.Map.Q<Label>("map-effects-lorien").pickingMode);
            var revision = actor.Board.View.Revision;
            await ComponentGestures.Hover(actor.Board.TableEffects.Tab);
            Assert.IsTrue(actor.Board.TableEffects.Root.ClassListContains(Wotr.Client.LeftRailPreview.PreviewClass), "UI-92: hover previews Effects read-only.");
            PointerGestureEvents.ClickAt(actor.Board.TableEffects.Tab);
            await room.Settle();
            Assert.IsTrue(actor.Board.Map.DraftOverlay.Targets.Contains("lorien"), "Effect inspection preserves the selected army's route.");
            actor.Board.TableEffects.Close();
            actor.Board.Cards.Close();
            actor.Board.Hover.Close();
            await room.Settle();
            FellowshipGestures.Region(actor, "lorien");
            await room.At(revision + 1);
            Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == "elves" && u.Location == "lorien"));
            Assert.IsFalse(actor.Board.TableEffects.IsOpen, "A legal movement target must not open Effects.");
        }

        // UI-94: a card source is read at the bottom of the Effects panel, never in a separate card window.
        internal static void AssertCardInPanel(BoardScreen board, string id)
        {
            var card = CardLibrary.Cards[id];
            Assert.IsEmpty(board.Cards.CardId, "No separate card window.");
            Assert.AreEqual(DisplayStyle.None, board.Cards.Root.resolvedStyle.display);
            var block = board.TableEffects.Root.Q("effect-card");
            Assert.IsNotNull(block);
            Assert.AreEqual(card.Event, block.Q<Label>("effect-card-event").text);
            Assert.AreEqual(CardTextFormat.Kind(card), block.Q<Label>("effect-card-kind").text);
            Assert.AreEqual(card.HasCombat ? DisplayStyle.Flex : DisplayStyle.None, block.Q("effect-card-combat").resolvedStyle.display);
            if (card.HasCombat)
                Assert.AreEqual(card.Combat, block.Q<Label>("effect-card-combat-text").text);
            Assert.That(block.resolvedStyle.backgroundImage.texture?.name, Does.StartWith("parchment"));
            var panel = board.TableEffects.Root.worldBound;
            Assert.LessOrEqual(block.worldBound.yMax, panel.yMax + 1, "The card text fits in the panel.");
            Assert.LessOrEqual(panel.yMax, board.TableEffects.Root.parent.worldBound.yMax + 1, "The panel stays above the bottom bar.");
            Assert.GreaterOrEqual(block.worldBound.xMin, panel.xMin - 1);
            Assert.LessOrEqual(block.worldBound.xMax, panel.xMax + 1);
        }

        private static async Task Open(MatchTestRoom room, MountedMatchClient actor)
        {
            PointerGestureEvents.ClickAt(actor.Board.TableEffects.Tab);
            await room.Settle();
            Assert.IsTrue(actor.Board.TableEffects.IsOpen);
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient actor)
        {
            var connected = actor.Connected;
            actor.Connection.Reconnect();
            await room.Until(() => actor.Connected > connected);
            await room.Settle();
        }
    }
}
