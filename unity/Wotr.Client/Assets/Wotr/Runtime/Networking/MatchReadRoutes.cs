using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // OBS-03: where a board reads its match — a player's own routes, or an observer's routes with the side being
    // viewed. The view, history, action details and battle reports ask the same routes, so one perspective holds for
    // every read and a change of perspective changes them all.
    public sealed class MatchReadRoutes
    {
        private readonly string root;
        private readonly Func<Side?> perspective;
        public string Match { get; }

        private MatchReadRoutes(string match, string root, Func<Side?> perspective)
        {
            Match = match;
            this.root = root;
            this.perspective = perspective;
        }

        public static MatchReadRoutes Player(string match) =>
            new MatchReadRoutes(match, "api/matches/" + Uri.EscapeDataString(match), () => null);

        public static MatchReadRoutes Observer(string match, Func<Side> perspective) =>
            new MatchReadRoutes(match, "api/observe/" + Uri.EscapeDataString(match), () => perspective());

        public string View => Path("", "");
        public string Updates => root + "/updates";

        // A route under the match with its query; an observer's routes add the perspective.
        public string Path(string path, string query)
        {
            var parts = new List<string>();
            if (query.Length > 0)
                parts.Add(query);
            if (perspective() is Side side)
                parts.Add("perspective=" + side);
            return root + path + (parts.Count == 0 ? "" : "?" + string.Join("&", parts));
        }
    }
}
