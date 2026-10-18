using System;

namespace Wotr.Client
{
    // SEC-02: after a series of failed sign-ins the server locks the account name for a while, from every address
    // (429 SignInLocked with Retry-After). The text names the wait in whole minutes.
    public static class SignInLock
    {
        public const string Error = "SignInLocked";
        private const int UsualMinutes = 15;

        public static string Text(TimeSpan? retryAfter)
        {
            var minutes = retryAfter.HasValue ? Math.Max(1, (int)Math.Ceiling(retryAfter.Value.TotalMinutes)) : UsualMinutes;
            return $"Too many failed sign-ins for this name. Try again in {minutes} {(minutes == 1 ? "minute" : "minutes")}.";
        }
    }
}
