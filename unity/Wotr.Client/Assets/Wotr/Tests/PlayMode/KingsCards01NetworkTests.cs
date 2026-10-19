using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class KingsCards01NetworkTests
    {
        [UnityTest] public IEnumerator WisdomBranchesRemainExclusiveAndInspectTheChosenSovereign() => Wait(Branch());
        [UnityTest] public IEnumerator InspectedTilesSurviveReconnectAndHideAfterReturningOne() => Wait(Inspection(false));
        [UnityTest] public IEnumerator WisdomCanKeepAllTilesAfterInspecting() => Wait(Inspection(true));
        [UnityTest] public IEnumerator AidAwakensTheChosenSovereignWithoutMovingIt() => Wait(Awakening());
        [UnityTest] public IEnumerator RidersPlansArmyAndLeaderBeforeOneCommit() => Wait(Riders());

        private static async Task Branch()
        {
            using (var room = await Connect("kings-wisdom-branch", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                var originalPolitics = room.Fp.Board.View.Nations.Select(n => n.StepsToWar).ToArray();
                await Layout(room, "wisdom-branch");
                await Reconnect(room);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                Click(room.Fp, "effect-choice-kings.wisdom.inspect");
                await room.At(revision + 1);
                await KingsCardNetworkChecks.ChooseKing(room, room.Fp, "kings.sovereign.dain");
                Assert.AreEqual("InspectedCorruption", room.Fp.Board.View.Decision.Presentation);
                CollectionAssert.AreEqual(originalPolitics, room.Fp.Board.View.Nations.Select(n => n.StepsToWar));
                Assert.IsNull(room.Sh.Board.View.Decision);
            }
        }

        private static async Task Inspection(bool keep)
        {
            using (var room = await Connect(keep ? "kings-wisdom-inspection" : "kings-dark-inspection", ScenarioKind.CardPack))
            {
                const string tile = "base.hunt.standard.1.0.1";
                var revision = room.Fp.Board.View.Revision;
                // UI-274: the inspected tiles stand face up beside Dáin; Keep tiles is the console's button.
                var inspected = await KingsCardNetworkChecks.InspectedTile(room, room.Fp, "kings.sovereign.dain", tile);
                Assert.IsTrue(inspected.ClassListContains("legal-target"));
                StringAssert.StartsWith("Click to return this tile", inspected.tooltip);
                Assert.IsNull(room.Sh.Board.Root.Q<Button>("corruption-choice-" + tile));
                Assert.IsFalse(room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.denethor").Tiles[0].Value.HasValue);
                await Layout(room, keep ? "wisdom-keep" : "dark-return");
                await Reconnect(room);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                Assert.IsTrue(room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Tiles.All(t => t.Value.HasValue));
                if (keep)
                    Click(room.Fp, "effect-choice-keep");
                else
                    PointerGestureEvents.ClickAt(await KingsCardNetworkChecks.InspectedTile(room, room.Fp, "kings.sovereign.dain", tile));
                await room.At(revision + 1);
                var sovereign = room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain");
                Assert.AreEqual(keep ? 2 : 1, sovereign.Tiles.Count);
                Assert.IsNull(sovereign.Corruption);
                Assert.IsTrue(sovereign.Tiles.Where(t => !t.Kings).All(t => t.TileId == "" && !t.Value.HasValue));
                Assert.IsNull(room.Fp.Board.Tray.Q<VisualElement>("inspected-corruption"));
                Assert.AreEqual(revision + 1, room.Sh.Board.View.Revision);
            }
        }

        private static async Task Awakening()
        {
            using (var room = await Connect("kings-aid-awakening", ScenarioKind.CardPack))
            {
                await Layout(room, "aid-awakening");
                await Reconnect(room);
                await KingsCardNetworkChecks.ChooseKing(room, room.Fp, "kings.sovereign.dain");
                Assert.AreEqual(SovereignStatus.Awakened, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Status);
                Assert.AreEqual("erebor", room.Sh.Board.View.Characters.Single(c => c.Id == "kings.sovereign.dain").Location);
                Assert.AreEqual(SovereignStatus.Initial, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.theoden").Status);
            }
        }

        private static async Task Reconnect(MatchTestRoom room)
        {
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
        }

        private static async Task Riders()
        {
            using (var room = await Connect("kings-riders-recruit", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                foreach (var kind in new[] { UnitKind.Elite, UnitKind.Leader })
                {
                    Click(room.Fp, "event-reserve-rohan-" + kind);
                    MapContourProbe.ClickRegion(room.Fp.Board.Map, "edoras");
                    await room.Settle();
                }
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                await Layout(room, "riders-recruit");
                await Reconnect(room);
                await room.Confirm(room.Fp);
                Assert.AreEqual(1, room.Sh.Board.View.Units.Count(u => u.Location == "edoras" && u.Kind == UnitKind.Elite));
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Location == "edoras" && u.Kind == UnitKind.Leader));
                Assert.AreEqual(1, room.Fp.Board.View.Hand.Count);
            }
        }

        private static async Task Layout(MatchTestRoom room, string name)
        {
            var source = room.Fp.Board.Tray.Query<Button>(className: "committed-choice").ToList().FirstOrDefault();
            if (source != null)
                CardNetworkTests.Click(source);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    room.Fp.Panel.targetTexture = target;
                    room.Fp.Panel.scale = scale;
                    await room.Settle();
                    FellowshipLayoutTests.Check(room.Fp.Board);
                    LayoutTests.AssertNoDecisionScroll(room.Fp.Board);
                    Assert.IsEmpty(room.Fp.Board.Tray.Query<TextField>().ToList());
                    foreach (var button in room.Fp.Board.Tray.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-choice-") && b.worldBound.width > 0))
                    {
                        Assert.LessOrEqual(button.worldBound.xMax, room.Fp.Board.Tray.worldBound.xMax + 1);
                        Assert.LessOrEqual(button.worldBound.yMax, room.Fp.Board.Tray.worldBound.yMax + 1);
                    }
                    var inspector = room.Fp.Board.Cards.Root;
                    if (inspector.resolvedStyle.display != DisplayStyle.None)
                    {
                        var rules = inspector.Q<Label>("card-text");
                        var size = rules.MeasureTextSize(rules.text, rules.contentRect.width,
                            VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                        Assert.LessOrEqual(size.y, rules.contentRect.height + 1, "Card rules clipped");
                    }
                    LayoutTests.Save(target, $"kings01-{name}-{width}-{scale:0.00}.png");
                    room.Fp.Panel.targetTexture = room.Fp.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            room.Fp.Panel.scale = 1f;
            await room.Settle();
        }
    }
}
