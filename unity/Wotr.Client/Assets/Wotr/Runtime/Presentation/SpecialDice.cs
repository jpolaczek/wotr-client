using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-267: the Lords special dice in the dice pool (LORDS pp. 9–13, docs/rules/T24-LORDS-DICE-RINGS.md). Each wears
    // the art and emblem of its origin; after the special-die choice the dice not kept stay in their slot, dimmed,
    // until the next turn's roll; during the choice a lit die says what keeping it does. A choice that allows a single
    // die (an Eye forced among several dice) still waits for its click (UI-279).
    public static class SpecialDice
    {
        private static readonly IReadOnlyDictionary<string, string> Origins = new Dictionary<string, string>
        {
            ["lords.die.vilya"] = "vilya",
            ["lords.die.nenya"] = "nenya",
            ["lords.die.narya"] = "narya",
            ["lords.die.balrog"] = "balrog",
            ["lords.die.gothmog"] = "gothmog"
        };

        // The origin key of a Lords special die (its art and emblem), or "" for any other die.
        public static string Origin(DieView die) => Origins.TryGetValue(die.Source, out var origin) ? origin : "";

        public static bool IsLords(DieView die) => Origin(die).Length > 0;

        // Not kept in this turn's choice: it waits for the next roll in its own slot.
        public static bool SetAside(DieView die) =>
            IsLords(die) && die.Location == DieLocation.Discarded && !die.SpecialDieChosen && !die.PermanentlyRemoved;

        public static bool Choosing(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.SpecialDieChoice && view.DecisionOwner == view.You && !view.Winner.HasValue;

        // One of the player's own dice that the open choice is about.
        public static bool Candidate(PlayerView view, DieView die) =>
            Choosing(view) && die.Owner == view.You && IsLords(die) && die.Location == DieLocation.AwaitingChoice;

        // An Eye among the dice forces the choice of an Eye.
        public static bool EyeForced(PlayerView view) =>
            Choosing(view) && view.Dice.Any(d => Candidate(view, d) && d.Face == DieFace.Eye) &&
            view.Dice.Any(d => Candidate(view, d) && d.Face != DieFace.Eye);

        public static string Hint(PlayerView view) => EyeForced(view)
            ? "An Eye must be kept: click a highlighted Eye. Your other special dice wait for the next turn."
            : "Click a highlighted die to keep it. Your other special dice wait for the next turn.";

        // The tooltip of a die the open choice is about or set aside: what keeping it does, why an Eye rules it out, or
        // that it waits; "" for any other die.
        public static string Tooltip(PlayerView view, DieView die)
        {
            if (SetAside(die))
                return SetAsideTooltip(die);
            if (!Candidate(view, die))
                return "";
            return Keepable(view, die.Id) ? KeepTooltip(die) : BlockedTooltip(die);
        }

        public static bool Keepable(PlayerView view, string id) =>
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseSpecialDie && o.EligibleDice.Contains(id));

        public static string KeepTooltip(DieView die)
        {
            var lines = new List<string>
            {
                "Click to keep " + Name(die) + ".",
                die.Face == DieFace.Eye ? EyeUse(die.Owner) : "Use it as an action die this turn.",
                "Your other special dice are set aside until the next turn."
            };
            if (die.RemoveSymbol)
                lines.Add(RemoveLine(die, true));
            return string.Join("\n", lines);
        }

        public static string BlockedTooltip(DieView die) =>
            Name(die) + "\nAn Eye among your special dice must be kept, so this die waits for the next turn.";

        public static string SetAsideTooltip(DieView die)
        {
            var lines = new List<string> { Name(die), "Not kept this turn · rolled again in the next turn." };
            if (die.RemoveSymbol)
                lines.Add(RemoveLine(die, false));
            return string.Join("\n", lines);
        }

        // The printed Remove star (LORDS p. 13): it acts at the next Recover only on a kept die.
        public static string RemoveLine(DieView die, bool kept)
        {
            if (!kept)
                return "★ Remove · not kept, so it removes nothing.";
            var keeper = die.Owner == Side.FreePeoples ? "Gandalf the White" : "the Witch-king (either version)";
            return "★ Remove · kept, this die leaves the game at the next Recover if " + keeper + " is then in play.";
        }

        private static string Name(DieView die) => ExpansionLabels.DieSource(die) + " · " + DieDescriptions.Face(die.Face);

        private static string EyeUse(Side owner) => owner == Side.FreePeoples
            ? "This Eye goes to the Hunt Box and strengthens the Shadow's Hunt."
            : "This Eye goes to the Hunt Box.";
    }
}
