using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-511 (owner's pictures: a nation's emblem and figures standing over the stage ribbon and the force heading): a force
    // of many nations and kinds keeps every figure, emblem and portrait inside its own column, under its heading — in the
    // four standard layouts and on narrower screens — and a whole nation leaving fades there too. The owner's case itself
    // was not reproduced by these; they guard the live window.
    public class BattleCrowdedForceTests
    {
        [UnityTest]
        public IEnumerator ManyNationsStayInsideTheirColumnUnderTheHeading()
        {
            using (var client = new MountedMatchClient("http://localhost", "battle-crowded"))
                foreach (var (width, height, scale) in new[]
                         {
                             (1366, 768, 1f), (1366, 768, 1.25f), (1920, 1080, 1f), (1920, 1080, 1.25f),
                             (1280, 720, 1f), (1152, 720, 1f), (1024, 768, 1f)
                         })
                {
                    var target = new RenderTexture(width, height, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Apply(Crowded());
                    yield return Settle();
                    if (!client.Board.Battles.IsOpen)
                        CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                    yield return Settle();
                    var label = $"{width}×{height}×{scale:0.00}";
                    LayoutTests.Save(target, $"ui511-crowded-{width}x{height}-{scale:0.00}.png");
                    foreach (var role in new[] { "attacker", "defender" })
                        InsideColumn(client.Board.Tray.Q("battle-" + role), role, label);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // The whole Sauron group leaves the crowded defender: its fading copy stands in the column, under the heading, and
        // goes within a moment.
        [UnityTest]
        public IEnumerator ALeavingNationGroupFadesInsideItsColumn()
        {
            using var client = new MountedMatchClient("http://localhost", "battle-crowded-leave");
            var target = new RenderTexture(1366, 768, 24);
            target.Create();
            client.Panel.targetTexture = target;
            client.Panel.scale = 1.25f;
            var view = Crowded();
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.4f);
            if (!client.Board.Battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            yield return new WaitForSecondsRealtime(.4f);
            client.Refresh(WithoutSauron(view));
            var column = client.Board.Tray.Q("battle-defender");
            var heading = column.Q("defender-strength");
            yield return null;
            yield return null;
            var ghosts = Ghosts(client);
            Assert.IsNotEmpty(ghosts, "The leaving group fades where it stood.");
            foreach (var ghost in ghosts.Where(g => !float.IsNaN(g.worldBound.height)))
            {
                Assert.GreaterOrEqual(ghost.worldBound.yMin + 1, heading.worldBound.yMax, ghost.name + " under the heading");
                Assert.LessOrEqual(ghost.worldBound.xMax - 1, column.worldBound.xMax, ghost.name + " inside the column");
            }
            yield return new WaitForSecondsRealtime((Appearance.CloseMilliseconds + 300) / 1000f);
            Assert.IsEmpty(Ghosts(client), "Then it goes.");
            client.Panel.targetTexture = client.Viewport;
            target.Release();
            Object.Destroy(target);
        }

        private static System.Collections.Generic.List<VisualElement> Ghosts(MountedMatchClient client) =>
            client.Board.Tray.Query(className: "battle-nation-group").ToList().Where(g => g.name.StartsWith("leaving-")).ToList();

        private static void InsideColumn(VisualElement column, string role, string label)
        {
            var heading = column.Q(role + "-strength");
            var row = column.Q("battle-force-row");
            var parts = row.Query(className: "battle-figure").ToList()
                .Concat(row.Query(className: "battle-nation-emblem").ToList())
                .Concat(row.Query(className: "portrait-medallion").ToList())
                .ToList();
            Assert.IsNotEmpty(parts, label + ": " + role + " shows its force");
            foreach (var part in parts)
            {
                var where = $"{label}: {role} {part.name} at {part.worldBound}";
                Assert.GreaterOrEqual(part.worldBound.yMin + 1, heading.worldBound.yMax, where + " stands under the heading " + heading.worldBound);
                Assert.GreaterOrEqual(part.worldBound.xMin + 1, column.worldBound.xMin, where + " inside the column " + column.worldBound);
                Assert.LessOrEqual(part.worldBound.xMax - 1, column.worldBound.xMax, where + " inside the column " + column.worldBound);
            }
        }

        // The Shadow defends with two characters and figures of three nations and all four kinds; the Free Peoples attack.
        private static PlayerView Crowded()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Defender;
            foreach (var id in new[] { "witch-king", "saruman" })
            {
                var character = new CharacterView { Id = id, DefinitionId = "base.character." + id, Side = Side.Shadow, Location = force.RegionId };
                view.Characters.Add(character);
                force.Characters.Add(character);
                force.ParticipantIds.Add(id);
            }
            Add(view, force, "sauron", UnitKind.Nazgul, 2);
            Add(view, force, "sauron", UnitKind.Elite, 2);
            Add(view, force, "isengard", UnitKind.Regular, 7);
            Add(view, force, "isengard", UnitKind.Elite, 3);
            Add(view, force, "southrons", UnitKind.Regular, 2);
            Add(view, force, "southrons", UnitKind.Elite, 1);
            return view;
        }

        private static PlayerView WithoutSauron(PlayerView view)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            var gone = next.Battle.Defender.Units.Where(u => u.Nation == "sauron").Select(u => u.Id).ToList();
            next.Battle.Defender.Units.RemoveAll(u => gone.Contains(u.Id));
            next.Battle.Defender.ParticipantIds.RemoveAll(gone.Contains);
            next.Units.RemoveAll(u => gone.Contains(u.Id));
            return next;
        }

        private static void Add(PlayerView view, BattleForceView force, string nation, UnitKind kind, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var unit = new UnitView
                {
                    Id = $"crowd-{nation}-{kind}-{i}", Nation = nation, Kind = kind, Side = force.Side, Location = force.RegionId
                };
                view.Units.Add(unit);
                force.Units.Add(unit);
                force.ParticipantIds.Add(unit.Id);
            }
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 3; i++)
                yield return null;
        }
    }
}
