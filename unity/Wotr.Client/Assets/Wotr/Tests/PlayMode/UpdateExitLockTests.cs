using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Client.Updates;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // DIST-18 (owner 2026-10-25): from the click on Restart and update or Decline and quit until the application quits,
    // the screen is greyed and nothing beneath takes the pointer, the wheel or a key; a failed save or install gives the
    // screen back with its error.
    public sealed class UpdateExitLockTests
    {
        private string folder;

        [SetUp]
        public void Create() => folder = Path.Combine(Path.GetTempPath(), "wotr-exit-lock-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Remove()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [UnityTest]
        public IEnumerator DeclineAndQuitGreysTheScreenAndTakesEveryInputUntilTheGameQuits()
        {
            using var client = new MountedMatchClient("http://localhost", "exit-lock-quit");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            var saving = new TaskCompletionSource<string>();
            var quits = 0;
            using var updates = Controller(client, _ => saving.Task, () => quits++);
            var board = client.Board;
            var keys = Count<KeyDownEvent>(client.Interface.Content);
            var wheels = Count<WheelEvent>(client.Interface.Content);
            var point = board.Map.worldBound.center;
            var die = FocusADie(board);
            updates.DeclineAndQuit();
            yield return Settle();
            var exitLock = client.Interface.ExitLock;
            Assert.IsTrue(exitLock.IsShown, "The click locks the screen at once.");
            Assert.AreEqual(UpdateTexts.Saving, exitLock.Step.text);
            Assert.IsTrue(exitLock.Root.parent.ClassListContains(ExitLock.LockedClass), "The screen beneath loses its colour.");
            Assert.AreSame(exitLock.Root, board.Root.panel.Pick(point), "The grey layer takes every pick.");
            Assert.AreSame(exitLock.Root, board.Root.panel.focusController.focusedElement, "Keys go to the lock, not the game.");
            Assert.IsFalse(board.Map.enabledInHierarchy, "No map gesture or edge pan runs beneath it.");
            Send(die, keys, wheels);
            yield return Settle();
            Assert.AreEqual(0, keys.Value, "No key reaches the game, Esc included.");
            Assert.AreEqual(0, wheels.Value, "No wheel reaches the map.");
            Assert.IsFalse(client.Interface.Settings.Root.enabledInHierarchy, "Settings take nothing either.");
            LayoutTests.Save(client.Viewport, "dist18-exit-lock.png");
            saving.SetResult("");
            yield return Settle();
            Assert.AreEqual(1, quits, "The game quits once.");
            Assert.IsTrue(exitLock.IsShown, "The lock stays until the application is gone.");
            Assert.AreEqual(UpdateTexts.Closing, exitLock.Step.text);
        }

        [UnityTest]
        public IEnumerator AFailedSaveOrInstallGivesTheScreenBackWithItsError()
        {
            using var client = new MountedMatchClient("http://localhost", "exit-lock-failure");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            var answer = new TaskCompletionSource<string>();
            var quits = 0;
            using var updates = Controller(client, _ => answer.Task, () => quits++);
            var board = client.Board;
            var keys = Count<KeyDownEvent>(client.Interface.Content);
            var wheels = Count<WheelEvent>(client.Interface.Content);
            var point = board.Map.worldBound.center;
            var die = FocusADie(board);
            updates.Restart();
            yield return Settle();
            Assert.IsTrue(client.Interface.ExitLock.IsShown);
            answer.SetResult("disk full");
            yield return Settle();
            AssertUnlocked(client, point);
            StringAssert.Contains("disk full", updates.Section.Root.Q<Label>(className: "update-status").text);
            Assert.AreSame(die, board.Root.panel.focusController.focusedElement, "The focus returns where it was.");
            Send(die, keys, wheels);
            yield return Settle();
            Assert.GreaterOrEqual(keys.Value, 1, "Keys reach the game again.");
            Assert.AreEqual(1, wheels.Value);
            // Saved now, but this copy has no ready update to stage: the install fails and the screen comes back again.
            answer = new TaskCompletionSource<string>();
            answer.SetResult("");
            updates.Restart();
            yield return Settle();
            AssertUnlocked(client, point);
            Assert.AreEqual(0, quits, "A failed exit never quits.");
        }

        private UpdateController Controller(MountedMatchClient client, Func<bool, Task<string>> save, Action quit)
        {
            var identity = new ClientBuildInfo { Version = "0.9.0", Platform = ClientPlatforms.MacOS, ProtocolVersion = 2 };
            var coordinator = new UpdateCoordinator(identity, new SilentCatalog(), new PackageDownloader(folder, "WotR test"),
                new UpdateStore(folder), ReleaseTrust.ForOrigin("https://downloads.example.test/"), null, new ServerClock(),
                () => TimeSpan.FromMinutes(5));
            return new UpdateController(coordinator, client.Interface, save, () => Task.CompletedTask, quit, () => { }, _ => { },
                _ => { }, "0.9.0");
        }

        // Keys go to the focused element; a die of the player's pool holds the focus before the exit.
        private static VisualElement FocusADie(BoardScreen board)
        {
            var die = board.OwnDice.Q<Button>("army");
            Assert.IsNotNull(die, "The fixture's pool has an Army die.");
            die.Focus();
            Assert.AreSame(die, board.Root.panel.focusController.focusedElement);
            return die;
        }

        private static void AssertUnlocked(MountedMatchClient client, Vector2 point)
        {
            Assert.IsFalse(client.Interface.ExitLock.IsShown, "A failed exit lifts the lock.");
            Assert.IsFalse(client.Interface.ExitLock.Root.parent.ClassListContains(ExitLock.LockedClass));
            Assert.IsTrue(client.Board.Map.enabledInHierarchy, "The game takes input again.");
            Assert.AreNotSame(client.Interface.ExitLock.Root, client.Board.Root.panel.Pick(point));
        }

        private static StrongBox Count<T>(VisualElement target) where T : EventBase<T>, new()
        {
            var box = new StrongBox();
            target.RegisterCallback<T>(_ => box.Value++, TrickleDown.TrickleDown);
            return box;
        }

        // Esc (the game may take it above the board) and a key nothing binds.
        private static void Send(VisualElement target, StrongBox keys, StrongBox wheels)
        {
            foreach (var code in new[] { KeyCode.Escape, KeyCode.F15 })
                using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = code }))
                    target.SendEvent(key);
            using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector2(0, 3) }))
                target.SendEvent(wheel);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }

        private sealed class StrongBox
        {
            public int Value;
        }

        // A catalog never asked during these tests; a read would wait for ever.
        private sealed class SilentCatalog : IReleaseCatalog
        {
            public Task<ClientReleaseView> Read(CancellationToken cancellation) =>
                Task.Delay(Timeout.Infinite, cancellation).ContinueWith(_ => new ClientReleaseView(), TaskScheduler.Default);
        }
    }
}
