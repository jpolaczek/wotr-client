using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-420 (owner): a ✓ says what it confirms — the card or ability and what was chosen, the armies and their routes,
    // the recruits and their settlements, the nation that advances — instead of "Ready" or "Confirm this step".
    public static class ConfirmSummary
    {
        // The console ✓: "Confirm · Wind from the West: Inspect Shadow hand".
        public static string Tooltip(PlayerView view, TurnDraft draft, string title)
        {
            var what = Generic(view, draft, title) ? DieUse.Tooltip(view, draft, Choices(view, draft)) : title;
            return what.Length == 0 ? "Confirm" : "Confirm · " + what;
        }

        // "Rohan → Fangorn (3 figures), Gondor → Osgiliath (1 figure)".
        public static string Moves(TurnDraft draft, BoardPresentation board) =>
            string.Join(", ", draft.Payload.Moves.Where(m => m.To.Length > 0).Select(m => Move(m, board)));

        // "Rohan Elite → Edoras, 2 Gondor Regular → Minas Tirith".
        public static string Recruits(PlayerView view, TurnDraft draft, BoardPresentation board) =>
            string.Join(", ", draft.Payload.Placements.GroupBy(p => p.RegionId)
                .Select(group => Units(view, group.Select(p => p.UnitId)) + " → " + Region(group.Key, board)));

        // "Wind from the West: Inspect Shadow hand"; the source alone before anything is chosen.
        public static string Choices(PlayerView view, TurnDraft draft)
        {
            var chosen = string.Join(", ", ChosenTitles(view, draft));
            var source = Source(view);
            if (source.Length == 0)
                return chosen;
            return chosen.Length == 0 ? source : source + ": " + chosen;
        }

        // The card or character whose decision this is; empty for other sources.
        public static string Source(PlayerView view)
        {
            var id = view?.Decision?.SourceId ?? "";
            if (id.Length == 0)
                return "";
            if (CardLibrary.Cards.ContainsKey(id))
                return CardLibrary.Title(id);
            return CharacterLibrary.Find(CharacterLibrary.Version(view, id)) != null ? CharacterLibrary.Name(view, id) : "";
        }

        private static IEnumerable<string> ChosenTitles(PlayerView view, TurnDraft draft)
        {
            var choices = DraftOptions.For(view, draft)?.EffectChoices ?? new List<EffectChoiceView>();
            return choices.Where(c => draft.Payload.Ids.Contains(c.Id) && c.Title.Length > 0).Select(c => c.Title);
        }

        // Before the preview the tray's ✓ only repeats the action's name ("Confirm choice.").
        private static bool Generic(PlayerView view, TurnDraft draft, string title) =>
            title.Length == 0 || draft.Kind is { } kind && title == DieUse.Tooltip(view, draft, DecisionTray.Label(kind) + ".");

        private static string Move(ArmyMoveChoice move, BoardPresentation board)
        {
            var figures = move.UnitIds.Count + move.CharacterIds.Count;
            return Region(move.From, board) + " → " + Region(move.To, board) + " (" + TooltipText.Count(figures, "figure") + ")";
        }

        private static string Units(PlayerView view, IEnumerable<string> ids) => string.Join(", ", ids
            .Select(id => view.Units.FirstOrDefault(u => u.Id == id))
            .Where(unit => unit != null)
            .GroupBy(unit => TooltipText.Unit(unit.Nation, unit.Kind))
            .Select(group => group.Count() > 1 ? group.Count() + " " + group.Key : group.Key));

        private static string Region(string id, BoardPresentation board) =>
            board != null ? ActionCardText.Region(id, board) : FigureGrid.Name(id);
    }
}
