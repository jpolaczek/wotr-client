using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-454 (owner, 2026-11-03: "there should also be the option to drag the unit on exactly the same principle as moving it
    // on the map"; this supersedes UI-421's decision 2, "the reserve keeps its gestures", for the Reserve's figures): a press
    // on a Reserve figure that crosses the drag threshold takes it up. The draft is kept as a checkpoint and the figure is
    // armed as its click arms it, never placed or sent (ReinforcementsInteraction.Arm), so the map lights exactly the
    // settlements it may go to; when nothing arms it (a figure only a click chooses) the draft is put back and the press stays
    // the UI-393 drag. Over the Reserve panel and the HUD it travels as the slot's picture; over the map as the miniature it
    // will be, the region under it lit as a carried figure's target (UI-405), the landing mark with "Recruit <figure>" on the
    // place it would stand on, and the map pans at its edge. A drop on a lit target calls exactly what that region's click
    // calls (TurnInteraction.ChooseRegion), so the payload is the click's, and its flight goes on from where it was dropped;
    // any other release, Esc, focus loss or a new revision puts the draft back and the figure glides back to its slot.
    public sealed class ReserveCarryInteraction : IFigureDrag, IDisposable
    {
        private readonly BoardScreen board;
        private readonly TurnDraft draft;
        private readonly Func<PlayerView> view;
        private readonly Func<bool> busy;
        // Arms a reserve figure (by id) without placing it; false when nothing arms it.
        private readonly Func<string, bool> arm;
        // The target region's click.
        private readonly Action<string> choose;
        // Puts a checkpoint of the draft back, saves it and shows it.
        private readonly Action<TurnDraft> restore;
        private readonly RecruitFlightInteraction flights;
        private TurnDraft before;
        private string unit = "";
        private Vector2 pointer;

        public ReserveCarryInteraction(BoardScreen board, TurnDraft draft, Func<PlayerView> view, Func<bool> busy,
            Func<string, bool> arm, Action<string> choose, Action<TurnDraft> restore, RecruitFlightInteraction flights)
        {
            this.board = board;
            this.draft = draft;
            this.view = view;
            this.busy = busy;
            this.arm = arm;
            this.choose = choose;
            this.restore = restore;
            this.flights = flights;
            board.Map.EdgePan.DragPanned += Panned;
        }

        public bool Carrying => before != null;

        public bool Begin(DraftToken figure, Vector2 press)
        {
            var current = view();
            var recruit = current?.Units.FirstOrDefault(u => u.Id == figure?.Id);
            var art = board.Reinforcements.Art(figure?.Id ?? "");
            if (Carrying || figure?.Object != DraftObject.Reserve || current?.Decision == null || busy() || recruit == null ||
                art?.sprite == null)
                return false;
            var checkpoint = draft.Checkpoint();
            if (!arm(figure.Id) || !board.Map.DraftOverlay.Targets.Any())
            {
                restore(checkpoint);
                return false;
            }
            before = checkpoint;
            unit = figure.Id;
            pointer = press;
            board.Map.RecruitCarry.Take(new ArmyUnit(recruit.Id, recruit.Nation, recruit.Kind), current.You,
                ReinforcementPlans.Active(current, draft), art.sprite, art.worldBound.size, Local(press));
            return true;
        }

        public void Carry(Vector2 position, DraftToken target)
        {
            if (!Carrying)
                return;
            pointer = position;
            var overMap = OverMap(position);
            board.Map.RecruitCarry.Follow(Local(position), overMap);
            board.Map.SetDragTarget(overMap ? Region(target) : "");
            board.Map.EdgePan.SampleDrag(position);
        }

        // An edge pan moved the map under the resting pointer: the figure stays under it, the region beneath changed.
        private void Panned()
        {
            if (Carrying && OverMap(pointer))
                board.Map.SetDragTarget(Region(DraftInput.At(board.Root.panel, pointer)));
        }

        // TurnInteraction.Drop's first branch: the carried figure is dropped on `target`.
        public bool Drop(DraftToken target)
        {
            if (!Carrying)
                return false;
            var checkpoint = before;
            var recruit = unit;
            var overMap = OverMap(pointer);
            var region = overMap ? Region(target) : "";
            End();
            var start = board.Map.RecruitCarry.Release();
            if (Legal(region) && start is ReserveFlightStart from && Place(recruit, region, from))
            {
                board.Map.RecruitCarry.Remove();
                return true;
            }
            restore(checkpoint);
            GoBack(recruit);
            return false;
        }

        // The region's click, its recruit's flight starting where the carried miniature shows. It placed the figure — or
        // sent it, the draft already moving on — when the figure is placed or no longer armed alone.
        private bool Place(string recruit, string region, ReserveFlightStart from)
        {
            flights.Carried(recruit, from);
            choose(region);
            flights.EndCarried();
            return draft.ReserveId != recruit || draft.Payload.Placements.Any(placement => placement.UnitId == recruit);
        }

        // A lit target while nothing is busy.
        private bool Legal(string region) => !busy() && region.Length > 0 && board.Map.DraftOverlay.Targets.Contains(region);

        public void Cancel()
        {
            if (!Carrying)
                return;
            var checkpoint = before;
            var recruit = unit;
            End();
            board.Map.RecruitCarry.Release();
            restore(checkpoint);
            GoBack(recruit);
        }

        // The figure glides back to its slot (the Reserve tab while the panel does not show it) and goes.
        private void GoBack(string recruit)
        {
            var art = board.Reinforcements.Art(recruit);
            var slot = art != null && art.worldBound.width > 0 ? art.worldBound.center : board.Reinforcements.Tab.worldBound.center;
            board.Map.RecruitCarry.Return(Local(slot), !board.ReducedMotion());
        }

        private void End()
        {
            before = null;
            unit = "";
            board.Map.SetDragTarget(null);
            board.Map.EdgePan.EndDrag();
        }

        // Whether a panel point lies on the map — its terrain or a figure on it — rather than on the HUD over it.
        private bool OverMap(Vector2 position)
        {
            var picked = board.Root.panel?.Pick(position);
            return picked == board.Map || picked != null && board.Map.Contains(picked);
        }

        private Vector2 Local(Vector2 position) => board.Map.WorldToLocal(position);

        // The region a drop lands in: the terrain's, or the one the map object under the pointer stands in.
        private static string Region(DraftToken target) => target?.StandsIn ?? "";

        // The board goes away mid-drag: the draft is put back as it was before the press, with nothing more shown.
        public void Dispose()
        {
            if (Carrying)
            {
                draft.Rewind(before);
                End();
                board.Map.RecruitCarry.Remove();
            }
            board.Map.EdgePan.DragPanned -= Panned;
        }
    }
}
