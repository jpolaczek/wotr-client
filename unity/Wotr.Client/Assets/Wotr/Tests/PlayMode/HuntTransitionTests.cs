using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-327 (owner 2026-10-25): the Hunt's phases change without jumps — the ribbon's marker glides to the new stage, the
    // window stays where it stands and the new content fades in after it — and two-sided tokens (the Ring, a nation's
    // token) turn over to their other side; under Skip animations all of it happens at once.
    public sealed class HuntTransitionTests
    {
        [UnityTest]
        public IEnumerator TheMarkerGlidesToTheNextPhaseAndTheWindowStaysWhereItStands()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-transition");
            var view = HuntLayoutTests.Screen(CommandKind.RerollHunt);
            client.Apply(view);
            client.SetOnline();
            client.Board.Hunt.Open();
            yield return new WaitForSecondsRealtime(.4f);
            var hunt = client.Board.Hunt;
            var ribbon = hunt.Root.Q("hunt-stages");
            var marker = ribbon.Q("stage-glide");
            var before = Current(ribbon);
            Assert.AreEqual("hunt-stage-reroll", before.name);
            var start = marker.layout.x;
            Assert.AreEqual(before.layout.x - 5, start, 1, "The marker stands at Re-roll.");
            var place = hunt.Root.worldBound;
            client.Apply(Damage(view));
            yield return null;
            Assert.AreSame(ribbon, hunt.Root.Q("hunt-stages"), "The same ribbon: the marker travels, the row stays.");
            var after = Current(ribbon);
            Assert.AreNotEqual(before, after, "A later stage is current.");
            Assert.IsTrue(hunt.Root.Q("hunt-main").ClassListContains(Appearance.AppearingClass), "The new phase's content fades in.");
            yield return new WaitForSecondsRealtime(.12f);
            var target = after.layout.x - 5;
            Assert.Greater(marker.layout.x, start + 2, "It sets off…");
            Assert.Less(marker.layout.x, target - 2, "…and is on its way, not there at once.");
            LayoutTests.Save(client.Viewport, "ui327-stage-glide.png");
            yield return new WaitForSecondsRealtime((float)StageRibbon.GlideSeconds + .2f);
            Assert.AreEqual(target, marker.layout.x, 1, "It arrives at the new stage.");
            Assert.AreEqual(place.y, hunt.Root.worldBound.y, 1, "The window keeps its top across the phases.");
            Assert.AreEqual(place.center.x, hunt.Root.worldBound.center.x, 2, "And stays centred across.");
        }

        [UnityTest]
        public IEnumerator TheRingAndANationsTokenTurnOverToTheirOtherSide()
        {
            using var client = new MountedMatchClient("http://localhost", "token-flip");
            var view = new BoardFixture().View;
            view.Fellowship.Hidden = true;
            view.Nations[0].Active = false;
            client.Apply(view);
            client.Board.Nations.Open();
            yield return new WaitForSecondsRealtime(.4f);
            var ring = client.Board.Fellowship.Status.Q<Image>("fellowship-ring");
            var token = client.Board.Nations.Drawer.Q(className: "nation-token");
            var hidden = ring.sprite;
            var next = new BoardFixture().View;
            next.Revision = view.Revision + 1;
            next.Fellowship.Hidden = false;
            next.Nations[0].Active = true;
            client.Apply(next);
            Assert.IsTrue(TokenFlip.IsTurning(ring), "The Ring turns over.");
            Assert.AreSame(hidden, ring.sprite, "Its Hidden side shows until the middle of the turn.");
            Assert.IsTrue(TokenFlip.IsTurning(token), "The nation's token turns over.");
            Assert.IsTrue(token.ClassListContains("passive-nation"));
            yield return new WaitForSecondsRealtime(.08f);
            Assert.Less(ring.resolvedStyle.scale.value.x, .9f, "It narrows to its edge.");
            yield return new WaitForSecondsRealtime((float)TokenFlip.HalfSeconds * 2 + .15f);
            Assert.AreSame(FellowshipArtwork.Sprite("revealed"), ring.sprite, "It ends on its Revealed side.");
            Assert.AreEqual(1f, ring.resolvedStyle.scale.value.x, .01f, "At its full width again.");
            Assert.IsTrue(token.ClassListContains("active-nation"));
            Assert.IsFalse(TokenFlip.IsTurning(ring) || TokenFlip.IsTurning(token));
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsTheMarkerAndTheTokensChangeAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var board = stage.Board;
            var ring = board.Fellowship.Status.Q<Image>("fellowship-ring");
            var view = stage.Latest();
            view.Revision++;
            view.Fellowship.Hidden = !board.View.Fellowship.Hidden;
            board.Apply(view);
            Assert.IsFalse(TokenFlip.IsTurning(ring), "No turn.");
            Assert.AreSame(FellowshipArtwork.Sprite(view.Fellowship.Hidden ? "hidden" : "revealed"), ring.sprite);
        }

        // UI-339 (owner 2026-10-25: "the next Hunt sometimes starts from a phase"): the window keeps one ribbon, but a new
        // Hunt starts it afresh — after the last Hunt's Result its marker stands at Roll at once, never gliding back.
        [UnityTest]
        public IEnumerator TheNextHuntStartsAtRollWithoutGlidingBack()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-next");
            var first = HuntLayoutTests.Screen(CommandKind.RerollHunt);
            first.HuntReport = Report("first", first.Revision, false);
            client.Apply(first);
            client.SetOnline();
            var hunt = client.Board.Hunt;
            hunt.Open();
            yield return new WaitForSecondsRealtime(.3f);
            var done = HuntLayoutTests.Screen(CommandKind.RerollHunt);
            done.Revision = first.Revision + 1;
            done.Hunt = null;
            done.HuntReport = Report("first", first.Revision, true);
            done.Options.Clear();
            done.Decision = null;
            client.Apply(done);
            yield return new WaitForSecondsRealtime((float)StageRibbon.GlideSeconds + .4f);
            Assert.AreEqual("hunt-stage-result", Current(hunt.Root.Q("hunt-stages")).name, "The first Hunt shows its Result.");
            var next = HuntLayoutTests.Screen(CommandKind.RerollHunt);
            next.Revision = done.Revision + 1;
            next.Hunt.SourceId = "fellowship-move-2";
            next.Hunt.RerollSources.Clear();
            next.HuntReport = Report("next", next.Revision, false);
            next.Options.Clear();
            next.Decision = null;
            client.Apply(next);
            yield return null;
            yield return null;
            var ribbon = hunt.Root.Q("hunt-stages");
            var roll = Current(ribbon);
            Assert.AreEqual("hunt-stage-roll", roll.name, "The new Hunt is at its roll.");
            yield return new WaitForSecondsRealtime(.08f);
            LayoutTests.Save(client.Viewport, "ui339-next-hunt.png");
            Assert.AreEqual(roll.layout.x - 5, ribbon.Q("stage-glide").layout.x, 1, "Its marker stands at Roll, not on its way back.");
        }

        private static HuntReportView Report(string id, long started, bool completed) =>
            new HuntReportView { Id = id, StartedRevision = started, Completed = completed };

        private static VisualElement Current(VisualElement ribbon) =>
            ribbon.Query(className: "battle-stage").ToList().Single(stage => stage.ClassListContains("current-stage"));

        // The same Hunt, a phase on: its tile drawn and its damage to resolve.
        private static PlayerView Damage(PlayerView reroll)
        {
            var view = HuntLayoutTests.Screen(CommandKind.RerollHunt);
            view.Revision = reroll.Revision + 1;
            view.Hunt.TileId = "base.hunt.special-sh.die.0.1";
            view.Hunt.Face = new HuntTileFaceView { Pool = "special-sh", Value = "die", Stop = true, Source = "base" };
            view.Hunt.DamagePending = true;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.ResolveHuntDamage });
            view.Decision = new DecisionView { Kind = DecisionKind.HuntDamage };
            return view;
        }
    }
}
