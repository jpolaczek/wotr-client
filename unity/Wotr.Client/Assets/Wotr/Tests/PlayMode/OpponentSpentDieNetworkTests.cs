using System;
using System.Collections;
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
    // UI-487/UI-488 (owner, 2026-11-04) with two real clients on fresh test scenarios: a Muster die with two points leaves
    // once, at the end, for both viewers; a whole round of forfeits ends the turn on one player's own click, whose board
    // still shows the opponent's recovered dice and draws; and the Shadow's own allocation shows the Free Peoples' roll.
    // Frames go to .artifacts/screenshots (ui487-network-*, ui488-network-*).
    public sealed class OpponentSpentDieNetworkTests
    {
        [UnityTest]
        public IEnumerator TheShadowsMusterDieLeavesOnceAtTheEnd() => Wait(TwoPoints("sh-muster-recruit", Side.Shadow));

        [UnityTest]
        public IEnumerator TheFreePeoplesMusterDieLeavesOnceAtTheEnd() => Wait(TwoPoints("fp-muster-recruit", Side.FreePeoples));

        [UnityTest]
        public IEnumerator TheEndOfTheTurnAndTheRollShowOnTheClickersOwnBoard() => Wait(WholeTurn());

        [UnityTest]
        public IEnumerator TheFellowshipsDieEntersTheHuntBoxOnTheShadowsBoard() => Wait(HuntDie());

        [UnityTest]
        public IEnumerator MordorInactivityShowsOnTheClickersOwnBoard() => Wait(Inactivity());

        private static async Task TwoPoints(string scenario, Side actor)
        {
            var session = await TestScenarioRoom.Start(scenario);
            try
            {
                var acting = session.Client(actor);
                var watching = session.Client(actor == Side.Shadow ? Side.FreePeoples : Side.Shadow);
                var die = MusterDie(acting.Board.View);
                var recruit = acting.Board.View.Options.First(o => o.Kind == CommandKind.Recruit).Recruitment
                    .First(r => r.Kind == UnitKind.Regular && r.ReserveIds.Count > 0 && r.Targets.Count > 0);
                await session.Send(actor, CommandKind.Recruit, new CommandPayload
                {
                    Stepwise = true, DieId = die, Placements = { new UnitPlacementChoice { UnitId = recruit.ReserveIds[0], RegionId = recruit.Targets[0] } }
                });
                var flown = await WatchUntilReleased(watching, () => Piece(watching) != null);
                Assert.IsFalse(flown, "The first point spends nothing yet.");
                Assert.IsNotNull(watching.Board.OpponentDice.Q<Button>(die), "It stays in its row for the second point.");
                await session.Until(() => !acting.Playback.Holding && acting.Board.View.Decision != null);
                await session.Send(actor, CommandKind.FinishActionSequence, new CommandPayload());
                var chip = watching.Board.TopBar.Opponent.Root.Q("opponent-spent");
                await session.Until(() => Piece(watching) != null);
                LayoutTests.Save(watching.Viewport, "ui487-network-" + scenario + ".png");
                await session.Until(() => !watching.Playback.Holding);
                Assert.AreEqual(DieLocation.Spent, watching.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.IsNull(watching.Board.OpponentDice.Q<Button>(die), "It left its row.");
                Assert.AreEqual("1", chip.Q<Label>(className: "summary-value").text);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task WholeTurn()
        {
            var session = await TestScenarioRoom.Start("fp-event-draw");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await ForfeitTheTurn(session, Ended);
                await session.Until(() => !fp.Playback.Holding && !sh.Playback.Holding);
                await Continue(session, fp, sh);
            }
            finally
            {
                await session.End();
            }
        }

        // Both sides forfeit every die; `ended` watches the board of each click until one of them ends the turn.
        private static async Task ForfeitTheTurn(TestScenarioRoom session, Func<MountedMatchClient, int, Task<bool>> ended)
        {
            var fp = session.Client(Side.FreePeoples);
            var sh = session.Client(Side.Shadow);
            var turn = fp.Board.View.Turn;
            while (fp.Board.View.Turn == turn && sh.Board.View.Turn == turn)
            {
                await session.Until(() => !fp.Playback.Holding && !sh.Playback.Holding);
                if (fp.Board.View.Turn != turn || sh.Board.View.Turn != turn)
                    return;
                var side = fp.Board.View.DecisionOwner ?? Side.FreePeoples;
                var clicker = session.Client(side);
                await session.Until(() => clicker.Board.View.Decision != null);
                var die = clicker.Board.View.Dice.First(d => d.Owner == side && d.Location == DieLocation.Available).Id;
                await session.Send(side, CommandKind.DiscardDie, new CommandPayload { DieId = die });
                await session.Until(() => clicker.Playback.Holding || clicker.Board.View.Dice.Single(d => d.Id == die).Location != DieLocation.Available);
                if (await ended(clicker, turn))
                    return;
            }
        }

        // E14 of the audit on the real server: the die stays Resolving through the Hunt, then enters the Box.
        private static async Task HuntDie()
        {
            var session = await TestScenarioRoom.Start("fp-character-move-fellowship");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var die = fp.Board.View.Options.First(o => o.Kind == CommandKind.MoveFellowship).EligibleDice.First();
                var count = sh.Board.Hunt.Box.Counts.Q<Label>("hunt-free-dice");
                var before = int.Parse(count.text);
                await session.Send(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = die });
                var flown = false;
                var deadline = DateTime.UtcNow.AddSeconds(40);
                while (Location(sh, die) == DieLocation.Available || Location(sh, die) == DieLocation.Resolving)
                {
                    Assert.Less(DateTime.UtcNow, deadline, "The die never left: " + Location(sh, die) + "; decision " + fp.Board.View.Decision?.Kind + " of " + fp.Board.View.DecisionOwner + "; holding " + fp.Playback.Holding + "/" + sh.Playback.Holding);
                    if (!flown && Piece(sh) != null)
                    {
                        flown = true;
                        await session.Until(() => Piece(sh) == null || PresentationGlow.IsLit(sh.Board.Hunt.Box.Count(Side.FreePeoples)));
                        LayoutTests.Save(sh.Viewport, "ui487-network-hunt-box.png");
                    }
                    await AnswerTheHunt(session, fp, sh);
                    await Task.Delay(20);
                }
                await session.Until(() => !sh.Playback.Holding);
                Assert.IsTrue(flown, "The die flies to the plaque.");
                Assert.AreEqual(DieLocation.Hunt, Location(sh, die), "The rules place it in the Box.");
                Assert.AreEqual((before + 1).ToString(), count.text);
            }
            finally
            {
                await session.End();
            }
        }

        private static DieLocation Location(MountedMatchClient client, string die) => client.Board.View.Dice.Single(d => d.Id == die).Location;

        // Whatever the Hunt's roll asks next, answered on a released board.
        private static async Task AnswerTheHunt(TestScenarioRoom session, MountedMatchClient fp, MountedMatchClient sh)
        {
            if (fp.Playback.Holding || sh.Playback.Holding || fp.Board.View.DecisionOwner is not { } owner)
                return;
            var view = session.Client(owner).Board.View;
            if (view.Decision == null || view.Revision != session.Client(owner == Side.FreePeoples ? Side.Shadow : Side.FreePeoples).Board.View.Revision)
                return;
            switch (view.Decision.Kind)
            {
                case DecisionKind.Reaction:
                    await session.Send(owner, CommandKind.DeclineReaction, new CommandPayload());
                    break;
                case DecisionKind.HuntReroll:
                    await session.Send(owner, CommandKind.RerollHunt, new CommandPayload());
                    break;
                case DecisionKind.HuntDamage:
                    await session.Send(owner, CommandKind.ResolveHuntDamage, new CommandPayload { HuntResponse = HuntResponse.Ring });
                    break;
                case DecisionKind.FellowshipRoute:
                    var travel = view.Options.Single(o => o.Kind == CommandKind.RevealFellowship).Travel.Single();
                    await session.Send(owner, CommandKind.RevealFellowship, new CommandPayload { Route = { travel.Origin, travel.Targets[0] } });
                    break;
                case DecisionKind.Action:
                    // The board has not received the move yet.
                    break;
                default:
                    Assert.Ignore("This roll asked for " + view.Decision.Kind + ", which this test does not play.");
                    break;
            }
            await session.Settle();
        }

        // Audit E20: the Fellowship in Mordor with no attempt this turn gains a Corruption, on the click that ends the actions.
        private static async Task Inactivity()
        {
            var session = await TestScenarioRoom.Start("fp-mordor-track");
            try
            {
                var moved = false;
                await ForfeitTheTurn(session, async (clicker, turn) =>
                {
                    var side = clicker.Board.View.You;
                    var corruption = clicker.Board.Fellowship.Status.Q<Label>("fellowship-status-corruption");
                    var before = corruption.text;
                    while (clicker.Playback.Holding)
                    {
                        if (!moved && corruption.text != before && clicker.Playback.Latest.Turn != turn)
                        {
                            moved = true;
                            LayoutTests.Save(clicker.Viewport, "ui488-network-mordor-" + side + ".png");
                        }
                        await Task.Delay(20);
                    }
                    return clicker.Board.View.Turn != turn;
                });
                Assert.IsTrue(moved, "The corruption moves in a beat on the board whose own click ended the actions.");
            }
            finally
            {
                await session.End();
            }
        }

        // The click that ended the turn: the opponent's dice and draws play on the clicker's own board, before its view.
        private static async Task<bool> Ended(MountedMatchClient clicker, int turn)
        {
            var drawn = false;
            var recovered = false;
            var side = clicker.Board.View.You;
            var opponent = side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (clicker.Playback.Holding && DateTime.UtcNow < deadline)
            {
                var row = clicker.Board.OpponentDice;
                recovered |= row.Query<Button>().ToList().Any(PresentationGlow.IsLit) && clicker.Board.View.Turn == turn &&
                    clicker.Playback.Latest?.Turn != turn;
                if (!drawn && clicker.Board.Root.Q(className: DrawFlight.BackClass) != null)
                {
                    drawn = true;
                    LayoutTests.Save(clicker.Viewport, "ui488-network-draws-" + side + ".png");
                }
                await Task.Delay(20);
            }
            if (clicker.Board.View.Turn == turn)
                return false;
            Assert.IsTrue(drawn, "The opponent's draws fly on the board whose own click ended the turn.");
            Assert.IsTrue(recovered, "The opponent's recovered dice light in its row.");
            Assert.IsTrue(clicker.Board.View.Players.Single(p => p.Side == opponent).HandCount > 0);
            return true;
        }

        // The next turn's Hunt allocation: the Shadow's own click also rolls the Free Peoples' dice.
        private static async Task Continue(TestScenarioRoom session, MountedMatchClient fp, MountedMatchClient sh)
        {
            await session.Until(() => fp.Board.View.Decision?.Kind == DecisionKind.FellowshipPhase);
            await session.Send(Side.FreePeoples, CommandKind.ContinueFellowship, new CommandPayload());
            await session.Until(() => !sh.Playback.Holding && sh.Board.View.Decision?.Kind == DecisionKind.AllocateHunt);
            var camera = sh.Board.Map.Camera.Pan;
            var freeDice = sh.Board.View.Dice.Where(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Unrolled).Select(d => d.Id).ToList();
            Assert.Greater(freeDice.Count, 0);
            await session.Send(Side.Shadow, CommandKind.AllocateHunt, new CommandPayload());
            var row = sh.Board.OpponentDice;
            var lit = await WatchUntilReleased(sh, () => freeDice.Any(id => PresentationGlow.IsLit(row.Q<Button>(id))), () =>
                LayoutTests.Save(sh.Viewport, "ui488-network-roll-shadow.png"));
            Assert.IsTrue(lit, "The Free Peoples' dice light on the Shadow's own allocation.");
            Assert.AreEqual(camera, sh.Board.Map.Camera.Pan, "The camera stays the player's.");
            Assert.IsTrue(sh.Board.View.Dice.Where(d => freeDice.Contains(d.Id)).All(d => d.Face != null || d.Location != DieLocation.Available));
        }

        // Whether the condition held at any moment before the client's presentation ended (it may not have begun yet).
        private static async Task<bool> WatchUntilReleased(MountedMatchClient client, Func<bool> condition, Action first = null)
        {
            var seen = false;
            var started = DateTime.UtcNow;
            var settled = false;
            while (!settled && DateTime.UtcNow < started.AddSeconds(15))
            {
                if (!seen && condition())
                {
                    seen = true;
                    first?.Invoke();
                }
                await Task.Delay(20);
                settled = !client.Playback.Holding && DateTime.UtcNow > started.AddSeconds(1);
            }
            return seen;
        }

        private static VisualElement Piece(MountedMatchClient client) => client.Board.Root.Q(className: SpentDieFlight.PieceClass);

        private static string MusterDie(PlayerView view) =>
            view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Muster && d.Location == DieLocation.Available).Id;
    }
}
