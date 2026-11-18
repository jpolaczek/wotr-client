using System.Collections;
using System.Collections.Generic;
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
    // UI-547 (owner, 2026-11-18: "if we recruit a unit and there is only one possible region, we do not place it
    // automatically, unless the effect by definition has no choice"): a Muster recruits in a settlement of the figure's
    // nation, a choice the rule gives, so a figure with one legal settlement is only taken up: nothing is sent, its one
    // settlement lights, and that settlement's click recruits there. Frame: ui547-muster-one-settlement.
    public sealed class MusterOneSettlementTests
    {
        [UnityTest]
        public IEnumerator AFigureWithOneLegalSettlementWaitsForThatSettlementsClick()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var group = view.Options.Single(o => o.Kind == CommandKind.Recruit).Recruitment.First(g => g.Targets.Count > 1);
            var only = group.Targets[0];
            group.Targets = new List<string> { only };
            var backend = new RecordingBackend(view);
            using var client = new MountedMatchClient("http://localhost", view.MatchId, backend);
            var target = new RenderTexture(1366, 768, 24);
            target.Create();
            try
            {
                client.Panel.targetTexture = target;
                client.Apply(view);
                client.SetOnline();
                yield return Settle();
                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                client.Board.Reinforcements.Open();
                yield return Settle();
                var token = client.Board.Reinforcements.Root.Q<Button>("reserve-token-" + group.Nation + "-" + group.Kind);
                Assert.IsNotNull(token, group.Nation + " " + group.Kind);
                CardNetworkTests.Click(token);
                yield return new WaitForSecondsRealtime(.6f);
                Assert.AreEqual(0, backend.Commands, "The figure's click sends nothing.");
                CollectionAssert.AreEqual(new[] { only }, client.Board.Map.DraftOverlay.PlacementTargets.ToList(), "Its one settlement lights.");
                Assert.AreEqual(MapRegionEmphasis.PlacementTarget, client.Board.Map.Renderer.CurrentEmphasis(only), only);
                LayoutTests.Save(target, "ui547-muster-one-settlement.png");
                MapContourProbe.ClickRegion(client.Board.Map, only);
                var deadline = Time.realtimeSinceStartup + 5;
                while (backend.Commands < 1 && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.AreEqual(1, backend.Commands, "The settlement's click recruits.");
                Assert.AreEqual(CommandKind.Recruit, backend.Last.Kind);
                StringAssert.Contains(only, JsonConvert.SerializeObject(backend.Last.Payload), "There.");
            }
            finally
            {
                client.Panel.targetTexture = client.Viewport;
                target.Release();
                Object.Destroy(target);
            }
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
