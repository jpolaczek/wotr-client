using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // UI-97: short die descriptions and a readable glyph for every face.
    public sealed class DieDescriptionsTests
    {
        [Test]
        public void EveryFaceHasAShortDescriptionWithoutParagraphsOrRulebookReferences()
        {
            foreach (DieFace face in Enum.GetValues(typeof(DieFace)))
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    var die = new DieView { Id = "d", Owner = side, Face = face, Location = DieLocation.Available };
                    var text = DieDescriptions.Tooltip(die);
                    var lines = text.Split('\n');
                    Assert.AreEqual(DieDescriptions.Face(face), lines[0]);
                    Assert.Greater(lines.Length, 1, face + " lists what it can do");
                    Assert.LessOrEqual(lines.Length, 6, face + " stays short");
                    foreach (var line in lines)
                        Assert.LessOrEqual(line.Length, 52, line);
                    foreach (var reference in new[] { "BASE", "LORDS", "KINGS", "p.", "pp." })
                        StringAssert.DoesNotContain(reference, text);
                }
        }

        [Test]
        public void SidesDifferWhereTheRulesDoAndStatesStayOnOneLine()
        {
            var shadowMuster = DieDescriptions.Options(DieFace.Muster, Side.Shadow);
            CollectionAssert.Contains(shadowMuster, "Bring a Minion into play");
            CollectionAssert.DoesNotContain(DieDescriptions.Options(DieFace.Muster, Side.FreePeoples), "Bring a Minion into play");
            CollectionAssert.Contains(DieDescriptions.Options(DieFace.Character, Side.FreePeoples), "Move or hide the Fellowship");
            var spent = new DieView { Id = "s", Owner = Side.Shadow, Face = DieFace.Army, Location = DieLocation.Spent };
            Assert.AreEqual("Army · Spent", DieDescriptions.Summary(spent));
            var hunt = new DieView { Id = "h", Owner = Side.Shadow, Face = DieFace.Eye, Location = DieLocation.Hunt };
            Assert.AreEqual("Eye\nIn the Hunt Box", DieDescriptions.Tooltip(hunt), "A die out of play lists no options.");
            var unrolled = new DieView { Id = "u", Owner = Side.FreePeoples, Location = DieLocation.Unrolled };
            Assert.AreEqual("Not rolled yet", DieDescriptions.Tooltip(unrolled));
            var now = DieDescriptions.Tooltip(new DieView { Id = "m", Owner = Side.FreePeoples, Face = DieFace.Muster }, new[] { "Recruit", "Politics" });
            StringAssert.EndsWith("Now · Recruit · Politics", now);
        }

        [Test]
        public void EveryRolledFaceHasItsGlyphTexture()
        {
            foreach (DieFace face in Enum.GetValues(typeof(DieFace)))
            {
                var icon = new DieFaceIcon(face);
                var key = DieFaceIcon.Key(face);
                Assert.IsTrue(icon.ClassListContains("die-face-" + key));
                Assert.IsNotNull(Resources.Load<Texture2D>("Wotr/Art/UI/dice/faces/" + key), key);
            }
            Assert.IsTrue(new DieFaceIcon(null).ClassListContains("die-face-unrolled"));
        }
    }
}
