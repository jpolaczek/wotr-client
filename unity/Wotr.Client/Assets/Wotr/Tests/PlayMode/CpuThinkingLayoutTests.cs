using System.Collections;
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
    // Owner (2026-11-11): while the CPU decides, the One Ring turns in the console's line beside "CPU is thinking…": inside
    // the console, clear of its actions, never taking a click; with reduced motion the ring stands still and the dots change.
    public sealed class CpuThinkingLayoutTests
    {
        private static readonly (int Width, int Height, float Scale)[] Layouts =
        {
            (1366, 768, 1.25f), (1920, 1080, 1f)
        };

        [UnityTest]
        public IEnumerator TheRingTurnsInTheConsoleWhileTheCpuThinks() => Wait(Thinking());

        private static async Task Thinking()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance);
            var waiting = room.Fp.Board.View.DecisionOwner == room.Fp.Board.View.You ? room.Sh : room.Fp;
            var board = waiting.Board;
            board.CpuOpponent = true;
            foreach (var (width, pixels, scale) in Layouts)
            {
                var texture = new RenderTexture(width, pixels, 24);
                texture.Create();
                try
                {
                    waiting.Panel.targetTexture = texture;
                    waiting.Interface.Preferences.SetScale(scale);
                    waiting.Interface.Preferences.SetReducedMotion(false);
                    board.Console.Status.SetThinking(true);
                    await room.Settle();
                    var thinking = board.Console.Status.Thinking;
                    var label = $"{width}x{pixels}-{scale:0.00}";
                    var first = thinking.Angle;
                    await Task.Delay(400);
                    Assert.AreNotEqual(first, thinking.Angle, "The ring turns · " + label);
                    Assert.AreEqual(DisplayStyle.None, board.Console.Status.Hint.resolvedStyle.display, "The hint gives way · " + label);
                    var console = board.Console.Root.worldBound;
                    var ring = thinking.Ring.worldBound;
                    var words = thinking.Text.worldBound;
                    Assert.Greater(ring.width, 10f, label);
                    Assert.IsTrue(console.Contains(ring.min) && console.Contains(ring.max), "The ring stays in the console · " + label);
                    Assert.IsTrue(console.Contains(words.min) && console.Contains(words.max), "The words stay in the console · " + label);
                    Assert.LessOrEqual(thinking.Text.MeasureTextSize(thinking.Text.text, 0, VisualElement.MeasureMode.Undefined, 0,
                        VisualElement.MeasureMode.Undefined).x, thinking.Text.contentRect.width + 1, "The words are whole · " + label);
                    foreach (var button in board.Console.Actions.Root.Query<Button>().ToList())
                        if (button.resolvedStyle.display == DisplayStyle.Flex)
                        {
                            Assert.IsFalse(ring.Overlaps(button.worldBound), $"The ring covers {button.name} · {label}");
                            Assert.IsFalse(words.Overlaps(button.worldBound), $"The words cover {button.name} · {label}");
                        }
                    LayoutTests.Save(texture, $"cpu-thinking-{label}.png");

                    waiting.Interface.Preferences.SetReducedMotion(true);
                    await room.Settle();
                    var still = thinking.Angle;
                    var text = thinking.Text.text;
                    await Task.Delay(1200);
                    Assert.AreEqual(still, thinking.Angle, "Reduced motion keeps the ring still · " + label);
                    Assert.AreNotEqual(text, thinking.Text.text, "Reduced motion changes the dots · " + label);
                    waiting.Interface.Preferences.SetReducedMotion(false);
                }
                finally
                {
                    waiting.Panel.targetTexture = null;
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
            board.Console.Status.SetThinking(false);
            await room.Settle();
            Assert.IsFalse(board.Console.Status.Thinking.Shown);
        }
    }
}
