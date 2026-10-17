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
    }
}
