using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: the server routes of the test mode (TEST-01). Without the mode the server answers 404.
    public static class TestScenarioApi
    {
        public static async Task<TestScenarioCatalogView> Catalog(GameApi api, CancellationToken cancellation)
        {
            try
            {
                return await api.Get<TestScenarioCatalogView>("api/test-scenarios", cancellation);
            }
            catch (ApiException error) when (error.Status == 404)
            {
                return null;
            }
        }

        public static Task<TestSessionView> Start(GameApi api, string scenario, CancellationToken cancellation) =>
            api.Post<TestSessionView>("api/test-scenarios/" + Uri.EscapeDataString(scenario) + "/start", null, cancellation);

        public static Task End(GameApi api, CancellationToken cancellation) => api.Delete("api/test-scenarios/session", cancellation);
    }
}
