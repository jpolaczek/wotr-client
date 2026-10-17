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
            return view.Decision.Kind == DecisionKind.Action && draft?.Kind is
                CommandKind.AwakenSovereign or CommandKind.AttemptCorruption or CommandKind.RemoveCorruptedSovereign
                ? draft.Payload.CharacterId : "";
        }

        public static bool OwnsDecision(PlayerView view, TurnDraft draft) => DecisionTarget(view, draft).Length > 0;

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
