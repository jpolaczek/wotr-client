using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One tile beside a king during a corruption attempt or an inspection: its face (or back) and its click.
    public sealed class SovereignTile
    {
        public string Id = "";
        public int? Value;
        public bool Kings;
        public bool Back;
        public bool Selectable;
        public string Tooltip = "";
    }

    // UI-274 (UX_SPEC): the Shadow's corruption attempt is sent from the button beside an Initial king's portrait.
    // UI-398 (owner): it starts from its Eye, as every action starts from its die (UI-389): the Eyes that may pay stand in
    // the Shadow's dice row (UI-230), and only the one clicked lights the kings and their attempt buttons; before that the
    // buttons stay dark and nothing glows for them. The tiles drawn for the attempt stand beside the king: the Shadow sees their values, the Free
    // Peoples their backs; a click on one attaches it. UI-442 (owner): a standard attempt draws all its tiles with the
    // attempt's click, and the server places a single tile itself; only Sequential corruption (or a game saved before
    // UI-442) draws from the console, a click per draw (UI-279), lit while it is the only step (UI-423).
    public static class SovereignAttempt
    {
        public const string ActionId = "attempt-corruption";

        public static bool Shown(PlayerView view, SovereignView king) => view.You == Side.Shadow && king.Status == SovereignStatus.Initial;

        public static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.AttemptCorruption) : null;

        // The Eye that pays: the one the player clicked; "" before an Eye is chosen.
        public static string Eye(PlayerView view, TurnDraft draft)
        {
            var option = Option(view);
            return option != null && draft?.Payload.DieId is { Length: > 0 } chosen && option.EligibleDice.Contains(chosen) ? chosen : "";
        }

        public static bool Enabled(PlayerView view, TurnDraft draft, string king) =>
            Option(view)?.EligibleIds.Contains(king) == true && Eye(view, draft).Length > 0;

        public static string Tooltip(PlayerView view, TurnDraft draft, SovereignView king)
        {
            var name = Name(king);
            if (Enabled(view, draft, king.Id))
            {
                var eye = view.Dice.First(d => d.Id == Eye(view, draft));
                return "Click to attempt to corrupt " + name + " · Pays " + DieDescriptions.Summary(eye) + "\n" + Draws(view, name);
            }
            if (Option(view)?.EligibleIds.Contains(king.Id) == true)
                return "Attempt to corrupt " + name + "\nClick an Eye that pays in your dice row first.";
            var availability = view.Availability.FirstOrDefault(a => a.SubjectId == king.Id && a.ActionId == ActionId);
            var unmet = availability?.Conditions.Where(c => !c.Met).Select(c => "○ " + c.Description).ToList() ?? new List<string>();
            return "Attempt to corrupt " + name + (unmet.Count > 0 ? "\n" + string.Join("\n", unmet) : "\nNot available now");
        }

        // UI-442 (owner): what the attempt's click draws. A standard attempt draws every tile at once: one tile is placed on
        // the king, more wait beside him for the choice; Sequential corruption draws one at a time, each a choice.
        private static string Draws(PlayerView view, string name)
        {
            var limit = DrawLimit(Option(view));
            if (limit == 1)
                return "Draws a Corruption tile and places it on " + name;
            var tiles = TooltipText.Count(limit, "Corruption tile");
            return view.Configuration.SequentialCorruption
                ? "Draws up to " + tiles + ", one at a time · Keep a tile or draw the next"
                : "Draws " + tiles + " at once · Then click the one to place on " + name;
        }

        // T28: a tile for each Eye left in the Hunt Box once this one pays, up to three (an attempt needs two Eyes).
        private static int DrawLimit(ActionOption option) => Math.Min(3, Math.Max(1, option.EligibleDice.Count - 1));

        // The tiles beside a king: an attempt's draws, or an inspection's face-up tiles.
        public static IReadOnlyList<SovereignTile> Tiles(PlayerView view, SovereignView king)
        {
            var inspected = SovereignChoices.InspectedTiles(view);
            if (SovereignChoices.InspectedKing(view) == king.Id)
                return king.Tiles.Select(t => new SovereignTile
                {
                    Id = t.TileId, Value = t.Value, Kings = t.Kings, Selectable = inspected.ContainsKey(t.TileId),
                    Tooltip = inspected.TryGetValue(t.TileId, out var choice) ? choice.Tooltip : Face(t)
                }).ToList();
            var attempt = view.CorruptionAttempt;
            if (attempt == null || attempt.SovereignId != king.Id)
                return new List<SovereignTile>();
            if (view.You != Side.Shadow)
                return Enumerable.Range(0, attempt.DrawnCount).Select(i => new SovereignTile
                {
                    Id = "drawn-" + i, Back = true, Tooltip = "A tile the Shadow drew · Only the Shadow sees its value"
                }).ToList();
            var choose = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile);
            return attempt.OwnChoices.Select(t => new SovereignTile
            {
                Id = t.TileId, Value = t.Value, Kings = t.Kings, Selectable = choose?.EligibleIds.Contains(t.TileId) == true,
                Tooltip = choose?.EligibleIds.Contains(t.TileId) == true
                    ? "Click to attach this tile to " + Name(king) + " · " + Face(t) +
                        (view.Configuration.VisibleCorruption ? " · face up" : " · face down")
                    : Face(t)
            }).ToList();
        }

        // A draw that is the only legal step of a standard attempt: since UI-442 only in a game saved before it, whose one
        // draw now draws every remaining tile.
        public static bool MustDraw(PlayerView view) => OwnAttempt(view) && view.CorruptionAttempt?.Sequential == false &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.DrawCorruptionTile) &&
            !view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile);

        public static bool OwnAttempt(PlayerView view) => view?.Decision?.Kind == DecisionKind.CorruptionTile &&
            view.DecisionOwner == view.You && !view.Winner.HasValue;

        // The draw is the console's button, offered by Sequential corruption since UI-442: Draw again (a click on the latest
        // tile keeps it); Draw tiles only for a standard draw saved before UI-442.
        public static IReadOnlyList<ConsoleOption> ConsoleOptions(PlayerView view)
        {
            var options = new List<ConsoleOption>();
            var draw = OwnAttempt(view) ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.DrawCorruptionTile) : null;
            if (draw == null)
                return options;
            var sequential = view.CorruptionAttempt?.Sequential == true;
            options.Add(new ConsoleOption
            {
                Id = "draw", Name = "action-DrawCorruptionTile", Label = sequential ? "Draw again" : "Draw tiles",
                Kind = ConsoleOptionKind.Action, Command = CommandKind.DrawCorruptionTile, Enabled = draw.Enabled,
                Tooltip = DrawTooltip(view, sequential), Lit = OnlyStep(view, draw)
            });
            return options;
        }

        // UI-423 (owner's playtest pattern, like UI-418's single option): the draw lights and pulses while it is the
        // attempt's only enabled step; once a drawn tile may be kept instead, the glowing tile shares the choice.
        private static bool OnlyStep(PlayerView view, ActionOption draw) =>
            draw.Enabled && view.Options.All(option => option == draw || !option.Enabled);

        // UI-420 (owner): the draw names its king and how many tiles it may draw.
        private static string DrawTooltip(PlayerView view, bool sequential)
        {
            var attempt = view.CorruptionAttempt;
            var king = view.Sovereigns.FirstOrDefault(s => s.Id == attempt?.SovereignId);
            var name = king == null ? "the king" : Name(king);
            if (sequential)
                return "Draw again · Draw another Corruption tile for " + name + ", or click the latest tile beside him to keep it";
            var remaining = attempt == null ? 0 : attempt.DrawLimit - attempt.DrawnCount;
            var tiles = remaining > 0 ? "up to " + TooltipText.Count(remaining, "Corruption tile") : "the Corruption tiles";
            return "Draw tiles · Draw " + tiles + " for " + name + " at once · A Kings tile ends the draw";
        }

        private static string Name(SovereignView king) => king.Title.Split(' ')[0];

        private static string Face(CorruptionTileView tile) => tile.Kings ? "Kings tile · Corruption 0" : tile.Value.HasValue
            ? "Corruption " + tile.Value + (tile.Reveal ? " · Reveal" : "") : "Face-down Corruption tile";
    }
}
