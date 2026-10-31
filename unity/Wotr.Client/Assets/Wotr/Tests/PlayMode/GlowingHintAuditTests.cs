using System;
using System.Collections;
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
    // UI-434 (owner, after UI-433): every object a hint asks the player to click visibly glows where it stands. Each test
    // opens a seeded match as the player sees it (no panel opened by the test), checks the hint, then every object it
    // names with GlowingTargetChecks at 1366×768 and 1920×1080 at 125 %, and leaves
    // .artifacts/screenshots/c434-<case>-<width>-<scale>.png. A seed is played once per run, so the tests use distinct
    // seeds and the class runs in one PlayMode run; the other tests of these seeds run in runs of their own.
    public sealed class GlowingHintAuditTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1920, 1.25f) };

        [UnityTest] public IEnumerator TheDecksTheDeckChoiceOffersGlowBesideTheHand() => Wait(Decks());
        [UnityTest] public IEnumerator TheMordorGateAndTheFellowshipFigureGlowInTheFellowshipPhase() => Wait(MordorGate());
        [UnityTest] public IEnumerator TheNazgulStrikesHuntBoxAndRedTableCardsGlow() => Wait(NazgulStrike());
        [UnityTest] public IEnumerator WornWithSorrowsRedHandCounterGlowsInTheTopBar() => Wait(Worn());
        [UnityTest] public IEnumerator LuresRedPortraitAndCorruptionMarkerGlowInTheOpenFellowshipPanel() => Wait(Lure());
        [UnityTest] public IEnumerator TheBlackCaptainsReserveNazgulGlowBehindTheLitReserveTab() => Wait(BlackCaptain());
        [UnityTest] public IEnumerator WorkOfTheEnemysRegionIsLitOnScreen() => Wait(WorkOfTheEnemy());
        [UnityTest] public IEnumerator TheHandsCardsToDiscardAreRed() => Wait(HandDiscard());
        [UnityTest] public IEnumerator ScoutsBattleRegionAndRetreatAreLitOnScreen() => Wait(Scouts());
        [UnityTest] public IEnumerator TheFiguresASiegeExtensionMayTakeGlowInTheBattleWindow() => Wait(SiegeExtension());

        // Base: a siege goes on — the Shadow's figures that may extend it glow in its column of the battle window.
        private static async Task SiegeExtension()
        {
            using var room = await Connect("siege", openPanels: false);
            var client = room.Sh;
            var ids = client.Board.View.Options.Single(o => o.Kind == CommandKind.ExtendSiege).EligibleIds;
            await Audit(room, client, "siege-extension", "Click a highlighted figure to extend the siege", async () =>
            {
                foreach (var id in ids)
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Tray.Q<Button>("figure-" + id), "figure " + id);
            });
        }

        // Base The Palantír of Orthanc (UI-390): the decks its draw may come from glow beside the hand, in the opened drawer.
        private static async Task Decks()
        {
            using var room = await Connect("shadow-palantir-draw", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            Assert.AreEqual(DeckChoice.Presentation, client.Board.View.Decision.Presentation);
            await Audit(room, client, "decks", "Click a glowing deck beside your hand", async () =>
            {
                foreach (var deck in new[] { "character", "strategy" }.Where(id => DeckChoice.Offers(client.Board.View, id)))
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Decks.Root.Q<Button>("deck-" + deck), deck + " deck");
            });
        }

        // Base: the Fellowship phase beside Mordor — the gate enters and the hidden Fellowship's figure declares.
        private static async Task MordorGate()
        {
            using var room = await Connect("ui34-mordor-morannon-True", ScenarioKind.Interaction, openPanels: false);
            var client = room.Fp;
            Assert.AreEqual(DecisionKind.FellowshipPhase, client.Board.View.Decision.Kind);
            await Audit(room, client, "mordor-gate", "gate on the map", async () =>
            {
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Map.MordorTrack.GateButton("morannon"), "Morannon's gate");
                if (client.Board.Map.FellowshipMarker.DeclareOffered)
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Map.FellowshipMarker.Root, "the Fellowship figure");
            }, "morannon");
        }

        // Base The Nazgûl Strike!: first its flight's lit Nazgûl on the map, then (UI-430) the Hunt Box in the console and the
        // red Free Peoples cards in Effects.
        private static async Task NazgulStrike()
        {
            using var room = await Connect("shadow-strike", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            var board = client.Board;
            // The flight brings its lit Nazgûl into the free map once (UI-434); here at each layout.
            var sources = board.Map.Query(className: "eligible-army").ToList().Select(force => Region(force.name)).ToArray();
            Assert.IsNotEmpty(sources, "The Nazgûl that may fly glow.");
            await Audit(room, client, "strike-flight", "Click a lit Nazgûl, then where it flies", async () =>
            {
                foreach (var force in board.Map.Query(className: "eligible-army").ToList())
                    await GlowingTargetChecks.AssertVisiblyOffered(board, force, force.name);
            }, sources);
            FellowshipGestures.Region(client, "barad-dur");
            await room.Settle();
            FellowshipGestures.Destination(client, "hollin");
            await room.Settle();
            await room.Confirm(client);
            await room.Until(() => board.View.Decision?.Presentation == NazgulStrikeChoice.Presentation);
            await Audit(room, client, "strike", "Hunt Box", async () =>
            {
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Hunt.Tab, "the Hunt Box");
                await RedTableCards(room, client);
            });
        }

        // Base Worn with Sorrow and Toil (UI-432): the Free Peoples' red hand counter in the top bar.
        private static async Task Worn()
        {
            using var room = await Connect("shadow-worn", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            await Audit(room, client, "worn", "Random card: red hand counter", async () =>
            {
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.TopBar.Opponent.Cards, "the red hand counter");
                await RedTableCards(room, client);
            });
        }

        // Base Lure of the Ring (UI-431): the panel opens with Gimli's red portrait and the offered corruption marker.
        private static async Task Lure()
        {
            using var room = await Connect("shadow-lure", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var board = client.Board;
            await Audit(room, client, "lure", "Click Gimli's red portrait", async () =>
            {
                if (!board.Fellowship.IsOpen)
                    board.Fellowship.Open();
                await room.Settle();
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Fellowship.Root.Q<Button>("fellowship-portrait-base.character.gimli"),
                    "Gimli's portrait");
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Fellowship.CorruptionMarker, "the corruption marker");
            });
        }

        // Base The Black Captain Commands (UI-431): the Reserve's Nazgûl, behind the lit Reserve tab, and the map's.
        private static async Task BlackCaptain()
        {
            using var room = await Connect("shadow-captain", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            var board = client.Board;
            await Audit(room, client, "captain", "Click a glowing Nazgûl in the Reserve", async () =>
            {
                await GlowingTargetChecks.AssertTabLit(board, board.Reinforcements.Tab, "the Reserve tab");
                board.Reinforcements.Open();
                await room.Settle();
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Reinforcements.Root.Q<Button>("captain-reserve-sauron-Nazgul"),
                    "the Reserve's Nazgûl");
                board.Reinforcements.Close();
                await room.Settle();
                // The map's Nazgûl: the order's reveal brings them out from under the console (UI-434).
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Map.Q("army-osgiliath-Shadow"), "Osgiliath's Nazgûl");
            }, "osgiliath");
        }

        // Work of the Enemy, the Kings card this branch carries (UI-431, as Dreadful Spells): the lit region whose panel
        // holds the forces.
        private static async Task WorkOfTheEnemy()
        {
            using var room = await Connect("kings-enemy-army", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            await Audit(room, client, "enemy-army", "Click the lit region", () =>
            {
                foreach (var region in client.Board.Map.DraftOverlay.Targets)
                    GlowingTargetChecks.AssertRegionOffered(client.Board, region);
                return Task.CompletedTask;
            });
        }

        // Base: a discard down to six — the hand's red cards (UI-222).
        private static async Task HandDiscard()
        {
            using var room = await Connect("ui31-discard", ScenarioKind.BaseAcceptance, openPanels: false);
            var client = room.Fp.Board.View.Decision?.Kind == DecisionKind.DiscardCards ? room.Fp : room.Sh;
            await Audit(room, client, "hand-discard", null, async () =>
            {
                var cards = client.Board.Hand.Query(className: HandCard.DiscardClass).ToList();
                Assert.IsNotEmpty(cards, "The cards to discard are red.");
                foreach (var card in cards)
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, card, card.name);
            });
        }

        // Base Scouts (UI-432): the battle's region and the retreat regions light on the map.
        private static async Task Scouts()
        {
            using var room = await Connect("strategy-scouts", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var region = client.Board.View.Battle.RegionId;
            // At 1920 × 1080 at 125 % the battle window's default place covers Helm's Deep, and the map cannot pan its west
            // edge past the screen to clear it: a limit left as it is (UX_SPEC, UI-434), so this case is checked at 1366 × 768.
            await AuditAt(new[] { (1366, 1f) }, room, client, "scouts", "to withdraw into its stronghold, or a lit region to retreat", () =>
            {
                GlowingTargetChecks.AssertRegionOffered(client.Board, region);
                GlowingTargetChecks.AssertRegionOffered(client.Board, "fords-of-isen");
                return Task.CompletedTask;
            });
        }

        // Each layout: the hint, the objects, and a screenshot.
        // reveal: the map regions a decision's own reveal brought into the free map; it ran once at the window's first size,
        // so it runs again at each layout (UI-434, MapCanvas.Reveal).
        private static Task Audit(MatchTestRoom room, MountedMatchClient client, string name, string hint, Func<Task> check,
            params string[] reveal) => AuditAt(Layouts, room, client, name, hint, check, reveal);

        private static async Task AuditAt((int Width, float Scale)[] layouts, MatchTestRoom room, MountedMatchClient client, string name,
            string hint, Func<Task> check, params string[] reveal)
        {
            foreach (var (width, scale) in layouts)
                using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, width, scale))
                {
                    await room.Settle();
                    await Task.Delay(200);
                    if (reveal.Length > 0)
                    {
                        client.Board.Map.Reveal(reveal);
                        await room.Settle();
                    }
                    if (hint != null)
                        StringAssert.Contains(hint, Hint(client), "The hint names the object.");
                    LayoutTests.Save(viewport.Texture, $"c434-{name}-{width}-{scale:0.00}.png");
                    await check();
                }
        }

        // "army-barad-dur-Shadow" → "barad-dur".
        private static string Region(string force) => force.Substring("army-".Length, force.LastIndexOf('-') - "army-".Length);

        // The console's hint, or the tray's own when the decision lives in a panel.
        private static string Hint(MountedMatchClient client) => client.Board.Console.Status.Hint.text + " | " +
            string.Join(" | ", client.Board.Tray.Query<Label>(className: "draft-status").ToList().Select(label => label.text));

        // The Effects tab lights; opened, every table card the effect lets the player discard is red.
        private static async Task RedTableCards(MatchTestRoom room, MountedMatchClient client)
        {
            var board = client.Board;
            await GlowingTargetChecks.AssertTabLit(board, board.TableEffects.Tab, "the Effects tab");
            board.TableEffects.Open();
            await room.Settle();
            var tiles = board.TableEffects.Root.Query(className: TableEffectsView.DiscardClass).ToList();
            Assert.IsNotEmpty(tiles, "A table card is red.");
            foreach (var tile in tiles)
                await GlowingTargetChecks.AssertVisiblyOffered(board, tile, tile.name);
            board.TableEffects.Close();
            await room.Settle();
        }
    }
}
