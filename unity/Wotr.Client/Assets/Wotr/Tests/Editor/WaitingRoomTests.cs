using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class WaitingRoomTests
    {
        [TestCase(RoomVisibility.Public)]
        [TestCase(RoomVisibility.Private)]
        public void HostNeedsBothReadyAndGuestSeesInvitationAndStartGreyedOut(RoomVisibility visibility)
        {
            var screen = new WaitingRoomScreen();
            var room = new RoomView { Id = "room", Name = "Game", Managed = true, IsHost = true,
                Visibility = visibility, FreePeoples = "Host", Shadow = "Guest" };
            screen.Update(room);
            Assert.AreEqual(DisplayStyle.Flex, screen.Root.Q<Button>("generate-room-invitation").parent.style.display.value);
            Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
            Assert.AreEqual(1, screen.Root.Query<TextField>().ToList().Count);
            Assert.AreEqual(0, screen.Root.Query<Toggle>().ToList().Count);
            room.FreePeoplesAccepted = room.ShadowAccepted = true;
            screen.Update(room);
            Assert.IsTrue(screen.Root.Q<Button>("start-game").enabledSelf);
            Assert.AreEqual("Not ready", screen.Root.Q<Button>("room-ready").text);
            screen.Busy(true);
            Assert.IsFalse(screen.Root.Q<Button>("waiting-back").enabledSelf);
            screen.Busy(false);
            room.IsHost = false;
            screen.Update(room);
            // MENU-25: every control keeps its place; what the guest cannot use is greyed out.
            Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
            Assert.IsFalse(screen.Root.Q<Button>("generate-room-invitation").enabledSelf);
            Assert.IsFalse(screen.Root.Q<Button>("seat-shadow-remove").enabledSelf);
            Assert.AreNotEqual(DisplayStyle.None, screen.Root.Q<Button>("start-game").style.display.value);
        }

        // OBS-04: an observer sees the seats and the audience; Ready, the demo and Observe are greyed out, a free seat
        // can be taken, and no seat is marked as theirs.
        [Test]
        public void AnObserverTakesAFreeSeatButCannotGetReady()
        {
            var screen = new WaitingRoomScreen();
            var room = new RoomView { Id = "room", Name = "Game", Managed = true, Role = RoomRole.Observer, You = Side.FreePeoples,
                FreePeoples = "Host", Host = "Host", ObserverLimit = 16 };
            room.Observers.Add(new RoomObserverView { Name = "Watcher" });
            screen.Update(room);
            foreach (var name in new[] { "room-ready", "room-demo", "room-observe", "seat-free-take", "seat-free-remove", "seat-shadow-remove", "remove-observer-Watcher" })
                Assert.IsFalse(screen.Root.Q<Button>(name).enabledSelf, name);
            Assert.IsTrue(screen.Root.Q<Button>("seat-shadow-take").enabledSelf);
            Assert.IsFalse(screen.Root.Q("seat-free").ClassListContains("yours"));
            Assert.AreEqual("Observers 1/16", screen.Root.Q<Label>("waiting-observers-title").text);
            StringAssert.Contains("Observers 1/16", screen.Root.Q<Label>("waiting-information").text);
        }

        // OBS-04: the host in the audience removes either player and any observer but not themselves; after the start
        // the seats and the audience no longer change.
        [Test]
        public void AHostInTheAudienceRemovesAnyoneButThemselvesUntilTheStart()
        {
            var screen = new WaitingRoomScreen();
            var room = new RoomView { Id = "room", Name = "Game", Managed = true, IsHost = true, Role = RoomRole.Observer,
                Host = "Host", FreePeoples = "Alice", Shadow = "Bob", ObserverLimit = 16 };
            room.Observers.Add(new RoomObserverView { Name = "Host", IsHost = true });
            room.Observers.Add(new RoomObserverView { Name = "Carol" });
            screen.Update(room);
            foreach (var name in new[] { "seat-free-remove", "seat-shadow-remove", "remove-observer-Carol", "generate-room-invitation" })
                Assert.IsTrue(screen.Root.Q<Button>(name).enabledSelf, name);
            foreach (var name in new[] { "remove-observer-Host", "seat-free-take", "seat-shadow-take", "room-ready" })
                Assert.IsFalse(screen.Root.Q<Button>(name).enabledSelf, name);
            Assert.AreEqual("Carol", screen.Root.Q("observer-Carol").Q<Label>().text);
            Assert.AreEqual("Host · Host", screen.Root.Q("observer-Host").Q<Label>().text);
            room.Status = RoomStatus.Started;
            room.Version++;
            screen.Update(room);
            foreach (var name in new[] { "seat-free-remove", "seat-shadow-remove", "remove-observer-Carol" })
                Assert.IsFalse(screen.Root.Q<Button>(name).enabledSelf, name);
        }

        // OBS-04: an observer's message names the role and does not take a side's colour.
        [Test]
        public void AnObserversChatLineNamesTheRole()
        {
            var chat = new RoomChatPanel();
            var view = new RoomChatView { Latest = 1 };
            view.Messages.Add(new RoomChatMessage { Sequence = 1, Author = "Carol", Role = RoomRole.Observer, Text = "Good luck" });
            chat.Show(view);
            var author = chat.Root.Q("room-chat-message-1").Q<Label>(className: "room-chat-author");
            Assert.AreEqual("Carol (observer):", author.text);
            Assert.IsTrue(author.ClassListContains("observer"));
            Assert.IsFalse(author.ClassListContains("side-free"));
        }

        // MENU-29: a failed message says why, so a server without the room chat is not mistaken for a broken room.
        [Test]
        public void AFailedChatMessageSaysWhy()
        {
            StringAssert.Contains("200 characters", RoomChatController.Problem(new ApiException(400, "InvalidMessage")));
            StringAssert.Contains("no room chat", RoomChatController.Problem(new ApiException(404, "")));
            StringAssert.Contains("Wait", RoomChatController.Problem(new ApiException(429, "")));
            Assert.AreEqual("Message not sent. Try again.", RoomChatController.Problem(new System.TimeoutException()));
        }
    }
}
