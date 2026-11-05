using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class FellowshipSummary
    {
        public static string Describe(TurnDraft draft, PlayerView view, ActionPreview preview, BoardPresentation data)
        {
            switch (draft.Kind)
            {
                case CommandKind.MoveFellowship:
                    return view.Fellowship.MordorStep.HasValue ? "Spend this die · Move the Fellowship · Draw and resolve a Hunt tile before advancing."
                        : "Spend this die · Move the Fellowship · Progress +1; its last known position stays · " +
                            (preview.HasUnknownResult ? "Then the Hunt; its result is unknown." : "No Hunt roll: the Hunt Box is empty.");
                case CommandKind.HideFellowship:
                    return "Spend this die · Hide the Fellowship at its current position.";
                case CommandKind.EnterMordor:
                    return "Enter Mordor · Prepare the Hunt pool · No die spent.";
                case CommandKind.SelectGuide:
                    return "★ " + CharacterLibrary.Name(draft.Payload.GuideId) + " becomes the Guide" +
                        (view.Phase == GamePhase.Fellowship ? " · Continue to Hunt allocation." : ".");
                case CommandKind.DeclareFellowship:
                    return "Remain hidden · Progress → 0" + (preview.Fellowship?.Healed > 0 ? " · Heal " + preview.Fellowship.Healed : "") +
                        string.Concat(preview.PoliticsChanges.Where(n => n.Active).Select(n => " · Activate " + FigureGrid.Name(n.Id))) + ".";
                case CommandKind.RevealFellowship:
                    return preview.Fellowship?.AdditionalHuntRegions.Count > 0 ? "Become revealed · Progress → 0 · Extra Hunt: " +
                        string.Join(", ", preview.Fellowship.AdditionalHuntRegions.Select(id => data.Regions[id].Name)) + " · Tiles unknown."
                        : "Become revealed · Progress → 0 · No stronghold Hunt on this route.";
                case CommandKind.SeparateCompanions:
                    var placed = preview.Placements.FirstOrDefault();
                    return (placed == null ? "" : "Separate to " + data.Regions[placed.RegionId].Name + ". ") +
                        (preview.RemovedIds.Count == 0 ? "Separated companions cannot rejoin." : "Leave play: " +
                            string.Join(", ", preview.RemovedIds.Select(id => CharacterLibrary.Name(view, id))) + ".");
                case CommandKind.MoveCompanions:
                    if (view.Decision?.Kind == DecisionKind.CompanionSeparation)
                        return CardJourney(view, draft, data) + " · Resolve the card’s remaining effects.";
                    return Groups(draft, data, ids => Companions(view, ids));
                case CommandKind.MoveShadowCharacters:
                    // UI-416 (owner): the figures by name, never "Nazgûl" for the Balrog.
                    if (view.Decision?.Kind == DecisionKind.CharacterEvent)
                        return (draft.Payload.CompanionMoves.Count == 0 ? "Keep " + Kept(view, draft) + " in place"
                            : "Move " + NazgulFlightDraft.Figures(view, draft.Payload.CompanionMoves.SelectMany(g => g.Ids))) +
                            " · Resolve the card’s remaining effects.";
                    return Groups(draft, data, ids => NazgulFlightDraft.Figures(view, ids));
                case CommandKind.RerollHunt:
                    return Reroll(view, draft);
                case CommandKind.ResolveHuntDamage:
                    return draft.HuntChoice.HasValue ? HuntResponseActions.Describe(view, draft.HuntChoice.Value) : "Choose a response.";
                default:
                    return "Ready.";
            }
        }

        private static string Kept(PlayerView view, TurnDraft draft) =>
            NazgulFlightDraft.Figures(view, NazgulFlightDraft.Offered(view, draft)) is { Length: > 0 } names ? names : "every figure";

        // UI-420 (owner): one die's groups by name and where each one goes —
        // "Move Strider → Fangorn, Gimli and Legolas → Erebor".
        private static string Groups(TurnDraft draft, BoardPresentation data, Func<IReadOnlyList<string>, string> names)
        {
            var groups = Moves(draft, data, names);
            return groups.Count == 0 ? "Move the selected groups along their own routes." : "Move " + string.Join(", ", groups) + ".";
        }

        private static List<string> Moves(TurnDraft draft, BoardPresentation data, Func<IReadOnlyList<string>, string> names) =>
            draft.Payload.CompanionMoves.Where(group => group.Ids.Count > 0)
                .Select(group => Destination(names(group.Ids), group.Route, data)).ToList();

        // UI-423 (owner's playtest pattern, like UI-420): a card's journey (Gwaihir, Book of Mazarbul, Kings' Aid) names its
        // companions and destinations as a die's does — "Move Gimli → Erebor" — or those who stay — "Keep Gimli and
        // Legolas in place".
        private static string CardJourney(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            var moves = Moves(draft, data, ids => Companions(view, ids));
            if (moves.Count > 0)
                return "Move " + string.Join(", ", moves);
            var offered = DraftOptions.For(view, draft)?.Travel.Select(travel => travel.Id).ToList() ?? new List<string>();
            return offered.Count == 0 ? "Keep every companion in place" : "Keep " + Companions(view, offered) + " in place";
        }

        private static string Companions(PlayerView view, IEnumerable<string> ids) =>
            Join(ids.Select(id => CharacterLibrary.Name(view, id)).Distinct().ToList());

        private static string Destination(string names, IReadOnlyList<string> route, BoardPresentation data) =>
            route.Count > 0 && data != null && data.Regions.TryGetValue(route[route.Count - 1], out var region)
                ? names + " → " + region.Name : names;

        // "Gimli", "Gimli and Legolas", "Gimli, Legolas and Boromir".
        private static string Join(IReadOnlyList<string> names) => names.Count <= 1 ? names.FirstOrDefault() ?? ""
            : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];

        // UI-420 (owner): Keep roll and Reroll selected name the roll and the dice, in the battle's words.
        private static string Reroll(PlayerView view, TurnDraft draft)
        {
            var selected = draft.Payload.Ids.Count;
            var rerolls = DraftOptions.For(view, draft)?.Maximum ?? 0;
            if (selected > 0)
                return RerollChoiceText.Reroll(selected, rerolls);
            return RerollChoiceText.Keep("Hunt", view.Hunt?.Dice.Count(die => die.Success) ?? 0, rerolls);
        }

        public static string Victory(VictoryCondition? reason) => reason switch
        {
            VictoryCondition.RingCorrupted => "The Ring-bearers are corrupted.",
            VictoryCondition.RingDestroyed => "The One Ring is destroyed.",
            VictoryCondition.ShadowMilitary => "Shadow reaches its military victory threshold.",
            VictoryCondition.FreePeoplesMilitary => "Free Peoples reach their military victory threshold.",
            _ => ""
        };
    }
}
