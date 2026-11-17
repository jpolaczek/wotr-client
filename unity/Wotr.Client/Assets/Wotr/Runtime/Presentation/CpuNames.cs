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
        // Owner, 2026-11-17: AlphaWOTR is annotated as created by Wey (its author's name in the game). The MIT licence's
        // copyright line stays in the credit and in THIRD_PARTY_NOTICES.md.
        public const string CreatedBy = "Created by Wey";
        public const string Credit = "AlphaWOTR, created by Wey · MIT licence, Copyright (c) 2026 Sarvesh Sadana · github.com/Weyoun5/AlphaWOTR";
        public const string BaseGameOnly = "Games against the CPU use the base game.";
        public const string Unavailable = "The CPU is unavailable. The game will continue when it is back.";

        public static bool IsAlphaWotr(BotSeatView bot) => bot != null && bot.Engine == AlphaWotrEngine;

        // Owner, 2026-11-17: the CPU joins only a Play vs CPU room, the private room Play vs CPU makes under this name;
        // the server knows it the same way (RoomRules.PlayVsCpu in wotr-server) and refuses the CPU elsewhere.
        public const string PlayVsCpuName = "Play vs CPU";
        public const string OnlyInPlayVsCpu = "The CPU plays only in games started with Play vs CPU.";

        public static CreateRoomRequest PlayVsCpuRequest(Side side) =>
            new CreateRoomRequest { Name = PlayVsCpuName, Side = side, Visibility = RoomVisibility.Private };

        public static bool PlayVsCpuRoom(RoomView room) =>
            room != null && room.Managed && room.Visibility == RoomVisibility.Private && room.Name == PlayVsCpuName;

        public static string Name(BotSeatView bot) => IsAlphaWotr(bot) ? AlphaWotr : Plain;

        // The seat's tooltip: the credit for AlphaWOTR, nothing for the scripted CPU.
        public static string Tooltip(BotSeatView bot) => IsAlphaWotr(bot) ? Credit : "";

        // The seat's line under the name: who made AlphaWOTR; nothing for the scripted CPU.
        public static string Annotation(BotSeatView bot) => IsAlphaWotr(bot) ? CreatedBy : "";

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
