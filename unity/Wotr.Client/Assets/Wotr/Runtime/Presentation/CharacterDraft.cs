using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CharacterDraft
    {
        public static bool UsesCharacters(CommandKind? kind) => kind is CommandKind.EnterCharacter or CommandKind.UseCharacterAbility;
        public static CharacterActionOption Selected(TurnDraft draft, ActionOption option) => option.CharacterActions
            .FirstOrDefault(c => c.CharacterId == draft.Payload.CharacterId && c.AbilityId == draft.Payload.AbilityId);

        public static void ClearSelection(TurnDraft draft) => draft.Change(p =>
        {
            p.CharacterId = "";
            p.AbilityId = "";
            p.RegionId = "";
            p.Ids.Clear();
            p.CouncilTokenId = "";
            p.Placements.Clear();
            draft.ReserveId = "";
        });

        public static void Select(TurnDraft draft, PlayerView view, CharacterActionOption choice)
        {
            draft.Change(p =>
            {
                p.CharacterId = choice.CharacterId;
                p.AbilityId = choice.AbilityId;
                p.RegionId = "";
                p.Ids.Clear();
                p.CouncilTokenId = "";
                p.Placements.Clear();
                draft.ReserveId = "";
                if (!choice.Dice.Contains(p.DieId))
                    p.DieId = UniformDie(view, choice);
                if (choice.RulerRecruitment == null && choice.Targets.Count == choice.TargetCount)
                {
                    if (draft.Kind == CommandKind.EnterCharacter)
                        p.RegionId = choice.Targets[0];
                    else
                        p.Ids.AddRange(choice.Targets);
                }
            });
        }

        // UI-239: one die, or identical dice, leave nothing to choose, so the cost is taken for the player.
        public static string UniformDie(PlayerView view, CharacterActionOption choice)
        {
            var dice = view.Dice.Where(d => choice.Dice.Contains(d.Id)).ToList();
            if (dice.Count == 0 || dice.Count != choice.Dice.Count)
                return "";
            var first = dice[0];
            return dice.All(d => d.Source == "standard" && first.Source == "standard" && d.Face == first.Face &&
                d.Location == first.Location && d.HuntOrigin == first.HuntOrigin) || dice.Count == 1 ? first.Id : "";
        }

        public static void Region(TurnDraft draft, ActionOption option, string region)
        {
            var choice = Selected(draft, option);
            if (choice?.RulerRecruitment != null)
            {
                CharacterRecruitDraft.Region(draft, choice, region);
                return;
            }
            if (choice?.Targets.Contains(region) != true)
                return;
            draft.Change(p =>
            {
                if (draft.Kind == CommandKind.EnterCharacter)
                    p.RegionId = region;
                else if (!p.Ids.Remove(region) && p.Ids.Count < choice.TargetCount)
                    p.Ids.Add(region);
            });
        }

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var choice = Selected(draft, option);
            return choice != null && choice.Dice.Contains(draft.Payload.DieId) &&
                (draft.Payload.CouncilTokenId.Length == 0 ? !choice.RequiresCouncilToken : choice.CouncilTokens.Contains(draft.Payload.CouncilTokenId)) &&
                CharacterRecruitDraft.Ready(draft, choice) && (choice.RulerRecruitment != null || (draft.Kind == CommandKind.EnterCharacter
                ? choice.Targets.Contains(draft.Payload.RegionId) : draft.Payload.Ids.Count == choice.TargetCount && draft.Payload.Ids.All(choice.Targets.Contains)));
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            if (!UsesCharacters(draft.Kind))
                return;
            var choice = Selected(draft, option);
            if (choice == null)
            {
                draft.Payload.CharacterId = "";
                draft.Payload.AbilityId = "";
            }
            if (choice?.Dice.Contains(draft.Payload.DieId) != true)
                draft.Payload.DieId = "";
            if (choice?.Targets.Contains(draft.Payload.RegionId) != true)
                draft.Payload.RegionId = "";
            if (choice?.CouncilTokens.Contains(draft.Payload.CouncilTokenId) != true)
                draft.Payload.CouncilTokenId = "";
            draft.Payload.Placements.RemoveAll(place => choice?.Recruitment.Any(g => g.ReserveIds.Contains(place.UnitId) && g.Targets.Contains(place.RegionId)) != true);
            if (choice?.Recruitment.Any(g => g.ReserveIds.Contains(draft.ReserveId)) != true)
                draft.ReserveId = "";
            draft.Payload.Ids.RemoveAll(id => choice?.Targets.Contains(id) != true);
        }
    }
}
