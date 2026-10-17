using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-31: the menu window's demo page. The side cards are the only question: a click on one is the complete
    // choice and starts the demo against the CPU at once (UI-142).
    public sealed class DemoStartPage
    {
        public VisualElement Root { get; } = new VisualElement { name = "demo-start" };
        public event Action<Side> Start;
        public event Action Back;
        private readonly SideChoice sides = new SideChoice("demo");

        public DemoStartPage()
        {
            Root.AddToClassList("host-form");
            Root.Add(Note("A private game against a simple CPU, to learn the game at your own pace. Choose your side to begin."));
            sides.Chosen += side => Start?.Invoke(side);
            Root.Add(sides.Root);
            Root.Add(Note("A new demo replaces your previous one."));
            Root.Add(Footer());
        }

        private static Label Note(string text)
        {
            var note = new Label(text) { pickingMode = PickingMode.Ignore };
            note.AddToClassList("demo-note");
            return note;
        }

        private VisualElement Footer()
        {
            var footer = new VisualElement();
            footer.AddToClassList("host-footer");
            var back = BoardScreen.Button("Back", () => Back?.Invoke());
            back.name = "cancel-demo";
            back.AddToClassList("host-back");
            footer.Add(back);
            return footer;
        }
    }
}
