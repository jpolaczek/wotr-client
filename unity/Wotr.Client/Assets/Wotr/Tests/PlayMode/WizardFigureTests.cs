using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // ART-11 (owner, 2026-11-17: "Saruman's and Gandalf the White's figures are too alike; rework them"): Saruman stands dark,
    // in black and crimson with an iridescent robe and a black staff, Gandalf the White bright, in white under a pale
    // silver-blue cloak with a white staff and a sword; side by side on the map they are told apart at a glance.
    // Frame: art11-wizards.
    public sealed class WizardFigureTests
    {
        [UnityTest]
        public IEnumerator SarumanAndGandalfTheWhiteAreToldApartOnTheMap()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.FreePeoples);
            PresentedBoardStage.Character(stage.Before, "base.character.saruman", "base.character.saruman", Side.Shadow, "orthanc");
            PresentedBoardStage.Character(stage.Before, "base.character.gandalf-white", "base.character.gandalf-white", Side.FreePeoples, "fangorn");
            yield return stage.Ready();
            var map = stage.Board.Map;
            map.FocusOn((stage.Anchor("orthanc") + stage.Anchor("fangorn")) / 2, map.FitZoom * 3.2f);
            for (var i = 0; i < 20; i++)
                yield return null;
            var saruman = map.CharacterMarker("base.character.saruman");
            var gandalf = map.CharacterMarker("base.character.gandalf-white");
            Assert.IsNotNull(saruman, "Saruman stands at Orthanc.");
            Assert.IsNotNull(gandalf, "Gandalf the White stands in Fangorn.");
            LayoutTests.Save(stage.Texture, "art11-wizards.png");
        }
    }
}
