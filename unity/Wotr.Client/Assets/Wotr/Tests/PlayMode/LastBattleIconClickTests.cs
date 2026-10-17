using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-112: the ⚔ control on the notice bar disappears once its report was opened.
    public sealed class LastBattleIconClickTests
    {
        [UnityTest]
        public IEnumerator OpeningTheReportHidesTheIconUntilTheNextRound()
        {
            using var client = new MountedMatchClient("http://localhost", "last-battle-icon");
            var view = Completed();
            client.Apply(view);
            yield return Settle();
            var icon = client.Board.Root.Q<Button>("last-battle");
            Assert.AreEqual(DisplayStyle.Flex, icon.resolvedStyle.display);
            PointerGestureEvents.ClickAt(icon);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, client.Board.BattleReport.Root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, icon.resolvedStyle.display, "The opened round is read.");
            LayoutTests.Save(client.Viewport, "ui112-last-battle-read.png");
            Assert.AreSame(client.Board.BattleReport.Root, client.Board.Root.focusController.focusedElement, "The report takes the focus of the hidden icon.");

            // The key goes where a real keyboard sends it: the focused element, else the board.
            Escape(client.Board.Root.focusController.focusedElement as VisualElement ?? client.Board.Root);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, client.Board.BattleReport.Root.resolvedStyle.display, "Escape closes the report.");
            Assert.AreNotSame(icon, client.Board.Root.focusController.focusedElement, "Focus does not stay on the hidden icon.");

            client.Refresh(Completed());
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, icon.resolvedStyle.display, "The same round stays read in a later snapshot.");

            var next = Completed();
            next.LastBattleRound.Round++;
            client.Refresh(next);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, icon.resolvedStyle.display, "The next round is unread.");
            LayoutTests.Save(client.Viewport, "ui112-last-battle-next-round.png");
        }

        private static PlayerView Completed()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            view.MatchId = "last-battle-icon";
            view.LastBattleRound = view.Battle;
            view.Battle = null;
            return view;
        }

        private static void Escape(VisualElement root)
        {
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                root.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                root.SendEvent(up);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 12; frame++)
                yield return null;
        }
    }
}
