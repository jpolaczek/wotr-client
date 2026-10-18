using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // LOBBY-01: the lobby's state between polls, its panel (chat, players online, the challenge tab) and the menu's
    // challenge tab, without a server.
    public sealed class LobbyTests
    {
        private static LobbyMessage Message(long sequence, string author, string text) =>
            new LobbyMessage { Sequence = sequence, Author = author, Text = text };

        private static ChallengeView Challenge(string id, string from, string to, ChallengeStatus status, string room = "") =>
            new ChallengeView { Id = id, From = from, To = to, Status = status, RoomId = room };

        private static LobbyView View(params LobbyMessage[] messages) => new LobbyView
        {
            Messages = messages.ToList(),
            Latest = messages.Length == 0 ? 0 : messages.Max(m => m.Sequence),
            Online = new List<OnlinePlayer> { new OnlinePlayer { Name = "vibestudios", You = true }, new OnlinePlayer { Name = "aragorn" } }
        };

        [Test]
        public void MessagesArriveInOrderOnceAndTheLatestIsKept()
        {
            var state = new LobbyState();
            state.Apply(View(Message(2, "aragorn", "second"), Message(1, "aragorn", "first")));
            state.Apply(View(Message(2, "aragorn", "second"), Message(3, "vibestudios", "third")));
            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, state.Messages.Select(m => m.Text));
            Assert.AreEqual(3, state.Latest);
            var many = Enumerable.Range(4, LobbyLimits.MessagesKept + 10).Select(i => Message(i, "a", "m" + i)).ToArray();
            state.AddMessages(many);
            Assert.AreEqual(LobbyLimits.MessagesKept, state.Messages.Count);
        }

        [Test]
        public void OwnChallengeOutcomesAreToldOnceAndAnIncomingOneStaysDismissed()
        {
            var state = new LobbyState();
            var view = View();
            view.Outgoing.Add(Challenge("c1", "vibestudios", "aragorn", ChallengeStatus.Accepted, "room-1"));
            view.Outgoing.Add(Challenge("c2", "vibestudios", "boromir", ChallengeStatus.Pending));
            view.Incoming.Add(Challenge("c3", "gimli", "vibestudios", ChallengeStatus.Pending));
            var outcomes = state.Apply(view);
            Assert.AreEqual(new[] { "c1" }, outcomes.Select(c => c.Id).ToArray());
            Assert.AreEqual("c3", state.Incoming.Id);
            Assert.IsEmpty(state.Apply(view), "An outcome is told once.");
            state.Dismiss("c3");
            state.Apply(view);
            Assert.IsNull(state.Incoming, "A dismissed challenge does not come back.");
            StringAssert.Contains("accepted", LobbyState.Outcome(view.Outgoing[0]));
            StringAssert.Contains("aragorn", LobbyState.Refusal("PlayerOffline", "aragorn"));
        }

        [Test]
        public void ThePanelShowsMessagesAndPlayersAndChallengesFromTheTab()
        {
            var panel = new LobbyPanel();
            var state = new LobbyState();
            state.Apply(View(Message(1, "aragorn", "Who plays the Shadow?")));
            panel.Apply(state);
            Assert.AreEqual("Who plays the Shadow?", panel.Root.Q("lobby-message-1").Q<Label>(className: "lobby-text").text);
            Assert.IsNotNull(panel.Root.Q<Label>("lobby-player-aragorn"));
            StringAssert.Contains("(you)", panel.Root.Q<Label>("lobby-player-vibestudios").text);
            Assert.AreEqual("Online · 2", panel.Root.Q<Label>("lobby-online-title").text);
            Assert.IsNull(panel.Root.Q("lobby-online-toggle"), "LOBBY-02: no fold button in the chat line");
            Assert.IsNotNull(panel.Root.Q("lobby-online-strip").Q<Button>("lobby-online-tab"));
            panel.OpenContext("aragorn", Vector2.zero);
            Assert.AreEqual(DisplayStyle.Flex, panel.ContextTab.style.display.value);
            Assert.AreEqual("aragorn", panel.ContextTab.Q<Label>("lobby-context-name").text);
            panel.CloseContext();
            Assert.AreEqual(DisplayStyle.None, panel.ContextTab.style.display.value);
            string sent = null;
            panel.Send += text => sent = text;
            panel.Input.value = "  hello  ";
            panel.Submit();
            Assert.AreEqual("hello", sent);
            Assert.AreEqual("", panel.Input.value);
            Assert.IsFalse(panel.OnlineShown, "The players online start folded, so the input is free.");
            Assert.AreEqual(DisplayStyle.None, panel.Root.Q("lobby-online").style.display.value);
            StringAssert.Contains("Show the players online (2)", panel.Root.Q<Button>("lobby-online-tab").tooltip);
            panel.ToggleOnline();
            Assert.IsTrue(panel.OnlineShown);
            Assert.AreEqual(DisplayStyle.Flex, panel.Root.Q("lobby-online").style.display.value);
            StringAssert.Contains("Hide", panel.Root.Q<Button>("lobby-online-tab").tooltip);
            panel.ToggleOnline();
            Assert.AreEqual(DisplayStyle.None, panel.Root.Q("lobby-online").style.display.value);
        }

        // LOBBY-02: a new sign-in starts the chat from now: none of the earlier messages and none of the earlier
        // challenge outcomes; what comes later shows as usual.
        [Test]
        public void ASignInStartsTheChatFromNow()
        {
            var state = LobbyState.FromSignIn();
            var before = View(Message(1, "aragorn", "Before you came"), Message(2, "gimli", "Also before"));
            before.Outgoing.Add(Challenge("c1", "vibestudios", "aragorn", ChallengeStatus.Accepted, "room-1"));
            before.Online.Add(new OnlinePlayer { Name = "aragorn" });

            Assert.IsEmpty(state.Apply(before), "An earlier accepted challenge does not open its room again.");
            Assert.IsEmpty(state.Messages);
            Assert.AreEqual(2, state.Latest);
            Assert.IsTrue(state.Online.Any(player => player.Name == "aragorn"), "Players online show at once.");

            var after = View(Message(3, "aragorn", "Welcome"));
            after.Outgoing.Add(Challenge("c1", "vibestudios", "aragorn", ChallengeStatus.Accepted, "room-1"));
            Assert.IsEmpty(state.Apply(after));
            Assert.AreEqual(new[] { "Welcome" }, state.Messages.Select(m => m.Text).ToArray());
            var plain = new LobbyState();
            Assert.AreEqual(1, plain.Apply(before).Count);
            Assert.AreEqual(2, plain.Messages.Count, "A state made outside a sign-in still takes the history.");
        }

        [Test]
        public void TheChallengeTabNamesTheChallengerAndAnswersOnce()
        {
            var tab = new ChallengeTab();
            Assert.IsFalse(tab.IsShown);
            tab.Show(Challenge("c9", "aragorn", "vibestudios", ChallengeStatus.Pending));
            Assert.IsTrue(tab.IsShown);
            Assert.AreEqual("aragorn challenges you to a game", tab.Root.Q<Label>("challenge-text").text);
            Assert.AreEqual("c9", tab.ChallengeId);
            tab.Show(null);
            Assert.IsFalse(tab.IsShown);
        }

        [Test]
        public void TheMenuHoldsTheLobbyUnderOpenGamesAndTheTabOverTheWindow()
        {
            var menu = new MainMenuScreen("vibestudios");
            Assert.IsTrue(menu.Root.Q("public-games").Contains(menu.Lobby.Root));
            Assert.IsTrue(menu.Root.Q("menu-window").Contains(menu.Challenge.Root));
        }
    }
}
