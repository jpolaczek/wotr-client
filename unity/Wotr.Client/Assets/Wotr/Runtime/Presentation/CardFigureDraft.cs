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

        // UI-423 (owner's playtest pattern, like UI-375's TravelDraft.DestinationHint): once figures are chosen the hint names
        // them and asks for what is left — the Dead Men's destination, more figures or ✓ — instead of repeating the start.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (view.Decision.Presentation == "CompanionDestination")
                return JourneyHint(view, draft);
            if (CompletesOnClick(view, draft))
                return "Click a lit region, then its lit figure.";
            var chosen = Chosen(view, draft).ToList();
            if (chosen.Count == 0)
                return "Click a lit region, then its lit figures · ✓ confirms.";
            return Names(view, chosen) + Remaining(view, draft, chosen);
        }

        // What is left once figures are chosen: more of them, ✓, or either.
        private static string Remaining(PlayerView view, TurnDraft draft, IReadOnlyCollection<string> chosen)
        {
            if (!draft.Ready(DraftOptions.For(view, draft)))
                return " · Click another lit figure.";
            return Eligible(view, draft).Any(id => !chosen.Contains(id)) ? " · ✓ confirms, or click another lit figure." : " · ✓ confirms.";
        }

        // The Dead Men's journey: the companions who travel, then their destination.
        private static string JourneyHint(PlayerView view, TurnDraft draft)
        {
            var chosen = Chosen(view, draft).ToList();
            var destination = Destination(view, draft);
            if (chosen.Count == 0)
                return destination ? "Click the lit companions who travel · ✓ confirms."
                    : "Click the lit companions who travel, then a lit destination · ✓ confirms.";
            return Names(view, chosen) + (destination ? " · ✓ confirms, or click another lit destination." : " · Click a lit destination.");
        }

        // The offered figures chosen so far.
        private static IEnumerable<string> Chosen(PlayerView view, TurnDraft draft) =>
            Offered(view, draft).Where(draft.Payload.Ids.Contains);

        private static bool Destination(PlayerView view, TurnDraft draft) => DraftOptions.For(view, draft)?.EffectChoices
            .Any(choice => choice.SelectionGroup == "destination" && draft.Payload.Ids.Contains(choice.Id)) == true;

        // "Gimli and Legolas", "Sauron Regular ×2": characters by name, units by nation and kind.
        private static string Names(PlayerView view, IEnumerable<string> ids)
        {
            var names = ids.Select(id => Name(view, id)).GroupBy(name => name)
                .Select(group => group.Count() > 1 ? group.Key + " ×" + group.Count() : group.Key).ToList();
            return names.Count <= 1 ? names.FirstOrDefault() ?? ""
                : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }

        private static string Name(PlayerView view, string id) => view.Units.FirstOrDefault(u => u.Id == id) is { } unit
            ? TooltipText.Unit(unit.Nation, unit.Kind) : CharacterLibrary.Name(view, id);

        private static string Location(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Location ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? "";

        private static bool Garrison(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Garrison ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison ?? false;
    }
}
