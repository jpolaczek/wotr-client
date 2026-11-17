using System;
using System.Globalization;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-540 (owner, 2026-11-16, BOT-219 D1/D2/D6): the CPU of new base-game CPU games is AlphaWOTR, named and credited
    // (its author, the MIT licence); a game begun with the scripted CPU keeps its plain name. Owner, 2026-11-17: when
    // AlphaWOTR cannot answer, the game waits (nothing else plays in its place) and the player is told so.
    public static class CpuNames
    {
        public const string AlphaWotrEngine = "alphawotr";
        public const string AlphaWotr = "AlphaWOTR";
        public const string Plain = "CPU";
        public const string Credit = "AlphaWOTR by Sarvesh Sadana · MIT licence · github.com/Weyoun5/AlphaWOTR";
        public const string BaseGameOnly = "Games against the CPU use the base game.";
        public const string Unavailable = "The CPU is unavailable. The game will continue when it is back.";

        public static bool IsAlphaWotr(BotSeatView bot) => bot != null && bot.Engine == AlphaWotrEngine;

        public static string Name(BotSeatView bot) => IsAlphaWotr(bot) ? AlphaWotr : Plain;

        // The seat's tooltip: the credit for AlphaWOTR, nothing for the scripted CPU.
        public static string Tooltip(BotSeatView bot) => IsAlphaWotr(bot) ? Credit : "";

        public static bool Waiting(CpuSeatState state) => state != null && state.Waiting;

        // What the waiting means, in the player's words (the kind of failure only, as the server tells it).
        public static string WaitingDetail(CpuSeatState state)
        {
            var since = state?.Since is DateTimeOffset at
                ? " since " + at.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture) : "";
            var reason = state?.Problem switch
            {
                "Timeout" => "took too long to answer",
                "Error" => "could not answer this decision",
                _ => "cannot be reached"
            };
            return "AlphaWOTR " + reason + since + ". No other CPU plays in its place: the game goes on as soon as it answers again.";
        }
    }
}
