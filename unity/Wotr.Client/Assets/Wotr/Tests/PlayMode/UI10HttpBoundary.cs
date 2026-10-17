using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // Delays or drops responses from the real server; never creates game data.
    internal sealed class UI10HttpBoundary : DelegatingHandler
    {
        public string MatchPath;
        public int Commands { get; private set; }
        public int ReceiptLookups { get; private set; }
        public GameCommand LastCommand { get; private set; }
        public bool HoldNextCommand;
        public bool HoldNextPreview;
        public bool HoldNextView;
        public bool CommandHeld { get; private set; }
        public bool PreviewHeld { get; private set; }
        public bool ViewHeld { get; private set; }
        private TaskCompletionSource<bool> commandGate = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> previewGate = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> viewGate = new TaskCompletionSource<bool>();

        public UI10HttpBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }

        public void ArmCommandLoss()
        {
            if (CommandHeld && !commandGate.Task.IsCompleted)
                throw new InvalidOperationException("Release the previous held command before arming another response loss.");
            commandGate = new TaskCompletionSource<bool>();
            CommandHeld = false;
            HoldNextCommand = true;
        }

        public void ReleaseCommand() => commandGate.TrySetResult(true);
        public void ReleasePreview() => previewGate.TrySetResult(true);
        public void ReleaseView() => viewGate.TrySetResult(true);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri.AbsolutePath;
            var command = path == MatchPath + "/commands";
            if (request.Method == HttpMethod.Get && path.StartsWith(MatchPath + "/receipts/", StringComparison.Ordinal))
                ReceiptLookups++;
            var holdCommand = command && Take(ref HoldNextCommand);
            var holdPreview = path == MatchPath + "/preview" && Take(ref HoldNextPreview);
            var holdView = request.Method == HttpMethod.Get && path == MatchPath && Take(ref HoldNextView);
            if (command)
            {
                Commands++;
                LastCommand = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
            }
            var response = await base.SendAsync(request, token);
            if (holdCommand)
            {
                CommandHeld = true;
                await commandGate.Task;
                response.Dispose();
                throw new HttpRequestException("Injected response loss after the production server committed.");
            }
            if (holdPreview)
            {
                PreviewHeld = true;
                await previewGate.Task;
            }
            if (holdView)
            {
                ViewHeld = true;
                await viewGate.Task;
            }
            return response;
        }

        private static bool Take(ref bool value)
        {
            var result = value;
            value = false;
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseCommand();
            ReleasePreview();
            ReleaseView();
            base.Dispose(disposing);
        }
    }
}
