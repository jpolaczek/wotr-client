using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ApiException : Exception
    {
        public int Status
        {
            get;
        }
        public TimeSpan? RetryAfter { get; }
        public ApiException(int status, string message, TimeSpan? retryAfter = null) : base(message)
        {
            Status = status;
            RetryAfter = retryAfter;
        }
    }

    public sealed class GameApi : IDisposable
    {
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };
        private readonly HttpClient client;
        public Uri Address
        {
            get;
        }
        public string Token { get; private set; } = "";
        public string AccountId { get; private set; } = "";
        public string Username { get; private set; } = "";
        public ClientBuildInfo Identity { get; }

        // DIST-07: a 409 refusal of this version (ClientUpdateRequired / ServerUpdateRequired) from any request, on
        // any screen; the updater listens, while each screen keeps its own message.
        public static event Action<string> VersionRefused;

        public GameApi(string address, HttpMessageHandler handler = null, ClientBuildInfo identity = null)
        {
            if (!Uri.TryCreate(address.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new ArgumentException("Use an HTTPS server address, or HTTP for a local server (localhost, 127.0.0.1 or [::1]).");
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("Enter a server address without credentials or query parameters.");
            Address = uri;
            // No redirects: the session token is never replayed to another host.
            client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
            {
                BaseAddress = Address,
                Timeout = TimeSpan.FromSeconds(15)
            };
            Identity = identity ?? ClientIdentity.Current;
            ClientIdentity.Declare(client.DefaultRequestHeaders, Identity);
        }

        public async Task Login(string username, string password, CancellationToken cancellation) =>
            UseSession(await Send<SessionView>("POST", "api/auth/login",
                new LoginRequest { Username = username, Password = password }, cancellation));

        // AUTH-03: players make accounts on the game's site; tests and tools register through the API with an address.
        public async Task Register(string username, string email, string password, CancellationToken cancellation) =>
            UseSession(await Send<SessionView>("POST", "api/auth/register",
                new RegisterRequest { Username = username, Email = email, Password = password }, cancellation));

        private void UseSession(SessionView session)
        {
            Token = session.Token;
            AccountId = session.AccountId;
            Username = session.Username;
        }

        public async Task Logout(CancellationToken cancellation)
        {
            try
            {
                await Send<object>("POST", "api/auth/logout", null, cancellation);
            }
            finally { Token = AccountId = Username = ""; }
        }

        public Task<T> Get<T>(string path, CancellationToken cancellation) => Send<T>("GET", path, null, cancellation);
        public Task<T> Post<T>(string path, object body, CancellationToken cancellation) => Send<T>("POST", path, body, cancellation);
        public Task<T> Put<T>(string path, object body, CancellationToken cancellation) => Send<T>("PUT", path, body, cancellation);
        public Task Delete(string path, CancellationToken cancellation) => Send<object>("DELETE", path, null, cancellation);

        // REPLAY-04: a file the server streams (a replay), copied into `target` as it arrives; errors as in Send.
        public async Task Download(string path, Stream target, CancellationToken cancellation)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, path))
            {
                if (Token.Length > 0)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new ApiException((int)response.StatusCode, ErrorCode(response.StatusCode, text), RetryDelay(response));
                    }
                    using (var body = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        await body.CopyToAsync(target, 81920, cancellation).ConfigureAwait(false);
                }
            }
        }

        // TEST-03: a second connection to the same server with a session the server issued, e.g. a test seat.
        public GameApi WithSession(SessionView session)
        {
            var api = new GameApi(Address.AbsoluteUri, null, Identity);
            api.UseSession(session);
            return api;
        }

        private async Task<T> Send<T>(string method, string path, object body, CancellationToken cancellation)
        {
            using (var request = new HttpRequestMessage(new HttpMethod(method), path))
            {
                if (Token.Length > 0)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                if (body != null)
                    request.Content = new StringContent(JsonConvert.SerializeObject(body, Json), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request, cancellation))
                {
                    var text = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        var code = ErrorCode(response.StatusCode, text);
                        if (response.StatusCode == HttpStatusCode.Conflict &&
                            (code == ClientCompatibility.ClientUpdateRequired || code == ClientCompatibility.ServerUpdateRequired))
                            VersionRefused?.Invoke(code);
                        throw new ApiException((int)response.StatusCode, code, RetryDelay(response));
                    }
                    return string.IsNullOrEmpty(text) ? default : JsonConvert.DeserializeObject<T>(text, Json);
                }
            }
        }

        private static string ErrorCode(HttpStatusCode status, string text)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<ApiError>(text);
                if (!string.IsNullOrEmpty(error?.Error))
                    return error.Error;
            }
            catch (JsonException) { }
            return status == HttpStatusCode.Unauthorized ? "SessionExpired"
                : (int)status == 429 ? "TooManyRequests" : "ServerUnavailable";
        }

        private static TimeSpan? RetryDelay(HttpResponseMessage response)
        {
            var retry = response.Headers.RetryAfter;
            if (retry?.Delta is TimeSpan delay)
                return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
            if (retry?.Date is DateTimeOffset date)
                return date > DateTimeOffset.UtcNow ? date - DateTimeOffset.UtcNow : TimeSpan.Zero;
            return null;
        }

        public void Dispose() => client.Dispose();
    }
}
