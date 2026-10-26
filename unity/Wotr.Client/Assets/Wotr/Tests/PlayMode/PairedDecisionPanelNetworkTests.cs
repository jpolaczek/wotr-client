using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class PairedDecisionPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator BothSidesReadHuntBesideFellowshipInFourLayoutsWithoutChangingTheDraft() =>
            MatchTestRoom.Wait(Layouts());

        [UnityTest]
        public IEnumerator WindowPositionsSurviveReconnectAndResetRestoresTheDefaultPlaces() =>
            MatchTestRoom.Wait(PositionAndRecovery());

        private static async Task Layouts()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-reroll", shadowBoundary: true);
            var snapshots = new[] { JsonConvert.SerializeObject(room.Fp.Board.View), JsonConvert.SerializeObject(room.Sh.Board.View) };
            var selected = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.RerollHunt).EligibleIds.First();
            foreach (var mounted in new[] { room.Fp, room.Sh })
                await BoardNavigation.Fellowship(mounted.Board);
            PointerGestureEvents.ClickAt(room.Sh.Board.Hunt.Root.Q<Button>("hunt-die-" + selected));
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var previous = client.Panel.targetTexture;
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        try
                        {
                            client.Panel.targetTexture = target;
                            client.Interface.Preferences.SetScale(scale);
                            client.Board.Windows.Clear();
                            await room.Settle();
                            Assert.IsTrue(client.Board.Fellowship.IsOpen);
                            Assert.IsTrue(client.Board.Hunt.IsOpen);
                            // UI-322: the layout is read once what closed has faded and the windows have glided to their places.
                            await Task.Delay((int)(Appearance.CloseMilliseconds + FloatingWindow.GlideSeconds * 1000) + 100);
                            Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            LayoutTests.Save(target, $"ui75-paired-{client.Board.View.You}-{width}-{scale:0.00}.png");
                            Assert.AreEqual(client.Board.Root.worldBound, client.Board.Map.worldBound,
                                "The map fills the complete screen behind both HUDs.");
                            if (client == room.Sh)
                                Assert.IsTrue(client.Board.Hunt.Root.Q<Button>("hunt-die-" + selected).ClassListContains("selected"));
                            else
                                Assert.IsNull(client.Board.View.Decision);
                        }
                        finally
                        {
                            client.Panel.targetTexture = previous;
                            target.Release();
                            Object.Destroy(target);
                        }
                    }
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.AreEqual(snapshots[0], JsonConvert.SerializeObject(room.Fp.Board.View));
            Assert.AreEqual(snapshots[1], JsonConvert.SerializeObject(room.Sh.Board.View));
        }

        private static async Task PositionAndRecovery()
        {
            using var room = await UI10TestRoom.Connect("ui34-hunt-reroll", shadowBoundary: true);
            var client = room.Sh;
            var board = client.Board;
            await BoardNavigation.Fellowship(board);
            Assert.IsTrue(board.Fellowship.IsOpen, "The map marker explicitly opens Fellowship beside the held Hunt.");
            await room.Settle();
            Check(board);
            var die = board.Hunt.Root.Q<Button>("hunt-die-" + board.View.Options.Single(o => o.Kind == CommandKind.RerollHunt).EligibleIds.First());
            PointerGestureEvents.ClickAt(die);
            // Capture the rendered selection before comparing it with reconnect.
            await room.Settle();
            var revision = board.View.Revision;
            // UI-162: the Hunt is a floating window like the battle window; the Fellowship panel stays docked.
            Assert.IsNull(board.Fellowship.Root.Q(className: FloatingWindow.HandleClass));
            var fellowship = board.Fellowship.Root.worldBound;
            var hunt = board.Hunt.Root.worldBound;
            var connections = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > connections);
            await room.Settle();
            Assert.AreEqual(fellowship, board.Fellowship.Root.worldBound);
            Assert.AreEqual(hunt, board.Hunt.Root.worldBound);
            Assert.IsFalse(board.Windows.TryGet(board.Fellowship.Root.name, out _));
            Assert.IsFalse(board.Windows.TryGet(board.Hunt.Root.name, out _));
            Assert.IsTrue(die.ClassListContains("selected"));
            board.Windows.Clear();
            await room.Settle();
            Check(board);
            board.Fellowship.Close();
            await client.Connection.Refresh();
            await room.Settle();
            Assert.IsFalse(board.Fellowship.Pinned);
            Assert.IsTrue(board.Hunt.IsOpen);
            Assert.IsTrue(die.ClassListContains("selected"));
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        // QUAL-19: since UI-162 the Hunt window stands beside the Fellowship's region like a battle window instead of
        // sharing the Fellowship panel's column; both stay readable side by side and every control is a pointer target.
        private static void Check(BoardScreen board)
        {
            Assert.IsTrue(board.Fellowship.IsOpen && board.Hunt.IsOpen);
            var first = board.Fellowship.Root.worldBound;
            var second = board.Hunt.Root.worldBound;
            Assert.IsFalse(first.Overlaps(second), $"Fellowship {first} and the Hunt window {second} must not overlap.");
            foreach (var panel in new[] { board.Fellowship.Root, board.Hunt.Root })
            {
                var bounds = panel.worldBound;
                var viewport = panel.parent.worldBound;
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 1));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(viewport.xMax + 1));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1));
                foreach (var button in panel.Query<Button>().ToList().Where(b => b.enabledInHierarchy && Visible(b)))
                {
                    var target = button.worldBound;
                    foreach (var point in new[] { target.center, target.min + Vector2.one * 2, target.max - Vector2.one * 2,
                        new Vector2(target.xMin + 2, target.yMax - 2), new Vector2(target.xMax - 2, target.yMin + 2) })
                    {
                        var picked = button.panel.Pick(point);
                        Assert.IsTrue(picked == button || button.Contains(picked), button.name + " covered by " + picked?.name);
                    }
                }
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
        }
    }
}
