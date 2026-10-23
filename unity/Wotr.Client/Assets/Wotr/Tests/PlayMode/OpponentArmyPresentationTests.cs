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
        private const string Die = "shadow-army";

        [UnityTest]
        public IEnumerator TwoMarchesWithOneDieShowTheCostOnceKeepTheRearGuardAndMergeWithoutDoubling()
        {
            using var stage = new ArmyStage(1366, 1f, false);
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
            using var stage = new ArmyStage(1920, 1.25f, false);
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
            using var stage = new ArmyStage(1366, 1f, false);
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
            using var stage = new ArmyStage(1366, 1f, false);
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
            using var stage = new ArmyStage(1366, 1f, false);
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
            using var stage = new ArmyStage(1366, 1f, false);
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
                using var stage = new ArmyStage(1366, 1f, reduced);
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
                using var stage = new ArmyStage(width, scale, false);
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

        // One mounted board receiving the Shadow's recorded steps while it presents the Free Peoples' earlier view.
        private sealed class ArmyStage : IDisposable
        {
            private readonly MountedMatchClient client;
            private readonly BoardFixture fixture;
            private readonly PlayerView before;
            private OpponentPresentationModel model;
            private long revision;
            public RenderTexture Texture { get; }
            public BoardScreen Board => client.Board;
            public PresentationPlayback Playback => client.Playback;
            public OpponentPresenters Presenters => client.Presenters;

            public ArmyStage(int width, float scale, bool reduced)
            {
                client = new MountedMatchClient("http://localhost", "ui306-presentation");
                Texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                Texture.Create();
                client.Panel.targetTexture = Texture;
                client.Interface.Preferences.SetScale(scale);
                client.Interface.Preferences.SetReducedMotion(reduced);
                fixture = new BoardFixture();
                before = fixture.View;
                before.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
            }

            public List<string> Put(string nation, UnitKind kind, string region, int count)
            {
                var ids = new List<string>();
                for (var i = 0; i < count; i++)
                    ids.Add(fixture.Put(nation, kind, region).Id);
                return ids;
            }

            public IEnumerator Ready()
            {
                client.Apply(before);
                yield return new WaitForSecondsRealtime(.3f);
                Board.SetConnection("Connected");
                Playback.SetConnection("Connected");
                model = new OpponentPresentationModel(Side.FreePeoples, Board.Presentation);
                model.Baseline(new ActionLogPage[0], 0);
            }

            public PlayerView Latest() => Next(before);

            public PlayerView Next(PlayerView view)
            {
                var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
                next.Revision = view.Revision + 1;
                next.Dice.Single(d => d.Id == Die).Location = DieLocation.Spent;
                return next;
            }

            public ActionLogStep Step(int order, long at = 1) => new ActionLogStep
            {
                Id = "ui306-" + at + "-" + order, ActionId = "ui306-action-" + at, Revision = at, Order = order, Actor = Side.Shadow,
                Kind = "ArmiesMoved"
            };

            // focus: a region far from the action, "between:a:b" for a view that already shows both, or null to keep it.
            public void Play(PlayerView latest, string focus, params ActionLogStep[] steps)
            {
                Focus(focus);
                revision = latest.Revision;
                client.Refresh(latest);
                model.Advance(revision);
                Playback.Enqueue(model.Merge(new[] { new ActionLogPage { Steps = steps.ToList() } }));
                Playback.HistoryRead(revision);
            }

            private void Focus(string focus)
            {
                if (focus == null)
                    return;
                var parts = focus.Split(':');
                if (parts[0] == "between")
                    Board.Map.FocusOn((Anchor(parts[1]) + Anchor(parts[2])) / 2, Board.Map.FitZoom * 1.4f);
                else
                    Board.Map.FocusOn(Anchor(focus), Board.Map.FitZoom * 2.2f);
            }

            public Vector2 Anchor(string region) =>
                new Vector2(Board.Map.Data.Shapes[region].Anchor[0], Board.Map.Data.Shapes[region].Anchor[1]);

            public int Army(string region, bool garrison = false) =>
                Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == Side.Shadow && a.Garrison == garrison).Sum(a => a.Total);

            public bool Flying(string destination) =>
                Board.Map.Figures.ActiveCount > 0 && Board.Map.Figures.Moves.Any(move => move.To.Region == destination);

            public IEnumerator Until(Func<bool> condition)
            {
                var deadline = Time.realtimeSinceStartup + 10;
                while (!condition() && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.IsTrue(condition(), "The presentation did not reach the expected state.");
            }

            public IEnumerator Released()
            {
                yield return Until(() => !Playback.Holding);
                yield return null;
                Assert.AreEqual(revision, Board.View.Revision);
            }

            // The released board matches the latest view: every Shadow stack and settlement.
            public void AssertFinal(PlayerView latest)
            {
                var expected = latest.Units.Where(u => u.Side == Side.Shadow && Board.Map.Data.Regions.ContainsKey(u.Location))
                    .GroupBy(u => (u.Location, u.Garrison)).ToDictionary(g => g.Key, g => g.Count());
                var shown = Board.Presentation.Armies.Where(a => a.Side == Side.Shadow)
                    .ToDictionary(a => (a.RegionId, a.Garrison), a => a.Total);
                CollectionAssert.AreEquivalent(expected, shown);
                foreach (var region in latest.Control.Keys)
                    Assert.AreEqual(latest.Control[region], Board.Presentation.Control.GetValueOrDefault(region), region);
            }

            // The release replays nothing that was presented (or skipped).
            public IEnumerator NothingFlies()
            {
                for (var frame = 0; frame < 20; frame++)
                {
                    Assert.AreEqual(0, Board.Map.Figures.ActiveCount, "Nothing replays after the release.");
                    yield return null;
                }
            }

            public void Dispose()
            {
                client.Dispose();
                Texture.Release();
                Object.Destroy(Texture);
            }
        }
    }
}
