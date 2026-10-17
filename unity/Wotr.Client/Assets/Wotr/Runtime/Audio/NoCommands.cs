using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-06: the command events of a board that sends none — an observer's or a replay's. Its sounds come only from
    // accepted facts.
    public sealed class NoCommands : ICommandEvents
    {
        public static readonly NoCommands Instance = new NoCommands();

        private NoCommands() { }

        public event Action CommandSending { add { } remove { } }
        public event Action<GameCommand, CommandReceipt, bool> CommandAnswered { add { } remove { } }
        public bool GestureSuppressed => false;
    }
}
