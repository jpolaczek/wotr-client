using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class CardServerCompatibilityTests
    {
        private const string Message = "Server update required.\nAsk the host to update the server to discard cards one at a time.";

        [UnityTest]
        public IEnumerator LegacyDiscardOptionExplainsBothClickSurfacesWithoutPosting()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                foreach (var preview in new[] { false, true })
                {
                    var view = LegacyView(side);
                    using var backend = new RejectingBackend();
                    using var client = new MountedMatchClient("http://localhost", view.MatchId, backend);
                    client.Interface.Preferences.SetScale(1.25f);
                    client.Apply(view);
                    client.SetOnline();
                    yield return Settle();
                    var board = client.Board;
                    var snapshot = JsonConvert.SerializeObject(board.View);
                    var card = board.Hand.Q<Button>(view.Hand[0]);
                    using var mouse = new EngineMouseInput(board.Root);
                    // UI-142: an excess-hand discard is one click, on the card or on its unrolled rules.
                    StringAssert.DoesNotContain("Server update required", board.Tray.Q<Label>("discard-hint").text);
                    var point = card.worldBound.center;
                    if (preview)
                    {
                        mouse.Move(point);
                        yield return Settle();
                        Assert.AreEqual(card.name, board.ExpandedCard);
                        point = card.Q<Label>("hand-card-event").worldBound.center;
                    }
                    mouse.ClickAtPoint(point);
                    yield return Settle();
                    CheckMessage(board);
                    Assert.AreEqual(0, backend.Posts, "The legacy contract must not trigger a command or preview POST.");
                    Assert.AreEqual(snapshot, JsonConvert.SerializeObject(board.View));
                    Assert.IsFalse(card.ClassListContains("selected"), "The rejected gesture must not replace the local choice.");
                    Assert.IsNull(board.Tray.Q<Button>("confirm-decision"), "Do not introduce a batch-discard fallback.");
                }
        }

        [UnityTest]
        public IEnumerator RestoredDiscardChoiceSurvivesTheWarningAndServerUpdateWithoutAutomaticCommit() =>
            MatchTestRoom.Wait(RestoreAfterUpdate());

        private static async Task RestoreAfterUpdate()
        {
            var storage = Path.Combine(Path.GetTempPath(), "wotr-ui83-" + Guid.NewGuid().ToString("N"));
            LocalMatchPersistence saved = null;
            try
            {
                var view = LegacyView(Side.FreePeoples);
                saved = await OpenSaved(storage, view);
                var draft = new TurnDraft();
                draft.Choose(CommandKind.DiscardCards);
                draft.Change(payload => payload.Ids.Add(view.Hand[0]));
                saved.SaveDraft(new SavedTurnDraft
                {
                    Draft = draft.Snapshot(), Context = DraftContext.From(view), Origin = view.Decision.Id
                });
                await saved.FlushStable();
                saved = await OpenSaved(storage, view);
                var original = JsonConvert.SerializeObject(saved.Draft);
                await GuardSavedChoice(view, saved, original);

                // A new controller reads the same files, as when reopening a saved game.
                // The server update changes legal options, not the saved game revision.
                saved = await OpenSaved(storage, view);
                Assert.AreEqual(original, JsonConvert.SerializeObject(saved.Draft));
                view.Options.Single().Minimum = 1;
                await ReopenUpdated(view, saved, original);
            }
            finally
            {
                if (saved != null)
                    await saved.FlushStable();
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }

        private static Task<LocalMatchPersistence> OpenSaved(string storage, PlayerView view) =>
            LocalMatchPersistence.Open(storage, "http://localhost/", "compatibility-fixture", view.MatchId);

        private static async Task GuardSavedChoice(PlayerView view, LocalMatchPersistence saved, string original)
        {
            using var backend = new RejectingBackend();
            using var client = new MountedMatchClient("http://localhost", view.MatchId, backend, persistence: saved);
            client.Refresh(view);
            client.SetOnline();
            await Task.Delay(100);
            var board = client.Board;
            Assert.IsTrue(board.Hand.Q<Button>(view.Hand[0]).ClassListContains("selected"));
            using var mouse = new EngineMouseInput(board.Root);
            var otherCard = board.Hand.Q<Button>(view.Hand[1]);
            var point = otherCard.worldBound.center;
            mouse.Click(otherCard);
            await Task.Delay(100);
            var firstCount = mouse.PressCounts.Last();
            mouse.ClickAtPoint(point + Vector2.right);
            Assert.Greater(mouse.PressCounts.Last(), firstCount);
            await Task.Delay(100);
            CheckMessage(board);
            await saved.FlushStable();
            Assert.AreEqual(original, JsonConvert.SerializeObject(saved.Draft), "The warning must preserve the on-disk choice.");
            Assert.IsTrue(board.Hand.Q<Button>(view.Hand[0]).ClassListContains("selected"));
            Assert.IsFalse(otherCard.ClassListContains("selected"));
            Assert.AreEqual(0, backend.Posts);
        }

        private static async Task ReopenUpdated(PlayerView view, LocalMatchPersistence saved, string original)
        {
            Assert.IsNull(saved.Actions.Pending, "An unsent draft must not become a recoverable command.");
            Assert.IsNull(saved.Policy.Pending);
            using var backend = new PreviewBackend();
            using var client = new MountedMatchClient("http://localhost", view.MatchId, backend, persistence: saved);
            var snapshot = JsonConvert.SerializeObject(view);
            client.Refresh(view);
            client.SetOnline();
            await Task.Delay(150);
            Assert.Greater(backend.Previews, 0, "The restored one-card choice is now legal and must reach preview.");
            StringAssert.Contains("Discard selected cards face down.", client.Board.Tray.Q<Label>("discard-hint").text);
            Assert.AreEqual(CommandKind.DiscardCards, backend.LastPreview.Kind);
            CollectionAssert.AreEqual(new[] { view.Hand[0] }, backend.LastPreview.Payload.Ids);
            Assert.IsTrue(client.Board.Hand.Q<Button>(view.Hand[0]).ClassListContains("selected"));
            await saved.FlushStable();
            Assert.AreEqual(original, JsonConvert.SerializeObject(saved.Draft));
            Assert.IsNull(saved.Actions.Pending);
            Assert.AreEqual(0, backend.Commands, "Reopening and receiving a legal preview must wait for a new player gesture.");
            Assert.AreEqual(snapshot, JsonConvert.SerializeObject(client.Board.View));
        }

        // QUAL-19: the explanation reaches the console's hint (UI-160), which holds two lines and keeps the full text in
        // its tooltip; the tray it came from sits in the closed decision window.
        private static void CheckMessage(BoardScreen board)
        {
            var hint = board.Console.Status.Hint;
            Assert.AreEqual(Message, hint.text);
            StringAssert.Contains(Message, hint.tooltip);
            for (var element = (VisualElement)hint; element != null; element = element.parent)
            {
                Assert.AreNotEqual(DisplayStyle.None, element.resolvedStyle.display);
                Assert.AreEqual(Visibility.Visible, element.resolvedStyle.visibility);
            }
            Assert.LessOrEqual(hint.worldBound.xMax, board.Root.worldBound.xMax + 1);
            Assert.LessOrEqual(hint.worldBound.yMax, board.Root.worldBound.yMax + 1);
        }

        private static PlayerView LegacyView(Side side)
        {
            var view = new BoardFixture().View;
            view.MatchId = "legacy-discard-contract-" + side;
            view.You = side;
            view.DecisionOwner = side;
            view.Decision = new DecisionView { Id = "legacy-excess", Kind = DecisionKind.DiscardCards, RequiredCount = 2 };
            var prefix = side == Side.FreePeoples ? "base.fp.character." : "base.sh.character.";
            view.Hand = Enumerable.Range(1, 8).Select(index => prefix + index.ToString("D2")).ToList();
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.DiscardCards, Minimum = 2, Maximum = 2, EligibleIds = view.Hand.ToList()
            });
            return view;
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 4; frame++)
                yield return null;
        }

        // Explicit old-server projection fixture. No authoritative rules or real
        // account are simulated: every unexpected POST is counted and rejected.
        private sealed class RejectingBackend : HttpMessageHandler
        {
            public int Posts { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                if (request.Method == HttpMethod.Post)
                    Posts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"UnexpectedTestRequest\"}")
                });
            }
        }

        // Only the legal-preview response is supplied. Commands are counted and
        // rejected, so this fixture cannot act as a substitute rules server.
        private sealed class PreviewBackend : HttpMessageHandler
        {
            public int Previews { get; private set; }
            public int Commands { get; private set; }
            public GameCommand LastPreview { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var path = request.RequestUri.AbsolutePath;
                if (request.Method == HttpMethod.Post && path.EndsWith("/preview", StringComparison.Ordinal))
                {
                    Previews++;
                    LastPreview = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(new ActionPreview
                        {
                            Legal = true, Revision = LastPreview.ExpectedRevision
                        }))
                    };
                }
                if (request.Method == HttpMethod.Post && path.EndsWith("/commands", StringComparison.Ordinal))
                    Commands++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"UnexpectedTestRequest\"}")
                };
            }
        }
    }
}
