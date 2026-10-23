using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-306/UI-307: one mounted board receiving the Shadow's recorded steps while it presents the Free Peoples' earlier
    // view, at one layout; the presentation tests drive it.
    internal sealed class PresentedBoardStage : IDisposable
    {
        public const string Die = "shadow-army";
        private readonly MountedMatchClient client;
        private readonly BoardFixture fixture;
        private readonly PlayerView before;
        private OpponentPresentationModel model;
        private long revision;
        public RenderTexture Texture { get; }
        public BoardScreen Board => client.Board;
        public PresentationPlayback Playback => client.Playback;
        public OpponentPresenters Presenters => client.Presenters;
        // The presented view before Ready(): a test may set its nations, control and figures.
        public PlayerView Before => before;

        public PresentedBoardStage(int width, float scale, bool reduced)
        {
            client = new MountedMatchClient("http://localhost", "presented-board");
            Texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            Texture.Create();
            client.Panel.targetTexture = Texture;
            client.Interface.Preferences.SetScale(scale);
            client.Interface.Preferences.SetReducedMotion(reduced);
            fixture = new BoardFixture();
            before = fixture.View;
            before.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
        }

        public List<string> Put(string nation, UnitKind kind, string region, int count)
        {
            var ids = new List<string>();
            for (var i = 0; i < count; i++)
                ids.Add(fixture.Put(nation, kind, region).Id);
            return ids;
        }

        public IEnumerator Ready()
        {
            client.Apply(before);
            yield return new WaitForSecondsRealtime(.3f);
            Board.SetConnection("Connected");
            Playback.SetConnection("Connected");
            model = new OpponentPresentationModel(Side.FreePeoples, Board.Presentation);
            model.Baseline(new ActionLogPage[0], 0);
        }

        public PlayerView Latest() => Next(before);

        public PlayerView Next(PlayerView view)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            next.Dice.Single(d => d.Id == Die).Location = DieLocation.Spent;
            return next;
        }

        public ActionLogStep Step(int order, long at = 1) => new ActionLogStep
        {
            Id = "presented-" + at + "-" + order, ActionId = "presented-action-" + at, Revision = at, Order = order, Actor = Side.Shadow,
            Kind = "ArmiesMoved"
        };

        // focus: a region far from the action, "between:a:b" for a view that already shows both, or null to keep it.
        public void Play(PlayerView latest, string focus, params ActionLogStep[] steps)
        {
            Focus(focus);
            revision = latest.Revision;
            client.Refresh(latest);
            model.Advance(revision);
            Playback.Enqueue(model.Merge(new[] { new ActionLogPage { Steps = steps.ToList() } }));
            Playback.HistoryRead(revision);
        }

        private void Focus(string focus)
        {
            if (focus == null)
                return;
            var parts = focus.Split(':');
            if (parts[0] == "between")
                Board.Map.FocusOn((Anchor(parts[1]) + Anchor(parts[2])) / 2, Board.Map.FitZoom * 1.4f);
            else
                Board.Map.FocusOn(Anchor(focus), Board.Map.FitZoom * 2.2f);
        }

        // The board's controls, without the map's markers (board objects a presentation stages) and the opponent's pool,
        // whose spent die leaves with the release.
        public int Buttons() => Board.Root.Query<Button>().ToList()
            .Count(button => !Board.OpponentDice.Contains(button) && !Board.Map.Contains(button));

        public Vector2 Anchor(string region) =>
            new Vector2(Board.Map.Data.Shapes[region].Anchor[0], Board.Map.Data.Shapes[region].Anchor[1]);

        public int Army(string region, bool garrison = false) =>
            Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == Side.Shadow && a.Garrison == garrison).Sum(a => a.Total);

        public bool Flying(string destination) =>
            Board.Map.Figures.ActiveCount > 0 && Board.Map.Figures.Moves.Any(move => move.To.Region == destination);

        public IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(condition(), "The presentation did not reach the expected state.");
        }

        public IEnumerator Released()
        {
            yield return Until(() => !Playback.Holding);
            yield return null;
            Assert.AreEqual(revision, Board.View.Revision);
        }

        // The released board matches the latest view: every Shadow stack and settlement.
        public void AssertFinal(PlayerView latest)
        {
            var expected = latest.Units.Where(u => u.Side == Side.Shadow && Board.Map.Data.Regions.ContainsKey(u.Location))
                .GroupBy(u => (u.Location, u.Garrison)).ToDictionary(g => g.Key, g => g.Count());
            var shown = Board.Presentation.Armies.Where(a => a.Side == Side.Shadow)
                .ToDictionary(a => (a.RegionId, a.Garrison), a => a.Total);
            CollectionAssert.AreEquivalent(expected, shown);
            foreach (var region in latest.Control.Keys)
                Assert.AreEqual(latest.Control[region], Board.Presentation.Control.GetValueOrDefault(region), region);
        }

        // The release replays nothing that was presented (or skipped).
        public IEnumerator NothingFlies()
        {
            for (var frame = 0; frame < 20; frame++)
            {
                Assert.AreEqual(0, Board.Map.Figures.ActiveCount, "Nothing replays after the release.");
                yield return null;
            }
        }

        public void Dispose()
        {
            client.Dispose();
            Texture.Release();
            Object.Destroy(Texture);
        }
    }
}
