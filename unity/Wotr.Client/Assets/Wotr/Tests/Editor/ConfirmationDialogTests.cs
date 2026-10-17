using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // MENU-14: both menus end with Quit game, which only opens a question with Quit and Cancel in fixed places.
    // MENU-32: the board menu has no Delete game; Exit match (formerly Games) leaves the match on the server.
    public sealed class ConfirmationDialogTests
    {
        [Test]
        public void TheQuestionStartsHiddenAndKeepsItsTextAndButtons()
        {
            var question = ConfirmationDialog.Quit("Your games stay on the server.");
            Assert.IsFalse(question.IsOpen);
            Assert.AreEqual("Quit War of the Ring?", question.Root.Q<Label>(className: "title").text);
            Assert.AreEqual("Your games stay on the server.", question.Root.Q<Label>("quit-note").text);
            CollectionAssert.AreEqual(new[] { "quit-confirm", "quit-cancel" }, question.Root.Query<Button>().ToList().Select(b => b.name));
            var confirmed = 0;
            question.Confirmed += () => confirmed++;
            question.Open();
            Assert.IsTrue(question.IsOpen);
            question.Close();
            Assert.IsFalse(question.IsOpen);
            question.Open();
            question.Hide();
            Assert.IsFalse(question.IsOpen);
            Assert.AreEqual(0, confirmed, "Opening and closing never confirm.");
        }

        [Test]
        public void TheBoardMenuEndsWithQuitGameAndClosingItHidesTheQuestion()
        {
            var menu = new BoardMenu();
            var items = menu.Panel.Query<Button>().ToList().Select(b => b.name).ToList();
            CollectionAssert.AreEqual(new[] { "board-menu-resume", "board-menu-victory", "open-interface", "back-to-games",
                "board-menu-quit", "board-menu-retry" }, items);
            Assert.AreEqual("Exit match", menu.Panel.Q<Button>("back-to-games").text);
            StringAssert.EndsWith("Exit match · Quit game", menu.Toggle.tooltip);
            Assert.IsNull(menu.Root.Q("delete-confirmation"));
            Assert.AreSame(menu.Root, menu.Quit.Root.parent);
            menu.Open();
            menu.Quit.Open();
            Assert.AreSame(menu.Quit, menu.OpenQuestion);
            menu.Close();
            Assert.IsFalse(menu.Quit.IsOpen);
            Assert.IsNull(menu.OpenQuestion);
        }

        [Test]
        public void ExitMatchSavesAndGreysOutWhileLeaving()
        {
            var menu = new BoardMenu();
            menu.SetSaving(true);
            Assert.AreEqual("Saving recovery data…", menu.Panel.Q<Label>("board-menu-status").text);
            Assert.IsFalse(menu.Panel.Q<Button>("back-to-games").enabledSelf, "The button keeps its place, greyed out.");
        }

        [Test]
        public void ADeletedGameKeepsItsNoticeUntilMainMenu()
        {
            var notice = new GameDeletedNotice();
            Assert.IsFalse(notice.IsOpen);
            Assert.AreEqual("Game deleted", notice.Root.Q<Label>(className: "title").text);
            StringAssert.Contains("other player deleted", notice.Root.Q<Label>("game-deleted-note").text);
            CollectionAssert.AreEqual(new[] { "game-deleted-menu" }, notice.Root.Query<Button>().ToList().Select(b => b.name));
            notice.Open();
            Assert.IsTrue(notice.IsOpen);
        }

        [Test]
        public void TheMainMenuEndsItsActionsWithQuitGame()
        {
            var screen = new MainMenuScreen("Player");
            var actions = screen.Root.Q("menu-actions").Query<Button>().ToList().Select(b => b.name).ToList();
            Assert.AreEqual("quit-game", actions.Last());
            Assert.AreEqual("sign-out", actions[actions.Count - 2]);
            Assert.AreSame(screen.Root, screen.Quit.Root.parent);
            Assert.IsFalse(screen.Quit.IsOpen);
        }
    }
}
