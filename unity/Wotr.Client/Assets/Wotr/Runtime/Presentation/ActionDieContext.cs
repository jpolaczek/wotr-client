using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionDieContext
    {
        private static readonly DieFace[] Combined = { DieFace.Army, DieFace.Muster };
        private static readonly DieFace[] Wild = { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.Event, DieFace.WillOfTheWest };
        public string DieId { get; private set; } = "";
        public DieFace? Face { get; private set; }
        public IReadOnlyList<DieFace> Choices { get; private set; } = System.Array.Empty<DieFace>();
        private string ability = "";
        public string AbilityId => ability;

        public void Sync(PlayerView view, string id)
        {
            if (view.Decision?.Kind == DecisionKind.ActionSequence && view.ActionSequence is { } sequence && sequence.Owner == view.You)
            {
                DieId = sequence.DieId;
                Face = sequence.Face;
                ability = sequence.AbilityId;
                Choices = System.Array.Empty<DieFace>();
                return;
            }
            var die = view.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You
                ? view.Dice.FirstOrDefault(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available)
                : null;
            if (die == null)
            {
                Clear();
                return;
            }
            DieId = id;
            ability = "";
            Choices = die.Face == DieFace.ArmyMuster ? Combined
                : die.Face == DieFace.WillOfTheWest ? Wild : System.Array.Empty<DieFace>();
            Face = die.Face;
        }

        public void BindCost(PlayerView view, TurnDraft draft)
        {
            if (view.Decision?.Kind != DecisionKind.Action || draft.Payload.DieId != DieId || !draft.Kind.HasValue)
                return;
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return;
            if (draft.Payload.AbilityId.Length == 0)
            {
                var intended = draft.Kind switch
                {
                    CommandKind.MoveArmies or CommandKind.AttackArmy => draft.Payload.MovementCost,
                    CommandKind.Recruit or CommandKind.AdvancePolitics => DieFace.Muster,
                    CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or CommandKind.MoveFellowship => DieFace.Character,
                    CommandKind.PlayEvent => draft.Payload.CardCost,
                    _ => Face
                };
                if (intended.HasValue && Supports(intended.Value))
                    Face = intended;
                return;
            }
            var cost = option.MovementCosts.FirstOrDefault(c => c.DieId == DieId && c.AbilityId == draft.Payload.AbilityId && c.Face == draft.Payload.MovementCost);
            if (cost != null)
                Face = cost.Face;
            else if (option.RulerTravel?.AbilityId == draft.Payload.AbilityId && option.RulerTravel.Dice.Contains(DieId))
                Face = DieFace.Character;
            else
                return;
            ability = draft.Payload.AbilityId;
            Choices = System.Array.Empty<DieFace>();
        }

        public bool Supports(DieFace face) => Choices.Count > 0 ? Choices.Contains(face) : Face == face;

        // The object gesture chooses an ordinary use of this exact die. Special
        // abilities remain explicit, and paid sequences expose only their saved face.
        public DieFace? ArmyFace(ActionOption option) => ArmyCost(option)?.Face;

        // UI-145: the cost this die pays for an army option; it tells whether the force needs a commander.
        public MovementCostOption ArmyCost(ActionOption option) => option.MovementCosts
            .Where(c => c.DieId == DieId && c.AbilityId == ability && Supports(c.Face))
            .OrderBy(c => c.Face == DieFace.Army ? 0 : 1)
            .FirstOrDefault();

        public static bool DirectIntent(TurnDraft draft) => draft.Kind is null or
            CommandKind.MoveArmies or CommandKind.AttackArmy or CommandKind.Recruit or
            CommandKind.AdvancePolitics or CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or
            CommandKind.PlayEvent or CommandKind.DrawCard;

        public bool Allows(ActionOption option)
        {
            if (DieId.Length == 0 || !option.Enabled || !option.EligibleDice.Contains(DieId))
                return false;
            if (option.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
                return ArmyFace(option).HasValue;
            return option.Kind switch
            {
                CommandKind.Recruit or CommandKind.AdvancePolitics => Supports(DieFace.Muster),
                CommandKind.DrawCard => Supports(DieFace.Event) || Supports(DieFace.CardDraw),
                CommandKind.MoveFellowship or CommandKind.HideFellowship or CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters => Supports(DieFace.Character),
                _ => true
            };
        }

        public void Clear()
        {
            DieId = "";
            ability = "";
            Face = null;
            Choices = System.Array.Empty<DieFace>();
        }
    }
}
