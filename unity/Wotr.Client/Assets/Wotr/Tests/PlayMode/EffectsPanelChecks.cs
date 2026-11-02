using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-424: the Effects panel's shared layout checks — every source title inside its tile, every entry inside the panel,
    // the panel and the details card beside it above the bottom row — for the layout, stress and network tests.
    internal static class EffectsPanelChecks
    {
        public static List<Button> Sources(BoardScreen board) =>
            board.TableEffects.Root.Query<Button>().ToList().Where(button => button.name.StartsWith("effect-source-") &&
                button.parent.resolvedStyle.display != DisplayStyle.None && button.worldBound.width > 0).ToList();

        // No title is drawn outside its tile: its wrapped text, aligned as the label aligns it, stays inside the tile's text
        // area — within the parchment's frame and clear of the card symbol; the tile stays inside its entry and the entry
        // inside the panel.
        public static void TitlesInsideTiles(BoardScreen board, string layout)
        {
            var panel = board.TableEffects.Root.worldBound;
            foreach (var title in Sources(board))
            {
                var message = $"{title.text} · {layout}";
                Assert.IsFalse(title.isElided, message);
                var text = TextRect(title);
                var area = title.contentRect;
                Assert.GreaterOrEqual(text.yMin, area.yMin - .5f, "title text above its tile's frame · " + message);
                Assert.LessOrEqual(text.yMax, area.yMax + .5f, "title text below its tile's frame · " + message);
                // A wrapped measure spans the whole line, so the width is checked word by word: none is wider than the area.
                foreach (var word in EffectTitleFit.Split(title.text))
                    Assert.LessOrEqual(SingleLine(title, word), area.width + 1, $"'{word}' wider than its tile's text area · " + message);
                Assert.GreaterOrEqual(title.layout.height, 32 - .5f, "a 32-point target · " + message);
                Inside(title.worldBound, title.parent.worldBound, "tile inside its entry · " + message);
                Inside(title.parent.worldBound, panel, "entry inside the panel · " + message);
            }
        }

        // The title's wrapped text block in the tile's own coordinates, centred vertically as the label centres it, at the
        // pixel boundary its label renders at.
        private static Rect TextRect(Button title)
        {
            var content = title.contentRect;
            var scale = title.scaledPixelsPerPoint;
            var width = Mathf.Ceil(content.width * scale) / scale;
            var measured = title.MeasureTextSize(title.text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
            return new Rect(content.x, content.y + (content.height - measured.y) * .5f, measured.x, measured.y);
        }

        private static float SingleLine(Button title, string text) =>
            title.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        // The panel ends above the bottom row (the console's row) and below the top bar.
        public static void PanelAboveBottomRow(BoardScreen board, string layout)
        {
            var panel = board.TableEffects.Root.worldBound;
            Assert.GreaterOrEqual(panel.yMin + 1, board.TopBar.Root.worldBound.yMax, "effects-panel below the top bar · " + layout);
            Assert.LessOrEqual(panel.yMax - 1, BottomRow(board), "effects-panel above the bottom row · " + layout);
        }

        // UI-424: a source's details stand on their own card beside the panel, whole, above the bottom row, every part inside.
        public static void DetailsBesideThePanel(BoardScreen board, string layout)
        {
            var details = board.TableEffects.Details;
            Assert.AreEqual(DisplayStyle.Flex, details.resolvedStyle.display, "details shown · " + layout);
            Assert.AreEqual(Visibility.Visible, details.resolvedStyle.visibility, "details placed · " + layout);
            var card = details.worldBound;
            var panel = board.TableEffects.Root.worldBound;
            Assert.GreaterOrEqual(card.xMin + 1, panel.xMax, "details beside the panel · " + layout);
            Assert.GreaterOrEqual(card.yMin + 1, board.TopBar.Root.worldBound.yMax, "details below the top bar · " + layout);
            Assert.LessOrEqual(card.yMax - 1, BottomRow(board), "details above the bottom row · " + layout);
            Assert.LessOrEqual(card.xMax - 1, board.TableEffects.Root.parent.worldBound.xMax, "details on the screen · " + layout);
            foreach (var element in details.Query<VisualElement>().ToList().Where(e => e != details && e.visible && e.worldBound.width > 0))
                Inside(element.worldBound, card, element.name + " inside the details · " + layout);
            foreach (var label in details.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None && l.text.Length > 0))
                WholeText(label, layout);
        }

        // Every line of a label is inside its box, measured at the pixel boundary its label renders at.
        private static void WholeText(Label label, string layout)
        {
            var scale = label.scaledPixelsPerPoint;
            var width = Mathf.Ceil(label.contentRect.width * scale) / scale;
            var noWrap = label.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            var measured = label.MeasureTextSize(label.text, noWrap ? 0 : width,
                noWrap ? VisualElement.MeasureMode.Undefined : VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(measured.y, label.contentRect.height + 1, $"{label.name}: {label.text} clipped · {layout}");
            if (noWrap)
                Assert.LessOrEqual(measured.x, label.contentRect.width + 1, $"{label.name}: {label.text} cut · {layout}");
        }

        public static float BottomRow(BoardScreen board) => board.Console.Root.parent.worldBound.yMin;

        // Every entry's place in the grid, its columns and rows (layout, so the panel's unrolling motion does not count).
        public static Rect[] Grid(BoardScreen board) =>
            board.TableEffects.Root.Query(className: "effect-entry").ToList().Select(entry => entry.layout).ToArray();

        public static void SameGrid(Rect[] before, Rect[] after, string layout)
        {
            Assert.AreEqual(before.Length, after.Length, "the same entries · " + layout);
            for (var i = 0; i < before.Length; i++)
            {
                Assert.That(after[i].x, Is.EqualTo(before[i].x).Within(.5f), "the grid's columns stay · " + layout);
                Assert.That(after[i].width, Is.EqualTo(before[i].width).Within(.5f), "the grid's columns stay · " + layout);
            }
        }

        private static void Inside(Rect inner, Rect outer, string message)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, message);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, message);
            Assert.LessOrEqual(inner.xMax - 1, outer.xMax, message);
            Assert.LessOrEqual(inner.yMax - 1, outer.yMax, message);
        }
    }
}
