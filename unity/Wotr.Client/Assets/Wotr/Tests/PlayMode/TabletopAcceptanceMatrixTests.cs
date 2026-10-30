using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-407 (TABLETOP_FIGURES_UX, "Acceptance (UI-407)"): the tabletop figures' screen matrix — 1366 × 768 and
    // 1920 × 1080 at 100 and 125 %, both seats, the whole map (tokens) and zoomed in (miniatures), on the dense map17 board
    // (the server's map17-dense scene) and UI-401's Helm's Deep siege; and on the server's fp-army-move and sh-army-move
    // scenes each side's army carried (both levels), held at the map's edge while it pans, and in flight after its drop,
    // seen by its own seat and by the opponent's. Every frame is saved as .artifacts/screenshots/ui407-*.png before its
    // checks run; a failed check is collected, so one run shows the whole matrix. Occlusions beyond UI-422's limits (a
    // count badge or a settlement's stone under another stand) are measured and logged as "UI-407 measure".
    public sealed class TabletopAcceptanceMatrixTests
    {
        internal static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };
        // The miniatures' level: zoomed in past the Detail level (1.3 × the whole map).
        internal const float Near = 2.2f;
        private const string Siege = "helms-deep";

        [UnityTest]
        public IEnumerator TheDenseMap17BoardReadsForBothSeatsAtEveryLayoutAndLevel() => MatchTestRoom.Wait(DenseBoard());

        [UnityTest]
        public IEnumerator TheHelmsDeepSiegeReadsForBothSeatsAtEveryLayoutAndLevel()
        {
            var findings = new MatrixFindings();
            foreach (var layout in Layouts)
            {
                var places = new Dictionary<Side, Dictionary<string, Vector2>>();
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    var fixture = SiegeSeat(side);
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    using var viewport = new MatrixViewport(client, layout);
                    client.Apply(fixture.View);
                    yield return Settle();
                    foreach (var near in new[] { false, true })
                    {
                        FrameWhole(client.Board, near);
                        yield return Settle();
                        var label = Label("siege", side, layout, near);
                        viewport.Save(Name("siege", side, layout, near));
                        findings.Check(label, () => TabletopFrameChecks.AssertStatic(client.Board.Map, near, Siege, label));
                        if (near)
                            findings.Check(label, () => MapStandLayoutTests.AssertSiege(client.Board.Map, label));
                        MatrixFindings.Measure(label, TabletopFrameChecks.Occlusions(client.Board.Map));
                    }
                    places[side] = TabletopFrameChecks.Places(client.Board.Map);
                }
                findings.Check("siege " + layout, () => TabletopFrameChecks.AssertSamePlaces(places[Side.FreePeoples],
                    places[Side.Shadow], "siege " + layout));
            }
            findings.AssertNone();
        }

        [UnityTest]
        public IEnumerator TheFreePeoplesCarryPanAndFlyAnArmyAtEveryLayout() =>
            MatchTestRoom.Wait(Carry(Side.FreePeoples, "fp-army-move"));

        [UnityTest]
        public IEnumerator TheShadowCarriesPansAndFliesAnArmyAtEveryLayout() =>
            MatchTestRoom.Wait(Carry(Side.Shadow, "sh-army-move"));

        // The seeder's map17-dense match from both seats: Helm's Deep's garrison with three companions under a siege of
        // Isengard and three Nazgûl, and Gondor's fourteen figures in Westemnet. Read only: nothing is sent.
        private static async Task DenseBoard()
        {
            var findings = new MatrixFindings();
            using var room = await UI10TestRoom.Connect("map17-dense");
            foreach (var layout in Layouts)
            {
                var places = new Dictionary<Side, Dictionary<string, Vector2>>();
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var side = client.Board.View.You;
                    using var viewport = new MatrixViewport(client, layout);
                    Quiet(client.Board);
                    await Settle(room);
                    foreach (var near in new[] { false, true })
                    {
                        FrameWhole(client.Board, near);
                        await Settle(room);
                        var label = Label("map17", side, layout, near);
                        viewport.Save(Name("map17", side, layout, near));
                        findings.Check(label, () => TabletopFrameChecks.AssertStatic(client.Board.Map, near, Siege, label));
                        MatrixFindings.Measure(label, TabletopFrameChecks.Occlusions(client.Board.Map));
                    }
                    places[side] = TabletopFrameChecks.Places(client.Board.Map);
                }
                findings.Check("map17 " + layout, () => TabletopFrameChecks.AssertSamePlaces(places[Side.FreePeoples],
                    places[Side.Shadow], "map17 " + layout));
            }
            Assert.AreEqual(0, room.Boundary.Commands, "The dense board is only looked at.");
            findings.AssertNone();
        }

        // One fresh test scene per layout, since each ends with a real march.
        private static async Task Carry(Side side, string scenario)
        {
            var findings = new MatrixFindings();
            foreach (var layout in Layouts)
            {
                var room = await TestScenarioRoom.Start(scenario);
                try
                {
                    using var scene = new TabletopCarryScene(room, side, layout, findings);
                    await scene.Run(layout == Layouts[0]);
                }
                finally
                {
                    await room.End();
                }
            }
            findings.AssertNone();
        }

        // UI-401's siege with its neighbours, seen from one seat.
        private static BoardFixture SiegeSeat(Side side)
        {
            var fixture = MapStandLayoutTests.Siege();
            fixture.View.You = side;
            fixture.View.DecisionOwner = side;
            fixture.Refresh();
            return fixture;
        }

        // The whole map (tokens), or zoomed in on the siege (miniatures).
        private static void FrameWhole(BoardScreen board, bool near)
        {
            Quiet(board);
            var map = board.Map;
            if (near)
                map.FocusOn(map.Stands.Stands(Siege).Field, map.FitZoom * Near);
            else
                map.Fit();
        }

        internal static void Quiet(BoardScreen board)
        {
            board.Hover.Close();
            board.Cards.Close();
            board.Windows.Clear();
        }

        internal static string Name(string board, Side side, (int Width, float Scale) layout, bool near) =>
            $"ui407-{board}-{Seat(side)}-{layout.Width}-{layout.Scale:0.00}-{(near ? "near" : "far")}.png";

        internal static string Name(string board, Side side, (int Width, float Scale) layout) =>
            $"ui407-{board}-{Seat(side)}-{layout.Width}-{layout.Scale:0.00}.png";

        internal static string Label(string board, Side side, (int Width, float Scale) layout, bool near) =>
            $"{board} {Seat(side)} {layout.Width} × {layout.Scale:0.00} {(near ? "near" : "far")}";

        internal static string Seat(Side side) => side == Side.FreePeoples ? "fp" : "sh";

        internal static async Task Settle(UI10TestRoom room)
        {
            await room.Settle();
            await room.Settle();
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }
    }

    // One layout of the matrix on a client's own render texture, at the layout's panel scale.
    internal sealed class MatrixViewport : IDisposable
    {
        private readonly MountedMatchClient client;
        public RenderTexture Texture { get; }

        public MatrixViewport(MountedMatchClient client, (int Width, float Scale) layout)
        {
            this.client = client;
            Texture = new RenderTexture(layout.Width, layout.Width == 1366 ? 768 : 1080, 24);
            Texture.Create();
            client.Panel.targetTexture = Texture;
            client.Panel.scale = layout.Scale;
        }

        public void Save(string file) => LayoutTests.Save(Texture, file);

        public void Dispose()
        {
            client.Panel.targetTexture = client.Viewport;
            client.Panel.scale = 1;
            Texture.Release();
            Object.Destroy(Texture);
        }
    }

    // The matrix's failed checks, each with its frame, reported together at the end so one run shows every frame.
    internal sealed class MatrixFindings
    {
        private readonly List<string> failures = new List<string>();

        public void Check(string frame, Action check)
        {
            try
            {
                check();
            }
            catch (AssertionException failure)
            {
                failures.Add(frame + ": " + failure.Message.Trim());
                Debug.LogWarning("UI-407 finding · " + frame + ": " + failure.Message.Trim());
            }
        }

        public void AssertNone() => Assert.IsEmpty(failures, string.Join("\n", failures));

        // Readability measured beyond the asserted limits, logged with its frame for the acceptance record.
        public static void Measure(string frame, IEnumerable<string> notes)
        {
            var list = new List<string>(notes);
            Debug.Log($"UI-407 measure · {frame}: {list.Count} occluded" + (list.Count > 0 ? " — " + string.Join("; ", list) : ""));
        }
    }
}
