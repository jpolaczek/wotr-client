using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-471: every game button wears the menus' thin line from the shared tokens of Styles/GameChrome.uss. These checks
    // read what UI Toolkit resolved, so they also prove that var() reaches background-image, the slices and the colours;
    // an art button keeps its own picture, slices and press.
    public sealed class GameButtonChromeTests
    {
        private const string Frontend = "Wotr/Art/UI/frontend/";
        private static readonly Color Muted = new Color32(228, 216, 188, 255);

        [UnityTest]
        public IEnumerator TextAndIconButtonsWearTheThinLineInEveryState()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.SetOnline();
            yield return Settle();
            var menu = client.Board.Root.Q<Button>("open-board-menu");
            var icon = client.Board.Console.Actions.Pass;
            foreach (var button in new[] { menu, icon })
            {
                var label = button.name;
                var style = button.resolvedStyle;
                Assert.AreEqual(Art("button"), style.backgroundImage.texture, label);
                Assert.AreEqual(0f, style.backgroundColor.a, 0.01f, label);
            }
            var slices = icon.resolvedStyle;
            Assert.AreEqual(12f, slices.unitySliceTop, icon.name + ": the 6-point slice of the 2x art");
            Assert.AreEqual(12f, slices.unitySliceRight, icon.name);
            Assert.AreEqual(12f, slices.unitySliceBottom, icon.name);
            Assert.AreEqual(12f, slices.unitySliceLeft, icon.name);
            Assert.AreEqual(0.5f, slices.unitySliceScale, 0.001f, icon.name);
            // UI-479: the Menu's picture stands whole at 28 points on its 32-point target: a slice would take no background-size.
            Assert.AreEqual(0f, menu.resolvedStyle.unitySliceTop, menu.name);
            Assert.AreEqual(28f, menu.resolvedStyle.backgroundSize.y.value, 0.01f, menu.name);
            Assert.AreEqual(Muted, menu.resolvedStyle.color, "Muted parchment words, as in the menus.");

            var cancel = client.Board.Console.Actions.Cancel;
            cancel.AddToClassList("selected");
            yield return Settle();
            Assert.AreEqual(Art("button-selected"), cancel.resolvedStyle.backgroundImage.texture, "Selected is the warm field.");
            Assert.AreEqual(2f, cancel.resolvedStyle.borderTopWidth, 0.01f, "The layout keeps the width of the old rim.");
            Assert.AreEqual(0f, cancel.resolvedStyle.borderTopColor.a, 0.01f, "Selected wears no gold rim: that belongs to a legal target.");
            cancel.RemoveFromClassList("selected");

            cancel.AddToClassList("legal-target");
            yield return Settle();
            Assert.AreEqual((Color)new Color32(255, 214, 112, 255), cancel.resolvedStyle.borderTopColor, "A legal target keeps its gold rim (UI-372).");
            Assert.AreEqual(2f, cancel.resolvedStyle.borderTopWidth, 0.01f);
            cancel.RemoveFromClassList("legal-target");

            foreach (var button in new[] { menu, icon })
            {
                button.AddToClassList("pressed");
                yield return Settle();
                Assert.AreEqual(Art("button-pressed"), button.resolvedStyle.backgroundImage.texture, button.name + " presses with its art.");
                Assert.AreEqual(1f, button.resolvedStyle.translate.y, 0.01f, button.name + " sinks a point (UI-113).");
                button.RemoveFromClassList("pressed");
            }
        }

        [UnityTest]
        public IEnumerator AnArtButtonKeepsItsPictureSlicesAndPress()
        {
            var fixture = new BoardFixture();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            client.SetOnline();
            yield return Settle();
            client.Board.Nations.Open();
            yield return Settle();
            var token = client.Board.Nations.Drawer.Query<Button>(className: "nation-token").ToList().First();
            var picture = token.resolvedStyle.backgroundImage.texture;
            Assert.IsNotNull(picture, token.name);
            StringAssert.StartsWith("politics-token", picture.name, token.name);
            Assert.AreEqual(24f, token.resolvedStyle.unitySliceTop, token.name + " keeps the slices it always had.");
            Assert.AreEqual(24f, token.resolvedStyle.unitySliceLeft, token.name);
            token.AddToClassList("pressed");
            yield return Settle();
            Assert.AreEqual(picture, token.resolvedStyle.backgroundImage.texture, token.name + " keeps its picture while pressed.");
            Assert.AreEqual((Color)new Color32(206, 196, 172, 255), token.resolvedStyle.unityBackgroundImageTintColor, token.name + " darkens as before.");
            token.RemoveFromClassList("pressed");
        }

        private static Texture2D Art(string name) => Resources.Load<Texture2D>(Frontend + name);

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 3; frame++)
                yield return null;
            yield return new WaitForSeconds(0.12f);
        }
    }
}
