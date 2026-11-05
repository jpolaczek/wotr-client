using UnityEngine;

namespace Wotr.Client
{
    // UI-405 (TABLETOP_FIGURES_UX, "The lifted figure, drag and drop"): what DraftInput asks of a carried army or character.
    // Begin decides whether a press that crossed the drag threshold lifts the figure (the player's own, a legal source or
    // already chosen, nothing busy); otherwise the press stays a click. Carry follows the pointer and the object under it;
    // the drop reaches the input's drop callback as any drag does; Cancel ends the gesture without a step.
    public interface IFigureDrag
    {
        // The press began at `press` (panel coordinates) on `figure`, an Army or a Miniature token.
        bool Begin(DraftToken figure, Vector2 press);

        // The pointer (panel coordinates) and what lies under it (null over nothing that takes a drop).
        void Carry(Vector2 pointer, DraftToken target);

        // Esc, focus loss, a cancelled pointer or a changed scope: nothing is sent and the draft stays as it was.
        void Cancel();
    }
}
