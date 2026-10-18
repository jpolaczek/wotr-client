using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-11: the main menu as one composed screen — a masthead, a fixed window that lists the open games (or,
    // from the right-hand column, your own games, the host form or the test scenarios) and a column of actions.
    // MENU-13: a started game has one row that resumes it; there is no recent-changes page.
    // Every control keeps its place; element names stay stable for the menu tests and the browser controller.
    public sealed class MainMenuScreen
    {
        public VisualElement Root { get; } = new VisualElement();
        public Label Status { get; } = new Label { name = "lobby-status" };
        private readonly VisualElement games = new VisualElement { name = "my-games" };
        private readonly VisualElement tables = new VisualElement { name = "public-games" };
        private readonly VisualElement matches = new VisualElement();
        private readonly VisualElement rooms = new VisualElement();
        private readonly Label empty = new Label("No started games yet. Join an open game or host one.");
        private readonly Dictionary<string, SavedMatchRow> rows = new Dictionary<string, SavedMatchRow>();
        private readonly HostGameForm host;
        private readonly DemoStartPage demo = new DemoStartPage();
        private readonly VisualElement lists = new VisualElement();
        private readonly VisualElement publicRooms = new VisualElement();
        private readonly Dictionary<string, PublicRoomRow> publicRows = new Dictionary<string, PublicRoomRow>();
        private readonly Dictionary<string, Button> ownRows = new Dictionary<string, Button>();
        private readonly ScrollView matchScroll = new ScrollView { name = "my-games-scroll" };
        private readonly ScrollView publicScroll = new ScrollView { name = "public-games-scroll" };
        private readonly TestScenarioList testScenarios = new TestScenarioList();
        private readonly Label windowTitle = new Label("Open games") { name = "menu-window-title" };
        private readonly Button showPublic;
        private readonly Button showMine;
        private readonly Label myTurns = new Label { name = "my-games-turns", pickingMode = PickingMode.Ignore };
        private readonly Button testButton;
        public event Action<CreateRoomRequest> Create;
        // MENU-31: the side chosen on the demo page; the flow starts a demo against the CPU at once.
        public event Action<Side> Demo;
        // OBS-04: a public game is joined to play or to observe.
        public event Action<string, RoomRole> JoinPublic;
        public event Action MorePublic;
        public event Action MoreMatches;
        public event Action<string, RoomRole> Join;
        public event Action<RoomView> Open;
        public event Action<MatchSummary> OpenMatch;
        public event Action Refresh;
        public event Action Logout;
        // MENU-14: a confirmed Quit game closes the application.
        public event Action QuitRequested;
        public ConfirmationDialog Quit { get; } = ConfirmationDialog.Quit("Your games stay on the server.");
        public event Action InterfaceRequested;
        public event Action<string> StartScenario;
        // REPLAY-04: the library page in the window; the flow fills it when it opens.
        public ReplayLibraryPage Replays { get; } = new ReplayLibraryPage(false);
        // LOBBY-01: the Open games page's chat and players online, and a challenge to this player over the window.
        public LobbyPanel Lobby { get; } = new LobbyPanel();
        public ChallengeTab Challenge { get; } = new ChallengeTab();
        // AUTH-06: the window for a new account name, opened from the masthead.
        public AccountNameDialog AccountName { get; } = new AccountNameDialog();
        private readonly Label player = new Label { name = "menu-player", pickingMode = PickingMode.Ignore };
        private string username;
        public event Action ReplaysOpened;
        private Button showReplays;

        public MainMenuScreen(string username)
        {
            this.username = username;
            Root.name = "main-menu";
            FrontendTheme.Apply(Root);
            Root.AddToClassList("gateway");
            Root.AddToClassList("main-menu");
            var page = Ignored("menu-page");
            page.pickingMode = PickingMode.Position;
            Root.Add(page);
            page.Add(Masthead(username));
            var body = new VisualElement();
            body.AddToClassList("menu-body");
            page.Add(body);
            var panel = new VisualElement();
            panel.name = "menu-window";
            panel.AddToClassList("menu-window");
            body.Add(panel);
            panel.Add(WindowHeader());
            BuildLists(panel);
            panel.Add(Challenge.Root);
            host = new HostGameForm(username);
            host.Create += request => Create?.Invoke(request);
            host.Cancel += ShowPublicGames;
            host.Root.style.display = DisplayStyle.None;
            panel.Add(host.Root);
            demo.Start += side => Demo?.Invoke(side);
            demo.Back += ShowPublicGames;
            demo.Root.style.display = DisplayStyle.None;
            panel.Add(demo.Root);
            testScenarios.Start += id => StartScenario?.Invoke(id);
            testScenarios.Back += ShowPublicGames;
            testScenarios.Root.style.display = DisplayStyle.None;
            panel.Add(testScenarios.Root);
            Replays.Root.style.display = DisplayStyle.None;
            panel.Add(Replays.Root);
            showPublic = Action("show-public-games", "Open games", ShowPublicGames);
            showMine = Action("show-my-games", "My games", ShowMyGames);
            showReplays = Action("show-replays", "Replays", ShowReplays);
            showReplays.tooltip = "Watch saved games again, and import or export replay files.";
            myTurns.AddToClassList("menu-action-badge");
            showMine.Add(myTurns);
            ShowTurns(0);
            testButton = Action("open-test-scenarios", "Test scenarios", ShowTestScenarioList);
            testButton.style.display = DisplayStyle.None;
            body.Add(ActionColumn());
            Status.AddToClassList("status-message");
            Status.AddToClassList("menu-status");
            page.Add(Status);
            Quit.Confirmed += () => QuitRequested?.Invoke();
            Root.Add(Quit.Root);
            Root.Add(AccountName.Root);
            ShowPublicGames();
        }

        // AUTH-06: "Signed in as" and, beside it, the change of the account name.
        private VisualElement Masthead(string name)
        {
            var masthead = Ignored("menu-masthead");
            var title = new Label("WAR OF THE RING") { name = "menu-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("menu-title");
            var account = Ignored("menu-account");
            player.AddToClassList("menu-player");
            ShowUsername(name);
            var change = BoardScreen.Button("Change name", () => AccountName.Open(username));
            change.name = "change-account-name";
            change.AddToClassList("menu-account-change");
            change.tooltip = "Change the name you sign in and play under.";
            account.Add(player);
            account.Add(change);
            masthead.Add(title);
            masthead.Add(account);
            return masthead;
        }

        public void ShowUsername(string name)
        {
            username = name;
            player.text = "Signed in as " + name;
        }

        private VisualElement WindowHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("menu-window-header");
            windowTitle.AddToClassList("menu-window-title");
            header.Add(windowTitle);
            var refresh = IconButton.Create("refresh", "Refresh", () => Refresh?.Invoke());
            refresh.name = "refresh-games";
            refresh.AddToClassList("menu-refresh");
            header.Add(refresh);
            return header;
        }

        private void BuildLists(VisualElement panel)
        {
            lists.AddToClassList("menu-lists");
            panel.Add(lists);
            publicScroll.AddToClassList("saved-games-scroll");
            publicScroll.AddToClassList("menu-scroll");
            publicScroll.Add(publicRooms);
            tables.Add(publicScroll);
            tables.Add(Lobby.Root);
            publicScroll.verticalScroller.valueChanged += value => RequestMore(publicScroll, value, MorePublic);
            matchScroll.AddToClassList("saved-games-scroll");
            matchScroll.AddToClassList("menu-scroll");
            matchScroll.Add(rooms);
            matchScroll.Add(matches);
            empty.AddToClassList("menu-empty");
            games.Add(empty);
            games.Add(matchScroll);
            matchScroll.verticalScroller.valueChanged += value => RequestMore(matchScroll, value, MoreMatches);
            foreach (var list in new[] { tables, games })
            {
                list.AddToClassList("menu-list-page");
                lists.Add(list);
            }
        }

        private VisualElement ActionColumn()
        {
            var column = new VisualElement { name = "menu-actions" };
            column.AddToClassList("menu-actions");
            column.Add(showPublic);
            column.Add(showMine);
            column.Add(showReplays);
            column.Add(Action("host-game", "Host a game", ShowHostForm));
            column.Add(Action("menu-demo", "Demo vs CPU", ShowDemo));
            column.Add(JoinBox());
            column.Add(testButton);
            var settings = InterfaceSettingsPanel.OpenButton(() => InterfaceRequested?.Invoke());
            settings.AddToClassList("menu-action");
            column.Add(settings);
            column.Add(Ignored("menu-actions-spacer"));
            // MENU-31: Sign out and Quit game share the column's last row, so Demo vs CPU fits above them.
            var footer = new VisualElement { name = "menu-actions-footer" };
            footer.AddToClassList("menu-actions-footer");
            var signOut = Action("sign-out", "Sign out", () => Logout?.Invoke());
            signOut.AddToClassList("menu-action-quiet");
            footer.Add(signOut);
            var quit = Action("quit-game", "Quit game", Quit.Open);
            quit.AddToClassList("menu-action-quiet");
            footer.Add(quit);
            column.Add(footer);
            return column;
        }

        private VisualElement JoinBox()
        {
            var box = new VisualElement { name = "join-code-row" };
            box.AddToClassList("menu-join");
            var heading = new Label("Join by code") { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("menu-join-title");
            box.Add(heading);
            var line = new VisualElement();
            line.AddToClassList("menu-join-line");
            var code = new TextField { name = "table-invitation" };
            code.AddToClassList("menu-join-code");
            code.textEdition.placeholder = "Code";
            var join = BoardScreen.Button("Join", () => Join?.Invoke(code.value.Trim(), RoomRole.Player));
            join.name = "join-invitation";
            join.AddToClassList("menu-join-submit");
            join.tooltip = "Take the free seat of the game with this code.";
            // OBS-04: the same code lets one watch, also when both seats are taken or the game has started.
            var observe = BoardScreen.Button("Observe", () => Join?.Invoke(code.value.Trim(), RoomRole.Observer));
            observe.name = "observe-invitation";
            observe.AddToClassList("menu-join-submit");
            observe.tooltip = "Watch the game with this code without playing.";
            line.Add(code);
            line.Add(join);
            line.Add(observe);
            box.Add(line);
            return box;
        }

        private static Button Action(string name, string text, Action click)
        {
            var button = BoardScreen.Button(text, click);
            button.name = name;
            button.AddToClassList("menu-action");
            return button;
        }

        private static VisualElement Ignored(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(name);
            return element;
        }

        // The window shows one page at a time; the column marks which one.
        private void ShowPage(string title, VisualElement shown)
        {
            windowTitle.text = title;
            lists.style.display = shown == tables || shown == games ? DisplayStyle.Flex : DisplayStyle.None;
            tables.style.display = shown == tables ? DisplayStyle.Flex : DisplayStyle.None;
            games.style.display = shown == games ? DisplayStyle.Flex : DisplayStyle.None;
            host.Root.style.display = shown == host.Root ? DisplayStyle.Flex : DisplayStyle.None;
            demo.Root.style.display = shown == demo.Root ? DisplayStyle.Flex : DisplayStyle.None;
            testScenarios.Root.style.display = shown == testScenarios.Root ? DisplayStyle.Flex : DisplayStyle.None;
            Replays.Root.style.display = shown == Replays.Root ? DisplayStyle.Flex : DisplayStyle.None;
            showPublic.EnableInClassList("selected", shown == tables);
            showMine.EnableInClassList("selected", shown == games);
            showReplays.EnableInClassList("selected", shown == Replays.Root);
            RequestMoreIfNeeded();
        }

        private void ShowPublicGames() => ShowPage("Open games", tables);

        private void ShowMyGames() => ShowPage("My games", games);

        public void ShowReplays()
        {
            ShowPage("Replays", Replays.Root);
            ReplaysOpened?.Invoke();
        }

        private void ShowHostForm() => ShowPage("Host a game", host.Root);

        private void ShowDemo() => ShowPage("Demo vs CPU", demo.Root);

        private static void RequestMore(ScrollView scroll, float value, Action more)
        {
            if (scroll.verticalScroller.highValue > 0 && value >= scroll.verticalScroller.highValue - 32)
                more?.Invoke();
        }

        public void RequestMoreIfNeeded()
        {
            // The scheduler runs before layout, so rows shown this frame still have the old scroll range.
            // Check on the following tick, or a full first page looks short and reads a page nobody scrolled to.
            Root.schedule.Execute(() => Root.schedule.Execute(MoreIfShort));
        }

        private void MoreIfShort()
        {
            if (ReachesEnd(publicScroll))
                MorePublic?.Invoke();
            if (ReachesEnd(matchScroll))
                MoreMatches?.Invoke();
        }

        private static bool ReachesEnd(ScrollView scroll)
        {
            return scroll.worldBound.height > 0 && scroll.verticalScroller.value >= scroll.verticalScroller.highValue - 32;
        }

        public void ShowMatches(MatchListView page)
        {
            var ids = new HashSet<string>(page.Matches.Select(m => m.MatchId));
            foreach (var id in rows.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                rows[id].Root.RemoveFromHierarchy();
                rows.Remove(id);
            }
            for (var index = 0; index < page.Matches.Count; index++)
            {
                var match = page.Matches[index];
                if (!rows.TryGetValue(match.MatchId, out var row))
                {
                    row = new SavedMatchRow(match, selected => OpenMatch?.Invoke(selected));
                    rows.Add(match.MatchId, row);
                }
                row.Apply(match);
                if (index >= matches.childCount || matches[index] != row.Root)
                    matches.Insert(index, row.Root);
            }
            ShowTurns(page.Matches.Count(match => match.Status == MatchStatus.YourDecision));
            UpdateEmpty();
        }

        // The My games action names how many loaded games wait for your decision.
        private void ShowTurns(int count)
        {
            myTurns.text = count.ToString();
            myTurns.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            showMine.tooltip = count == 0 ? "Your started games" : count == 1 ? "1 game waits for your decision" : count + " games wait for your decision";
        }

        public void ShowTestScenarios(TestScenarioCatalogView catalog)
        {
            testScenarios.Show(catalog);
            testButton.style.display = catalog.Scenarios.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowTestScenarioList() => ShowPage("Test scenarios", testScenarios.Root);

        public void Busy(bool value)
        {
            host.Busy(value);
            testScenarios.Root.SetEnabled(!value);
            Root.Q<Button>("join-invitation").SetEnabled(!value);
            Root.Q<Button>("observe-invitation").SetEnabled(!value);
            foreach (var row in publicRows.Values) row.Root.SetEnabled(!value);
            foreach (var row in ownRows.Values) row.SetEnabled(!value);
        }

        public void ShowRooms(IEnumerable<RoomView> values)
        {
            // OBS-04: rooms one plays in while they wait, and rooms one watches, also while their game goes on.
            var pending = values.Where(room => room.Status == RoomStatus.Waiting && room.MatchId.Length == 0 ||
                room.Role == RoomRole.Observer && room.Status == RoomStatus.Started).ToArray();
            var ids = new HashSet<string>(pending.Select(room => room.Id));
            foreach (var id in ownRows.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                ownRows[id].RemoveFromHierarchy();
                ownRows.Remove(id);
            }
            for (var index = 0; index < pending.Length; index++)
            {
                var room = pending[index];
                if (!ownRows.TryGetValue(room.Id, out var button))
                {
                    var id = room.Id;
                    button = BoardScreen.Button("", () => Open?.Invoke(currentRooms[id]));
                    button.name = "open-room-" + id;
                    button.AddToClassList("menu-row");
                    button.AddToClassList("own-room-row");
                    ownRows.Add(id, button);
                }
                currentRooms[room.Id] = room;
                button.text = room.Name + " · " + OwnRoomState(room);
                if (index >= rooms.childCount || rooms[index] != button) rooms.Insert(index, button);
            }
            foreach (var id in currentRooms.Keys.Where(id => !ids.Contains(id)).ToArray()) currentRooms.Remove(id);
            UpdateEmpty();
        }
        private readonly Dictionary<string, RoomView> currentRooms = new Dictionary<string, RoomView>();
        private readonly Label publicEmpty = new Label("No open games right now. Host one, or join a friend's game by code.") { name = "public-games-empty" };
        public void ShowPublic(PublicRoomListView page)
        {
            publicEmpty.RemoveFromHierarchy();
            var ids = new HashSet<string>(page.Rooms.Select(room => room.Id));
            foreach (var id in publicRows.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                publicRows[id].Root.RemoveFromHierarchy();
                publicRows.Remove(id);
            }
            for (var index = 0; index < page.Rooms.Count; index++)
            {
                var room = page.Rooms[index];
                if (!publicRows.TryGetValue(room.Id, out var row))
                {
                    row = new PublicRoomRow(room.Id, (id, role) => JoinPublic?.Invoke(id, role));
                    publicRows.Add(room.Id, row);
                }
                row.Apply(room);
                if (index >= publicRooms.childCount || publicRooms[index] != row.Root)
                    publicRooms.Insert(index, row.Root);
            }
            if (page.Rooms.Count == 0)
                publicRooms.Add(publicEmpty);
        }

        private static string OwnRoomState(RoomView room)
        {
            if (room.Role == RoomRole.Observer)
                return room.Status == RoomStatus.Started ? "Watching · In progress" : "Watching";
            return string.IsNullOrEmpty(room.FreePeoples) || string.IsNullOrEmpty(room.Shadow) ? "Waiting for opponent" : "Prepare game";
        }

        private void UpdateEmpty() => empty.style.display = rows.Count == 0 && ownRows.Count == 0
            ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
