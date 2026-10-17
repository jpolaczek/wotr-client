using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wotr.PlayTests
{
    // Only interrupts real detail reads. Never supplies history or gameplay responses.
    internal sealed class HuntActivityBoundary : DelegatingHandler
    {
        public bool Fail;
        public bool Hold;
        public bool Held { get; private set; }
        private readonly TaskCompletionSource<bool> gate = new TaskCompletionSource<bool>();

        public HuntActivityBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
        public void Release() => gate.TrySetResult(true);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.Contains("/actions/"))
            {
                if (Fail)
                    throw new HttpRequestException("Injected Hunt detail connection loss.");
                if (Hold)
                {
                    Hold = false;
                    Held = true;
                    await gate.Task;
                }
            }
            return await base.SendAsync(request, token);
        }

        protected override void Dispose(bool disposing)
        {
            Release();
            base.Dispose(disposing);
        }
    }
}
