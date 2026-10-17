using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionLayoutTests
    {
        private static async Task FourLayouts(MatchTestRoom room, string name,
            Func<MatchTestRoom, MountedMatchClient, Task> inspect, bool captureFinal = true, Side? onlySide = null)
        {
            var before = new[] { Json(room.Fp.Board.View), Json(room.Sh.Board.View) };
            foreach (var client in new[] { room.Fp, room.Sh }.Where(client => !onlySide.HasValue || client.Board.View.You == onlySide))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        using var viewport = new LayoutViewport(client, width, scale);
                        await room.Settle();
                        try
                        {
                            await inspect(room, client);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            ExpansionMatrixLayoutTests.ResourcesFit(client.Board);
                        }
                        catch
                        {
                            if (!captureFinal)
                                LayoutTests.Save(viewport.Texture, $"ui72-failure-{name}-{client.Board.View.You}-{width}-{scale:0.00}.png");
                            throw;
                        }
                        finally
                        {
                            if (captureFinal)
                                LayoutTests.Save(viewport.Texture, $"ui72-{name}-{client.Board.View.You}-{width}-{scale:0.00}.png");
                        }
                    }
            await Unchanged(room, before);
        }

        private static async Task Unchanged(MatchTestRoom room, string[] expected)
        {
            var clients = new[] { room.Fp, room.Sh };
            for (var index = 0; index < clients.Length; index++)
            {
                var client = clients[index];
                Assert.AreEqual(expected[index], Json(client.Board.View));
                var actual = await client.Api.Get<PlayerView>("api/matches/" + client.Board.View.MatchId, room.Token);
                Assert.AreEqual(expected[index], Json(actual), "Inspection cannot change either server projection.");
            }
        }

        private static string Json(object value) => JsonConvert.SerializeObject(value);

        private static async Task Press(MatchTestRoom room, VisualElement target)
        {
            Assert.IsNotNull(target);
            Pickable(target);
            PointerGestureEvents.ClickAt(target);
            await room.Settle();
        }

        private static async Task ReadTooltip(MatchTestRoom room, MountedMatchClient client, VisualElement source)
        {
            Assert.IsTrue(source.focusable);
            // Focus exposes read-only text; clicking an ability chip would unroll its text instead (UI-139).
            // Keep one layout wait per ability so the complete corpus fits the room.
            PointerGestureEvents.ClickAt(client.Board.Phase);
            source.Focus();
            var tooltip = client.Interface.Content.parent.Q<Label>("runtime-tooltip");
            Assert.AreSame(source, source.panel.focusController.focusedElement);
            await room.Settle();
            Assert.IsTrue(Visible(tooltip), source.name);
            Assert.AreEqual(source.tooltip, tooltip.text);
            Bounds(tooltip, client.Interface.Content.parent.worldBound);
            FullText(tooltip);
            source.Blur();
            PointerGestureEvents.ClickAt(client.Board.Phase);
            Assert.IsFalse(Visible(tooltip), "An outside click dismisses the description.");
        }

        private static void CheckPanel(VisualElement root, Rect available)
        {
            Assert.IsTrue(Visible(root), root.name);
            Bounds(root, available);
            Assert.IsEmpty(root.Query<ScrollView>().ToList(), root.name + " must not scroll decisions.");
            Assert.IsEmpty(root.Query<TextField>().ToList());
            foreach (var element in root.Query<VisualElement>().ToList().Where(Visible))
            {
                if (element is not Button && element is not Label)
                    continue;
                Bounds(element, root.worldBound);
                if (element is Label label)
                {
                    FullText(label);
                    var button = label.GetFirstAncestorOfType<Button>();
                    if (button != null)
                        Bounds(label, button.worldBound);
                }
                if (element is Button control && control.enabledInHierarchy)
                {
                    FullText(control);
                    Pickable(control);
                }
            }
        }

        private static void FullText(TextElement text)
        {
            if (string.IsNullOrEmpty(text.text))
                return;
            if (text is Button button)
            {
                FullButtonText(button);
                return;
            }
            // MeasureTextSize floors its width to physical pixels; rendering retains the fractional width.
            // Preserve wrapping at that pixel boundary instead of introducing a fictitious extra line.
            var scale = text.scaledPixelsPerPoint;
            var width = Mathf.Ceil(text.contentRect.width * scale) / scale;
            var noWrap = text.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            var measured = text.MeasureTextSize(text.text, noWrap ? 0 : width,
                noWrap ? VisualElement.MeasureMode.Undefined : VisualElement.MeasureMode.Exactly,
                0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(measured.y, text.contentRect.height + 1,
                $"{text.name}: {text.text}\ncontent={text.contentRect}, measured={measured}, wrap={text.resolvedStyle.whiteSpace}, " +
                $"pixelWidth={width}, pixelsPerPoint={scale}");
        }

        private static void FullButtonText(Button button)
        {
            // Unity aligns the text in contentRect; that box excludes padding and is not its clip boundary.
            Assert.IsFalse(button.isElided, button.name + ": " + button.text);
            var style = button.resolvedStyle;
            var width = Mathf.Ceil(button.contentRect.width * button.scaledPixelsPerPoint) / button.scaledPixelsPerPoint;
            var measured = button.MeasureTextSize(button.text, style.whiteSpace == WhiteSpace.NoWrap ? 0 : width,
                style.whiteSpace == WhiteSpace.NoWrap ? VisualElement.MeasureMode.Undefined : VisualElement.MeasureMode.Exactly,
                0, VisualElement.MeasureMode.Undefined);
            var align = style.unityTextAlign;
            var horizontal = align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft
                ? 0f : align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight ? 1f : .5f;
            var vertical = align == TextAnchor.UpperLeft || align == TextAnchor.UpperCenter || align == TextAnchor.UpperRight
                ? 0f : align == TextAnchor.LowerLeft || align == TextAnchor.LowerCenter || align == TextAnchor.LowerRight ? 1f : .5f;
            var origin = button.contentRect.position + new Vector2(
                (button.contentRect.width - measured.x) * horizontal, (button.contentRect.height - measured.y) * vertical);
            var message = button.name + ": " + button.text;
            Assert.GreaterOrEqual(origin.x, style.borderLeftWidth - 1, message);
            Assert.GreaterOrEqual(origin.y, style.borderTopWidth - 1, message);
            Assert.LessOrEqual(origin.x + measured.x, button.layout.width - style.borderRightWidth + 1, message);
            Assert.LessOrEqual(origin.y + measured.y, button.layout.height - style.borderBottomWidth + 1, message);
        }

        private static void Bounds(VisualElement element, Rect parent)
        {
            var bounds = element.worldBound;
            Assert.GreaterOrEqual(bounds.xMin, parent.xMin - 1, element.name);
            Assert.GreaterOrEqual(bounds.yMin, parent.yMin - 1, element.name);
            Assert.LessOrEqual(bounds.xMax, parent.xMax + 1, element.name);
            Assert.LessOrEqual(bounds.yMax, parent.yMax + 1, element.name);
        }

        private static void Pickable(VisualElement element)
        {
            Assert.IsNotNull(element);
            Assert.IsTrue(Visible(element) && element.enabledInHierarchy, element.name);
            var rect = element.worldBound;
            var points = new[] { rect.center, new Vector2(rect.xMin + 2, rect.yMin + 2),
                new Vector2(rect.xMax - 2, rect.yMin + 2), new Vector2(rect.xMin + 2, rect.yMax - 2),
                new Vector2(rect.xMax - 2, rect.yMax - 2) };
            foreach (var point in points)
            {
                var picked = element.panel.Pick(point);
                Assert.IsTrue(picked == element || element.Contains(picked),
                    element.name + " at " + point + " covered by " + picked?.name);
            }
        }

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }

        private sealed class LayoutViewport : IDisposable
        {
            public RenderTexture Texture { get; }
            private readonly MountedMatchClient client;
            private readonly RenderTexture previous;
            private readonly float scale;
            private readonly bool motion;

            public LayoutViewport(MountedMatchClient client, int width, float size)
            {
                this.client = client;
                previous = client.Panel.targetTexture;
                scale = client.Interface.Preferences.Scale;
                motion = client.Interface.Preferences.ReducedMotion;
                Texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                Texture.Create();
                client.Panel.targetTexture = Texture;
                client.Interface.Preferences.SetScale(size);
                client.Interface.Preferences.SetReducedMotion(true);
                client.Board.Windows.Clear();
            }

            public void Dispose()
            {
                client.Panel.targetTexture = previous;
                client.Interface.Preferences.SetScale(scale);
                client.Interface.Preferences.SetReducedMotion(motion);
                Texture.Release();
                Object.Destroy(Texture);
            }
        }
    }
}
