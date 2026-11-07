using System.Collections;
using System.Collections.Generic;
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
    // four standard layouts and on narrower screens — and a whole nation leaving fades there too. The owner's case came
    // from 1920×1080 at 100 %, where the composition's rounding broke its last nation into a line the force row had not
    // made room for (Battle.uss); the owner's forces and a sweep of window sizes guard it.
    public class BattleCrowdedForceTests
    {
        [UnityTest]
        public IEnumerator ManyNationsStayInsideTheirColumnUnderTheHeading()
        {
            using (var client = new MountedMatchClient("http://localhost", "battle-crowded"))
                foreach (var (width, height, scale) in new[]
                         {
                             (1366, 768, 1f), (1366, 768, 1.25f), (1920, 1080, 1f), (1920, 1080, 1.25f),
                             (1280, 720, 1f), (1152, 720, 1f), (1024, 768, 1f), (1470, 956, 1.25f)
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

        // UI-511, the owner's battles (their last production game, read with their consent): the Shadow attacks while the
        // Free Peoples choose their Combat card — Saruman in the field with Isengard (7 Regulars, 3 Elites) and two Nazgûl, and
        // the Witch-king's siege of Minas Tirith in round two with two Nazgûl, two Sauron Regulars and four Isengard Elites —
        // on laptop screens at 125 %, where the owner saw the Sauron group over the ribbon and the heading.
        [UnityTest]
        public IEnumerator TheOwnersBattlesStayInsideTheirColumns([Values("saruman", "witch-king")] string commander)
        {
            using (var client = new MountedMatchClient("http://localhost", "battle-owner-" + commander))
                foreach (var (width, height, scale) in new[]
                         {
                             (1470, 956, 1.25f), (1512, 982, 1.25f), (1440, 900, 1.25f), (1366, 768, 1.25f), (1280, 800, 1.25f),
                             (1470, 956, 1f), (1920, 1080, 1.25f), (1920, 1080, 1f)
                         })
                {
                    var target = new RenderTexture(width, height, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Apply(commander == "saruman"
                        ? OwnersBattle(commander, BattleMode.Field, 1, ("isengard", UnitKind.Regular, 7), ("isengard", UnitKind.Elite, 3), ("sauron", UnitKind.Nazgul, 2))
                        : OwnersBattle(commander, BattleMode.Siege, 2, ("sauron", UnitKind.Nazgul, 2), ("sauron", UnitKind.Regular, 2), ("isengard", UnitKind.Elite, 4)));
                    yield return Settle();
                    if (!client.Board.Battles.IsOpen)
                        CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                    yield return new WaitForSecondsRealtime(.3f);
                    var label = $"{commander} {width}×{height}×{scale:0.00}";
                    LayoutTests.Save(target, $"ui511-owner-{commander}-{width}x{height}-{scale:0.00}.png");
                    foreach (var role in new[] { "attacker", "defender" })
                        InsideColumn(client.Board.Tray.Q("battle-" + role), role, label);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // UI-511: the owner's pictures came from 1920×1080 at 100 %, where the layout's rounding to whole pixels left the
        // composition a fraction narrower than its figures; its last nation broke into a line the force row had not made
        // room for and the first one stood over the heading. Window sizes from 1104 to 2560 pixels wide, at 100 and 125 %,
        // with three Shadow forces of two and three nations: every figure and emblem stays under the heading and inside
        // its column. (Before the fix, 858 of 2196 such windows, swept every 4 pixels, lifted a nation.)
        [UnityTest]
        public IEnumerator ForcesStayUnderTheHeadingAtEveryWindowSize()
        {
            var problems = new List<string>();
            using (var client = new MountedMatchClient("http://localhost", "battle-sizes"))
                foreach (var scale in new[] { 1f, 1.25f })
                    for (var width = 1104; width <= 2560; width += 52)
                    {
                        var height = width * 10 / 16;
                        var target = new RenderTexture(width, height, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        var forces = SweptForces();
                        for (var index = 0; index < forces.Length; index++)
                        {
                            client.Apply(forces[index]);
                            yield return null;
                            if (!client.Board.Battles.IsOpen)
                                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                            yield return null;
                            yield return null;
                            foreach (var role in new[] { "attacker", "defender" })
                                problems.AddRange(Outside(client.Board.Tray.Q("battle-" + role), role, $"{width}×{height}×{scale:0.00} force {index}"));
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(12)));
        }

        private static PlayerView[] SweptForces()
        {
            var forces = new[]
            {
                OwnersBattle("witch-king", BattleMode.Siege, 2, ("sauron", UnitKind.Nazgul, 2), ("sauron", UnitKind.Regular, 2), ("isengard", UnitKind.Elite, 4)),
                OwnersBattle("saruman", BattleMode.Field, 1, ("isengard", UnitKind.Regular, 7), ("isengard", UnitKind.Elite, 3), ("sauron", UnitKind.Nazgul, 2),
                    ("southrons", UnitKind.Regular, 1)),
                OwnersBattle("witch-king", BattleMode.Field, 1, ("sauron", UnitKind.Nazgul, 1), ("sauron", UnitKind.Regular, 2), ("sauron", UnitKind.Elite, 1),
                    ("isengard", UnitKind.Elite, 4), ("southrons", UnitKind.Regular, 1), ("southrons", UnitKind.Elite, 1))
            };
            for (var index = 0; index < forces.Length; index++)
                forces[index].MatchId = "battle-sizes-" + index;
            return forces;
        }

        private static IEnumerable<string> Outside(VisualElement column, string role, string label)
        {
            var heading = column.Q(role + "-strength");
            var row = column.Q("battle-force-row");
            return row.Query(className: "battle-figure").ToList()
                .Concat(row.Query(className: "battle-nation-emblem").ToList())
                .Where(part => part.worldBound.yMin + 1 < heading.worldBound.yMax || part.worldBound.xMax - 1 > column.worldBound.xMax)
                .Select(part => $"{label}: {role} {part.name} at {part.worldBound}, heading {heading.worldBound}, column {column.worldBound}");
        }

        private static PlayerView OwnersBattle(string commander, BattleMode mode, int round, params (string Nation, UnitKind Kind, int Count)[] shadow)
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            view.MatchId = "battle-owner-" + commander;
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Hand = new List<string>
            {
                "base.fp.character.01", "base.fp.character.02", "base.fp.strategy.01", "base.fp.strategy.02"
            };
            view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds = view.Hand.ToList();
            var battle = view.Battle;
            var freePeoples = battle.Attacker;
            freePeoples.CardCommitted = false;
            freePeoples.CardId = null;
            freePeoples.Dice.Clear();
            var attacker = new BattleForceView
            {
                Side = Side.Shadow, RegionId = battle.RegionId, HitTarget = 5, CombatDice = 5, Leadership = 2, CardBack = "strategy"
            };
            var character = new CharacterView
            {
                Id = commander, DefinitionId = "base.character." + commander, Side = Side.Shadow, Location = battle.RegionId
            };
            view.Characters.Add(character);
            attacker.Characters.Add(character);
            attacker.ParticipantIds.Add(commander);
            foreach (var (nation, kind, count) in shadow)
                Add(view, attacker, nation, kind, count);
            attacker.Strength = attacker.Units.Count(ArmyDraft.IsCombat);
            battle.Attacker = attacker;
            battle.Defender = freePeoples;
            battle.Mode = mode;
            battle.Round = round;
            battle.CardsRevealed = false;
            return view;
        }

        private static List<VisualElement> Ghosts(MountedMatchClient client) =>
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
