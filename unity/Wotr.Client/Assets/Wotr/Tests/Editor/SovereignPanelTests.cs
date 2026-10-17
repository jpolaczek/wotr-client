using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class SovereignPanelTests
    {
        [Test]
        public void PlacementKeepsFixedControlsClearEvenWhenAFloatingWindowFillsTheRemainingMap()
        {
            var viewport = new Rect(0, 0, 1092, 364);
            var fixedControls = new[]
            {
                new Rect(16, 8, 326, 34), new Rect(0, 60, 94, 84),
                new Rect(110, 112, 480, 40), new Rect(650, 8, 326, 40),
                new Rect(980, 8, 90, 34), new Rect(1000, 60, 90, 34),
                new Rect(950, 318, 130, 34), new Rect(12, 320, 32, 32)
            };
            var occupied = fixedControls.Append(new Rect(0, 0, 1092, 364));
            // The network layout suite verifies this upper bound in the same compact viewport.
            var placement = SovereignPlacement.Near(new Vector2(260, 132), new Vector2(440, 216),
                viewport, occupied, fixedControls);
            Assert.GreaterOrEqual(placement.xMin, viewport.xMin);
            Assert.GreaterOrEqual(placement.yMin, viewport.yMin);
            Assert.LessOrEqual(placement.xMax, viewport.xMax);
            Assert.LessOrEqual(placement.yMax, viewport.yMax);
            foreach (var control in fixedControls)
                Assert.IsFalse(placement.Overlaps(control), "Fixed control is covered: " + control);
        }

        [Test]
        public void DecisionOpensOnceClosePreservesDraftAndInspectionNeverRetargetsItsControls()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCorruptionTile);
            draft.Change(p => p.Ids.Add("tile"));
            var panel = Panel(view, out var tray, out var dock);
            panel.Context(view, draft);
            Assert.IsTrue(panel.IsOpen);
            Assert.AreEqual("king", panel.Selected);
            Assert.AreSame(panel.DecisionHost, tray.parent);
            panel.Close();
            view.Revision++;
            panel.Apply(view);
            panel.Context(view, draft);
            Assert.IsFalse(panel.IsOpen);
            CollectionAssert.AreEqual(new[] { "tile" }, draft.Payload.Ids);
            panel.Show("other");
            Assert.AreEqual(DisplayStyle.None, panel.DecisionHost.style.display.value);
            Assert.AreSame(panel.DecisionHost, tray.parent);
            Assert.AreEqual("king", panel.DecisionTarget);
            panel.OpenDecision();
            Assert.AreEqual("king", panel.Selected);
            Assert.AreEqual(DisplayStyle.Flex, panel.DecisionHost.style.display.value);
            panel.Close();
            view.Decision.Id = "next";
            panel.Context(view, draft);
            Assert.IsTrue(panel.IsOpen);
            view.Decision = null;
            view.DecisionOwner = Side.FreePeoples;
            panel.Context(view, draft);
            Assert.AreSame(dock, tray.parent);
            Assert.IsTrue(panel.IsOpen, "An automatic result updates the open public inspector.");
            Assert.AreEqual(DisplayStyle.None, panel.DecisionHost.style.display.value);
        }

        [Test]
        public void PublicInspectorKeepsUnknownSumAndCannotRenderPrivateChoicesEvenInVisibleVariant()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.Decision = null;
            view.Configuration.VisibleCorruption = true;
            view.Sovereigns[0].Tiles.Add(new CorruptionTileView { Id = "back-0" });
            view.CorruptionAttempt.OwnChoices.Add(new CorruptionTileView { Id = "secret", TileId = "private-physical-tile", Value = 3 });
            var panel = Panel(view, out var tray, out var dock);
            panel.Context(view, new TurnDraft());
            Assert.IsFalse(panel.IsOpen);
            panel.Show("king");
            Assert.AreSame(dock, tray.parent);
            StringAssert.Contains("Corruption ? / 4", panel.Inspector.Q<Label>("sovereign-state").text);
            Assert.AreEqual("▧", panel.Inspector.Q<Label>("attached-tile-back-0").text);
            Assert.IsEmpty(panel.DecisionHost.Children());
            var labels = string.Join(" ", panel.Inspector.Query<Label>().ToList().Select(l => l.text + l.tooltip + l.name));
            StringAssert.DoesNotContain("private-physical-tile", labels);
            StringAssert.DoesNotContain("secret", labels);
            Assert.IsNull(panel.Inspector.Q<Button>("corruption-choice-private-physical-tile"));
        }

        // UI-101: an awakened sovereign the player's roster lists as a character keeps only that roster entry.
        [Test]
        public void AwakenedSovereignListedInTheRosterHidesItsPortraitAndReportsAnEmptySection()
        {
            var view = View();
            view.Decision = null;
            view.Sovereigns.Add(new SovereignView { Id = "other-king", Title = "Other King" });
            var panel = Panel(view, out _, out _);
            var applied = 0;
            panel.Applied += () => applied++;
            panel.ListedAsCharacter = id => id == "king";
            panel.Apply(view);
            Assert.AreEqual(1, applied);
            Assert.AreEqual(DisplayStyle.Flex, panel.Root.Q<Button>("sovereign-king").style.display.value, "Not awakened yet.");
            view.Sovereigns[0].Status = SovereignStatus.Awakened;
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.None, panel.Root.Q<Button>("sovereign-king").style.display.value);
            Assert.IsTrue(panel.HasShownPortraits);
            foreach (var sovereign in view.Sovereigns)
                sovereign.Status = SovereignStatus.Awakened;
            panel.ListedAsCharacter = _ => true;
            panel.Apply(view);
            Assert.IsFalse(panel.HasShownPortraits, "A section of listed sovereigns only is empty.");
            panel.ListedAsCharacter = _ => false;
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.Flex, panel.Root.Q<Button>("sovereign-king").style.display.value,
                "Another side's awakened sovereign keeps its portrait.");
        }

        [Test]
        public void ThreeOrFivePortraitsFollowProjectionAndRetainControlsAcrossPublicStateChanges()
        {
            var view = View();
            view.Decision = null;
            view.Sovereigns.AddRange(Enumerable.Range(2, 3).Select(i => new SovereignView { Id = "s" + i, Title = "Sovereign " + i }));
            var panel = Panel(view, out _, out _);
            Assert.AreEqual(5, panel.Root.childCount);
            var portrait = panel.Root.Q<Button>("sovereign-king");
            panel.Show("king");
            var heading = panel.Inspector.Q<Label>(className: "title");
            view.Sovereigns[0].Status = SovereignStatus.Awakened;
            view.Sovereigns[0].Corruption = 0;
            panel.Apply(view);
            Assert.AreSame(portrait, panel.Root.Q<Button>("sovereign-king"));
            Assert.AreSame(heading, panel.Inspector.Q<Label>(className: "title"));
            StringAssert.Contains("Awakened", portrait.text);
            view.Sovereigns.RemoveRange(3, 2);
            panel.Apply(view);
            Assert.AreEqual(3, panel.Root.childCount);
            view.Sovereigns.Clear();
            panel.Apply(view);
            Assert.IsFalse(panel.IsOpen);
            Assert.AreEqual(0, panel.Root.childCount);
        }

        [Test]
        public void CleansingUsesItsTargetAndIsDistinctFromRemovingACorruptedSovereign()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = view.You;
            view.CorruptionAttempt = null;
            view.Decision.Kind = DecisionKind.EffectChoice;
            view.Decision.Presentation = "CorruptionRemoval";
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseEffect,
                EffectChoices = { new EffectChoiceView { Id = "remove", TargetId = "king" } } });
            Assert.AreEqual("king", SovereignPresentation.DecisionTarget(view, null));
            var attention = DecisionAttention.For(view);
            Assert.AreEqual(DecisionContext.Sovereign, attention.Context);
            Assert.AreEqual("king", attention.Subject);
            view.Battle = new BattleView { RegionId = "erebor" };
            Assert.AreEqual(DecisionContext.Sovereign, DecisionAttention.For(view).Context,
                "A political interruption belongs to the Sovereign while the battle waits.");
            var tray = new VisualElement();
            new DecisionTray(tray).Render(view, new TurnDraft(), false, false);
            Assert.IsEmpty(tray.Query(className: "battle-stages").ToList());
            Assert.AreEqual("Random tile return", tray.Q<Label>(className: "title").text);
            view.Decision.SourceId = "kings.sovereign.dain";
            Assert.AreEqual("Return a Corruption tile?", DecisionNoticeText.Describe(view));
            view.Battle = null;
            StringAssert.Contains("random non-Kings tile", SovereignPresentation.Consequence(view, null));
            view.Decision.Kind = DecisionKind.Action;
            view.Decision.Presentation = "";
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RemoveCorruptedSovereign, "cost");
            draft.Change(p => p.CharacterId = "king");
            StringAssert.Contains("not a tile return", SovereignPresentation.Consequence(view, draft));
            view.Winner = Side.FreePeoples;
            Assert.IsFalse(SovereignPresentation.OwnsDecision(view, draft));
        }

        [Test]
        public void AwakeningShowsBoundTokenAndConditionalSharedRulerWithoutDuplicateCostControls()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = view.You;
            view.Decision.Kind = DecisionKind.Action;
            view.CorruptionAttempt = null;
            view.Dice.Add(new DieView { Id = "cost", Face = DieFace.Muster, Location = DieLocation.Available, Owner = view.You });
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.AwakenSovereign, EligibleDice = { "cost" }, EligibleIds = { "king" },
                CharacterActions = { new CharacterActionOption { CharacterId = "king", Dice = { "cost" } } },
                Travel = { new TravelOption { Id = "king", Origin = "erebor", Range = 1, Targets = { "erebor" } } }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AwakenSovereign, "cost");
            draft.Change(p => p.CharacterId = "king");
            var root = new VisualElement();
            var choices = new ExpansionChoices(root, view, draft, action => action(), BoardPresentation.Load());
            choices.Refresh();
            Assert.IsNull(root.Q<Button>("cost-cost"));
            Assert.IsNull(root.Q<Button>("sovereign-choice-king"));
            Assert.IsTrue(root.Q<Button>("travel-stay").enabledSelf);
            Assert.IsFalse(draft.Ready(DraftOptions.For(view, draft)));
            var text = SovereignPresentation.Consequence(view, draft);
            StringAssert.Contains("Return all attached tiles", text);
            StringAssert.Contains("Ruler token", text);
            StringAssert.Contains("if an Awakened Sovereign remains", text);
            view.Configuration.RemovingRulerTokens = true;
            StringAssert.Contains("tokens are not used", SovereignPresentation.Consequence(view, draft));
            Assert.AreEqual("cost", draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.Route);
            var tray = new VisualElement();
            var decision = new DecisionTray(tray);
            decision.Render(view, draft, false, false);
            Assert.AreSame(tray.Q<Button>("travel-stay").parent, tray.Q<Button>("decision-back").parent);
            Assert.IsEmpty(FellowshipDraft.Hint(draft, view));
            // UI-105: the destination click places the sovereign; no route is drawn.
            StringAssert.Contains("Click a highlighted destination", tray.Q<Label>("travel-summary").tooltip);
            decision.Preview(false, "Offline · Your draft is saved locally.");
            var status = tray.Q<Label>(className: "draft-status");
            Assert.AreEqual(DisplayStyle.Flex, status.style.display.value);
            StringAssert.Contains("Offline", status.text);
        }

        [Test]
        public void AvailabilityExplainsOnlyProjectedOwnConditionsWithoutGrantingAnAction()
        {
            var view = View();
            view.Availability.Add(new ActionAvailabilityView { SubjectId = "king", Title = "Attempt corruption", Cost = "One eligible Eye",
                Conditions = { new AvailabilityConditionView { Description = "Already attempted this turn", Met = false } } });
            var panel = Panel(view, out _, out _);
            panel.Show("king");
            StringAssert.Contains("Already attempted", panel.Inspector.Q<Label>("sovereign-availability").tooltip);
            Assert.IsNull(ExpansionActions.Sovereign(view, new TurnDraft(), "king"));
            panel.Show("other");
            Assert.AreEqual(DisplayStyle.None, panel.Inspector.Q<Label>("sovereign-availability").style.display.value);
            Assert.IsEmpty(panel.Inspector.Q<Label>("sovereign-availability").tooltip);
        }

        private static SovereignPanel Panel(PlayerView view, out VisualElement tray, out VisualElement dock)
        {
            var panel = new SovereignPanel(BoardPresentation.Load(), _ => Assert.Fail("Inspection cannot move the camera."));
            tray = new VisualElement();
            dock = new VisualElement();
            dock.Add(tray);
            panel.Attach(tray, dock);
            panel.Apply(view);
            return panel;
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "match", You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "decision", Kind = DecisionKind.CorruptionTile },
            CorruptionAttempt = new CorruptionAttemptView { SovereignId = "king", DrawLimit = 3, DrawnCount = 1 },
            Sovereigns =
            {
                new SovereignView { Id = "king", Title = "Dáin", RegionId = "erebor", Resistance = 4 },
                new SovereignView { Id = "other", Title = "Théoden", RegionId = "edoras", Resistance = 3 }
            }
        };
    }
}
