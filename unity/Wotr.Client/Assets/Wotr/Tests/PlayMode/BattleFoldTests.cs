using System.Collections;
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
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-521 (owner, choosing UI-517's first direction: "fold it into a small plaque, keeping the clickable buttons"): the
    // battle window folds into a small plaque — its header, the step and the footer's buttons, which keep working; the scene,
    // the ribbon and the forces wait for the unfold; the player's own losses unfold it.
    public sealed class BattleFoldTests
    {
        [UnityTest]
        public IEnumerator TheFoldedPlaqueKeepsItsButtonsInFourLayouts()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
                    var backend = new RecordingBackend(view);
                    using var client = new MountedMatchClient("http://localhost", view.MatchId, backend);
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Apply(view);
                    client.SetOnline();
                    yield return Frames(4);
                    var battles = client.Board.Battles;
                    if (!battles.IsOpen)
                        CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                    yield return Frames(4);
                    var label = $"{width}×{scale:0.00}";
                    var open = battles.Root.worldBound;
                    var fold = battles.Root.Q<Button>("fold-battle");
                    Assert.IsNotNull(fold, "The header has a fold button · " + label);
                    CardNetworkTests.Click(fold);
                    yield return Frames(6);
                    Assert.IsTrue(battles.Folded, label);
                    var plaque = battles.Root.worldBound;
                    Assert.AreEqual(RegionalBattlePanel.FoldedWidth, plaque.width, 1f, "A small plaque · " + label);
                    Assert.Less(plaque.height, open.height / 2, "Much lower than the window · " + label);
                    Assert.AreEqual(open.xMin, plaque.xMin, 1f, "It stays where the window was · " + label);
                    foreach (var hidden in new[] { battles.Root.Q("battle-scene"), battles.Root.Q(className: "stage-ribbon"), battles.Root.Q("battle-forces") })
                        Assert.AreEqual(DisplayStyle.None, hidden.resolvedStyle.display, hidden.name + " waits for the unfold · " + label);
                    var step = battles.Root.Q<Label>("battle-folded-step");
                    Assert.AreEqual(DisplayStyle.Flex, step.resolvedStyle.display, label);
                    StringAssert.StartsWith("You · ", step.text, "The plaque names the step · " + label);
                    var go = battles.Root.Q<Button>("action-ContinueBattle");
                    Assert.IsNotNull(go, label);
                    Assert.AreEqual(DisplayStyle.Flex, go.resolvedStyle.display, "The buttons stay · " + label);
                    Assert.IsTrue(plaque.Contains(go.worldBound.center), "Inside the plaque · " + label);
                    Assert.IsTrue(plaque.Contains(battles.Root.Q<Button>("action-StopBattle").worldBound.center), label);
                    LayoutTests.Save(target, $"ui521-folded-{width}-{scale:0.00}.png");
                    CardNetworkTests.Click(go);
                    var deadline = Time.realtimeSinceStartup + 5;
                    while (backend.Commands < 1 && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    Assert.AreEqual(1, backend.Commands, "A folded button still sends · " + label);
                    Assert.AreEqual(CommandKind.ContinueBattle, backend.Last.Kind, label);
                    CardNetworkTests.Click(fold);
                    yield return Frames(6);
                    Assert.IsFalse(battles.Folded, label);
                    Assert.AreEqual(DisplayStyle.Flex, battles.Root.Q("battle-forces").resolvedStyle.display, "Unfolded, the forces return · " + label);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        [UnityTest]
        public IEnumerator ThePlayersOwnLossesUnfoldThePlaque()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Frames(4);
            var battles = client.Board.Battles;
            if (!battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            yield return Frames(4);
            CardNetworkTests.Click(battles.Root.Q<Button>("fold-battle"));
            yield return Frames(4);
            Assert.IsTrue(battles.Folded);
            var losses = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            losses.Revision = view.Revision + 1;
            client.Apply(losses);
            yield return Frames(4);
            Assert.IsFalse(battles.Folded, "The losses are picked inside the window, so it unfolds.");
            Assert.AreEqual(DisplayStyle.Flex, battles.Root.Q("battle-forces").resolvedStyle.display);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }

        private sealed class RecordingBackend : HttpMessageHandler
        {
            private readonly PlayerView view;
            public int Commands;
            public GameCommand Last;

            public RecordingBackend(PlayerView view) => this.view = view;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                object response = view;
                if (request.RequestUri.AbsolutePath.EndsWith("/preview"))
                    response = new ActionPreview { Legal = true, Revision = view.Revision };
                if (request.RequestUri.AbsolutePath.EndsWith("/commands"))
                {
                    Last = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    Commands++;
                    response = new CommandReceipt { Accepted = true, Revision = view.Revision, CommandId = Last.CommandId };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response)) };
            }
        }
    }
}
