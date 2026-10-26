using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-322 (owner rule 2026-10-24): a number that changes swells and lights for a moment while its new value already
    // stands; a quiet view and Skip animations change it without any movement.
    public sealed class ValuePulseTests
    {
        [UnityTest]
        public IEnumerator AChangedCountPulsesWithItsNewValueAndSettles()
        {
            using var client = new MountedMatchClient("http://localhost", "value-pulse");
            var view = new BoardFixture().View;
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.3f);
            var value = OpponentSpent(client.Board);
            var before = value.text;
            client.Apply(Spend(view));
            Assert.AreEqual(Spent(view).ToString(), value.text, "The new value stands at once.");
            Assert.AreNotEqual(before, value.text);
            Assert.IsTrue(value.ClassListContains(ValuePulse.PulseClass), "It pulses.");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.Greater(value.resolvedStyle.scale.value.x, 1.05f, "It swells…");
            LayoutTests.Save(client.Viewport, "ui322-value-pulse.png");
            yield return new WaitForSecondsRealtime(ValuePulse.PulseMilliseconds / 1000f + .3f);
            Assert.IsFalse(value.ClassListContains(ValuePulse.PulseClass));
            Assert.AreEqual(1f, value.resolvedStyle.scale.value.x, .01f, "…and settles.");
        }

        [UnityTest]
        public IEnumerator AQuietViewAndSkipAnimationsChangeTheCountWithoutMovement()
        {
            using (var client = new MountedMatchClient("http://localhost", "value-quiet"))
            {
                var view = new BoardFixture().View;
                client.Apply(view);
                yield return new WaitForSecondsRealtime(.3f);
                client.Board.ApplyQuietly(Spend(view));
                var value = OpponentSpent(client.Board);
                Assert.AreEqual(Spent(view).ToString(), value.text);
                Assert.IsFalse(value.ClassListContains(ValuePulse.PulseClass), "A quiet view shows the state.");
            }
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var latest = stage.Latest();
            stage.Board.Apply(Spend(latest));
            yield return null;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(1f, OpponentSpent(stage.Board).resolvedStyle.scale.value.x, .01f, "Skip animations: no swell.");
        }

        // The opponent's spent dice chip (Shadow's, for the Free Peoples fixture).
        private static Label OpponentSpent(BoardScreen board) => board.TopBar.Opponent.Root.Q("opponent-spent").Q<Label>(className: "summary-value");

        private static int Spent(PlayerView view) => view.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent) + 1;

        // One more Shadow die spent, the next revision.
        private static PlayerView Spend(PlayerView view)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            next.Dice.Add(new DieView { Id = "pulse-" + next.Revision, Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Spent });
            return next;
        }
    }
}
