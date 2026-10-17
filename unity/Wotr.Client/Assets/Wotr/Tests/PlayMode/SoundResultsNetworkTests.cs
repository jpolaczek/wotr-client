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
    // AUDIO-05 (AU08, AU09) and AUDIO-07 (AU15) on the real server: an accepted command sounds its acceptance once and
    // the gesture that sent it does not click; the decision passing to the other player sounds for them; the turn's
    // roll sounds once — for the player whose action it now is, the decision's tone stands for it.
    public sealed class SoundResultsNetworkTests
    {
        [UnityTest]
        public IEnumerator AcceptedCommandsAndTheTurnsRollSoundOnceForEachPlayer() => Wait(Results());

        private static List<SoundCue> Record(MountedMatchClient client)
        {
            var played = new List<SoundCue>();
            client.Interface.Sounds.Policy.Played += played.Add;
            return played;
        }

        private static async Task Results()
        {
            using var room = await Connect("turn-hunt-optional", ScenarioKind.BaseAcceptance);
            var fp = Record(room.Fp);
            var sh = Record(room.Sh);
            var revision = room.Fp.Board.View.Revision;
            FellowshipGestures.Action(room.Fp, "fellowship-continue");
            await room.At(revision + 1);
            await room.Settle();
            CollectionAssert.AreEqual(new[] { SoundCue.ActionAccepted }, fp,
                "The Free Peoples hear their accepted step once; the button that sent it does not click.");
            CollectionAssert.AreEqual(new[] { SoundCue.DecisionRequired }, sh, "The Hunt allocation passes to the Shadow.");
            fp.Clear();
            sh.Clear();
            // A player takes longer than the tone to decide; a roll during it would be dropped (one clip at a time).
            await Task.Delay(800, room.Token);
            var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.AllocateHunt);
            Assert.AreEqual(0, option.Minimum);
            await T22NetworkActions.Ready(room, room.Sh);
            await room.Confirm(room.Sh);
            await room.Settle();
            Assert.AreEqual(GamePhase.ActionResolution, room.Fp.Board.View.Phase);
            CollectionAssert.AreEqual(new[] { SoundCue.DecisionRequired }, fp,
                "The action passes to the Free Peoples with the roll: one tone, the more important one.");
            Assert.AreEqual(1, sh.Count(c => c == SoundCue.DiceRoll), "The Shadow hears the roll once: " + string.Join(", ", sh));
            Assert.IsFalse(sh.Contains(SoundCue.DecisionRequired), "The Shadow's own next step is not announced.");
            Assert.IsFalse(sh.Contains(SoundCue.ActionAccepted), "The roll stands for the Shadow's accepted allocation.");
            var before = fp.Count + sh.Count;
            room.Fp.Connection.Reconnect();
            await room.Settle();
            await room.Settle();
            Assert.AreEqual(before, fp.Count + sh.Count, "A reconnect plays nothing again.");
        }
    }
}
