using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-30: the two sides as cards in the menu's frame (frames/entry), each with its emblem, name and how it wins,
    // like the seats of the waiting room. A click chooses the side; the chosen card is marked. MENU-31 uses the same
    // cards, where the click itself starts the demo.
    public sealed class SideChoice
    {
        public const string SelectedClass = "selected";
        public VisualElement Root { get; } = new VisualElement();
        public Side Selected { get; private set; } = Side.FreePeoples;
        public event Action<Side> Chosen;
        private readonly Button free;
        private readonly Button shadow;

        public SideChoice(string prefix)
        {
            Root.name = prefix + "-sides";
            Root.AddToClassList("side-choice");
            free = Card(prefix, Side.FreePeoples, "Carry the Ring to Mount Doom, or win the war in the West.");
            shadow = Card(prefix, Side.Shadow, "Corrupt the Ring-bearer, or conquer the Free Peoples' cities.");
            Root.Add(free);
            Root.Add(shadow);
            Select(Selected);
        }

        public void Select(Side side)
        {
            Selected = side;
            free.EnableInClassList(SelectedClass, side == Side.FreePeoples);
            shadow.EnableInClassList(SelectedClass, side == Side.Shadow);
        }

        private Button Card(string prefix, Side side, string detail)
        {
            var key = side == Side.FreePeoples ? "fp" : "shadow";
            var card = BoardScreen.Button("", () => Choose(side));
            card.name = prefix + "-" + key;
            card.AddToClassList("side-card");
            var heading = new VisualElement { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("side-card-heading");
            var emblem = new VisualElement { pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("menu-row-emblem");
            emblem.AddToClassList(side == Side.FreePeoples ? "side-free" : "side-shadow");
            heading.Add(emblem);
            var name = new Label(side == Side.FreePeoples ? "Free Peoples" : "Shadow") { pickingMode = PickingMode.Ignore };
            name.AddToClassList("side-card-name");
            heading.Add(name);
            card.Add(heading);
            var text = new Label(detail) { pickingMode = PickingMode.Ignore };
            text.AddToClassList("side-card-detail");
            card.Add(text);
            return card;
        }

        private void Choose(Side side)
        {
            Select(side);
            Chosen?.Invoke(side);
        }
    }
}
