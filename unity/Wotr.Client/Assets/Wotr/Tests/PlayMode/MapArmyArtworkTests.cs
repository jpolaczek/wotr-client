using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-126: tokens and figures on a base, with emblems, counts and the base picture of every state.
    public sealed class MapArmyArtworkTests
    {
        private static readonly (string Class, string Suffix)[] States =
        {
            ("", ""), ("moved", "-moved"), ("eligible-army", ""), ("ability-owner", "-selected"),
            ("selected-force", "-selected"), ("pressed", "-pressed")
        };

        [UnityTest]
        public IEnumerator TokensAndFiguresKeepTheirPicturesEmblemsAndCountsInsideTheClickableMarker()
        {
            using var client = new MountedMatchClient("http://localhost", "army-artwork-layout");
            var gallery = new VisualElement { name = "army-artwork-gallery" };
            gallery.style.position = Position.Absolute;
            gallery.style.left = 120;
            gallery.style.top = 120;
            gallery.style.width = 900;
            gallery.style.height = 460;
            client.Board.Root.Add(gallery);
            var cases = AddCases(gallery);
            foreach (var scale in new[] { 1f, 1.25f })
            {
                client.Panel.scale = scale;
                for (var frame = 0; frame < 6; frame++)
                    yield return null;
                LayoutTests.Save(client.Viewport, $"ui126-army-artwork-{scale:0.00}.png");
                foreach (var sample in cases)
                    Check(sample);
            }
        }

        private static List<Sample> AddCases(VisualElement gallery)
        {
            var cases = new List<Sample>();
            foreach (var figures in new[] { true, false })
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    var nation = side == Side.FreePeoples ? "rohan" : "isengard";
                    var ally = side == Side.FreePeoples ? "gondor" : "sauron";
                    cases.Add(new Sample("single", Army(side, false, (nation, UnitKind.Regular, 1)), figures, ""));
                    var mixed = Army(side, false, (nation, UnitKind.Regular, 2), (ally, UnitKind.Elite, 1));
                    mixed.Characters = 1;
                    mixed.CharacterAppearance = side == Side.FreePeoples ? FigureAppearance.Companion : FigureAppearance.Minion;
                    cases.Add(new Sample("mixed", mixed, figures, ""));
                    cases.Add(new Sample("garrison", Army(side, true, (nation, UnitKind.Regular, 10)), figures, ""));
                    foreach (var state in States.Skip(1))
                        cases.Add(new Sample("state", Army(side, false, (nation, UnitKind.Elite, 1)), figures, state.Class));
                }
            for (var index = 0; index < cases.Count; index++)
            {
                var sample = cases[index];
                // UI-192: seven larger markers a row, still inside the picture at 125%.
                sample.Button.style.left = index % 7 * 124;
                sample.Button.style.top = index / 7 * 96;
                gallery.Add(sample.Button);
            }
            return cases;
        }

        private static ArmyMarker Army(Side side, bool garrison, params (string Nation, UnitKind Kind, int Count)[] units) =>
            ArmyMarker.From("osgiliath", side, garrison, units.SelectMany(u => Enumerable.Repeat((u.Nation, u.Kind), u.Count)));

        private static void Check(Sample sample)
        {
            var button = sample.Button;
            var size = MapArmyArtwork.Size(sample.Marker.Garrison, sample.Figures);
            Assert.That(button.resolvedStyle.width, Is.EqualTo(size.x).Within(.5f), button.name);
            Assert.That(button.resolvedStyle.height, Is.EqualTo(size.y).Within(.5f), button.name);
            var plinth = button.Q("army-plinth");
            Assert.AreEqual(Picture(sample), plinth.resolvedStyle.backgroundImage.texture?.name,
                "The base picture follows the level, side, garrison and state: " + button.name);
            // UI-301: an army a click may choose keeps its picture and wears the ChoiceGlow halo behind it, larger than
            // the marker; no other state shows the halo.
            var halo = button.Q("army-choice-glow");
            var eligible = sample.State == "eligible-army";
            Assert.AreEqual(eligible ? DisplayStyle.Flex : DisplayStyle.None, halo.resolvedStyle.display, button.name);
            if (eligible)
            {
                Assert.Greater(halo.worldBound.width, button.worldBound.width, button.name);
                Assert.That(halo.resolvedStyle.borderTopWidth, Is.EqualTo(3f).Within(.5f), button.name);
                Assert.AreEqual(new Color32(255, 214, 112, 255), (Color32)halo.resolvedStyle.borderTopColor, button.name);
            }
            var images = Shown(button, "army-silhouette");
            Assert.AreEqual(sample.Figures ? Expected(sample.Marker) : 0, images.Length, button.name);
            // UI-185: each shown figure has its shadow, and neither leaves the marker.
            var shadows = Shown(button, "army-silhouette-shadow");
            Assert.AreEqual(images.Length, shadows.Length, button.name);
            foreach (var shadow in shadows)
                AssertInside(shadow, button);
            foreach (var image in images)
            {
                AssertInside(image, button);
                Assert.GreaterOrEqual(image.layout.height, 59.5f, "UI-192: a figure stands 60 points tall · " + button.name);
                Assert.IsNotNull(image.sprite, button.name);
                Click(sample, new Vector2(image.worldBound.center.x, image.worldBound.yMin + 3));
            }
            var emblem = button.Q("army-emblem-0");
            Assert.IsTrue(emblem.ClassListContains("nation-emblem-" + sample.Marker.Nations[0].Nation), button.name);
            AssertInside(emblem, button);
            var second = button.Q("army-emblem-1");
            var showsSecond = sample.Figures && sample.Marker.Nations.Count > 1;
            Assert.AreEqual(showsSecond ? DisplayStyle.Flex : DisplayStyle.None, second.resolvedStyle.display, button.name);
            var count = button.Q<Label>("army-count");
            Assert.AreEqual(sample.Marker.Total.ToString(), count.text);
            AssertInside(count, button);
            Assert.IsFalse(count.isElided, count.text);
            Assert.IsFalse(count.worldBound.Overlaps(emblem.worldBound), "Count and emblem must not cover each other: " + button.name);
            Click(sample, count.worldBound.center);
            var moved = button.Q("army-moved");
            Assert.AreEqual(sample.State == "moved" ? DisplayStyle.Flex : DisplayStyle.None, moved.resolvedStyle.display, button.name);
        }

        private static Image[] Shown(Button button, string className) => button.Query<Image>(className: className).ToList()
            .Where(image => image.resolvedStyle.display != DisplayStyle.None).ToArray();

        private static int Expected(ArmyMarker marker) => marker.Characters > 0 ? 3 : marker.Total > 1 ? 2 : 1;

        private static string Picture(Sample sample)
        {
            var suffix = States.First(s => s.Class == sample.State).Suffix;
            return (sample.Figures ? "base-" : "token-") + (sample.Marker.Garrison ? "garrison-" : "field-") +
                (sample.Marker.Side == Side.FreePeoples ? "free" : "shadow") + suffix;
        }

        private static void AssertInside(VisualElement child, Button button)
        {
            var message = button.name + " / " + child.name;
            Assert.GreaterOrEqual(child.worldBound.xMin, button.worldBound.xMin - .5f, message);
            Assert.GreaterOrEqual(child.worldBound.yMin, button.worldBound.yMin - .5f, message);
            Assert.LessOrEqual(child.worldBound.xMax, button.worldBound.xMax + .5f, message);
            Assert.LessOrEqual(child.worldBound.yMax, button.worldBound.yMax + .5f, message);
        }

        private static void Click(Sample sample, Vector2 point)
        {
            var picked = sample.Button.panel.Pick(point);
            Assert.AreSame(sample.Button, picked, "Visible artwork must belong to its clickable marker: " + sample.Button.name);
            var before = sample.Clicks;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(up);
            Assert.AreEqual(before + 1, sample.Clicks, "A click on the figure must activate exactly its marker.");
            sample.Button.RemoveFromClassList("pressed");
            if (sample.State == "pressed")
                sample.Button.AddToClassList("pressed");
        }

        private sealed class Sample
        {
            public Button Button { get; }
            public ArmyMarker Marker { get; }
            public bool Figures { get; }
            public string State { get; }
            public int Clicks { get; private set; }

            public Sample(string name, ArmyMarker marker, bool figures, string state)
            {
                Marker = marker;
                Figures = figures;
                State = state;
                Button = new Button(() => Clicks++)
                    { name = $"artwork-{name}-{marker.Side}-{(figures ? "figures" : "token")}{(state.Length > 0 ? "-" + state : "")}" };
                Button.AddToClassList("army");
                Button.AddToClassList(marker.Side == Side.FreePeoples ? "fp" : "sh");
                Button.EnableInClassList("garrison", marker.Garrison);
                if (state.Length > 0)
                    Button.AddToClassList(state);
                var artwork = new MapArmyArtwork(Button);
                artwork.Apply(marker);
                artwork.Layout(figures);
            }
        }
    }
}
