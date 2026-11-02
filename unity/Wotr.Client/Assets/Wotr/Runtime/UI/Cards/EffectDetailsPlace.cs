using UnityEngine;

namespace Wotr.Client
{
    // UI-424: a source's details (state, expiry, the card's text) open on their own card beside the Effects panel, so the
    // panel keeps its width and its grid stays where it is. The card stands level with the panel's top and moves up only as
    // far as it must to end above the bottom row, never above the top of the board.
    public static class EffectDetailsPlace
    {
        public const float Margin = 8;

        // The card's top relative to the panel's top; panelTop and stageHeight are in the stage's coordinates.
        public static float Offset(float panelTop, float cardHeight, float stageHeight)
        {
            var lowest = stageHeight - Margin - cardHeight;
            var top = Mathf.Max(Margin, Mathf.Min(panelTop, lowest));
            return Mathf.Round(top - panelTop);
        }
    }
}
