using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Describes projected state and locates a decision; it never grants a game action.
    public static class SovereignPresentation
    {
        public static string DecisionTarget(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You || view.Winner.HasValue)
                return "";
            if (view.Decision.Kind == DecisionKind.CorruptionTile)
                return view.CorruptionAttempt?.SovereignId ?? "";
            if (view.Decision.Presentation == "CorruptionRemoval")
                return view.Options.SelectMany(o => o.EffectChoices).Select(c => c.TargetId)
                    .FirstOrDefault(id => view.Sovereigns.Any(s => s.Id == id)) ?? "";
            if (view.Decision.Presentation == SovereignChoices.Inspection)
                return SovereignChoices.InspectedKing(view);
            return view.Decision.Kind == DecisionKind.Action && draft?.Kind is
                CommandKind.AwakenSovereign or CommandKind.AttemptCorruption or CommandKind.RemoveCorruptedSovereign
                ? draft.Payload.CharacterId : "";
        }

        // UI-274: the decision is made on the kings' portraits, attempt buttons and tiles; the tray builds no choice. A
        // king's action whose king is not chosen yet (a die selected for it) waits on them too.
        public static bool OwnsDecision(PlayerView view, TurnDraft draft) => DecisionTarget(view, draft).Length > 0 || SovereignChoices.Owns(view) ||
            KingAction(view, draft);

        private static bool KingAction(PlayerView view, TurnDraft draft) => view?.Decision?.Kind == DecisionKind.Action &&
            view.DecisionOwner == view.You && !view.Winner.HasValue && draft?.Kind is CommandKind.AwakenSovereign or CommandKind.AttemptCorruption or CommandKind.RemoveCorruptedSovereign;

        // What the step waits for, for the console's hint.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (!OwnsDecision(view, draft))
                return "";
            if (SovereignAttempt.OwnAttempt(view))
                return view.CorruptionAttempt?.Sequential == true ? "Click the latest tile beside the king to keep it · Or draw again"
                    : SovereignAttempt.ForcedDraw(view) ? "Draw the Corruption tiles for the attempt" : "Click a glowing tile beside the king to attach it";
            if (view.Decision.Presentation == SovereignChoices.Inspection)
                return "Click a tile beside the king to return it to the Hunt Pool · Or keep the tiles";
            if (view.Decision.Presentation == "CorruptionRemoval")
                return "Click the glowing king to return one random tile · Or keep the tiles";
            if (view.Decision.Presentation == SovereignChoices.Sovereign)
                return "Click a glowing king, by his portrait or on the map";
            if (KingAction(view, draft) && draft.Payload.CharacterId.Length == 0)
                return draft.Kind == CommandKind.AttemptCorruption ? "Click the attempt button beside a king in the Characters panel"
                    : "Click a glowing king, by his portrait or on the map";
            return draft?.Kind == CommandKind.AwakenSovereign ? "Click a highlighted region on the map for the awakened king" : "";
        }

        // UI-274: what a click on a king the selected die lights does, for his portrait's and map object's tooltips.
        public static string LitHint(PlayerView view, TurnDraft draft, string id)
        {
            var name = view.Sovereigns.FirstOrDefault(s => s.Id == id)?.Title ?? id;
            return ExpansionActions.Sovereign(view, draft, id)?.Kind switch
            {
                CommandKind.AwakenSovereign => "Click to awaken " + name,
                CommandKind.RemoveCorruptedSovereign => "Click to remove the corrupted " + name,
                CommandKind.AttemptCorruption => "Click to attempt to corrupt " + name,
                _ => "Click to choose " + name
            };
        }

        public static string Status(SovereignStatus status) => status switch
        {
            SovereignStatus.Initial => "♙ Initial",
            SovereignStatus.Awakened => "★ Awakened",
            SovereignStatus.Corrupted => "◉ Corrupted",
            _ => "× Removed"
        };

        public static string Corruption(SovereignView sovereign) => "Corruption " +
            (sovereign.Corruption.HasValue ? sovereign.Corruption.Value.ToString() : "?") + " / " + sovereign.Resistance;

        public static string Consequence(PlayerView view, TurnDraft draft)
        {
            if (view.Decision?.Presentation == "CorruptionRemoval")
                return "Return one random non-Kings tile to the Hunt Pool, or keep the tiles. No extra die.";
            if (draft?.Kind == CommandKind.AwakenSovereign)
                return "Return all attached tiles · " + (view.Configuration.RemovingRulerTokens
                    ? "Ruler tokens are not used."
                    : "Gain its Ruler token.") + "\nShared Ruler: next Recover if an Awakened Sovereign remains.";
            if (draft?.Kind == CommandKind.RemoveCorruptedSovereign)
                return "Remove this Corrupted Sovereign through the eligible Companion. This is not a tile return.";
            if (view.CorruptionAttempt != null)
                return view.CorruptionAttempt.Sequential ? "Draw again or keep only the latest tile. Kings ends the draw."
                    : "Draw up to the limit, then choose one tile. Kings ends the draw.";
            return "";
        }

        public static string Cost(PlayerView view, TurnDraft draft, string target)
        {
            var id = view.CorruptionAttempt?.SovereignId == target ? view.CorruptionAttempt.DieId :
                DecisionTarget(view, draft) == target ? draft?.Payload.DieId ?? "" : "";
            var die = view.Dice.FirstOrDefault(d => d.Id == id);
            return die == null ? "" : "Cost · " + ExpansionLabels.DieFace(die) +
                (ExpansionLabels.Special(die) ? " · " + ExpansionLabels.DieSource(die) : "") +
                (view.CorruptionAttempt?.SovereignId == target ? " · Committed" : "");
        }
    }
}
