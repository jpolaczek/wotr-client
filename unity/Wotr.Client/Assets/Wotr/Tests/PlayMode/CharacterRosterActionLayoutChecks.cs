using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.PlayTests
{
    internal static class CharacterRosterActionLayoutChecks
    {
        public static async Task Capture(MatchTestRoom room, MountedMatchClient client, string name)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Interface.Preferences.SetScale(scale);
                        client.Interface.Windows.Clear();
                        client.Board.Characters.OpenDecision();
                        await room.Settle();
                        LayoutTests.Save(texture, $"ui67-{name}-{width}-{scale:0.00}.png");
                        Check(client);
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
            client.Interface.Preferences.SetScale(1f);
            await room.Settle();
        }

        private static void Check(MountedMatchClient client)
        {
            var board = client.Board;
            var inspector = board.Characters.Inspector.Root;
            Inside(inspector, board.Map.worldBound);
            Assert.AreSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.IsEmpty(inspector.Query<ScrollView>().ToList());
            ExpansionMatrixLayoutTests.ResourcesFit(board);
            Inside(board.OwnDice, board.Root.worldBound);
            Inside(board.OpponentDice, board.Root.worldBound);
            foreach (var rectangle in board.Characters.FixedOcclusions())
                Assert.IsFalse(inspector.worldBound.Overlaps(rectangle), "Fixed control covered: " + rectangle);
            foreach (var button in inspector.Query<Button>().ToList().Where(Visible))
            {
                Inside(button, inspector.worldBound);
                if (board.Characters.DecisionHost.Contains(button))
                    Assert.GreaterOrEqual(button.resolvedStyle.height, 32f, button.name);
                var picked = button.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked), button.name + " " + button.worldBound + " covered by " +
                    picked?.name + " " + picked?.worldBound + " in " + picked?.parent?.name);
            }
            foreach (var label in inspector.Query<Label>().ToList().Where(Visible))
            {
                Inside(label, inspector.worldBound);
                var measured = label.MeasureTextSize(label.text, label.contentRect.width,
                    VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.y, label.contentRect.height + 1,
                    label.name + " clipped text: " + label.text + " at " + label.contentRect);
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
        }

        private static void Inside(VisualElement element, Rect bounds)
        {
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.xMin - 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.yMax + 1, element.name);
        }
    }
}
