using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed partial class MapContourRecoveryTests
    {
        [UnityTest]
        public IEnumerator BothRolesRenderServerOverridesAndRetainThemAcrossReconnect() =>
            MatchTestRoom.Wait(Configurations());

        [UnityTest]
        public IEnumerator RemovingWhiteHandChangesTwoBandsButRetainsCaptureAndItsPoints() =>
            MatchTestRoom.Wait(RemoveWhiteHand());

        private static async Task Configurations()
        {
            foreach (var name in new[] { "base", "fate", "newcities", "captured" })
            {
                using var room = await UI10TestRoom.Connect("map16-" + name);
                Assert.AreEqual(PublicMap(room.Fp.Board), PublicMap(room.Sh.Board), name);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    AssertRegions(board, name);
                    var snapshot = PublicMap(board);
                    var builds = board.Map.Renderer.BorderBuilds;
                    var revision = board.View.Revision;
                    var connected = client.Connected;
                    client.Connection.Reconnect();
                    await room.Until(() => client.Connected > connected);
                    await room.Settle();
                    Assert.AreEqual(snapshot, PublicMap(board));
                    Assert.AreEqual(builds, board.Map.Renderer.BorderBuilds);
                    Assert.AreEqual(revision, board.View.Revision);
                    board.Map.Fit();
                    await room.Settle();
                    LayoutTests.Save(client.Viewport, $"map16-{name}-{board.View.You}-fit.png");
                    await CaptureRegion(client, name, "erebor", room);
                    if (name == "newcities")
                    {
                        await CaptureRegion(client, name, "ered-luin", room);
                        await CaptureRegion(client, name, "south-rhun", room);
                    }
                    if (name == "captured")
                        await CaptureRegion(client, name, "helms-deep", room);
                }
                Assert.AreEqual(0, room.Boundary.Commands, "Loading the map must not play an action.");
            }
        }

        private static async Task RemoveWhiteHand()
        {
            using var room = await UI10TestRoom.Connect("map16-whitehand");
            var fp = room.Fp.Board;
            var revision = fp.View.Revision;
            var builds = new[] { fp.Map.Renderer.BorderBuilds, room.Sh.Board.Map.Renderer.BorderBuilds };
            var topology = Topology(fp);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                AssertRegions(client.Board, "whitehand");
                Assert.AreEqual(2, client.Board.View.MilitaryVictory.Single(v => v.Side == Side.Shadow).Points);
                await CaptureRegion(client, "whitehand-before", "helms-deep", room);
            }
            var option = fp.View.Options.Single(o => o.Kind == CommandKind.RemoveCorruptedSovereign &&
                o.EligibleIds.Contains("kings.sovereign.theoden"));
            var receipt = await room.Fp.Api.Post<CommandReceipt>($"api/matches/{fp.View.MatchId}/commands", new GameCommand
            {
                MatchId = fp.View.MatchId, ExpectedRevision = revision, DecisionId = fp.View.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = option.Kind,
                Payload = new CommandPayload { DieId = option.EligibleDice[0], CharacterId = "kings.sovereign.theoden" }
            }, room.Token);
            Assert.IsTrue(receipt.Accepted, receipt.Error);
            await room.At(revision + 1);
            var index = 0;
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                AssertRegions(client.Board, "captured");
                Assert.AreEqual(builds[index++] + 2, client.Board.Map.Renderer.BorderBuilds);
                Assert.AreEqual(topology, Topology(client.Board));
                Assert.AreEqual(2, client.Board.View.MilitaryVictory.Single(v => v.Side == Side.Shadow).Points);
                await CaptureRegion(client, "whitehand-after", "helms-deep", room);
                var connected = client.Connected;
                client.Connection.Reconnect();
                await room.Until(() => client.Connected > connected);
                await room.Settle();
                AssertRegions(client.Board, "captured");
            }
            Assert.AreEqual(PublicMap(fp), PublicMap(room.Sh.Board));
        }

        private static void AssertRegions(BoardScreen board, string configuration)
        {
            var regions = board.Presentation.Regions;
            var fate = configuration == "fate" || configuration == "newcities";
            Assert.AreEqual(fate ? "sauron" : "dwarves", regions["erebor"].Nation);
            Assert.AreEqual(fate ? null : "north", regions["dale"].Nation);
            Assert.AreEqual(fate ? "Fortification" : "City", regions["dale"].Settlement);
            Assert.AreEqual(fate ? "City" : "Town", regions["iron-hills"].Settlement);
            Assert.AreEqual(configuration == "newcities" ? "City" : "Town", regions["ered-luin"].Settlement);
            Assert.AreEqual(configuration == "newcities" ? "City" : "Town", regions["south-rhun"].Settlement);
            Assert.AreEqual("dwarves", regions["iron-hills"].Nation);
            Assert.AreEqual("dwarves", regions["ered-luin"].Nation);
            Assert.AreEqual("southrons", regions["south-rhun"].Nation);
            Assert.AreEqual(configuration == "whitehand" ? "isengard" : "rohan", regions["helms-deep"].Nation);
            if (configuration == "captured" || configuration == "whitehand")
                Assert.AreEqual(Side.Shadow, board.Presentation.Control["helms-deep"]);
            Assert.AreEqual(232, board.Presentation.Regions.Values.SelectMany(r => r.Neighbors.Select(n =>
                string.CompareOrdinal(r.Id, n) < 0 ? r.Id + ":" + n : n + ":" + r.Id)).Distinct().Count());
            Assert.AreEqual("contours-2", board.Presentation.Geometry.Version);
        }

        private static string PublicMap(BoardScreen board) => JsonConvert.SerializeObject(new
        {
            Regions = board.Presentation.Regions.Values.OrderBy(r => r.Id).ToArray(),
            Control = board.Presentation.Control.OrderBy(r => r.Key).ToArray(),
            Borders = board.Presentation.Borders.Paths.Values.OrderBy(p => p.Id).Select(path => new
            {
                path.Id,
                Bands = MapBorderData.Bands(path, MapBorderKind.Passage, board.Presentation.Regions)
            }).ToArray()
        });

        private static string Topology(BoardScreen board) => JsonConvert.SerializeObject(new
        {
            board.Presentation.Geometry,
            board.Presentation.Borders.Catalog,
            Neighbors = board.Presentation.Regions.Values.OrderBy(r => r.Id).Select(r => new { r.Id, r.Neighbors }).ToArray()
        });

        private static async Task CaptureRegion(MountedMatchClient client, string name, string region, UI10TestRoom room)
        {
            var map = client.Board.Map;
            var anchor = map.Data.Shapes[region].Anchor;
            // Stay below the army-marker zoom threshold so the settlement itself
            // remains visible. The public units are not removed from the snapshot.
            map.Camera.Zoom = map.FitZoom;
            map.Camera.Pan = map.contentRect.center - new Vector2(anchor[0], anchor[1]) * map.Camera.Zoom;
            map.Refresh();
            await room.Settle();
            LayoutTests.Save(client.Viewport, $"map16-{name}-{client.Board.View.You}-{region}.png");
        }
    }
}
