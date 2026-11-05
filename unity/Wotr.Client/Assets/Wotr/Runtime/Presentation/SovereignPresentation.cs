using System.Collections.Generic;
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
                    : SovereignAttempt.MustDraw(view) ? "Click Draw tiles to draw the Corruption tiles" : "Click a glowing tile beside the king to attach it";
            if (view.Decision.Presentation == SovereignChoices.Inspection)
                return "Click a tile beside the king to return it to the Hunt pool";
            if (view.Decision.Presentation == "CorruptionRemoval")
                return "Click the glowing king to return one random tile";
            if (view.Decision.Presentation == SovereignChoices.Sovereign)
                return SovereignInBattle(view);
            if (KingAction(view, draft) && draft.Payload.CharacterId.Length == 0)
                return draft.Kind == CommandKind.AttemptCorruption ? "Click the attempt button beside a king in the Characters panel"
                    : "Click a glowing king, by his portrait or on the map";
            return draft?.Kind == CommandKind.AwakenSovereign ? "Click a highlighted region on the map for the awakened king" : "";
        }

        // UI-278 (D16): a king who takes part in the battle glows in its window's army, any other by his portrait.
        private static string SovereignInBattle(PlayerView view)
        {
            var kings = view.Options.Where(o => o.Enabled && o.Kind == CommandKind.ChooseEffect).SelectMany(o => o.EligibleIds)
                .Where(id => view.Sovereigns.Any(s => s.Id == id)).ToList();
            var battle = kings.Count(id => SovereignChoices.InBattle(view, id));
            if (battle == 0)
                return "Click a glowing king, by his portrait or on the map";
            return battle == kings.Count ? "Click a glowing king in the battle window's army"
                : "Click a glowing king in the battle window's army or by his portrait";
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

        // UI-436: the window's status badge reads the word beside its icon, and its tooltip what the state means
        // (KINGS p. 8–13: an Initial sovereign is a Free Peoples Leader, an Awakened one a Companion).
        public static string StatusWord(SovereignStatus status) => status switch
        {
            SovereignStatus.Initial => "Initial",
            SovereignStatus.Awakened => "Awakened",
            SovereignStatus.Corrupted => "Corrupted",
            _ => "Removed"
        };

        public static string StatusMeaning(SovereignStatus status) => status switch
        {
            SovereignStatus.Initial => "Initial · Not yet awakened: he leads his force as a Leader",
            SovereignStatus.Awakened => "Awakened · A Companion with the abilities of his card",
            SovereignStatus.Corrupted => "Corrupted · He serves the Shadow and his weaknesses apply",
            _ => "Removed · Out of play"
        };

        // UI-436: the window's corruption line and what it means, one short line each — an Initial king is corrupted when
        // his tiles reach his resistance (CorruptionRules); UI-383: a corrupted or removed king has no count left.
        public static string WindowCorruption(SovereignView king) => king.Status switch
        {
            SovereignStatus.Initial or SovereignStatus.Awakened => Corruption(king),
            SovereignStatus.Corrupted => "Corruption resolved",
            _ => "Out of play"
        };

        public static string CorruptionMeaning(SovereignView king) => king.Status switch
        {
            SovereignStatus.Initial => "Corrupted when his tiles reach " + king.Resistance,
            SovereignStatus.Corrupted => "He now serves the Shadow",
            SovereignStatus.Awakened => "Awakened: no longer corrupted",
            _ => "Removed from play"
        };

        public static string Flags(SovereignView king) => TooltipText.Parts(
            king.Garrison ? "In the stronghold's garrison" : "",
            king.AttemptedThisTurn && king.Status == SovereignStatus.Initial ? "Corruption attempted this turn" : "");

        // UI-436: when the abilities listed under the window's heading apply.
        public static string AbilityHeading(SovereignStatus status) => status switch
        {
            SovereignStatus.Initial => "Abilities once awakened",
            SovereignStatus.Corrupted => "Weaknesses while corrupted",
            SovereignStatus.Awakened => "Abilities",
            _ => "Out of play: no abilities"
        };

        // UI-436: the king's actions for this player in words (AvailabilityLines); without one, what his state means.
        public static IReadOnlyList<AvailabilityLine> Actions(PlayerView view, SovereignView king)
        {
            var lines = AvailabilityLines.For(view, king.Id);
            return lines.Count > 0 ? lines : new[] { Standing(king) };
        }

        public static AvailabilityLine Standing(SovereignView king) => king.Status switch
        {
            SovereignStatus.Initial => new AvailabilityLine { Title = "Not yet awakened",
                State = "The Free Peoples may awaken him, the Shadow corrupt him" },
            SovereignStatus.Awakened => new AvailabilityLine { Title = "Awakened", State = "A Companion with the abilities below" },
            SovereignStatus.Corrupted => new AvailabilityLine { Title = "Serves the Shadow", State = "His weaknesses below apply" },
            _ => new AvailabilityLine { Title = "Out of play", State = "Nothing more can happen to him" }
        };

        // UI-383 (owner): a corrupted king's tiles are resolved and a removed one is out of play (T28, KINGS p. 12–13), so
        // neither shows a Corruption count. UI-420 (owner): the count reads as the Fellowship's ("Corruption 2/4"), and the
        // status word stays with the status ("◉ Corrupted"), not repeated here.
        public static string Corruption(SovereignView sovereign) => sovereign.Status switch
        {
            SovereignStatus.Corrupted => "His tiles are resolved; he now serves the Shadow",
            SovereignStatus.Removed => "Removed from play",
            _ => "Corruption " + (sovereign.Corruption.HasValue ? sovereign.Corruption.Value.ToString() : "?") + "/" + sovereign.Resistance
        };

        // UI-420 (owner): a king's portrait and map marker read alike: "Dáin Ironfoot · ◉ Corrupted · Erebor", then his
        // corruption; a removed king is one line.
        public static string Describe(SovereignView king, string region)
        {
            if (king.Status == SovereignStatus.Removed)
                return TooltipText.Parts(king.Title, "× Removed from play");
            var heading = TooltipText.Parts(king.Title, Status(king.Status), region, king.Garrison ? "Garrison" : "");
            return TooltipText.Lines(heading, Corruption(king));
        }

        public static string Consequence(PlayerView view, TurnDraft draft)
        {
            if (view.Decision?.Presentation == "CorruptionRemoval")
                return "Return one random non-Kings tile to the Hunt pool, or keep the tiles. No extra die.";
            if (draft?.Kind == CommandKind.AwakenSovereign)
                return "Return all attached tiles · " + (view.Configuration.RemovingRulerTokens
                    ? "Ruler tokens are not used."
                    : "Gain his Ruler token.") + "\nThe shared Ruler die arrives at the next Recover if an Awakened sovereign remains.";
            if (draft?.Kind == CommandKind.RemoveCorruptedSovereign)
                return "Remove this Corrupted sovereign from play; his weaknesses end.";
            return view.CorruptionAttempt != null ? AttemptConsequence(view) : "";
        }

        // UI-442: a standard attempt waits only once its tiles are drawn; a draw still due is a game saved before UI-442.
        private static string AttemptConsequence(PlayerView view)
        {
            if (view.CorruptionAttempt.Sequential)
                return "Draw again, or keep the latest tile. A Kings tile ends the draw.";
            return SovereignAttempt.MustDraw(view) ? "Draw the remaining tiles at once, then choose one. A Kings tile ends the draw."
                : "Attach one drawn tile; the others are revealed and return to the Hunt pool.";
        }

        public static string Cost(PlayerView view, TurnDraft draft, string target)
        {
            var id = view.CorruptionAttempt?.SovereignId == target ? view.CorruptionAttempt.DieId :
                DecisionTarget(view, draft) == target ? draft?.Payload.DieId ?? "" : "";
            var die = view.Dice.FirstOrDefault(d => d.Id == id);
            return die == null ? "" : "Cost: " + ExpansionLabels.DieFace(die) +
                (ExpansionLabels.Special(die) ? " · " + ExpansionLabels.DieSource(die) : "") +
                (view.CorruptionAttempt?.SovereignId == target ? " · Committed" : "");
        }
    }
}
