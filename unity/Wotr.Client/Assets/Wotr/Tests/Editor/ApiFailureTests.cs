using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Wotr.Client.Tests
{
    public class ApiFailureTests
    {
        [TestCase("http://127.0.0.1:5088")]
        [TestCase("http://localhost:5088")]
        [TestCase("http://[::1]:5088")]
        [TestCase("https://example.com")]
        public void LocalHttpAndRemoteHttpsCanBeUsedForServerSettings(string address)
        {
            using var api = new GameApi(address);
            Assert.AreEqual(new Uri(address.TrimEnd('/') + "/"), api.Address);
        }

        [TestCase("http://example.com")]
        [TestCase("http://192.168.1.2:5088")]
        [TestCase("http://localhost.example.com")]
        [TestCase("ftp://localhost")]
        [TestCase("relative-address")]
        [TestCase("http://user:password@localhost:5088")]
        [TestCase("http://localhost:5088?x=y")]
        [TestCase("http://localhost:5088#fragment")]
        public void InvalidServerSettingsAreRejected(string address)
        {
            Assert.Throws<ArgumentException>(() => new GameApi(address));
        }

        [TestCase("")]
        [TestCase("{\"error\":\"TooManyRequests\"}")]
        public void AuthenticationThrottlePreservesDelayAndDoesNotResendCredentials(string body)
        {
            using var handler = new FailureHandler(HttpStatusCode.TooManyRequests, body);
            using var api = new GameApi("https://localhost", handler);
            var error = Assert.ThrowsAsync<ApiException>(async () =>
                await api.Login("user", "private-password", CancellationToken.None));
            Assert.AreEqual(429, error.Status);
            Assert.AreEqual("TooManyRequests", error.Message);
            Assert.AreEqual(TimeSpan.FromSeconds(23), error.RetryAfter);
            Assert.AreEqual(1, handler.Requests);
            Assert.IsEmpty(api.Token);
        }

        [TestCase("CorruptSave")]
        [TestCase("UnsupportedSaveVersion")]
        public void SaveFailureKeepsItsSafeCodeForTheClient(string code)
        {
            using var handler = new FailureHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":\"" + code + "\"}");
            using var api = new GameApi("https://localhost", handler);
            var error = Assert.ThrowsAsync<ApiException>(async () =>
                await api.Get<object>("api/matches/test", CancellationToken.None));
            Assert.AreEqual(code, error.Message);
            Assert.AreEqual(1, handler.Requests);
        }

        [TestCase("CorruptSave")]
        [TestCase("UnsupportedSaveVersion")]
        public async Task PermanentSaveFailureStopsAutomaticRetryAndAllowsAnExplicitCheck(string code)
        {
            using var handler = new FailureHandler(HttpStatusCode.ServiceUnavailable, "{\"error\":\"" + code + "\"}");
            using var api = new GameApi("https://localhost", handler);
            using var connection = new MatchConnection(api, "test");
            var failures = 0;
            connection.StatusChanged += status => { if (status == code) failures++; };
            connection.Start();
            await Task.Delay(50);
            Assert.AreEqual(1, failures);
            Assert.AreEqual(1, handler.Requests);
            connection.Reconnect();
            await Task.Delay(50);
            Assert.AreEqual(2, failures);
            Assert.AreEqual(2, handler.Requests);
        }

        private sealed class FailureHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode status;
            private readonly string body;
            public int Requests { get; private set; }

            public FailureHandler(HttpStatusCode status, string body)
            {
                this.status = status;
                this.body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Requests++;
                var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(23));
                return Task.FromResult(response);
            }
        }
    }
}
