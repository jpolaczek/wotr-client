using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class StrongholdNetworkTests
    {
        [UnityTest]
        public IEnumerator ShelterRequiresFiveOnlyWhenTheAttackerActuallyEnters() => Wait(Shelter());

        [UnityTest]
        public IEnumerator SiegeExtensionSortieAndReliefKeepSeparateForcesAndCosts() => Wait(Modes());

        private static async Task Shelter()
        {
            foreach (var advance in new[] { false, true })
                using (var room = await Connect(advance ? "shelter-advance" : "shelter-stay"))
                {
                    await room.Attack(room.Sh, "osgiliath", "minas-tirith");
                    await room.Direct(room.Fp, "action-RetreatIntoSiege");
                    Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Garrison && u.Location == "minas-tirith"));
                    if (!advance)
                    {
                        await room.Direct(room.Sh, "battle-stay");
                        Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "minas-tirith" && !u.Garrison));
                        continue;
                    }
                    await room.SelectAllAdvancing(room.Sh);
                    Assert.AreEqual(DecisionKind.RemoveExcess, room.Fp.Board.View.Decision.Kind);
                    await room.RemoveExcess(room.Fp);
                    Assert.AreEqual(5, room.Fp.Board.View.Units.Count(u => u.Garrison && u.Location == "minas-tirith"));
                    Assert.AreEqual(2, room.Fp.Board.Presentation.Armies.Count(a => a.RegionId == "minas-tirith"));
                    Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent && d.Owner == Side.Shadow));
                }
        }

        private static async Task Modes()
        {
            using (var room = await Connect("siege"))
            {
                Assert.AreEqual(BattleMode.Siege, room.Sh.Board.View.Battle.Mode);
                await room.Settle();
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("confirm-decision"));
                var elite = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ExtendSiege).EligibleIds[0];
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                // UI-142: one figure completes the extension, so its single click extends the siege; a fast second
                // click does not send another command.
                var revision = room.Sh.Board.View.Revision;
                var figure = room.Sh.Board.Tray.Q<Button>("figure-" + elite);
                CardNetworkTests.Click(figure);
                ArmyGestures.Click(figure, 2);
                await room.At(revision + 1);
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Round);
                Assert.AreEqual("reserve", room.Sh.Board.View.Units.Single(u => u.Id == elite).Location);
                Assert.AreEqual(1, room.Sh.Board.View.Units.Count(u => u.Location == "minas-tirith" && !u.Garrison && u.Kind == UnitKind.Regular));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Resolving && d.Owner == Side.Shadow));
            }
            using (var room = await Connect("sortie"))
            {
                Assert.AreEqual(BattleMode.Sortie, room.Fp.Board.View.Battle.Mode);
                Assert.IsTrue(room.Fp.Board.View.Battle.Attacker.Garrison);
                await room.Direct(room.Fp, "action-StopBattle");
                Assert.IsNull(room.Fp.Board.View.Battle);
                Assert.AreEqual(5, room.Fp.Board.View.Units.Count(u => u.Garrison && u.Location == "minas-tirith"));
            }
            using (var room = await Connect("relief"))
            {
                Assert.AreEqual(BattleMode.Relief, room.Fp.Board.View.Battle.Mode);
                Assert.AreEqual(6, room.Fp.Board.View.Battle.Attacker.Units.Count);
                await room.SelectAllAdvancing(room.Fp);
                Assert.AreEqual(DecisionKind.RemoveExcess, room.Fp.Board.View.Decision.Kind);
                await room.RemoveExcess(room.Fp);
                Assert.AreEqual(10, room.Fp.Board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "minas-tirith"));
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Garrison));
                Assert.IsNull(room.Fp.Board.View.Battle);
            }
        }
    }
}
