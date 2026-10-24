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
    // UI-309 on a mounted board: a character's entry, transformation, removal and separation, a travelling group and a
    // reaction's source, as the opponent's recorded facts present them. The viewer names the character and its place from
    // the portrait moment or the group's identities; a new version replaces its figure once; no control is added. Frames
    // go to .artifacts/screenshots (ui309-*).
    public sealed class OpponentCharacterPresentationTests
    {
        private const string WitchKing = "base.character.witch-king";
        private const string Grey = "base.character.gandalf-grey";
        private const string White = "base.character.gandalf-white";
        private const string Strider = "base.character.strider";
        private const string Aragorn = "base.character.aragorn";
        private const string Gimli = "base.character.gimli";
        private const string Legolas = "base.character.legolas";
        private const string Boromir = "base.character.boromir";

        [UnityTest]
        public IEnumerator TheWitchKingsEntryShowsHisPortraitAndNameBeforeHisFigureLands()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Entry(stage);
            yield return stage.Until(() => stage.Moments() == "The Witch-king");
            Assert.AreEqual(0, stage.Characters("minas-morgul", Side.Shadow), "The moment comes before the figure.");
            var moment = stage.Board.Map.Q("observed-portraits");
            Assert.AreEqual(PickingMode.Ignore, moment.pickingMode);
            yield return new WaitForSecondsRealtime(.3f);
            var safe = stage.Presenters.Camera.SafeArea();
            var bounds = new Rect(stage.Board.Map.WorldToLocal(moment.worldBound.position), moment.worldBound.size);
            Assert.IsTrue(safe.Contains(bounds.min) && safe.Contains(bounds.max), "The moment stays in the safe map area.");
            LayoutTests.Save(stage.Texture, "ui309-entry-moment.png");
            yield return stage.Until(() => stage.Characters("minas-morgul", Side.Shadow) == 1);
            Assert.AreEqual("The Witch-king", stage.Moments(), "The moment stays beside the landing figure.");
            Assert.IsNotNull(stage.Board.Map.Q("observed-delta"));
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual("", stage.Moments(), "The moment leaves with the presentation.");
        }

        [UnityTest]
        public IEnumerator GandalfTheWhiteReplacesTheGreyOnceAtFangorn()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            yield return stage.Ready();
            var latest = stage.Latest();
            var white = PresentedBoardStage.Character(latest, Grey, White, Side.FreePeoples, "fangorn");
            var grey = new ActionParticipant { Id = Grey, Side = Side.FreePeoples, DefinitionId = Grey };
            var step = stage.Paid(stage.Step(1), DieFace.WillOfTheWest);
            step.Facts.Figures.Add(new ActionFigureChange { Before = grey, After = PresentedBoardStage.Figure(white), From = "casualties", To = "fangorn" });
            stage.Play(latest, "grey-havens", step);
            yield return stage.Until(() => stage.Moments() == "Gandalf the White");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-white-moment.png");
            yield return stage.Until(() => stage.Characters("fangorn", Side.FreePeoples) == 1);
            Assert.AreEqual(1, stage.Board.Map.Query("observed-portraits").ToList().Count, "One identity, never two.");
            Assert.AreEqual(1, stage.Board.Map.Q("observed-delta").Query(className: "observed-delta-entry").ToList().Count, "One replacement.");
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "One cost.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(White, stage.Board.View.Characters.Single().DefinitionId);
        }

        [UnityTest]
        public IEnumerator AragornReplacesStriderInPlaceWithoutASecondFigure()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            var strider = PresentedBoardStage.Character(stage.Before, Strider, Strider, Side.FreePeoples, "minas-tirith");
            yield return stage.Ready();
            var latest = stage.Latest();
            var aragorn = PresentedBoardStage.Character(latest, Strider, Aragorn, Side.FreePeoples, "minas-tirith");
            var step = stage.Paid(stage.Step(1), DieFace.WillOfTheWest);
            step.Facts.Figures.Add(new ActionFigureChange
            {
                Before = PresentedBoardStage.Figure(strider), After = PresentedBoardStage.Figure(aragorn), From = "minas-tirith", To = "minas-tirith"
            });
            stage.Play(latest, "grey-havens", step);
            yield return stage.Until(() => stage.Moments() == "Aragorn");
            while (stage.Playback.Holding)
            {
                Assert.AreEqual(1, stage.Characters("minas-tirith", Side.FreePeoples), "The new version replaces the figure.");
                Assert.AreEqual(0, stage.Board.Map.Figures.Moves.Count(move => !move.IsArrival), "Nothing travels.");
                yield return null;
            }
            yield return stage.Released();
            Assert.AreEqual(Aragorn, stage.Board.View.Characters.Single().DefinitionId);
            Assert.AreEqual(1, stage.Characters("minas-tirith", Side.FreePeoples));
        }

        [UnityTest]
        public IEnumerator GimliAndLegolasTravelTogetherAndAreNamedWhereTheyLand()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            var gimli = PresentedBoardStage.Character(stage.Before, Gimli, Gimli, Side.FreePeoples, "lorien");
            var legolas = PresentedBoardStage.Character(stage.Before, Legolas, Legolas, Side.FreePeoples, "lorien");
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Character(latest, Gimli, Gimli, Side.FreePeoples, "dimrill-dale");
            PresentedBoardStage.Character(latest, Legolas, Legolas, Side.FreePeoples, "dimrill-dale");
            var step = stage.Paid(stage.Step(1), DieFace.Character);
            step.Movements.Add(Travel("lorien", "dimrill-dale", PresentedBoardStage.Figure(gimli), PresentedBoardStage.Figure(legolas)));
            stage.Play(latest, "grey-havens", step);
            yield return stage.Until(() => stage.Flying("dimrill-dale"));
            Assert.AreEqual(2, stage.Board.Map.Figures.Moves.Count(move => move.From.Region == "lorien" && move.To.Region == "dimrill-dale"));
            yield return stage.Until(() => stage.Identities() == "Gimli,Legolas");
            Assert.AreEqual(2, stage.Board.Map.Q("observed-identities").Query(className: "observed-medallion-free").ToList().Count,
                "Each Companion keeps its portrait.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-companions.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(2, stage.Characters("dimrill-dale", Side.FreePeoples));
        }

        [UnityTest]
        public IEnumerator TheWitchKingMarchesWithAnArmyAndTheNazgulTravelAlone()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var king = PresentedBoardStage.Character(stage.Before, WitchKing, WitchKing, Side.Shadow, "minas-morgul");
            var orcs = stage.Put("sauron", UnitKind.Regular, "minas-morgul", 2);
            var nazgul = stage.Put("sauron", UnitKind.Nazgul, "minas-morgul", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Character(latest, WitchKing, WitchKing, Side.Shadow, "north-ithilien");
            PresentedBoardStage.Place(latest, orcs.Concat(nazgul.Take(1)), "north-ithilien");
            PresentedBoardStage.Place(latest, nazgul.Skip(1), "dagorlad");
            var march = stage.Paid(stage.Step(1), DieFace.Army);
            march.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.March, From = "minas-morgul", To = "north-ithilien",
                Participants = orcs.Concat(nazgul.Take(1)).Select(stage.Unit).Append(PresentedBoardStage.Figure(king)).ToList()
            });
            var flight = stage.Step(2);
            flight.Movements.Add(Travel("minas-morgul", "dagorlad", nazgul.Skip(1).Select(stage.Unit).ToArray()));
            stage.Play(latest, "between:minas-morgul:dagorlad", march, flight);
            yield return stage.Until(() => stage.Identities() == "×2,×1,Witch-king");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-army-with-witch-king.png");
            yield return stage.Until(() => stage.Identities() == "×2");
            Assert.AreEqual(2, stage.Board.Map.Figures.Moves.Count(move => move.To.Region == "dagorlad"), "Only the two Nazgûl fly.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator ACompanionLeavesTheFellowshipFromItsKnownPosition()
        {
            using var stage = Separation(0, out var latest);
            yield return stage.Ready();
            Play(stage, latest);
            yield return stage.Until(() => stage.Flying("fords-of-bruinen"));
            var move = stage.Board.Map.Figures.Moves.Single(m => m.To.Key == "character:" + Gimli);
            Assert.AreEqual("rivendell", move.From.Region, "The public Fellowship region is the origin.");
            Assert.IsFalse(move.IsArrival);
            Assert.IsTrue(Rings(stage).Any(ring => ring.Overlaps(stage.Board.Map.FellowshipMarker.Bounds("rivendell").Value)),
                "The Fellowship's figure is the traced source.");
            yield return stage.Until(() => stage.Identities() == "Gimli");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-separation-known.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator ACompanionLeavingAHiddenFellowshipArrivesWithoutARoute()
        {
            using var stage = Separation(2, out var latest);
            yield return stage.Ready();
            Play(stage, latest);
            yield return stage.Until(() => stage.Board.Map.Figures.Moves.Any(m => m.To.Key == "character:" + Gimli));
            var arrival = stage.Board.Map.Figures.Moves.Single(m => m.To.Key == "character:" + Gimli);
            Assert.IsTrue(arrival.IsArrival, "Unknown progress is never a claimed origin.");
            Assert.AreEqual("fords-of-bruinen", arrival.From.Region);
            Assert.IsFalse(Rings(stage).Any(ring => ring.Overlaps(stage.Board.Map.FellowshipMarker.Bounds("rivendell") ?? Rect.zero)),
                "The last known region is not traced.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator BoromirsRemovalShowsHisDimmedPortraitThenHisFigureLeaves()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var boromir = PresentedBoardStage.Character(stage.Before, Boromir, Boromir, Side.FreePeoples, "osgiliath");
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Characters.RemoveAll(c => c.Id == Boromir);
            var step = stage.Paid(stage.Step(1), DieFace.Event);
            var figure = PresentedBoardStage.Figure(boromir);
            step.Facts.Figures.Add(new ActionFigureChange { Before = figure, After = figure, From = "osgiliath", To = "casualties" });
            stage.Play(latest, "grey-havens", step);
            yield return stage.Until(() => stage.Moments() == "Boromir");
            Assert.IsNotNull(stage.Board.Map.Q(className: "observed-portrait-removed"));
            var plates = stage.Presenters.Trace.Plates;
            Assert.AreEqual(stage.Board.Map.childCount - 1, stage.Board.Map.IndexOf(plates), "No stack covers the name.");
            Assert.AreEqual(1, stage.Characters("osgiliath", Side.FreePeoples), "The portrait comes first.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-removal.png");
            yield return stage.Until(() => stage.Characters("osgiliath", Side.FreePeoples) == 0);
            Assert.AreEqual("−1", stage.Board.Map.Q<Label>("observed-delta-count").text);
            yield return stage.Released();
            Assert.IsFalse(stage.Board.View.Characters.Any());
        }

        [UnityTest]
        public IEnumerator TheShadowsReactionDuringTheFreePeoplesActionMarksItsSourceWithoutMovingTheCamera()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            PresentedBoardStage.Character(stage.Before, WitchKing, WitchKing, Side.Shadow, "minas-morgul");
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Available;
            var own = stage.Step(1);
            own.Actor = Side.FreePeoples;
            var reaction = stage.Step(2);
            reaction.Kind = "CharacterAbilityUsed";
            reaction.Costs.Add(new ActionCost { Kind = ActionCostKind.Ability, Id = WitchKing, Source = WitchKing, Owner = Side.Shadow });
            reaction.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Draw, Deck = "character", Count = 1 });
            var buttons = stage.Buttons();
            stage.Play(latest, "minas-morgul", own, reaction);
            var zoom = stage.Board.Map.Camera.Zoom;
            var pan = stage.Board.Map.Camera.Pan;
            yield return stage.Until(() => stage.Moments() == "The Witch-king");
            Assert.AreEqual(zoom, stage.Board.Map.Camera.Zoom, "The player's own action keeps the camera.");
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan);
            Assert.IsTrue(Rings(stage).Any(), "The source's force is traced.");
            Assert.AreEqual(buttons + 1, stage.Buttons(), "Only the Skip target is added.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui309-reaction-source.png");
            yield return stage.Released();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual("", stage.Moments(), "The compact source leaves with the presentation.");
        }

        [UnityTest]
        public IEnumerator SkipDuringAMomentEndsOnTheLatestBoard()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Entry(stage);
            yield return stage.Until(() => stage.Moments() == "The Witch-king");
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(1, stage.Characters("minas-morgul", Side.Shadow));
            yield return stage.NothingFlies();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual("", stage.Moments());
        }

        [UnityTest]
        public IEnumerator TheEntryAndFlightAreRecordedAsMutedClipsInNormalAndReducedMotion()
        {
            foreach (var reduced in new[] { false, true })
            {
                using var stage = new PresentedBoardStage(1920, 1.25f, reduced);
                var nazgul = stage.Put("sauron", UnitKind.Nazgul, "minas-morgul", 2);
                yield return stage.Ready();
                var latest = Entry(stage, nazgul);
                float first = -1, last = -1;
                for (var frame = 0; stage.Playback.Holding && frame < 80; frame++)
                {
                    LayoutTests.Save(stage.Texture, "ui309-clip-" + (reduced ? "reduced-" : "normal-") + frame.ToString("00") + ".png");
                    if (stage.Moments() == "The Witch-king")
                    {
                        last = Time.realtimeSinceStartup;
                        first = first < 0 ? last : first;
                    }
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Assert.GreaterOrEqual(last - first, .9f, "The moment lasts at least its second in both modes.");
                Assert.IsFalse(stage.Playback.Holding);
                stage.AssertFinal(latest);
            }
        }

        // The Shadow's Muster die brings the Witch-king into Minas Morgul (the engine records him from "fellowship"); with
        // Nazgûl given, he then flies with them to Dagorlad in the same action.
        private static PlayerView Entry(PresentedBoardStage stage, List<string> nazgul = null)
        {
            var latest = stage.Latest();
            var king = PresentedBoardStage.Character(latest, WitchKing, WitchKing, Side.Shadow, "minas-morgul");
            var entry = stage.Paid(stage.Step(1), DieFace.Muster);
            entry.Facts.Figures.Add(new ActionFigureChange { After = PresentedBoardStage.Figure(king), From = "fellowship", To = "minas-morgul" });
            if (nazgul == null)
            {
                stage.Play(latest, "grey-havens", entry);
                return latest;
            }
            var after = stage.Next(latest);
            var flown = PresentedBoardStage.Character(after, WitchKing, WitchKing, Side.Shadow, "dagorlad");
            PresentedBoardStage.Place(after, nazgul, "dagorlad");
            var flight = stage.Step(1, 2);
            flight.ActionId = entry.ActionId;
            flight.Movements.Add(Travel("minas-morgul", "dagorlad", nazgul.Select(stage.Unit).Append(PresentedBoardStage.Figure(flown)).ToArray()));
            stage.Play(after, "grey-havens", entry, flight);
            return after;
        }

        // The Shadow watches Gimli leave a Fellowship at Rivendell for the Fords of Bruinen; progress hides its position.
        private static PresentedBoardStage Separation(int progress, out PlayerView latest)
        {
            var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            stage.Before.Fellowship.CompanionIdentities.AddRange(new[] { "gimli", "legolas", "merry", "pippin" });
            stage.Before.Fellowship.CompanionCount = 4;
            stage.Before.Fellowship.Progress = progress;
            latest = stage.Latest();
            latest.Fellowship.CompanionIdentities.Remove("gimli");
            latest.Fellowship.CompanionCount = 3;
            PresentedBoardStage.Character(latest, Gimli, Gimli, Side.FreePeoples, "fords-of-bruinen");
            return stage;
        }

        private static void Play(PresentedBoardStage stage, PlayerView latest)
        {
            var gimli = PresentedBoardStage.Figure(latest.Characters.Single(c => c.Id == Gimli));
            var step = stage.Paid(stage.Step(1), DieFace.Character);
            step.Kind = "CompanionsSeparated";
            step.Movements.Add(Travel("rivendell", "fords-of-bruinen", gimli));
            step.Facts.Figures.Add(new ActionFigureChange { After = gimli, From = "fellowship", To = "fords-of-bruinen" });
            stage.Play(latest, "grey-havens", step);
        }

        private static ActionMovement Travel(string from, string to, params ActionParticipant[] participants) => new ActionMovement
        {
            Kind = ActionMovementKind.Travel, From = from, To = to, Participants = participants.ToList()
        };

        // The observed rings on the map, in map-local points.
        private static List<Rect> Rings(PresentedBoardStage stage) =>
            stage.Board.Map.Query(className: "observed-ring").ToList().Where(ring => ring.resolvedStyle.display == DisplayStyle.Flex)
                .Select(ring => new Rect(ring.resolvedStyle.left, ring.resolvedStyle.top, ring.resolvedStyle.width, ring.resolvedStyle.height)).ToList();
    }
}
