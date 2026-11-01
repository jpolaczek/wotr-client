using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-436 (owner: "make sure every place any character can be has its picture"): every region and every place off the
    // map has its picture in Art/Places (scripts/export_place_pictures.py), and every state of every character and
    // sovereign leads to one of them.
    public sealed class PlacePictureTests
    {
        private static readonly Vector2Int Size = new Vector2Int(256, 160);

        [Test]
        public void EveryRegionHasItsPicture()
        {
            var regions = BoardPresentation.Load().Regions;
            Assert.AreEqual(105, regions.Count);
            var missing = regions.Keys.Where(id => PlacePicture.Picture(id) == null).ToList();
            CollectionAssert.IsEmpty(missing, "Regions without a picture: " + string.Join(", ", missing));
            foreach (var id in regions.Keys)
                AssertSize(id);
        }

        [Test]
        public void EveryPlaceOffTheMapHasItsPicture()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                "fellowship", "not-in-play-free-peoples", "not-in-play-shadow", "removed-free-peoples", "removed-shadow", "removed-sovereign"
            }, PlacePresentation.SpecialPlaces);
            foreach (var key in PlacePresentation.SpecialPlaces)
            {
                Assert.IsNotNull(PlacePicture.Picture(key), key);
                AssertSize(key);
            }
        }

        // Every lifecycle of either side's characters and every status of a sovereign, on and off the map.
        [Test]
        public void EveryStateOfEveryFigureLeadsToAPicture()
        {
            var regions = BoardPresentation.Load().Regions;
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                foreach (CharacterRosterLifecycle lifecycle in Enum.GetValues(typeof(CharacterRosterLifecycle)))
                    foreach (var region in new[] { "", "minas-tirith" })
                    {
                        var row = new CharacterRosterView { Side = side, Lifecycle = lifecycle, RegionId = region };
                        var place = PlacePresentation.For(row, regions);
                        var label = side + " · " + lifecycle + " · " + region;
                        Assert.IsNotNull(PlacePicture.Picture(place.Key), label);
                        Assert.IsNotEmpty(place.Caption, label);
                        Assert.IsNotEmpty(place.Meaning, label);
                        Assert.AreEqual(lifecycle == CharacterRosterLifecycle.OnBoard && region.Length > 0, place.OnMap, label);
                    }
            foreach (SovereignStatus status in Enum.GetValues(typeof(SovereignStatus)))
                foreach (var region in new[] { "", "erebor" })
                {
                    var king = new SovereignView { Status = status, RegionId = region };
                    var place = PlacePresentation.For(king, regions);
                    Assert.IsNotNull(PlacePicture.Picture(place.Key), status + " · " + region);
                    Assert.AreEqual(status != SovereignStatus.Removed && region.Length > 0, place.OnMap, status + " · " + region);
                }
        }

        [Test]
        public void APlaceIsNamedAndSaysWhatBeingThereMeans()
        {
            var regions = BoardPresentation.Load().Regions;
            var gimli = new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.OnBoard, RegionId = "erebor", Garrison = true };
            var place = PlacePresentation.For(gimli, regions);
            Assert.AreEqual(("erebor", "Erebor", "erebor"), (place.Key, place.Caption, place.RegionId));
            Assert.AreEqual("On the board · In the stronghold's garrison", place.Meaning);
            Assert.AreEqual("Erebor\nClick to show this region on the map", place.Tooltip);
            var strider = PlacePresentation.For(new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.InFellowship }, regions);
            Assert.AreEqual(("fellowship", "The Fellowship", "Travels with the Fellowship"), (strider.Key, strider.Caption, strider.Meaning));
            Assert.AreEqual("The Fellowship\nTravels with the Fellowship\nNot on the map", strider.Tooltip);
            var saruman = PlacePresentation.For(new CharacterRosterView { Side = Side.Shadow, Lifecycle = CharacterRosterLifecycle.NotEntered }, regions);
            Assert.AreEqual(("not-in-play-shadow", "Not in play", "Not entered yet"), (saruman.Key, saruman.Caption, saruman.Meaning));
            var removed = PlacePresentation.For(new SovereignView { Status = SovereignStatus.Removed, RegionId = "erebor" }, regions);
            Assert.AreEqual(("removed-sovereign", "Out of play"), (removed.Key, removed.Caption));
        }

        private static void AssertSize(string key)
        {
            var picture = PlacePicture.Picture(key);
            Assert.AreEqual(Size, new Vector2Int(picture.width, picture.height), key);
        }
    }

    // UI-436: a window's actions in words instead of "Unavailable" and "Limits".
    public sealed class AvailabilityLinesTests
    {
        [Test]
        public void AnActionReadsWhatItTakesAndWhatItStillNeeds()
        {
            var view = new PlayerView();
            view.Availability.Add(Awaken(met: false));
            var line = AvailabilityLines.For(view, "king").Single();
            Assert.AreEqual("Awaken · Muster or Ruler die", line.Title);
            Assert.AreEqual("His nation must be active", line.State);
            Assert.IsFalse(line.Ready);
            StringAssert.Contains("○ Dáin Ironfoot's nation is active.", line.Tooltip);
            StringAssert.Contains("KINGS p.10", line.Tooltip);
            view.Availability[0] = Awaken(met: true);
            line = AvailabilityLines.For(view, "king").Single();
            Assert.AreEqual(AvailabilityLines.Ready, line.State);
            Assert.IsTrue(line.Ready);
        }

        [Test]
        public void AnActionOfAnotherStateOrOutsideItsMomentIsLeftOut()
        {
            var view = new PlayerView();
            var awaken = Awaken(met: true);
            awaken.Conditions[0].Met = false;
            view.Availability.Add(awaken);
            view.Availability.Add(new ActionAvailabilityView { SubjectId = "king", ActionId = "cleanse", Title = "Return one Corruption tile" });
            view.Availability.Add(new ActionAvailabilityView { SubjectId = "other", ActionId = "attempt-corruption", Title = "Attempt corruption" });
            Assert.IsEmpty(AvailabilityLines.For(view, "king"), "Awaken belongs to an Initial king; the cleanse waits for its moment.");
            view.Availability[1].WindowOpen = true;
            Assert.AreEqual("Return one Corruption tile · no die", AvailabilityLines.For(view, "king").Single().Title);
        }

        [Test]
        public void ClosedTimingAndUnknownConditionsReadAsTheServerSaysThem()
        {
            var timing = new ActionAvailabilityView { Title = "Attempt corruption", ActionId = "attempt-corruption", Timing = "Instead of your normal action." };
            Assert.AreEqual("Instead of your normal action", AvailabilityLines.Line(timing).State);
            var unknown = new ActionAvailabilityView
            {
                Title = "Enter", Conditions = { new AvailabilityConditionView { Code = "minion", Description = "A Minion is in play.", Met = false } }
            };
            Assert.AreEqual("A Minion is in play", AvailabilityLines.Line(unknown).State);
            Assert.AreEqual("Enter", AvailabilityLines.Line(unknown).Title);
        }

        private static ActionAvailabilityView Awaken(bool met) => new ActionAvailabilityView
        {
            SubjectId = "king", ActionId = "awaken", Title = "Awaken", Source = "KINGS p.10", CanAct = met, WindowOpen = met,
            Cost = "One Muster result (including Army/Muster or Will of the West), or any available Ruler result.",
            Conditions =
            {
                new AvailabilityConditionView { Category = "Condition", Code = "initial", Description = "The sovereign is in its initial state.", Met = true },
                new AvailabilityConditionView { Category = "Condition", Code = "nation", Description = "Dáin Ironfoot's nation is active.", Met = met }
            }
        };
    }

    // UI-436: every ruler's and character's window at one place beside the Characters panel's slot, level with its tab.
    public sealed class CharacterWindowPlaceTests
    {
        private static readonly Rect Viewport = new Rect(0, 0, 1092, 520);
        private static readonly Rect Slot = new Rect(94, 58, 462, 0);
        private static readonly Rect Tab = new Rect(0, 214, 94, 66);

        [Test]
        public void TheWindowStandsBesideTheSlotLevelWithTheTab()
        {
            var point = CharacterWindowPlace.Point(Viewport, Slot, Tab, new Vector2(380, 264));
            Assert.AreEqual(new Vector2(Slot.xMax + CharacterWindowPlace.Gap, Tab.yMin), point);
        }

        [Test]
        public void ATallWindowRisesOnlyAsFarAsItMustAndNeverLeavesTheBoard()
        {
            var point = CharacterWindowPlace.Point(Viewport, Slot, Tab, new Vector2(380, 400));
            Assert.AreEqual(Viewport.yMax - 400 - CharacterWindowPlace.BottomMargin, point.y);
            point = CharacterWindowPlace.Point(Viewport, Slot, Tab, new Vector2(380, 700));
            Assert.AreEqual(Viewport.yMin, point.y);
            point = CharacterWindowPlace.Point(new Rect(0, 0, 700, 520), Slot, Tab, new Vector2(380, 264));
            Assert.AreEqual(700 - 380 - CharacterWindowPlace.Gap, point.x, "A narrow board keeps the window whole.");
        }
    }
}
