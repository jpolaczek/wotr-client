using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One choice a sovereign's object (portrait or tile) makes in the current decision, and what its click does.
    public sealed class SovereignChoice
    {
        public string EffectId = "";
        public string Tooltip = "";
    }

    // UI-274 (UX_SPEC): a card's choice of a king, the Politics cleanse and a card's inspection of a king's tiles are
    // made on the sovereigns' portraits and tiles in the Characters panel: a lit portrait or tile says what its click
    // does, and the click sends the choice. Keep tiles stays a console button.
    public static class SovereignChoices
    {
        public const string Sovereign = "Sovereign";
        public const string Removal = "CorruptionRemoval";
        public const string Inspection = "InspectedCorruption";
        public const string Keep = "keep";
        public const string Decline = "decline";
        public const string Remove = "remove";
        private static readonly IReadOnlyDictionary<string, SovereignChoice> None = new Dictionary<string, SovereignChoice>();

        public static bool Owns(PlayerView view) => Option(view) != null &&
            view.Decision.Presentation is Sovereign or Removal or Inspection;

        // King id → the choice its portrait's click makes.
        public static IReadOnlyDictionary<string, SovereignChoice> Portraits(PlayerView view)
        {
            var option = Option(view);
            if (option == null)
                return None;
            if (view.Decision.Presentation == Sovereign)
                return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && view.Sovereigns.Any(s => s.Id == c.Id))
                    .ToDictionary(c => c.Id, c => new SovereignChoice
                    {
                        EffectId = c.Id, Tooltip = "Click to choose " + Title(view, c.Id) + (c.Detail.Length > 0 ? " · " + c.Detail : "")
                    });
            if (view.Decision.Presentation == Removal)
                return option.EffectChoices.Where(c => c.Id == Remove && option.EligibleIds.Contains(c.Id) && view.Sovereigns.Any(s => s.Id == c.TargetId))
                    .ToDictionary(c => c.TargetId, c => new SovereignChoice
                    {
                        EffectId = c.Id, Tooltip = "Click to return one random tile of " + Title(view, c.TargetId) + " to the Hunt Pool · " + c.Detail
                    });
            return None;
        }

        // The king whose tiles an inspection shows face up, or "".
        public static string InspectedKing(PlayerView view) => Option(view) is { } option && view.Decision.Presentation == Inspection
            ? option.EffectChoices.Select(c => c.TargetId).FirstOrDefault(id => id.Length > 0) ?? "" : "";

        // Tile id → the inspection's choice to return it.
        public static IReadOnlyDictionary<string, SovereignChoice> InspectedTiles(PlayerView view)
        {
            var option = Option(view);
            if (option == null || view.Decision.Presentation != Inspection)
                return None;
            return option.EffectChoices.Where(c => c.Id != Keep && option.EligibleIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => new SovereignChoice
                {
                    EffectId = c.Id, Tooltip = "Click to return this tile to the Hunt Pool · " + c.Title
                });
        }

        // The object-less answers: the cleanse's and the inspection's Keep tiles.
        public static IReadOnlyList<ConsoleOption> ConsoleOptions(PlayerView view, TurnDraft draft)
        {
            var option = Option(view);
            if (option == null)
                return new List<ConsoleOption>();
            return option.EffectChoices.Where(c => c.Id is Keep or Decline).Select(c => new ConsoleOption
            {
                Id = c.Id, Name = "effect-choice-" + c.Id, Label = c.Title.Length > 0 ? c.Title : "Keep tiles",
                Tooltip = c.Detail.Length > 0 ? c.Detail : "Keep the tiles in place", Kind = ConsoleOptionKind.Effect,
                Enabled = EffectChoiceDraft.CanChoose(draft, option, c.Id)
            }).ToList();
        }

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;

        private static string Title(PlayerView view, string id) => view.Sovereigns.FirstOrDefault(s => s.Id == id)?.Title ?? id;
    }
}
