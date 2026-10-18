using System;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // SEC-02: a locked account name reads as a wait in whole minutes, at sign-in and in the account name dialog.
    public sealed class SignInLockTests
    {
        [Test]
        public void TheLockNamesTheWaitInWholeMinutes()
        {
            Assert.AreEqual("Too many failed sign-ins for this name. Try again in 15 minutes.",
                SignInLock.Text(TimeSpan.FromSeconds(900)));
            StringAssert.EndsWith("in 15 minutes.", SignInLock.Text(TimeSpan.FromSeconds(841)));
            StringAssert.EndsWith("in 1 minute.", SignInLock.Text(TimeSpan.FromSeconds(20)));
            StringAssert.EndsWith("in 15 minutes.", SignInLock.Text(null));
        }

        [Test]
        public void TheAccountNameDialogUsesTheSameWords()
        {
            Assert.AreEqual(SignInLock.Text(TimeSpan.FromMinutes(3)),
                AccountNameRules.Refusal(SignInLock.Error, TimeSpan.FromMinutes(3)));
            StringAssert.Contains("Wait", AccountNameRules.Refusal("TooManyRequests"));
        }
    }
}
