using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class CardViewportLayoutTests
    {
        [UnityTest]
        public IEnumerator ActionCardFitsBothEffectsAtFourLayoutsAndKeepsPinnedPositionAfterRefresh()
        {
            using var client = new MountedMatchClient("http://localhost", "card-viewport");
            var view = new BoardFixture().View;
            const string card = "base.fp.character.18";
            view.Hand.Add(card);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        client.Apply(view);
                        client.Board.Cards.Show(card, true);
                        yield return new WaitForSecondsRealtime(.2f);
                        AssertFits(client.Board);
                        Assert.AreEqual(DisplayStyle.Flex, client.Board.Cards.Root.Q(className: "card-toolbar").resolvedStyle.display,
                            "The regression requires the action toolbar; discard-only previews are shorter.");
                        var header = client.Board.Cards.Root.Q(className: "card-header");
                        header.Focus();
                        using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow }))
                            header.SendEvent(key);
                        yield return null;
                        var retained = client.Board.Cards.Root.worldBound;
                        client.Apply(view);
                        yield return new WaitForSecondsRealtime(.1f);
                        Assert.AreEqual(retained, client.Board.Cards.Root.worldBound,
                            "A refresh must retain the settled size and the pinned window position.");
                        Assert.IsTrue(client.Board.Cards.Pinned);
                        AssertFits(client.Board);
                        LayoutTests.Save(target, $"map17-card-fit-{width}-{scale:0.00}.png");
                        AssertRulesExposed(client.Board);
                        AssertParchment(client.Board.Cards.Root);
                        Assert.IsFalse(client.Board.Cards.Root.worldBound.Overlaps(client.Board.Menu.Toggle.worldBound),
                            "The permanent Menu shortcut must leave the complete card readable.");
                        yield return MatchTestRoom.Wait(BoardNavigation.Victory(client.Board));
                        yield return new WaitForSecondsRealtime(.1f);
                        Assert.IsTrue(client.Board.Victory.IsOpen);
                        Assert.IsTrue(client.Board.Cards.Pinned);
                        Assert.AreEqual(retained, client.Board.Cards.Root.worldBound,
                            "Moving the shortcut must not rewrite the player's pinned card position.");
                        PointerGestureEvents.ClickAt(client.Board.Victory.Root.Q<Button>("close-victory"));
                        yield return new WaitForSecondsRealtime(.1f);
                        Assert.IsFalse(client.Board.Victory.IsOpen);
                        AssertRulesExposed(client.Board);
                    }
                    finally
                    {
                        client.Board.Cards.Close();
                        client.Panel.targetTexture = client.Viewport;
                        client.Panel.scale = 1;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        internal static void AssertFits(BoardScreen board)
        {
            var root = board.Cards.Root;
            var area = board.Map.worldBound;
            Assert.GreaterOrEqual(root.worldBound.xMin, area.xMin - 1);
            Assert.GreaterOrEqual(root.worldBound.yMin, area.yMin - 1);
            Assert.LessOrEqual(root.worldBound.xMax, area.xMax + 1);
            Assert.LessOrEqual(root.worldBound.yMax, area.yMax + 1, "The map viewport clips the card's bottom.");
            foreach (var label in root.Query<Label>(className: "card-rules").ToList())
            {
                if (label.parent.resolvedStyle.display == DisplayStyle.None)
                    continue;
                var required = label.MeasureTextSize(label.text, label.contentRect.width,
                    VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.GreaterOrEqual(label.resolvedStyle.fontSize, 14);
                Assert.LessOrEqual(required.y, label.contentRect.height + 1, "Measured card text is clipped: " + label.name);
                Assert.LessOrEqual(label.worldBound.yMax, root.worldBound.yMax - 7, "Rules exceed the card padding.");
            }
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
        }

        // UI-106: the opened hand card is parchment with dark ink, like the hand and the recorded actions.
        private static void AssertParchment(VisualElement card)
        {
            Assert.AreEqual("parchment", card.resolvedStyle.backgroundImage.texture?.name);
            foreach (var label in card.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None &&
                         l.text.Length > 0 && !(l.parent is Button) && !(l is Button)))
            {
                var color = label.resolvedStyle.color;
                Assert.Less(color.r + color.g + color.b, 1.5f, "Dark ink on parchment: " + label.name);
            }
        }

        private static void AssertRulesExposed(BoardScreen board)
        {
            foreach (var label in board.Cards.Root.Query<Label>(className: "card-rules").ToList())
            {
                if (label.parent.resolvedStyle.display == DisplayStyle.None)
                    continue;
                var text = label.contentRect;
                var y = text.yMax - label.resolvedStyle.fontSize / 2;
                foreach (var fraction in new[] { .05f, .5f, .95f })
                {
                    var point = label.LocalToWorld(new Vector2(Mathf.Lerp(text.xMin, text.xMax, fraction), y));
                    var picked = label.panel.Pick(point);
                    Assert.IsTrue(picked == label || label.Contains(picked),
                        $"The final rule line in {label.name} is obscured at {point} by {picked?.name}.");
                }
            }
        }
    }
}
