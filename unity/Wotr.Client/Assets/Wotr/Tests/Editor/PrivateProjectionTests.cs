using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class PrivateProjectionTests
    {
        [Test]
        public async Task LateGetCannotRestoreAnOlderPreferenceAtTheSameGameRevision()
        {
            var transport = new ReorderedViews();
            using (var api = new GameApi("http://localhost", transport))
            using (var connection = new MatchConnection(api, "match"))
            {
                var changes = new List<PlayerView>();
                connection.Updated += changes.Add;
                var older = connection.Refresh();
                var latest = connection.Refresh();
                transport.Complete(1, 20, 4);
                await latest;
                transport.Complete(0, 20, 3);
                await older;
                Assert.AreEqual(1, changes.Count);
                Assert.AreEqual(4, changes[0].AutoDecision.Version);
                var next = connection.Refresh();
                transport.Complete(2, 21, 4);
                await next;
                Assert.AreEqual(2, changes.Count);
                Assert.AreEqual(21, connection.Revision);
            }
        }

        private sealed class ReorderedViews : HttpMessageHandler
        {
            private readonly List<TaskCompletionSource<HttpResponseMessage>> requests = new List<TaskCompletionSource<HttpResponseMessage>>();
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var completion = new TaskCompletionSource<HttpResponseMessage>();
                requests.Add(completion);
                return completion.Task;
            }

            public void Complete(int index, long revision, long privateVersion)
            {
                var view = new PlayerView { MatchId = "match", Revision = revision, AutoDecision = new AutoDecisionView { Version = privateVersion } };
                requests[index].SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(view)) });
            }
        }
    }
}
