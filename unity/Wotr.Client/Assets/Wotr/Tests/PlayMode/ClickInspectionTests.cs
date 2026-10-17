using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class ClickInspectionTests
    {
        [UnityTest]
        public IEnumerator BothPlayersInspectBoardFamiliesOnlyAfterExplicitActivation() => Wait(BoardFamilies());

        [UnityTest]
        public IEnumerator EffectsAndTheirCardsStayClosedUntilClicked() => Wait(Effects());

        [UnityTest]
        public IEnumerator HistoryHoverAndFocusDoNotLoadDetailsOrAcknowledgeActions() => Wait(History());

        private static async Task BoardFamilies()
        {
            using var room = await Connect("ui66-full", ScenarioKind.Interaction, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var original = JsonConvert.SerializeObject(board.View);
                board.Fellowship.Close();
                board.Hunt.Close();
                Assert.IsTrue(board.HandDrawer.Docked && board.HandDrawer.IsOpen);
                Assert.AreEqual(DisplayStyle.None, board.HandDrawer.Tab.resolvedStyle.display);
                var card = board.Hand.Query<Button>(className: "hand-card").First();
                await Silent(card, room);
                Assert.IsEmpty(board.Cards.CardId, "Hover unfolds the card inside the permanent hand.");
                Assert.AreEqual(card.name, board.ExpandedCard);
                PointerGestureEvents.ClickAt(card);
                await room.Settle();
                Assert.IsEmpty(board.Cards.CardId);
                await ComponentGestures.Hover(board.Phase);
                Assert.IsEmpty(board.Cards.CardId);
                board.Cards.Close();
                board.HandDrawer.Close();
                await Inspect(board.Nations.Root, () => board.Nations.Drawer.style.display.value == DisplayStyle.Flex, room, board.Nations.Drawer);
                var token = board.Nations.Drawer.Query<Button>(className: "nation-token").First();
                await Silent(token, room);
                PointerGestureEvents.ClickAt(token);
                await room.Settle();
                Assert.IsEmpty(board.ActionDie.DieId);
                board.Nations.Close();
                await Inspect(board.Characters.Tab, () => board.Characters.Root.style.display.value == DisplayStyle.Flex, room, board.Characters.Root);
                var portrait = board.Characters.Root.Query<Button>(className: "character-roster-row").ToList().FirstOrDefault()
                    ?? board.Characters.Root.Query<Button>().ToList().First(b => b.name.StartsWith("character-roster-", StringComparison.Ordinal));
                await Inspect(portrait, () => board.Characters.Inspector.Root.style.display.value == DisplayStyle.Flex, room);
                board.Characters.Close();
                await Inspect(board.TableEffects.Tab, () => board.TableEffects.IsOpen, room, board.TableEffects.Root);
                board.TableEffects.Close();
                await BoardNavigation.Victory(board);
                board.Victory.Close();
                board.Fellowship.Close();
                board.Hunt.Close();
                await room.Settle();
                Assert.AreEqual(board.Fellowship.Held, board.Fellowship.IsOpen,
                    "The current Fellowship decision keeps its panel open.");
                Assert.IsFalse(board.Hunt.IsOpen, "UI-162: a closed Hunt window stays closed until the next Hunt.");
                // UI-101: the sovereign buttons live in the Characters panel, which unrolls from its tab.
                var sovereign = await BoardNavigation.Sovereign(board, "kings.sovereign.dain");
                await Inspect(sovereign, () => board.Sovereigns.IsOpen ||
                    board.Characters.Inspector.Root.style.display.value == DisplayStyle.Flex, room);
                board.Sovereigns.Close();
                board.Characters.Close();
                Assert.AreEqual(original, JsonConvert.SerializeObject(board.View));
                Assert.IsEmpty(board.ActionDie.DieId);
            }
            await CornerPlaques();
        }

        private static async Task CornerPlaques()
        {
            using var room = await Connect("map15-navigation", ScenarioKind.Interaction, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var original = JsonConvert.SerializeObject(board.View);
                Assert.IsFalse(board.Fellowship.Held || board.Hunt.Held);
                await Inspect(board.Fellowship.Status, () => board.Fellowship.IsOpen, room, board.Edges.CornerPanels);
                board.Fellowship.Close();
                board.Hunt.Close();
                // UI-160: the Hunt plaque in the console opens its panel on a click, never on hover or focus.
                await Inspect(board.Hunt.Tab, () => board.Hunt.IsOpen, room);
                board.Fellowship.Close();
                board.Hunt.Close();
                Assert.AreEqual(original, JsonConvert.SerializeObject(board.View));
                Assert.IsEmpty(board.ActionDie.DieId);
            }
        }

        private static async Task Effects()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                await Inspect(board.TableEffects.Tab, () => board.TableEffects.IsOpen, room, board.TableEffects.Root);
                var source = board.TableEffects.Root.Query<Button>(className: "effect-source").First();
                await Inspect(source, () => board.TableEffects.SelectedSource.Length > 0, room);
                Assert.IsNotEmpty(board.TableEffects.Root.Q<Label>("effect-card-event").text, "UI-94: the card text is in the panel.");
                Assert.IsEmpty(board.Cards.CardId);
                Assert.AreEqual(revision, board.View.Revision);
            }
        }

        private static async Task History()
        {
            using var boundary = new InspectionBoundary();
            using var room = await Connect("ui41-history", ScenarioKind.BaseAcceptance,
                fpHandler: boundary, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            await Inspect(board.Log.Toggle, () => board.Log.IsOpen, room);
            await room.Until(() => board.Log.Model.Loaded);
            await room.Settle();
            var entry = board.Log.Root.Query<Button>(className: "action-log-open").First();
            var reads = boundary.DetailReads;
            await Silent(entry, room);
            Assert.IsEmpty(board.Log.SelectedId);
            Assert.AreEqual(reads, boundary.DetailReads, "Hover/focus must not load history details.");
            PointerGestureEvents.ClickAt(entry);
            await room.Until(() => board.Log.SelectedId.Length > 0);
            await room.Settle();
            Assert.IsTrue(board.Log.Pinned);
            board.Log.CloseCard();
            board.Log.Close();
            var notice = board.Notifications.Root.Query<Button>(className: "action-notice").ToList().FirstOrDefault();
            Assert.NotNull(notice, "The history scenario includes unread opponent actions.");
            reads = boundary.DetailReads;
            await Silent(notice, room);
            Assert.IsEmpty(board.Log.SelectedId);
            Assert.AreEqual(reads, boundary.DetailReads);
            PointerGestureEvents.ClickAt(notice);
            await room.Until(() => board.Log.SelectedId.Length > 0);
            board.Log.CloseCard();
            if (board.Notifications.More.style.display.value == DisplayStyle.Flex)
            {
                await Inspect(board.Notifications.More,
                    () => board.Notifications.Overflow.style.display.value == DisplayStyle.Flex, room);
                board.Notifications.Close();
            }
            Assert.AreEqual(0, boundary.Commands);
            Assert.AreEqual(revision, board.View.Revision);
        }

        // UI-92/UI-98: an edge tab or plaque unrolls its panel on hover/focus and a click never pins it;
        // every other source stays closed until clicked.
        private static async Task Inspect(VisualElement target, Func<bool> opened, MatchTestRoom room, VisualElement preview = null)
        {
            Assert.NotNull(target);
            Assert.IsFalse(opened(), target.name + " initially closed");
            await Silent(target, room);
            if (preview == null)
                Assert.IsFalse(opened(), target.name + " must not open on hover/focus");
            else
            {
                Assert.IsTrue(opened() && preview.ClassListContains(LeftRailPreview.PreviewClass), target.name + " unrolls its panel");
                // The full corner form covers its plaque; hover is the complete input.
                return;
            }
            PointerGestureEvents.ClickAt(target);
            await room.Settle();
            Assert.IsTrue(opened(), target.name + " opens on click");
        }

        private static async Task Silent(VisualElement target, MatchTestRoom room)
        {
            await ComponentGestures.Hover(target);
            target.Focus();
            await Task.Delay(250, room.Token);
        }

        private sealed class InspectionBoundary : DelegatingHandler
        {
            public int DetailReads { get; private set; }
            public int Commands { get; private set; }
            public InspectionBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var path = request.RequestUri.AbsolutePath;
                if (request.Method == HttpMethod.Get && (path.Contains("/actions/") || path.Contains("/battles/")))
                    DetailReads++;
                if (request.Method == HttpMethod.Post && path.EndsWith("/commands", StringComparison.Ordinal))
                    Commands++;
                return base.SendAsync(request, token);
            }
        }
    }
}
