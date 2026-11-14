using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class MainMenuTests
    {
        [Test]
        public void HostingDefaultsToPublicBaseGameAndValidatesNames()
        {
            var form = new HostGameForm("Player");
            CreateRoomRequest sent = null;
            form.Create += request => sent = request;
            Assert.IsNull(form.Root.Q<Toggle>("lords"));
            form.Root.Q<TextField>("room-name").value = " ";
            form.Submit();
            Assert.IsNull(sent);
            form.Root.Q<TextField>("room-name").value = " Player's game ";
            form.Submit();
            Assert.AreEqual("Player's game", sent.Name);
            Assert.AreEqual(RoomVisibility.Public, sent.Visibility);
            Assert.AreEqual(Side.FreePeoples, sent.Side);
            // MENU-30: the side is a card and the visibility a segment button; each click changes only its own choice.
            form.SetVisibility(RoomVisibility.Private);
            form.Choose(Side.Shadow);
            Assert.IsTrue(form.Root.Q<Button>("host-shadow").ClassListContains("selected"));
            Assert.IsTrue(form.Root.Q<Button>("host-private").ClassListContains("selected"));
            Assert.IsFalse(form.Root.Q<Button>("host-public").ClassListContains("selected"));
            Assert.IsFalse(form.Root.Q<Button>("host-fp").ClassListContains("selected"));
            StringAssert.Contains("Invitation only", form.Root.Q<Label>("host-visibility-note").text);
            form.Submit();
            Assert.AreEqual(RoomVisibility.Private, sent.Visibility);
            Assert.AreEqual(Side.Shadow, sent.Side);
            form.Busy(true);
            sent = null;
            form.Submit();
            Assert.IsNull(sent);
        }

        [Test]
        public void PublicAndOwnedRowsStayStableAndRemovedRoomsCannotBeOpened()
        {
            var screen = new MainMenuScreen("Player");
            var page = new PublicRoomListView { Rooms = new List<PublicRoomSummary>
                { new PublicRoomSummary { Id = "public", Name = "Public", Host = "Host", OpenSide = Side.Shadow } } };
            screen.ShowPublic(page);
            var button = screen.Root.Q<Button>("join-public-public");
            screen.ShowPublic(page);
            Assert.AreSame(button, screen.Root.Q<Button>("join-public-public"));
            var room = new RoomView { Id = "own", Name = "Own", Status = RoomStatus.Waiting };
            screen.ShowRooms(new[] { room });
            var own = screen.Root.Q<Button>("open-room-own");
            screen.ShowRooms(new[] { room });
            Assert.AreSame(own, screen.Root.Q<Button>("open-room-own"));
            screen.ShowPublic(new PublicRoomListView());
            screen.ShowRooms(new RoomView[0]);
            Assert.IsNull(screen.Root.Q<Button>("join-public-public"));
            Assert.IsNull(screen.Root.Q<Button>("open-room-own"));
        }

        // OBS-04: an open game says whether it waits or is in progress, with its seats and audience; Play and Observe
        // keep their places and the one not available is greyed out.
        [Test]
        public void OpenGamesOfferPlayAndObserveByTheirSeatsAndAudience()
        {
            var screen = new MainMenuScreen("Player");
            screen.ShowPublic(new PublicRoomListView { Rooms = new List<PublicRoomSummary>
            {
                new PublicRoomSummary { Id = "waiting", Name = "Waiting", Host = "Arwen", OpenSide = Side.FreePeoples, Players = 1, Observers = 2, CanPlay = true, CanObserve = true },
                new PublicRoomSummary { Id = "started", Name = "Started", Host = "Arwen", Status = RoomStatus.Started, Players = 2, Observers = 16 }
            } });
            Assert.AreEqual("Hosted by Arwen · Waiting · Seats 1/2 · Observers 2/16",
                screen.Root.Q("public-room-waiting").Q<Label>(className: "menu-row-detail").text);
            Assert.AreEqual("Hosted by Arwen · In progress · Observers 16/16",
                screen.Root.Q("public-room-started").Q<Label>(className: "menu-row-detail").text);
            Assert.IsTrue(screen.Root.Q<Button>("join-public-waiting").enabledSelf);
            Assert.IsTrue(screen.Root.Q<Button>("observe-public-waiting").enabledSelf);
            Assert.IsFalse(screen.Root.Q<Button>("join-public-started").enabledSelf);
            Assert.IsFalse(screen.Root.Q<Button>("observe-public-started").enabledSelf);
            Assert.AreEqual("The game has started.", screen.Root.Q<Button>("join-public-started").tooltip);
            screen.ShowRooms(new[] { new RoomView { Id = "watched", Name = "Watched", Role = RoomRole.Observer, Status = RoomStatus.Started, MatchId = "m" } });
            Assert.AreEqual("Watched · Watching · In progress", screen.Root.Q<Button>("open-room-watched").text);
        }

        [Test]
        public void TheWindowOpensOnOpenGamesWithTheirHostsAndMyGamesCountsYourDecisions()
        {
            var screen = new MainMenuScreen("Player");
            Assert.AreEqual("Open games", screen.Root.Q<Label>("menu-window-title").text);
            Assert.AreEqual(DisplayStyle.Flex, screen.Root.Q("public-games").style.display.value);
            Assert.AreEqual(DisplayStyle.None, screen.Root.Q("my-games").style.display.value);
            Assert.IsTrue(screen.Root.Q<Button>("show-public-games").ClassListContains("selected"));
            Assert.IsFalse(screen.Root.Q<Button>("show-my-games").ClassListContains("selected"));
            screen.ShowPublic(new PublicRoomListView { Rooms = new List<PublicRoomSummary>
                { new PublicRoomSummary { Id = "open", Name = "Evening game", Host = "Aragorn", OpenSide = Side.Shadow, Players = 1, CanPlay = true, CanObserve = true } } });
            var row = screen.Root.Q("public-room-open");
            Assert.AreEqual("Evening game", row.Q<Label>(className: "menu-row-title").text);
            StringAssert.StartsWith("Hosted by Aragorn · Waiting", row.Q<Label>(className: "menu-row-detail").text);
            StringAssert.StartsWith("Join as ", row.Q<Button>("join-public-open").tooltip);
            Assert.IsTrue(row.Q(className: "menu-row-emblem").ClassListContains("side-shadow"));
            var badge = screen.Root.Q<Label>("my-games-turns");
            Assert.AreEqual(DisplayStyle.None, badge.style.display.value);
            var page = new MatchListView();
            page.Matches.Add(new MatchSummary { MatchId = "a", Status = MatchStatus.YourDecision, YourDecision = DecisionKind.FellowshipPhase });
            page.Matches.Add(new MatchSummary { MatchId = "b", Status = MatchStatus.Waiting });
            page.Matches.Add(new MatchSummary { MatchId = "c", Status = MatchStatus.YourDecision, YourDecision = DecisionKind.AllocateHunt });
            screen.ShowMatches(page);
            Assert.AreEqual("2", badge.text);
            Assert.AreEqual(DisplayStyle.Flex, badge.style.display.value);
            page.Matches.RemoveAt(2);
            page.Matches.RemoveAt(0);
            screen.ShowMatches(page);
            Assert.AreEqual(DisplayStyle.None, badge.style.display.value);
        }
    

        // UI-537 (owner, 2026-11-14): an x beside each My games row deletes the game for both players after a question;
        // a ranked game (RANK-04: never removed) has none; Cancel changes nothing.
        [Test]
        public void TheXBesideAGameAsksAndOnlyDeleteRemovesIt()
        {
            var screen = new MainMenuScreen("Player");
            var page = new MatchListView();
            page.Matches.Add(new MatchSummary { MatchId = "plain", Status = MatchStatus.Waiting, Opponent = "Gandalf" });
            page.Matches.Add(new MatchSummary { MatchId = "ranked", Status = MatchStatus.Waiting, Ranked = true, Opponent = "Saruman" });
            screen.ShowMatches(page);
            var finished = new MatchListView();
            finished.Matches.Add(new MatchSummary { MatchId = "done", Status = MatchStatus.Finished, Winner = Side.Shadow, You = Side.Shadow });
            screen.ShowFinished(finished);
            var deleted = new List<string>();
            screen.DeleteMatch += match => deleted.Add(match.MatchId);
            var x = screen.Root.Q<Button>("delete-match-plain");
            Assert.IsNotNull(x);
            Assert.AreEqual("×", x.text);
            Assert.AreEqual(DisplayStyle.Flex, x.style.display.value);
            Assert.AreEqual(DisplayStyle.None, screen.Root.Q<Button>("delete-match-ranked").style.display.value, "A ranked game is never deleted.");
            Assert.IsNotNull(screen.Root.Q<Button>("delete-match-done"), "Finished games have their x too.");
            // The x is beside the row's button, not inside it: pressing it never opens the game.
            Assert.IsNull(screen.Root.Q<Button>("resume-match-plain").Q<Button>("delete-match-plain"));
            Assert.IsFalse(screen.DeleteGame.IsOpen);
            Click(x);
            Assert.IsTrue(screen.DeleteGame.IsOpen);
            Assert.AreEqual("Delete this game?", screen.DeleteGame.Root.Q<Label>(className: "title").text);
            Click(screen.DeleteGame.Root.Q<Button>("delete-game-cancel"));
            Assert.IsFalse(screen.DeleteGame.IsOpen);
            CollectionAssert.IsEmpty(deleted, "Cancel deletes nothing.");
            Click(x);
            Click(screen.DeleteGame.Root.Q<Button>("delete-game-confirm"));
            CollectionAssert.AreEqual(new[] { "plain" }, deleted);
            screen.RemoveMatch("plain");
            Assert.IsNull(screen.Root.Q("saved-match-plain"), "The deleted game leaves the list at once.");
            Assert.IsNotNull(screen.Root.Q("saved-match-ranked"));
        }

        // Without a panel no event reaches the button: its click action is invoked as a press would.
        private static void Click(Button button)
        {
            var invoke = typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public, null, new[] { typeof(EventBase) }, null);
            Assert.IsNotNull(invoke, "Clickable.Invoke");
            invoke.Invoke(button.clickable, new object[] { null });
        }
    }
}
