using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-304 with two real clients on fresh test scenarios: the Free Peoples' board presents the Shadow's actions through
    // the playback coordinator (with a stand-in presenter until the family presenters ship), so the source army and the
    // spent die stay until their beat ends, later revisions queue behind it, the resulting decision opens only at its
    // boundary, Skip and a reconnect jump to the newest view, and the player's own command leaves once.
    public sealed class OpponentPresentationNetworkTests
    {
        [UnityTest]
        public IEnumerator TheOpponentsMarchesPlayInOrderBeforeTheirBoardAndAReconnectShowsTheLatestState() => Wait(Marches());

        [UnityTest]
        public IEnumerator AnAttackOpensItsDecisionsAtTheirBoundariesThroughSkipAReactionAndOurOwnCommand() => Wait(Attack());

        [UnityTest]
        public IEnumerator AMarchKeepsItsSourceUntilItsBeatEndsAndThenItsFiguresMove() => Wait(Movement());

        private static async Task Marches()
        {
            var session = await TestScenarioRoom.Start("sh-army-move");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var start = fp.Board.View.Revision;
                var first = Movable(sh.Board.View, null);
                var die = ArmyDie(sh.Board.View);
                var board = new List<string>();
                var presenter = new ScriptedCuePresenter(new Dictionary<PresentationCueKind, double> { [PresentationCueKind.March] = 1.5 },
                    cue => board.Add(Board(fp, first.Army)));
                fp.Playback.Register(presenter);
                await session.Send(Side.Shadow, CommandKind.MoveArmies, Move(die, first.Army, first.Target));
                await session.Until(() => presenter.Begun.Count == 1);
                Assert.AreEqual(start, fp.Board.View.Revision, "The board still presents the state before the march.");
                Assert.AreEqual("source;die", board[0], "The moving army stands at its source and the die is unspent.");
                Assert.IsTrue(fp.Playback.Holding);
                CollectionAssert.AreEqual(new[] { first.Army.RegionId, first.Target.RegionId }, presenter.Begun[0].Regions);

                await session.Until(() => sh.Board.View.Decision?.Kind == DecisionKind.ActionSequence);
                var second = Movable(sh.Board.View, first.Target.RegionId);
                await session.Send(Side.Shadow, CommandKind.MoveArmies, Move(sh.Board.View.ActionSequence.DieId, second.Army, second.Target));
                await session.Until(() => presenter.Begun.Count == 2);
                Assert.AreEqual(1, presenter.Ended, "The second march waits for the first.");
                Assert.AreEqual(0, presenter.Finished);
                Assert.AreEqual(start, fp.Board.View.Revision, "A revision received during a beat is never shown on its own.");
                CollectionAssert.AreEqual(new[] { second.Army.RegionId, second.Target.RegionId }, presenter.Begun[1].Regions);

                var connected = fp.Connected;
                fp.Connection.Reconnect();
                await session.Until(() => fp.Connected > connected && !fp.Playback.Holding);
                Assert.AreEqual(1, presenter.Finished, "A reconnect ends the beat at once.");
                Assert.AreEqual(sh.Board.View.Revision, fp.Board.View.Revision, "The newest view, without a catch-up.");
                await Task.Delay(1500);
                Assert.AreEqual(2, presenter.Begun.Count, "Missed history never replays a move.");
                Assert.IsTrue(fp.Board.View.Units.Where(u => second.Army.UnitIds.Contains(u.Id)).All(u => u.Location == second.Target.RegionId));
            }
            finally
            {
                await session.End();
            }
        }

        // The board's own figure motion (UI-77) still runs after the release: the held march flies from its source.
        private static async Task Movement()
        {
            var session = await TestScenarioRoom.Start("sh-army-move");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var march = Movable(sh.Board.View, null);
                var board = new List<string>();
                var staged = new List<int>();
                var flown = 0;
                var presenter = new ScriptedCuePresenter(new Dictionary<PresentationCueKind, double> { [PresentationCueKind.March] = 1 },
                    cue =>
                    {
                        board.Add(Board(fp, march.Army));
                        staged.Add(fp.Board.Presentation.Armies.Where(a => a.RegionId == march.Target.RegionId && a.Side == Side.Shadow).Sum(a => a.Total));
                    });
                fp.Playback.Register(presenter);
                await session.Send(Side.Shadow, CommandKind.MoveArmies, Move(ArmyDie(sh.Board.View), march.Army, march.Target));
                await session.Until(() =>
                {
                    if (fp.Playback.Holding && fp.Board.Map.Figures.ActiveCount > 0)
                        flown = Math.Max(flown, fp.Board.Map.Figures.Moves.Count(m => m.To.Region == march.Target.RegionId));
                    return presenter.Begun.Count == 1;
                });
                Assert.AreEqual("source;die", board[0], "The board's view still has the army at its source and the die unspent.");
                Assert.AreEqual(march.Army.UnitIds.Count, flown, "The recorded movers flew while the view was held.");
                var settled = fp.Playback.Latest.Units.Count(u => u.Side == Side.Shadow && u.Location == march.Target.RegionId && !u.Garrison) +
                    fp.Playback.Latest.Characters.Count(c => c.Side == Side.Shadow && c.Location == march.Target.RegionId && !c.Garrison);
                Assert.AreEqual(settled, staged[0], "The presented destination settled with its true composition.");
                // UI-306: the production movement presenter flew the recorded movers before the stand-in's beat, on the
                // presented board (its view still shows them at the source); the release then replays nothing.
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount, "The release does not replay the presented march.");
                Assert.IsTrue(fp.Board.View.Units.Where(u => march.Army.UnitIds.Contains(u.Id)).All(u => u.Location == march.Target.RegionId));
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Attack()
        {
            var session = await TestScenarioRoom.Start("sh-army-attack");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var opened = new List<bool>();
                var presenter = new ScriptedCuePresenter(new Dictionary<PresentationCueKind, double>
                {
                    [PresentationCueKind.Attack] = 60,
                    [PresentationCueKind.Battle] = .8,
                    [PresentationCueKind.Card] = .8
                }, cue => opened.Add(fp.Board.Battles.IsOpen));
                fp.Playback.Register(presenter);
                var commands = 0;
                fp.Interaction.CommandAnswered += (command, receipt, recovering) => commands++;

                var attack = sh.Board.View.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies
                    .First(a => a.Targets.Any(t => t.RegionId == "north-ithilien"));
                await session.Send(Side.Shadow, CommandKind.AttackArmy, new CommandPayload
                {
                    DieId = ArmyDie(sh.Board.View), MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = attack.RegionId, To = "north-ithilien", UnitIds = attack.UnitIds, CharacterIds = attack.CharacterIds } }
                });
                // UI-305: the production camera frames the attack first; the stand-in's beat follows on the same cue.
                await session.Until(() => presenter.Begun.Any(cue => cue.Kind == PresentationCueKind.Attack));
                Assert.IsNull(fp.Board.View.Battle, "The battle is not on the board before its attack is shown.");
                Assert.IsTrue(opened[0], "UI-311: its window opens once the attack's trace ends, inside the presentation.");
                fp.Playback.Skip();
                Assert.IsNull(fp.Board.View.Battle, "The newest view follows on the next frame, so the skip click opens nothing.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(1, presenter.Finished);
                Assert.AreEqual(fp.Playback.Latest.Revision, fp.Board.View.Revision);
                Assert.IsNotNull(fp.Board.View.Battle);
                Assert.IsTrue(fp.Board.Battles.IsOpen, "The new battle opens its window at the release.");

                // The Shadow's Combat card choice is the opponent's reaction; ours opens only after it is shown.
                await session.Until(() => sh.Board.View.Decision?.Kind == DecisionKind.CombatCard && sh.Board.View.DecisionOwner == Side.Shadow);
                var shown = presenter.Begun.Count;
                await session.Send(Side.Shadow, CommandKind.CommitCombatCard, new CommandPayload { CardId = Card(sh.Board.View) });
                await session.Until(() => presenter.Begun.Count > shown || fp.Board.View.Decision?.Kind == DecisionKind.CombatCard);
                Assert.Greater(presenter.Begun.Count, shown, "The opponent's commitment is presented.");
                Assert.IsTrue(presenter.Begun.Skip(shown).All(cue => cue.Actor == Side.Shadow || !cue.Actor.HasValue));
                Assert.AreNotEqual(DecisionKind.CombatCard, fp.Board.View.Decision?.Kind, "Our choice waits for its causal beat.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(DecisionKind.CombatCard, fp.Board.View.Decision.Kind);
                Assert.AreEqual(Side.FreePeoples, fp.Board.View.DecisionOwner);

                // Our own command: one click commits; the shared rolls it completes are shown before the next decision.
                await session.Until(() => NoCard(fp)?.enabledInHierarchy == true && NoCard(fp).worldBound.width > 0);
                await session.Settle();
                var noCard = NoCard(fp);
                var before = fp.Board.View.Revision;
                shown = presenter.Begun.Count;
                PointerGestureEvents.ClickAt(noCard);
                await session.Until(() => fp.Playback.Latest?.Revision > before);
                await session.Until(() => presenter.Begun.Count > shown || !fp.Playback.Holding);
                Assert.Greater(presenter.Begun.Count, shown, "The shared rolls our click completed are presented.");
                Assert.IsTrue(fp.Playback.Holding);
                Assert.AreEqual(before, fp.Board.View.Revision, "The rolls play on the board as it was.");
                Assert.IsFalse(fp.Board.Tray.enabledInHierarchy, "Nothing can be sent while they play.");
                Assert.IsTrue(presenter.Begun.Skip(shown).All(cue => !cue.Actor.HasValue), "Only the shared results, never our own choice.");
                await session.Until(() => !fp.Playback.Holding);
                await session.Settle();
                Assert.AreEqual(1, commands, "The click sent one command, answered once.");
                Assert.AreEqual(fp.Playback.Latest.Revision, fp.Board.View.Revision);
                await session.Until(() => sh.Board.View.Revision == fp.Board.View.Revision);
            }
            finally
            {
                await session.End();
            }
        }

        private static (ArmyOption Army, ArmyTargetOption Target) Movable(PlayerView view, string excluded)
        {
            var option = view.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var army = option.Armies.First(a => !a.Garrison && a.RegionId != excluded && a.UnitIds.Count > 0 &&
                a.Targets.Any(t => t.BattleMode == null && t.RegionId != excluded));
            return (army, army.Targets.First(t => t.BattleMode == null && t.RegionId != excluded));
        }

        private static CommandPayload Move(string die, ArmyOption army, ArmyTargetOption target) => new CommandPayload
        {
            Stepwise = true, DieId = die, MovementCost = DieFace.Army,
            Moves = { new ArmyMoveChoice
            {
                From = army.RegionId, To = target.RegionId, Route = target.Route, UnitIds = army.UnitIds, CharacterIds = army.CharacterIds
            } }
        };

        private static Button NoCard(MountedMatchClient client) => client.Board.Tray.Q<Button>("battle-no-card");

        private static string ArmyDie(PlayerView view) =>
            view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id;

        private static string Card(PlayerView view) =>
            view.Options.First(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds.FirstOrDefault() ?? "";

        // What the presented board shows of a march when its beat begins: its army at the source, its die unspent.
        private static string Board(MountedMatchClient client, ArmyOption army)
        {
            var view = client.Board.View;
            var source = army.UnitIds.All(id => view.Units.Any(u => u.Id == id && u.Location == army.RegionId)) ? "source" : "moved";
            var die = view.Dice.Any(d => d.Owner == Side.Shadow && d.Face == DieFace.Army && d.Location == DieLocation.Available) ? "die" : "spent";
            return source + ";" + die;
        }
    }
}
