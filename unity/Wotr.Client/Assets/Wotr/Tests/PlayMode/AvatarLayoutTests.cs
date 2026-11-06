using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-505, UI-506: every place of AVATAR-01 A8 shows the right picture in the four standard layouts, and no row grows for
    // it, measured against the same row with no avatar.
    public class AvatarLayoutTests
    {
        private const string LongName = "PlayerWithLongUsername12345678901";

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }

        private static IEnumerable<(int Width, int Height, float Scale)> Layouts()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    yield return (width, width == 1366 ? 768 : 1080, scale);
        }

        private static void Inside(VisualElement element, VisualElement bounds)
        {
            Assert.IsNotNull(element);
            Assert.Greater(element.worldBound.width, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
        }

        // One physical pixel: a panel scaled to 125 % rounds equal rows up to that far apart.
        private static void SameHeight(VisualElement plain, VisualElement pictured, string what) =>
            Assert.AreEqual(plain.worldBound.height, pictured.worldBound.height, 1f, what + " keeps its height.");

        // A name beside its picture has its full size, stands after the picture and on its line.
        private static void Reads(Label name)
        {
            Assert.IsNotNull(name);
            Assert.Greater(name.worldBound.width, 20, "The name has room: " + name.name);
            Assert.Greater(name.worldBound.height, 10, "The name has its height: " + name.name);
            var image = Picture(name.parent);
            Assert.IsNotNull(image, "A picture beside " + name.name);
            Assert.LessOrEqual(image.worldBound.xMax, name.worldBound.xMin + 0.6f, "The name stands after the picture.");
            Assert.Greater(image.worldBound.width, 10, "The picture has its size.");
            Assert.AreEqual(image.worldBound.center.y, name.worldBound.center.y, 2f, "On the same line.");
        }

        private static VisualElement Picture(VisualElement holder) => holder.Q(className: "avatar-image");

        [UnityTest]
        public IEnumerator TheMenuShowsEachPlayersPictureAndNoRowGrows()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var (width, height, scale) in Layouts())
                {
                    var target = new RenderTexture(width, height, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var menu = new MainMenuScreen(LongName, "gondor");
                    content.Add(menu.Root);
                    var rooms = new PublicRoomListView();
                    rooms.Rooms.Add(new PublicRoomSummary { Id = "plain", Name = "A plain game", Host = LongName, Status = RoomStatus.Waiting });
                    rooms.Rooms.Add(new PublicRoomSummary { Id = "pictured", Name = "A game", Host = LongName, HostAvatar = "rohan", Status = RoomStatus.Waiting });
                    menu.ShowPublic(rooms);
                    var state = new LobbyState();
                    var view = new LobbyView { Latest = 2 };
                    view.Messages.Add(new LobbyMessage { Sequence = 1, Author = LongName, Text = "No picture" });
                    view.Messages.Add(new LobbyMessage { Sequence = 2, Author = LongName, AuthorAvatar = "elves", Text = "A picture" });
                    view.Online.Add(new OnlinePlayer { Name = "plainplayer" });
                    view.Online.Add(new OnlinePlayer { Name = LongName, Avatar = "dwarves" });
                    state.Apply(view);
                    menu.Lobby.Apply(state);
                    menu.Challenge.Show(new ChallengeView { Id = "c1", From = LongName, FromAvatar = "north", To = "you" });
                    yield return Settle();

                    Assert.AreEqual("gondor", AvatarImage.Shown(menu.Root.Q("menu-player").parent));
                    Reads(menu.Root.Q<Label>("menu-player"));
                    Assert.AreEqual("rohan", AvatarImage.Shown(menu.Root.Q("public-room-pictured")));
                    Assert.IsNull(AvatarImage.Shown(menu.Root.Q("public-room-plain")));
                    SameHeight(menu.Root.Q("public-room-plain"), menu.Root.Q("public-room-pictured"), "An open games row");
                    Assert.AreEqual("elves", AvatarImage.Shown(menu.Root.Q("lobby-message-2")));
                    SameHeight(menu.Root.Q("lobby-message-1"), menu.Root.Q("lobby-message-2"), "A lobby chat line");
                    var authors = menu.Root.Q("lobby-message-2").Query<Label>("lobby-author").ToList();
                    Reads(authors[0]);
                    Assert.Less(authors[0].worldBound.xMax, 900, "The author stays in the chat.");
                    var message = menu.Root.Q("lobby-message-2").Query<Label>(className: "lobby-text").First();
                    Assert.IsFalse(authors[0].worldBound.Overlaps(message.worldBound), "The text does not lie over the name.");
                    var pictured = menu.Root.Q<Label>("lobby-player-" + LongName);
                    Assert.AreEqual("dwarves", AvatarImage.Shown(pictured.parent));
                    Reads(pictured);
                    SameHeight(menu.Root.Q("lobby-player-plainplayer"), pictured, "An online row");
                    Inside(pictured, menu.Root.Q("lobby-online"));
                    Inside(Picture(pictured.parent), menu.Root.Q("lobby-online"));
                    var host = menu.Root.Q("public-room-pictured").Query<Label>(className: "menu-row-detail").First();
                    Reads(host);
                    Inside(host, menu.Root.Q("public-room-pictured"));
                    Assert.AreEqual("north", AvatarImage.Shown(menu.Challenge.Root));
                    Inside(menu.Root.Q<Label>("challenge-text"), menu.Challenge.Root);
                    Inside(menu.Root.Q<Button>("challenge-decline"), menu.Challenge.Root);
                    LayoutTests.Save(target, $"ui505-open-games-{width}-{scale:0.00}.png");

                    menu.Challenge.Hide();
                    ReturnTestFixture.Click(menu.Root.Q<Button>("show-my-games"));
                    var matches = new MatchListView();
                    matches.Matches.Add(new MatchSummary { MatchId = "plain", Opponent = LongName, Status = MatchStatus.Waiting });
                    matches.Matches.Add(new MatchSummary { MatchId = "unnamed", Opponent = LongName, OpponentAvatar = "sauron", Status = MatchStatus.Waiting });
                    matches.Matches.Add(new MatchSummary { MatchId = "named", Name = "A long game", Opponent = LongName, OpponentAvatar = "isengard", Status = MatchStatus.Waiting });
                    menu.ShowMatches(matches);
                    yield return Settle();
                    Assert.AreEqual("sauron", AvatarImage.Shown(menu.Root.Q("saved-match-unnamed")));
                    Reads(menu.Root.Q("saved-match-unnamed").Query<Label>(className: "saved-match-heading").First());
                    Reads(menu.Root.Q("saved-match-named").Query<Label>(className: "saved-match-detail").First());
                    Assert.Greater(menu.Root.Q("saved-match-named").Query<Label>(className: "saved-match-heading").First().worldBound.height, 10);
                    Assert.AreEqual("isengard", AvatarImage.Shown(menu.Root.Q("saved-match-named")));
                    SameHeight(menu.Root.Q("saved-match-plain"), menu.Root.Q("saved-match-unnamed"), "A My games row");
                    SameHeight(menu.Root.Q("saved-match-plain"), menu.Root.Q("saved-match-named"), "A My games row");
                    LayoutTests.Save(target, $"ui505-my-games-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        [UnityTest]
        public IEnumerator TheWaitingRoomShowsSeatsObserversAndChatWithPictures()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var (width, height, scale) in Layouts())
                {
                    var target = new RenderTexture(width, height, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var waiting = new WaitingRoomScreen();
                    content.Add(waiting.Root);
                    var room = new RoomView
                    {
                        Id = "room", Name = "A game", Host = LongName, IsHost = true, Managed = true, FreePeoples = LongName,
                        ObserverLimit = 16, Version = 1
                    };
                    room.Observers.Add(new RoomObserverView { Name = "Watcher", Avatar = "southrons" });
                    waiting.Update(room);
                    var chat = new RoomChatPanel();
                    waiting.ChatHost.Add(chat.Root);
                    var lines = new RoomChatView { Latest = 2 };
                    lines.Messages.Add(new RoomChatMessage { Sequence = 1, Author = LongName, Text = "No picture" });
                    lines.Messages.Add(new RoomChatMessage { Sequence = 2, Author = LongName, AuthorAvatar = "ring", Text = "A picture" });
                    chat.Show(lines);
                    yield return Settle();
                    var seatHeight = waiting.Root.Q("seat-free").worldBound.height;
                    var chipHeight = waiting.Root.Q("observer-Watcher").worldBound.height;
                    SameHeight(chat.Root.Q("room-chat-message-1"), chat.Root.Q("room-chat-message-2"), "A room chat line");
                    Assert.AreEqual("ring", AvatarImage.Shown(chat.Root.Q("room-chat-message-2")));
                    var author = chat.Root.Q("room-chat-message-2").Query<Label>(className: "room-chat-author").First();
                    Reads(author);
                    Assert.IsFalse(author.worldBound.Overlaps(chat.Root.Q("room-chat-message-2").Query<Label>(className: "room-chat-text").First().worldBound),
                        "The text does not lie over the name.");
                    Assert.AreEqual("southrons", AvatarImage.Shown(waiting.Root.Q("observer-Watcher")));
                    Inside(Picture(waiting.Root.Q("observer-Watcher")), waiting.Root.Q("observer-Watcher"));
                    Assert.IsNull(AvatarImage.Shown(waiting.Root.Q("seat-free")), "No identifier yet.");
                    Assert.IsNull(AvatarImage.Shown(waiting.Root.Q("seat-shadow")), "An empty seat shows no picture.");

                    room.FreePeoplesAvatar = "eye";
                    room.Shadow = "SecondPlayer";
                    room.ShadowAvatar = "dwarves";
                    waiting.Update(room);
                    yield return Settle();
                    Assert.AreEqual("eye", AvatarImage.Shown(waiting.Root.Q("seat-free")));
                    Reads(waiting.Root.Q<Label>("seat-free-player"));
                    Reads(waiting.Root.Q<Label>("seat-shadow-player"));
                    Inside(waiting.Root.Q<Label>("seat-free-player"), waiting.Root.Q("seat-free"));
                    Assert.AreEqual("dwarves", AvatarImage.Shown(waiting.Root.Q("seat-shadow")));
                    Assert.AreEqual(seatHeight, waiting.Root.Q("seat-free").worldBound.height, 0.6f, "A seat keeps its height.");
                    Assert.AreEqual(chipHeight, waiting.Root.Q("observer-Watcher").worldBound.height, 0.6f, "A chip keeps its height.");
                    LayoutTests.Save(target, $"ui505-waiting-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        // The small panel on the board, where the lines are lowest and the width tightest.
        [UnityTest]
        public IEnumerator TheBoardChatKeepsItsLineCountWithPictures()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                var target = new RenderTexture(1366, 768, 24);
                target.Create();
                client.Panel.targetTexture = target;
                client.Interface.Preferences.SetScale(1.25f);
                content.Clear();
                var board = new VisualElement { name = "board-screen" };
                board.style.flexGrow = 1;
                content.Add(board);
                var plain = BoardChat(board, false);
                var pictured = BoardChat(board, true);
                yield return Settle();
                var plainRows = plain.Chat.Root.Query(className: "room-chat-message").ToList();
                var picturedRows = pictured.Chat.Root.Query(className: "room-chat-message").ToList();
                Assert.AreEqual(plainRows.Count, picturedRows.Count);
                for (var i = 0; i < plainRows.Count; i++)
                    SameHeight(plainRows[i], picturedRows[i], "Board chat line " + i);
                Assert.AreEqual("gondor", AvatarImage.Shown(picturedRows[0]));
                foreach (var row in picturedRows)
                {
                    var author = row.Query<Label>(className: "room-chat-author").First();
                    Reads(author);
                    Assert.IsFalse(author.worldBound.Overlaps(row.Query<Label>(className: "room-chat-text").First().worldBound),
                        "The text does not lie over the name.");
                }
                LayoutTests.Save(target, "ui505-board-chat-1366-1.25.png");
                target.Release();
            }
        }

        private static GameChatPanel BoardChat(VisualElement board, bool pictures)
        {
            var panel = new GameChatPanel();
            panel.Root.style.position = Position.Absolute;
            board.Add(panel.Root);
            panel.Available(true);
            panel.Open();
            var view = new RoomChatView { Latest = 4 };
            var samples = new[] { "Ready when you are", "Your move", "Well played, that was close", "gg" };
            for (var i = 0; i < samples.Length; i++)
            {
                view.Messages.Add(new RoomChatMessage
                {
                    Sequence = i + 1, Author = i % 2 == 0 ? "gracz1" : "Boromir77", Side = i % 2 == 0 ? Side.FreePeoples : Side.Shadow,
                    AuthorAvatar = pictures ? "gondor" : "", Text = samples[i]
                });
            }
            panel.Chat.Show(view);
            return panel;
        }

        [UnityTest]
        public IEnumerator FindGameShowsOnesOwnPictureAndPresentsTheOpponentsAtItsLargest()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var (width, height, scale) in Layouts())
                {
                    var target = new RenderTexture(width, height, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var menu = new MainMenuScreen(LongName, "gondor");
                    content.Add(menu.Root);
                    yield return Settle();
                    ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                    menu.FindGame.ShowRating(1532, "gondor");
                    yield return Settle();
                    var rating = menu.Root.Q<Label>("find-rating");
                    Assert.AreEqual("gondor", AvatarImage.Shown(rating.parent));
                    Reads(rating);
                    LayoutTests.Save(target, $"ui506-find-own-{width}-{scale:0.00}.png");

                    var form = menu.Root.Q(className: "find-form");
                    menu.FindGame.ShowOpponent(LongName, 2147, "sauron");
                    yield return Settle();
                    var avatar = menu.Root.Q("find-opponent-avatar");
                    Assert.AreEqual("sauron", avatar.userData);
                    Assert.AreEqual(AvatarImage.Presentation, avatar.resolvedStyle.width, 0.5f);
                    Assert.AreEqual(AvatarImage.Presentation, avatar.resolvedStyle.height, 0.5f, "The picture is not squeezed.");
                    Inside(avatar, form);
                    Inside(menu.Root.Q<Label>("find-opponent-name"), form);
                    Inside(menu.Root.Q<Label>("find-opponent-rating"), form);
                    LayoutTests.Save(target, $"ui506-opponent-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        [UnityTest]
        public IEnumerator AnUnknownIdentifierShowsTheEyeAndAnEmptyOneShowsNothing()
        {
            var image = AvatarImage.Create(AvatarImage.Tiny, "a-future-avatar");
            Assert.AreEqual(AvatarIds.Eye, image.userData);
            Assert.AreEqual(DisplayStyle.Flex, image.style.display.value);
            foreach (var id in AvatarIds.All)
            {
                AvatarImage.Show(image, id);
                Assert.AreEqual(id, image.userData);
                Assert.IsNotNull(Resources.Load<Texture2D>("Wotr/Art/Avatars/" + id), id);
            }
            AvatarImage.Show(image, "");
            Assert.AreEqual(DisplayStyle.None, image.style.display.value);
            Assert.IsNull(AvatarImage.Shown(image));
            yield return null;
        }

        [Test]
        public void TheAccountPageIsTheServersOwn()
        {
            Assert.AreEqual("https://wotr.example.test/account/", GatewayScreen.AccountPage("https://wotr.example.test/", ""));
            Assert.AreEqual("http://127.0.0.1:5088/account/", GatewayScreen.AccountPage("http://127.0.0.1:5088", ""));
        }

        [UnityTest]
        public IEnumerator SettingsOffersTheAccountPage()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var opened = 0;
                client.Interface.Settings.AccountPageRequested += () => opened++;
                client.Interface.Open();
                yield return Settle();
                var button = client.Root.Q<Button>("open-account-page");
                Assert.IsNotNull(button);
                Assert.Greater(button.worldBound.height, 0);
                ReturnTestFixture.Click(button);
                Assert.AreEqual(1, opened);
                client.Interface.Settings.Close();
            }
        }
    }
}
