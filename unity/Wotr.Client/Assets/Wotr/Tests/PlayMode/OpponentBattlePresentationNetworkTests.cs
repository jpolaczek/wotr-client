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
    // UI-311 with two real clients on `sh-army-attack`: the Shadow attacks North Ithilien and the Free Peoples' board opens
    // the battle window inside the presentation; the Shadow's committed card stays a back; once the Free Peoples commit,
    // the shared reveal and rolls play in the window before their next decision, and the controls come back at the release.
    public sealed class OpponentBattlePresentationNetworkTests
    {
        [UnityTest]
        public IEnumerator TheFreePeoplesWatchTheShadowsAttackRoundInTheBattleWindow() => Wait(Round());

        private static async Task Round()
        {
            var session = await TestScenarioRoom.Start("sh-army-attack");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var attack = sh.Board.View.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies
                    .First(a => a.Targets.Any(t => t.RegionId == "north-ithilien"));
                await session.Send(Side.Shadow, CommandKind.AttackArmy, new CommandPayload
                {
                    DieId = sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id,
                    MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = attack.RegionId, To = "north-ithilien", UnitIds = attack.UnitIds, CharacterIds = attack.CharacterIds } }
                });
                await Seen(session, () => fp.Playback.Holding && fp.Board.Battles.IsOpen && fp.Board.Battles.Presenting,
                    "The window opens inside the presentation, after the attack's trace.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.IsFalse(fp.Board.Battles.Presenting, "The release gives the window back to the decision controls.");

                await session.Until(() => sh.Board.View.Decision?.Kind == DecisionKind.CombatCard && sh.Board.View.DecisionOwner == Side.Shadow);
                var card = sh.Board.View.Options.First(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds.FirstOrDefault() ?? "";
                await session.Send(Side.Shadow, CommandKind.CommitCombatCard, new CommandPayload { CardId = card });
                if (card.Length > 0)
                    await Seen(session, () => fp.Playback.Holding &&
                        fp.Board.Battles.Presented.Q<Button>("battle-card-attacker")?.ClassListContains("battle-card-back") == true,
                        "The Shadow's committed card is a back.");
                await session.Until(() => !fp.Playback.Holding && fp.Board.View.Decision?.Kind == DecisionKind.CombatCard);

                var noCard = fp.Board.Tray.Q<Button>("battle-no-card");
                await session.Until(() => noCard.enabledInHierarchy && noCard.worldBound.width > 0);
                await session.Settle();
                PointerGestureEvents.ClickAt(noCard);
                await Seen(session, () => fp.Playback.Holding && fp.Board.Battles.Presented.Q(className: BattlePresenter.RollClass) != null,
                    "The shared rolls ring their dice in the window.");
                Assert.IsFalse(fp.Board.Tray.enabledInHierarchy, "Nothing can be sent while they play.");
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
