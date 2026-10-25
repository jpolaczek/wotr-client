using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §6.3): the one transient control of a presentation — a 36 pt round medallion with the
    // skip glyph at the bottom-right corner of the map area, 8 pt above the console and 8 pt left of the right column. It
    // exists only while a cue plays or waits, covers no HUD text and moves nothing; it has the normal tooltip, press
    // feedback and Enter/Space. Its click skips to the latest state on the next frame, so the click cannot reach the
    // decision the release opens.
    public sealed class SkipControl
    {
        public const string Name = "presentation-skip";
        public const float Size = 36;
        public const float Gap = 8;
        private readonly BoardScreen board;
        private readonly PresentationLayer layer;
        private readonly PresentationPlayback playback;
        public Button Button { get; }

        public SkipControl(BoardScreen board, PresentationLayer layer, PresentationPlayback playback, System.Action skip)
        {
            this.board = board;
            this.layer = layer;
            this.playback = playback;
            Button = IconButton.Create("skip", "Skip presentation", skip);
            Button.name = Name;
            Button.AddToClassList(Name);
        }

        public void Refresh()
        {
            if (!playback.Presenting)
            {
                Button.RemoveFromHierarchy();
                return;
            }
            if (Button.parent == null)
            {
                layer.Root.Add(Button);
                Appearance.Arrive(Button);
            }
            Place();
        }

        private void Place()
        {
            var right = Visible(board.Edges.Right) ? board.Edges.Right.worldBound.xMin : board.Root.worldBound.xMax;
            var bottom = board.Console.Root.parent.worldBound.yMin;
            var corner = layer.Root.WorldToLocal(new Vector2(right - Gap - Size, bottom - Gap - Size));
            Button.style.left = corner.x;
            Button.style.top = corner.y;
        }

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;
    }
}
