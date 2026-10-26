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
    // UI-322 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible"): in the battle window a figure
    // stack that leaves fades out where it stood and a stack whose count changed pulses; under Skip animations the window
    // shows the new stacks at once.
    public sealed class BattleStackFadeTests
    {
        [UnityTest]
        public IEnumerator ALeavingStackFadesOutWhereItStoodAndAChangedCountPulses()
        {
            using var client = new MountedMatchClient("http://localhost", "battle-stack-fade");
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.4f);
            var (gone, fewer) = Stacks(view);
            var place = Figure(client, gone).worldBound;
            client.Refresh(Losses(view, gone, fewer));
            Assert.IsNull(Figure(client, gone), "The stack is gone for every query at once.");
            var ghost = client.Board.Tray.Q("leaving-" + Name(gone));
            Assert.IsNotNull(ghost, "A copy of it fades out instead.");
            Assert.AreEqual(PickingMode.Ignore, ghost.pickingMode);
            Assert.IsTrue(Figure(client, fewer).Q("battle-figure-count").ClassListContains(ValuePulse.PulseClass), "The smaller stack's count pulses.");
            yield return new WaitForSecondsRealtime(.08f);
            Assert.AreEqual(place.center.x, ghost.worldBound.center.x, 1, "It fades where it stood.");
            Assert.Less(ghost.resolvedStyle.opacity, .99f, "It is fading.");
            LayoutTests.Save(client.Viewport, "ui322-battle-stack.png");
            yield return new WaitForSecondsRealtime((Appearance.CloseMilliseconds + 200) / 1000f);
            Assert.IsNull(ghost.parent, "Then it goes.");
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsTheStacksChangeAtOnce()
        {
            using var client = new MountedMatchClient("http://localhost", "battle-stack-skip");
            client.Interface.Preferences.SetReducedMotion(true);
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.4f);
            var (gone, fewer) = Stacks(view);
            client.Refresh(Losses(view, gone, fewer));
            Assert.IsNull(client.Board.Tray.Q("leaving-" + Name(gone)), "No fading copy.");
            Assert.IsNull(Figure(client, gone));
        }

        private static string Name(UnitView unit) => "battle-figure-" + unit.Nation + "-" + unit.Kind;

        private static VisualElement Figure(MountedMatchClient client, UnitView unit) => client.Board.Tray.Q(Name(unit));

        // The attacker's Gondor Elite (its only one) is wiped out; the defender's five Sauron Regulars lose one.
        private static (UnitView gone, UnitView fewer) Stacks(PlayerView view) => (
            view.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite),
            view.Battle.Defender.Units.First(u => u.Kind == UnitKind.Regular));

        // The next revision without those figures.
        private static PlayerView Losses(PlayerView view, UnitView gone, UnitView fewer)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            var removed = new[] { gone.Id, fewer.Id };
            next.Battle.Attacker.Units.RemoveAll(u => removed.Contains(u.Id));
            next.Battle.Defender.Units.RemoveAll(u => removed.Contains(u.Id));
            next.Units.RemoveAll(u => removed.Contains(u.Id));
            return next;
        }
    }
}
