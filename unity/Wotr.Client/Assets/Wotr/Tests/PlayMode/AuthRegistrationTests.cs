using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    public class AuthRegistrationTests
    {
        // AUTH-03: an account made on the site (here through the API, with an address) signs in on the gateway by its
        // email in any letter case and by its name; a wrong password asks again with the password focused.
        [UnityTest]
        public IEnumerator AnAccountSignsInByEmailAndByName()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real accounts.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            // MENU-20: the test's own server instead of the build's configuration.
            ServerConfiguration.Use(fixture.Address);
            try
            {
                var task = Run(fixture);
                while (!task.IsCompleted)
                    yield return null;
                task.GetAwaiter().GetResult();
            }
            finally
            {
                ServerConfiguration.Use(null);
            }
        }

        private static async Task Run(Fixture fixture)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var token = timeout.Token;
            var username = "auth_" + Guid.NewGuid().ToString("N").Substring(0, 20);
            using (var api = new GameApi(fixture.Address))
                await TestAccounts.Register(api, username, token);
            using var client = new MountedFlowClient(null, null);
            var root = client.Root;
            await ReturnTestFixture.Until(() => root.Q<TextField>("auth-username") != null, token);
            Assert.IsNull(root.Q<TextField>("auth-invitation"));
            Fill(root, username.ToUpperInvariant() + "@EXAMPLE.test", "Incorrect password");
            await ReturnTestFixture.ClickReady(root, "auth-submit", token);
            await ReturnTestFixture.Until(() => Status(root).StartsWith("Username, email or password not accepted"), token);
            var failedPassword = root.Q<TextField>("auth-password");
            await ReturnTestFixture.Until(() => failedPassword.panel.focusController.focusedElement == failedPassword ||
                failedPassword.Contains(failedPassword.panel.focusController.focusedElement as VisualElement), token);
            failedPassword.value = TestAccounts.Password;
            await ReturnTestFixture.ClickReady(root, "auth-submit", token);
            await ReturnTestFixture.Until(() => SignOut(root) != null, token);
            Assert.IsEmpty(failedPassword.value, "Signing in clears the old password control.");
            await SignOutAndWait(root, token);
            Fill(root, username, TestAccounts.Password);
            await ReturnTestFixture.ClickReady(root, "auth-submit", token);
            await ReturnTestFixture.Until(() => SignOut(root) != null, token);
            await SignOutAndWait(root, token);
        }

        private static async Task SignOutAndWait(VisualElement root, CancellationToken token)
        {
            await Task.Delay(100, token);
            CardNetworkTests.Click(SignOut(root));
            await ReturnTestFixture.Until(() => root.Q<TextField>("auth-username") != null, token);
        }

        private static void Fill(VisualElement root, string login, string password)
        {
            root.Q<TextField>("auth-username").value = login;
            root.Q<TextField>("auth-password").value = password;
        }

        private static string Status(VisualElement root) => root.Q<Label>(className: "status-message")?.text ?? "";

        private static Button SignOut(VisualElement root) =>
            root.Query<Button>().ToList().FirstOrDefault(button => button.text == "Sign out");

        private sealed class Fixture
        {
            public string Address;
        }
    }
}
