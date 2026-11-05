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
    // UI-97: the dice panel in the board's gilt and enamel, with readable faces and short descriptions.
    public sealed class DicePanelLayoutTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public IEnumerator BothSidesShowFramedMedallionsWithFaceGlyphsAndShortDescriptionsInFourLayouts() => Wait(Panels());

        private static async Task Panels()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var revision = client.Board.View.Revision;
                foreach (var (width, scale) in Layouts)
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Interface.Preferences.SetScale(scale);
                        await room.Settle();
                        await room.Settle();
                        Check(client.Board, $"{client.Board.View.You}-{width}-{scale:0.00}");
                        LayoutTests.Save(texture, $"ui97-dice-{client.Board.View.You}-{width}-{scale:0.00}.png");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Interface.Preferences.SetScale(1);
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
                Assert.AreEqual(revision, client.Board.View.Revision, "Reading the dice sends no command.");
            }
        }

        private static void Check(BoardScreen board, string label)
        {
            var panel = board.OwnDice;
            Assert.AreEqual("card-board", Texture(panel), "The panels' thin card · " + label);
            var header = panel.Q("dice-header");
            // UI-466 (owner): the Free Peoples' band is blue, the Shadow's red; UI-484: a wash melting into the card.
            var band = board.View.You == Side.FreePeoples ? "side-band-free" : "side-band-shadow";
            Assert.AreEqual(band, Texture(header), "Header band in the side's colour · " + label);
            Inside(header, panel, label);
            var title = header.Q<Label>(className: "title");
            var name = title.MeasureTextSize(title.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(name.x, title.contentRect.width + 1, "The side name is not cut · " + label);
            var side = board.View.You == Side.FreePeoples ? "free" : "shadow";
            var dice = panel.Q(className: "available-dice").Query<Button>(className: "die").ToList();
            Assert.Greater(dice.Count, 0, label);
            foreach (var die in dice)
            {
                var view = board.View.Dice.Single(d => d.Id == die.name);
                Assert.That(Texture(die), Does.StartWith("medallion-" + side), die.name + " · " + label);
                Inside(die, panel, label);
                var face = die.Q(className: DieFaceIcon.ClassName);
                Assert.IsNotNull(face, die.name);
                if (view.Face.HasValue)
                {
                    Assert.AreEqual(DieFaceIcon.Key(view.Face), Texture(face), "Readable face glyph · " + die.name);
                    Assert.GreaterOrEqual(face.worldBound.width, 20, "Glyph size · " + label);
                }
                var lines = die.tooltip.Split('\n');
                Assert.LessOrEqual(lines.Length, 7, "Short description · " + die.name);
                StringAssert.DoesNotContain("pp.", die.tooltip);
                StringAssert.DoesNotContain(die.name, die.tooltip, "No die id in the description.");
            }
            foreach (var pair in dice.SelectMany((a, i) => dice.Skip(i + 1).Select(b => (a, b))))
                Assert.IsFalse(pair.a.worldBound.Overlaps(pair.b.worldBound), "Dice never overlap · " + label);
            // UI-420 (owner, TOOLTIP_AUDIT): the location's name leads its count on the first line ("In the Hunt Box · 2",
            // "Out · none"); one line per die follows.
            foreach (var (location, heading) in new[] { ("Spent", "Spent"), ("Hunt", "In the Hunt Box"), ("Discarded", "Out") })
            {
                var counter = panel.Q<Button>("pool-" + location);
                Inside(counter, panel, label);
                var icon = counter.Q("pool-count-icon");
                Assert.IsNotNull(icon.resolvedStyle.backgroundImage.texture, "Counter icon · " + location);
                Assert.Less(icon.worldBound.xMax, counter.worldBound.xMax, label);
                StringAssert.StartsWith(heading + " · ", counter.tooltip, "The name leads the description.");
            }
            // UI-160: the Elven Rings have their sockets in the turn console.
            Assert.IsEmpty(panel.Query<Button>(className: "pool-ring").ToList(), label);
            Assert.LessOrEqual(panel.worldBound.yMax, board.Root.worldBound.yMax + 1, label);
        }

        private static void Inside(VisualElement inner, VisualElement outer, string label)
        {
            Assert.GreaterOrEqual(inner.worldBound.xMin, outer.worldBound.xMin - 1, inner.name + " · " + label);
            Assert.LessOrEqual(inner.worldBound.xMax, outer.worldBound.xMax + 1, inner.name + " · " + label);
            Assert.GreaterOrEqual(inner.worldBound.yMin, outer.worldBound.yMin - 1, inner.name + " · " + label);
            Assert.LessOrEqual(inner.worldBound.yMax, outer.worldBound.yMax + 1, inner.name + " · " + label);
        }

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;
    }
}
