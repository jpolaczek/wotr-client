using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class BoardInteractionTests
    {
        [UnityTest]
        public IEnumerator FinalTargetClickSendsTheStepWithoutConfirmAndTheArmyIsNeverDragged()
        {
            using var test = new Harness();
            yield return Settle();
            yield return SelectArmy(test.Board, "minas-tirith");
            yield return Settle();
            Assert.AreEqual(0, test.Backend.Commands);
            Assert.IsNull(test.Board.Tray.Q<Button>("confirm-decision"));
            yield return HoverRegion(test.Board, "osgiliath");
            MapContourProbe.Click(test.Board.Map, MapContourProbe.Terrain(test.Board.Map, "osgiliath"));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(1, test.Backend.Commands);
            Assert.IsTrue(test.Backend.Last.Payload.Stepwise);
            var clicked = JsonConvert.SerializeObject(test.Backend.Last.Payload);
            test.Reset();
            yield return Settle();
            yield return SelectArmy(test.Board, "minas-tirith");
            yield return Settle();
            MapContourProbe.ReachRegion(test.Board.Map, "osgiliath");
            yield return Settle();
            // UI-105: dragging the army is not a move; the destination click sends the same step.
            MapContourProbe.AttemptDrag(test.Board, test.Board.Map.Q<Button>("army-minas-tirith-FreePeoples"),
                MapContourProbe.Terrain(test.Board.Map, "osgiliath"));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(1, test.Backend.Commands);
            Assert.IsTrue(test.Board.Map.DraftOverlay.Targets.Contains("osgiliath"), "The selection survives the drag attempt.");
            MapContourProbe.ReachRegion(test.Board.Map, "osgiliath");
            yield return Settle();
            // The region panel of the source is still open: the terrain press closes it and the markers settle again,
            // yet the release on the destination is the step (UI-105).
            Assert.AreEqual("minas-tirith", test.Board.Hover.Panel.Region);
            Assert.AreEqual(DisplayStyle.Flex, test.Board.Hover.Panel.Root.resolvedStyle.display);
            MapContourProbe.Click(test.Board.Map, MapContourProbe.Terrain(test.Board.Map, "osgiliath"));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(2, test.Backend.Commands);
            Assert.AreEqual(clicked, JsonConvert.SerializeObject(test.Backend.Last.Payload));
        }

        // UI-127: one click selects the whole army and lights its targets; a figure click leaves it behind; a hover
        // over a target shows the army there faintly with the server's caption; the first Escape drops the
        // selection in one step, the second closes the panel. Nothing is sent.
        [UnityTest]
        public IEnumerator UI127OneClickSelectsTheArmyAGhostPreviewsTheTargetAndEscapeClearsIt()
        {
            using var test = new Harness();
            yield return Settle();
            Click(test.Board.OwnDice.Q<Button>("army"));
            yield return Settle();
            test.Board.Hover.Close();
            var marker = test.Board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            yield return MatchTestRoom.Wait(MapContourProbe.ReachMarker(test.Board.Map, marker, "minas-tirith"));
            PointerGestureEvents.ClickAt(marker);
            yield return Settle();
            var panel = test.Board.Hover.Panel.Root;
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "One click opens the force's panel.");
            var eligible = panel.Query<Button>(className: "eligible-figure").ToList();
            var selected = panel.Query<Button>(className: "selected").ToList();
            Assert.IsNotEmpty(eligible);
            CollectionAssert.AreEquivalent(eligible, selected, "One click selects every figure that can move.");
            Assert.IsTrue(marker.ClassListContains("selected-force"));
            Assert.IsTrue(test.Board.Map.DraftOverlay.Targets.Contains("osgiliath"), "Targets light at once.");
            var regular = selected.First(b => test.Board.View.Units.Any(u => u.Id == ((DraftToken)b.userData).Id && u.Kind == UnitKind.Regular));
            PointerGestureEvents.ClickAt(regular);
            yield return Settle();
            Assert.AreEqual(selected.Count - 1, panel.Query<Button>(className: "selected").ToList().Count, "A click leaves one figure behind.");
            Assert.AreEqual(0, test.Backend.Commands);

            yield return HoverRegion(test.Board, "osgiliath");
            var ghost = test.Board.Map.Q<Button>("army-ghost");
            var caption = test.Board.Map.Q<Label>("army-ghost-caption");
            Assert.NotNull(ghost);
            Assert.AreEqual(DisplayStyle.Flex, ghost.resolvedStyle.display, "Hovering a target shows the army there.");
            Assert.AreEqual(PickingMode.Ignore, ghost.pickingMode);
            Assert.AreEqual("Move " + (selected.Count - 1) + " figures", caption.text);
            LayoutTests.Save(test.Viewport, "ui127-ghost.png");
            // The pointer leaves the map the way RegionHover reports it; a move to the screen edge would edge-pan.
            test.Board.Map.SetHovered("");
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, ghost.resolvedStyle.display, "Leaving the target removes the ghost.");

            test.Board.Hover.Open("minas-tirith");
            // Keyboard events go to the focused element; a player who clicked the map or a figure has focus there.
            ((VisualElement)test.Board.Map).Focus();
            yield return Settle();
            BoardMenuLayoutTests.Key(test.Board.Root, true);
            BoardMenuLayoutTests.Key(test.Board.Root, false);
            yield return Settle();
            Assert.IsEmpty(panel.Query<Button>(className: "selected").ToList(), "The first Escape drops the whole selection.");
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "The panel stays for a new choice.");
            Assert.AreEqual("army", test.Board.ActionDie.DieId, "The die stays chosen.");
            Assert.IsEmpty(test.Board.Map.DraftOverlay.Targets);
            BoardMenuLayoutTests.Key(test.Board.Root, true);
            BoardMenuLayoutTests.Key(test.Board.Root, false);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "The second Escape closes the panel.");
            Assert.AreEqual(0, test.Backend.Commands);
        }

        // UI-130/UI-179/UI-181: Muster lights the Reserve tab and the player opens it; a closed nation keeps a plain,
        // inert row with the server's reason; a reserve token lights only its nation's settlements and a click on one
        // recruits; the console reopens the tab the player closed.
        [UnityTest]
        public IEnumerator UI130MusterOpensReinforcementsAndAReserveThenASettlementRecruits()
        {
            var fixture = new BoardFixture();
            fixture.View.Nations.First(n => n.Id == "rohan").StepsToWar = 1;
            fixture.Refresh();
            fixture.View.Availability.Add(new ActionAvailabilityView
            {
                SubjectId = "rohan", ActionId = "recruit",
                Conditions = { new AvailabilityConditionView { Code = "at-war", Description = "Rohan is 1 step from war.", Met = false } }
            });
            using (var test = new Harness(fixture))
            {
                yield return Settle();
                var panel = test.Board.Reinforcements.Root;
                Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "No Muster, no panel.");
                Click(test.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display, "UI-181: Muster unrolls nothing by itself.");
                Assert.IsTrue(test.Board.Reinforcements.Tab.ClassListContains("legal-target"), "UI-181: Muster lights the Reserve tab.");
                Assert.AreEqual("Reserve", test.Board.Reinforcements.Tab.text, "UI-181: the tab reads Reserve.");
                test.Board.Reinforcements.Open();
                yield return Settle();
                Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
                Assert.IsTrue(panel.ClassListContains("edge-left-panel"), "UI-179: a left rail panel, not a window.");
                var rohan = panel.Q("reinforcement-rohan");
                StringAssert.Contains("Rohan is 1 step from war.", rohan.tooltip, "UI-134: the reason is the row's tooltip.");
                Assert.IsNull(rohan.Q<Button>("reinforcement-advance-rohan"), "UI-179: steps toward war belong to Politics.");
                Assert.IsFalse(rohan.ClassListContains("recruitable"), "UI-179: a nation that cannot recruit is not lit.");
                Assert.IsTrue(panel.Q("reinforcement-gondor").ClassListContains("recruitable"), "A recruiting nation is lit.");
                Assert.IsFalse(rohan.Q<Button>("reserve-token-rohan-Regular").enabledSelf);
                var token = panel.Q<Button>("reserve-token-gondor-Regular");
                Assert.IsTrue(token.enabledSelf);
                Assert.AreEqual(DisplayStyle.None, panel.Q<Button>("reinforcements-finish").resolvedStyle.display,
                    "✓ appears only once a recruitment sequence is open.");
                MapContourProbe.ClickRegion(test.Board.Map, "edoras");
                yield return Settle();
                Assert.IsTrue(rohan.ClassListContains("reinforcement-focused"), "A clicked settlement marks its nation's row.");
                Assert.AreEqual(0, test.Backend.Commands, "Inspecting a settlement sends nothing.");
                LayoutTests.Save(test.Viewport, "ui130-reinforcements.png");
                MapContourProbe.ClickRegion(test.Board.Map, "edoras");
                yield return Settle();
                PointerGestureEvents.ClickAt(token);
                yield return Settle();
                Assert.IsTrue(token.ClassListContains("selected"));
                var targets = test.Board.Map.DraftOverlay.Targets.ToList();
                Assert.Contains("pelargir", targets);
                Assert.IsFalse(targets.Contains("edoras"), "Only the chosen figure's settlements light.");
                Assert.AreEqual(0, test.Backend.Commands, "Choosing a reserve sends nothing.");
                LayoutTests.Save(test.Viewport, "ui130-reserve-chosen.png");
                MapContourProbe.ClickRegion(test.Board.Map, "pelargir");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(1, test.Backend.Commands);
                Assert.AreEqual("pelargir", test.Backend.Last.Payload.Placements.Single().RegionId);
                Assert.IsTrue(test.Backend.Last.Payload.Stepwise);
                test.Reset();
                yield return Settle();
                Click(test.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                test.Board.Reinforcements.Open();
                yield return Settle();
                PointerGestureEvents.ClickAt(panel.Q<Button>("reinforcements-close"));
                yield return Settle();
                Assert.AreEqual(DisplayStyle.None, panel.resolvedStyle.display);
                PointerGestureEvents.ClickAt(test.Board.Console.Status.Plaque);
                yield return Settle();
                Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "The tray reopens Reinforcements.");
                fixture.View.Decision.Kind = DecisionKind.ActionSequence;
                fixture.View.Dice.First(d => d.Id == "muster").Location = DieLocation.Resolving;
                fixture.View.ActionSequence = new ActionSequenceView { Kind = CommandKind.Recruit, Owner = Side.FreePeoples,
                    DieId = "muster", Face = DieFace.Muster, CompletedSteps = 1, Remaining = 1 };
                fixture.View.Options.Add(new ActionOption { Kind = CommandKind.FinishActionSequence });
                test.Reset();
                yield return Settle();
                Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "The sequence keeps Reinforcements open.");
                Assert.IsTrue(panel.Q("reinforcement-budget").Children().First().ClassListContains("filled"));
                Assert.IsFalse(panel.Q("reinforcement-budget").Children().Last().ClassListContains("filled"));
                var finish = panel.Q<Button>("reinforcements-finish");
                Assert.AreEqual(DisplayStyle.Flex, finish.resolvedStyle.display);
                LayoutTests.Save(test.Viewport, "ui130-sequence.png");
                PointerGestureEvents.ClickAt(finish);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(CommandKind.FinishActionSequence, test.Backend.Last.Kind, "✓ ends the recruitment early.");
            }
        }

        // UI-179: a row is the emblem, the name and the reserves, without the political track or its actions; outside
        // a recruitment the tab shows the reserves, inert. A reserve token brings its settlements into view (UI-134);
        // the Muster die's step toward war is the Politics arrow (UI-178). UI-150: the cards stay in the hand.
        [UnityTest]
        public IEnumerator UI179RowsShowOnlyTheReservesAndTheTokensSettlements()
        {
            const string card = "base.fp.strategy.01";
            var fixture = new BoardFixture();
            var elves = fixture.View.Nations.First(n => n.Id == "elves");
            elves.Active = false;
            fixture.Refresh();
            fixture.View.Hand.Add(card);
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = card, Costs = { new EventCostOption { DieId = "muster", Face = DieFace.Muster } } } } });
            using (var test = new Harness(fixture))
            {
                yield return Settle();
                var panel = test.Board.Reinforcements.Root;
                Assert.AreSame(test.Board.Nations.Root.parent, test.Board.Reinforcements.Tab.parent, "The tab stands on the left rail.");
                Assert.AreEqual(test.Board.Nations.Root.parent.IndexOf(test.Board.Nations.Root) + 1,
                    test.Board.Reinforcements.Tab.parent.IndexOf(test.Board.Reinforcements.Tab), "Reinforcements is right under Politics.");
                test.Board.Reinforcements.Open();
                yield return Settle();
                Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "The tab shows the reserves without a recruitment.");
                Assert.IsFalse(test.Board.Reinforcements.Held);
                Assert.IsEmpty(panel.Query(className: "recruitable").ToList(), "Without a recruitment no row is lit.");
                Assert.IsTrue(panel.Query<Button>(className: "reserve-token").ToList().All(token => !token.enabledSelf));
                Assert.AreEqual(DisplayStyle.None, panel.Q("reinforcement-budget").resolvedStyle.display);
                test.Board.Reinforcements.Collapse();
                yield return Settle();

                Click(test.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                test.Board.Reinforcements.Open();
                yield return Settle();
                var row = panel.Q("reinforcement-elves");
                Assert.IsNull(row.Q(className: "reinforcement-track"), "No political track in the row.");
                Assert.IsNull(row.Q<Button>("reinforcement-advance-elves"));
                Assert.IsNull(row.Q<Button>("reinforcement-activate-elves"));
                StringAssert.DoesNotContain("to war", row.tooltip, "The row does not tell the nation's political step.");
                StringAssert.DoesNotContain("Passive", row.tooltip);
                Assert.IsEmpty(panel.Query<Label>("reinforcement-reason").ToList(), "No reason line in the row.");

                test.Board.Map.Focus("erebor");
                yield return Settle();
                PointerGestureEvents.ClickAt(panel.Q<Button>("reserve-token-gondor-Regular"));
                yield return Settle();
                var map = test.Board.Map;
                var pelargir = map.Data.Shapes["pelargir"];
                Assert.IsTrue(map.contentRect.Contains(map.Camera.ToScreen(new Vector2(pelargir.Anchor[0], pelargir.Anchor[1]))),
                    "Choosing a reserve brings its settlements into view.");
                Assert.AreEqual(0, test.Backend.Commands);
                LayoutTests.Save(test.Viewport, "ui179-reinforcements.png");

                // UI-150: the panel holds no cards; the Muster die's card stands out in the hand instead.
                Assert.IsNull(panel.Q(className: "reinforcement-cards"));
                Assert.IsNull(panel.Q<Button>("reinforcement-card-" + card));
                Assert.IsTrue(test.Board.Hand.Q<Button>(card).ClassListContains("playable-card"), "The hand offers the card.");
                Assert.AreEqual(0, test.Backend.Commands);

                test.Reset();
                yield return Settle();
                Click(test.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                test.Board.Nations.Open();
                yield return Settle();
                PointerGestureEvents.ClickAt(test.Board.Nations.Drawer.Q<Button>("nation-advance-elves"));
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(1, test.Backend.Commands, "The Politics arrow sends the step without a confirmation.");
                Assert.AreEqual(CommandKind.AdvancePolitics, test.Backend.Last.Kind);
                Assert.AreEqual("elves", test.Backend.Last.Payload.NationId);
            }
        }

        // UI-151: a Combat choice without a legal card goes as No card without a click, once per decision; while the
        // opponent chooses, No card stays in the footer, greyed out.
        [UnityTest]
        public IEnumerator UI151NoLegalCombatCardSendsNoCardOnceAndKeepsTheButton()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            view.Battle.Stage = "DefenderCard";
            view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds.Clear();
            using (var test = new Harness(view))
            {
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(1, test.Backend.Commands, "Without a legal card, No card goes without a click.");
                Assert.AreEqual(CommandKind.CommitCombatCard, test.Backend.Last.Kind);
                Assert.IsTrue(string.IsNullOrEmpty(test.Backend.Last.Payload.CardId));
                test.Push();
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(1, test.Backend.Commands, "The same decision is answered once.");
                var window = test.Board.Battles.Root;
                Assert.IsTrue(test.Board.Battles.IsOpen);
                var place = window.worldBound.position;

                test.Backend.View.Decision = null;
                test.Backend.View.DecisionOwner = Side.FreePeoples;
                test.Backend.View.Revision++;
                test.Push();
                // UI-225: the window neither disappears nor moves while the opponent chooses.
                for (var frame = 0; frame < 6; frame++)
                {
                    yield return null;
                    Assert.IsTrue(test.Board.Battles.IsOpen, "frame " + frame);
                    Assert.AreEqual(DisplayStyle.Flex, window.resolvedStyle.display, "frame " + frame);
                }
                yield return Settle();
                Assert.Less(Vector2.Distance(place, window.worldBound.position), 1f, "The battle window keeps its place after No card.");
                var none = test.Board.Tray.Q<Button>("battle-no-card");
                Assert.IsNotNull(none, "No card keeps its place while the opponent chooses.");
                Assert.IsFalse(none.enabledInHierarchy);
                Assert.AreEqual(1, test.Backend.Commands);
            }
        }

        // UI-140: a force's toggle selects every figure that can move, and leaves them all behind once they are.
        [UnityTest]
        public IEnumerator UI140ForceToggleSelectsAndDeselectsEveryMovableFigure()
        {
            using (var test = new Harness())
            {
                yield return Settle();
                var panel = test.Board.Hover.Panel.Root;
                test.Board.Hover.Open("minas-tirith");
                yield return Settle();
                var toggle = panel.Q<Button>("hover-select-all-FreePeoples-field");
                Assert.AreEqual(DisplayStyle.None, toggle.resolvedStyle.display, "Without an Army die nothing can move.");
                Click(test.Board.OwnDice.Q<Button>("army"));
                test.Board.Hover.Open("minas-tirith");
                yield return Settle();
                toggle = panel.Q<Button>("hover-select-all-FreePeoples-field");
                Assert.AreEqual(DisplayStyle.Flex, toggle.resolvedStyle.display);
                StringAssert.StartsWith("Select all", toggle.tooltip);
                var figures = panel.Query<Button>(className: "eligible-figure").ToList();
                Assert.IsNotEmpty(figures);
                Click(toggle);
                yield return Settle();
                Assert.IsTrue(figures.All(f => f.ClassListContains("selected")), "Every figure that can move is selected.");
                StringAssert.StartsWith("Deselect all", toggle.tooltip);
                Assert.IsTrue(test.Board.Map.DraftOverlay.Targets.Count > 0, "The selection lights its targets.");
                Click(toggle);
                yield return Settle();
                Assert.IsTrue(figures.All(f => !f.ClassListContains("selected")), "A second click leaves them all behind.");
                Assert.AreEqual(0, test.Backend.Commands, "Selecting sends nothing.");
                LayoutTests.Save(test.Viewport, "ui140-select-all.png");
            }
        }

        // UI-136: the chosen die puts a halo on every control it can use, read from the server's options.
        [UnityTest]
        public IEnumerator UI136ChosenDieLightsTheControlsItCanUse()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice.Add(new DieView { Id = "will", Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available });
            fixture.Refresh();
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.MoveFellowship, EligibleDice = { "will" } });
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.EnterCharacter,
                CharacterActions = { new CharacterActionOption { CharacterId = "base.character.gandalf-white", Dice = { "will" } } } });
            using (var test = new Harness(fixture))
            {
                yield return Settle();
                var characters = test.Board.Characters.Tab;
                var politics = test.Board.Nations.Root;
                var fellowship = test.Board.Fellowship.Status;
                Assert.IsFalse(characters.ClassListContains(DieOptionGlow.LitClass) || politics.ClassListContains(DieOptionGlow.LitClass),
                    "Nothing glows before a die is chosen.");
                Click(test.Board.OwnDice.Q<Button>("muster"));
                yield return Settle();
                Assert.IsTrue(politics.ClassListContains(DieOptionGlow.LitClass), "Muster can move a nation on the track.");
                Assert.IsFalse(characters.ClassListContains(DieOptionGlow.LitClass));
                Assert.IsFalse(fellowship.ClassListContains(DieOptionGlow.LitClass));
                // UI-193: a rail tab has no halo; its medallion glows gold.
                Assert.IsNull(politics.Q(DieOptionGlow.HaloClass));
                Assert.AreEqual("edge-tab-available", politics.resolvedStyle.backgroundImage.texture?.name);
                Assert.AreEqual("edge-tab", characters.resolvedStyle.backgroundImage.texture?.name);
                test.Reset();
                yield return Settle();
                Click(test.Board.OwnDice.Q<Button>("will"));
                yield return Settle();
                Assert.IsTrue(fellowship.ClassListContains(DieOptionGlow.LitClass), "Will of the West can move the Fellowship.");
                Assert.IsTrue(characters.ClassListContains(DieOptionGlow.LitClass), "It can also bring in a character.");
                Assert.AreEqual(DisplayStyle.Flex, fellowship.Q(DieOptionGlow.HaloClass).resolvedStyle.display,
                    "The Fellowship plaque keeps its halo.");
                LayoutTests.Save(test.Viewport, "ui136-will-options.png");
            }
        }

        [UnityTest]
        public IEnumerator ArmyDragAndEscapeCannotSpendTheSelectedDie()
        {
            using var test = new Harness();
            yield return Settle();
            yield return SelectArmy(test.Board, "minas-tirith");
            yield return Settle();
            var selected = test.Board.Hover.Panel.Root.Query<Button>(className: "selected").ToList().Select(b => b.name).ToArray();
            MapContourProbe.AttemptDrag(test.Board, test.Board.Map.Q<Button>("army-minas-tirith-FreePeoples"), test.Board.Phase.worldBound.center);
            yield return null;
            CollectionAssert.AreEqual(selected, test.Board.Hover.Panel.Root.Query<Button>(className: "selected").ToList().Select(b => b.name));
            Assert.AreEqual(0, test.Backend.Commands);
            for (var i = 0; i < 6; i++)
            {
                using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                    test.Board.Root.SendEvent(down);
                using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                    test.Board.Root.SendEvent(up);
                yield return null;
            }
            Assert.IsEmpty(test.Board.ActionDie.DieId);
            Assert.IsEmpty(test.Board.Map.DraftOverlay.Targets);
            Assert.AreEqual(0, test.Backend.Commands);
        }

        [UnityTest]
        public IEnumerator OtherArmyReplacesPendingSelectionAndRecruitmentSendsOnePlacement()
        {
            using (var test = new Harness())
            {
                yield return Settle();
                yield return SelectArmy(test.Board, "minas-tirith");
                yield return Settle();
                yield return MatchTestRoom.Wait(ArmyGestures.All(test.Board, "dale"));
                yield return Settle();
                Assert.AreEqual(0, test.Backend.Commands);
                MapContourProbe.ClickRegion(test.Board.Map, "erebor");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.NotNull(test.Backend.Last, "Target result: " + string.Join(" | ", test.Board.Tray.Query<Label>().ToList().Select(l => l.text)));
                Assert.AreEqual(1, test.Backend.Last.Payload.Moves.Count);
                Assert.AreEqual("dale", test.Backend.Last.Payload.Moves[0].From);
                Assert.AreEqual("erebor", test.Backend.Last.Payload.Moves[0].To);
                test.Reset();
                yield return Settle();
                OpenReserve(test.Board, "pelargir");
                yield return Settle();
                var regular = test.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Regular");
                Click(regular);
                yield return Settle();
                Assert.IsTrue(test.Board.Map.DraftOverlay.Targets.Contains("pelargir"));
                Assert.IsNull(test.Board.Tray.Q<Button>("confirm-decision"));
                test.Board.Hover.Close();
                MapContourProbe.ClickRegion(test.Board.Map, "pelargir");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.AreEqual(2, test.Backend.Commands);
                Assert.AreEqual(1, test.Backend.Last.Payload.Placements.Count);
                Assert.IsTrue(test.Backend.Last.Payload.Stepwise);
                Assert.AreEqual("muster", test.Backend.Last.Payload.DieId);
            }
        }

        [UnityTest]
        public IEnumerator ExtremeMixedArmyAndReservesFitAtBothResolutionsAndLargerScale()
        {
            using (var test = new Harness(true))
            {
                var document = test.Bootstrap.GetComponent<UIDocument>();
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        document.panelSettings.targetTexture = target;
                        document.panelSettings.scale = scale;
                        test.Reset();
                        yield return Settle();
                        test.Board.Map.Fit();
                        yield return Settle();
                        Click(test.Board.OwnDice.Q<Button>("army"));
                        yield return Settle();
                        Assert.AreEqual("army", test.Board.ActionDie.DieId);
                        yield return MatchTestRoom.Wait(ArmyGestures.All(test.Board, "minas-tirith"));
                        yield return Settle();
                        var figures = test.Board.Hover.Panel.Root.Query<Button>().ToList();
                        Assert.GreaterOrEqual(figures.Count, 30, "Stress fixture includes all FP leaders and eight character slots.");
                        var first = figures.First(b => b.userData is DraftToken { Object: DraftObject.Figure } && b.worldBound.width > 0 &&
                            b.parent.parent.resolvedStyle.display != DisplayStyle.None);
                        LayoutTests.Save(target, $"ui17-army-{width}-{scale:0.00}.png");
                        Assert.AreEqual(DisplayStyle.Flex, test.Board.Hover.Panel.Root.resolvedStyle.display);
                        Assert.IsTrue(first.ClassListContains("selected"), "One click selects the complete force before correcting it.");
                        var before = first.worldBound;
                        var started = Time.realtimeSinceStartup;
                        Click(first);
                        Assert.Less(Time.realtimeSinceStartup - started, .1f);
                        Assert.IsFalse(first.ClassListContains("selected"));
                        Assert.AreSame(first, test.Board.Hover.Panel.Root.Q<Button>(first.name));
                        Assert.AreEqual(before, first.worldBound);
                        LayoutTests.Save(target, $"ui17-army-{width}-{scale:0.00}.png");
                        CheckBounds(test.Board);
                        test.Reset();
                        yield return Settle();
                        OpenReserve(test.Board, "minas-tirith");
                        yield return Settle();
                        CheckContextBounds(test.Board.Hover.Panel.Root, test.Board.Map);
                        LayoutTests.Save(target, $"ui20-dense-recruit-{width}-{scale:0.00}.png");
                        test.Board.Hover.Open("pelargir");
                        yield return Settle();
                        LayoutTests.Save(target, $"ui20-recruit-{width}-{scale:0.00}.png");
                        CheckBounds(test.Board);
                        CheckContextBounds(test.Board.Hover.Panel.Root, test.Board.Map);
                        // UI-135: the region panel no longer holds a reserve palette; Reinforcements does.
                        Assert.IsNull(test.Board.Hover.Panel.Root.Q<Button>("reserve-gondor-Regular"));
                        var regular = test.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Regular");
                        Click(regular);
                        yield return Settle();
                        Assert.AreSame(regular, test.Board.Reinforcements.Root.Q<Button>(regular.name));
                        Assert.IsTrue(regular.ClassListContains("selected"));
                        Assert.IsTrue(test.Board.Map.DraftOverlay.Targets.Contains("pelargir"));
                        test.Board.Hover.Close();
                        test.Board.Nations.Open();
                        yield return Settle();
                        CheckContextBounds(test.Board.Nations.Drawer, test.Board.Map);
                        Assert.IsTrue(test.Board.Nations.Drawer.Q<Button>("political-slot-elves-1").enabledSelf);
                        LayoutTests.Save(target, $"ui20-politics-{width}-{scale:0.00}.png");
                        document.panelSettings.scale = 1;
                        document.panelSettings.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                    }
            }
        }

        [UnityTest]
        public IEnumerator TemporaryExcessProjectionLocksArrivalsAndRemovalUsesIndividualFigures()
        {
            var fixture = new BoardFixture(true);
            fixture.Put("gondor", UnitKind.Regular, "osgiliath", 9);
            var arrivals = fixture.View.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id)
                .Concat(fixture.View.Characters.Where(c => c.Location == "minas-tirith").Select(c => c.Id)).ToList();
            foreach (var unit in fixture.View.Units.Where(u => arrivals.Contains(u.Id)))
                unit.Location = "osgiliath";
            foreach (var character in fixture.View.Characters.Where(c => arrivals.Contains(c.Id)))
                character.Location = "osgiliath";
            fixture.Refresh();
            fixture.View.Decision.Kind = DecisionKind.ActionSequence;
            fixture.View.Dice[0].Location = DieLocation.Resolving;
            fixture.View.ActionSequence = new ActionSequenceView { Kind = CommandKind.MoveArmies, Owner = Side.FreePeoples,
                DieId = "army", Face = DieFace.Army, CompletedSteps = 1, Remaining = 1, UsedIds = arrivals };
            var movement = fixture.View.Options.First(o => o.Kind == CommandKind.MoveArmies);
            foreach (var army in movement.Armies)
            {
                army.UnitIds.RemoveAll(arrivals.Contains);
                army.CharacterIds.RemoveAll(arrivals.Contains);
            }
            using var test = new Harness(fixture);
            test.Bootstrap.GetComponent<UIDocument>().panelSettings.scale = 1.25f;
            yield return Settle();
            yield return MatchTestRoom.Wait(ArmyGestures.All(test.Board, "osgiliath"));
            yield return Settle();
            foreach (var id in arrivals)
                Assert.IsFalse(test.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id).ClassListContains("eligible-figure"));
            LayoutTests.Save(test.Bootstrap.GetComponent<UIDocument>().panelSettings.targetTexture, "ui17-merged-1366-1.25.png");
            CheckBounds(test.Board);
            var candidates = fixture.View.Units.Where(u => u.Location == "osgiliath" && ArmyDraft.IsCombat(u)).Select(u => u.Id).ToList();
            fixture.View.ActionSequence = null;
            fixture.View.Decision = new DecisionView { Kind = DecisionKind.RemoveExcess, RequiredCount = 1 };
            fixture.View.Options.Clear();
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.RemoveExcess, EligibleIds = candidates, Minimum = 1, Maximum = 1 });
            test.Reset();
            yield return Settle();
            Assert.IsTrue(test.Board.OwnDice.Q<Button>("army").ClassListContains("paid-die"));
            Assert.IsFalse(test.Board.OwnDice.Q<Button>("army").enabledSelf);
            // UI-215: the excess is chosen in the region panel; a single figure is sent with its click.
            test.Board.Map.Select("osgiliath");
            yield return Settle();
            var figure = test.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + candidates[0]);
            Assert.IsTrue(figure.ClassListContains("eligible-figure"));
            Assert.IsFalse(test.Board.DecisionWindow.IsOpen);
            CheckBounds(test.Board);
            Click(figure);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(CommandKind.RemoveExcess, test.Backend.Last.Kind);
            CollectionAssert.AreEqual(new[] { candidates[0] }, test.Backend.Last.Payload.Ids);
        }

        // UI-254: excess units are a decision the game waits on. The red notice names it; the console's plaque focuses the
        // region and opens its panel, which stays while the pointer is back on the console; the plaque closes it and the
        // notice brings it back; the figures and ✓ resolve it.
        [UnityTest]
        public IEnumerator RequiredExcessOpensItsRegionPanelFromThePlaqueAndTheNotice()
        {
            var fixture = new BoardFixture(true);
            fixture.Put("gondor", UnitKind.Regular, "osgiliath", 9);
            fixture.Refresh();
            var candidates = fixture.View.Units.Where(u => u.Location == "osgiliath" && ArmyDraft.IsCombat(u)).Select(u => u.Id).ToList();
            fixture.View.ActionSequence = null;
            fixture.View.Decision = new DecisionView { Kind = DecisionKind.RemoveExcess, RequiredCount = 2 };
            fixture.View.Options.Clear();
            fixture.View.Options.Add(new ActionOption { Kind = CommandKind.RemoveExcess, Enabled = true, EligibleIds = candidates, Minimum = 2, Maximum = 2 });
            using var test = new Harness(fixture);
            yield return Settle();
            var board = test.Board;
            Assert.IsTrue(board.Notice.Required, "Excess units raise the required notice.");
            StringAssert.Contains("Return 2 excess units", board.Notice.Button.text);
            board.Map.Fit();
            yield return Settle();
            PointerGestureEvents.ClickAt(board.Console.Status.Plaque);
            yield return Settle();
            Assert.AreEqual("osgiliath", board.Map.Selected, "The plaque focuses the region.");
            Assert.IsTrue(board.Hover.HoldsDecision, "The plaque opens the region panel for the decision.");
            Assert.IsTrue(board.Hover.Panel.Root.Q<Button>("hover-figure-" + candidates[0]).ClassListContains("eligible-figure"));
            Assert.IsFalse(board.DecisionWindow.IsOpen, "No new window: the region panel is the decision's host.");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display, "The pointer on the console keeps the panel.");
            PointerGestureEvents.ClickAt(board.Console.Status.Plaque);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display, "The plaque closes the panel it opened.");
            PointerGestureEvents.ClickAt(board.Notice.Button);
            yield return Settle();
            Assert.IsTrue(board.Hover.HoldsDecision, "The notice opens the same panel.");
            Click(board.Hover.Panel.Root.Q<Button>("hover-figure-" + candidates[0]));
            yield return Settle();
            Click(board.Hover.Panel.Root.Q<Button>("hover-figure-" + candidates[1]));
            yield return Settle();
            Assert.IsTrue(board.Hover.HoldsDecision, "Choosing figures keeps the panel.");
            var confirm = board.Console.Actions.Confirm;
            for (var frame = 0; frame < 120 && !confirm.enabledSelf; frame++)
                yield return null;
            Assert.IsTrue(confirm.enabledSelf, confirm.tooltip + " · " + board.Console.Status.Hint.text);
            PointerGestureEvents.ClickAt(confirm);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(CommandKind.RemoveExcess, test.Backend.Last.Kind);
            CollectionAssert.AreEquivalent(candidates.Take(2), test.Backend.Last.Payload.Ids);
        }

        // UI-259: the board's chat field shows what is typed — light text on a dark field with a clear contrast.
        [UnityTest]
        public IEnumerator ChatInputShowsTypedTextWithReadableContrast()
        {
            using var test = new Harness();
            yield return Settle();
            var chat = test.Board.GameChat;
            chat.Available(true);
            chat.Open();
            chat.Chat.Input.value = "Hello from Rivendell";
            yield return Settle();
            var field = chat.Chat.Input.Q(className: "unity-base-text-field__input");
            var text = field.Q<TextElement>();
            var contrast = Contrast(text.resolvedStyle.color, field.resolvedStyle.backgroundColor);
            Assert.GreaterOrEqual(contrast, 7f, "Typed chat text reads against its field.");
            Assert.Greater(text.resolvedStyle.color.grayscale, field.resolvedStyle.backgroundColor.grayscale, "Light text on a dark field.");
        }

        private static float Contrast(Color a, Color b)
        {
            var light = Mathf.Max(Luminance(a), Luminance(b));
            var dark = Mathf.Min(Luminance(a), Luminance(b));
            return (light + 0.05f) / (dark + 0.05f);
        }

        private static float Luminance(Color color) => 0.2126f * color.linear.r + 0.7152f * color.linear.g + 0.0722f * color.linear.b;

        [UnityTest]
        public IEnumerator EliteDragAndPoliticalEmblemNeedOnlyOneCommitEach()
        {
            using (var test = new Harness())
            {
                yield return Settle();
                MapContourProbe.ReachRegion(test.Board.Map, "minas-tirith");
                yield return Settle();
                OpenReserve(test.Board, "pelargir");
                yield return Settle();
                test.Board.Hover.Close();
                yield return DragToRegion(test.Board, test.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Elite"), "minas-tirith");
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(1, test.Backend.Commands);
                Assert.AreEqual(1, test.Backend.Last.Payload.Placements.Count);
                var unit = test.Backend.View.Units.First(u => u.Id == test.Backend.Last.Payload.Placements[0].UnitId);
                Assert.AreEqual(UnitKind.Elite, unit.Kind);
                test.Reset();
                yield return Settle();
                Click(test.Board.OwnDice.Q<Button>("muster"));
                test.Board.Nations.Open();
                yield return Settle();
                Click(test.Board.Nations.Drawer.Q<Button>("political-slot-elves-1"));
                yield return new WaitForSeconds(.2f);
                Assert.AreEqual(2, test.Backend.Commands);
                Assert.AreEqual("elves", test.Backend.Last.Payload.NationId);
            }
        }

        [UnityTest]
        public IEnumerator AmbiguousCostsAndEmptyReservesRemainExplicitWithoutChangingTheForce()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice.Add(new DieView { Id = "wild", Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available });
            fixture.View.Dice.Add(new DieView { Id = "special", Face = DieFace.Army, Source = "fixture-special", Location = DieLocation.Available });
            foreach (var unit in fixture.View.Units.Where(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve"))
                unit.Location = "casualties";
            fixture.Refresh();
            using (var test = new Harness(fixture))
            {
                yield return Settle();
                var marker = test.Board.Map.Q<Button>("army-minas-tirith-FreePeoples");
                yield return MatchTestRoom.Wait(MapContourProbe.ReachMarker(test.Board.Map, marker, "minas-tirith"));
                PointerGestureEvents.ClickAt(marker);
                yield return Settle();
                Assert.IsEmpty(test.Board.OwnDice.Query<Button>(className: "selected").ToList(), "No strategic cost is selected automatically.");
                Assert.IsTrue(test.Board.OwnDice.Q<Button>("special").ClassListContains("special-die"));
                StringAssert.Contains("Fixture Special", test.Board.OwnDice.Q<Button>("special").tooltip);
                Assert.IsEmpty(test.Board.Hover.Panel.Root.Query<Button>(className: "selected").ToList());
                Click(test.Board.OwnDice.Q<Button>("wild"));
                yield return Settle();
                yield return MatchTestRoom.Wait(ArmyGestures.All(test.Board, "minas-tirith"));
                yield return Settle();
                Assert.IsNotEmpty(test.Board.Hover.Panel.Root.Query<Button>(className: "selected").ToList());
                MapContourProbe.ClickRegion(test.Board.Map, "osgiliath");
                yield return new WaitForSeconds(.2f);
                CheckBounds(test.Board);
                Assert.AreEqual("wild", test.Backend.Last.Payload.DieId);
                Assert.AreEqual(DieFace.Army, test.Backend.Last.Payload.MovementCost);
                test.Reset();
                yield return Settle();
                OpenReserve(test.Board, "pelargir");
                yield return Settle();
                var empty = test.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Regular");
                Assert.IsFalse(empty.enabledSelf);
                Assert.AreEqual("0", empty.Q<Label>("reserve-token-count").text);
                Click(empty);
                Assert.AreEqual(1, test.Backend.Commands);
                Assert.IsNull(test.Board.Tray.Q<Button>("confirm-decision"));
            }
        }

        private static void OpenReserve(BoardScreen board, string region)
        {
            Click(board.OwnDice.Q<Button>("muster"));
            board.Hover.Open(region);
        }

        private static void CheckContextBounds(VisualElement panel, VisualElement map)
        {
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display);
            Assert.IsEmpty(panel.Query<ScrollView>().ToList());
            Assert.IsEmpty(panel.Query<TextField>().ToList());
            Assert.LessOrEqual(panel.worldBound.xMax, map.worldBound.xMax + 1);
            Assert.LessOrEqual(panel.worldBound.yMax, map.worldBound.yMax + 1);
            Assert.GreaterOrEqual(panel.worldBound.xMin, map.worldBound.xMin - 1);
            Assert.GreaterOrEqual(panel.worldBound.yMin, map.worldBound.yMin - 1);
        }

        // Leaving the selected army's region panel closes it, so the target's markers take their full size;
        // click terrain that stays exposed then, as a player would (see ArmyGestures.Target).
        private static IEnumerator HoverRegion(BoardScreen board, string region)
        {
            board.Hover.Close();
            yield return Settle();
            MapContourProbe.ReachRegion(board.Map, region);
            MapContourProbe.Hover(board.Root, MapContourProbe.Anchor(board.Map, region));
            yield return Settle();
        }

        private static IEnumerator SelectArmy(BoardScreen board, string region)
        {
            Click(board.OwnDice.Q<Button>("army"));
            yield return MatchTestRoom.Wait(ArmyGestures.All(board, region));
        }

        private static void CheckBounds(BoardScreen board)
        {
            Assert.IsEmpty(board.Tray.Query<ScrollView>().ToList());
            Assert.IsEmpty(board.Tray.Query<TextField>().ToList());
            foreach (var button in board.Tray.Query<Button>().ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None))
            {
                Assert.LessOrEqual(button.worldBound.xMax, board.Tray.worldBound.xMax + 1, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, board.Root.worldBound.yMax + 1, button.name + ": " + button.text);
            }
            Assert.Greater(board.Map.worldBound.height, board.Tray.worldBound.height);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }

        private static void Click(VisualElement target) => CardNetworkTests.Click(target);

        private static IEnumerator DragToRegion(BoardScreen board, VisualElement source, string region)
        {
            BeginDrag(board, source);
            yield return null;
            var destination = MapContourProbe.Terrain(board.Map, region);
            Assert.AreSame(board.Map, board.Map.panel.Pick(destination));
            MapContourProbe.Drop(board, destination);
            yield return null;
        }

        private static void BeginDrag(BoardScreen board, VisualElement source)
        {
            Assert.IsNotNull(source);
            var picked = source.panel.Pick(source.worldBound.center);
            Assert.IsTrue(picked == source || source.Contains(picked), "Drag source is covered by " + picked?.name);
            MapContourProbe.BeginDrag(board, source);
        }

        private sealed class Harness : IDisposable
        {
            public ClientBootstrap Bootstrap { get; }
            public BoardScreen Board => Bootstrap.BoardScreen;
            public FixtureBackend Backend { get; }
            private readonly GameApi api;
            private readonly MatchConnection connection;
            private TurnInteraction interaction;
            private readonly RenderTexture viewport;
            public RenderTexture Viewport => viewport;
            public Harness(bool stress = false) : this(new BoardFixture(stress)) { }
            public Harness(BoardFixture fixture) : this(fixture.View) { }
            public Harness(PlayerView view)
            {
                Bootstrap = Object.FindAnyObjectByType<ClientBootstrap>();
                if (Bootstrap == null)
                    Bootstrap = new GameObject("Board interaction test").AddComponent<ClientBootstrap>();
                Bootstrap.ShowFixture();
                var panel = Bootstrap.GetComponent<UIDocument>().panelSettings;
                panel.scale = 1;
                viewport = new RenderTexture(1366, 768, 24);
                viewport.Create();
                panel.targetTexture = viewport;
                Backend = new FixtureBackend(view);
                api = new GameApi("http://127.0.0.1:5088", Backend);
                connection = new MatchConnection(api, Backend.View.MatchId);
                connection.Updated += Update;
                Reset();
            }
            public void Reset()
            {
                // Independent layout fixtures reuse revision zero; a reset starts a new controller.
                interaction?.Dispose();
                interaction = new TurnInteraction(api, connection, Board);
                Backend.View.Decision.Id = Guid.NewGuid().ToString("N");
                Update(JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(Backend.View)));
                interaction.SetConnection("Connected");
            }
            private void Update(PlayerView view) { Board.Apply(view); interaction.Update(view); }
            // Delivers the backend's current view, as a server push would.
            public void Push() => Update(JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(Backend.View)));
            public void Dispose()
            {
                interaction.Dispose();
                connection.Dispose();
                api.Dispose();
                var panel = Bootstrap.GetComponent<UIDocument>().panelSettings;
                panel.scale = 1;
                if (panel.targetTexture != null)
                {
                    panel.targetTexture.Release();
                    if (panel.targetTexture != viewport)
                        Object.Destroy(panel.targetTexture);
                    panel.targetTexture = null;
                }
                viewport.Release();
                Object.Destroy(viewport);
            }
        }

        // Isolates UI; real server enforcement is covered by T12 and the T13 integration sequence.
        private sealed class FixtureBackend : HttpMessageHandler
        {
            public PlayerView View { get; }
            public int Commands { get; private set; }
            public GameCommand Last { get; private set; }
            public FixtureBackend(PlayerView view) { View = view; }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                if (request.RequestUri.AbsolutePath.EndsWith("/preview"))
                {
                    await Task.Delay(60, cancellation);
                    return Reply(new ActionPreview { Legal = true, Revision = View.Revision });
                }
                if (request.RequestUri.AbsolutePath.EndsWith("/commands"))
                {
                    Last = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    Commands++;
                    return Reply(new CommandReceipt { Accepted = true, CommandId = Last.CommandId, Revision = View.Revision });
                }
                return Reply(View);
            }
            private static HttpResponseMessage Reply(object value) => new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(JsonConvert.SerializeObject(value)) };
        }
    }
}
