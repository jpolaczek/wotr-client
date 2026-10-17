using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public class SaveFailureLayoutTests
    {
        [UnityTest]
        public IEnumerator SaveFailureFitsBeforeTheFirstViewAndAllowsManualRetry()
        {
            foreach (var code in new[] { "CorruptSave", "UnsupportedSaveVersion" })
            {
                var backend = new FailedSave(code);
                using (var client = new MountedMatchClient("http://127.0.0.1:5088", "failed-save", backend))
                {
                    // MountedMatchClient omits navigation; ClientFlow owns this binding in production.
                    client.Board.Reconnect += client.Connection.Reconnect;
                    client.Panel.scale = 1.25f;
                    client.Connection.Start();
                    yield return Settle();
                    Assert.AreEqual("Save unavailable", client.Board.Connection.text);
                    var message = client.Board.Root.Q<Label>("match-loading");
                    Assert.IsFalse(string.IsNullOrEmpty(message.text));
                    Assert.IsFalse(message.text.Contains(code), "The visible explanation uses player language.");
                    InBounds(client.Board.Root, message);
                    InBounds(client.Board.Root, client.Board.Connection);
                    LayoutTests.Save(client.Viewport, "t37-" + code + "-1366-1.25.png");
                    Assert.AreEqual(1, backend.Requests);
                    CardNetworkTests.Click(client.Board.Connection);
                    yield return Settle();
                    Assert.AreEqual(2, backend.Requests, "An explicit check retries once.");
                    Assert.AreEqual("Save unavailable", client.Board.Connection.text);
                }
            }
        }

        [UnityTest]
        public IEnumerator SaveFailureKeepsTheLoadedMapAndExplainsWhyPlayIsUnavailable()
        {
            using (var client = new MountedMatchClient("http://127.0.0.1:5088", "failed-save", new FailedSave("CorruptSave")))
            {
                client.Panel.scale = 1.25f;
                var view = new BoardFixture().View;
                client.Apply(view);
                client.SetOnline();
                yield return Settle();
                client.Board.Map.Focus("minas-tirith");
                var pan = client.Board.Map.Camera.Pan;
                var zoom = client.Board.Map.Camera.Zoom;
                client.Connection.Start();
                yield return Settle();
                Assert.AreSame(view, client.Board.View);
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                Assert.AreEqual(zoom, client.Board.Map.Camera.Zoom);
                StringAssert.Contains("restore", client.Board.Connection.tooltip);
                Assert.IsFalse(client.Board.Tray.Q<Button>("confirm-decision")?.enabledInHierarchy ?? false);
                InBounds(client.Board.Root, client.Board.Connection);
                Assert.IsNotNull(client.Board.Map.panel.Pick(client.Board.Map.worldBound.center));
                LayoutTests.Save(client.Viewport, "t37-loaded-save-failure-1366-1.25.png");
            }
        }

        private static void InBounds(VisualElement root, VisualElement element)
        {
            Assert.Greater(element.worldBound.width, 0);
            Assert.GreaterOrEqual(element.worldBound.xMin, root.worldBound.xMin);
            Assert.LessOrEqual(element.worldBound.xMax, root.worldBound.xMax + 1);
            Assert.LessOrEqual(element.worldBound.yMax, root.worldBound.yMax + 1);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 12; frame++)
                yield return null;
        }

        private sealed class FailedSave : HttpMessageHandler
        {
            private readonly string code;
            public int Requests { get; private set; }
            public FailedSave(string code) => this.code = code;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                Requests++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"error\":\"" + code + "\"}")
                });
            }
        }
    }
}
