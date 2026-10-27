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
    // does, and the click sends the choice. UI-278: Keep tiles is the console's Pass (D7); a Kings card's branch takes
    // its king's portrait (D15); during a battle a king who takes part is chosen in the battle window's army instead (D16).
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
            if (view.Decision.Presentation == KingsCardChoices.Branch)
                return KingsCardChoices.Kings(view);
            if (view.Decision.Presentation == Sovereign)
                return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && view.Sovereigns.Any(s => s.Id == c.Id) &&
                        !InBattle(view, c.Id))
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

        // UI-278 (D16): a king who takes part in the battle is one of its window's figures (an Initial king's Leader, an
        // Awakened king's portrait), so his choice glows there; a king outside it (a Corrupted king has no figure) keeps
        // his portrait.
        public static bool InBattle(PlayerView view, string id) => view.Battle != null && BattleObjectChoices.Owns(view, id);

        // A Combat card's choice of a king (There Is Hope for Victory, His Power Waxes) is made with the battle window open.
        public static bool InBattleWindow(PlayerView view) => view?.Battle != null && Option(view) != null &&
            view.Decision.Presentation == Sovereign;

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;

        private static string Title(PlayerView view, string id) => view.Sovereigns.FirstOrDefault(s => s.Id == id)?.Title ?? id;
    }
}
