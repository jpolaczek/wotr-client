using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        public bool ArmyActive => view != null && ContextArmySelection.Active(view, draft, board.ActionDie);
        public Func<string, bool> FigureSelector { get; set; }

        public ActionContextInteraction(BoardScreen board, Action<Action> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            // UI-105: figures are chosen in the region's panel on the map; the Characters list only inspects.
            board.Hover.Panel.FigureChosen += ChooseFigure;
            board.Hover.Panel.SelectAllRequested += SelectAll;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.ActionDie.Sync(view, ResourcePresentation.CouncilPending(draft) ? "" : draft.Payload.DieId, draft.Payload.RulerTokenId);
            board.ActionDie.BindCost(view, draft);
            var eligible = ArmyActive ? ContextArmySelection.Eligible(view, board.ActionDie) : new HashSet<string>();
            var current = ArmyActive ? ArmyDraft.Current(draft) : null;
            var selected = current == null ? new HashSet<string>() : current.UnitIds.Concat(current.CharacterIds).ToHashSet();
            var moved = Marched(view);
            board.Hover.Panel.Eligible = id => !busy() && eligible.Contains(id);
            board.Hover.Panel.Selected = selected.Contains;
            board.Hover.Panel.Moved = moved.Contains;
            board.Hover.Panel.Offer = _ => "";
            board.Hover.Panel.RefreshSelection();
            board.Map.ShowMarched(MarchedForces(view, moved));
            var sources = ArmyActive ? ContextArmySelection.Sources(view, board.ActionDie) : Enumerable.Empty<ArmyOption>();
            board.Map.HighlightSources(sources, view.You);
            board.Map.SetGhost(ArmyActive ? ArmyGhost.Of(view, draft, board.ActionDie) : null);
        }

        // UI-127: figures that already marched in the current army action (the server's used ids).
        private static HashSet<string> Marched(PlayerView view) =>
            view.Decision?.Kind == DecisionKind.ActionSequence && view.ActionSequence is { Kind: CommandKind.MoveArmies } sequence
                ? sequence.UsedIds.ToHashSet() : new HashSet<string>();

        // A force is marked on the map only when every one of its figures has marched.
        private static IEnumerable<(string, Side, bool)> MarchedForces(PlayerView view, HashSet<string> moved)
        {
            if (moved.Count == 0)
                return Enumerable.Empty<(string, Side, bool)>();
            var figures = view.Units.Select(u => (Key: (u.Location, u.Side, u.Garrison), u.Id))
                .Concat(view.Characters.Select(c => (Key: (c.Location, c.Side, c.Garrison), c.Id)));
            return figures.GroupBy(f => f.Key).Where(g => g.All(f => moved.Contains(f.Id))).Select(g => g.Key).ToList();
        }

        // UI-127: an unsent selection that a click on a target would move.
        public bool HasArmySelection => ArmyActive && ArmyDraft.Current(draft) is { } move && move.To.Length == 0 &&
            move.UnitIds.Count + move.CharacterIds.Count > 0;

        // UI-127: a click on an own army selects every figure that can move. A selection already made from the
        // same force is kept, so a split survives another click on its marker. Returns false for a force that
        // is not a legal source, which only opens its region panel.
        public bool SelectArmy(DraftToken army)
        {
            if (!ArmyActive || busy() || army.Side.HasValue && army.Side != view.You)
                return false;
            if (ArmyDraft.Current(draft) is { } move && move.To.Length == 0 && move.From == army.Id &&
                move.UnitIds.Count + move.CharacterIds.Count > 0 && SameForce(move, army.Garrison))
                return true;
            if (!ContextArmySelection.Sources(view, board.ActionDie).Any(a => a.RegionId == army.Id && a.Garrison == army.Garrison))
                return false;
            var selected = false;
            edit(() => draft.Gesture(() =>
                selected = ContextArmySelection.SelectAll(draft, view, board.ActionDie, army.Id, army.Garrison)));
            return selected;
        }

        // UI-140: the region panel's toggle selects every figure of the force that can move, or, once all of them
        // are selected, leaves them all behind. One gesture is one undo step and sends nothing.
        public bool ToggleAll(string region, bool garrison)
        {
            if (!ArmyActive || busy())
                return false;
            if (AllSelected(region, garrison))
            {
                edit(() => draft.Gesture(() => draft.Change(p =>
                {
                    p.Moves[0].UnitIds.Clear();
                    p.Moves[0].CharacterIds.Clear();
                })));
                return true;
            }
            var selected = false;
            edit(() => draft.Gesture(() =>
                selected = ContextArmySelection.SelectAll(draft, view, board.ActionDie, region, garrison)));
            return selected;
        }

        private bool AllSelected(string region, bool garrison)
        {
            if (!(ArmyDraft.Current(draft) is { } move) || move.From != region || move.To.Length > 0)
                return false;
            var eligible = ContextArmySelection.Sources(view, board.ActionDie)
                .Where(a => a.RegionId == region && a.Garrison == garrison)
                .SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).Distinct().ToList();
            return eligible.Count > 0 && eligible.All(id => move.UnitIds.Contains(id) || move.CharacterIds.Contains(id));
        }

        private bool SameForce(ArmyMoveChoice move, bool garrison) =>
            move.UnitIds.All(id => view.Units.First(u => u.Id == id).Garrison == garrison) &&
            move.CharacterIds.All(id => view.Characters.First(c => c.Id == id).Garrison == garrison);

        private void ChooseFigure(string id) => SelectFigure(id);

        private void SelectAll(string region, bool garrison) => ToggleAll(region, garrison);

        // UI-105: figures chosen in one region move together. An army selection keeps a Nazgûl or a character the
        // player adds to it, and a character group chosen first joins the army as soon as a soldier is added.
        public bool SelectFigure(string id)
        {
            if (view == null || busy())
                return false;
            var army = ArmyActive ? ContextArmySelection.Eligible(view, board.ActionDie) : new HashSet<string>();
            if (army.Contains(id) && Gathering(id))
            {
                edit(() => draft.Gesture(() => ContextArmySelection.Select(draft, view, board.ActionDie, id)));
                return true;
            }
            if (FigureSelector?.Invoke(id) == true)
                return true;
            if (!army.Contains(id))
                return false;
            var group = TravelActions.Selected(draft).Where(member => member != id && army.Contains(member) &&
                Location(member) == Location(id)).ToList();
            edit(() => draft.Gesture(() =>
            {
                ContextArmySelection.Select(draft, view, board.ActionDie, id);
                foreach (var member in group)
                    ContextArmySelection.Select(draft, view, board.ActionDie, member);
            }));
            return true;
        }

        private bool Gathering(string id) => ArmyDraft.Current(draft) is { } move && move.To.Length == 0 &&
            move.UnitIds.Count + move.CharacterIds.Count > 0 && move.From == Location(id);

        private string Location(string id) => view.Units.FirstOrDefault(u => u.Id == id)?.Location ??
            view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? "";

        public bool Target(string region) => ArmyActive && !busy() &&
            ContextArmySelection.Target(draft, view, board.ActionDie, region);

        public void Dispose()
        {
            board.Hover.Panel.FigureChosen -= ChooseFigure;
            board.Hover.Panel.SelectAllRequested -= SelectAll;
            board.Hover.Panel.Eligible = _ => false;
            board.Hover.Panel.Selected = _ => false;
            board.Hover.Panel.Moved = _ => false;
            board.Hover.Panel.Offer = _ => "";
            board.Map.SetGhost(null);
            board.Map.ShowMarched(Enumerable.Empty<(string, Side, bool)>());
            board.Hover.Close();
        }
    }
}
