using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-25: the waiting room in the main menu's frame — a masthead with the room's name, a window with a seat for
    // each side, the invitation and the chat, a column of actions and a status line. Sizes are fixed and every
    // control keeps its place; what is not available is greyed out, never hidden (docs/ui/WAITING_ROOM_UX.md).
    // OBS-04: each seat takes a free place or removes its player, the audience line lists the observers, and a
    // player may move to the audience; the host may run the room from the audience.
    // UI-263: under the seats the window splits: the audience, the invitation and the chat on the left, the rules
    // (the table's expansions and variants) on the right. Only the host changes the rules, and Ready accepts them;
    // UI-364: the host's Ready also locks them until Not ready.
    public sealed class WaitingRoomScreen
    {
        public VisualElement Root { get; } = new VisualElement { name = "waiting-room" };
        public Label Status { get; } = new Label { name = "waiting-status" };
        // MENU-26: the chat panel's place in the window.
        public VisualElement ChatHost { get; } = new VisualElement { name = "waiting-chat" };
        public RoomView Room { get; private set; }
        public event Action<bool> Ready;
        public event Action Start;
        public event Action Demo;
        public event Action Back;
        public event Action Invite;
        public event Action AddCpu;
        // UI-533: the human in a room with the CPU plays the other side (the CPU takes this one).
        public event Action SwapSides;
        // BOT-37: the host switches the seated CPU's pace while the room waits.
        public event Action<BotThinkingMode> ChangeCpuPace;
        // OBS-04: the participant the host removes, a player or an observer, by name.
        public event Action<string> Remove;
        public event Action<Side> TakeSeat;
        public event Action Observe;
        public event Action Refresh;
        public event Action InterfaceRequested;
        // UI-263: the configuration the host asks for after one click on the rules strip.
        public event Action<MatchConfiguration> Configure;
        public RulesStrip Rules { get; } = new RulesStrip();
        private readonly Label title = new Label { name = "waiting-title" };
        private readonly Label information = new Label { name = "waiting-information" };
        private readonly SeatCard freePeoples;
        private readonly SeatCard shadow;
        private readonly AudienceLine audience;
        private readonly Button observe;
        private readonly Button ready;
        private readonly Button start;
        private readonly Button demo;
        private readonly Button back;
        private readonly Button invite;
        private readonly Button copy;
        private readonly TextField code = new TextField { name = "room-invitation", isReadOnly = true };
        // UI-525 (owner, 2026-11-08: toggling expansions "makes a large part of the layout and the whole menu on the right
        // blink"): a request blocks further actions at once (WaitingRoomController.Run ignores them), but the controls grey
        // out only when the server takes longer than this, so a quick change keeps the page still.
        public const long BusyShownAfterMilliseconds = 400;
        private bool busy;
        private bool shownBusy;
        private IVisualElementScheduledItem showBusy;
        private Side? invitingSide;
        private BotSeatStatus invitationStatus;

        public WaitingRoomScreen()
        {
            FrontendTheme.Apply(Root);
            var sheet = Resources.Load<StyleSheet>("Wotr/Styles/WaitingRoom");
            if (sheet != null)
                Root.styleSheets.Add(sheet);
            Root.AddToClassList("gateway");
            Root.AddToClassList("main-menu");
            Root.AddToClassList("waiting-room");
            freePeoples = new SeatCard(Side.FreePeoples, side => TakeSeat?.Invoke(side), player => Remove?.Invoke(player), () => AddCpu?.Invoke(),
                mode => ChangeCpuPace?.Invoke(mode), () => SwapSides?.Invoke());
            shadow = new SeatCard(Side.Shadow, side => TakeSeat?.Invoke(side), player => Remove?.Invoke(player), () => AddCpu?.Invoke(),
                mode => ChangeCpuPace?.Invoke(mode), () => SwapSides?.Invoke());
            audience = new AudienceLine(observer => Remove?.Invoke(observer));
            observe = Action("room-observe", "Observe", () => Observe?.Invoke(), "menu-action");
            ready = Action("room-ready", "Ready", () => Ready?.Invoke(!OwnReady), "menu-action");
            start = Action("start-game", "Start game", () => Start?.Invoke(), "menu-action");
            // DEMO-04: a practice game against the CPU, for either member while the room waits; the room stays.
            demo = Action("room-demo", "Play demo vs CPU", () => Demo?.Invoke(), "menu-action");
            demo.tooltip = "Practice against a simple computer opponent playing the other side. Starting a new demo replaces your previous demo.";
            back = Action("waiting-back", "Back", () => Back?.Invoke(), "menu-action");
            back.AddToClassList("menu-action-quiet");
            invite = Action("generate-room-invitation", "New code", () => Invite?.Invoke(), "waiting-invite-action");
            copy = Action("copy-room-invitation", "Copy", CopyCode, "waiting-invite-action");
            Rules.Toggled += Toggle;
            var page = new VisualElement { pickingMode = PickingMode.Ignore };
            page.AddToClassList("menu-page");
            Root.Add(page);
            page.Add(Masthead());
            var body = new VisualElement();
            body.AddToClassList("menu-body");
            body.Add(Window());
            body.Add(ActionColumn());
            page.Add(body);
            Status.AddToClassList("status-message");
            Status.AddToClassList("menu-status");
            page.Add(Status);
        }

        private bool Player => Room != null && Room.Role == RoomRole.Player;

        private bool OwnReady => RulesChoices.OwnReady(Room);

        private VisualElement Masthead()
        {
            var masthead = new VisualElement { pickingMode = PickingMode.Ignore };
            masthead.AddToClassList("menu-masthead");
            title.pickingMode = PickingMode.Ignore;
            title.AddToClassList("menu-title");
            title.AddToClassList("waiting-title");
            information.pickingMode = PickingMode.Ignore;
            information.AddToClassList("menu-player");
            masthead.Add(title);
            masthead.Add(information);
            return masthead;
        }

        private VisualElement Window()
        {
            var window = new VisualElement { name = "waiting-window" };
            window.AddToClassList("menu-window");
            window.Add(WindowHeader());
            var seats = new VisualElement { name = "waiting-seats" };
            seats.AddToClassList("waiting-seats");
            seats.Add(freePeoples.Root);
            seats.Add(shadow.Root);
            window.Add(seats);
            var lower = new VisualElement { name = "waiting-lower" };
            lower.AddToClassList("waiting-lower");
            var social = new VisualElement { name = "waiting-social" };
            social.AddToClassList("waiting-social");
            social.Add(audience.Root);
            social.Add(InvitationLine());
            ChatHost.AddToClassList("waiting-chat");
            social.Add(ChatHost);
            lower.Add(social);
            lower.Add(Rules.Root);
            window.Add(lower);
            return window;
        }

        // Refresh sits at the end of the header plate, as in the main menu, away from the larger buttons.
        private VisualElement WindowHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("menu-window-header");
            var heading = new Label("Players") { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("menu-window-title");
            header.Add(heading);
            var refresh = IconButton.Create("refresh", "Refresh", () => Refresh?.Invoke());
            refresh.name = "refresh-room";
            refresh.AddToClassList("menu-refresh");
            header.Add(refresh);
            return header;
        }

        private VisualElement InvitationLine()
        {
            var line = new VisualElement { name = "waiting-invitation" };
            line.AddToClassList("waiting-invitation");
            var label = new Label("Invitation") { pickingMode = PickingMode.Ignore };
            label.AddToClassList("waiting-invitation-title");
            code.AddToClassList("waiting-invitation-code");
            line.Add(label);
            line.Add(code);
            line.Add(copy);
            line.Add(invite);
            return line;
        }

        private VisualElement ActionColumn()
        {
            var column = new VisualElement { name = "waiting-actions" };
            column.AddToClassList("menu-actions");
            column.Add(ready);
            column.Add(start);
            column.Add(demo);
            column.Add(observe);
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("menu-actions-spacer");
            column.Add(spacer);
            var settings = InterfaceSettingsPanel.OpenButton(() => InterfaceRequested?.Invoke());
            settings.AddToClassList("menu-action");
            column.Add(settings);
            column.Add(back);
            return column;
        }

        private void Toggle(string rule)
        {
            if (Room != null && !busy)
                Configure?.Invoke(RulesChoices.Toggle(Room.Configuration, rule));
        }

        private static Button Action(string name, string text, Action action, string style)
        {
            var button = BoardScreen.Button(text, action);
            button.name = name;
            button.AddToClassList(style);
            return button;
        }

        private void CopyCode()
        {
            if (Room == null || Room.JoinCode.Length == 0)
                return;
            GUIUtility.systemCopyBuffer = Room.JoinCode;
            Status.text = "Code copied. Paste it in Join by code in the main menu.";
        }

        public void Update(RoomView room)
        {
            if (Room != null && Room.Id == room.Id && room.Version < Room.Version)
                return;
            Room = room;
            title.text = room.Name;
            information.text = $"{room.Visibility} · {RulesChoices.Summary(room.Configuration)} · Two players · " +
                $"Observers {room.Observers.Count}/{Limit(room)}";
            freePeoples.Show(room, invitingSide, invitationStatus);
            shadow.Show(room, invitingSide, invitationStatus);
            audience.Show(room, Limit(room));
            code.SetValueWithoutNotify(room.JoinCode);
            back.tooltip = room.IsHost ? "Cancel this waiting room for everyone in it."
                : Player ? "Free your seat. Disconnecting alone keeps your seat." : "Leave the audience of this room.";
            UpdateButtons();
        }

        public void Busy(bool value)
        {
            busy = value;
            showBusy?.Pause();
            if (value)
            {
                showBusy = Root.schedule.Execute(ShowBusy);
                showBusy.ExecuteLater(BusyShownAfterMilliseconds);
                return;
            }
            shownBusy = false;
            UpdateButtons();
        }

        // A request still running after the moment greys the controls out until it ends.
        private void ShowBusy()
        {
            if (!busy || shownBusy)
                return;
            shownBusy = true;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (Room == null)
                return;
            var waiting = Room.Status == RoomStatus.Waiting;
            ready.text = OwnReady ? "Not ready" : "Ready";
            ready.SetEnabled(!shownBusy && waiting && Player);
            ready.tooltip = ReadyTooltip();
            back.SetEnabled(!shownBusy);
            demo.SetEnabled(!shownBusy && waiting && Player);
            var audienceFull = Room.Observers.Count >= Limit(Room);
            observe.SetEnabled(!shownBusy && waiting && Player && !audienceFull);
            observe.tooltip = !Player ? "You are watching this room." : audienceFull ? "All observer places are taken."
                : "Leave your seat and watch this game.";
            var cpuReady = Room.Bot == null || Room.Bot.Status == BotSeatStatus.Ready;
            start.SetEnabled(!shownBusy && waiting && Room.IsHost && Room.FreePeoplesAccepted && Room.ShadowAccepted && cpuReady && Room.StartUnavailableReason.Length == 0);
            start.tooltip = !Room.IsHost ? "Only the host starts the game."
                : !cpuReady ? "Wait for CPU to be ready."
                : Room.StartUnavailableReason.Length == 0 ? "Both players must be ready."
                : "This server no longer offers an expansion chosen here. " +
                  (OwnReady ? "Press Not ready and change the rules first." : "Change the rules first.");
            Rules.Show(Room, shownBusy);
            freePeoples.Enable(Room, shownBusy);
            shadow.Enable(Room, shownBusy);
            audience.Enable(Room, shownBusy);
            // OBS-04: the code also lets observers in, so the host shows it while the room is open.
            invite.SetEnabled(!shownBusy && Room.IsHost && Room.Status != RoomStatus.Closed);
            copy.SetEnabled(!shownBusy && Room.JoinCode.Length > 0);
        }

        // UI-364: the host's Ready locks the rules strip and Not ready opens it again.
        private string ReadyTooltip()
        {
            if (!Player)
                return "Only the players get ready.";
            if (OwnReady)
                return Room.IsHost ? "Withdraw your acceptance of these rules to change them again."
                    : "Withdraw your acceptance of these rules.";
            return Room.IsHost ? "Accept these rules and get ready to play. The rules stay locked until Not ready."
                : "Accept these rules and get ready to play.";
        }

        private static int Limit(RoomView room) => room.ObserverLimit > 0 ? room.ObserverLimit : RoomLimits.Observers;

        public void ShowInvitation(Side? side, BotSeatStatus status = BotSeatStatus.Joining)
        {
            invitingSide = side;
            invitationStatus = status;
            if (Room != null)
            {
                freePeoples.Show(Room, invitingSide, invitationStatus);
                shadow.Show(Room, invitingSide, invitationStatus);
            }
        }

        // One seat: the side's emblem and name, the player or an empty place, Host/You and readiness; Take seat for a free
        // place and Remove for the host, both always in place. UI-533 (owner, 2026-11-12): in a room with the CPU the host's
        // own seat offers Switch side in its top right corner, beside the side's name (where the CPU's seat shows its pace).
        private sealed class SeatCard
        {
            public VisualElement Root { get; }
            private readonly Side side;
            private readonly Label player = new Label();
            private readonly VisualElement avatar = AvatarImage.Create(AvatarImage.Small);
            private readonly Label tags = new Label();
            private readonly Label state = new Label();
            private readonly Button take;
            private readonly Button remove;
            // BOT-37: the CPU's pace, Fast or Thorough; the host's press switches it.
            private readonly Button pace;
            private readonly Button swap;
            private BotThinkingMode mode;
            private string holder = "";

            public SeatCard(Side side, Action<Side> takeSeat, Action<string> removePlayer, Action addCpu, Action<BotThinkingMode> changePace,
                Action swapSides)
            {
                this.side = side;
                var key = side == Side.FreePeoples ? "free" : "shadow";
                Root = new VisualElement { name = "seat-" + key };
                Root.AddToClassList("waiting-seat");
                var heading = new VisualElement { pickingMode = PickingMode.Ignore };
                heading.AddToClassList("waiting-seat-heading");
                var emblem = new VisualElement { pickingMode = PickingMode.Ignore };
                emblem.AddToClassList("menu-row-emblem");
                emblem.AddToClassList("side-" + key);
                heading.Add(emblem);
                var sideName = new Label(side == Side.FreePeoples ? "Free Peoples" : "Shadow") { pickingMode = PickingMode.Ignore };
                sideName.AddToClassList("waiting-seat-side");
                heading.Add(sideName);
                Root.Add(heading);
                player.name = "seat-" + key + "-player";
                player.AddToClassList("waiting-seat-player");
                tags.AddToClassList("waiting-seat-tags");
                state.name = "seat-" + key + "-state";
                state.AddToClassList("waiting-seat-state");
                foreach (var label in new[] { player, tags, state })
                    label.pickingMode = PickingMode.Ignore;
                Root.Add(AvatarImage.Line(player, avatar));
                Root.Add(tags);
                Root.Add(state);
                take = Action("seat-" + key + "-take", "Take seat", () => takeSeat(side), "waiting-take");
                remove = Action("seat-" + key + "-remove", "Remove", () =>
                {
                    if (holder.Length == 0) addCpu();
                    else removePlayer(holder);
                }, "waiting-remove");
                pace = Action("seat-" + key + "-pace", "Pace: Fast", () =>
                    changePace(mode == BotThinkingMode.Fast ? BotThinkingMode.Thorough : BotThinkingMode.Fast), "waiting-pace");
                swap = Action("seat-" + key + "-swap", "Switch side", swapSides, "waiting-swap");
                Root.Add(take);
                Root.Add(remove);
                Root.Add(pace);
                Root.Add(swap);
            }

            public void Show(RoomView room, Side? inviting = null, BotSeatStatus invitation = BotSeatStatus.Joining)
            {
                holder = (side == Side.FreePeoples ? room.FreePeoples : room.Shadow) ?? "";
                var taken = holder.Length > 0;
                var accepted = side == Side.FreePeoples ? room.FreePeoplesAccepted : room.ShadowAccepted;
                var yours = room.Role == RoomRole.Player && room.You == side;
                var cpu = room.Bot?.Side == side;
                var status = cpu ? room.Bot.Status : invitation;
                var pending = !taken && inviting == side;
                player.text = cpu || pending ? "CPU" : taken ? holder : "Waiting for a player…";
                // AVATAR-01: the occupant's picture; the CPU and an empty seat show none.
                AvatarImage.Show(avatar, taken && !cpu ? (side == Side.FreePeoples ? room.FreePeoplesAvatar : room.ShadowAvatar) : "");
                Root.EnableInClassList("empty", !taken);
                Root.EnableInClassList("yours", yours);
                var host = taken && holder == room.Host;
                tags.text = host && yours ? "Host · You" : host ? "Host" : yours ? "You" : "";
                state.text = cpu || pending ? status.ToString() : taken ? accepted ? "Ready" : "Not ready" : "";
                state.EnableInClassList("ready", (cpu || pending) ? status == BotSeatStatus.Ready && accepted : taken && accepted);
                state.EnableInClassList("unavailable", (cpu || pending) && status == BotSeatStatus.Unavailable);
                remove.text = taken ? "Remove" : "Add CPU";
                mode = cpu ? room.Bot.ThinkingMode : BotThinkingMode.Fast;
                pace.text = mode == BotThinkingMode.Thorough ? "Pace: Thorough" : "Pace: Fast";
                pace.style.display = cpu ? DisplayStyle.Flex : DisplayStyle.None;
                var cpuRoom = room.Bot != null || inviting != null;
                swap.style.display = cpuRoom && yours && room.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public void Enable(RoomView room, bool busy)
            {
                var waiting = room.Status == RoomStatus.Waiting;
                var taken = holder.Length > 0;
                var yours = room.Role == RoomRole.Player && room.You == side;
                take.SetEnabled(!busy && waiting && !taken);
                take.tooltip = taken ? "This seat is taken." : !waiting ? "Seats cannot change after the start."
                    : "Play " + BoardScreen.SideName(side) + ". Both players get ready again.";
                var canInvite = room.Role == RoomRole.Player && room.You != side;
                remove.SetEnabled(!busy && waiting && room.IsHost && (taken ? !yours : canInvite));
                remove.tooltip = taken ? room.Bot?.Side == side ? "Remove CPU from this room."
                    : room.IsHost ? "Remove this player from the room. They cannot join it again." : "Only the host can remove a player."
                    : !room.IsHost || !canInvite ? "Only a seated host can invite CPU to the opposite seat."
                    : "Invite CPU. If the connection was interrupted, retry the same invitation.";
                var cpu = room.Bot?.Side == side;
                swap.SetEnabled(!busy && waiting && room.IsHost && yours);
                var other = side == Side.FreePeoples ? "the Shadow" : "the Free Peoples";
                swap.tooltip = !waiting ? "Sides cannot change after the start."
                    : $"Play {other}; the CPU takes {(side == Side.FreePeoples ? "the Free Peoples" : "the Shadow")}. The rules and the CPU's pace stay.";
                pace.SetEnabled(!busy && waiting && room.IsHost && cpu);
                pace.tooltip = !cpu ? "" : !waiting ? "The CPU's pace is saved with the game."
                    : !room.IsHost ? "Only the host chooses the CPU's pace."
                    // The paces' times belong to the server's CPU (a new CPU may change them without a client release), so
                    // the text names no seconds.
                    : mode == BotThinkingMode.Fast
                        ? "Fast: the CPU answers quickly. Press for Thorough: it looks further ahead and takes longer. Both players get ready again."
                        : "Thorough: the CPU looks further ahead and takes longer. Press for Fast: it answers quickly. Both players get ready again.";
            }
        }

        // OBS-04: the observers of the room in one line of fixed height; the host removes one with its ×.
        private sealed class AudienceLine
        {
            public VisualElement Root { get; } = new VisualElement { name = "waiting-observers" };
            private readonly Label title = new Label { name = "waiting-observers-title", pickingMode = PickingMode.Ignore };
            private readonly VisualElement names = new VisualElement { name = "waiting-observer-names" };
            private readonly Action<string> remove;
            private readonly List<Button> removals = new List<Button>();
            private string shown = "";

            public AudienceLine(Action<string> remove)
            {
                this.remove = remove;
                Root.AddToClassList("waiting-observers");
                title.AddToClassList("waiting-observers-title");
                names.AddToClassList("waiting-observer-names");
                Root.Add(title);
                Root.Add(names);
            }

            // The names are rebuilt only when the audience itself changes, not on every refresh.
            public void Show(RoomView room, int limit)
            {
                title.text = "Observers " + room.Observers.Count + "/" + limit;
                var roster = string.Join("\n", room.Observers.Select(o => o.Name + "/" + o.Avatar + (o.IsHost ? "*" : "")));
                if (roster == shown)
                    return;
                shown = roster;
                names.Clear();
                removals.Clear();
                if (room.Observers.Count == 0)
                    names.Add(Empty());
                foreach (var observer in room.Observers)
                    names.Add(Chip(observer));
            }

            public void Enable(RoomView room, bool busy)
            {
                var allowed = !busy && room.Status == RoomStatus.Waiting && room.IsHost;
                foreach (var button in removals)
                    button.SetEnabled(allowed && !(bool)button.userData);
            }

            private static Label Empty()
            {
                var empty = new Label("Nobody is watching yet.") { pickingMode = PickingMode.Ignore };
                empty.AddToClassList("waiting-observers-empty");
                return empty;
            }

            private VisualElement Chip(RoomObserverView observer)
            {
                var chip = new VisualElement { name = "observer-" + observer.Name };
                chip.AddToClassList("waiting-observer");
                chip.Add(AvatarImage.Create(AvatarImage.Tiny, observer.Avatar));
                var label = new Label(observer.IsHost ? observer.Name + " · Host" : observer.Name) { pickingMode = PickingMode.Ignore, enableRichText = false };
                label.AddToClassList("waiting-observer-name");
                chip.Add(label);
                var name = observer.Name;
                var removal = IconButton.Create("close", "Remove " + name + " from this room. They cannot join it again.", () => remove(name));
                removal.name = "remove-observer-" + name;
                removal.userData = observer.IsHost;
                removal.AddToClassList("waiting-observer-remove");
                removals.Add(removal);
                chip.Add(removal);
                return chip;
            }
        }
    }
}
