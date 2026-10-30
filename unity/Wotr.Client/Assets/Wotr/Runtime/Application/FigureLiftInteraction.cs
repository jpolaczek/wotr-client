using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-405 (TABLETOP_FIGURES_UX, "The lifted figure, drag and drop"; owner decision 2): the player's own army or character
    // is picked up and carried to a region, and its drop is that region's click. A press that crosses the drag threshold on
    // a figure that is a legal source (or already chosen) lifts it: the draft is kept, the figure's click makes its
    // selection without opening a panel, and the raised pieces of the hand (UI-403) follow the pointer. When the click
    // chooses nothing to carry, or nothing it could carry to, the draft is put back and the press stays a click. A drop on
    // a lit target other than the source calls the target's click (TurnInteraction.ChooseRegion), so the step is the
    // click's own: sent, the dropped figure stays where it is and its flight leaves from there (UI-404's hand-off);
    // planned, it dissolves there into the planned figure; otherwise the draft is put back and the figure glides home.
    // Esc, focus loss, a cancelled pointer and a new revision cancel the same way. Clicks remain the whole path (R16).
    public sealed class FigureLiftInteraction : IFigureDrag, IDisposable
    {
        private readonly BoardScreen board;
        private readonly TurnDraft draft;
        private readonly Func<PlayerView> view;
        private readonly Func<bool> busy;
        // The figure's click, made quietly; false when it would have sent a step (then it lifts nothing).
        private readonly Func<DraftToken, bool> select;
        // The target region's click.
        private readonly Action<string> choose;
        // Puts a checkpoint of the draft back, saves it and shows it.
        private readonly Action<TurnDraft> restore;
        private DraftToken source;
        private TurnDraft before;
        private Vector2 pointer;

        public FigureLiftInteraction(BoardScreen board, TurnDraft draft, Func<PlayerView> view, Func<bool> busy,
            Func<DraftToken, bool> select, Action<string> choose, Action<TurnDraft> restore)
        {
            this.board = board;
            this.draft = draft;
            this.view = view;
            this.busy = busy;
            this.select = select;
            this.choose = choose;
            this.restore = restore;
            board.Map.EdgePan.DragPanned += Panned;
        }

        public bool Carrying => source != null;

        public bool Begin(DraftToken figure, Vector2 press)
        {
            if (Carrying || !Liftable(figure))
                return false;
            var checkpoint = draft.Checkpoint();
            var chosen = board.Map.Lifts(figure);
            if (!TakenUp(figure, chosen, press))
            {
                if (!chosen)
                    restore(checkpoint);
                return false;
            }
            source = figure;
            before = checkpoint;
            pointer = press;
            return true;
        }

        // Raised for the unsent move — chosen before, or now by its own quiet click — with somewhere to go, and in hand.
        private bool TakenUp(DraftToken figure, bool chosen, Vector2 press) =>
            (chosen || select(figure)) && board.Map.Lifts(figure) && HasTarget(figure) &&
            board.Map.GrabFigures(board.Map.WorldToLocal(press));

        // The player's own figure, while a decision waits and nothing is busy; a lit king's force chooses the king instead.
        private bool Liftable(DraftToken figure)
        {
            var current = view();
            return current?.Decision != null && !busy() && figure.Side == current.You && board.Map.LitSovereign(figure) == null;
        }

        // Somewhere to carry it: a lit target other than the region it stands in.
        private bool HasTarget(DraftToken figure) => board.Map.DraftOverlay.Targets.Any(region => region != figure.RegionId);

        public void Carry(Vector2 position, DraftToken target)
        {
            if (!Carrying)
                return;
            pointer = position;
            board.Map.CarryFigures(board.Map.WorldToLocal(position));
            board.Map.SetDragTarget(Region(target));
            board.Map.EdgePan.SampleDrag(position);
        }

        // An edge pan moved the map under the resting pointer: the figure stays under it, the region beneath changed.
        private void Panned()
        {
            if (Carrying)
                board.Map.SetDragTarget(Region(DraftInput.At(board.Root.panel, pointer)));
        }

        // TurnInteraction.Drop's first branch: the carried figure is dropped on `target`.
        public bool Drop(DraftToken target)
        {
            if (!Carrying)
                return false;
            var figure = source;
            var checkpoint = before;
            End();
            var carried = board.Map.DropFigures();
            var region = Region(target);
            if (Legal(figure, region))
                choose(region);
            var placed = !carried.Overlaps(LiftedFigures.Selection(view(), draft));
            if (!placed)
                restore(checkpoint);
            board.Map.HomeFigures();
            return placed;
        }

        // A lit target the figure does not stand in, while nothing is busy.
        private bool Legal(DraftToken figure, string region) => !busy() && region.Length > 0 && region != figure.RegionId &&
            board.Map.DraftOverlay.Targets.Contains(region);

        public void Cancel()
        {
            if (!Carrying)
                return;
            var checkpoint = before;
            End();
            board.Map.DropFigures();
            restore(checkpoint);
            board.Map.HomeFigures();
        }

        private void End()
        {
            source = null;
            before = null;
            board.Map.SetDragTarget(null);
            board.Map.EdgePan.EndDrag();
        }

        // The region a drop lands in: the terrain's, an army marker's or a character miniature's.
        private static string Region(DraftToken target) =>
            target?.Object is DraftObject.Region or DraftObject.Army or DraftObject.Miniature ? target.RegionId : "";

        // The board goes away mid-drag: the draft is put back as it was before the press, with nothing more shown.
        public void Dispose()
        {
            if (Carrying)
            {
                draft.Rewind(before);
                End();
            }
            board.Map.EdgePan.DragPanned -= Panned;
        }
    }
}
