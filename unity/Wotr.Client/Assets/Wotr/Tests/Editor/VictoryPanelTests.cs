using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class VictoryPanelTests
    {
        [Test]
        public void ThresholdIsPendingUntilTheServerProvidesItsWinner()
        {
            var view = Screen();
            view.MilitaryVictory[0].Points = 4;
            view.MilitaryVictory[0].ThresholdReached = true;
            var panel = Panel();
            panel.Apply(view);
            Assert.AreEqual("FP 4/4 · SH 0/10", panel.Tab.text);
            Assert.AreEqual("Threshold reached · Checked at turn end", panel.Root.Q<Label>("victory-timing").text);
            Assert.AreEqual(DisplayStyle.None, panel.Banner.style.display.value);
            Assert.IsNull(view.Winner);
            Assert.IsNull(view.Victory);
        }

        [TestCase(Side.FreePeoples, VictoryCondition.RingDestroyed, "Ring destroyed")]
        [TestCase(Side.Shadow, VictoryCondition.RingCorrupted, "Ring-bearers corrupted")]
        [TestCase(Side.FreePeoples, VictoryCondition.FreePeoplesMilitary, "Military victory")]
        [TestCase(Side.Shadow, VictoryCondition.ShadowMilitary, "Military victory")]
        public void FinalMessageUsesTheAuthoritativeWinnerAndReason(Side winner, VictoryCondition reason, string text)
        {
            var view = Screen();
            view.Winner = winner;
            view.Victory = reason;
            // Conflicting visual progress is deliberately not an authority for the UI.
            view.Fellowship.Corruption = 12;
            view.Fellowship.MordorStep = 5;
            var panel = Panel();
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.Flex, panel.Banner.style.display.value);
            Assert.AreEqual((winner == Side.Shadow ? "Shadow wins" : "Free Peoples win") + " · " + text, panel.Banner.text);
            Assert.AreEqual("Final military score", panel.Root.Q<Label>("victory-timing").text);
        }

        [Test]
        public void MissingBreakdownShowsTheOldPublicTotalWithoutInventingRegions()
        {
            var view = new PlayerView { Players = { new PlayerSummary { Side = Side.FreePeoples, VictoryPoints = 3 } } };
            var panel = Panel();
            panel.Apply(view);
            Assert.AreEqual("FP 3/4 · SH —/10", panel.Tab.text);
            Assert.AreEqual("Breakdown unavailable", panel.Root.Q<Label>("victory-empty-FreePeoples").text);
            Assert.IsNull(panel.Root.Q<Button>("victory-locate-dol-guldur"));
            Assert.AreEqual(DisplayStyle.None, panel.Banner.style.display.value);
        }

        [Test]
        public void ScoringUsesProjectedSetupValuesWithoutReclassifyingKingsOrFateRegions()
        {
            var view = Screen();
            view.MilitaryVictory[1].Regions.Add(new VictoryRegionView
            {
                RegionId = "helms-deep", Name = "Helm’s Deep", Settlement = "Stronghold", Points = 2
            });
            view.MilitaryVictory[1].Points = 2;
            view.MilitaryVictory[0].Regions.Add(new VictoryRegionView
            {
                RegionId = "south-rhun", Name = "South Rhûn", Settlement = "City", Points = 1
            });
            view.MilitaryVictory[0].Points = 1;
            var panel = Panel();
            panel.Apply(view);
            Assert.AreEqual("FP 1/4 · SH 2/10", panel.Tab.text);
            Assert.AreEqual("SH control · Stronghold · 2", panel.Root.Q("victory-region-helms-deep").Query<Label>().ToList()[1].text);
            Assert.AreEqual("FP control · City · 1", panel.Root.Q("victory-region-south-rhun").Query<Label>().ToList()[1].text);
        }

        [Test]
        public void UpdatesPreserveRegionControlsWhileLostConquestsRemoveTheirOldTargets()
        {
            var view = Screen();
            var score = view.MilitaryVictory[0];
            score.Regions.Add(new VictoryRegionView { RegionId = "moria", Name = "Moria", Settlement = "Stronghold", Points = 2 });
            score.Points = 2;
            var panel = Panel();
            panel.Apply(view);
            panel.Open();
            var target = panel.Root.Q<Button>("victory-locate-moria");
            view.Revision++;
            panel.Apply(view);
            Assert.AreSame(target, panel.Root.Q<Button>(target.name));
            Assert.IsTrue(panel.Pinned);
            score.Points = 0;
            score.Regions.Clear();
            panel.Apply(view);
            Assert.IsNull(panel.Root.Q<Button>(target.name));
            Assert.AreEqual("No captured cities or strongholds", panel.Root.Q<Label>("victory-empty-FreePeoples").text);
        }

        [Test]
        public void OpeningClosingAndRefreshDoNotChangeTheViewOrReopenAClosedPanel()
        {
            var view = Screen();
            var panel = Panel();
            panel.Apply(view);
            panel.Open();
            panel.Close();
            panel.Apply(view);
            Assert.IsFalse(panel.IsOpen);
            Assert.AreEqual(0, view.Revision);
            Assert.AreEqual(GamePhase.ActionResolution, view.Phase);
            Assert.IsNull(view.Winner);
            Assert.IsEmpty(view.Options);
            panel.Open();
            panel.Apply(new PlayerView { MatchId = "another" });
            Assert.IsFalse(panel.Pinned);
        }

        private static VictoryPanel Panel() => new VictoryPanel(new VisualElement(), new MapCanvas(BoardPresentation.Load()), () => { });

        private static PlayerView Screen() => new PlayerView
        {
            MatchId = "victory", Phase = GamePhase.ActionResolution,
            MilitaryVictory =
            {
                new MilitaryVictoryView { Side = Side.FreePeoples, Threshold = 4 },
                new MilitaryVictoryView { Side = Side.Shadow, Threshold = 10 }
            }
        };
    }
}
