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
    // UI-126: tokens and figures on a base, with emblems, counts and the picture of every state. UI-400: the figures are
    // miniatures — a painted stand with a contact shadow, the state as a rim, the feet on one line, and only the miniature
    // takes a click. UI-406: the token is a puck on the table too — its side's puck (a garrison's walled) over a contact
    // shadow, the state as a rim drawn round it.
    public sealed class MapArmyArtworkTests
    {
        private static readonly (string Class, string Rim)[] States =
        {
            ("", ""), ("moved", "rim-moved"), ("eligible-army", "rim-eligible"), ("ability-owner", "rim-ability"),
            ("selected-force", "rim-selected"), ("pressed", "rim-pressed")
        };

        [UnityTest]
        public IEnumerator TokensAndFiguresKeepTheirPicturesEmblemsAndCountsInsideTheClickableMarker()
        {
            using var client = new MountedMatchClient("http://localhost", "army-artwork-layout");
            var gallery = new VisualElement { name = "army-artwork-gallery" };
            gallery.style.position = Position.Absolute;
            gallery.style.left = 120;
            gallery.style.top = 40;
            gallery.style.width = 900;
            gallery.style.height = 520;
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
                    // UI-402: an army's figures are its units; its characters stand beside it on their own.
                    var mixed = Army(side, false, (nation, UnitKind.Regular, 2), (ally, UnitKind.Elite, 1), (nation, UnitKind.Leader, 1));
                    cases.Add(new Sample("mixed", mixed, figures, ""));
                    cases.Add(new Sample("garrison", Army(side, true, (nation, UnitKind.Regular, 10)), figures, ""));
                    foreach (var state in States.Skip(1))
                        cases.Add(new Sample("state", Army(side, false, (nation, UnitKind.Elite, 1)), figures, state.Class));
                    // UI-510: too many to show all — one figure of each kind with its own count (figures only; a token shows
                    // its total).
                    if (figures)
                        cases.Add(new Sample("crowd", side == Side.FreePeoples
                            ? Army(side, false, (nation, UnitKind.Regular, 3), (ally, UnitKind.Elite, 2), (nation, UnitKind.Leader, 1))
                            : Army(side, false, (nation, UnitKind.Regular, 4), (ally, UnitKind.Elite, 2), (ally, UnitKind.Nazgul, 2)),
                            figures, ""));
                }
            for (var index = 0; index < cases.Count; index++)
            {
                var sample = cases[index];
                // UI-192: seven larger markers a row, still inside the picture at 125%.
                sample.Button.style.left = index % 7 * 124;
                sample.Button.style.top = index / 7 * 100;
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
            if (sample.Figures)
                CheckMiniature(sample);
            else
                CheckToken(sample);
            // UI-301: an army a click may choose keeps its picture and wears the ChoiceGlow halo behind it, larger than
            // the marker; UI-374: so does the army of a figure whose ability a click uses; no other state shows the halo.
            var halo = button.Q("army-choice-glow");
            var eligible = sample.State is "eligible-army" or "ability-owner";
            Assert.AreEqual(eligible ? DisplayStyle.Flex : DisplayStyle.None, halo.resolvedStyle.display, button.name);
            if (eligible)
            {
                Assert.Greater(halo.worldBound.width, button.worldBound.width, button.name);
                Assert.That(halo.resolvedStyle.borderTopWidth, Is.EqualTo(3f).Within(.5f), button.name);
                Assert.AreEqual(new Color32(255, 214, 112, 255), (Color32)halo.resolvedStyle.borderTopColor, button.name);
            }
            var images = Shown(button, "army-silhouette");
            Assert.AreEqual(sample.Figures ? Expected(sample.Marker) : 0, images.Length, button.name);
            CheckFigureCounts(sample, images.Length);
            foreach (var image in images)
            {
                AssertInside(image, button);
                Assert.GreaterOrEqual(image.layout.height, 48f, "UI-400: a figure stands about 54 points tall · " + button.name);
                Assert.IsNotNull(image.sprite, button.name);
                Click(sample, image.worldBound.center);
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

        // UI-400: the stand of the side and kind, its contact shadow under it, the state's rim, every figure's feet on the
        // stand's top face (the nearer ones on one line), and the empty space above the figures passing the press on.
        private static void CheckMiniature(Sample sample)
        {
            var button = sample.Button;
            var stand = button.Q("army-stand");
            Assert.AreEqual(Stand(sample), stand.resolvedStyle.backgroundImage.texture?.name, button.name);
            Assert.AreEqual(DisplayStyle.None, button.Q("army-puck").resolvedStyle.display, button.name);
            Assert.AreEqual(DisplayStyle.None, button.Q("army-token-rim").resolvedStyle.display, button.name);
            var shadow = button.Q("army-contact-shadow");
            Assert.AreEqual(DisplayStyle.Flex, shadow.resolvedStyle.display, button.name);
            AssertInside(shadow, button);
            Assert.GreaterOrEqual(shadow.worldBound.yMax, stand.worldBound.yMax - .5f, "The shadow lies under the stand: " + button.name);
            var rim = States.First(s => s.Class == sample.State).Rim;
            var rimElement = button.Q("army-rim");
            Assert.AreEqual(rim.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None, rimElement.resolvedStyle.display, button.name);
            if (rim.Length > 0)
                Assert.AreEqual("rim-field-" + rim, rimElement.resolvedStyle.backgroundImage.texture?.name, button.name);
            var images = Shown(button, "army-silhouette");
            var front = images.Max(image => image.worldBound.yMax);
            foreach (var image in images)
            {
                Assert.That(image.worldBound.yMax, Is.InRange(stand.worldBound.yMin, stand.worldBound.yMax),
                    "Feet on the stand: " + button.name);
                Assert.That(image.worldBound.yMax, Is.InRange(front - 9f, front + .5f), "One feet line, in depth: " + button.name);
            }
            var above = new Vector2(button.worldBound.xMin + 3, button.worldBound.yMin + 3);
            Assert.AreNotSame(button, button.panel.Pick(above), "The headroom above the figures takes no press: " + button.name);
        }

        // UI-406: the puck of the side (walled for a garrison), its contact shadow below it and to the right, and the
        // state's rim round it instead of a picture per state; the miniature's stand and rim stay hidden.
        private static void CheckToken(Sample sample)
        {
            var button = sample.Button;
            var puck = button.Q("army-puck");
            Assert.AreEqual(Puck(sample), puck.resolvedStyle.backgroundImage.texture?.name, button.name);
            Assert.That(puck.worldBound.width, Is.EqualTo(button.worldBound.width).Within(.5f), "The puck spans the token: " + button.name);
            var shadow = button.Q("army-contact-shadow");
            Assert.AreEqual(DisplayStyle.Flex, shadow.resolvedStyle.display, button.name);
            Assert.Greater(shadow.worldBound.yMax, puck.worldBound.yMax, "The shadow lies under the puck: " + button.name);
            Assert.Greater(shadow.worldBound.center.x, puck.worldBound.center.x, "Light from the upper left: " + button.name);
            var rim = States.First(s => s.Class == sample.State).Rim;
            var rimElement = button.Q("army-token-rim");
            Assert.AreEqual(rim.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None, rimElement.resolvedStyle.display, button.name);
            if (rim.Length > 0)
            {
                Assert.AreEqual("rim-token-" + rim, rimElement.resolvedStyle.backgroundImage.texture?.name, button.name);
                Assert.IsTrue(rimElement.worldBound.Contains(puck.worldBound.center), "The rim lies round the puck: " + button.name);
            }
            Assert.AreEqual(DisplayStyle.None, button.Q("army-stand").resolvedStyle.display, button.name);
            Assert.AreEqual(DisplayStyle.None, button.Q("army-rim").resolvedStyle.display, button.name);
        }

        private static string Puck(Sample sample) => "token-puck-" + (sample.Marker.Garrison ? "garrison-" : "") +
            (sample.Marker.Side == Side.FreePeoples ? "free" : "shadow");

        private static string Stand(Sample sample) => "stand-" + (sample.Marker.Garrison ? "garrison-" : "field-") +
            (sample.Marker.Side == Side.FreePeoples ? "free" : "shadow");

        private static Image[] Shown(Button button, string className) => button.Query<Image>(className: className).ToList()
            .Where(image => image.resolvedStyle.display != DisplayStyle.None).ToArray();

        // UI-510: up to three units every one stands; more stand one figure of each kind (Elite, Regular, Nazgûl, Leader).
        private static int Expected(ArmyMarker marker) => marker.Total <= MapArmyArtwork.AllShown ? marker.Total :
            new[] { marker.Elite, marker.Regular, marker.Nazgul, marker.Leaders - marker.Nazgul }.Count(count => count > 0);

        // UI-510: a figure standing for several of its kind shows their number at its feet, inside the marker and clear of
        // the total; the figures' numbers add up to the army. A token and a small army show none.
        private static void CheckFigureCounts(Sample sample, int figures)
        {
            var button = sample.Button;
            var counts = button.Query<Label>(className: MapArmyArtwork.FigureCountClass).ToList()
                .Where(label => label.resolvedStyle.display != DisplayStyle.None).ToList();
            if (!sample.Figures || sample.Marker.Total <= MapArmyArtwork.AllShown)
            {
                Assert.IsEmpty(counts, "No figure counts: " + button.name);
                return;
            }
            Assert.IsNotEmpty(counts, "A big army's figures carry their counts: " + button.name);
            var total = button.Q<Label>("army-count");
            var sum = figures - counts.Count;
            foreach (var count in counts)
            {
                StringAssert.StartsWith("×", count.text, button.name);
                sum += int.Parse(count.text.Substring(1));
                AssertInside(count, button);
                Assert.IsFalse(count.isElided, count.text);
                Assert.IsFalse(count.worldBound.Overlaps(total.worldBound), "A figure's count clear of the total: " + button.name);
            }
            Assert.AreEqual(sample.Marker.Total, sum, "The figures' counts add up to the army: " + button.name);
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
                Button = new MiniatureButton(() => Clicks++)
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
