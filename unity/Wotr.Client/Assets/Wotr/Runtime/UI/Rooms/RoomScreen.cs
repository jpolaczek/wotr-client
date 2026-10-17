using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class RoomScreen
    {
        public VisualElement Root { get; } = new VisualElement();
        public Label Status { get; } = new Label();
        public RoomView Room
        {
            get; private set;
        }
        public event Action<MatchConfiguration> Configure;
        public event Action Confirm;
        public event Action Start;
        public event Action Back;
        public event Action InterfaceRequested;
        private readonly Label seats = new Label();
        private readonly Label accepted = new Label();
        private readonly VisualElement options = new VisualElement();
        private readonly Button confirm;
        private readonly Button start;
        private bool busy;
        private int shownVersion = -1;
        private string shownRoom = "";

        public RoomScreen()
        {
            Root.AddToClassList("gateway");
            var panel = new VisualElement();
            panel.AddToClassList("lobby-panel");
            Root.Add(panel);
            var header = BoardScreen.Row();
            header.Add(BoardScreen.Title("Prepare your table"));
            header.Add(BoardScreen.Button("Tables", () => Back?.Invoke()));
            header.Add(BoardScreen.Button("Copy table invitation", () => GUIUtility.systemCopyBuffer = Room?.JoinCode ?? ""));
            header.Add(InterfaceSettingsPanel.OpenButton(() => InterfaceRequested?.Invoke()));
            panel.Add(header);
            panel.Add(seats);
            panel.Add(options);
            panel.Add(accepted);
            var actions = BoardScreen.Row();
            confirm = BoardScreen.Button("Accept configuration", () => Confirm?.Invoke());
            confirm.name = "confirm-configuration";
            start = BoardScreen.Button("Start game", () => Start?.Invoke());
            start.name = "start-game";
            actions.Add(confirm);
            actions.Add(start);
            panel.Add(actions);
            panel.Add(Status);
        }

        public void Update(RoomView room)
        {
            if (Room != null && Room.Id == room.Id && room.Version < Room.Version)
                return;
            Room = room;
            seats.text = $"Free Peoples  ·  {Seat(room.FreePeoples)}   |   Shadow  ·  {Seat(room.Shadow)}";
            accepted.text = $"Free Peoples  {(room.FreePeoplesAccepted ? "✓ Ready" : "○ Reviewing")}   |   Shadow  {(room.ShadowAccepted ? "✓ Ready" : "○ Reviewing")}";
            if (shownRoom != room.Id || shownVersion != room.Version)
            {
                shownRoom = room.Id;
                shownVersion = room.Version;
                ConfigurationEditor.Render(options, room.Configuration, room.ExpansionsAvailable, config => Configure?.Invoke(config));
            }
            UpdateButtons();
        }

        public void Busy(bool value)
        {
            busy = value;
            UpdateButtons();
        }
        private static string Seat(string username) => string.IsNullOrEmpty(username) ? "Open seat" : username;

        private void UpdateButtons()
        {
            if (Room == null)
                return;
            var started = Room.MatchId.Length > 0;
            options.SetEnabled(!busy && !started);
            var ownAccepted = Room.You == Side.FreePeoples ? Room.FreePeoplesAccepted : Room.ShadowAccepted;
            confirm.SetEnabled(!busy && !started && !ownAccepted);
            start.SetEnabled(!busy && Room.FreePeoplesAccepted && Room.ShadowAccepted && Room.StartUnavailableReason.Length == 0);
            start.text = started ? "Open game" : "Start game";
            start.tooltip = started ? "Return to this game." : Room.StartUnavailableReason.Length == 0
                ? "Both players must accept the same configuration." : Room.StartUnavailableReason == "ExpansionsUnavailable"
                ? "This server offers only the base game." : "This server cannot start the selected configuration.";
        }
    }
}
