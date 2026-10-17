using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-215 (CARD_CHOICES_UX): a card's choice of figures on the board — one army's figures (ArmyFigure, ArmyFigures),
    // the companions travelling with the Dead Men, and the figures removed from an overstacked region after a card —
    // is made in the region panel. The regions holding offered figures glow, the panel lights the figures, a click
    // toggles one (a choice of a single figure sends at once) and the console's ✓ confirms the rest.
    public static class CardFigureDraft
    {
        public static bool Active(PlayerView view, TurnDraft draft) => CardDecisions.Owns(view, draft) &&
            (draft.Kind == CommandKind.RemoveExcess || draft.Kind == CommandKind.ChooseEffect && FigurePresentation(view.Decision.Presentation));

        private static bool FigurePresentation(string presentation) => presentation is "ArmyFigure" or "ArmyFigures" or "CompanionDestination";

        // The figures the decision offers; a Dead Men destination is a region, not a figure.
        public static IEnumerable<string> Offered(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return Enumerable.Empty<string>();
            var ids = draft.Kind == CommandKind.RemoveExcess ? option.EligibleIds : option.EffectChoices.Select(c => c.Id);
            return ids.Where(id => Location(view, id).Length > 0);
        }

        // The offered figures a click may toggle now: chosen ones, or ones the limits still admit.
        public static IEnumerable<string> Eligible(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            return Offered(view, draft).Where(id => draft.Payload.Ids.Contains(id) || Admits(draft, option, id));
        }

        private static bool Admits(TurnDraft draft, ActionOption option, string id) => draft.Kind == CommandKind.RemoveExcess
            ? draft.Payload.Ids.Count < option.Maximum
            : EffectChoiceDraft.CanChoose(draft, option, id);

        // Where the eligible figures stand, for the map's source highlight.
        public static IEnumerable<ArmyOption> Sources(PlayerView view, TurnDraft draft) => Eligible(view, draft)
            .Select(id => (Region: Location(view, id), Garrison: Garrison(view, id)))
            .Distinct()
            .Select(source => new ArmyOption { RegionId = source.Region, Garrison = source.Garrison });

        public static bool IsSource(PlayerView view, TurnDraft draft, string region) => Eligible(view, draft).Any(id => Location(view, id) == region);

        // The side whose markers glow: the offered figures' side.
        public static Side FigureSide(PlayerView view, TurnDraft draft)
        {
            var first = Offered(view, draft).FirstOrDefault() ?? "";
            return view.Units.FirstOrDefault(u => u.Id == first)?.Side ?? view.Characters.FirstOrDefault(c => c.Id == first)?.Side ?? view.You;
        }

        public static bool Toggle(PlayerView view, TurnDraft draft, string id)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null || !Eligible(view, draft).Contains(id))
                return false;
            if (draft.Kind == CommandKind.RemoveExcess)
                draft.Toggle(id);
            else if (view.Decision.Presentation == "CompanionDestination")
                EffectChoiceDraft.Choose(draft, option, id);
            else
                EffectChoiceDraft.Choose(draft, option, id, Location(view, id));
            return true;
        }

        // A choice of exactly one figure is complete with its click; a Dead Men journey still needs its destination.
        public static bool CompletesOnClick(PlayerView view, TurnDraft draft) =>
            DraftOptions.For(view, draft)?.Maximum == 1 && view.Decision.Presentation != "CompanionDestination";

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (view.Decision.Presentation == "CompanionDestination")
                return "Click the lit companions who travel, then a lit destination · ✓ confirms.";
            return CompletesOnClick(view, draft) ? "Click a lit region, then its lit figure."
                : "Click a lit region, then its lit figures · ✓ confirms.";
        }

        private static string Location(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Location ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? "";

        private static bool Garrison(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Garrison ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison ?? false;
    }
}
