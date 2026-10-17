using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // AUDIO-06 (AU10) on the real server: a new battle sounds once for both players; opening the window again and
    // the battle's later steps do not repeat it.
    public sealed class BattleSoundNetworkTests
    {
        [UnityTest]
        public IEnumerator ANewBattleSoundsOnceForBothPlayers() => Wait(Battle());

        private static async Task Battle()
        {
            using var room = await Connect("ui43-stale", ScenarioKind.BaseAcceptance, openPanels: false);
            var fp = new List<SoundCue>();
            var sh = new List<SoundCue>();
            room.Fp.Interface.Sounds.Policy.Played += fp.Add;
            room.Sh.Interface.Sounds.Policy.Played += sh.Add;
            await room.Attack(room.Fp, "minas-tirith", "lossarnach");
            await room.Until(() => room.Fp.Board.Battles.IsOpen && room.Sh.Board.Battles.IsOpen);
            await room.Settle();
            Assert.AreEqual(1, fp.Count(c => c == SoundCue.BattleStarted), "Free Peoples: " + string.Join(", ", fp));
            Assert.AreEqual(1, sh.Count(c => c == SoundCue.BattleStarted), "Shadow: " + string.Join(", ", sh));
            room.Sh.Board.Battles.Close();
            room.Sh.Board.Battles.Open();
            await room.Settle();
            Assert.AreEqual(1, sh.Count(c => c == SoundCue.BattleStarted), "Reopening the window does not repeat it.");
        }
    }
}
