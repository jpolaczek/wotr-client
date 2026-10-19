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

    // UI-274 (UX_SPEC): the Shadow's corruption attempt starts at the button beside an Initial king's portrait. Its click
    // pays a standard Shadow Eye from the Hunt Box (then a Ruler Eye); a Lords special Eye pays only when the player
    // clicked it first. The tiles drawn for the attempt stand beside the king: the Shadow sees their values, the Free
    // Peoples their backs; a click on one attaches it. Every draw is the console's button, even when it is the only legal
    // step (UI-279).
    public static class SovereignAttempt
    {
        public const string ActionId = "attempt-corruption";

        public static bool Shown(PlayerView view, SovereignView king) => view.You == Side.Shadow && king.Status == SovereignStatus.Initial;

        public static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.AttemptCorruption) : null;

        // The Eye that pays: one the player clicked first, else a standard Shadow Eye, else a Ruler Eye.
        public static string Eye(PlayerView view, TurnDraft draft)
        {
            var option = Option(view);
            if (option == null)
                return "";
            if (draft?.Payload.DieId is { Length: > 0 } chosen && option.EligibleDice.Contains(chosen))
                return chosen;
            var eyes = option.EligibleDice.Select(id => view.Dice.FirstOrDefault(d => d.Id == id)).Where(d => d != null).ToList();
            return eyes.FirstOrDefault(d => d.Source == "standard")?.Id ?? eyes.FirstOrDefault(RulerDice.IsRuler)?.Id ?? "";
        }

        public static bool Enabled(PlayerView view, TurnDraft draft, string king) =>
            Option(view)?.EligibleIds.Contains(king) == true && Eye(view, draft).Length > 0;

        public static string Tooltip(PlayerView view, TurnDraft draft, SovereignView king)
        {
            var name = king.Title.Split(' ')[0];
            if (Enabled(view, draft, king.Id))
            {
                var eye = view.Dice.First(d => d.Id == Eye(view, draft));
                return "Click to attempt to corrupt " + name + " · Pays " + DieDescriptions.Summary(eye) +
                    "\nDraw up to three tiles with the other Eyes in the Hunt Box.";
            }
            if (Option(view)?.EligibleIds.Contains(king.Id) == true)
                return "Attempt to corrupt " + name + "\nClick the special Eye that pays in the Hunt Box first.";
            var availability = view.Availability.FirstOrDefault(a => a.SubjectId == king.Id && a.ActionId == ActionId);
            var unmet = availability?.Conditions.Where(c => !c.Met).Select(c => "○ " + c.Description).ToList() ?? new List<string>();
            return "Attempt to corrupt " + name + (unmet.Count > 0 ? "\n" + string.Join("\n", unmet) : "\nNot available now");
        }

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
                    ? "Click to attach this tile to " + king.Title.Split(' ')[0] + " · " + Face(t) +
                        (view.Configuration.VisibleCorruption ? " · face up" : " · face down")
                    : Face(t)
            }).ToList();
        }

        // A draw that is the only legal step (Hidden or Visible corruption before the limit).
        public static bool MustDraw(PlayerView view) => OwnAttempt(view) && view.CorruptionAttempt?.Sequential == false &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.DrawCorruptionTile) &&
            !view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile);

        public static bool OwnAttempt(PlayerView view) => view?.Decision?.Kind == DecisionKind.CorruptionTile &&
            view.DecisionOwner == view.You && !view.Winner.HasValue;

        // The draw is the console's button: Draw again in Sequential corruption (a click on the latest tile keeps it),
        // Draw tiles otherwise.
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
                Tooltip = sequential ? "Draw another tile · Or click the latest tile beside the king to keep it"
                    : "Click to draw the Corruption tiles for this attempt"
            });
            return options;
        }

        private static string Face(CorruptionTileView tile) => tile.Kings ? "Kings tile · Corruption 0" : tile.Value.HasValue
            ? "Corruption " + tile.Value + (tile.Reveal ? " · Reveal" : "") : "Face-down Corruption tile";
    }
}
