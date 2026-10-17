using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUDIO-04/AUDIO-05: what the sounds of a game need to know about its own commands (TurnInteraction).
    public interface ICommandEvents
    {
        // A command leaves in this frame.
        event Action CommandSending;
        // The answer to an own command; true when it was the recovered receipt of the pending command.
        event Action<GameCommand, CommandReceipt, bool> CommandAnswered;
        // The last press was ignored as the quick second click after a step (UI-142).
        bool GestureSuppressed { get; }
    }
}
