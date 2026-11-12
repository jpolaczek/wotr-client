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
        // UI-496 (owner): My games' second tab, the finished games; the first keeps the games still going.
        private readonly VisualElement finished = new VisualElement();
        private readonly Label emptyFinished = new Label("No finished games yet.");
        private readonly Dictionary<string, SavedMatchRow> finishedRows = new Dictionary<string, SavedMatchRow>();
        private readonly ScrollView finishedScroll = new ScrollView { name = "finished-games-scroll" };
        private readonly Label newResults = new Label { name = "my-games-finished-new", pickingMode = PickingMode.Ignore };
        private Button activeTab;
        private Button finishedTab;
        private bool finishedShown;
        private int yourTurns;
        private int unseenResults;
        private readonly HostGameForm host;
        // UI-497: the ranked search's page; opening it asks the flow for the released modules and the rating.
        private readonly FindGameForm find = new FindGameForm();
        public event Action FindOpened;
        // UI-503: the ranked top 25; opening it asks the flow to read it.
        public LeaderboardPage Leaderboards { get; } = new LeaderboardPage();
        public event Action LeaderboardsOpened;
        private Button showLeaderboards;
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
        // The chosen side opens a private CPU waiting room; it does not start the game.
        public event Action<Side> PlayCpu;
        // OBS-04: a public game is joined to play or to observe.
        public event Action<string, RoomRole> JoinPublic;
        public event Action MorePublic;
        public event Action MoreMatches;
        public event Action MoreFinished;
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
        public event Action ReplaysOpened;
        private Button showReplays;
        // DIST-17: the glow and note of an available update on Settings.
        private SettingsUpdateHint updateHint;
        private VisualElement ownAvatar;

        public MainMenuScreen(string username, string avatar = "")
        {
            Root.name = "main-menu";
            FrontendTheme.Apply(Root);
            Root.AddToClassList("gateway");
            Root.AddToClassList("main-menu");
            var page = Ignored("menu-page");
            page.pickingMode = PickingMode.Position;
            Root.Add(page);
            page.Add(Masthead(username, avatar));
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
            find.Back += ShowPublicGames;
            find.Root.style.display = DisplayStyle.None;
            panel.Add(find.Root);
            Leaderboards.Root.style.display = DisplayStyle.None;
            panel.Add(Leaderboards.Root);
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
            showLeaderboards = Action("show-leaderboards", "Leaderboards", ShowLeaderboards);
            showLeaderboards.tooltip = "The 25 highest ranked players, and your own place.";
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
            ShowPublicGames();
        }

        // UI-505: a change made in the browser reaches this line with the menu's next refresh.
        public void ShowOwnAvatar(string avatar) => AvatarImage.Show(ownAvatar, avatar);

        private VisualElement Masthead(string username, string avatar)
        {
            ownAvatar = AvatarImage.Create(AvatarImage.Tiny, avatar);
            var masthead = Ignored("menu-masthead");
            var title = new Label("WAR OF THE RING") { name = "menu-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("menu-title");
            var ownName = new Label("Signed in as " + username) { name = "menu-player", pickingMode = PickingMode.Ignore };
            ownName.AddToClassList("menu-player");
            masthead.Add(title);
            masthead.Add(AvatarImage.Line(ownName, ownAvatar));
            return masthead;
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
            emptyFinished.AddToClassList("menu-empty");
            finishedScroll.AddToClassList("saved-games-scroll");
            finishedScroll.AddToClassList("menu-scroll");
            finishedScroll.Add(finished);
            games.Add(GameTabs());
            games.Add(empty);
            games.Add(emptyFinished);
            games.Add(matchScroll);
            games.Add(finishedScroll);
            matchScroll.verticalScroller.valueChanged += value => RequestMore(matchScroll, value, MoreMatches);
            finishedScroll.verticalScroller.valueChanged += value => RequestMore(finishedScroll, value, MoreFinished);
            ShowFinishedTab(false);
            foreach (var list in new[] { tables, games })
            {
                list.AddToClassList("menu-list-page");
                lists.Add(list);
            }
        }

        public void ShowUpdate(Updates.UpdateNote note) => updateHint.Show(note);

        private VisualElement ActionColumn()
        {
            var column = new VisualElement { name = "menu-actions" };
            column.AddToClassList("menu-actions");
            column.Add(showPublic);
            column.Add(showMine);
            column.Add(showReplays);
            column.Add(Action("find-game", "Find game", ShowFindGame));
            column.Add(showLeaderboards);
            column.Add(Action("host-game", "Host a game", ShowHostForm));
            // UI-533: Play vs CPU opens its waiting room at once (the player on the Free Peoples, the CPU invited); the
            // expansions and who plays which side are chosen there (Swap sides).
            column.Add(Action("menu-cpu", "Play vs CPU", () => PlayCpu?.Invoke(Side.FreePeoples)));
            column.Add(JoinBox());
            column.Add(testButton);
            var settings = InterfaceSettingsPanel.OpenButton(() => InterfaceRequested?.Invoke());
            settings.AddToClassList("menu-action");
            updateHint = new SettingsUpdateHint(settings);
            column.Add(settings);
            column.Add(Ignored("menu-actions-spacer"));
            // MENU-31: Sign out and Quit game share the column's last row, so Play vs CPU fits above them.
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

        private VisualElement GameTabs()
        {
            var tabs = BoardScreen.Row("my-games-tabs");
            tabs.AddToClassList("menu-tabs");
            activeTab = GameTab("my-games-active", "In progress", false);
            finishedTab = GameTab("my-games-finished", "Finished", true);
            newResults.AddToClassList("menu-action-badge");
            newResults.style.display = DisplayStyle.None;
            finishedTab.Add(newResults);
            tabs.Add(activeTab);
            tabs.Add(finishedTab);
            return tabs;
        }

        private Button GameTab(string name, string text, bool showsFinished)
        {
            var tab = BoardScreen.Button(text, () => ShowFinishedTab(showsFinished));
            tab.name = name;
            tab.AddToClassList("menu-tab");
            return tab;
        }

        private void ShowFinishedTab(bool shown)
        {
            finishedShown = shown;
            activeTab.EnableInClassList("selected", !shown);
            finishedTab.EnableInClassList("selected", shown);
            matchScroll.style.display = shown ? DisplayStyle.None : DisplayStyle.Flex;
            finishedScroll.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateEmpty();
            RequestMoreIfNeeded();
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
            find.Root.style.display = shown == find.Root ? DisplayStyle.Flex : DisplayStyle.None;
            Leaderboards.Root.style.display = shown == Leaderboards.Root ? DisplayStyle.Flex : DisplayStyle.None;
            showLeaderboards.EnableInClassList("selected", shown == Leaderboards.Root);
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

        private void ShowFindGame()
        {
            find.Reset();
            ShowPage("Find game", find.Root);
            FindOpened?.Invoke();
        }

        public FindGameForm FindGame => find;

        public bool LeaderboardsShown => Leaderboards.Root.style.display.value == DisplayStyle.Flex;

        private void ShowLeaderboards()
        {
            ShowPage("Leaderboards", Leaderboards.Root);
            LeaderboardsOpened?.Invoke();
        }

        // UI-498: while a search runs, only its Cancel search responds; the menu's other actions are disabled, not hidden.
        public void ShowSearching(bool value)
        {
            Root.Q("menu-actions").SetEnabled(!value);
            Root.Q<Button>("refresh-games").SetEnabled(!value);
            Challenge.Root.SetEnabled(!value);
            find.ShowSearching(value);
        }

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
            if (ReachesEnd(finishedScroll))
                MoreFinished?.Invoke();
        }

        private static bool ReachesEnd(ScrollView scroll)
        {
            return scroll.worldBound.height > 0 && scroll.verticalScroller.value >= scroll.verticalScroller.highValue - 32;
        }

        public void ShowMatches(MatchListView page)
        {
            PlaceRows(page, rows, matches);
            yourTurns = page.Matches.Count(match => match.Status == MatchStatus.YourDecision);
            ShowTurns(yourTurns);
            UpdateEmpty();
        }

        public void ShowFinished(MatchListView page)
        {
            PlaceRows(page, finishedRows, finished);
            unseenResults = page.Matches.Count(match => match.HasUnseenChanges);
            newResults.text = unseenResults.ToString();
            newResults.style.display = unseenResults > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            finishedTab.tooltip = unseenResults == 0 ? "Your finished games" : unseenResults == 1 ? "1 new result" : unseenResults + " new results";
            ShowTurns(yourTurns);
            UpdateEmpty();
        }

        private void PlaceRows(MatchListView page, Dictionary<string, SavedMatchRow> shown, VisualElement list)
        {
            var ids = new HashSet<string>(page.Matches.Select(m => m.MatchId));
            foreach (var id in shown.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                shown[id].Root.RemoveFromHierarchy();
                shown.Remove(id);
            }
            for (var index = 0; index < page.Matches.Count; index++)
            {
                var match = page.Matches[index];
                if (!shown.TryGetValue(match.MatchId, out var row))
                {
                    row = new SavedMatchRow(match, selected => OpenMatch?.Invoke(selected));
                    shown.Add(match.MatchId, row);
                }
                row.Apply(match);
                if (index >= list.childCount || list[index] != row.Root)
                    list.Insert(index, row.Root);
            }
        }

        // The My games action names how many loaded games wait for your decision, and how many results you have not seen (UI-496).
        private void ShowTurns(int turns)
        {
            var count = turns + unseenResults;
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

        // UI-368: the test list's place, kept when a scenario starts and returned to when the player leaves it.
        public TestScenarioList.Place TestScenarioPlace => testScenarios.Remember();

        public void ReturnToTestScenarios(TestScenarioList.Place place)
        {
            testScenarios.Return(place);
            ShowTestScenarioList();
        }

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

        private void UpdateEmpty()
        {
            empty.style.display = !finishedShown && rows.Count == 0 && ownRows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyFinished.style.display = finishedShown && finishedRows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
