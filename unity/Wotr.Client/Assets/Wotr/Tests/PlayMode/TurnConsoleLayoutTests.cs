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
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-160: the turn console keeps one height and four fixed zones in every layout and state; every control has
    // its place and a hit target of at least 32 points.
    public sealed class TurnConsoleLayoutTests
    {
        // UI-176: besides 16:9, a 16:10 screen, a MacBook screen (about 1.54) and a 21:9 screen; the panel expands, so
        // the console keeps its zones and the board fills every one of them.
        private static readonly (int Width, int Height, float Scale)[] Layouts =
        {
            (1366, 768, 1f), (1366, 768, 1.25f), (1920, 1080, 1f), (1920, 1080, 1.25f),
            (1440, 900, 1f), (3024, 1964, 1.25f), (2560, 1080, 1f), (2560, 1080, 1.25f)
        };

        [UnityTest]
        public IEnumerator BothSidesKeepTheConsoleFixedWithEveryZoneInFourLayouts() => Wait(Consoles());

        private static async Task Consoles()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var revision = client.Board.View.Revision;
                foreach (var (width, pixels, scale) in Layouts)
                {
                    var texture = new RenderTexture(width, pixels, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Interface.Preferences.SetScale(scale);
                        await room.Settle();
                        await room.Settle();
                        var board = client.Board;
                        var label = $"{board.View.You}-{width}x{pixels}-{scale:0.00}";
                        var height = board.Console.Root.layout.height;
                        var bottom = board.Root.Q(className: "bottom").layout.height;
                        var places = Places(board);
                        Check(board, label);
                        Assert.IsFalse(board.Console.Actions.Cancel.enabledSelf, "UI-455: nothing chosen, Cancel greyed · " + label);
                        LayoutTests.Save(texture, $"ui160-console-{label}.png");
                        var die = board.OwnDice.Query<Button>(className: "die").ToList().FirstOrDefault(b => b.enabledInHierarchy);
                        if (board.View.DecisionOwner == board.View.You && die != null)
                        {
                            PointerGestureEvents.ClickAt(die);
                            await room.Settle();
                            StringAssert.Contains(" die", board.Console.Status.Headline.text, label);
                            Assert.AreEqual(height, board.Console.Root.layout.height, 0.01f, "Same console height with a die · " + label);
                            Assert.AreEqual(bottom, board.Root.Q(className: "bottom").layout.height, 0.01f, "Same bottom row · " + label);
                            Check(board, label + " die");
                            // UI-455 (owner): the chosen die can be cancelled; no control moved for it.
                            Assert.IsTrue(board.Console.Actions.Cancel.enabledSelf, "A chosen die can be cancelled · " + label);
                            CollectionAssert.AreEqual(places, Places(board), "Every console control keeps its place · " + label);
                            LayoutTests.Save(texture, $"ui160-console-die-{label}.png");
                            PointerGestureEvents.ClickAt(board.Console.Actions.Back);
                            await room.Settle();
                        }
                        Assert.AreEqual(height, board.Console.Root.layout.height, 0.01f, label);
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Interface.Preferences.SetScale(1);
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
                Assert.AreEqual(revision, client.Board.View.Revision, "Reading the console sends no command.");
            }
        }

        private static void Check(BoardScreen board, string label)
        {
            var console = board.Console;
            var root = console.Root;
            Assert.That(root.layout.height, Is.GreaterThan(80), label);
            Assert.LessOrEqual(root.worldBound.yMax, board.Root.worldBound.yMax + 1, label);
            Assert.LessOrEqual(root.worldBound.xMax, board.Edges.Right.worldBound.xMin + 1, "The hand column stays clear · " + label);
            Assert.IsNotEmpty(console.Status.Headline.text, label);
            Inside(console.Status.Plaque, root, label);
            Inside(console.Status.Hint, root, label);
            Assert.AreSame(console.Hunt.Well, board.Hunt.Tab.parent, label);
            Target(board.Hunt.Tab, root, label);
            for (var index = 0; index < ConsoleRings.Count; index++)
            {
                Target(console.Rings.Socket(index), root, label);
                AssertRoundRingOnly(console.Rings.Socket(index), label);
            }
            foreach (var action in new[] { console.Actions.Back, console.Actions.Confirm, console.Actions.Pass, console.Actions.Discard })
                Target(action, root, label);
            // UI-455 (owner, 2026-11-03): the small Cancel on its own line under the four, always shown.
            var cancel = console.Actions.Cancel;
            Assert.AreEqual(DisplayStyle.Flex, cancel.resolvedStyle.display, "The fixed Cancel · " + label);
            Inside(cancel, root, label);
            Assert.GreaterOrEqual(cancel.worldBound.yMin + 0.5f, console.Actions.Pass.worldBound.yMax, "Cancel under the four · " + label);
            Assert.GreaterOrEqual(cancel.worldBound.xMin + 0.5f, console.Actions.Pass.worldBound.xMin, "Cancel within their column · " + label);
            Assert.LessOrEqual(cancel.worldBound.xMax, console.Actions.Discard.worldBound.xMax + 0.5f, "Cancel within their column · " + label);
            Assert.IsEmpty(root.Query<ScrollView>().ToList(), label);
            Assert.AreEqual(3, board.View.Rings.Count, "The base game has three Elven Rings · " + label);
            FillsTheScreen(board, label);
        }

        // UI-455: the places of the console's controls, to compare between states.
        private static Rect[] Places(BoardScreen board)
        {
            var actions = board.Console.Actions;
            return new[] { actions.Back, actions.Confirm, actions.Pass, actions.Discard, actions.Cancel, board.Console.Status.Plaque, board.Hunt.Tab }
                .Select(element => element.worldBound).ToArray();
        }

        // UI-176: the board takes the whole picture; the dice keep to the left edge, the cards to the right and the
        // console fills the middle between them.
        private static void FillsTheScreen(BoardScreen board, string label)
        {
            var screen = board.Root.panel.visualTree.worldBound;
            var root = board.Root.worldBound;
            Assert.AreEqual(screen.width, root.width, 1, "The board is as wide as the picture · " + label);
            Assert.AreEqual(screen.height, root.height, 1, "The board is as tall as the picture · " + label);
            Assert.LessOrEqual(board.Map.worldBound.xMin, root.xMin + 1, "The map reaches the left edge · " + label);
            Assert.GreaterOrEqual(board.Map.worldBound.xMax, root.xMax - 1, "The map reaches the right edge · " + label);
            var dice = board.OwnDice.worldBound;
            var console = board.Console.Root.worldBound;
            var cards = board.Edges.Right.worldBound;
            Assert.Less(dice.xMin - root.xMin, 12, "The dice keep to the left edge · " + label);
            Assert.Less(root.xMax - cards.xMax, 12, "The cards keep to the right edge · " + label);
            Assert.GreaterOrEqual(console.xMin, dice.xMax - 1, "The console stands right of the dice · " + label);
            Assert.LessOrEqual(console.xMax, cards.xMin + 1, "The console stands left of the cards · " + label);
        }

        // UI-175: a socket shows only its round ring picture: no well, bezel, border or button colour.
        internal static void AssertRoundRingOnly(VisualElement socket, string label)
        {
            var style = socket.resolvedStyle;
            Assert.IsNull(style.backgroundImage.texture, socket.name + " draws no socket art · " + label);
            Assert.IsNull(style.backgroundImage.sprite, socket.name + " draws no socket art · " + label);
            Assert.AreEqual(0f, style.backgroundColor.a, socket.name + " has no button colour · " + label);
            Assert.AreEqual(0f, style.borderLeftWidth + style.borderRightWidth + style.borderTopWidth + style.borderBottomWidth,
                socket.name + " has no border · " + label);
            Assert.IsNotNull(socket.Q("console-ring-image").resolvedStyle.backgroundImage.texture, socket.name + " shows its ring · " + label);
        }

        private static void Target(VisualElement element, VisualElement root, string label)
        {
            Assert.AreEqual(DisplayStyle.Flex, element.resolvedStyle.display, element.name + " · " + label);
            Inside(element, root, label);
            Assert.GreaterOrEqual(element.worldBound.width, 32 * Scale(element), element.name + " width · " + label);
            Assert.GreaterOrEqual(element.worldBound.height, 32 * Scale(element), element.name + " height · " + label);
            var picked = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(picked == element || element.Contains(picked), element.name + " is reachable · " + label);
        }

        private static float Scale(VisualElement element) => element.worldBound.width / Mathf.Max(1, element.layout.width);

        private static void Inside(VisualElement element, VisualElement root, string label)
        {
            var outer = root.worldBound;
            var inner = element.worldBound;
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), element.name + " · " + label);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), element.name + " · " + label);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), element.name + " · " + label);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), element.name + " · " + label);
        }
    }
}
