using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-424: the Effects panel's shared layout checks — every source title inside its tile, every entry inside the panel,
    // the panel above the bottom row; UI-446: a hovered source's unrolled text whole, with only its part in effect, and a
    // read-only source's click opening nothing — for the layout, stress and network tests.
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

        // UI-446 (owner): a hovered source's text unrolls whole — below the top bar, above the bottom row, inside the screen,
        // every shown part inside it — and shows only the source's part in effect (ActivePart). It replaces the details card
        // UI-424 stood beside the panel.
        public static void UnrolledText(BoardScreen board, string sourceId, string layout)
        {
            var unroll = board.TableEffects.Root.Q("effect-unroll");
            Assert.AreEqual(sourceId, board.TableEffects.UnrolledSource, "the hovered source unrolls · " + layout);
            Assert.AreEqual(DisplayStyle.Flex, unroll.resolvedStyle.display, "unrolled text shown · " + layout);
            var text = unroll.worldBound;
            var stage = board.TableEffects.Root.parent.worldBound;
            Assert.GreaterOrEqual(text.yMin + 1, board.TopBar.Root.worldBound.yMax, "unrolled text below the top bar · " + layout);
            Assert.LessOrEqual(text.yMax - 1, BottomRow(board), "unrolled text above the bottom row · " + layout);
            Assert.GreaterOrEqual(text.xMin + 1, stage.xMin, "unrolled text on the screen · " + layout);
            Assert.LessOrEqual(text.xMax - 1, stage.xMax, "unrolled text on the screen · " + layout);
            foreach (var element in unroll.Query<VisualElement>().ToList().Where(e => e != unroll && Displayed(e, unroll)))
                Inside(element.worldBound, text, element.name + " inside the unrolled text · " + layout);
            foreach (var label in unroll.Query<Label>().ToList().Where(l => l.text.Length > 0 && Displayed(l, unroll)))
                WholeText(label, layout);
            ActivePart(board, unroll, sourceId, layout);
        }

        // A card shows its Event text, or only its Combat text while every effect of the source lasts a battle or a combat
        // round (EffectsPresentation.CombatPart); an ability or another effect shows its descriptions as plain text.
        private static void ActivePart(BoardScreen board, VisualElement unroll, string sourceId, string layout)
        {
            var sources = EffectsPresentation.Sources(board.View).Where(source => source.SourceId == sourceId).ToList();
            var plain = unroll.Q<Label>("effect-unroll-text");
            if (!CardLibrary.Cards.TryGetValue(sourceId, out var card))
            {
                Assert.IsNull(unroll.Q("effect-card"), "an ability or effect has no card text · " + layout);
                Assert.AreEqual(DisplayStyle.Flex, plain.resolvedStyle.display, "its descriptions shown · " + layout);
                Assert.IsTrue(sources.Any(source => plain.text == Descriptions(source)), $"'{plain.text}' describes the source · {layout}");
                return;
            }
            Assert.AreEqual(DisplayStyle.None, plain.resolvedStyle.display, "a card has no plain text · " + layout);
            var block = unroll.Q("effect-card");
            Assert.IsNotNull(block, "the card's text · " + layout);
            Assert.That(block.resolvedStyle.backgroundImage.texture?.name, Does.StartWith("parchment"), layout);
            Assert.AreEqual(CardTextFormat.Kind(card), block.Q<Label>("effect-card-kind").text, layout);
            Assert.AreEqual(CardLibrary.EventText(card, board.View.Configuration), block.Q<Label>("effect-card-event").text, layout);
            var combat = EffectsPresentation.CombatPart(sources.Single());
            Assert.AreEqual(combat ? DisplayStyle.None : DisplayStyle.Flex, block.Q("effect-card-event").resolvedStyle.display,
                "only the part in effect: the Event text · " + layout);
            Assert.AreEqual(combat && card.HasCombat ? DisplayStyle.Flex : DisplayStyle.None, block.Q("effect-card-combat").resolvedStyle.display,
                "only the part in effect: the Combat text · " + layout);
            if (combat && card.HasCombat)
                Assert.AreEqual(card.Combat, block.Q<Label>("effect-card-combat-text").text, layout);
        }

        private static string Descriptions(EffectSource source) => string.Join("\n", source.Effects.Select(effect => effect.Description)
            .Where(description => !string.IsNullOrEmpty(description)).Distinct());

        // UI-446 (owner): a click on a read-only source does nothing — no details card, no selection, no card window.
        public static void ClickOpensNothing(BoardScreen board, string sourceId, string layout)
        {
            Assert.IsTrue(ReadOnlyControl.Is(board.TableEffects.Root.Q<Button>("effect-source-" + sourceId)), "a read-only source · " + layout);
            Assert.AreEqual(DisplayStyle.None, board.TableEffects.Details.resolvedStyle.display, "no details · " + layout);
            Assert.IsEmpty(board.TableEffects.SelectedSource, "nothing selected · " + layout);
            Assert.IsEmpty(board.Cards.CardId, "no separate card window · " + layout);
        }

        // The element and every parent up to the root are displayed, and it has a size.
        private static bool Displayed(VisualElement element, VisualElement root)
        {
            for (var current = element; current != null && current != root.parent; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
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
