using System;

namespace Wotr.Client
{
    // REPLAY-03: a file that is not a usable replay; the message is shown to the player as it is.
    public sealed class ReplayFormatException : Exception
    {
        public ReplayFormatException(string message) : base(message) { }
    }
}
