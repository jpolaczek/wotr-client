using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // A signed-in player can open multiple matches without repeating authentication.
    // Keep the production authentication rate limiter enabled in integration runs.
    [SetUpFixture]
    public sealed class BattleTestSessions
    {
        private static readonly Dictionary<string, GameApi> sessions = new Dictionary<string, GameApi>();

        public static async Task<GameApi> Login(string address, string user, string password, CancellationToken token)
        {
            var key = address + "/" + user;
            if (sessions.TryGetValue(key, out var existing))
                return existing;
            var api = new GameApi(address);
            try
            {
                await api.Login(user, password, token);
                sessions.Add(key, api);
                return api;
            }
            catch { api.Dispose(); throw; }
        }

        [OneTimeTearDown]
        public void Close()
        {
            foreach (var api in sessions.Values)
                api.Dispose();
            sessions.Clear();
        }
    }
}
