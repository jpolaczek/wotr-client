using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-499 (docs/ui/RANKED_UX.md): a clock running out against the real server. scripts/test-client.sh runs its clocks at a
    // tenth, so the Fast preset's move is 9 s and its warning comes 1 s before the limit.
    public class RankedClockNetworkTests
    {
        [UnityTest]
        public IEnumerator TheClockRunsOutWarnsItsOwnerOnceAndEndsTheGame()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var task = Run(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>());
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(string address)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = timeout.Token;
            var firstApi = new GameApi(address);
            var secondApi = new GameApi(address);
            await TestAccounts.Register(firstApi, "rc_a_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            await TestAccounts.Register(secondApi, "rc_b_" + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            var storage = Path.Combine(Path.GetTempPath(), "wotr-clock-" + Guid.NewGuid().ToString("N"));
            using var first = new MountedFlowClient(firstApi, storage);
            using var second = new MountedFlowClient(secondApi, storage);
            var heard = new Dictionary<MountedFlowClient, List<SoundCue>> { [first] = new List<SoundCue>(), [second] = new List<SoundCue>() };
            foreach (var client in new[] { first, second })
            {
                var cues = heard[client];
                client.Interface.Sounds.Policy.Played += cues.Add;
                // Both windows in the background: the warning must reach its owner there.
                client.Interface.Sounds.Policy.Focused = false;
                await Search(client, token);
            }
            await ReturnTestFixture.Until(() => first.Board?.View?.Clock != null && second.Board?.View?.Clock != null, token);

            var clock = first.Board.View.Clock;
            var owner = clock.Owner == first.Board.View.You ? first : second;
            var other = owner == first ? second : first;
            var ownerClock = owner.Root.Q<Label>("move-clock");
            var otherClock = other.Root.Q<Label>("move-clock");
            StringAssert.StartsWith("You ", ownerClock.text);
            StringAssert.StartsWith("Opponent ", otherClock.text);
            Assert.AreEqual(1000, clock.WarningMilliseconds, "A tenth of 10 s.");
            Assert.IsFalse(ownerClock.ClassListContains(TopBarClock.WarningClass));

            var lastText = "";
            var redSeen = false;
            while (owner.Board.View.Winner == null || other.Board.View.Winner == null)
            {
                if (ownerClock.panel != null && owner.Board.View.Winner == null)
                {
                    lastText = ownerClock.text;
                    if (ownerClock.ClassListContains(TopBarClock.WarningClass))
                    {
                        redSeen = true;
                        StringAssert.EndsWith(" 0:01", lastText == "You 0:00" ? " 0:01" : lastText, "Red only inside the warning.");
                    }
                }
                await Task.Delay(20, token);
            }

            Assert.IsTrue(redSeen, "The owner saw the clock turn red.");
            StringAssert.StartsWith("You 0:0", lastText);
            Assert.That(lastText, Is.EqualTo("You 0:00").Or.EqualTo("You 0:01"), "The game never ends while the clock shows more than a second.");
            Assert.AreEqual(1, heard[owner].FindAll(cue => cue == SoundCue.ClockWarning).Count, "Its owner hears it once, in the background.");
            Assert.AreEqual(0, heard[other].FindAll(cue => cue == SoundCue.ClockWarning).Count, "The other player does not.");
            Assert.AreEqual(VictoryCondition.TimedOut, owner.Board.View.Victory);
            Assert.AreNotEqual(owner.Board.View.You, owner.Board.View.Winner, "Whoever ran out of time lost.");
            Assert.IsNull(owner.Root.Q("move-clock"), "A finished game has no clock: it cannot read zero after the end.");
            Assert.IsNull(other.Root.Q("move-clock"));
        }

        private static async Task Search(MountedFlowClient client, CancellationToken token)
        {
            await ReturnTestFixture.ClickReady(client.Root, "find-game", token);
            await ReturnTestFixture.Until(() => client.Root.Q("find-game-form").resolvedStyle.display == DisplayStyle.Flex, token);
            await ReturnTestFixture.ClickReady(client.Root, "find-fast-enforce", token);
            await ReturnTestFixture.ClickReady(client.Root, "submit-find", token);
        }
    }
}
