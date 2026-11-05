using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionDieContext
    {
        private static readonly DieFace[] Combined = { DieFace.Army, DieFace.Muster };
        public string DieId { get; private set; } = "";
        public DieFace? Face { get; private set; }
        public IReadOnlyList<DieFace> Choices { get; private set; } = System.Array.Empty<DieFace>();
        private string ability = "";
        public string AbilityId => ability;
        // UI-461 (owner, 2026-11-03): the chosen die shows Will of the West; until the console's face menu turns it into
        // another result (TurnDraft.WillFace) it is Will of the West itself and lights only those uses (WillOfTheWestUses).
        public bool Will { get; private set; }
        public DieFace? WillFace => Will && Face != DieFace.WillOfTheWest ? Face : null;
        // UI-459 (owner, 2026-11-03): the die's every movement cost names one ability (the Balrog die's Use Balrog), so the
        // die is that ability: the map lights its moves with no icon to choose first.
        public bool Implied { get; private set; }

        public static ActionDieContext Of(PlayerView view, TurnDraft draft)
        {
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId, draft.Payload.RulerTokenId, draft.WillFace);
            return context;
        }

        // UI-389: a chosen Ruler token turns the Ruler die into the token's face for its actions (the server's
        // RulerTokenView.Face), so what the die lights is judged by that face, not by the face the die shows.
        public void Sync(PlayerView view, string id, string rulerToken = "", DieFace? willFace = null)
        {
            Will = false;
            Implied = false;
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
            var token = string.IsNullOrEmpty(rulerToken) || die.Source != "ruler" ? null : view.RulerTokens.FirstOrDefault(t => t.Id == rulerToken);
            var face = token?.Face ?? die.Face;
            Choices = face == DieFace.ArmyMuster ? Combined : System.Array.Empty<DieFace>();
            Will = face == DieFace.WillOfTheWest;
            Face = Will && willFace.HasValue ? willFace : face;
            if (face == DieFace.UseBalrog)
                Imply(ImpliedAbility(view, id));
        }

        private void Imply(string implied)
        {
            if (implied.Length == 0)
                return;
            ability = implied;
            Face = DieFace.Character;
            Implied = true;
        }

        // UI-459: the one ability that every movement cost the server offers this die names ("" when there is none or
        // more than one).
        public static string ImpliedAbility(PlayerView view, string id)
        {
            var abilities = view.Options.Where(o => o.Enabled).SelectMany(o => o.MovementCosts)
                .Where(c => c.DieId == id && c.AbilityId.Length > 0).Select(c => c.AbilityId).Distinct().ToList();
            return abilities.Count == 1 ? abilities[0] : "";
        }

        // Whether the draft's ability is this die's: the same, or none yet while the die implies its own (UI-459).
        public bool Matches(string draftAbility) => draftAbility == ability || Implied && draftAbility.Length == 0;

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
                // UI-461: a started action of a Will die names the result it is used as, so a reloaded draft needs no menu.
                if (intended.HasValue && (Supports(intended.Value) || Will && WillOfTheWestUses.Faces.Contains(intended.Value)))
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
                _ => !Will || WillOfTheWestUses.Pays(option.Kind, WillFace)
            };
        }

        // UI-461: whether this die pays a character's entry or ability: the server lists the die, and a Will die is used
        // as the result the action takes (as itself until the face menu turns it).
        public bool Pays(CommandKind kind, CharacterActionOption action) => DieId.Length > 0 && action.Dice.Contains(DieId) &&
            Takes(kind, action);

        // Whether the die's result takes the action the server lists it for: any die but a Will die, which takes what its
        // chosen result (or Will of the West itself) does.
        public bool Takes(CommandKind kind, CharacterActionOption action) => !Will || WillOfTheWestUses.Pays(kind, action, WillFace);

        public void Clear()
        {
            DieId = "";
            ability = "";
            Face = null;
            Will = false;
            Implied = false;
            Choices = System.Array.Empty<DieFace>();
        }
    }
}
