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
    // .artifacts/screenshots (ui306-*). TEST-20 (owner, 2026-11-04): each scene also plays with the Shadow watching.
    public sealed class OpponentArmyPresentationTests
    {
        [UnityTest]
        public IEnumerator TwoMarchesWithOneDieShowTheCostOnceKeepTheRearGuardAndMergeWithoutDoubling([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var leaving = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            var joining = stage.Put(stage.Second, UnitKind.Regular, "moria", 2);
            stage.Put(stage.Second, UnitKind.Regular, "hollin", 1);
            yield return stage.Ready();
            var movers = leaving.Take(2).ToList();
            var latest = stage.Latest();
            Move(latest, movers, "eastern-brown-lands");
            Move(latest, joining, "hollin");
            stage.Play(latest, "grey-havens", stage.Paid(stage.Step(1), DieFace.Army), March(stage, stage.Step(2), "dol-guldur", "eastern-brown-lands", movers),
                March(stage, stage.Step(3), "moria", "hollin", joining));
            yield return stage.Until(() => stage.Flying("eastern-brown-lands"));
            CollectionAssert.AreEquivalent(movers.Select(id => "unit:" + id), stage.Board.Map.Figures.Moves.Select(m => m.To.Key),
                "Exactly the recorded movers fly.");
            Assert.IsTrue(stage.Board.Map.Figures.Moves.All(m => m.From.Region == "dol-guldur"));
            Assert.AreEqual(1, stage.Army("dol-guldur"), "The rear guard stays visibly at the source.");
            Assert.AreEqual(2, stage.Army("eastern-brown-lands"));
            Assert.AreEqual(0, stage.Board.View.Revision, "The board's view is still the presented one.");
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "The die is marked once for both marches.");
            stage.Save("ui306-two-marches-1-first.png");
            yield return stage.Until(() => stage.Flying("hollin"));
            Assert.AreEqual(3, stage.Army("hollin"), "The merge shows the true total, never the movers twice.");
            Assert.AreEqual(0, stage.Army("moria"));
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "The continuation does not reintroduce the cost.");
            stage.Save("ui306-two-marches-2-second.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator TheCameraSettlesOnAnOffScreenSourceBeforeAnyFigureMoves([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1920, 1.25f, false, viewer);
            var army = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, army, "eastern-brown-lands");
            stage.Play(latest, "grey-havens", stage.Paid(stage.Step(1), DieFace.Army), March(stage, stage.Step(2), "dol-guldur", "eastern-brown-lands", army));
            yield return stage.Until(() => stage.Board.Map.Figures.ActiveCount > 0);
            var map = stage.Board.Map;
            var safe = stage.Presenters.Camera.SafeArea();
            Assert.IsTrue(safe.Contains(map.Camera.ToScreen(stage.Anchor("dol-guldur"))), "The source was framed before the flight.");
            Assert.IsFalse(map.EdgePan.Suspended, "The camera had settled.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator ARetreatIntoAStrongholdAndASortieMoveBetweenFieldAndGarrison([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var army = stage.Put(stage.Main, UnitKind.Regular, "north-ithilien", 2);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, army, "minas-morgul", garrison: true);
            stage.Play(latest, "between:north-ithilien:minas-morgul",
                Movement(stage, stage.Step(1), ActionMovementKind.Retreat, "north-ithilien", "minas-morgul", army, garrison: true));
            yield return stage.Until(() => stage.Flying("minas-morgul"));
            Assert.AreEqual(2, stage.Army("minas-morgul", garrison: true), "The retreat ends on the walls.");
            Assert.AreEqual(0, stage.Army("minas-morgul"));
            yield return stage.Released();
            stage.AssertFinal(latest);
            var sortie = stage.Next(latest);
            Move(sortie, army, "minas-morgul", garrison: false);
            stage.Play(sortie, null, March(stage, stage.Step(1, 2), "minas-morgul", "minas-morgul", army, garrison: false));
            yield return stage.Until(() => stage.Flying("minas-morgul"));
            Assert.AreEqual(2, stage.Army("minas-morgul"), "The sortie leaves the walls for the field.");
            Assert.AreEqual(0, stage.Army("minas-morgul", garrison: true));
            yield return stage.Released();
            stage.AssertFinal(sortie);
        }

        // UI-489 (owner, 2026-11-04): a withdrawal as the server records it (EXP-20): an automatic step of the battle, a Retreat
        // from the field onto the walls of one region, with its participants in the Garrison. The figures rise, fly and land
        // on the walls, for the opponent with the camera framing it.
        [UnityTest]
        public IEnumerator TheOpponentsWithdrawalFliesFromTheFieldOntoTheWalls([Values(Side.FreePeoples, Side.Shadow)] Side viewer,
            [Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced, viewer);
            var region = viewer == Side.FreePeoples ? "minas-morgul" : "helms-deep";
            var army = stage.Put(stage.Main, UnitKind.Regular, region, 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, army, region, garrison: true);
            stage.Play(latest, "grey-havens", Withdrawal(stage, stage.Opponent, region, army));
            yield return stage.Until(() => stage.Presenters.Trace.Lit.Any());
            Assert.AreEqual(3, stage.Army(region), "They stand in the field while they are marked.");
            yield return stage.Until(() => stage.Flying(region));
            Assert.IsTrue(stage.Board.Map.Figures.Moves.All(move => !move.From.Garrison && move.To.Garrison && move.From.Region == region),
                "From the field onto the walls of the same region.");
            Assert.IsFalse(stage.Board.Map.Figures.Moves.Any(move => move.IsArrival), "A flight, not an arrival in place.");
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor(region))), "The camera framed it first.");
            Assert.AreEqual(3, stage.Army(region, garrison: true));
            Assert.AreEqual(0, stage.Army(region));
            Assert.AreEqual(0, stage.Board.View.Units.Count(u => army.Contains(u.Id) && u.Garrison), "The board's view is still the presented one.");
            if (!reduced)
            {
                yield return new WaitForSecondsRealtime(.25f);
                LayoutTests.Save(stage.Texture, "ui489-withdrawal-" + viewer + ".png");
            }
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        // The withdrawing player's own click plays the same shared step, as their own retreat does; the camera stays theirs.
        [UnityTest]
        public IEnumerator OurOwnWithdrawalPlaysAsOurOwnRetreatWithTheCameraKept([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var region = viewer == Side.FreePeoples ? "helms-deep" : "minas-morgul";
            var own = stage.Put(stage.Own, UnitKind.Regular, region, 2);
            yield return stage.Ready();
            var latest = stage.Latest();
            Move(latest, own, region, garrison: true);
            var pan = stage.Board.Map.Camera.Pan;
            stage.Play(latest, null, Withdrawal(stage, viewer, region, own));
            yield return stage.Until(() => stage.Flying(region));
            Assert.IsTrue(stage.Board.Map.Figures.Moves.All(move => !move.From.Garrison && move.To.Garrison));
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan, "After the player's own click the camera stays theirs.");
            yield return stage.Released();
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan);
            Assert.AreEqual(2, stage.Board.Presentation.FigureCount(region, viewer, true));
            Assert.AreEqual(0, stage.Board.Presentation.FigureCount(region, viewer, false));
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator APartialAdvanceMovesOnlyItsFiguresAndAnEmptyOneMovesNothing([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var army = stage.Put(stage.Main, UnitKind.Regular, "south-ithilien", 3);
            yield return stage.Ready();
            var there = stage.Army("osgiliath");
            var advancing = army.Take(2).ToList();
            var latest = stage.Latest();
            Move(latest, advancing, "osgiliath");
            stage.Play(latest, "between:south-ithilien:osgiliath",
                Movement(stage, stage.Step(1), ActionMovementKind.Advance, "south-ithilien", "osgiliath", advancing));
            yield return stage.Until(() => stage.Flying("osgiliath"));
            Assert.AreEqual(1, stage.Army("south-ithilien"));
            Assert.AreEqual(there + 2, stage.Army("osgiliath"));
            yield return stage.Released();
            stage.AssertFinal(latest);
            var empty = stage.Next(latest);
            stage.Play(empty, null, Movement(stage, stage.Step(1, 2), ActionMovementKind.Advance, "south-ithilien", "osgiliath", new List<string>()));
            yield return stage.Released();
            Assert.AreEqual(0, stage.Board.Map.Figures.ActiveCount, "An empty advance leaves the figures in place.");
            stage.AssertFinal(empty);
        }

        [UnityTest]
        public IEnumerator ACaptureChangesTheGemAndItsPointsOnlyWithTheRecordedTakeover([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var (scoring, plain) = viewer == Side.FreePeoples ? ("pelargir", "osgiliath") : ("moria", "north-dunland");
            var enamel = viewer == Side.FreePeoples ? "observed-badge-shadow" : "observed-badge-free";
            var latest = stage.Latest();
            latest.Control[scoring] = stage.Opponent;
            latest.Control[plain] = stage.Opponent;
            stage.Play(latest, "between:" + scoring + ":" + plain,
                Control(stage.Step(1), scoring, viewer, stage.Opponent), Control(stage.Step(2), plain, viewer, stage.Opponent));
            yield return stage.Until(() => stage.Presenters.Trace.Count > 0 && stage.Board.Presentation.Control[scoring] == stage.Opponent);
            Assert.AreEqual(viewer, stage.Board.View.Control[scoring], "The presented view keeps its control.");
            var badge = stage.Board.Map.Q<Label>("observed-badge");
            Assert.IsNotNull(badge, "A scoring settlement shows its points.");
            Assert.AreEqual("+" + stage.Board.Presentation.Regions[scoring].VictoryPoints + " VP", badge.text);
            Assert.IsTrue(badge.ClassListContains(enamel));
            Assert.AreEqual(PickingMode.Ignore, badge.pickingMode);
            yield return new WaitForSecondsRealtime(.3f);
            stage.Save("ui306-capture.png");
            yield return stage.Until(() => stage.Board.Presentation.Control.GetValueOrDefault(plain) == stage.Opponent);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(0, stage.Board.Map.Query<Label>("observed-badge").ToList().Count(label => label.resolvedStyle.opacity > .5f &&
                label.parent.ClassListContains("shown")), "A region without a scoring settlement gains no point.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            var retaken = stage.Next(latest);
            retaken.Control[scoring] = viewer;
            stage.Play(retaken, null, Control(stage.Step(1, 2), scoring, stage.Opponent, viewer));
            yield return stage.Until(() => stage.Board.Map.Q<Label>("observed-badge")?.text.StartsWith("−") == true);
            Assert.IsTrue(stage.Board.Map.Q<Label>("observed-badge").ClassListContains(enamel), "The side that captured it loses the point.");
            yield return stage.Released();
            stage.AssertFinal(retaken);
        }

        [UnityTest]
        public IEnumerator AnAttackTracesTheAttackersWhereTheyStandAndOpensTheBattleAtTheRelease([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var attackers = stage.Put(stage.Main, UnitKind.Regular, "minas-morgul", 3);
            if (viewer == Side.Shadow)
                stage.Put(stage.Own, UnitKind.Regular, "osgiliath", 1);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Battle = Battle(stage, "minas-morgul", "osgiliath", attackers);
            stage.Play(latest, "between:minas-morgul:osgiliath", Attack(stage.Step(1), latest.Battle));
            yield return stage.Until(() => stage.Presenters.Trace.Count > 0);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(3, stage.Army("minas-morgul"), "A recorded attack is not an occupation.");
            Assert.AreEqual(0, stage.Board.Map.Figures.ActiveCount);
            Assert.IsFalse(stage.Board.Battles.IsOpen);
            Assert.AreEqual(2, stage.Board.Map.Query(className: PresentationGlow.ClassName).ToList().Count,
                "UI-314: both forces glow, the attackers and the defenders.");
            stage.Save("ui306-attack.png");
            yield return stage.Released();
            Assert.IsTrue(stage.Board.Battles.IsOpen, "The battle window opens at its normal host after the attack.");
        }

        [UnityTest]
        public IEnumerator SkipAndReducedMotionEndOnTheSameCountsWithoutAReplay([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            foreach (var reduced in new[] { false, true })
            {
                using var stage = new PresentedBoardStage(1366, 1f, reduced, viewer);
                var army = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
                var second = stage.Put(stage.Second, UnitKind.Regular, "moria", 2);
                yield return stage.Ready();
                var latest = stage.Latest();
                Move(latest, army, "eastern-brown-lands");
                Move(latest, second, "hollin");
                stage.Play(latest, null, stage.Paid(stage.Step(1), DieFace.Army), March(stage, stage.Step(2), "dol-guldur", "eastern-brown-lands", army),
                    March(stage, stage.Step(3), "moria", "hollin", second));
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
                var leaving = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
                var joining = stage.Put(stage.Second, UnitKind.Regular, "moria", 2);
                yield return stage.Ready();
                var movers = leaving.Take(2).ToList();
                var latest = stage.Latest();
                Move(latest, movers, "eastern-brown-lands");
                Move(latest, joining, "hollin");
                stage.Play(latest, "grey-havens", stage.Paid(stage.Step(1), DieFace.Army), March(stage, stage.Step(2), "dol-guldur", "eastern-brown-lands", movers),
                    March(stage, stage.Step(3), "moria", "hollin", joining));
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

        // The decision moves nothing; the automatic StrongholdShelter step records the battle's retreat onto the walls.
        private static ActionLogStep[] Withdrawal(PresentedBoardStage stage, Side actor, string region, IEnumerable<string> ids)
        {
            var decision = stage.Step(1);
            decision.Actor = actor;
            decision.Command = CommandKind.RetreatIntoSiege;
            decision.Context.BattleId = "ui489-battle";
            var shelter = stage.Step(2);
            shelter.Actor = null;
            shelter.Automatic = true;
            shelter.Kind = "StrongholdShelter";
            shelter.Context.BattleId = "ui489-battle";
            shelter.Context.Regions.Add(region);
            shelter.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.Retreat, From = region, To = region, Route = { region },
                Participants = ids.Select(id => new ActionParticipant { Id = id, Side = actor, UnitKind = UnitKind.Regular, Garrison = true }).ToList()
            });
            return new[] { decision, shelter };
        }

        private static void Move(PlayerView view, IEnumerable<string> ids, string region, bool garrison = false)
        {
            foreach (var unit in view.Units.Where(u => ids.Contains(u.Id)))
            {
                unit.Location = region;
                unit.Garrison = garrison;
            }
        }

        private static ActionLogStep March(PresentedBoardStage stage, ActionLogStep step, string from, string to, IEnumerable<string> ids, bool garrison = false) =>
            Movement(stage, step, ActionMovementKind.March, from, to, ids, garrison);

        private static ActionLogStep Movement(PresentedBoardStage stage, ActionLogStep step, ActionMovementKind kind, string from, string to,
            IEnumerable<string> ids, bool garrison = false)
        {
            step.Movements.Add(new ActionMovement
            {
                Kind = kind, From = from, To = to,
                Participants = ids.Select(id => new ActionParticipant { Id = id, Side = stage.Opponent, UnitKind = UnitKind.Regular, Garrison = garrison })
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

        private static BattleView Battle(PresentedBoardStage stage, string from, string to, IEnumerable<string> attackers) => new BattleView
        {
            Id = "ui306-battle", RegionId = to, Round = 1,
            Attacker = new BattleForceView { Side = stage.Opponent, RegionId = from, ParticipantIds = attackers.ToList() },
            Defender = new BattleForceView { Side = stage.Viewer, RegionId = to }
        };
    }
}
