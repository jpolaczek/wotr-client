using UnityEngine;

namespace Wotr.Client
{
    // UI-454 (owner, 2026-11-03): the one IFigureDrag the board's input asks, for both kinds of carried figure — a Reserve
    // figure (ReserveCarryInteraction) and the map's armies and characters (FigureLiftInteraction, UI-405). The press's own
    // object decides which carries it; the moves and a cancellation go to the one that took it up.
    public sealed class FigureDragRoute : IFigureDrag
    {
        private readonly IFigureDrag map;
        private readonly IFigureDrag reserve;
        private IFigureDrag active;

        public FigureDragRoute(IFigureDrag map, IFigureDrag reserve)
        {
            this.map = map;
            this.reserve = reserve;
        }

        public bool Begin(DraftToken figure, Vector2 press)
        {
            var drag = figure?.Object == DraftObject.Reserve ? reserve : map;
            active = drag.Begin(figure, press) ? drag : null;
            return active != null;
        }

        public void Carry(Vector2 pointer, DraftToken target) => active?.Carry(pointer, target);

        public void Cancel()
        {
            var drag = active;
            active = null;
            drag?.Cancel();
        }
    }
}
