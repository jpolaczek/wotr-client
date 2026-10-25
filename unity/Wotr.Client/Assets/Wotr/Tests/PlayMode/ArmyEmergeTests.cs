using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-334 (owner 2026-10-25): an army that appears in a region flares, then emerges from transparent and soft to clear,
    // and only then shows its number while the flare fades; a growing army keeps its figures and steps its number at the
    // end. The first view, a quiet view and Skip animations show the state at once.
    public sealed class ArmyEmergeTests
    {
        [UnityTest]
        public IEnumerator ARecruitedArmyFlaresEmergesThenShowsItsNumber()
        {
            using var client = new MountedMatchClient("http://localhost", "army-emerge");
            var view = new BoardFixture().View;
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsNull(client.Board.Map.Q<Button>(className: MapArmyArtwork.FlaringClass), "The first view only sets the state.");
            var next = Recruit(view, "lossarnach", 2);
            client.Apply(next);
            yield return null;
            yield return null;
            var marker = client.Board.Map.Q<Button>("army-lossarnach-FreePeoples");
            Assert.IsNotNull(marker);
            Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.FlaringClass), "It flares first.");
            var artwork = marker.Q("army-artwork");
            Assert.Less(artwork.resolvedStyle.opacity, .5f, "Its figures are not there yet.");
            yield return new WaitForSecondsRealtime((MapArmyArtwork.FlareMilliseconds + MapArmyArtwork.EmergeMilliseconds / 2) / 1000f);
            var halfway = artwork.resolvedStyle.opacity;
            Assert.Greater(halfway, .05f, "They emerge…");
            Assert.Less(halfway, .98f, "…gradually.");
            LayoutTests.Save(client.Viewport, "ui334-emerging.png");
            yield return new WaitForSecondsRealtime(MapArmyArtwork.EmergeMilliseconds / 1000f);
            Assert.IsFalse(marker.ClassListContains(MapArmyArtwork.FlaringClass), "The flare fades.");
            Assert.AreEqual(1f, artwork.resolvedStyle.opacity, .01f, "Fully clear.");
            Assert.AreEqual("2", marker.Q<Label>("army-count").text, "Then its number.");
        }

        [UnityTest]
        public IEnumerator AGrowingArmyKeepsItsFiguresAndStepsItsNumberAtTheEnd()
        {
            using var client = new MountedMatchClient("http://localhost", "army-grow");
            var view = new BoardFixture().View;
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.3f);
            // The first Free Peoples field army of the setup.
            var marker = client.Board.Map.Query<Button>(className: "army").ToList()
                .First(button => button.name.EndsWith("-FreePeoples"));
            var region = marker.name.Substring("army-".Length, marker.name.Length - "army-".Length - "-FreePeoples".Length);
            var before = marker.Q<Label>("army-count").text;
            client.Apply(Recruit(view, region, 1));
            yield return null;
            yield return null;
            Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.FlaringClass));
            Assert.IsFalse(marker.ClassListContains(MapArmyArtwork.HiddenClass), "Its figures stay.");
            Assert.AreEqual(before, marker.Q<Label>("army-count").text, "The old number while it flares.");
            yield return new WaitForSecondsRealtime((MapArmyArtwork.FlareMilliseconds + MapArmyArtwork.EmergeMilliseconds) / 1000f + .1f);
            Assert.AreEqual((int.Parse(before) + 1).ToString(), marker.Q<Label>("army-count").text, "The new one at the end.");
        }

        [UnityTest]
        public IEnumerator AQuietViewAndSkipAnimationsShowTheArmyAtOnce()
        {
            using (var client = new MountedMatchClient("http://localhost", "army-quiet"))
            {
                var view = new BoardFixture().View;
                client.Apply(view);
                yield return new WaitForSecondsRealtime(.3f);
                client.Board.ApplyQuietly(Recruit(view, "lossarnach", 1));
                yield return null;
                yield return null;
                Assert.IsFalse(client.Board.Map.Q<Button>("army-lossarnach-FreePeoples").ClassListContains(MapArmyArtwork.FlaringClass),
                    "A quiet view shows the state.");
            }
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            stage.Board.Apply(Recruit(stage.Latest(), "lossarnach", 1));
            yield return null;
            yield return null;
            Assert.IsFalse(stage.Board.Map.Q<Button>("army-lossarnach-FreePeoples").ClassListContains(MapArmyArtwork.FlaringClass),
                "Skip animations shows it at once.");
        }

        // Gondor Regulars from the reserve, the next revision.
        private static PlayerView Recruit(PlayerView view, string region, int count)
        {
            var next = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerView>(Newtonsoft.Json.JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            foreach (var unit in next.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Take(count))
                unit.Location = region;
            return next;
        }
    }
}
