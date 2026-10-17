using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // REPLAY-05: a valid v1 replay of the given length built from the server's sample: every step's views carry the
    // revision 100 + step and their turn, and each side's history has one step per frame.
    internal static class ReplayFixture
    {
        public static readonly string Sample = Path.Combine(Application.dataPath, "Wotr/Tests/Editor/ReplaySample.wotrreplay");

        // `change` may alter both views of a step (REPLAY-06: a move, the tracks).
        public static string Build(string folder, int steps, Func<int, int> turnOf, Action<int, PlayerView> change = null)
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "synthetic-" + Guid.NewGuid().ToString("N") + ".wotrreplay");
            ReplayFrame template;
            var catalog = new Dictionary<string, byte[]>();
            ReplayManifest original;
            using (var sample = ReplayArchive.Open(Sample))
            {
                template = sample.Frames(0)[0];
                original = sample.Manifest;
                foreach (var file in new[] { "base-cards.json", "base-card-text.json", "base-characters.json" })
                    catalog[file] = Encoding.UTF8.GetBytes(sample.Catalog(file));
            }
            var entries = new Dictionary<string, byte[]>();
            var frames = Enumerable.Range(0, steps).Select(step => Frame(template, step, turnOf(step))).ToList();
            if (change != null)
                foreach (var frame in frames)
                {
                    change(frame.Step, frame.FreePeoples);
                    change(frame.Step, frame.Shadow);
                }
            for (var chunk = 0; chunk * ReplayFormat.FramesPerChunk < steps; chunk++)
                entries[ReplayFormat.FramesEntry(chunk)] = Json(frames.Skip(chunk * ReplayFormat.FramesPerChunk).Take(ReplayFormat.FramesPerChunk).ToList());
            var index = frames.Select(f => new ReplayStep { Step = f.Step, Revision = f.Revision, Turn = turnOf(f.Step), Phase = GamePhase.ActionResolution }).ToList();
            entries[ReplayFormat.StepsEntry] = Json(index);
            entries[ReplayFormat.TurnsEntry] = Json(index.GroupBy(s => s.Turn).Select(g => new ReplayTurn { Turn = g.Key, Step = g.Min(s => s.Step) }).ToList());
            entries[ReplayFormat.BattlesEntry] = Json(new List<ReplayBattleReport>());
            entries[ReplayFormat.HistoryEntry(Side.FreePeoples, 0)] = Json(History(steps, Side.FreePeoples));
            entries[ReplayFormat.HistoryEntry(Side.Shadow, 0)] = Json(History(steps, Side.Shadow));
            foreach (var file in catalog)
                entries[ReplayFormat.CatalogEntry(file.Key)] = file.Value;
            var manifest = new ReplayManifest
            {
                Format = ReplayFormat.Name, FormatVersion = ReplayFormat.Version, ReplayId = "synthetic" + steps, MatchId = "synthetic",
                Source = "test", Name = "Synthetic game", Players = original.Players, Configuration = original.Configuration,
                Winner = Side.Shadow, Victory = VictoryCondition.ShadowMilitary, Turns = index.Select(s => s.Turn).Distinct().Count(),
                Steps = steps, FrameChunks = (steps + ReplayFormat.FramesPerChunk - 1) / ReplayFormat.FramesPerChunk,
                FreePeoplesHistoryChunks = 1, ShadowHistoryChunks = 1, Complete = true,
                StartedAt = DateTimeOffset.UtcNow.AddHours(-1), FinishedAt = DateTimeOffset.UtcNow, ExportedAt = DateTimeOffset.UtcNow,
                Files = entries.Select(e => new ReplayFile { Path = e.Key, Size = e.Value.Length, Sha256 = Sha(e.Value) }).ToList()
            };
            using var zip = new ZipArchive(new FileStream(path, FileMode.CreateNew), ZipArchiveMode.Create);
            foreach (var entry in entries)
                Write(zip, entry.Key, entry.Value);
            Write(zip, ReplayFormat.ManifestEntry, Json(manifest));
            return path;
        }

        // Each frame copies the sample's first frame; its revision names the step, so a test sees which step is shown.
        private static ReplayFrame Frame(ReplayFrame template, int step, int turn)
        {
            var frame = JsonConvert.DeserializeObject<ReplayFrame>(JsonConvert.SerializeObject(template, ReplayArchive.Json), ReplayArchive.Json);
            frame.Step = step;
            frame.Revision = 100 + step;
            foreach (var view in new[] { frame.FreePeoples, frame.Shadow })
            {
                view.Revision = frame.Revision;
                view.Turn = turn;
                view.Winner = null;
            }
            return frame;
        }

        private static ActionLogPage History(int steps, Side side) => new ActionLogPage
        {
            MatchId = "synthetic",
            Steps = Enumerable.Range(0, steps).Select(step => new ActionLogStep
            {
                Id = side + "-s" + step, ActionId = side + "-a" + step, Revision = 100 + step, Order = 1, Turn = 1,
                Actor = step % 2 == 0 ? Side.FreePeoples : Side.Shadow, Kind = "Test", Text = "Step " + step
            }).ToList(),
            Actions = Enumerable.Range(0, steps).Select(step => new ActionLogSummary
            {
                Id = side + "-a" + step, Version = "1", Initiator = step % 2 == 0 ? Side.FreePeoples : Side.Shadow, Turn = 1, StepCount = 1
            }).ToList()
        };

        private static byte[] Json<T>(T value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, ReplayArchive.Json));

        private static string Sha(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }

        private static void Write(ZipArchive zip, string name, byte[] bytes)
        {
            using var output = zip.CreateEntry(name).Open();
            output.Write(bytes, 0, bytes.Length);
        }
    }
}
