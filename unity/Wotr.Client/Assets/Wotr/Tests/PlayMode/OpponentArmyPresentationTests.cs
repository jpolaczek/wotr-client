using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-306 on a mounted board: the Shadow's recorded army actions presented to the Free Peoples by the production
    // presenters. The presented board keeps the earlier view while only the recorded movers fly; the rear guard stays,
    // the destination settles with its true count, control and victory points change only with the recorded capture, and
    // the release neither replays a move nor lands on other counts — also after Skip and under Reduced motion. Frames go to
    // .artifacts/screenshots (ui306-*).
    public sealed class OpponentArmyPresentationTests
    {
        private const string Die = PresentedBoardStage.Die;

        [UnityTest]
        public IEnumerator TwoMarchesWithOneDieShowTheCostOnceKeepTheRearGuardAndMergeWithoutDoubling()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var leaving = stage.Put("sauron", UnitKind.Regular, "dol-guldur", 3);
            var joining = stage.Put("isengard", UnitKind.Regular, "moria", 2);
            stage.Put("isengard", UnitKind.Regular, "hollin", 1);
            yield return stage.Ready();
            var movers = leaving.Take(2).ToList();
            var latest = stage.Latest();
            Move(latest, movers, "eastern-brown-lands");
            Move(latest, joining, "hollin");
            stage.Play(latest, "grey-havens", Cost(stage.Step(1)), March(stage.Step(2), "dol-guldur", "eastern-brown-lands", movers),
                March(stage.Step(3), "moria", "hollin", joining));
            yield return stage.Until(() => stage.Flying("eastern-brown-lands"));
            CollectionAssert.AreEquivalent(movers.Select(id => "unit:" + id), stage.Board.Map.Figures.Moves.Select(m => m.To.Key),
                "Exactly the recorded movers fly.");
            Assert.IsTrue(stage.Board.Map.Figures.Moves.All(m => m.From.Region == "dol-guldur"));
            Assert.AreEqual(1, stage.Army("dol-guldur"), "The rear guard stays visibly at the source.");
            Assert.AreEqual(2, stage.Army("eastern-brown-lands"));
            Assert.AreEqual(0, stage.Board.View.Revision, "The board's view is still the presented one.");
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "The die is marked once for both marches.");
            LayoutTests.Save(stage.Texture, "ui306-two-marches-1-first.png");
            yield return stage.Until(() => stage.Flying("hollin"));
            Assert.AreEqual(3, stage.Army("hollin"), "The merge shows the true total, never the movers twice.");
            Assert.AreEqual(0, stage.Army("moria"));
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "The continuation does not reintroduce the cost.");
            LayoutTests.Save(stage.Texture, "ui306-two-marches-2-second.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator TheCameraSettlesOnAnOffScreenSourceBeforeAnyFigureMoves()
        {
            using var stage = new PresentedBoardStage(1920, 1.25f, false);
            var army = stage.Put("sauron", UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, army, "eastern-brown-lands");
            stage.Play(latest, "grey-havens", Cost(stage.Step(1)), March(stage.Step(2), "dol-guldur", "eastern-brown-lands", army));
            yield return stage.Until(() => stage.Board.Map.Figures.ActiveCount > 0);
            var map = stage.Board.Map;
            var safe = stage.Presenters.Camera.SafeArea();
            Assert.IsTrue(safe.Contains(map.Camera.ToScreen(stage.Anchor("dol-guldur"))), "The source was framed before the flight.");
            Assert.IsFalse(map.EdgePan.Suspended, "The camera had settled.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator ARetreatIntoAStrongholdAndASortieMoveBetweenFieldAndGarrison()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var army = stage.Put("sauron", UnitKind.Regular, "north-ithilien", 2);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, army, "minas-morgul", garrison: true);
            stage.Play(latest, "between:north-ithilien:minas-morgul",
                Movement(stage.Step(1), ActionMovementKind.Retreat, "north-ithilien", "minas-morgul", army, garrison: true));
            yield return stage.Until(() => stage.Flying("minas-morgul"));
            Assert.AreEqual(2, stage.Army("minas-morgul", garrison: true), "The retreat ends on the walls.");
            Assert.AreEqual(0, stage.Army("minas-morgul"));
            yield return stage.Released();
            stage.AssertFinal(latest);
            var sortie = stage.Next(latest);
            Move(sortie, army, "minas-morgul", garrison: false);
            stage.Play(sortie, null, March(stage.Step(1, 2), "minas-morgul", "minas-morgul", army, garrison: false));
            yield return stage.Until(() => stage.Flying("minas-morgul"));
            Assert.AreEqual(2, stage.Army("minas-morgul"), "The sortie leaves the walls for the field.");
            Assert.AreEqual(0, stage.Army("minas-morgul", garrison: true));
            yield return stage.Released();
            stage.AssertFinal(sortie);
        }

        [UnityTest]
        public IEnumerator APartialAdvanceMovesOnlyItsFiguresAndAnEmptyOneMovesNothing()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var army = stage.Put("sauron", UnitKind.Regular, "south-ithilien", 3);
            yield return stage.Ready();
            var advancing = army.Take(2).ToList();
            var latest = stage.Latest();
            Move(latest, advancing, "osgiliath");
            stage.Play(latest, "between:south-ithilien:osgiliath",
                Movement(stage.Step(1), ActionMovementKind.Advance, "south-ithilien", "osgiliath", advancing));
            yield return stage.Until(() => stage.Flying("osgiliath"));
            Assert.AreEqual(1, stage.Army("south-ithilien"));
            Assert.AreEqual(2, stage.Army("osgiliath"));
            yield return stage.Released();
            stage.AssertFinal(latest);
            var empty = stage.Next(latest);
            stage.Play(empty, null, Movement(stage.Step(1, 2), ActionMovementKind.Advance, "south-ithilien", "osgiliath", new List<string>()));
            yield return stage.Released();
            Assert.AreEqual(0, stage.Board.Map.Figures.ActiveCount, "An empty advance leaves the figures in place.");
            stage.AssertFinal(empty);
        }

        [UnityTest]
        public IEnumerator ACaptureChangesTheGemAndItsPointsOnlyWithTheRecordedTakeover()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Control["pelargir"] = Side.Shadow;
            latest.Control["osgiliath"] = Side.Shadow;
            stage.Play(latest, "between:pelargir:osgiliath",
                Control(stage.Step(1), "pelargir", Side.FreePeoples, Side.Shadow), Control(stage.Step(2), "osgiliath", Side.FreePeoples, Side.Shadow));
            yield return stage.Until(() => stage.Presenters.Trace.Count > 0 && stage.Board.Presentation.Control["pelargir"] == Side.Shadow);
            Assert.AreEqual(Side.FreePeoples, stage.Board.View.Control["pelargir"], "The presented view keeps its control.");
            var badge = stage.Board.Map.Q<Label>("observed-badge");
            Assert.IsNotNull(badge, "A scoring settlement shows its points.");
            Assert.AreEqual("+" + stage.Board.Presentation.Regions["pelargir"].VictoryPoints + " VP", badge.text);
            Assert.IsTrue(badge.ClassListContains("observed-badge-shadow"));
            Assert.AreEqual(PickingMode.Ignore, badge.pickingMode);
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui306-capture.png");
            yield return stage.Until(() => stage.Board.Presentation.Control.GetValueOrDefault("osgiliath") == Side.Shadow);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0, stage.Board.Map.Query<Label>("observed-badge").ToList().Count(label => label.resolvedStyle.opacity > .5f &&
                label.parent.ClassListContains("shown")), "A region without a scoring settlement gains no point.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            var retaken = stage.Next(latest);
            retaken.Control["pelargir"] = Side.FreePeoples;
            stage.Play(retaken, null, Control(stage.Step(1, 2), "pelargir", Side.Shadow, Side.FreePeoples));
            yield return stage.Until(() => stage.Board.Map.Q<Label>("observed-badge")?.text.StartsWith("−") == true);
            Assert.IsTrue(stage.Board.Map.Q<Label>("observed-badge").ClassListContains("observed-badge-shadow"), "The Shadow loses the point.");
            yield return stage.Released();
            stage.AssertFinal(retaken);
        }

        [UnityTest]
        public IEnumerator AnAttackTracesTheAttackersWhereTheyStandAndOpensTheBattleAtTheRelease()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var attackers = stage.Put("sauron", UnitKind.Regular, "minas-morgul", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Battle = Battle("minas-morgul", "osgiliath", attackers);
            stage.Play(latest, "between:minas-morgul:osgiliath", Attack(stage.Step(1), latest.Battle));
            yield return stage.Until(() => stage.Presenters.Trace.Count > 0);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(3, stage.Army("minas-morgul"), "A recorded attack is not an occupation.");
            Assert.AreEqual(0, stage.Board.Map.Figures.ActiveCount);
            Assert.IsFalse(stage.Board.Battles.IsOpen);
            Assert.AreEqual(2, stage.Board.Map.Query(className: "observed-ring").ToList().Count(ring => ring.resolvedStyle.display == DisplayStyle.Flex));
            LayoutTests.Save(stage.Texture, "ui306-attack.png");
            yield return stage.Released();
            Assert.IsTrue(stage.Board.Battles.IsOpen, "The battle window opens at its normal host after the attack.");
        }

        [UnityTest]
        public IEnumerator SkipAndReducedMotionEndOnTheSameCountsWithoutAReplay()
        {
            foreach (var reduced in new[] { false, true })
            {
                using var stage = new PresentedBoardStage(1366, 1f, reduced);
                var army = stage.Put("sauron", UnitKind.Regular, "dol-guldur", 3);
                var second = stage.Put("isengard", UnitKind.Regular, "moria", 2);
                yield return stage.Ready();
                var latest = stage.Latest();
                Move(latest, army, "eastern-brown-lands");
                Move(latest, second, "hollin");
                stage.Play(latest, null, Cost(stage.Step(1)), March(stage.Step(2), "dol-guldur", "eastern-brown-lands", army),
                    March(stage.Step(3), "moria", "hollin", second));
                yield return stage.Until(() => stage.Flying("eastern-brown-lands"));
                if (!reduced)
                    PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
                yield return stage.Released();
                stage.AssertFinal(latest);
                yield return stage.NothingFlies();
            }
        }

        [UnityTest]
        public IEnumerator TwoMarchesAreRecordedAsClipsAtBothSizesAndScales()
        {
            foreach (var (width, scale) in new[] { (1366, 1f), (1920, 1.25f) })
            {
                using var stage = new PresentedBoardStage(width, scale, false);
                var leaving = stage.Put("sauron", UnitKind.Regular, "dol-guldur", 3);
                var joining = stage.Put("isengard", UnitKind.Regular, "moria", 2);
                yield return stage.Ready();
                var movers = leaving.Take(2).ToList();
                var latest = stage.Latest();
                Move(latest, movers, "eastern-brown-lands");
                Move(latest, joining, "hollin");
                stage.Play(latest, "grey-havens", Cost(stage.Step(1)), March(stage.Step(2), "dol-guldur", "eastern-brown-lands", movers),
                    March(stage.Step(3), "moria", "hollin", joining));
                var name = "ui306-clip-marches-" + width + "-" + scale.ToString("0.00") + "-";
                var frame = 0;
                while (stage.Playback.Holding && frame < 60)
                {
                    LayoutTests.Save(stage.Texture, name + frame.ToString("00") + ".png");
                    frame++;
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Assert.IsFalse(stage.Playback.Holding);
                stage.AssertFinal(latest);
            }
        }

        private static void Move(PlayerView view, IEnumerable<string> ids, string region, bool garrison = false)
        {
            foreach (var unit in view.Units.Where(u => ids.Contains(u.Id)))
            {
                unit.Location = region;
                unit.Garrison = garrison;
            }
        }

        private static ActionLogStep Cost(ActionLogStep step)
        {
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Use = DieFace.Army });
            return step;
        }

        private static ActionLogStep March(ActionLogStep step, string from, string to, IEnumerable<string> ids, bool garrison = false) =>
            Movement(step, ActionMovementKind.March, from, to, ids, garrison);

        private static ActionLogStep Movement(ActionLogStep step, ActionMovementKind kind, string from, string to, IEnumerable<string> ids,
            bool garrison = false)
        {
            step.Movements.Add(new ActionMovement
            {
                Kind = kind, From = from, To = to,
                Participants = ids.Select(id => new ActionParticipant { Id = id, Side = Side.Shadow, UnitKind = UnitKind.Regular, Garrison = garrison })
                    .ToList()
            });
            return step;
        }

        private static ActionLogStep Control(ActionLogStep step, string region, Side before, Side after)
        {
            step.Facts.Control.Add(new ActionControlChange { RegionId = region, Before = before, After = after });
            return step;
        }

        private static ActionLogStep Attack(ActionLogStep step, BattleView battle)
        {
            step.Context.BattleId = battle.Id;
            step.Facts.Battle = battle;
            return step;
        }

        private static BattleView Battle(string from, string to, IEnumerable<string> attackers) => new BattleView
        {
            Id = "ui306-battle", RegionId = to, Round = 1,
            Attacker = new BattleForceView { Side = Side.Shadow, RegionId = from, ParticipantIds = attackers.ToList() },
            Defender = new BattleForceView { Side = Side.FreePeoples, RegionId = to }
        };
    }
}
