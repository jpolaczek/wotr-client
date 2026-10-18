using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class MainMenuLayoutTests
    {
        [UnityTest]
        public IEnumerator EntryMenuHostWaitingAndSettingsFitBothSizesAndScales()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var gateway = new GatewayScreen();
                    content.Add(gateway.Root);
                    gateway.ShowAuthenticationFailure();
                    yield return Settle();
                    foreach (var field in gateway.Root.Query<TextField>().ToList()) Fits(field, content);
                    Fits(gateway.Root.Q<Button>("auth-submit"), content);
                    Fits(gateway.Root.Q<Button>("auth-create"), content);
                    Fits(gateway.Root.Q<Button>("auth-forgot"), content);
                    Fits(gateway.Root.Q<Label>("auth-browser-hint"), content);
                    Fits(gateway.Status, content);
                    CoversWithMap(gateway.Root, content);
                    CreditsClear(gateway.Root, content, gateway.Root.Q(className: "login-card"));
                    LayoutTests.Save(target, $"menu09-entry-{width}-{scale:0.00}.png");
                    client.Interface.Open();
                    yield return Settle();
                    // MENU-20: the server comes from the build's configuration, never from Settings.
                    Assert.IsNull(client.Root.Q<TextField>("settings-server"));
                    Assert.IsNull(client.Root.Q<Button>("save-server"));
                    Fits(client.Root.Q<Button>("close-interface"), client.Root);
                    LayoutTests.Save(target, $"menu09-settings-{width}-{scale:0.00}.png");
                    client.Interface.Settings.Close();
                    content.Clear();
                    var menu = new MainMenuScreen("PlayerWithLongUsername12345678901");
                    content.Add(menu.Root);
                    var matches = new MatchListView { NextCursor = "next" };
                    var rooms = new PublicRoomListView { NextCursor = "next" };
                    for (var i = 0; i < 20; i++)
                    {
                        matches.Matches.Add(new MatchSummary { MatchId = "match" + i, Name = new string('M', 64), Opponent = "AnotherPlayer", Turn = 12 });
                        rooms.Rooms.Add(new PublicRoomSummary { Id = "room" + i, Name = new string('G', 64), Host = i == 1 ? "PlayerWithLongUsername12345678901" : "AnotherPlayer", OpenSide = Side.Shadow,
                            Status = i % 2 == 0 ? RoomStatus.Waiting : RoomStatus.Started, Players = 1 + i % 2, Observers = 16, CanPlay = i % 2 == 0 });
                    }
                    menu.ShowMatches(matches);
                    menu.ShowPublic(rooms);
                    yield return Settle();
                    // OBS-04: Play and Observe stay inside every open game's row.
                    foreach (var name in new[] { "join-public-room0", "observe-public-room0", "join-public-room1", "observe-public-room1" })
                        Fits(menu.Root.Q<Button>(name), menu.Root.Q(name.Contains("room0") ? "public-room-room0" : "public-room-room1"));
                    // LOBBY-01: the chat, the players online, a name's challenge tab and a challenge to this player fit the window.
                    var lobbyState = new LobbyState();
                    var lobbyView = new LobbyView { Latest = 30 };
                    for (var i = 1; i <= 30; i++)
                        lobbyView.Messages.Add(new LobbyMessage { Sequence = i, Author = i % 2 == 0 ? "PlayerWithLongUsername12345678901" : "aragorn",
                            Text = i % 3 == 0 ? new string('w', 120) : "Anyone for a game tonight? " + i });
                    for (var i = 0; i < 12; i++)
                        lobbyView.Online.Add(new OnlinePlayer { Name = i == 0 ? "PlayerWithLongUsername12345678901" : "player" + i, You = i == 0 });
                    lobbyState.Apply(lobbyView);
                    menu.Lobby.Apply(lobbyState);
                    menu.Lobby.Status.text = "Challenge sent to PlayerWithLongUsername12345678901. Waiting for an answer…";
                    menu.Challenge.Show(new ChallengeView { Id = "c1", From = "PlayerWithLongUsername12345678901", To = "you" });
                    yield return Settle();
                    var menuWindow = menu.Root.Q("menu-window");
                    foreach (var name in new[] { "lobby-panel", "lobby-chat-log", "lobby-chat-input", "lobby-chat-send", "lobby-online-toggle", "lobby-status", "lobby-online", "challenge-tab", "challenge-accept", "challenge-decline" })
                        Fits(menu.Root.Q(name), menuWindow);
                    Fits(menu.Root.Q("public-room-room0"), menu.Root.Q<ScrollView>("public-games-scroll").contentViewport);
                    Assert.LessOrEqual(menu.Root.Q("public-room-room0").worldBound.height, 47f, "Open games rows are compact.");
                    menu.Lobby.OpenContext("player3", new Vector2(10000, 10000));
                    yield return Settle();
                    Fits(menu.Lobby.ContextTab, menu.Root.Q("lobby-panel"));
                    LayoutTests.Save(target, $"lobby01-open-games-{width}-{scale:0.00}.png");
                    menu.Lobby.CloseContext();
                    menu.Challenge.Hide();
                    // UI-262: a row's name and details stay inside the row, and the row inside the list's frame.
                    LayoutTests.Save(target, $"ui262-open-games-{width}-{scale:0.00}.png");
                    foreach (var id in new[] { "room0", "room1" })
                    {
                        var row = menu.Root.Q("public-room-" + id);
                        Fits(row, menu.Root.Q<ScrollView>("public-games-scroll").contentViewport);
                        foreach (var label in row.Query<Label>().ToList())
                            Fits(label, row);
                    }
                    // REPLAY-04: the library page in the window, with a long row and Import at the foot.
                    menu.ShowReplays();
                    menu.Replays.Show(new System.Collections.Generic.List<ReplayEntry> { new ReplayEntry { Path = "/tmp/long.wotrreplay", Manifest = new ReplayManifest
                    {
                        Name = new string('R', 64), Winner = Side.Shadow, Victory = VictoryCondition.ShadowMilitary, Turns = 12,
                        Players = { new ReplayPlayer { Side = Side.FreePeoples, Name = "PlayerWithLongUsername12345678901" }, new ReplayPlayer { Side = Side.Shadow, Name = "AnotherPlayer" } }
                    } } });
                    yield return Settle();
                    foreach (var name in new[] { "replay-play-long", "replay-export-long", "replay-delete-long" })
                        Fits(menu.Root.Q<Button>(name), menu.Root.Q("replay-long"));
                    Fits(menu.Root.Q<Button>("replay-import"), menu.Root.Q("menu-window"));
                    Fits(menu.Root.Q("replay-long"), menu.Root.Q("menu-window"));
                    foreach (var name in new[] { "show-public-games", "show-my-games", "show-replays", "host-game", "menu-demo", "join-invitation", "observe-invitation", "open-interface", "sign-out", "quit-game" })
                    {
                        Fits(menu.Root.Q<Button>(name), content);
                        Fits(menu.Root.Q<Button>(name), menu.Root.Q("menu-actions"));
                    }
                    // UI-257: Join and Observe keep their words on one line, and the code field is wide enough for its placeholder.
                    foreach (var name in new[] { "join-invitation", "observe-invitation" })
                    {
                        var button = menu.Root.Q<Button>(name);
                        var words = button.MeasureTextSize(button.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                        Assert.LessOrEqual(words.x, button.contentRect.width + 0.5f, name + " fits on one line");
                    }
                    Assert.GreaterOrEqual(menu.Root.Q<TextField>("table-invitation").worldBound.width, 84, "The code field shows its placeholder.");
                    // MENU-31: Sign out and Quit game share the last row without overlapping.
                    Assert.IsFalse(menu.Root.Q<Button>("sign-out").worldBound.Overlaps(menu.Root.Q<Button>("quit-game").worldBound));
                    Assert.That(menu.Root.Q<Button>("sign-out").worldBound.yMin, Is.EqualTo(menu.Root.Q<Button>("quit-game").worldBound.yMin).Within(.5f));
                    // MENU-14: Quit game asks first; Cancel keeps the menu, Quit asks the flow to close the game.
                    var quits = 0;
                    menu.QuitRequested += () => quits++;
                    ReturnTestFixture.Click(menu.Root.Q<Button>("quit-game"));
                    yield return Settle();
                    Assert.IsTrue(menu.Quit.IsOpen);
                    foreach (var name in new[] { "quit-confirm", "quit-cancel" })
                        Fits(menu.Root.Q<Button>(name), menu.Root.Q("quit-window"));
                    Fits(menu.Root.Q("quit-window"), content);
                    BoardMenuLayoutTests.Key(menu.Root.Q<Button>("quit-cancel"), true);
                    BoardMenuLayoutTests.Key(menu.Root.Q<Button>("quit-cancel"), false);
                    yield return Settle();
                    Assert.IsFalse(menu.Quit.IsOpen, "Escape answers the question.");
                    ReturnTestFixture.Click(menu.Root.Q<Button>("quit-game"));
                    yield return Settle();
                    ReturnTestFixture.Click(menu.Root.Q<Button>("quit-cancel"));
                    yield return Settle();
                    Assert.IsFalse(menu.Quit.IsOpen);
                    Assert.AreEqual(0, quits);
                    ReturnTestFixture.Click(menu.Root.Q<Button>("quit-game"));
                    yield return Settle();
                    ReturnTestFixture.Click(menu.Root.Q<Button>("quit-confirm"));
                    yield return Settle();
                    Assert.AreEqual(1, quits);
                    Assert.IsFalse(menu.Quit.IsOpen);
                    Fits(menu.Root.Q<TextField>("table-invitation"), content);
                    foreach (var name in new[] { "next-public", "previous-public", "next-matches", "previous-matches", "filter-All", "filter-YourDecision", "filter-Waiting", "filter-Finished" })
                        Assert.IsNull(menu.Root.Q(name), name);
                    Assert.Greater(menu.Root.Q<ScrollView>("public-games-scroll").verticalScroller.highValue, 0);
                    var window = menu.Root.Q("menu-window");
                    Fits(window, content);
                    CoversWithMap(menu.Root, content);
                    CreditsClear(menu.Root, content, window, menu.Root.Q("menu-actions"), menu.Status);
                    var size = window.worldBound.size;
                    LayoutTests.Save(target, $"menu09-main-{width}-{scale:0.00}.png");
                    // MENU-11: My games replaces the open games in the same window; the window keeps its size.
                    ReturnTestFixture.Click(menu.Root.Q<Button>("show-my-games"));
                    yield return Settle();
                    Assert.Greater(menu.Root.Q<ScrollView>("my-games-scroll").verticalScroller.highValue, 0);
                    Assert.AreEqual(DisplayStyle.None, menu.Root.Q("public-games").resolvedStyle.display);
                    Assert.AreEqual("My games", menu.Root.Q<Label>("menu-window-title").text);
                    Assert.AreEqual(size, window.worldBound.size);
                    Fits(menu.Root.Q<Button>("resume-match-match0"), window);
                    LayoutTests.Save(target, $"menu11-mine-{width}-{scale:0.00}.png");
                    ReturnTestFixture.Click(menu.Root.Q<Button>("host-game"));
                    yield return Settle();
                    // MENU-30: every control of Host a game in its place inside the window, nothing overlapping.
                    var form = menu.Root.Q("host-game-form");
                    var parts = new VisualElement[] { menu.Root.Q<TextField>("room-name"), menu.Root.Q<Button>("host-fp"),
                        menu.Root.Q<Button>("host-shadow"), menu.Root.Q<Button>("host-public"), menu.Root.Q<Button>("host-private"),
                        menu.Root.Q<Label>("host-visibility-note"), menu.Root.Q<Button>("cancel-host"), menu.Root.Q<Button>("submit-host") };
                    foreach (var part in parts)
                        Fits(part, window);
                    for (var i = 0; i < parts.Length; i++)
                        for (var j = i + 1; j < parts.Length; j++)
                            Assert.IsFalse(parts[i].worldBound.Overlaps(parts[j].worldBound), parts[i].name + " overlaps " + parts[j].name);
                    Assert.Less(menu.Root.Q<Button>("cancel-host").worldBound.xMax, menu.Root.Q<Button>("submit-host").worldBound.xMin,
                        "Back on the left, Create game on the right.");
                    Assert.IsNull(form.Q<Toggle>(), "Visibility is a pair of segments, not a toggle.");
                    ReturnTestFixture.Click(menu.Root.Q<Button>("host-shadow"));
                    yield return Settle();
                    Assert.IsTrue(menu.Root.Q<Button>("host-shadow").ClassListContains("selected"), "A click on a side card chooses it.");
                    Assert.IsFalse(menu.Root.Q<Button>("host-fp").ClassListContains("selected"));
                    ReturnTestFixture.Click(menu.Root.Q<Button>("host-fp"));
                    yield return Settle();
                    // MENU-31: the demo page asks only for the side, with its cards in the same window.
                    ReturnTestFixture.Click(menu.Root.Q<Button>("menu-demo"));
                    yield return Settle();
                    Assert.AreEqual("Demo vs CPU", menu.Root.Q<Label>("menu-window-title").text);
                    foreach (var part in new[] { "demo-fp", "demo-shadow", "cancel-demo" })
                        Fits(menu.Root.Q<Button>(part), window);
                    Assert.AreEqual(size, window.worldBound.size);
                    Fits(menu.Root.Q<Button>("menu-demo"), menu.Root.Q("menu-actions"));
                    Fits(menu.Root.Q<Button>("quit-game"), menu.Root.Q("menu-actions"));
                    Assert.AreEqual(size, window.worldBound.size);
                    LayoutTests.Save(target, $"menu09-host-{width}-{scale:0.00}.png");
                    content.Clear();
                    var waiting = new WaitingRoomScreen();
                    content.Add(waiting.Root);
                    waiting.Update(new RoomView { Id = "room", Name = new string('N', 64), Host = "PlayerWithLongUsername12345678901",
                        IsHost = true, Managed = true, Visibility = RoomVisibility.Public, FreePeoples = "PlayerWithLongUsername12345678901",
                        JoinCode = new string('A', 43), ObserverLimit = 16, Observers = Audience() });
                    waiting.Status.text = "Connection interrupted. Your seat is kept. Retry Refresh when the server is available.";
                    // MENU-26: the chat in its place, with a long line and markup shown as plain text.
                    var chat = new RoomChatPanel();
                    waiting.ChatHost.Add(chat.Root);
                    var markup = "<b>bold</b> " + new string('w', 150);
                    var lines = new RoomChatView { Latest = 12 };
                    for (var i = 1; i <= 12; i++)
                        lines.Messages.Add(new RoomChatMessage { Sequence = i, Author = "PlayerWithLongUsername12345678901",
                            Side = i % 2 == 0 ? Side.Shadow : Side.FreePeoples, Text = i == 12 ? markup : "Message " + i });
                    chat.Show(lines);
                    yield return Settle();
                    // MENU-25: the waiting room's window is the main menu's window; every control keeps its place inside it.
                    var waitingWindow = waiting.Root.Q("waiting-window");
                    Assert.AreEqual(size, waitingWindow.worldBound.size, "The waiting room uses the menu's frame.");
                    foreach (var name in new[] { "waiting-back", "room-ready", "start-game", "room-demo", "room-observe", "open-interface" })
                        Fits(waiting.Root.Q<Button>(name), waiting.Root.Q("waiting-actions"));
                    foreach (var name in new[] { "generate-room-invitation", "copy-room-invitation", "refresh-room", "room-chat-send" })
                        Fits(waiting.Root.Q<Button>(name), waitingWindow);
                    foreach (var name in new[] { "seat-free", "seat-shadow", "waiting-observers", "room-invitation", "room-chat-input", "room-chat-log" })
                        Fits(waiting.Root.Q(name), waitingWindow);
                    // OBS-04: Take seat and Remove at the foot of each seat, side by side; the audience line in its height.
                    foreach (var key in new[] { "free", "shadow" })
                    {
                        Fits(waiting.Root.Q<Button>("seat-" + key + "-take"), waiting.Root.Q("seat-" + key));
                        Fits(waiting.Root.Q<Button>("seat-" + key + "-remove"), waiting.Root.Q("seat-" + key));
                        Assert.IsFalse(waiting.Root.Q<Button>("seat-" + key + "-take").worldBound.Overlaps(waiting.Root.Q<Button>("seat-" + key + "-remove").worldBound));
                    }
                    Fits(waiting.Root.Q<Button>("remove-observer-Observer01"), waiting.Root.Q("waiting-observers"));
                    Assert.AreEqual(52, waiting.Root.Q("waiting-observers").resolvedStyle.height, 0.5f);
                    Assert.AreEqual(waiting.Root.Q("seat-free").worldBound.width, waiting.Root.Q("seat-shadow").worldBound.width, 0.5f, "Two equal seats.");
                    Assert.AreEqual(waiting.Root.Q("seat-free").worldBound.height, waiting.Root.Q("seat-shadow").worldBound.height, 0.5f, "Two equal seats.");
                    Assert.AreEqual("Back", waiting.Root.Q<Button>("waiting-back").text);
                    Assert.AreEqual(markup, waiting.Root.Q("room-chat-message-12").Q<Label>(className: "room-chat-text").text);
                    Assert.IsFalse(waiting.Root.Q("room-chat-message-12").Q<Label>(className: "room-chat-text").enableRichText);
                    Fits(waiting.Status, content);
                    CoversWithMap(waiting.Root, content);
                    CreditsClear(waiting.Root, content, waitingWindow, waiting.Root.Q("waiting-actions"), waiting.Status);
                    LayoutTests.Save(target, $"menu09-waiting-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = null;
                    target.Release();
                    Object.Destroy(target);
                }
            }
        }
        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++) yield return null;
        }
        // MENU-15: the map backdrop is drawn and covers the whole screen behind its content.
        private static void CoversWithMap(VisualElement screen, VisualElement content)
        {
            var backdrop = screen.Q("frontend-backdrop");
            Assert.IsNotNull(backdrop.resolvedStyle.backgroundImage.texture, screen.name);
            Assert.AreEqual(content.worldBound.width, backdrop.worldBound.width, 1, screen.name);
            Assert.AreEqual(content.worldBound.height, backdrop.worldBound.height, 1, screen.name);
        }

        // MENU-16: the credit line is whole on the screen and stays clear of the screen's panels.
        private static void CreditsClear(VisualElement screen, VisualElement content, params VisualElement[] panels)
        {
            var credits = screen.Q<Label>("frontend-credits");
            Fits(credits, content);
            var text = credits.MeasureTextSize(credits.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(text.x, credits.worldBound.width, "The credit line is not cut.");
            foreach (var panel in panels)
                Assert.IsFalse(Overlap(credits, panel), panel.name + " · " + string.Join(" ", panel.GetClasses()));
        }

        // The text's own band, centred in the full-width line.
        private static bool Overlap(Label credits, VisualElement panel)
        {
            var text = credits.MeasureTextSize(credits.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            var band = new Rect(credits.worldBound.center.x - text.x / 2, credits.worldBound.yMin, text.x, credits.worldBound.height);
            return band.Overlaps(panel.worldBound);
        }

        private static void Fits(VisualElement element, VisualElement bounds)
        {
            Assert.IsNotNull(element);
            Assert.Greater(element.worldBound.height, 0, element.name);
            Assert.Greater(element.worldBound.width, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.worldBound.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
        }

        // OBS-04: a full audience with names of different lengths.
        private static System.Collections.Generic.List<RoomObserverView> Audience()
        {
            var audience = new System.Collections.Generic.List<RoomObserverView>();
            for (var i = 1; i <= 16; i++)
                audience.Add(new RoomObserverView { Name = i % 3 == 0 ? "ObserverWithAVeryLongName" + i : "Observer" + i.ToString("D2"), IsHost = i == 2 });
            return audience;
        }
    }
}
