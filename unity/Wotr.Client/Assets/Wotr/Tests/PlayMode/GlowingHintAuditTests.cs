using System;
using System.Collections;
using System.Collections.Generic;
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
    // .artifacts/screenshots/ui434-<case>-<width>-<scale>.png. A seed is played once per run, so the tests use distinct
    // seeds and the class runs in one PlayMode run.
    public sealed class GlowingHintAuditTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1920, 1.25f) };

        [UnityTest] public IEnumerator TheSpecialDiceToKeepGlowInTheOwnPool() => Wait(SpecialDice());
        [UnityTest] public IEnumerator TheDiceTheRulerDiscardTakesGlowInTheOwnPool() => Wait(RulerDiscard());
        [UnityTest] public IEnumerator BothRulerDiesGlowForDismayBeforeLoreEachInItsPool() => Wait(Dismay("ui58-dismay-before-lore", "dismay-before-lore"));
        [UnityTest] public IEnumerator BothRulerDiesGlowForDismayEachInItsPoolAndTheHintNamesBoth() => Wait(Dismay("ui09-dismay", "dismay"));
        [UnityTest] public IEnumerator VilyasSocketAndThePaidDieGlow() => Wait(Vilya());
        [UnityTest] public IEnumerator NenyasSocketGlowsInTheTileStep() => Wait(Nenya());
        [UnityTest] public IEnumerator TheRingsThreeRingsRecoversGlowInTheConsole() => Wait(ThreeRings());
        [UnityTest] public IEnumerator TheDiceLoreMayChangeGlowInTheOwnPool() => Wait(Lore());
        [UnityTest] public IEnumerator TheRingsARemovalMayPayWithGlowInTheConsole() => Wait(RingPayment());
        [UnityTest] public IEnumerator TheDecksADeckChoiceOffersGlowBesideTheHand() =>
            Wait(Decks("ui348-anarion", Side.FreePeoples, "decks"));
        [UnityTest] public IEnumerator TheDecksThePalantirOfOrthancsDrawOffersGlowBesideTheHand() =>
            Wait(Decks("shadow-palantir-draw", Side.Shadow, "orthanc-decks"));
        [UnityTest] public IEnumerator TheMordorGateAndTheFellowshipFigureGlowInTheFellowshipPhase() => Wait(MordorGate());
        [UnityTest] public IEnumerator TheChieftainsRowAndTheShadowNationsGlowBehindTheirLitTabs() => Wait(CouncilPolitics());
        [UnityTest] public IEnumerator TheNazgulStrikesHuntBoxAndRedTableCardsGlow() => Wait(NazgulStrike());
        [UnityTest] public IEnumerator WornWithSorrowsRedHandCounterGlowsInTheTopBar() => Wait(Worn());
        [UnityTest] public IEnumerator LuresRedPortraitAndCorruptionMarkerGlowInTheOpenFellowshipPanel() => Wait(Lure());
        [UnityTest] public IEnumerator TheBlackCaptainsReserveNazgulGlowBehindTheLitReserveTab() => Wait(BlackCaptain());
        [UnityTest] public IEnumerator WorkOfTheEnemysRegionIsLitOnScreen() => Wait(WorkOfTheEnemy());
        [UnityTest] public IEnumerator MonstersRousedsRegionsAreLitOnScreen() => Wait(Monsters());
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

        // Lords special dice: the Free Peoples keep one of their lit special dice in the pool beside the console.
        private static async Task SpecialDice()
        {
            using var room = await Connect("ui267-tie", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            Assert.AreEqual(DecisionKind.SpecialDieChoice, client.Board.View.Decision.Kind);
            await Audit(room, client, "special-dice", "Click a highlighted die to keep it", () => OwnDice(client, Offered(client)));
        }

        // Kings Ruler discard: after both special-die choices, the Shadow's dice the discard may take glow.
        private static async Task RulerDiscard()
        {
            using var room = await Connect("ui09-dice", ScenarioKind.CardPack, openPanels: false);
            await KeepFirstOffered(room, room.Sh);
            await KeepFirstOffered(room, room.Fp);
            var client = room.Sh;
            await room.Until(() => client.Board.View.Decision?.Kind == DecisionKind.RulerDiscard);
            await Audit(room, client, "ruler-discard", "Click a glowing die to discard it", () => OwnDice(client, Offered(client)));
        }

        // Kings Dismay: the Shadow may reroll either Ruler die — the Free Peoples' glows in the top bar (UI-433), its own in its
        // pool beside the console — and the hint names both places.
        private static async Task Dismay(string seed, string name)
        {
            using var room = await Connect(seed, ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            var dice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseDismay).EligibleDice;
            var own = dice.Where(id => client.Board.View.Dice.Single(d => d.Id == id).Owner == Side.Shadow).ToList();
            var opponent = dice.Except(own).ToList();
            var hint = own.Count > 0 && opponent.Count > 0 ? "Click a glowing Ruler die, yours or the top bar's, to reroll it with Dismay."
                : own.Count > 0 ? "Click your glowing Ruler die" : "Click the glowing Ruler die in the top bar";
            await Audit(room, client, name, hint, async () =>
            {
                foreach (var id in own)
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.OwnDice.Q<Button>(id), "own Ruler die " + id);
                foreach (var id in opponent)
                    await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.OpponentDice.Q<Button>(id), "Ruler die " + id);
            });
        }

        // Lords Vilya: after a paid action its console socket keeps the paid die, which glows in the pool too.
        private static async Task Vilya()
        {
            using var room = await Connect("ui09-vilya", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var die = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseVilya).EligibleDice.Single();
            await Audit(room, client, "vilya", "Click the glowing Vilya in the console", async () =>
            {
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Console.Rings.For(NamedRings.Vilya), "Vilya's socket");
                await OwnDice(client, new[] { die });
            });
        }

        // Lords Nenya: in the Hunt's tile step its console socket replaces the Eye tile.
        private static async Task Nenya()
        {
            using var room = await Connect("t34-nenya", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            await Audit(room, client, "nenya", null, async () =>
            {
                StringAssert.Contains("click the glowing Nenya in the console", client.Board.Tray.Q<Label>("nenya-socket-hint").text);
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Console.Rings.For(NamedRings.Nenya), "Nenya's socket");
            });
        }

        // Lords Three Rings for the Elven-Kings: the Rings it may recover glow in their console sockets.
        private static async Task ThreeRings()
        {
            using var room = await Connect("ui272-three-rings", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var rings = client.Board.View.Options.Single(o => o.Enabled && o.Kind == CommandKind.ChooseEffect).EligibleIds;
            await Audit(room, client, "three-rings", "glowing Rings in the console", () => Sockets(client, rings));
        }

        // Lords The Lore of the Elven Rings: the dice it may change glow in the Shadow's pool.
        private static async Task Lore()
        {
            using var room = await Connect("lords-lore-choice", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            await Audit(room, client, "lore", "Click a lit die to change its result with Lore", () => OwnDice(client, Candidates(client)));
        }

        // Lords: a removal paid with a die and a Ring waits for the Ring, which glows in its console socket.
        private static async Task RingPayment()
        {
            using var room = await Connect("lords-lore-remove", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var die = client.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army);
            var rings = client.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardLore).EligibleIds;
            await ComponentGestures.Cost(room, client, die.Id);
            await ComponentGestures.Remove(client, "lords.sh.character.26");
            await room.Settle();
            client.Board.TableEffects.Close();
            await Audit(room, client, "ring-payment", "Click a lit Elven Ring", () => Sockets(client, rings));
        }

        // A deck choice (UI-390) — Kings Palantír of Anárion's extra card, the base Palantír of Orthanc's draw: the decks the
        // card may come from glow beside the hand, in the opened drawer.
        private static async Task Decks(string seed, Side side, string name)
        {
            using var room = await Connect(seed, ScenarioKind.CardPack, openPanels: false);
            var client = side == Side.Shadow ? room.Sh : room.Fp;
            Assert.AreEqual(DeckChoice.Presentation, client.Board.View.Decision.Presentation);
            await Audit(room, client, name, "Click a glowing deck beside your hand", async () =>
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

        // Lords Council with Kings: a marked token lights the Chieftain's row (Characters) and the Shadow nations (Politics).
        private static async Task CouncilPolitics()
        {
            using var room = await Connect("ui09-council-politics", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            var board = client.Board;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("pool-council.advance-politics"));
            await room.Settle();
            await Audit(room, client, "council-politics", "Click a lit Chieftain's row in Characters to enter him, or a glowing Shadow nation's token or → in Politics.", async () =>
            {
                await GlowingTargetChecks.AssertTabLit(board, board.Characters.Tab, "the Characters tab");
                await GlowingTargetChecks.AssertTabLit(board, board.Nations.Root, "the Politics tab");
                board.Characters.Open();
                await room.Settle();
                await GlowingTargetChecks.AssertVisiblyOffered(board, board.Characters.Root.Q<Button>("character-roster-kings.character.black-serpent"),
                    "the Black Serpent's row");
                board.Characters.Close();
                board.Nations.Open();
                await room.Settle();
                var tokens = board.Nations.Drawer.Query(className: ChoiceGlow.ClassName).ToList();
                Assert.IsNotEmpty(tokens, "A Shadow nation's token glows in Politics.");
                foreach (var token in tokens)
                    await GlowingTargetChecks.AssertVisiblyOffered(board, token, token.name);
                board.Nations.Close();
                await room.Settle();
            });
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

        // Kings Work of the Enemy (UI-431): the lit region whose panel holds the forces.
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

        // Fate Monsters Roused (UI-431): the regions of both halves, revealed once for the decision.
        private static async Task Monsters()
        {
            using var room = await Connect("fate-monsters", ScenarioKind.CardPack, openPanels: false);
            var client = room.Sh;
            await Audit(room, client, "monsters", "Click a lit region west or east of the Misty Mountains", () =>
            {
                foreach (var region in new[] { "angmar", "ettenmoors", "weather-hills", "trollshaws", "western-mirkwood", "northern-mirkwood",
                             "withered-heath" })
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

        // Base Scouts (UI-432): the battle's region and the retreat regions light on the map. UI-435: at 1920 × 1080 at 125 %
        // the window's middle place covered Helm's Deep and the console hid it; the window now keeps to the middle's top
        // and the camera brings both regions into the strip below it, in both layouts.
        private static async Task Scouts()
        {
            using var room = await Connect("strategy-scouts", ScenarioKind.CardPack, openPanels: false);
            var client = room.Fp;
            var region = client.Board.View.Battle.RegionId;
            await AuditAt(Layouts, room, client, "scouts", "to withdraw into its stronghold, or a lit region to retreat", () =>
            {
                Assert.IsTrue(client.Board.Battles.IsOpen, "The battle window stays open.");
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
                    LayoutTests.Save(viewport.Texture, $"ui434-{name}-{width}-{scale:0.00}.png");
                    await check();
                }
        }

        // "army-barad-dur-Shadow" → "barad-dur".
        private static string Region(string force) => force.Substring("army-".Length, force.LastIndexOf('-') - "army-".Length);

        // The console's hint, or the tray's own when the decision lives in a panel.
        private static string Hint(MountedMatchClient client) => client.Board.Console.Status.Hint.text + " | " +
            string.Join(" | ", client.Board.Tray.Query<Label>(className: "draft-status").ToList().Select(label => label.text));

        private static IReadOnlyList<string> Offered(MountedMatchClient client) => client.Board.View.Options.Where(o => o.Enabled)
            .SelectMany(o => o.EligibleDice).Distinct().ToList();

        private static IReadOnlyList<string> Candidates(MountedMatchClient client) => client.Board.OwnDice
            .Query<Button>(className: "resource-candidate").ToList().Select(button => button.name).ToList();

        private static async Task OwnDice(MountedMatchClient client, IEnumerable<string> dice)
        {
            var ids = dice.ToList();
            Assert.IsNotEmpty(ids, "The decision offers dice.");
            foreach (var id in ids)
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.OwnDice.Q<Button>(id), "die " + id);
        }

        private static async Task Sockets(MountedMatchClient client, IEnumerable<string> rings)
        {
            foreach (var ring in rings)
                await GlowingTargetChecks.AssertVisiblyOffered(client.Board, client.Board.Console.Rings.For(ring), "the socket of " + ring);
        }

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

        private static async Task KeepFirstOffered(MatchTestRoom room, MountedMatchClient client)
        {
            await room.Until(() => client.Board.View.Decision?.Kind == DecisionKind.SpecialDieChoice && client.Board.View.DecisionOwner == client.Board.View.You);
            await room.Settle();
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>(Offered(client).First()));
            await room.At(revision + 1);
        }
    }
}
