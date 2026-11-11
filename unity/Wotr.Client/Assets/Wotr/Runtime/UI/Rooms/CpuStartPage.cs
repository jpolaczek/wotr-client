using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Saved PvE uses the same waiting room and explicit Ready/Start flow as a human game.
    public sealed class CpuStartPage
    {
        public VisualElement Root { get; } = new VisualElement { name = "cpu-start" };
        public event Action<Side> Start;
        public event Action Back;
        private readonly SideChoice sides = new SideChoice("cpu");

        public CpuStartPage()
        {
            Root.AddToClassList("host-form");
            Root.Add(Note("Choose your side. Ready and start in the waiting room."));
            sides.Chosen += side => Start?.Invoke(side);
            Root.Add(sides.Root);
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
            back.name = "cancel-cpu";
            back.AddToClassList("host-back");
            footer.Add(back);
            return footer;
        }
    }
}
