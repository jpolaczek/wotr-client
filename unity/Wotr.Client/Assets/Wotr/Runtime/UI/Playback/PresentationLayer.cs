using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-305: the board's layer for presentation marks (traces, die copies) and the Skip target: above the map, the top
    // bar and the console, below the ring picker, the game end and the menu. It ignores the pointer; only the Skip
    // target inside it takes clicks.
    public sealed class PresentationLayer
    {
        public VisualElement Root { get; } = new VisualElement { name = "presentation-layer", pickingMode = PickingMode.Ignore };

        public PresentationLayer(BoardScreen board)
        {
            Root.AddToClassList("presentation-layer");
            var bottom = board.Console.Root.parent;
            board.Root.Insert(board.Root.IndexOf(bottom) + 1, Root);
        }

        public Rect Local(Rect world) => new Rect(Root.WorldToLocal(world.position), world.size);
    }
}
