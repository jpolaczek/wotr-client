using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-92, UI-98: hovering a left tab unrolls an interactive panel; a click never pins it.
    public sealed class LeftRailPreviewTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public IEnumerator HoverUnrollsAnInteractivePanelAndNothingPinsItInFourLayouts() => MatchTestRoom.Wait(Preview());

        private static async Task Preview()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            var client = room.Fp;
            var board = client.Board;
            var revision = board.View.Revision;
            foreach (var (width, scale) in Layouts)
            {
                var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                texture.Create();
                try
                {
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    CloseAll(board);
                    await room.Settle();
                    await Check(board, texture, $"{width}-{scale:0.00}");
                }
                finally
                {
                    CloseAll(board);
                    client.Panel.targetTexture = client.Viewport;
                    client.Interface.Preferences.SetScale(1);
                    texture.Release();
                    Object.Destroy(texture);
                }
            }
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(0, room.Boundary.Commands, "Previews never send a game command.");
            NoKeepOpenOption(board);
        }

        private static async Task Check(BoardScreen board, RenderTexture texture, string label)
        {
            var politics = board.Nations.Drawer;
            var effects = board.TableEffects.Root;
            var characters = board.Characters.Root;
            CardGestures.Move(board.Nations.Root);
            await Task.Delay(260);
            Assert.IsTrue(Visible(politics), "Hover unrolls the panel · " + label);
            Assert.IsTrue(politics.ClassListContains(LeftRailPreview.PreviewClass));
            Assert.AreEqual("politics", board.Edges.Preview.Previewing);
            Assert.IsFalse(board.Nations.Root.ClassListContains("selected"), "A preview keeps the hover frame, not the red lacquer · " + label);
            Assert.IsEmpty(board.Nations.Root.tooltip, "No tooltip covers the preview · " + label);
            Assert.IsFalse(politics.ClassListContains(LeftRailPreview.EnteringClass), "The unroll finishes · " + label);
            var duration = politics.resolvedStyle.transitionDuration.FirstOrDefault();
            Assert.LessOrEqual(duration.value, duration.unit == TimeUnit.Millisecond ? 200 : .2f, label);
            var toggle = board.Nations.ShowOpponent;
            var picked = politics.panel.Pick(toggle.worldBound.center);
            Assert.IsTrue(picked == toggle || toggle.Contains(picked), "The unrolled panel is interactive at once · " + label);
            LayoutTests.Save(texture, $"ui98-left-preview-{label}.png");
            CardGestures.Move(toggle);
            await Task.Delay(300);
            Assert.IsTrue(Visible(politics), "Reading the panel keeps it open · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            Assert.IsFalse(Visible(politics), "Leaving rolls the panel back · " + label);
            Assert.IsEmpty(board.Edges.Preview.Previewing);

            PointerGestureEvents.ClickAt(board.Nations.Root);
            await Task.Delay(200);
            Assert.IsTrue(Visible(politics), "A click unrolls the panel as hovering does · " + label);
            Assert.IsTrue(politics.ClassListContains(LeftRailPreview.PreviewClass), "A click does not pin the panel · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            Assert.IsFalse(Visible(politics), "Without a pin the clicked panel rolls back too · " + label);

            // The click left focus on the tab; focus arriving from the keyboard is a new focus-in.
            board.Nations.Root.Blur();
            await Task.Delay(50);
            board.Nations.Root.Focus();
            await Task.Delay(260);
            Assert.IsTrue(Visible(politics), "Keyboard focus unrolls the panel · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            Assert.IsTrue(Visible(politics), "Keyboard focus holds the panel without the pointer · " + label);
            board.Nations.Root.Blur();
            await Task.Delay(420);
            Assert.IsFalse(Visible(politics), "Moving focus away rolls the panel back · " + label);

            board.Characters.Open();
            await Task.Delay(200);
            CardGestures.Move(board.TableEffects.Tab);
            await Task.Delay(260);
            Assert.IsTrue(Visible(effects) && effects.ClassListContains(LeftRailPreview.PreviewClass), label);
            Assert.IsFalse(Visible(characters), "One left panel at a time · " + label);
            CardGestures.Move(board.Map);
            await Task.Delay(420);
            Assert.IsFalse(Visible(effects), label);
            Assert.IsTrue(Visible(characters), "A panel opened by its context returns after a preview · " + label);
            Assert.IsFalse(characters.ClassListContains(LeftRailPreview.PreviewClass), label);
        }

        // UI-98: no veil, hint or description anywhere offers to keep a panel open.
        private static void NoKeepOpenOption(BoardScreen board)
        {
            Assert.IsNull(board.Root.Q(className: "edge-panel-veil"));
            foreach (var element in board.Root.Query<VisualElement>().ToList())
            {
                StringAssert.DoesNotContain("keep open", (element.tooltip ?? "").ToLowerInvariant(), element.name);
                if (element is TextElement text)
                    StringAssert.DoesNotContain("keep open", (text.text ?? "").ToLowerInvariant(), element.name);
            }
        }

        private static void CloseAll(BoardScreen board)
        {
            board.Nations.Close();
            board.TableEffects.Close();
            board.Characters.Close();
        }

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }
    }
}
