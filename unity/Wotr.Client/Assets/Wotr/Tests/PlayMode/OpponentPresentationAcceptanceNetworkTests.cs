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
    // UI-313 with two real clients: the cases the component suites leave to the aggregate — the Shadow's captured Elven
    // Ring charged once at its socket with its die's new face, and the Shadow's real response (its Combat card) during
    // the Free Peoples' own attack, presented before their next decision.
    public sealed class OpponentPresentationAcceptanceNetworkTests
    {
        [UnityTest]
        public IEnumerator TheShadowsRingIsChargedAtItsSocketAndItsDieShowsItsNewFace() => Wait(Ring());

        [UnityTest]
        public IEnumerator TheShadowsCombatCardDuringOurAttackPlaysBeforeOurNextDecision() => Wait(Response());

        private static async Task Ring()
        {
            var session = await TestScenarioRoom.Start("sh-elven-ring");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var ring = sh.Board.View.Rings.First(r => r.Owner == Side.Shadow).Id;
                var die = sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Face == DieFace.Army).Id;
                await session.Send(Side.Shadow, CommandKind.UseRing, new CommandPayload { RingId = ring, DieId = die, Face = DieFace.Character });
                await Seen(session, () => fp.Playback.Holding && fp.Board.Root.Q(ResourcePresenter.RingName) is { } mark &&
                    mark.worldBound.Overlaps(fp.Board.Console.Rings.For(ring).worldBound), "The Ring is charged at its socket.");
                await Seen(session, () => fp.Board.Root.Q(ResourcePresenter.PlateName) != null, "Its die shows the old and the new face.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(DieFace.Character, fp.Board.View.Dice.Single(d => d.Id == die).Face);
                Assert.IsTrue(fp.Board.Console.Rings.For(ring).ClassListContains("ring-removed"), "The Ring left the game.");
                Assert.IsNull(fp.Board.Root.Q(ResourcePresenter.RingName), "The marks leave with the presentation.");
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Response()
        {
            var session = await TestScenarioRoom.Start("fp-army-attack");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var attack = fp.Board.View.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies.First();
                var target = attack.Targets.First().RegionId;
                await session.Send(Side.FreePeoples, CommandKind.AttackArmy, new CommandPayload
                {
                    DieId = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id,
                    MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = attack.RegionId, To = target, UnitIds = attack.UnitIds, CharacterIds = attack.CharacterIds } }
                });
                await session.Until(() => !fp.Playback.Holding && fp.Board.View.Battle != null);
                if (fp.Board.View.Decision?.Kind == DecisionKind.CombatCard)
                    await session.Send(Side.FreePeoples, CommandKind.CommitCombatCard, new CommandPayload { CardId = "" });
                await session.Until(() => sh.Board.View.Decision?.Kind == DecisionKind.CombatCard && sh.Board.View.DecisionOwner == Side.Shadow);
                var card = sh.Board.View.Options.First(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds.FirstOrDefault() ?? "";
                var before = fp.Board.View.Revision;
                await session.Send(Side.Shadow, CommandKind.CommitCombatCard, new CommandPayload { CardId = card });
                await Seen(session, () => fp.Playback.Holding && fp.Board.Battles.Presenting,
                    "The Shadow's response plays in the battle window during our attack.");
                Assert.AreEqual(before, fp.Board.View.Revision, "Our next decision waits for it.");
                Assert.IsFalse(fp.Board.Tray.enabledInHierarchy, "Nothing can be sent while it plays.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.IsFalse(fp.Board.Battles.Presenting);
                Assert.AreEqual(fp.Playback.Latest.Revision, fp.Board.View.Revision);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Seen(TestScenarioRoom session, Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
            Assert.IsTrue(condition(), what);
        }
    }
}
