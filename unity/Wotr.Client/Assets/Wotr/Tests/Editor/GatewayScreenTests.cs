using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Wotr.Client.Tests
{
    public class GatewayScreenTests
    {
        // AUTH-03/AUTH-05: one field for the account name, the password, Login and the two account pages; no invitation.
        [Test]
        public void EntryHasTheTwoCredentialsLoginAndTheTwoAccountPages()
        {
            var screen = new GatewayScreen(() => "https://wotr.example.test", _ => { });
            CollectionAssert.AreEqual(new[] { "auth-username", "auth-password" },
                screen.Root.Query<TextField>().ToList().Select(field => field.name));
            Assert.AreEqual("Account name", screen.Root.Q<TextField>("auth-username").label);
            Assert.IsTrue(screen.Root.Q<TextField>("auth-password").isPasswordField);
            Assert.IsNull(screen.Root.Q<TextField>("auth-invitation"));
            Assert.IsNull(screen.Root.Q<Toggle>("auth-register"));
            Assert.IsNull(screen.Root.Q<TextField>("auth-server"));
            Assert.AreEqual("Login", screen.Root.Q<Button>("auth-submit").text);
            Assert.AreEqual("Create account", screen.Root.Q<Button>("auth-create").text);
            Assert.AreEqual("Forgot password?", screen.Root.Q<Button>("auth-forgot").text);
            CollectionAssert.AreEqual(new[] { "auth-submit", "auth-create", "auth-forgot", "open-interface" },
                screen.Root.Q("auth-actions").Children().Select(child => child.name), "The four stand one under another; Settings last.");
            StringAssert.Contains("browser", screen.Root.Q<Label>("auth-browser-hint").text);
        }

        // AUTH-03: the account buttons open the configured server's account pages and send nothing to the server.
        [TestCase("https://wotr.example.test", "https://wotr.example.test/account/")]
        [TestCase(" https://localhost:7443/ ", "https://localhost:7443/account/")]
        public void AccountButtonsOpenTheServersAccountPages(string server, string pages)
        {
            var opened = new List<string>();
            var screen = new GatewayScreen(() => server, opened.Add);
            var submitted = 0;
            screen.Submitted += (_, _) => submitted++;
            screen.OpenAccountPage(GatewayScreen.RegisterPage);
            screen.OpenAccountPage(GatewayScreen.ForgotPage);
            CollectionAssert.AreEqual(new[] { pages + "register", pages + "forgot" }, opened);
            Assert.Zero(submitted);
        }

        [TestCase("Player")]
        [TestCase("player.one_2")]
        public void LoginAcceptsAnAccountNameAndKeepsPasswordSpaces(string login)
        {
            var screen = new GatewayScreen(() => "https://wotr.example.test", _ => { });
            var count = 0;
            screen.Submitted += (name, password) =>
            {
                count++;
                Assert.AreEqual(login, name);
                Assert.AreEqual("  long account password  ", password);
                screen.Busy(true);
            };
            screen.Root.Q<TextField>("auth-username").value = "bad name";
            screen.Submit();
            Assert.Zero(count);
            StringAssert.StartsWith("Account name:", screen.Status.text);
            // AUTH-05: an email address is refused with a hint before anything is sent.
            screen.Root.Q<TextField>("auth-username").value = "player@example.test";
            screen.Root.Q<TextField>("auth-password").value = "  long account password  ";
            screen.Submit();
            Assert.Zero(count);
            StringAssert.Contains("not the email address", screen.Status.text);
            screen.Root.Q<TextField>("auth-username").value = " " + login + " ";
            screen.Root.Q<TextField>("auth-password").value = "short";
            screen.Submit();
            Assert.Zero(count);
            StringAssert.StartsWith("Password:", screen.Status.text);
            screen.Root.Q<TextField>("auth-password").value = "  long account password  ";
            screen.Submit();
            screen.Submit();
            Assert.AreEqual(1, count);
            Assert.IsFalse(screen.Root.Q<Button>("auth-submit").enabledSelf);
            Assert.IsTrue(screen.Root.Q<Button>("auth-create").enabledSelf, "The account pages stay at hand while signing in.");
            screen.ClearPassword();
            Assert.IsEmpty(screen.Root.Q<TextField>("auth-password").value);
        }

        [Test]
        public void AFailedLoginPointsToThePasswordReset()
        {
            var screen = new GatewayScreen(() => "https://wotr.example.test", _ => { });
            screen.ShowAuthenticationFailure();
            StringAssert.StartsWith("Account name or password not accepted", screen.Status.text);
            StringAssert.Contains("reset your password", screen.Status.text);
        }
    }
}
