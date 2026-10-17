using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class FellowshipTrackTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(11, 11)]
        [TestCase(12, 12)]
        [TestCase(13, 12)]
        [TestCase(20, 12)]
        public void PrintedProgressRangeNeverChangesTheExactProjectedDistance(int progress, int position)
        {
            var view = new FellowshipView { Hidden = true, Progress = progress, RegionId = "rivendell" };
            var before = JsonConvert.SerializeObject(view);
            var model = FellowshipTrackState.Journey(view);
            Assert.AreEqual(progress, model.Value);
            Assert.AreEqual(position, model.Position);
            Assert.AreEqual(12, model.Maximum);
            Assert.AreEqual(progress > 12, model.BeyondTrack);
            StringAssert.Contains("Progress " + progress, model.Description);
            StringAssert.Contains("does not select a secret route", model.Description);
            Assert.AreEqual(before, JsonConvert.SerializeObject(view));
        }

        [TestCase(0, true, 0)]
        [TestCase(4, false, 11)]
        [TestCase(5, false, 12)]
        public void MordorReplacesOnlyJourneyAndDoesNotPredictVictory(int step, bool hidden, int corruption)
        {
            var view = new FellowshipView { Progress = 9, MordorStep = step, Hidden = hidden, Corruption = corruption };
            var model = FellowshipTrackState.Journey(view);
            Assert.AreEqual(step, model.Value);
            Assert.AreEqual(5, model.Maximum);
            Assert.AreEqual(hidden ? "hidden" : "revealed", model.Symbol);
            var damage = FellowshipTrackState.Corruption(view);
            Assert.AreEqual(corruption, damage.Value);
            Assert.AreEqual(12, damage.Maximum);
            StringAssert.Contains("before advancing", model.Description);
            StringAssert.Contains("Shadow wins immediately", damage.Description);
            StringAssert.DoesNotContain("Free Peoples win", model.Description);
        }

        [Test]
        public void StatusUpdatesExistingCellsAndKeepsBothTracksVisibleDuringRoutesAndHunt()
        {
            var status = new FellowshipStatus(null);
            var view = new FellowshipView { Hidden = true, Progress = 3, Corruption = 0, RegionId = "rivendell" };
            var data = BoardPresentation.Load();
            status.Apply(view, data);
            var cell = status.Root.Q<Label>("journey-step-12");
            var value = status.Root.Q<Label>("corruption-label");
            var marker = status.Root.Q<Image>("journey-track-marker");
            view.Progress = 13;
            view.Hidden = false;
            view.Corruption = 11;
            status.Apply(view, data);
            status.Compact(true, true, false);
            Assert.AreSame(cell, status.Root.Q<Label>("journey-step-12"));
            Assert.AreSame(value, status.Root.Q<Label>("corruption-label"));
            Assert.AreSame(marker, status.Root.Q<Image>("journey-track-marker"));
            Assert.IsTrue(cell.ClassListContains("selected"));
            Assert.AreEqual("13", status.Root.Q<Label>("journey-label").text);
            Assert.AreEqual("11", value.text);
            Assert.AreEqual("Revealed", status.StateRow.Q<Label>("fellowship-condition").text);
            Assert.AreSame(FellowshipArtwork.Sprite("revealed"), marker.sprite);
            Assert.AreEqual(DisplayStyle.Flex, status.Root.Q("journey-track").style.display.value);
            Assert.AreNotEqual(DisplayStyle.None, status.Root.Q("corruption-track").style.display.value);
            Assert.IsTrue(status.Root.Q("journey-track").ClassListContains("track-overflow"));
            status.Compact(false, false, false);
            Assert.AreEqual(DisplayStyle.Flex, status.Root.Q(className: "fellowship-guide-row").style.display.value);
            status.Compact(false, false, true);
            Assert.AreEqual(DisplayStyle.None, status.Root.Q(className: "fellowship-guide-row").style.display.value, "UI-90: the roster's Guide portrait replaces the row.");
            Assert.IsNull(status.Root.Q(className: "fellowship-state-row"), "UI-90: the panel header hosts the state row.");
            view.MordorStep = 0;
            status.Apply(view, data);
            Assert.AreEqual(DisplayStyle.None, status.Root.Q("journey-track").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, status.Root.Q("mordor-track").style.display.value);
            Assert.AreEqual("0", status.Root.Q<Label>("mordor-label").text);
            Assert.AreEqual("11", value.text);
        }

        [Test]
        public void PublicStatusDoesNotInferUnrevealedCouncilVersionsOrSendAnAction()
        {
            var open = 0;
            var status = new FellowshipStatus(() => open++);
            var view = new FellowshipView
            {
                Guide = "base.character.strider", Hidden = true, RegionId = "rivendell", Progress = 4,
                CompanionIdentities = { "gandalf", "gimli" }
            };
            var before = JsonConvert.SerializeObject(view);
            status.Apply(view, BoardPresentation.Load());
            var text = string.Join("\n", status.Root.Query<TextElement>().ToList().Select(e => e.text + " " + e.tooltip));
            StringAssert.DoesNotContain("lords.character", text);
            StringAssert.DoesNotContain("Fellowship Box", text);
            Assert.AreEqual(before, JsonConvert.SerializeObject(view));
            Assert.AreEqual(0, open);
            view.RevealedCompanionVersions.Add("base.character.strider", "lords.character.strider");
            status.Apply(view, BoardPresentation.Load());
            StringAssert.Contains("Fellowship Box", status.Root.Q<Label>("fellowship-guide").tooltip);
            // UI-110: only each track's symbol is a button (the Fellowship's move); it stays disabled until offered.
            var buttons = status.Root.Query<Button>().ToList();
            Assert.IsTrue(buttons.All(b => b.ClassListContains("fellowship-track-move") && !b.enabledSelf),
                "Track fields are read-only, not destinations.");
            status.OfferMove(true, "Move the Fellowship");
            Assert.IsTrue(status.Root.Q<Button>(FellowshipStatus.MoveName).enabledSelf);
            Assert.AreSame(status.Root.Q("journey-track"), status.Root.Q<Button>(FellowshipStatus.MoveName).parent);
            Assert.AreEqual(1, status.Root.Query<Button>().ToList().Count(b => b.enabledSelf), "Only the visible track moves.");
        }
    }
}
