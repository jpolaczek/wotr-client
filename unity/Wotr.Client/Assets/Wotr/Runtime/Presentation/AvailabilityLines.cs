using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-436: one action of a character's or a sovereign's window in two short lines, instead of a bare "Unavailable" or
    // "Limits" whose meaning hid in a tooltip: what the action is and what it takes ("Awaken · Muster or Ruler die"), then
    // whether it can be done now or what it still needs ("His nation must be active"). The tooltip keeps the server's
    // full text: the cost, every condition with ✓ or ○, the timing and the rules page.
    public sealed class AvailabilityLine
    {
        public string Title { get; set; } = "";
        public string State { get; set; } = "";
        public bool Ready { get; set; }
        public string Tooltip { get; set; } = "";
    }

    // Describes the projected availability only; it never grants an action.
    public static class AvailabilityLines
    {
        public const string Ready = "Available now";
        private const string Cleanse = "cleanse";
        private static readonly string[] StatusCodes = { "initial", "corrupted" };

        // The short form of a cost the window has no room for; the full cost stays in the tooltip.
        private static readonly IReadOnlyDictionary<string, string> Costs = new Dictionary<string, string>
        {
            ["awaken"] = "Muster or Ruler die",
            ["attempt-corruption"] = "an Eye from the Hunt Box",
            ["remove-corrupted"] = "Character die",
            [Cleanse] = "no die"
        };

        // The short form of a condition that is not met (SovereignAvailabilityProjection's codes); another condition reads
        // as the server describes it.
        private static readonly IReadOnlyDictionary<string, string> Missing = new Dictionary<string, string>
        {
            ["awakening-route"] = "No region he can reach",
            ["awakening-die"] = "No Muster or Ruler die available",
            ["companion-icon"] = "Needs a Companion of his nation in his region",
            ["character-die"] = "No Character die available",
            ["attempt-turn"] = "Already attempted this turn",
            ["fp-unused-die"] = "The Free Peoples have no unused die",
            ["hunt-eyes"] = "Needs 2 Eyes in the Hunt Box",
            ["ring-action"] = "An Elven Ring's action comes first",
            ["cleansing-conditions"] = "Needs a non-Kings tile and a Companion of his nation here"
        };

        public static IReadOnlyList<AvailabilityLine> For(PlayerView view, string subject) =>
            (view?.Availability ?? new List<ActionAvailabilityView>())
            .Where(availability => availability.SubjectId == subject && Applies(availability))
            .Select(Line)
            .ToList();

        // An action that belongs to another state of the subject (Awaken for a Corrupted king) is left out, and so is
        // Return one Corruption tile outside the moment it may be done.
        private static bool Applies(ActionAvailabilityView availability) =>
            !availability.Conditions.Any(c => !c.Met && StatusCodes.Contains(c.Code)) &&
            (availability.ActionId != Cleanse || availability.WindowOpen);

        public static AvailabilityLine Line(ActionAvailabilityView availability) => new AvailabilityLine
        {
            Title = TooltipText.Parts(availability.Title, Cost(availability)),
            State = State(availability),
            Ready = availability.CanAct,
            Tooltip = CharacterRosterPresentation.Conditions(availability)
        };

        private static string Cost(ActionAvailabilityView availability) =>
            Costs.TryGetValue(availability.ActionId ?? "", out var cost) ? cost : "";

        private static string State(ActionAvailabilityView availability)
        {
            if (availability.CanAct)
                return Ready;
            var missing = availability.Conditions.FirstOrDefault(c => !c.Met);
            if (missing != null)
                return Condition(missing);
            return availability.WindowOpen ? "Not available now" : Sentence(availability.Timing);
        }

        private static string Condition(AvailabilityConditionView condition)
        {
            if (Missing.TryGetValue(condition.Code ?? "", out var text))
                return text;
            if (condition.Code == "nation")
                return condition.Description.Contains("at war") ? "His nation must be at war" : "His nation must be active";
            return Sentence(condition.Description);
        }

        private static string Sentence(string text) => (text ?? "").Trim().TrimEnd('.');
    }
}
