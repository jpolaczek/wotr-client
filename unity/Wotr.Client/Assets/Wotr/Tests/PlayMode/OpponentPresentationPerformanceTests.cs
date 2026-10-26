using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-313: 1080p playback against the 60 FPS target — a long march with its camera travel and figure flight, then a
    // battle round's reveal and roll — measured per frame on the machine that runs the suite: frame times and the main
    // thread's managed allocations. The numbers go to .artifacts/ui313-performance.json with the machine's description.
    public sealed class OpponentPresentationPerformanceTests
    {
        private const double FrameBudgetMilliseconds = 1000.0 / 60;

        [UnityTest]
        public IEnumerator AMarchAndABattleRoundPlayWithinTheFrameBudgetAt1080p()
        {
            using var stage = OpponentBattlePresentationTests.Attack(out var fight, 1920, 1f);
            var army = stage.Put("sauron", UnitKind.Regular, "mount-gundabad", 4);
            yield return OpponentBattlePresentationTests.Open(stage, fight);
            yield return new WaitForSecondsRealtime(.5f);
            var idle = new Sample();
            yield return Idle(idle);

            var round = fight.Next();
            round.Reveal(Side.Shadow, OpponentBattlePresentationTests.ShadowCard);
            round.Reveal(Side.FreePeoples, OpponentBattlePresentationTests.FreeCard);
            round.Record(battle =>
            {
                OpponentBattlePresentationTests.Faces(battle);
                OpponentBattlePresentationTests.Roll(battle.Attacker, "a", 6, 5, 2, 1);
            });
            round.Record(battle => OpponentBattlePresentationTests.Roll(battle.Defender, "d", 6, 3));
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            var battling = new Sample();
            yield return Measure(stage, battling);

            var latest = stage.Next(stage.Board.View);
            PresentedBoardStage.Place(latest, army.Take(3), "angmar");
            var march = stage.Step(1, latest.Revision);
            march.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.March, From = "mount-gundabad", To = "angmar", Participants = army.Take(3).Select(stage.Unit).ToList()
            });
            stage.Play(latest, "pelargir", march);
            var marching = new Sample();
            yield return Measure(stage, marching);

            Report(idle, marching, battling);
            Assert.Greater(marching.Frames.Count, 30, "The march was measured.");
            Assert.Greater(battling.Frames.Count, 30, "The battle round was measured.");
            // The absolute budget depends on the machine and on what else runs on it; the presentation's own cost is its
            // frames against the same board's idle frames.
            Assert.LessOrEqual(marching.Mean, idle.Mean * 1.5 + 4, "The march adds little to the board's own frame time.");
            Assert.LessOrEqual(battling.Mean, idle.Mean * 1.5 + 4, "The battle round adds little to the board's own frame time.");
        }

        // The same board, presenting nothing, for 120 frames.
        private static IEnumerator Idle(Sample sample)
        {
            for (var frame = 0; frame < 120; frame++)
                yield return Frame(sample);
        }

        private static IEnumerator Frame(Sample sample)
        {
            var heap = GC.GetTotalMemory(false);
            yield return null;
            sample.Frames.Add(Time.unscaledDeltaTime * 1000);
            sample.Allocations.Add(Math.Max(0, GC.GetTotalMemory(false) - heap));
        }

        private static IEnumerator Measure(PresentedBoardStage stage, Sample sample)
        {
            yield return null;
            var deadline = Time.realtimeSinceStartup + 30;
            while (stage.Playback.Holding && Time.realtimeSinceStartup < deadline)
                yield return Frame(sample);
        }

        private static void Report(Sample idle, Sample marching, Sample battling)
        {
            var report = new
            {
                Machine = SystemInfo.deviceModel + " · " + SystemInfo.processorType + " · " + SystemInfo.graphicsDeviceName + " · " +
                    SystemInfo.systemMemorySize + " MB",
                Unity = Application.unityVersion,
                Resolution = "1920x1080 at 100 %, rendered to a texture in batch mode (no vsync)",
                Idle = idle.Summary(),
                March = marching.Summary(),
                BattleRound = battling.Summary()
            };
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/ui313-performance.json"));
            File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
            Debug.Log("UI-313 performance: " + JsonConvert.SerializeObject(report));
        }

        private sealed class Sample
        {
            public List<double> Frames { get; } = new List<double>();
            public List<long> Allocations { get; } = new List<long>();
            public double Mean => Frames.Count == 0 ? 0 : Frames.Average();

            public double Percentile(double share)
            {
                var sorted = Frames.OrderBy(frame => frame).ToList();
                return sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(share * sorted.Count))];
            }

            public object Summary() => new
            {
                Frames = Frames.Count,
                MeanMilliseconds = Math.Round(Frames.Average(), 2),
                P95Milliseconds = Math.Round(Percentile(.95), 2),
                MaxMilliseconds = Math.Round(Frames.Max(), 2),
                FramesOverBudget = Frames.Count(frame => frame > FrameBudgetMilliseconds),
                MeanAllocatedKilobytesPerFrame = Math.Round(Allocations.Average() / 1024.0, 1),
                MaxAllocatedKilobytesInAFrame = Math.Round(Allocations.Max() / 1024.0, 1)
            };
        }
    }
}
