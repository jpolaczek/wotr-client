using System.Threading;
using System.Threading.Tasks;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // AUTH-03: players make accounts on the game's site; tests register through the API with an address on the
    // reserved .test domain, which never receives mail.
    internal static class TestAccounts
    {
        public const string Password = "Test account password";

        public static Task Register(GameApi api, string username, CancellationToken token) =>
            api.Register(username, username + "@example.test", Password, token);
    }
}
