using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // CHAT-01: the board's chat icon appears where the server has the chat, marks later messages with "?" while the
    // panel is rolled up, and clears the mark when the panel opens.
    public sealed class GameChatPanelTests
    {
        [Test]
        public void TheIconMarksLaterMessagesUntilThePanelOpens()
        {
            var chat = new GameChatPanel();
            Assert.AreEqual(DisplayStyle.None, chat.Icon.style.display.value, "No icon before the server answers.");
            chat.Available(true);
            Assert.AreEqual(DisplayStyle.Flex, chat.Icon.style.display.value);
            chat.Chat.Show(View(1, "already there"));
            Assert.IsFalse(chat.Unread, "Messages present when the board opens are not news.");
            chat.Chat.Show(View(2, "later"));
            Assert.IsTrue(chat.Unread);
            Assert.AreEqual("?", chat.Badge.text);
            Assert.AreEqual(DisplayStyle.Flex, chat.Badge.style.display.value);
            chat.Open();
            Assert.IsTrue(chat.IsOpen);
            Assert.IsFalse(chat.Unread);
            Assert.AreEqual(DisplayStyle.None, chat.Badge.style.display.value);
            chat.Chat.Show(View(3, "while open"));
            Assert.IsFalse(chat.Unread, "A message read in the open panel is not unread.");
            chat.Available(false);
            Assert.IsFalse(chat.IsOpen);
            Assert.AreEqual(DisplayStyle.None, chat.Icon.style.display.value);
        }

        private static RoomChatView View(long sequence, string text) => new RoomChatView
        {
            Latest = sequence,
            Messages = { new RoomChatMessage { Sequence = sequence, Author = "frodo", Role = RoomRole.Player, Side = Side.FreePeoples, Text = text } }
        };
    }
}
