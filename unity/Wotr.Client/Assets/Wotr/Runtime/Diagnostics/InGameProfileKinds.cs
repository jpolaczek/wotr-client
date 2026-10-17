#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal static class InGameProfileKinds
    {
        public static readonly string[] Legacy = { "map-idle", "map-zoom", "hover", "army", "battle", "card", "tooltip",
            "hunt-allocation", "hunt-reroll", "hunt-response", "hunt-report", "mordor-refresh", "activity-scroll", "activity-card" };
        public static readonly string[] InGame = { "effects", "resources-fp", "resources-sh", "sovereigns", "victory",
            "dense-fp", "dense-sh" };

        public static string[] For(string suite) => suite == "legacy" ? Legacy : suite == "in-game" ? InGame
            : suite == "board" ? BoardProfileScene.Kinds
            : suite == "figure-motion" ? FigureMotionProfileScene.Kinds
            : throw new ArgumentException("Unknown presentation profile suite: " + suite);

        public static bool Contains(string kind) => Array.IndexOf(InGame, kind) >= 0;

        public static PlayerView View(Dictionary<string, PlayerView> views, string suite, string kind)
        {
            if (views.TryGetValue(kind, out var view))
                return view;
            if (suite == "legacy" && views.TryGetValue("map", out view))
                return view;
            throw new InvalidOperationException("Missing projected profile workload: " + kind);
        }
    }
}
#endif
