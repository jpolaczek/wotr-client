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
    // view, at one layout; the presentation tests drive it. UI-309: the Shadow may watch instead, with the Free Peoples'
    // Will of the West die in play.
    internal sealed class PresentedBoardStage : IDisposable
    {
        public const string Die = "shadow-army";
        public const string FreeDie = "free-will";
        private readonly MountedMatchClient client;
        private readonly BoardFixture fixture;
        private readonly PlayerView before;
        private OpponentPresentationModel model;
        private long revision;
        public RenderTexture Texture { get; }
        public BoardScreen Board => client.Board;
        public PresentationPlayback Playback => client.Playback;
        public OpponentPresenters Presenters => client.Presenters;
        // UI-314: the objects the presentation lights now (the observed glow).
        public List<VisualElement> Lit => Board.Root.Query(className: PresentationGlow.ClassName).ToList();
        // The presented view before Ready(): a test may set its nations, control and figures.
        public PlayerView Before => before;
        public Side Viewer { get; }
        public Side Opponent => Viewer == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
        public string OpponentDie => Viewer == Side.FreePeoples ? Die : FreeDie;

        public PresentedBoardStage(int width, float scale, bool reduced, Side viewer = Side.FreePeoples)
        {
            Viewer = viewer;
            client = new MountedMatchClient("http://localhost", "presented-board");
            Texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            Texture.Create();
            client.Panel.targetTexture = Texture;
            client.Interface.Preferences.SetScale(scale);
            client.Interface.Preferences.SetReducedMotion(reduced);
            fixture = new BoardFixture();
            before = fixture.View;
            if (viewer == Side.Shadow)
                Watch(before);
            else
                before.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
        }

        // The Shadow's board while the Free Peoples act: no decision of its own, their Will of the West die available.
        private static void Watch(PlayerView view)
        {
            view.You = Side.Shadow;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = null;
            view.Options.Clear();
            view.Dice.Add(new DieView { Id = FreeDie, Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available });
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
            model = new OpponentPresentationModel(Viewer, Board.Presentation);
            model.Baseline(new ActionLogPage[0], 0);
        }

        public PlayerView Latest() => Next(before);

        // Reserve figures of the presented view.
        public List<string> Reserve(string nation, UnitKind kind, int count) =>
            Board.View.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count).Select(u => u.Id).ToList();

        // A figure change as the engine records it: the same participant before and after, between two places.
        public ActionFigureChange Change(string id, string from, string to)
        {
            var unit = Board.View.Units.Single(u => u.Id == id);
            var figure = new ActionParticipant { Id = id, Side = unit.Side, NationId = unit.Nation, UnitKind = unit.Kind };
            return new ActionFigureChange { Before = figure, After = figure, From = from, To = to };
        }

        public ActionLogStep Recruits(ActionLogStep step, IEnumerable<string> ids, string region)
        {
            foreach (var id in ids)
                step.Facts.Figures.Add(Change(id, "reserve", region));
            return step;
        }

        // UI-309: a character on a view as the server projects it; its figure id stays across versions (Grey → White).
        public static CharacterView Character(PlayerView view, string id, string definition, Side side, string region)
        {
            view.Characters.RemoveAll(c => c.Id == id);
            var character = new CharacterView { Id = id, DefinitionId = definition, Side = side, Location = region, Level = 2, Leadership = 1 };
            view.Characters.Add(character);
            return character;
        }

        public static ActionParticipant Figure(CharacterView character) => new ActionParticipant
        {
            Id = character.Id, Side = character.Side, DefinitionId = character.DefinitionId, Garrison = character.Garrison,
            Level = character.Level, Leadership = character.Leadership
        };

        public ActionParticipant Unit(string id)
        {
            var unit = Board.View.Units.Single(u => u.Id == id);
            return new ActionParticipant { Id = id, Side = unit.Side, NationId = unit.Nation, UnitKind = unit.Kind };
        }

        // The opponent's die paid the step.
        public ActionLogStep Paid(ActionLogStep step, DieFace face)
        {
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = OpponentDie, Owner = Opponent, Face = face, Use = face });
            return step;
        }

        public int Characters(string region, Side side) =>
            Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == side).Sum(a => a.Characters);

        // The names on the character moments and the moving groups' identities now on the map.
        public string Moments() => string.Join(",", Board.Map.Query<Label>("observed-portrait-name").ToList().Select(label => label.text));
        public string Identities() => string.Join(",", Board.Map.Query<Label>("observed-identity-name").ToList().Select(label => label.text));

        public static void Place(PlayerView view, IEnumerable<string> ids, string region)
        {
            foreach (var unit in view.Units.Where(u => ids.Contains(u.Id)))
                unit.Location = region;
        }

        public PlayerView Next(PlayerView view)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            next.Dice.Single(d => d.Id == OpponentDie).Location = DieLocation.Spent;
            return next;
        }

        public ActionLogStep Step(int order, long at = 1) => new ActionLogStep
        {
            Id = "presented-" + at + "-" + order, ActionId = "presented-action-" + at, Revision = at, Order = order, Actor = Opponent,
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
            Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == Opponent && a.Garrison == garrison).Sum(a => a.Total);

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

        // The released board matches the latest view: every stack of the opponent (its units and characters) and settlement.
        public void AssertFinal(PlayerView latest)
        {
            var figures = latest.Units.Where(u => u.Side == Opponent).Select(u => (u.Location, u.Garrison))
                .Concat(latest.Characters.Where(c => c.Side == Opponent).Select(c => (c.Location, c.Garrison)));
            var expected = figures.Where(f => Board.Map.Data.Regions.ContainsKey(f.Location))
                .GroupBy(f => f).ToDictionary(g => g.Key, g => g.Count());
            var shown = Board.Presentation.Armies.Where(a => a.Side == Opponent)
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
