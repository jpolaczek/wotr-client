using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUTH-06: the checks the game makes before asking the server to change an account name, and the words for the
    // server's answers. The server checks again; uniqueness and the password only it can tell.
    public static class AccountNameRules
    {
        public const string NameRule = "Use 3–32 letters a–z, digits or . _ - (not an email address).";

        // The first problem with the new name and the password, or "" when both may be sent.
        public static string Problem(string currentName, string newName, string password)
        {
            var name = newName?.Trim() ?? "";
            if (!AccountInput.ValidUsername(name) || AccountInput.ValidEmail(name))
                return NameRule;
            if (name == (currentName ?? ""))
                return "That is already your account name.";
            if (!AccountInput.ValidPassword(password))
                return "Enter your current password (12–128 characters).";
            return "";
        }

        public static string Refusal(string error, TimeSpan? retryAfter = null) => error switch
        {
            SignInLock.Error => SignInLock.Text(retryAfter),
            "NameTaken" => "That name is taken. Choose another.",
            "WrongPassword" => "The password is not right.",
            "InvalidName" => NameRule,
            "TooManyRequests" => "Too many attempts. Wait a minute and try again.",
            _ => "The name could not be changed. Try again."
        };
    }
}
