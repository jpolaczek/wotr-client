using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ClientFlow : IDisposable
    {
        private readonly VisualElement root;
        private readonly WindowLayout windows;
        private GameApi api;
        private CancellationTokenSource screenLifetime = new CancellationTokenSource();
        private readonly string storageRoot;
        private readonly Func<string, string, string, ILocalMatchStorage> localStorage;
        private readonly Action quit;
        private readonly SoundModule sounds;
        private MatchSession matchSession;
        // OBS-05: the live board of a game one watches; never at the same time as a player's match session.
        private ObserverSession observer;
        public ObserverSession Observer => observer;
        // REPLAY-04/05: the replays of this installation, the library page's actions, a replay on the board and where
        // its Back leads, with the row and scroll to show again.
        private readonly string replaysFolder;
        private ReplayLibrary replayLibrary;
        private ReplayLibraryController libraryController;
        private ReplaySession replay;
        private float libraryScroll;
        public ReplayLibrary Replays => replayLibrary ??= new ReplayLibrary(replaysFolder);
        public ReplaySession Replay => replay;
        public IReplayFilePicker FilePicker { get; set; } = new NativeReplayFilePicker();
        private TestSession testSession;
        private MainMenuBrowser lobbyBrowser;
        private LobbyController lobbyController;
        private MainMenuScreen mainMenu;
        private Task localWrites = Task.CompletedTask;
        private WaitingRoomController waitingRoom;
        private GatewayScreen authScreen;
        public bool IsAuthenticated => api != null && api.AccountId.Length > 0;
        private Label errorLabel;
        private bool busy;
        private bool disposed;
        public event Action InterfaceRequested;
        public Task LocalWrites => matchSession?.LocalWrites ?? localWrites;
        public BoardScreen Board
        {
            get; private set;
        }

        // MENU-14: quit closes the game after a confirmed Quit game (ApplicationQuit unless a test replaces it).
        public ClientFlow(VisualElement root, GameApi authenticatedApi = null, string storageRoot = null, WindowLayout windows = null,
            Func<string, string, string, ILocalMatchStorage> localStorage = null, Action quit = null, SoundModule sounds = null)
        {
            this.sounds = sounds;
            this.root = root;
            this.quit = quit ?? ApplicationQuit.Exit;
            this.windows = windows ?? new WindowLayout();
            replaysFolder = storageRoot == null ? ReplayLibrary.DefaultFolder : Path.Combine(storageRoot, "Replays");
            this.storageRoot = storageRoot ?? Path.Combine(Application.persistentDataPath, "match-recovery");
            this.localStorage = localStorage ?? ((server, account, match) => new LocalMatchFiles(this.storageRoot, server, account, match));
            api = authenticatedApi;
            if (api != null && api.AccountId.Length > 0)
                Perform(ShowLobby);
            else
                ShowLogin();
        }

        private void ShowLogin()
        {
            ChangeScreen();
            authScreen = new GatewayScreen();
            errorLabel = authScreen.Status;
            root.Add(authScreen.Root);
            authScreen.InterfaceRequested += OpenInterface;
            authScreen.ReplaysRequested += () => ShowReplayLibrary();
            // AUTH-03/AUTH-05: the game only signs in, with the account name; accounts are made on the game's site.
            authScreen.Submitted += (login, password) => Perform(async () =>
            {
                api?.Dispose();
                // MENU-20: the build's configuration names the server.
                api = new GameApi(ServerConfiguration.Address);
                await api.Login(login, password, screenLifetime.Token);
                authScreen.ClearPassword();
                await ShowLobby();
            });
        }

        // MENU-25: the main menu with a notice in its status line (a player removed from a room).
        private async Task ShowLobby(string notice)
        {
            await ShowLobby();
            if (lobbyBrowser != null)
                lobbyBrowser.Notice = notice;
            if (mainMenu != null)
                mainMenu.Status.text = notice;
        }

        private Task ShowLobby()
        {
            ChangeScreen();
            var lobby = new MainMenuScreen(api.Username);
            mainMenu = lobby;
            errorLabel = lobby.Status;
            root.Add(lobby.Root);
            lobby.InterfaceRequested += OpenInterface;
            lobby.Create += request => Perform(async () => ShowRoom(await api.Post<RoomView>("api/rooms", request, screenLifetime.Token)));
            lobby.Demo += side => Perform(() => StartDemo(side));
            lobby.Join += (code, role) => Perform(async () => ShowRoom(await api.Post<RoomView>("api/rooms/join",
                new JoinRoomRequest { Code = code, Role = role }, screenLifetime.Token)));
            lobby.JoinPublic += (id, role) => Perform(async () => ShowRoom(await api.Post<RoomView>(
                $"api/rooms/{id}/join" + (role == RoomRole.Observer ? "?role=Observer" : ""), null, screenLifetime.Token)));
            lobby.Open += ShowRoom;
            lobby.OpenMatch += match => Perform(() => ShowMatch(match.MatchId));
            lobby.Logout += () => Perform(async () => { try { await api.Logout(screenLifetime.Token); } finally { ShowLogin(); } });
            lobby.QuitRequested += quit;
            lobby.StartScenario += id => Perform(() => StartTestScenario(id));
            lobby.AccountName.Submitted += (name, password) => _ = RenameAccount(lobby, name, password);
            libraryController = new ReplayLibraryController(Replays, lobby.Replays, FilePicker, entry => PlayReplay(entry, false));
            lobby.ReplaysOpened += libraryController.Refresh;
            lobbyBrowser = new MainMenuBrowser(api, lobby);
            lobbyBrowser.Refresh();
            lobbyController = new LobbyController(api, lobby.Lobby, lobby.Challenge);
            lobbyController.RoomOpened += ShowRoom;
            _ = LoadTestScenarios(lobby, screenLifetime.Token);
            return Task.CompletedTask;
        }

        // AUTH-06: the server checks the name again and the password; a refusal stays in the window. After a change the
        // menu is built again, so every default (e.g. a new game's name) uses the new name.
        private async Task RenameAccount(MainMenuScreen lobby, string name, string password)
        {
            lobby.AccountName.Busy(true);
            try
            {
                var renamed = await api.RenameAccount(name, password, screenLifetime.Token);
                lobby.AccountName.Close();
                await ShowLobby("Your account name is now " + renamed + ". Sign in with it from now on.");
            }
            catch (ApiException error)
            {
                lobby.AccountName.Refused(error.Status == 429 && error.Message != SignInLock.Error ? "TooManyRequests" : error.Message,
                    error.RetryAfter);
            }
            catch (Exception) when (!screenLifetime.IsCancellationRequested)
            {
                lobby.AccountName.Refused("");
            }
            finally
            {
                lobby.AccountName.Busy(false);
            }
        }

        // MENU-31: the menu's demo makes a private room on the chosen side, starts the demo against the CPU from it (DEMO-04)
        // and lets the room go: an empty room is deleted (MENU-32), and the demo opens like any started game.
        private async Task StartDemo(Side side)
        {
            var token = screenLifetime.Token;
            var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest
                { Name = "Demo", Side = side, Visibility = RoomVisibility.Private }, token);
            DemoMatchView demo;
            try
            {
                demo = await api.Post<DemoMatchView>("api/rooms/" + room.Id + "/demo", null, token);
            }
            finally
            {
                await LeaveRoomQuietly(room.Id, token);
            }
            await ShowMatch(demo.MatchId);
        }

        // A room that is already gone needs no leaving; any other failure is left to the sweeper (MENU-27).
        private async Task LeaveRoomQuietly(string room, CancellationToken token)
        {
            try
            {
                await api.Post<object>("api/rooms/" + room + "/leave", null, token);
            }
            catch (ApiException) { }
            catch (HttpRequestException) { }
        }

        // TEST-03: an optional server feature; when the catalog is missing or unreadable the menu stays as it is.
        private async Task LoadTestScenarios(MainMenuScreen lobby, CancellationToken cancellation)
        {
            try
            {
                var catalog = await TestScenarioApi.Catalog(api, cancellation);
                if (catalog != null && mainMenu == lobby)
                    lobby.ShowTestScenarios(catalog);
            }
            catch (Exception error) when (error is ApiException || error is HttpRequestException || error is OperationCanceledException) { }
        }

        private async Task StartTestScenario(string scenario)
        {
            var view = await TestScenarioApi.Start(api, scenario, screenLifetime.Token);
            CloseMatch(false);
            testSession?.Dispose();
            testSession = new TestSession(api, view);
            await ShowTestMatch(testSession);
        }

        // A test game opens like a saved game, without local drafts or read markers, under the test bar.
        private Task ShowTestMatch(TestSession session) => OpenBoard(session.View.MatchId, session.Api, board =>
        {
            board.TestBar.Show(session.View.Scenario, session.Side);
            board.TestBar.SwitchSide += () => Perform(() => SwitchTestSide(session));
            board.TestBar.Restart += () => Perform(() => StartTestScenario(session.View.Scenario.Id));
            board.TestBar.Exit += () => Perform(EndTestScenario);
            board.Menu.GamesRequested += _ => Perform(EndTestScenario);
            board.Menu.QuitRequested += _ => Perform(QuitTestScenario);
            board.GameDeleted.MenuRequested += () => Perform(EndTestScenario);
        }, false);

        private Task SwitchTestSide(TestSession session)
        {
            if (session != testSession)
                return Task.CompletedTask;
            session.SwitchSide();
            return ShowTestMatch(session);
        }

        private async Task EndTestScenario()
        {
            await CloseTestScenario();
            await ShowLobby();
        }

        // MENU-14: Quit game in a test scenario ends it on the server before the game closes.
        private async Task QuitTestScenario()
        {
            try
            {
                await CloseTestScenario();
            }
            finally
            {
                quit();
            }
        }

        private async Task CloseTestScenario()
        {
            CloseMatch(false);
            try
            {
                await TestScenarioApi.End(api, screenLifetime.Token);
            }
            finally
            {
                testSession?.Dispose();
                testSession = null;
            }
        }

        private void ShowRoom(RoomView room)
        {
            // OBS-05: a room one watches whose game is going on opens its board, not the waiting room.
            if (room.Role == RoomRole.Observer && room.Status == RoomStatus.Started && room.MatchId.Length > 0)
            {
                _ = ObserveMatch(room);
                return;
            }
            ChangeScreen();
            var waiting = new WaitingRoomScreen();
            errorLabel = waiting.Status;
            root.Add(waiting.Root);
            waiting.InterfaceRequested += OpenInterface;
            waitingRoom = new WaitingRoomController(api, waiting, room, ShowMatch, ShowLobby, () =>
            {
                api?.Dispose();
                api = null;
                ShowLogin();
            }, removed: () => ShowLobby("The host removed you from this room."), observe: ObserveMatch);
        }

        // REPLAY-04: Replays before sign-in — no account, no server.
        private void ShowReplayLibrary()
        {
            ChangeScreen();
            var screen = new ReplayLibraryScreen();
            errorLabel = screen.Page.Status;
            root.Add(screen.Root);
            screen.Page.Back += ShowLogin;
            libraryController = new ReplayLibraryController(Replays, screen.Page, FilePicker, entry => PlayReplay(entry, true));
            libraryController.Refresh();
            screen.Page.ScrollOffset = libraryScroll;
        }

        // REPLAY-05: a saved replay on the board; Exit match leads back to the library it came from.
        private void PlayReplay(ReplayEntry entry, bool beforeSignIn)
        {
            ReplayArchive archive;
            try
            {
                archive = ReplayArchive.Open(entry.Path);
            }
            catch (Exception error) when (error is ReplayFormatException || error is IOException || error is UnauthorizedAccessException)
            {
                if (errorLabel != null)
                    errorLabel.text = error.Message;
                return;
            }
            libraryScroll = mainMenu?.Replays.ScrollOffset ?? libraryScroll;
            ChangeScreen();
            var board = new BoardScreen(windows);
            Board = board;
            errorLabel = board.Connection;
            root.Add(board.Root);
            board.InterfaceRequested += OpenInterface;
            board.Menu.GamesRequested += _ => LeaveReplay(beforeSignIn);
            board.Menu.QuitRequested += _ =>
            {
                CloseMatch(false);
                quit();
            };
            board.GameDeleted.MenuRequested += () => LeaveReplay(beforeSignIn);
            replay = new ReplaySession(archive, board, soundModule: sounds);
        }

        private void LeaveReplay(bool beforeSignIn)
        {
            CloseMatch(false);
            if (beforeSignIn || api == null || api.AccountId.Length == 0)
            {
                ShowReplayLibrary();
                return;
            }
            Perform(async () =>
            {
                await ShowLobby();
                mainMenu?.ShowReplays();
                if (mainMenu != null)
                    mainMenu.Replays.ScrollOffset = libraryScroll;
            });
        }

        // OBS-05: an observer's board for the room's match. Exit match, Quit game and the end notice free the place in
        // the audience; nothing on this board sends a command.
        private Task ObserveMatch(RoomView room)
        {
            ChangeScreen();
            var board = new BoardScreen(windows);
            Board = board;
            errorLabel = board.Connection;
            root.Add(board.Root);
            board.InterfaceRequested += OpenInterface;
            board.SetConnection("Connecting");
            board.Menu.GamesRequested += _ => Perform(() => LeaveObserving(room.Id, false));
            board.Menu.QuitRequested += _ => Perform(() => LeaveObserving(room.Id, true));
            board.GameDeleted.MenuRequested += () => Perform(() => LeaveObserving(room.Id, false));
            observer = new ObserverSession(api, room, board, replays: Replays, soundModule: sounds);
            board.Reconnect += () =>
            {
                if (observer?.Expired == true)
                    ShowLogin();
                else
                    observer?.Reconnect();
            };
            return Task.CompletedTask;
        }

        private async Task LeaveObserving(string room, bool quitting)
        {
            var expired = observer?.Expired == true;
            var token = screenLifetime.Token;
            CloseMatch(false);
            if (!expired)
                await LeaveRoomQuietly(room, token);
            if (quitting)
                quit();
            else if (expired)
                ShowLogin();
            else
                await ShowLobby();
        }

        private Task ShowMatch(string matchId) =>
            OpenBoard(matchId, api, board =>
            {
                board.Menu.GamesRequested += retry => RequestLeaveMatch(board, retry, false);
                board.Menu.QuitRequested += retry => RequestLeaveMatch(board, retry, true);
                board.GameDeleted.MenuRequested += () => Perform(LeaveDeletedMatch);
            }, true);

        // The board of one match seen through one session; only saved games keep local drafts and read markers.
        private async Task OpenBoard(string matchId, GameApi matchApi, Action<BoardScreen> configure, bool persistent)
        {
            ChangeScreen();
            var cancellation = screenLifetime.Token;
            var board = new BoardScreen(windows);
            Board = board;
            errorLabel = board.Connection;
            root.Add(board.Root);
            board.InterfaceRequested += OpenInterface;
            board.SetConnection("Connecting");
            configure(board);
            board.Reconnect += () =>
            {
                if (matchSession?.Expired == true)
                    ShowLogin();
                else if (matchSession != null)
                    matchSession.Reconnect();
                else
                    Perform(() => OpenBoard(matchId, matchApi, configure, persistent));
            };
            await localWrites;
            cancellation.ThrowIfCancellationRequested();
            var persistence = persistent
                ? await LocalMatchPersistence.Open(localStorage(matchApi.Address.AbsoluteUri, matchApi.AccountId, matchId))
                : null;
            cancellation.ThrowIfCancellationRequested();
            matchSession = new MatchSession(matchApi, matchId, board, persistence, sounds, Replays);
        }

        private async void RequestLeaveMatch(BoardScreen board, bool retry, bool quitting)
        {
            var request = board.Menu.Request;
            try { await LeaveMatch(board, request, retry, quitting); }
            catch (OperationCanceledException) when (disposed || Board != board) { }
            catch (Exception error)
            {
                if (!disposed && Board == board && board.Menu.Request == request)
                    board.Menu.SetSaving(false, ErrorText(error));
            }
        }

        // Games and Quit game both save the recovery data first; a failed save keeps the board with Retry.
        private async Task LeaveMatch(BoardScreen board, int request, bool retry, bool quitting)
        {
            var session = matchSession;
            board.Menu.SetSaving(true);
            do
            {
                var error = session == null ? "" : await session.Exit.Prepare(retry);
                if (disposed || Board != board || board.Menu.Request != request)
                    return;
                if (error.Length > 0)
                {
                    board.Menu.SetSaving(false, error);
                    return;
                }
                retry = false;
                // Check again in this continuation before disposing: a live update may
                // have queued another write after the previous await completed.
            } while (session != null && !session.Exit.Ready);
            var expired = session?.Expired == true;
            CloseMatch(false);
            if (quitting)
                quit();
            else if (expired)
                ShowLogin();
            else
                await ShowLobby();
        }

        private Task LeaveDeletedMatch()
        {
            CloseMatch(false);
            return ShowLobby();
        }

        // DIST-07: Restart and update and Decline and quit save the open game the way Quit game does; an error keeps
        // the game open for Retry. Nothing is sent to the server: no LeaveRoom or CancelGame, and a pending command
        // keeps its journal and commandId for the receipt check after the restart.
        public async Task<string> SaveForExit(bool retry)
        {
            var session = matchSession;
            while (session != null && !disposed)
            {
                var error = await session.Exit.Prepare(retry);
                if (error.Length > 0)
                    return error;
                if (session != matchSession || session.Exit.Ready)
                    break;
                retry = false;
            }
            return "";
        }

        // Right before the game quits: the saved match closes and its last local writes finish.
        public async Task CloseForExit()
        {
            CloseMatch(false);
            await LocalWrites;
        }

        private async void Perform(Func<Task> operation)
        {
            if (busy || disposed)
                return;
            busy = true;
            mainMenu?.Busy(true);
            authScreen?.Busy(true);
            if (errorLabel != null)
                errorLabel.text = "";
            try
            {
                await operation();
            }
            catch (OperationCanceledException) when (disposed || screenLifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (authScreen != null && error is ApiException apiError && apiError.Message == "InvalidCredentialsOrInvitation")
                    authScreen.ShowAuthenticationFailure();
                else if (errorLabel != null)
                    errorLabel.text = ErrorText(error);
                else
                    Board?.SetConnection("Reconnecting");
            }
            finally
            {
                busy = false;
                mainMenu?.Busy(false);
                authScreen?.Busy(false);
            }
        }

        private void OpenInterface() => InterfaceRequested?.Invoke();

        private static string ErrorText(Exception error)
        {
            if (error is ArgumentException || error is LocalSaveException)
                return error.Message;
            if (!(error is ApiException apiError))
                return "Connection interrupted. Check the server address and try again.";
            switch (apiError.Message)
            {
                case "StaleConfiguration":
                    return "The table settings changed. Review the latest configuration.";
                case "RoomFull":
                    return "Both seats at this table are taken.";
                // OBS-04: joining or watching a game with its seats and audience.
                case "ObserversFull":
                    return "All observer places in this game are taken.";
                case "MatchFinished":
                    return "This game has ended. It no longer takes observers.";
                case "RemovedFromRoom":
                    return "The host removed you from this room.";
                case "MatchAlreadyStarted":
                    return "This game has started. You can still observe it.";
                case "ContentNotImplemented":
                    return "These expansion rules are not available yet.";
                case "ExpansionsUnavailable":
                    return "This server offers only the base game.";
                case "ConfirmationRequired":
                    return "Both players must accept this configuration.";
                // QUAL-05: a busy database is temporary; a pending command keeps its id for "Check result".
                case "ServerBusy":
                    return "The server is busy. Try again in a moment.";
                case SignInLock.Error:
                    return SignInLock.Text(apiError.RetryAfter);
                // QUAL-02: sign-in and game budgets share one message; the server says when to retry.
                case "TooManyRequests":
                    return apiError.RetryAfter.HasValue
                        ? $"Too many requests. Try again in {Math.Max(1, (int)Math.Ceiling(apiError.RetryAfter.Value.TotalSeconds))} seconds."
                        : "Too many requests. Try again in a minute.";
                case "UnsupportedSaveVersion":
                    return "This game needs a compatible server version. Your saved game is unchanged.";
                case "CorruptSave":
                    return "This saved game could not be opened. It needs to be restored by the server administrator.";
                case ClientCompatibility.ClientUpdateRequired:
                case ClientCompatibility.ServerUpdateRequired:
                    return VersionRefusal.Text(apiError.Message);
                case ClientCompatibility.InvalidClientVersion:
                    return "This copy of the game could not identify its version. Reinstall it from the download page.";
                case "ScenarioNotFound":
                    return "This test scenario is not available on this server.";
            }
            return apiError.Status == 401 ? "Session expired. Return to sign in."
                : apiError.Status == 403 ? "This account may not do that."
                : apiError.Status == 404 ? "Table unavailable. Check your invitation."
                : apiError.Status == 429 ? "Too many requests. Try again in a minute." : "Request declined. Refresh and try again.";
        }

        private void ChangeScreen()
        {
            screenLifetime.Cancel();
            screenLifetime.Dispose();
            screenLifetime = new CancellationTokenSource();
            CloseMatch();
            waitingRoom?.Dispose();
            waitingRoom = null;
            lobbyBrowser?.Dispose();
            lobbyBrowser = null;
            lobbyController?.Dispose();
            lobbyController = null;
            libraryController?.Dispose();
            libraryController = null;
            mainMenu = null;
            authScreen = null;
            Board = null;
            errorLabel = null;
            root.Clear();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            screenLifetime.Cancel();
            screenLifetime.Dispose();
            CloseMatch();
            waitingRoom?.Dispose();
            waitingRoom = null;
            lobbyBrowser?.Dispose();
            lobbyController?.Dispose();
            testSession?.Dispose();
            api?.Dispose();
        }

        private void CloseMatch(bool saveDraft = true)
        {
            observer?.Dispose();
            observer = null;
            replay?.Dispose();
            replay = null;
            if (matchSession == null)
                return;
            matchSession.Dispose(saveDraft);
            localWrites = matchSession.LocalWrites;
            matchSession = null;
        }
    }
}
