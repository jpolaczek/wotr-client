using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-432 (owner's rule, UI-430): a decline is the battle or Hunt window's Skip effect (the console's Pass) and a window
    // list that repeats an object goes, on a real server. Each object's click sends a payload JSON-equal to the one its old
    // button sent, recorded from the buttons at 7b924fb4 (a temporary probe on the same seeded matches). A seeded match is
    // played once per run, so Worn with Sorrow and Toil's three paths are three tests, each in its own run. Each test leaves
    // .artifacts/screenshots/ui432-<case>-1366-1.00.png.
    public sealed class BoardRuleDeclineTests
    {
        private const string Mithril = "base.fp.character.05";

        [UnityTest] public IEnumerator BlackBreathsSkipEffectSendsTheOldKeepAllPayload() =>
            Wait(SkipsInTheBattle("shadow-black-breath", Side.Shadow, OptionalDecline.BlackBreathKeep, "Black Breath", "black-breath"));
        [UnityTest] public IEnumerator HeroicDeathsSkipEffectSendsTheOldKeepAllPayload() =>
            Wait(SkipsInTheBattle("heroic", Side.FreePeoples, OptionalDecline.HeroicDeathKeep, "Heroic Death", "heroic-death"));
        [UnityTest] public IEnumerator AndurilsSkipEffectSendsTheOldKeepMissesPayload() =>
            Wait(SkipsInTheBattle("anduril", Side.FreePeoples, OptionalDecline.AndurilKeep, "Andúril", "anduril"));
        [UnityTest] public IEnumerator WornsRedTableCardSendsTheOldDiscardPayload() => Wait(WornTableCard());
        [UnityTest] public IEnumerator WornsRedHandCounterSendsTheOldRandomHandPayload() => Wait(WornRandomCard());
        [UnityTest] public IEnumerator WornsSkipEffectSendsTheOldKeepCardsPayload() => Wait(WornKeep());
        [UnityTest] public IEnumerator ScoutsBattleRegionSendsTheOldWithdrawPayload() => Wait(ScoutsShelterRegion());

        // The payload the old buttons sent at 7b924fb4: every field at its default but the option's id.
        private static string OldPayload(string id) =>
            "{\"Stepwise\":false,\"DieId\":\"\",\"RingId\":\"\",\"RulerTokenId\":\"\",\"CouncilTokenId\":\"\",\"Deck\":\"\",\"Face\":null," +
            "\"Ids\":[\"" + id + "\"],\"NationId\":\"\",\"Placements\":[],\"MovementCost\":1,\"Moves\":[],\"CardId\":\"\",\"CardCost\":4," +
            "\"ReactionId\":\"\",\"GuideId\":\"\",\"Route\":[],\"CompanionMoves\":[],\"HuntResponse\":0,\"Losses\":[],\"RegionId\":\"\"," +
            "\"AutoDecision\":null,\"CharacterId\":\"\",\"AbilityId\":\"\"}";

        // A battle card's decline: no button lists it; the battle window's Skip effect names the card and sends it.
        private static async Task SkipsInTheBattle(string scenario, Side side, string decline, string effect, string shot)
        {
            using var room = await Connect(scenario, ScenarioKind.CardPack);
            var client = side == Side.Shadow ? room.Sh : room.Fp;
            var sent = Sent(client);
            Button skip;
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, 1366, 1f))
            {
                await ReopenBattle(room, client);
                Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-" + decline), "No button repeats the decline.");
                skip = client.Board.Tray.Q<Button>("battle-skip-effect");
                Assert.IsNotNull(skip, "The battle window skips the effect.");
                StringAssert.StartsWith("Skip " + effect + " · ", skip.tooltip);
                Assert.AreEqual("Pass · " + skip.tooltip, client.Board.Console.Actions.Pass.tooltip, "Skip effect and Pass skip the same.");
                LayoutTests.Save(viewport.Texture, "ui432-" + shot + "-1366-1.00.png");
                var revision = client.Board.View.Revision;
                PointerGestureEvents.ClickAt(skip);
                await room.At(revision + 1);
            }
            AssertSent(sent, OldPayload(decline));
        }

        // Worn with Sorrow and Toil: the Effects tab lights and Mithril's tile is red; its click discards it.
        private static async Task WornTableCard()
        {
            using var room = await Connect("shadow-worn", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                AssertWornObjects(room.Sh);
                var tile = await RedTile(room, room.Sh.Board, Mithril);
                LayoutTests.Save(viewport.Texture, "ui432-worn-table-1366-1.00.png");
                var revision = room.Sh.Board.View.Revision;
                PointerGestureEvents.ClickAt(tile);
                await room.At(revision + 1);
            }
            AssertSent(sent, OldPayload(Mithril));
            Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == Mithril));
        }

        // Worn with Sorrow and Toil: the Free Peoples card counter is red; its click discards a random card.
        private static async Task WornRandomCard()
        {
            using var room = await Connect("shadow-worn", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                await room.Settle();
                AssertWornObjects(room.Sh);
                var counter = room.Sh.Board.TopBar.Opponent.Cards;
                Assert.IsTrue(counter.ClassListContains(OpponentSummary.DiscardTargetClass), "The hand counter is red.");
                StringAssert.StartsWith("Discard a random card · Click to discard a random card from the Free Peoples hand because of " +
                    "Worn with Sorrow and Toil", counter.tooltip);
                LayoutTests.Save(viewport.Texture, "ui432-worn-counter-1366-1.00.png");
                var revision = room.Sh.Board.View.Revision;
                PointerGestureEvents.ClickAt(counter);
                await room.At(revision + 1);
            }
            AssertSent(sent, OldPayload(WornDiscardChoice.RandomHand));
            Assert.IsFalse(room.Sh.Board.TopBar.Opponent.CardsOffered, "Answered, the counter is a picture again.");
        }

        // Worn with Sorrow and Toil: Keep cards is the Hunt window's Skip effect.
        private static async Task WornKeep()
        {
            using var room = await Connect("shadow-worn", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                await room.Settle();
                if (!room.Sh.Board.Hunt.IsOpen)
                    CardNetworkTests.Click(room.Sh.Board.Hunt.Tab);
                await room.Settle();
                var skip = room.Sh.Board.Tray.Q<Button>("hunt-skip-effect");
                Assert.IsNotNull(skip, "The Hunt window skips Keep cards.");
                Assert.AreEqual("Skip Worn with Sorrow and Toil · Discard no Free Peoples Character card", skip.tooltip);
                CollectionAssert.IsEmpty(room.Sh.Board.Tray.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-choice-"))
                    .Select(b => b.name), "No list repeats the objects.");
                LayoutTests.Save(viewport.Texture, "ui432-worn-skip-1366-1.00.png");
                var revision = room.Sh.Board.View.Revision;
                PointerGestureEvents.ClickAt(skip);
                await room.At(revision + 1);
            }
            AssertSent(sent, OldPayload(OptionalDecline.WornKeep));
        }

        // Scouts: the battle's own region is lit with the retreat; its click withdraws into siege.
        private static async Task ScoutsShelterRegion()
        {
            using var room = await Connect("strategy-scouts", ScenarioKind.CardPack);
            var sent = Sent(room.Fp);
            var board = room.Fp.Board;
            var region = board.View.Battle.RegionId;
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Fp, 1366, 1f))
            {
                await ReopenBattle(room, room.Fp);
                Assert.IsNull(board.Tray.Q<Button>("effect-choice-" + ScoutsShelter.Shelter), "No button repeats the region.");
                var targets = board.Map.DraftOverlay.Targets;
                Assert.IsTrue(targets.Contains(region) && targets.Contains("fords-of-isen"), "The stronghold and the retreat light.");
                var force = board.Map.Q("army-" + region + "-FreePeoples");
                StringAssert.StartsWith("Withdraw into siege · Click to withdraw into the stronghold because of Scouts", force.tooltip);
                StringAssert.StartsWith("Click " + board.Presentation.Regions[region].Name + " to withdraw into its stronghold, or a lit " +
                    "region to retreat", board.Console.Status.Hint.text);
                board.Map.Reveal(new[] { region, "fords-of-isen" });
                await room.Settle();
                LayoutTests.Save(viewport.Texture, "ui432-scouts-1366-1.00.png");
                var revision = board.View.Revision;
                MapContourProbe.ClickRegion(board.Map, region);
                await room.At(revision + 1);
            }
            AssertSent(sent, OldPayload(ScoutsShelter.Shelter));
        }

        private static void AssertWornObjects(MountedMatchClient client)
        {
            Assert.AreEqual(WornDiscardChoice.Presentation, client.Board.View.Decision.Presentation);
            Assert.IsTrue(client.Board.TableEffects.Tab.ClassListContains("legal-target"), "The Effects tab lights.");
            Assert.IsTrue(client.Board.TopBar.Opponent.CardsOffered, "The hand counter is offered.");
            Assert.AreEqual("Pass · Skip Worn with Sorrow and Toil · Discard no Free Peoples Character card",
                client.Board.Console.Actions.Pass.tooltip);
            Assert.AreEqual("Discard: red card in Effects · Random card: red hand counter · Pass discards none",
                client.Board.Console.Status.Hint.text);
        }

        // The Effects tab's click opens the panel, where Worn's cards are red and say why.
        private static async Task<Button> RedTile(MatchTestRoom room, BoardScreen board, string card)
        {
            if (!board.TableEffects.Pinned)
                PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            var tile = board.TableEffects.Root.Q<Button>("effect-source-" + card);
            await room.Until(() => tile.enabledInHierarchy && tile.worldBound.width > 0);
            await room.Settle();
            Assert.IsTrue(tile.ClassListContains(TableEffectsView.DiscardClass), card + " is red.");
            StringAssert.StartsWith("Discard " + CardLibrary.Title(card) + " · Click to discard it because of Worn with Sorrow and Toil: " +
                WornDiscardChoice.Reason, tile.tooltip);
            return tile;
        }

        // The viewport closes the windows; the console's plaque opens the battle window again.
        private static async Task ReopenBattle(MatchTestRoom room, MountedMatchClient client)
        {
            await room.Settle();
            if (!client.Board.Battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            await room.Settle();
        }

        private static List<GameCommand> Sent(MountedMatchClient client)
        {
            var sent = new List<GameCommand>();
            client.Interaction.CommandAnswered += (command, receipt, _) =>
            {
                Assert.IsTrue(receipt.Accepted, receipt.Error);
                sent.Add(command);
            };
            return sent;
        }

        private static void AssertSent(List<GameCommand> sent, string payload)
        {
            Assert.AreEqual(1, sent.Count, "One command.");
            Assert.AreEqual(CommandKind.ChooseEffect, sent[0].Kind);
            Assert.AreEqual(payload, JsonConvert.SerializeObject(sent[0].Payload), "The object sends what the old button sent.");
        }
    }
}
