using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // OBS-03: marks an observer's board under the notices — "Observing" and the side shown — with both sides as fixed
    // choices one click away. Root is a lane centred over the map beside the right column; only the bar takes the
    // pointer. A choice changes what is shown, never what the observer may do.
    public sealed class ViewingBar
    {
        public VisualElement Root { get; } = new VisualElement { name = "viewing-lane", pickingMode = PickingMode.Ignore };
        public VisualElement Bar { get; } = new VisualElement { name = "viewing-bar" };
        public event Action<Side> PerspectiveChosen;
        public Side Perspective { get; private set; } = Side.FreePeoples;
        private readonly Label title = new Label { name = "viewing-title", pickingMode = PickingMode.Ignore };
        private readonly Button freePeoples;
        private readonly Button shadow;

        public ViewingBar()
        {
            Root.AddToClassList("viewing-lane");
            Root.style.display = DisplayStyle.None;
            Bar.AddToClassList("viewing-bar");
            Bar.AddToClassList("map-edge-control");
            Root.Add(Bar);
            title.AddToClassList("viewing-title");
            Bar.Add(title);
            freePeoples = Choice("viewing-free", "Free Peoples", Side.FreePeoples);
            shadow = Choice("viewing-shadow", "Shadow", Side.Shadow);
        }

        public bool Visible => Root.style.display == DisplayStyle.Flex;

        public void Show(Side perspective)
        {
            Perspective = perspective;
            title.text = "OBSERVING · VIEWING";
            title.tooltip = "You watch this game. Choose whose hand and knowledge the board shows; nothing you do changes the game.";
            freePeoples.EnableInClassList("selected", perspective == Side.FreePeoples);
            shadow.EnableInClassList("selected", perspective == Side.Shadow);
            Root.style.display = DisplayStyle.Flex;
        }

        private Button Choice(string name, string text, Side side)
        {
            var button = BoardScreen.Button(text, () => Choose(side));
            button.name = name;
            button.tooltip = "Show the game as the " + text + " see it: their hand, history and reports.";
            button.AddToClassList("viewing-choice");
            Bar.Add(button);
            return button;
        }

        // A click on a side, or the same from code (tests, keys).
        public void Choose(Side side)
        {
            if (side == Perspective)
                return;
            Show(side);
            PerspectiveChosen?.Invoke(side);
        }
    }
}
