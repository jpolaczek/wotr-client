using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // AUTH-06: the game checks a new account name before sending it and words the server's refusals; the menu's
    // masthead offers the change and shows the new name.
    public sealed class AccountNameTests
    {
        private const string Password = "a long enough password";

        [Test]
        public void TheGameChecksTheNameAndThePasswordBeforeSending()
        {
            Assert.AreEqual("", AccountNameRules.Problem("kuba", "vibestudios", Password));
            Assert.AreEqual("", AccountNameRules.Problem("kuba", "  vibe.studios-1  ", Password));
            foreach (var bad in new[] { "ab", "has space", "x@example.com", new string('a', 33), "zażółć", "" })
                Assert.AreEqual(AccountNameRules.NameRule, AccountNameRules.Problem("kuba", bad, Password), bad);
            StringAssert.Contains("already", AccountNameRules.Problem("kuba", "kuba", Password));
            StringAssert.Contains("password", AccountNameRules.Problem("kuba", "vibestudios", "short"));
        }

        [Test]
        public void TheServersRefusalsReadAsSentences()
        {
            StringAssert.Contains("taken", AccountNameRules.Refusal("NameTaken"));
            StringAssert.Contains("password", AccountNameRules.Refusal("WrongPassword"));
            Assert.AreEqual(AccountNameRules.NameRule, AccountNameRules.Refusal("InvalidName"));
            StringAssert.Contains("Wait", AccountNameRules.Refusal("TooManyRequests"));
            StringAssert.Contains("Try again", AccountNameRules.Refusal("Unexpected"));
        }

        [Test]
        public void TheDialogSendsOnlyAValidNameAndKeepsARefusal()
        {
            var dialog = new AccountNameDialog();
            string sent = null;
            dialog.Submitted += (name, _) => sent = name;
            dialog.Open("kuba");
            Assert.IsTrue(dialog.IsOpen);
            dialog.SetValues("x", Password);
            dialog.Submit();
            Assert.IsNull(sent, "An invalid name is not sent.");
            Assert.AreEqual(AccountNameRules.NameRule, dialog.Status.text);
            dialog.SetValues(" vibestudios ", Password);
            dialog.Submit();
            Assert.AreEqual("vibestudios", sent);
            dialog.Refused("NameTaken");
            StringAssert.Contains("taken", dialog.Status.text);
            Assert.IsTrue(dialog.IsOpen, "A refusal stays in the window.");
            dialog.Close();
            Assert.IsFalse(dialog.IsOpen);
        }

        [Test]
        public void TheMastheadOffersTheChangeAndShowsTheNewName()
        {
            var menu = new MainMenuScreen("kuba");
            Assert.AreEqual("Signed in as kuba", menu.Root.Q<Label>("menu-player").text);
            Assert.IsNotNull(menu.Root.Q<Button>("change-account-name"));
            Assert.IsNotNull(menu.Root.Q("account-name-dialog"));
            menu.ShowUsername("vibestudios");
            Assert.AreEqual("Signed in as vibestudios", menu.Root.Q<Label>("menu-player").text);
        }
    }
}
