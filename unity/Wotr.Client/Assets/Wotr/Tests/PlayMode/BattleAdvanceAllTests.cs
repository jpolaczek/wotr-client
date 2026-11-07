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
    // UI-515 (owner): after a won battle one button advances every figure that fought into the battle's region: one
    // click sends the advance with every eligible figure; Select all and Stay stay beside it in the footer.
    public sealed class BattleAdvanceAllTests
    {
        [UnityTest]
        public IEnumerator AdvanceAllSendsEveryEligibleFigureInOneClick()
        {
            var view = BattleScreenFixture.Create(CommandKind.AdvanceBattle);
            var eligible = view.Options.Single(o => o.Kind == CommandKind.AdvanceBattle).EligibleIds.ToList();
            Assert.Greater(eligible.Count, 1, "The fixture's attacker has several figures.");
            var backend = new RecordingBackend(view);
            using var client = new MountedMatchClient("http://localhost", view.MatchId, backend);
            var target = new RenderTexture(1366, 768, 24);
            target.Create();
            client.Panel.targetTexture = target;
            client.Panel.scale = 1.25f;
            client.Apply(view);
            client.SetOnline();
            yield return Settle();
            if (!client.Board.Battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            yield return Settle();
            var footer = client.Board.Tray;
            var all = footer.Q<Button>("battle-advance-all");
            Assert.IsNotNull(all, "Advance all stands in the footer.");
            Assert.IsTrue(all.enabledSelf);
            StringAssert.Contains("Lossarnach", all.tooltip);
            foreach (var name in new[] { "battle-select-all", "battle-stay" })
            {
                var other = footer.Q<Button>(name);
                Assert.IsNotNull(other, name);
                Assert.AreEqual(other.worldBound.center.y, all.worldBound.center.y, 1, name + " on the same row");
                Assert.IsFalse(other.worldBound.Overlaps(all.worldBound), name + " beside it");
            }
            var text = all.MeasureTextSize(all.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(text.x, all.contentRect.width + .5f, "The label fits its button.");
            LayoutTests.Save(target, "ui515-advance-all.png");
            CardNetworkTests.Click(all);
            var deadline = Time.realtimeSinceStartup + 5;
            while (backend.Commands < 1 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(1, backend.Commands, "One click sends one command.");
            Assert.AreEqual(CommandKind.AdvanceBattle, backend.Last.Kind);
            CollectionAssert.AreEquivalent(eligible, backend.Last.Payload.Ids);
            client.Panel.targetTexture = client.Viewport;
            target.Release();
            Object.Destroy(target);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 4; i++)
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
