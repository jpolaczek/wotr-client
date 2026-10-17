using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class T36DesktopGameTests
    {
        [UnityTest, Timeout(620000)]
        public IEnumerator BaseGameAt125PercentWithReducedMotion() => Wait(
            T23FullGameNetworkTests.Play(largeInterface: true, artifact: "t36-base-full-game.json"));

        [UnityTest, Timeout(620000)]
        public IEnumerator AllExpansionsAt125PercentWithReducedMotion() => Wait(
            T23FullGameNetworkTests.Play(new MatchConfiguration
            {
                Lords = true, Kings = true, Fate = true, Council = true, NewCities = true
            }, true, "t36-expansions-full-game.json"));

        private static IEnumerator Wait(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 610;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Desktop full game exceeded its time bound.");
            task.GetAwaiter().GetResult();
        }
    }
}
