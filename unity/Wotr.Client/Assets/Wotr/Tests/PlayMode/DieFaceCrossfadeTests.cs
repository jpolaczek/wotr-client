using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-322 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible"): a die whose face changes where
    // it stands crossfades from its old face to the new one; under Skip animations the new face shows at once.
    public sealed class DieFaceCrossfadeTests
    {
        [UnityTest]
        public IEnumerator AChangedFaceFadesInOverTheOldOneAndTheOldOneGoes()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            stage.Board.Apply(Turned(stage.Latest()));
            var die = stage.Board.OwnDice.Q<Button>("army");
            var faces = Faces(die);
            Assert.AreEqual(2, faces.Length, "The old face stays while the new one comes.");
            Assert.IsTrue(faces[0].ClassListContains("die-face-army"), "The old face is behind…");
            Assert.IsTrue(faces[1].ClassListContains("die-face-character"), "…the new one.");
            Assert.IsTrue(faces[1].ClassListContains(Appearance.AppearingClass), "The new face fades in.");
            Assert.AreEqual(PickingMode.Ignore, faces[0].pickingMode);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.Less(faces[1].resolvedStyle.opacity, .99f, "Midway it is not yet clear…");
            Assert.Less(faces[0].resolvedStyle.opacity, .99f, "…and the old face is fading.");
            LayoutTests.Save(stage.Texture, "ui322-die-face.png");
            yield return new WaitForSecondsRealtime((Appearance.CloseMilliseconds + 150) / 1000f);
            faces = Faces(die);
            Assert.AreEqual(1, faces.Length, "Only the new face remains.");
            Assert.IsTrue(faces[0].ClassListContains("die-face-character"));
            Assert.AreEqual(1f, faces[0].resolvedStyle.opacity, .01f);
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsAndOnAFirstShowTheFaceChangesAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            Assert.AreEqual(1, Faces(stage.Board.OwnDice.Q<Button>("army")).Length, "A die first shown has one face.");
            stage.Board.Apply(Turned(stage.Latest()));
            var faces = Faces(stage.Board.OwnDice.Q<Button>("army"));
            Assert.AreEqual(1, faces.Length, "Skip animations: no crossfade.");
            Assert.IsTrue(faces[0].ClassListContains("die-face-character"));
        }

        private static VisualElement[] Faces(VisualElement die) => die.Query(className: DieFaceIcon.ClassName).ToList().ToArray();

        // The Free Peoples' Army die turned to a Character (as the Elven Rings turn a die).
        private static PlayerView Turned(PlayerView view)
        {
            view.Dice.Single(d => d.Id == "army").Face = DieFace.Character;
            return view;
        }
    }
}
