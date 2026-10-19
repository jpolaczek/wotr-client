using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-30: the Host a game page of the menu window, in the waiting room's style and with a fixed place for every
    // control: the game's name, the two sides as cards, the visibility as two segment buttons, the rules line, and a
    // footer with Back on the left and Create game on the right. The window's header carries the title.
    public sealed class HostGameForm
    {
        public VisualElement Root { get; } = new VisualElement { name = "host-game-form" };
        public event Action<CreateRoomRequest> Create;
        public event Action Cancel;
        private readonly TextField name = new TextField { name = "room-name" };
        private readonly SideChoice sides = new SideChoice("host");
        private readonly Label status = new Label { name = "host-status" };
        private readonly Label visibilityNote = new Label { name = "host-visibility-note" };
        private Button publicButton;
        private Button privateButton;
        private RoomVisibility visibility = RoomVisibility.Public;

        public HostGameForm(string username)
        {
            Root.AddToClassList("host-form");
            name.value = username + "'s game";
            name.maxLength = 64;
            Root.Add(Field("Game name", name));
            Root.Add(Heading("Choose your side"));
            Root.Add(sides.Root);
            Root.Add(Field("Visibility", Visibility()));
            visibilityNote.AddToClassList("host-note");
            Root.Add(visibilityNote);
            // UI-263: the rules are chosen in the waiting room, after the table exists.
            var rules = new Label("Two players · Play at your own pace · Choose expansions in the waiting room")
            {
                name = "host-rules-note", pickingMode = PickingMode.Ignore
            };
            rules.AddToClassList("host-note");
            Root.Add(rules);
            Root.Add(Footer());
            SetVisibility(RoomVisibility.Public);
        }

        public Side Side => sides.Selected;

        public void Choose(Side side) => sides.Select(side);

        public void Busy(bool value) => Root.SetEnabled(!value);

        public void Submit()
        {
            if (!Root.enabledSelf)
                return;
            if (!RoomInput.ValidName(name.value))
            {
                status.text = "Use 1–64 characters without control characters.";
                name.Focus();
                return;
            }
            status.text = "";
            Create?.Invoke(new CreateRoomRequest { Name = name.value.Trim(), Side = sides.Selected, Visibility = visibility });
        }

        private static VisualElement Field(string caption, VisualElement control)
        {
            var row = new VisualElement();
            row.AddToClassList("host-field");
            var label = new Label(caption) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("host-label");
            row.Add(label);
            control.AddToClassList("host-control");
            row.Add(control);
            return row;
        }

        private static Label Heading(string text)
        {
            var heading = new Label(text) { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("host-label");
            heading.AddToClassList("host-heading");
            return heading;
        }

        private VisualElement Visibility()
        {
            var segments = new VisualElement { name = "host-visibility" };
            segments.AddToClassList("host-segments");
            publicButton = Segment("host-public", "Public", RoomVisibility.Public);
            privateButton = Segment("host-private", "Private", RoomVisibility.Private);
            segments.Add(publicButton);
            segments.Add(privateButton);
            return segments;
        }

        private Button Segment(string id, string text, RoomVisibility value)
        {
            var button = BoardScreen.Button(text, () => SetVisibility(value));
            button.name = id;
            button.AddToClassList("host-segment");
            return button;
        }

        public void SetVisibility(RoomVisibility value)
        {
            visibility = value;
            publicButton.EnableInClassList("selected", value == RoomVisibility.Public);
            privateButton.EnableInClassList("selected", value == RoomVisibility.Private);
            visibilityNote.text = value == RoomVisibility.Public
                ? "Listed under Open games; anyone may take the free seat."
                : "Invitation only: share the game's code with your opponent.";
        }

        private VisualElement Footer()
        {
            var footer = new VisualElement();
            footer.AddToClassList("host-footer");
            var back = BoardScreen.Button("Back", () => Cancel?.Invoke());
            back.name = "cancel-host";
            back.AddToClassList("host-back");
            footer.Add(back);
            status.AddToClassList("host-status");
            footer.Add(status);
            var submit = BoardScreen.Button("Create game", Submit);
            submit.name = "submit-host";
            submit.AddToClassList("host-submit");
            footer.Add(submit);
            return footer;
        }
    }
}
