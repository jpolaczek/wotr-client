using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-437 (owner's playtest, 2026-11-01): after the Black Serpent's Red Wrath the battle window kept his portrait in a row
    // of its own above his force and the replacement's marker beside the card, so the Shadow's statistics, card, roll and
    // dice stood lower than the Free Peoples'. Both columns now hold the same rows, and the characters stand in the force row.
    public class BattleRowsNetworkTests
    {
        private const string Serpent = "kings.character.black-serpent";

        [UnityTest] public IEnumerator RedWrathKeepsBothSidesRowsLevel() => Wait(RedWrath());

        private static async Task RedWrath()
        {
            using (var room = await Connect("ui277-chieftain", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                await room.Settle();
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(room.Sh.Board.Tray.Q("battle-defender").Q<Button>("battle-replacement-" + Serpent));
                await room.At(revision + 1);
                await room.Until(() => room.Fp.Board.Tray.Q("battle-defender")?.Q("battle-replacement-applied") != null);
                // The Shadow takes Red Wrath's hit in its own column; the Free Peoples see the marker beside its card.
                await Measure(room, room.Fp, "red-wrath", rolled: false);
                await TakeLosses(room, room.Sh);
                await RollBoth(room);
                var measured = 0;
                foreach (var client in new[] { room.Sh, room.Fp })
                    if (!Choosing(client))
                    {
                        await Measure(room, client, "rolled-" + (client == room.Sh ? "shadow" : "free-peoples"), rolled: true);
                        measured++;
                    }
                Assert.Greater(measured, 0, "At least one player sees both forces after the roll.");
                watch.NeverOpened();
            }
        }

        // The cards are revealed and both armies roll; the steps before that (a hit to take) are made as the player would.
        private static async Task RollBoth(MatchTestRoom room)
        {
            for (var step = 0; step < 6 && !Rolled(room.Fp); step++)
            {
                var client = room.Actor;
                if (client.Board.View.Decision?.Kind != DecisionKind.Losses)
                    break;
                await TakeLosses(room, client);
            }
            await room.Until(() => Rolled(room.Fp) && Rolled(room.Sh));
        }

        private static bool Rolled(MountedMatchClient client)
        {
            var battle = client.Board.View.Battle;
            return battle != null && battle.Attacker.Dice.Count > 0 && battle.Defender.Dice.Count > 0;
        }

        private static async Task TakeLosses(MatchTestRoom room, MountedMatchClient client)
        {
            await room.Until(() => client.Board.View.Options.Any(o => o.Kind == CommandKind.TakeBattleLosses));
            await room.Until(() => !client.Playback.Holding);
            await room.Settle();
            var view = client.Board.View;
            var option = view.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses);
            var remaining = view.Battle.PendingHits;
            foreach (var unit in view.Units.Where(u => option.EligibleIds.Contains(u.Id)).OrderBy(u => u.Kind).Take(remaining))
                Click(client, "loss-" + unit.Id);
            await room.Confirm(client);
        }

        private static bool Choosing(MountedMatchClient client) =>
            client.Board.Tray.Query(className: "battle-force-choosing").ToList().Count > 0;

        private static async Task Measure(MatchTestRoom room, MountedMatchClient client, string name, bool rolled)
        {
            await room.Until(() => !client.Playback.Holding && client.Board.Tray.Q("battle-defender") != null);
            await room.Settle();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    await room.Settle();
                    await room.Settle();
                    LayoutTests.Save(target, $"ui437-{name}-{width}-{scale:0.00}.png");
                    var label = $"{name} {width}×{scale:0.00}";
                    Level(client.Board.Tray, label, rolled);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Panel.scale = 1f;
            await room.Settle();
        }

        private static void Level(VisualElement tray, string label, bool rolled)
        {
            BattleRowChecks.Level(tray, label, rolled);
            var defender = tray.Q("battle-defender");
            Assert.IsNotNull(defender.Q("battle-replacement-applied"), label + ": Red Wrath's marker beside the Shadow's card");
            if (rolled)
                StringAssert.StartsWith("Combat · Hits", defender.Q<Label>("battle-roll-source").text, label);
            SerpentInForceRow(defender, label);
        }

        // The Black Serpent stands in his force's row, on the line of his army's figures.
        private static void SerpentInForceRow(VisualElement defender, string label)
        {
            var row = defender.Q("battle-force-row");
            var portrait = defender.Q("portrait-" + Serpent);
            Assert.IsNotNull(portrait, label + ": the Black Serpent's portrait");
            Assert.IsTrue(row.Contains(portrait), label + ": the portrait stands in the force row");
            var figure = defender.Q(className: "battle-figure");
            Assert.IsNotNull(figure, label + ": the Shadow's figure");
            var cell = defender.Q("battle-character-" + Serpent);
            Assert.That(cell.worldBound.yMax, Is.EqualTo(figure.worldBound.yMax).Within(BattleRowChecks.Tolerance + 1), label + ": on the figures' line");
            Assert.AreEqual("Black Serpent", cell.Q<Label>("battle-character-name").text, label);
            Assert.Greater(portrait.worldBound.height, 0, label);
        }
    }
}
