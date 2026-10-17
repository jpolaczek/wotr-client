using System;
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
                    return view.Fellowship.MordorStep.HasValue ? "Spend this die · Draw and resolve a Hunt tile before advancing."
                        : "Spend this die · Progress +1" + (preview.HasUnknownResult ? " · Hunt result unknown." : " · No Hunt dice.");
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
                        return draft.Payload.CompanionMoves.Count == 0 ? "Keep all companions in place · Resolve the card’s remaining effects."
                            : "Move the selected groups · Resolve the card’s remaining effects.";
                    return "One die moves all selected groups along their own routes.";
                case CommandKind.MoveShadowCharacters:
                    if (view.Decision?.Kind == DecisionKind.CharacterEvent)
                        return draft.Payload.CompanionMoves.Count == 0 ? "Keep all Nazgûl in place · Resolve the card’s remaining effects."
                            : "Move the selected Nazgûl · Resolve the card’s remaining effects.";
                    return "One die moves all selected groups along their own routes.";
                case CommandKind.RerollHunt:
                    return draft.Payload.Ids.Count > 0 ? "Reroll selected failures · Result unknown." : "Keep this roll" + (preview.HasUnknownResult ? " · Next tile unknown." : ".");
                case CommandKind.ResolveHuntDamage:
                    return draft.HuntChoice.HasValue ? HuntResponseActions.Describe(view, draft.HuntChoice.Value) : "Choose a response.";
                default:
                    return "Ready.";
            }
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
