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
        // UI-383 (owner): a corrupted king's tiles are resolved and a removed one is out of play, so neither shows a
        // Corruption count.
        [Test]
        public void ACorruptedOrRemovedKingShowsNoCorruptionCount()
        {
            var king = new SovereignView { Id = "king", Resistance = 4, Corruption = 0, Status = SovereignStatus.Corrupted };
            StringAssert.StartsWith("Corrupted", SovereignPresentation.Corruption(king));
            StringAssert.DoesNotContain("/ 4", SovereignPresentation.Corruption(king));
            king.Status = SovereignStatus.Removed;
            Assert.AreEqual("Removed from play", SovereignPresentation.Corruption(king));
            king.Status = SovereignStatus.Initial;
            Assert.AreEqual("Corruption 0 / 4", SovereignPresentation.Corruption(king));
        }

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

        // UI-274: the attempt's tiles stand beside the king, the Shadow's with their values; their click attaches; the
        // tray builds no choice, so no window opens, and the inspector does not open by itself.
        [Test]
        public void AttemptTilesStandBesideTheKingAndTheTrayStaysEmpty()
        {
            var view = View();
            view.CorruptionAttempt.OwnChoices.Add(new CorruptionTileView { Id = "a", TileId = "t2", Value = 2 });
            view.CorruptionAttempt.OwnChoices.Add(new CorruptionTileView { Id = "b", TileId = "t1", Value = 1 });
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseCorruptionTile, Enabled = true, EligibleIds = { "t2", "t1" } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCorruptionTile);
            var panel = Panel(view);
            panel.Context(view, draft);
            panel.Highlight(_ => false);
            Assert.IsFalse(panel.IsOpen, "The decision opens no inspector.");
            var slot = panel.Root.Q("sovereign-slot-king");
            var tile = slot.Q<Button>("corruption-choice-t2");
            Assert.AreEqual("2", tile.Q<Label>().text);
            Assert.IsTrue(tile.ClassListContains(SovereignPortrait.LitClass));
            StringAssert.StartsWith("Click to attach this tile to Dáin", tile.tooltip);
            Assert.AreEqual(2, slot.Query<Button>(className: "sovereign-tile").ToList().Count);
            Assert.IsEmpty(panel.Root.Q("sovereign-slot-other").Query<Button>(className: "sovereign-tile").ToList());
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices, "The tiles are on the king: no window.");
            Assert.AreEqual(DecisionHost.Characters, DecisionRoutes.Of(view));
            Assert.AreEqual(DecisionContext.Sovereign, DecisionAttention.For(view).Context);
        }

        // The Free Peoples see the drawn tiles' backs beside the king and never a private value or tile id.
        [Test]
        public void PublicInspectorKeepsUnknownSumAndCannotRenderPrivateChoicesEvenInVisibleVariant()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.Decision = null;
            view.Configuration.VisibleCorruption = true;
            view.Sovereigns[0].Tiles.Add(new CorruptionTileView { Id = "back-0" });
            view.CorruptionAttempt.OwnChoices.Add(new CorruptionTileView { Id = "secret", TileId = "private-physical-tile", Value = 3 });
            var panel = Panel(view);
            panel.Context(view, new TurnDraft());
            panel.Highlight(_ => false);
            Assert.IsFalse(panel.IsOpen);
            var slot = panel.Root.Q("sovereign-slot-king");
            Assert.IsTrue(slot.Q<Button>("corruption-choice-drawn-0").ClassListContains("sovereign-tile-back"));
            Assert.AreEqual(Visibility.Hidden, slot.Q<Button>("sovereign-attempt-king").style.visibility.value,
                "No attempt control for the Free Peoples.");
            panel.Show("king");
            StringAssert.Contains("Corruption ? / 4", panel.Inspector.Q<Label>("sovereign-state").text);
            Assert.AreEqual("▧", panel.Inspector.Q<Label>("attached-tile-back-0").text);
            var labels = string.Join(" ", panel.Root.Query<VisualElement>().ToList().Concat(panel.Inspector.Query<VisualElement>().ToList())
                .Select(e => e.name + e.tooltip + (e as TextElement)?.text));
            StringAssert.DoesNotContain("private-physical-tile", labels);
            StringAssert.DoesNotContain("secret", labels);
        }

        // UI-274: every sovereign keeps its slot in every state, its badge and meter following the projection.
        [Test]
        public void EverySovereignKeepsItsSlotInEveryState()
        {
            var view = View();
            view.Decision = null;
            view.CorruptionAttempt = null;
            var panel = Panel(view);
            var slot = panel.Root.Q("sovereign-slot-king");
            Assert.AreEqual(4, slot.Q("sovereign-meter").childCount, "A pip per point of resistance.");
            Assert.IsTrue(slot.Q("sovereign-status").ClassListContains("sovereign-status-initial"));
            view.Sovereigns[0].Tiles.Add(new CorruptionTileView { Id = "back-0" });
            panel.Apply(view);
            Assert.IsTrue(slot.Q("sovereign-meter")[0].ClassListContains("hidden"), "A hidden tile hatches a pip.");
            foreach (var status in new[] { SovereignStatus.Awakened, SovereignStatus.Corrupted, SovereignStatus.Removed })
            {
                view.Sovereigns[0].Status = status;
                panel.Apply(view);
                Assert.AreSame(slot, panel.Root.Q("sovereign-slot-king"), "The slot stays in its place.");
                Assert.IsTrue(slot.Q("sovereign-status").ClassListContains("sovereign-status-" + status.ToString().ToLowerInvariant()));
            }
            Assert.IsTrue(panel.HasShownPortraits);
        }

        // UI-274: the attempt button beside an Initial king sends the attempt; disabled, it says why. UI-398 (owner): it pays
        // the Eye the player clicked, and before an Eye is chosen it stays dark.
        [Test]
        public void TheAttemptButtonPaysTheEyeClickedFirst()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.Action;
            view.CorruptionAttempt = null;
            view.Dice.Add(new DieView { Id = "lesser", Owner = Side.Shadow, Source = "lords.die.gothmog", Face = DieFace.Eye, Location = DieLocation.Hunt });
            view.Dice.Add(new DieView { Id = "eye", Owner = Side.Shadow, Source = "standard", Face = DieFace.Eye, Location = DieLocation.Hunt });
            view.Options.Add(new ActionOption { Kind = CommandKind.AttemptCorruption, Enabled = true, EligibleIds = { "king" }, EligibleDice = { "lesser", "eye" } });
            view.Availability.Add(new ActionAvailabilityView { SubjectId = "other", ActionId = SovereignAttempt.ActionId,
                Conditions = { new AvailabilityConditionView { Description = "This sovereign has not been targeted this turn.", Met = false } } });
            var draft = new TurnDraft();
            Assert.AreEqual("", SovereignAttempt.Eye(view, draft), "No Eye pays before one is chosen.");
            Assert.IsFalse(SovereignAttempt.Enabled(view, draft, "king"), "Nothing lights before an Eye is chosen.");
            StringAssert.Contains("Click an Eye that pays in your dice row first", SovereignAttempt.Tooltip(view, draft, view.Sovereigns[0]));
            draft.Choose(CommandKind.AttemptCorruption, "eye");
            Assert.AreEqual("eye", SovereignAttempt.Eye(view, draft));
            var panel = Panel(view);
            panel.Context(view, draft);
            panel.Highlight(_ => false);
            var button = panel.Root.Q<Button>("sovereign-attempt-king");
            Assert.IsTrue(button.enabledSelf);
            StringAssert.StartsWith("Click to attempt to corrupt Dáin", button.tooltip);
            var other = panel.Root.Q<Button>("sovereign-attempt-other");
            Assert.IsFalse(other.enabledSelf);
            StringAssert.Contains("○ This sovereign has not been targeted this turn.", other.tooltip);
            draft.Choose(CommandKind.AttemptCorruption, "lesser");
            Assert.AreEqual("lesser", SovereignAttempt.Eye(view, draft), "A special Eye clicked first pays.");
            view.Sovereigns[0].Status = SovereignStatus.Awakened;
            panel.Apply(view);
            Assert.AreEqual(Visibility.Hidden, button.style.visibility.value, "An awakened king takes no attempt.");
        }

        [Test]
        public void ThreeOrFivePortraitsFollowProjectionAndRetainControlsAcrossPublicStateChanges()
        {
            var view = View();
            view.Decision = null;
            view.Sovereigns.AddRange(Enumerable.Range(2, 3).Select(i => new SovereignView { Id = "s" + i, Title = "Sovereign " + i }));
            var panel = Panel(view);
            Assert.AreEqual(5, panel.Root.childCount);
            var portrait = panel.Root.Q<Button>("sovereign-king");
            panel.Show("king");
            var heading = panel.Inspector.Q<Label>(className: "title");
            view.Sovereigns[0].Status = SovereignStatus.Awakened;
            view.Sovereigns[0].Corruption = 0;
            panel.Apply(view);
            Assert.AreSame(portrait, panel.Root.Q<Button>("sovereign-king"));
            Assert.AreSame(heading, panel.Inspector.Q<Label>(className: "title"));
            Assert.IsTrue(portrait.ClassListContains("sovereign-awakened"));
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
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, EligibleIds = { "remove", "decline" },
                EffectChoices = { new EffectChoiceView { Id = "remove", TargetId = "king" }, new EffectChoiceView { Id = "decline", TargetId = "king", Title = "Keep tiles" } } });
            Assert.AreEqual("king", SovereignPresentation.DecisionTarget(view, null));
            // UI-274: the king's portrait returns the tile; UI-278 (D7): Keep tiles is the console's Pass.
            StringAssert.StartsWith("Click to return one random tile of Dáin", SovereignChoices.Portraits(view)["king"].Tooltip);
            Assert.AreEqual("decline", OptionalDecline.Option(view, new TurnDraft()).Id);
            Assert.AreEqual("Pass · Keep the king's tiles in place", OptionalDecline.Option(view, new TurnDraft()).Tooltip);
            var effect = new TurnDraft();
            effect.Choose(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, effect), "No Keep tiles button remains.");
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
            StringAssert.Contains("his weaknesses end", SovereignPresentation.Consequence(view, draft));
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
            StringAssert.Contains("if an Awakened sovereign remains", text);
            view.Configuration.RemovingRulerTokens = true;
            StringAssert.Contains("tokens are not used", SovereignPresentation.Consequence(view, draft));
            Assert.AreEqual("cost", draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.Route);
            var tray = new VisualElement();
            var decision = new DecisionTray(tray);
            decision.Render(view, draft, false, false);
            // UI-274: the destination is a click on the map; the tray offers no Stay button and opens no window.
            Assert.IsNull(tray.Q<Button>("travel-stay"));
            Assert.IsFalse(decision.HasChoices);
            StringAssert.Contains("Click a highlighted region on the map", tray.Q<Label>(className: "draft-status").text);
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
            var panel = Panel(view);
            panel.Show("king");
            StringAssert.Contains("Already attempted", panel.Inspector.Q<Label>("sovereign-availability").tooltip);
            Assert.IsNull(ExpansionActions.Sovereign(view, new TurnDraft(), "king"));
            panel.Show("other");
            Assert.AreEqual(DisplayStyle.None, panel.Inspector.Q<Label>("sovereign-availability").style.display.value);
            Assert.IsEmpty(panel.Inspector.Q<Label>("sovereign-availability").tooltip);
        }

        // UI-345: a sovereign's changed status and corruption pulse on his slot; his first values never do.
        [Test]
        public void AChangedStatusOrCorruptionPulsesOnTheSlot()
        {
            var view = View();
            view.Decision = null;
            view.CorruptionAttempt = null;
            var panel = Panel(view);
            var slot = panel.Root.Q("sovereign-slot-king");
            Assert.IsFalse(slot.Q("sovereign-status").ClassListContains(ValuePulse.PulseClass), "A first status never pulses.");
            Assert.IsFalse(slot.Q("sovereign-meter").ClassListContains(ValuePulse.PulseClass), "A first meter never pulses.");
            view.Sovereigns[0].Tiles.Add(new CorruptionTileView { Id = "back-0" });
            panel.Apply(view);
            Assert.IsTrue(slot.Q("sovereign-meter").ClassListContains(ValuePulse.PulseClass), "An attached tile pulses on the meter.");
            view.Sovereigns[0].Status = SovereignStatus.Corrupted;
            panel.Apply(view);
            Assert.IsTrue(slot.Q("sovereign-status").ClassListContains(ValuePulse.PulseClass), "A changed status pulses on its badge.");
        }

        // UI-276 (EXP-15 D19): a king who holds his Ruler token wears its badge on his portrait; the token itself stands in
        // the dice pool.
        [Test]
        public void AKingHoldingHisRulerTokenWearsItsBadge()
        {
            var view = View();
            view.Decision = null;
            view.CorruptionAttempt = null;
            view.RulerTokens.Add(new RulerTokenView { Id = "kings.token.king", SovereignId = "king", Location = "free-peoples" });
            var panel = Panel(view);
            var portrait = panel.Root.Q<Button>("sovereign-king");
            Assert.AreEqual(DisplayStyle.Flex, portrait.Q("sovereign-token").style.display.value);
            StringAssert.Contains("Holds his Ruler token", portrait.tooltip);
            Assert.AreEqual(DisplayStyle.None, panel.Root.Q<Button>("sovereign-other").Q("sovereign-token").style.display.value);
            view.RulerTokens[0].Location = "dismay";
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.None, portrait.Q("sovereign-token").style.display.value, "A spent token leaves the badge.");
        }

        private static SovereignPanel Panel(PlayerView view)
        {
            var panel = new SovereignPanel(BoardPresentation.Load(), _ => Assert.Fail("Inspection cannot move the camera."));
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
