using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum DraftObject { Die, Ring, Card, Action, Deck, Tray, Army, Region, Reserve, RulerToken, Figure, Nation, PoliticalSlot, Fellowship, FellowshipPortrait, Sovereign, CouncilPortrait, HuntEye, HuntTile, HuntResponse, Character }

    public sealed class DraftToken
    {
        public DraftObject Object { get; }
        public string Id { get; }
        public CommandKind? Action { get; }
        public Side? Side { get; }
        public bool Garrison { get; }
        public DraftToken(DraftObject item, string id = "", CommandKind? action = null, Side? side = null, bool garrison = false)
        {
            Object = item;
            Id = id;
            Action = action;
            Side = side;
            Garrison = garrison;
        }
    }

    public static class DraftGesture
    {
        public static string EquivalentDie(PlayerView view, ActionOption option)
        {
            var dice = view.Dice.Where(d => option.EligibleDice.Contains(d.Id)).ToList();
            if (dice.Count == 0 || dice.Count != option.EligibleDice.Count)
                return "";
            if (option.MovementCosts.Any(c => c.AbilityId.Length > 0) || option.DrawCosts.Any(c => c.AbilityId.Length > 0))
                return "";
            var first = dice[0];
            return dice.All(d => d.Source == "standard" && d.Owner == view.You &&
                d.Location == DieLocation.Available && d.Face == first.Face &&
                d.Face != DieFace.WillOfTheWest && d.HuntOrigin == first.HuntOrigin)
                ? first.Id : "";
        }

        public static bool Choose(TurnDraft draft, PlayerView view, CommandKind kind)
        {
            var option = DraftOptions.For(view, draft, kind);
            if (option == null || !option.Enabled)
                return false;
            var token = draft.Payload.RulerTokenId;
            var die = option.EligibleDice.Contains(draft.Payload.DieId)
                ? draft.Payload.DieId : EquivalentDie(view, option);
            draft.Choose(kind, die);
            ExpansionDraft.Initialize(draft, view, option, token);
            CardDrawDraft.MatchCost(draft, option);
            if (kind == CommandKind.DiscardProtection && option.Targets.Count == 1)
                draft.Payload.CardId = option.Targets[0];
            if (option.Travel.FirstOrDefault(t => t.RequiredGroup.Count > 0) is { } forced)
                draft.Payload.Ids.AddRange(forced.RequiredGroup);
            if (kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
                ArmyDraft.MatchCost(draft, option);
            return true;
        }

        public static bool Drop(TurnDraft draft, PlayerView view, DraftToken source, DraftToken target)
        {
            if (source == null || target == null || view.Decision == null)
                return false;
            // UI-270: a companion's portrait on the Fellowship panel dropped on its homeland.
            if (source.Object == DraftObject.CouncilPortrait && target.Object == DraftObject.Region && draft.Kind == CommandKind.ChooseCouncilHomelands)
            {
                var choice = DraftOptions.For(view, draft)?.CharacterActions.FirstOrDefault(c => c.CharacterId == source.Id);
                if (choice?.Targets.Contains(target.Id) != true)
                    return false;
                CouncilDraft.Homeland(draft, source.Id, target.Id);
                return true;
            }
            // UI-393: a reserve figure dropped on a settlement's army marker is dropped on that settlement.
            if (source.Object == DraftObject.Reserve && target.Object == DraftObject.Army)
                target = new DraftToken(DraftObject.Region, target.Id);
            if (source.Object == DraftObject.RulerToken && target.Object == DraftObject.Tray && DraftOptions.CanSelectToken(view, source.Id))
            {
                DraftOptions.SelectToken(draft, view, source.Id);
                return true;
            }
            if (source.Object == DraftObject.Reserve && target.Object == DraftObject.Region && CharacterDraft.UsesCharacters(draft.Kind))
            {
                var character = CharacterDraft.Selected(draft, DraftOptions.For(view, draft));
                return character != null && CharacterRecruitDraft.Drop(draft, character, source.Id, target.Id);
            }
            if (source.Object == DraftObject.Card && draft.Kind == CommandKind.DiscardProtection && target.Object == DraftObject.Tray)
            {
                var removal = view.Options.First(o => o.Kind == draft.Kind);
                if (!removal.EligibleIds.Contains(source.Id))
                    return false;
                CardDraft.Choose(draft, removal, source.Id);
                return true;
            }
            if (source.Object == DraftObject.Card && view.Decision.Kind == DecisionKind.CombatCard && target.Object == DraftObject.Tray)
                return BattleDraft.SelectCard(draft, view, source.Id);
            if (source.Object == DraftObject.Card && DraftOptions.All(view, draft).Any(o => o.Kind == CommandKind.PlayEvent) &&
                (target.Object == DraftObject.Tray || target.Action == CommandKind.PlayEvent))
                return CardDraft.Select(draft, view, source.Id);
            if (source.Object == DraftObject.Reserve && target.Object == DraftObject.Region && EventRecruitDraft.Option(view) != null)
                return EventRecruitDraft.Drop(draft, EventRecruitDraft.Option(view), source.Id, target.Id);
            // UI-128: armies move by clicks only (UI-105); a drop places a reserve figure for recruitment.
            if (source.Object == DraftObject.Army)
                return false;
            if (source.Object == DraftObject.Reserve)
                return BoardDraft.Drop(draft, view, source, target);
            var kind = target.Action ?? draft.Kind;
            if (source.Object == DraftObject.Ring && target.Object == DraftObject.Die)
                kind = draft.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardLore ? draft.Kind : CommandKind.UseRing;
            var option = DraftOptions.For(view, draft, kind);
            if (option == null)
                return false;
            if (source.Object == DraftObject.Ring && target.Object == DraftObject.Die &&
                option.EligibleIds.Contains(source.Id) && option.EligibleDice.Contains(target.Id))
            {
                Choose(draft, view, option.Kind);
                draft.Change(p => { p.RingId = source.Id; p.DieId = target.Id; });
                return true;
            }
            if (target.Object != DraftObject.Action && target.Object != DraftObject.Deck && target.Object != DraftObject.Tray)
                return false;
            if (kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardLore && source.Object == DraftObject.Ring && option.EligibleIds.Contains(source.Id))
            {
                if (draft.Kind != kind)
                    Choose(draft, view, option.Kind);
                draft.Change(p => p.RingId = source.Id);
                return true;
            }
            if (kind == CommandKind.ChooseEffect && source.Object == DraftObject.Die && view.Decision.Presentation is "ActionDie" or "DieAndResult" && option.EligibleIds.Contains(source.Id))
            {
                Choose(draft, view, option.Kind);
                CardDraft.Choose(draft, option, source.Id);
                return true;
            }
            if (source.Object == DraftObject.Die && option.EligibleDice.Contains(source.Id))
            {
                if (target.Object == DraftObject.Deck && !option.Targets.Contains(target.Id))
                    return false;
                if (draft.Kind != kind)
                    Choose(draft, view, option.Kind);
                draft.Change(p => { p.DieId = source.Id; if (target.Object == DraftObject.Deck) p.Deck = target.Id; });
                CardDrawDraft.MatchCost(draft, option);
                if (option.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
                    ArmyDraft.MatchCost(draft, option);
                return true;
            }
            var selection = kind == CommandKind.AllocateHunt && source.Object == DraftObject.Die ||
                kind == CommandKind.DiscardCards && source.Object == DraftObject.Card;
            if (!selection || !option.EligibleIds.Contains(source.Id))
                return false;
            if (draft.Kind != kind)
                Choose(draft, view, option.Kind);
            draft.Toggle(source.Id);
            return true;
        }
    }
}
