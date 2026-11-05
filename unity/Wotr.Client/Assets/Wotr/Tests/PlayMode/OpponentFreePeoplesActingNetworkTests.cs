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
    // TEST-20 (owner, 2026-11-04; audit gap 12): the network suites watched almost only as the Free Peoples. Here the Shadow
    // watches the Free Peoples act on the real server: a march, a political step, an Event, a Ring, a card's dice, a capture,
    // a quiet battle decision and a withdrawal into a stronghold.
    public sealed class OpponentFreePeoplesActingNetworkTests
    {
        [UnityTest] public IEnumerator TheShadowWatchesAMarchFromItsSourceToItsDestination() => Wait(March());
        [UnityTest] public IEnumerator TheShadowWatchesAPoliticalStepInItsPanel() => Wait(Politics());
        [UnityTest] public IEnumerator TheShadowReadsAnEventAsTheHandCountStepsWithIt() => Wait(Event());
        [UnityTest] public IEnumerator TheShadowSeesTheRingAtItsSocketAndItsDiesNewFace() => Wait(Ring());
        [UnityTest] public IEnumerator TheShadowSeesTheDiceOfDeadMenAndTheCaptureOfPelargir() => Wait(DeadMen());

        // UI-491 (owner, 2026-11-04): a side's No card reads as a caption for the other, on the real commit.
        [UnityTest] public IEnumerator TheShadowReadsTheFreePeoplesNoCard() => Wait(NoCard("fp-army-attack", Side.FreePeoples, "osgiliath"));
        [UnityTest] public IEnumerator TheFreePeoplesReadTheShadowsNoCard() => Wait(NoCard("sh-army-attack", Side.Shadow, "north-ithilien"));

        // UI-489 (owner, 2026-11-04): a retreat into a siege on the real server, both from the decision and from Scouts.
        [UnityTest] public IEnumerator AWithdrawalFliesOntoTheWallsForBothPlayers() => Wait(Withdrawal());
        [UnityTest] public IEnumerator ScoutsWithdrawalIsTheSameScene() => Wait(Scouts());

        private static async Task March()
        {
            var session = await TestScenarioRoom.Start("fp-army-move");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var army = fp.Board.View.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies
                    .First(a => !a.Garrison && a.UnitIds.Count > 0 && a.Targets.Any(t => t.BattleMode == null));
                var target = army.Targets.First(t => t.BattleMode == null);
                var start = sh.Board.View.Revision;
                var before = sh.Board.Presentation.FigureCount(target.RegionId, Side.FreePeoples);
                await session.Send(Side.FreePeoples, CommandKind.MoveArmies, new CommandPayload
                {
                    Stepwise = true, DieId = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id,
                    MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = army.RegionId, To = target.RegionId, Route = target.Route, UnitIds = army.UnitIds, CharacterIds = army.CharacterIds } }
                });
                await Seen(() => sh.Playback.Holding && sh.Board.Root.Q(CostEmphasis.MarkName) != null, "Its die is marked.");
                await Seen(() => sh.Board.Map.Figures.ActiveCount > 0 && sh.Board.Map.Figures.Moves.Any(m => m.To.Region == target.RegionId),
                    "The march flies for the Shadow.");
                Assert.AreEqual(start, sh.Board.View.Revision, "The board's view is still the presented one.");
                var moved = sh.Board.Map.Figures.Moves.Select(m => m.To.Key).Where(key => key.StartsWith("unit:")).Select(key => key.Substring(5));
                CollectionAssert.AreEquivalent(army.UnitIds, moved, "Exactly the recorded movers.");
                Assert.AreEqual(before + army.UnitIds.Count + army.CharacterIds.Count, sh.Board.Presentation.FigureCount(target.RegionId, Side.FreePeoples),
                    "The destination settles with its true count.");
                LayoutTests.Save(sh.Viewport, "test20-network-march.png");
                await session.Until(() => !sh.Playback.Holding);
                Assert.AreEqual(0, sh.Board.Map.Figures.ActiveCount, "The release replays nothing.");
                Assert.IsTrue(sh.Board.View.Units.Where(u => army.UnitIds.Contains(u.Id)).All(u => u.Location == target.RegionId));
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Politics()
        {
            var session = await TestScenarioRoom.Start("fp-muster-politics");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var option = fp.Board.View.Options.First(o => o.Kind == CommandKind.AdvancePolitics);
                var nation = option.Targets.Concat(option.EligibleIds).First(id => fp.Board.View.Nations.Any(n => n.Id == id));
                var steps = sh.Board.View.Nations.Single(n => n.Id == nation).StepsToWar;
                await session.Send(Side.FreePeoples, CommandKind.AdvancePolitics, new CommandPayload { DieId = MusterDie(fp.Board.View), NationId = nation });
                await Seen(() => sh.Playback.Holding && sh.Board.Nations.Drawer.style.display.value == DisplayStyle.Flex, "The panel opens for the Shadow.");
                await Seen(() => PresentationGlow.IsLit(sh.Board.Nations.Drawer.Q<Button>("nation-status-" + nation)), "The nation's token glows.");
                Assert.AreEqual(steps, sh.Board.View.Nations.Single(n => n.Id == nation).StepsToWar, "The board's view is still the presented one.");
                await session.Until(() => !sh.Playback.Holding);
                Assert.AreEqual(steps - 1, sh.Board.View.Nations.Single(n => n.Id == nation).StepsToWar);
                Assert.IsTrue(Appearance.Closed(sh.Board.Nations.Drawer), "The panel the presentation opened closes.");
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Event()
        {
            var session = await TestScenarioRoom.Start("fp-character-event");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var card = fp.Board.View.Options.First(o => o.Kind == CommandKind.PlayEvent).Events.First();
                var cost = card.Costs.First();
                var count = sh.Board.TopBar.Opponent.Root.Q("opponent-cards").Q<Label>(className: "summary-value");
                var hand = int.Parse(count.text);
                await session.Send(Side.FreePeoples, CommandKind.PlayEvent, new CommandPayload
                {
                    CardId = card.CardId, DieId = cost.DieId, AbilityId = cost.AbilityId, CardCost = cost.Face
                });
                await Seen(() => sh.Playback.Holding && sh.Board.Cards.CardId == card.CardId && sh.Board.Cards.Root.style.display.value == DisplayStyle.Flex,
                    "The Shadow reads the card.");
                Assert.IsFalse(sh.Board.Cards.Pinned);
                Assert.AreEqual(CardRevealPresenter.PlayedNote, sh.Board.Cards.Root.Q<Label>("card-window").text);
                await Seen(() => count.text == (hand - 1).ToString(), "The hand count steps with the card.");
                Assert.IsTrue(sh.Playback.Holding, "UI-493: it does not wait for the release.");
                LayoutTests.Save(sh.Viewport, "test20-network-event.png");
                await session.Until(() => !sh.Playback.Holding);
                Assert.AreEqual(hand - 1, sh.Board.View.Players.Single(p => p.Side == Side.FreePeoples).HandCount, "The staged count was the true one.");
                Assert.AreEqual(DisplayStyle.None, sh.Board.Cards.Root.style.display.value);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Ring()
        {
            var session = await TestScenarioRoom.Start("fp-elven-ring");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var ring = fp.Board.View.Rings.First(r => r.Owner == Side.FreePeoples).Id;
                var die = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Muster).Id;
                await session.Send(Side.FreePeoples, CommandKind.UseRing, new CommandPayload { RingId = ring, DieId = die, Face = DieFace.Character });
                await Seen(() => sh.Playback.Holding && PresentationGlow.IsLit(sh.Board.Console.Rings.For(ring)), "The Ring is charged at its socket.");
                await Seen(() => sh.Board.Root.Q(ResourcePresenter.PlateName) != null, "Its die shows the old and the new face.");
                var faces = sh.Board.Root.Q(ResourcePresenter.PlateName).Query(className: DieFaceIcon.ClassName).ToList();
                CollectionAssert.AreEqual(new[] { "die-face-muster", "die-face-character" }, faces.Select(face => face.GetClasses().Last()));
                LayoutTests.Save(sh.Viewport, "test20-network-ring.png");
                await session.Until(() => !sh.Playback.Holding);
                Assert.AreEqual(DieFace.Character, sh.Board.View.Dice.Single(d => d.Id == die).Face);
                Assert.AreEqual(Side.Shadow, sh.Board.View.Rings.Single(r => r.Id == ring).Owner, "The Ring passes to the Shadow.");
                Assert.IsEmpty(sh.Board.Root.Query(className: PresentationGlow.ClassName).ToList(), "The marks leave with the presentation.");
            }
            finally
            {
                await session.End();
            }
        }

        // The Free Peoples' Dead Men of Dunharrow: Gimli to Pelargir, whose Shadow garrison is hit by a die roll, and the
        // region taken back. UI-492: the Shadow sees the die; TEST-20: and the capture, its point lost in its own enamel.
        private static async Task DeadMen()
        {
            using var room = await Connect("dead-men", ScenarioKind.CardPack);
            await room.ChooseFigure(room.Fp, "base.character.gimli");
            MapContourProbe.ClickRegion(room.Fp.Board.Map, "pelargir");
            var dice = new Sight(room.Sh, () => room.Sh.Board.Root.Q(CardRollPresenter.PlateName) is { } plate && plate.Query<DiePips>().ToList().Count > 0,
                "ui492-network-dead-men");
            await room.Settle();
            await room.Confirm(room.Fp);
            await dice.Finish();
            Assert.IsTrue(dice.Seen, "The Shadow sees the card's die at the region.");
            var losses = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.TakeEventLosses);
            foreach (var id in losses.EligibleIds.Take(room.Sh.Board.View.EventLoss.Hits))
                Click(room.Sh, "loss-" + id);
            await room.Confirm(room.Sh);
            var destination = room.Sh.Board.View.Options.Single().EligibleIds[0];
            MapContourProbe.ClickRegion(room.Sh.Board.Map, destination);
            await room.Confirm(room.Sh);
            foreach (var id in room.Fp.Board.View.Options.Single().EligibleIds)
                Click(room.Fp, "effect-choice-" + id);
            var capture = new Sight(room.Sh, () => room.Sh.Board.Map.Q<Label>("observed-badge")?.text.StartsWith("−") == true &&
                room.Sh.Board.Map.Q<Label>("observed-badge").ClassListContains("observed-badge-shadow"), "test20-network-capture");
            await room.Confirm(room.Fp);
            await capture.Finish();
            Assert.IsTrue(capture.Seen, "The Shadow sees Pelargir's point go.");
            Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Control["pelargir"]);
        }

        private static async Task NoCard(string scenario, Side actor, string target)
        {
            var session = await TestScenarioRoom.Start(scenario);
            try
            {
                var acting = session.Client(actor);
                var watching = session.Client(actor == Side.Shadow ? Side.FreePeoples : Side.Shadow);
                var view = acting.Board.View;
                var attack = view.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies.First(a => a.Targets.Any(t => t.RegionId == target));
                await session.Send(actor, CommandKind.AttackArmy, new CommandPayload
                {
                    DieId = view.Dice.First(d => d.Owner == actor && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id,
                    MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = attack.RegionId, To = target, UnitIds = attack.UnitIds, CharacterIds = attack.CharacterIds } }
                });
                await session.Until(() => !watching.Playback.Holding && watching.Board.Battles.IsOpen &&
                    acting.Board.View.Decision?.Kind == DecisionKind.CombatCard && acting.Board.View.DecisionOwner == actor);
                await session.Send(actor, CommandKind.CommitCombatCard, new CommandPayload { CardId = "" });
                await Seen(() => watching.Playback.Holding && watching.Board.Root.Q<Label>(BattlePresenter.PlateName)?.text == "No card",
                    "The side watching reads the decision.");
                await Task.Delay(300);
                LayoutTests.Save(watching.Viewport, "ui491-network-no-card-" + actor + ".png");
                await session.Until(() => !watching.Playback.Holding);
                Assert.IsNull(watching.Board.Root.Q(BattlePresenter.PlateName), "The caption ends with its beat.");
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Withdrawal()
        {
            const string region = "minas-tirith";
            using var room = await Connect("shelter-stay");
            await room.Attack(room.Sh, "osgiliath", region);
            await room.Until(() => !room.Fp.Playback.Holding && room.Fp.Board.View.Battle != null);
            await room.Settle();
            var pan = room.Fp.Board.Map.Camera.Pan;
            var opponent = new Sight(room.Sh, () => Withdrawing(room.Sh, region), "ui489-network-opponent");
            var own = new Sight(room.Fp, () => Withdrawing(room.Fp, region), "");
            await room.Direct(room.Fp, "action-RetreatIntoSiege");
            await opponent.Finish();
            await own.Finish();
            Assert.IsTrue(opponent.Seen, "The opponent watches the figures withdraw into the stronghold.");
            Assert.IsTrue(own.Seen, "The player's own click plays the shared step, as an ordinary retreat does.");
            Assert.AreEqual(pan, room.Fp.Board.Map.Camera.Pan, "The player's own click keeps their camera.");
            Assert.AreEqual(6, room.Sh.Board.View.Units.Count(u => u.Garrison && u.Location == region));
            await room.Until(() => Lines(room.Sh).Any(line => line.Contains("withdraws into the stronghold")));
            Assert.IsFalse(Lines(room.Sh).Any(line => line.Contains("retreats")), string.Join(" | ", Lines(room.Sh)));
            Assert.AreEqual(0, room.Sh.Board.Map.Figures.ActiveCount, "The release replays nothing.");
        }

        private static async Task Scouts()
        {
            using var room = await Connect("strategy-scouts", ScenarioKind.CardPack);
            var region = room.Fp.Board.View.Battle.RegionId;
            var revision = room.Fp.Board.View.Revision;
            var opponent = new Sight(room.Sh, () => Withdrawing(room.Sh, region), "ui489-network-scouts");
            await room.Settle();
            MapContourProbe.ClickRegion(room.Fp.Board.Map, region);
            await room.At(revision + 1);
            await opponent.Finish();
            Assert.IsTrue(opponent.Seen, "Scouts' Withdraw into siege plays the same scene.");
            // Its pennant is the Free Peoples' effect choice; the action's card reads the Movement (ActionResultTextTests).
            Assert.IsFalse(Lines(room.Sh).Any(line => line.Contains("retreats")), string.Join(" | ", Lines(room.Sh)));
        }

        private static bool Withdrawing(MountedMatchClient client, string region) => client.Playback.Holding &&
            client.Board.Map.Figures.ActiveCount > 0 && client.Board.Map.Figures.Moves.Any(move =>
                move.From.Region == region && move.To.Region == region && !move.From.Garrison && move.To.Garrison);

        private static System.Collections.Generic.IEnumerable<string> Lines(MountedMatchClient client) =>
            client.Activity.Model.Groups.SelectMany(group => group.Result.Lines);

        private static string MusterDie(PlayerView view) =>
            view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Muster && d.Location == DieLocation.Available).Id;

        private static async Task Seen(Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
            Assert.IsTrue(condition(), what);
        }

        // Something that shows for a moment while another client's command is on its way.
        private sealed class Sight
        {
            private readonly MountedMatchClient client;
            private readonly Func<bool> condition;
            private readonly string frame;
            private readonly Task loop;
            private bool stopping;
            public bool Seen { get; private set; }

            public Sight(MountedMatchClient client, Func<bool> condition, string frame)
            {
                this.client = client;
                this.condition = condition;
                this.frame = frame;
                loop = Watch();
            }

            // Ends once the client's presentation has played out.
            public async Task Finish()
            {
                await Task.Delay(100);
                stopping = true;
                await loop;
            }

            private async Task Watch()
            {
                while (!stopping || client.Playback.Holding)
                {
                    if (!Seen && condition())
                    {
                        Seen = true;
                        await Task.Delay(200);
                        if (frame.Length > 0)
                            LayoutTests.Save(client.Viewport, frame + ".png");
                    }
                    await Task.Delay(15);
                }
            }
        }
    }
}
