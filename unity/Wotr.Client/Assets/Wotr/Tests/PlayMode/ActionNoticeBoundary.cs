using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wotr.PlayTests
{
    // Faults/delays only around real HTTP. Never supplies fabricated state or history.
    internal sealed class ActionNoticeBoundary : DelegatingHandler
    {
        public bool HoldAction;
        public bool FailBattle;
        public bool HoldBattle;
        public bool BattleHeld { get; private set; }
        public bool BattleDelivered { get; private set; }
        public bool ActionHeld { get; private set; }
        private readonly TaskCompletionSource<bool> action = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> battle = new TaskCompletionSource<bool>();
        public ActionNoticeBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
        public void Release()
        {
            action.TrySetResult(true);
            battle.TrySetResult(true);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri.AbsolutePath;
            if (request.Method == HttpMethod.Get && HoldAction && path.Contains("/actions/"))
            {
                HoldAction = false;
                ActionHeld = true;
                await action.Task;
            }
            if (FailBattle && path.Contains("/battles/"))
                throw new HttpRequestException("Injected read connection loss.");
            var response = await base.SendAsync(request, token);
            if (HoldBattle && request.Method == HttpMethod.Get && path.Contains("/battles/"))
            {
                HoldBattle = false;
                BattleHeld = true;
                await battle.Task;
                BattleDelivered = true;
            }
            return response;
        }
        protected override void Dispose(bool disposing) { Release(); base.Dispose(disposing); }
    }
}
