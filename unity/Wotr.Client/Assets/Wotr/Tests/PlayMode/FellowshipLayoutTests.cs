using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class FellowshipLayoutTests
    {
        [UnityTest]
        public IEnumerator GuideModeShowsItsConsequenceAndKeepsAllPortraitsReachable()
        {
            using var client = new MountedMatchClient("http://localhost", "guide-layout");
            var view = Screen(CommandKind.SelectGuide);
            view.Hunt = null;
            view.Phase = GamePhase.Fellowship;
            view.Decision.Kind = DecisionKind.FellowshipPhase;
            view.Options[0].EligibleIds.AddRange(new[] { "base.character.gandalf-grey", "base.character.strider" });
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    view.Decision.Id = $"guide-{width}-{scale}";
                    client.Apply(view);
                    client.SetOnline();
                    client.Board.Fellowship.Open();
                    yield return Settle();
                    PointerGestureEvents.ClickAt(client.Board.Fellowship.Root.Q<Button>("fellowship-change-guide"));
                    yield return Settle();
                    var hint = client.Board.Fellowship.Root.Q<Label>("fellowship-guide-hint");
                    Assert.AreEqual("Choosing a guide ends the Fellowship phase", hint.text);
                    foreach (var portrait in client.Board.Fellowship.Root.Query<Button>(className: "companion-token").ToList())
                    {
                        var picked = portrait.panel.Pick(portrait.worldBound.center);
                        Assert.IsTrue(picked == portrait || portrait.Contains(picked), "Covered portrait: " + portrait.name);
                        Assert.IsTrue(portrait.enabledInHierarchy, "Non-candidates remain readable and focusable.");
                        AssertPortraitTextFits(portrait.Q<Label>("portrait-name"), portrait);
                    }
                    var guide = client.Board.Fellowship.Root.Q<Button>("fellowship-portrait-" + view.Fellowship.Guide);
                    var badge = guide.Q<Label>("guide-badge");
                    Assert.AreEqual(DisplayStyle.Flex, badge.resolvedStyle.display);
                    AssertPortraitTextFits(badge, guide);
                    Assert.IsFalse(guide.Q<Label>("portrait-name").worldBound.Overlaps(badge.worldBound),
                        "The guide badge and the figure's name must remain separately readable.");
                    // UI-187: the Guide's ability is one of the portrait's short ability lines.
                    StringAssert.Contains("Guide · ", guide.tooltip);
                    var measured = hint.MeasureTextSize(hint.text, hint.contentRect.width, VisualElement.MeasureMode.Exactly, 0,
                        VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(measured.y, hint.contentRect.height + 1);
                    Check(client.Board);
                    LayoutTests.Save(target, $"ui34-guide-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        private static void AssertPortraitTextFits(Label label, Button portrait)
        {
            Assert.IsNotEmpty(label.text, portrait.name);
            Assert.IsFalse(label.isElided, "Elided portrait text: " + label.text);
            Assert.GreaterOrEqual(label.worldBound.xMin, portrait.worldBound.xMin - 1, label.name);
            Assert.LessOrEqual(label.worldBound.xMax, portrait.worldBound.xMax + 1, label.name);
            Assert.GreaterOrEqual(label.worldBound.yMin, portrait.worldBound.yMin - 1, label.name);
            Assert.LessOrEqual(label.worldBound.yMax, portrait.worldBound.yMax + 1, label.name);
            var fullText = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined,
                0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(fullText.x, label.contentRect.width + 1, "Truncated portrait text: " + label.text);
            Assert.LessOrEqual(fullText.y, label.contentRect.height + 1, "Clipped portrait text: " + label.text);
        }

        [UnityTest]
        public IEnumerator NamedStatusAndActionsRemainStableAndReadableAtEveryLayout()
        {
            using var client = new MountedMatchClient("http://localhost", "panel-layout");
            var view = Screen(CommandKind.MoveFellowship);
            view.Hunt = null;
            view.Phase = GamePhase.ActionResolution;
            view.Options.Single().EligibleDice.Add("character");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    client.Apply(view);
                    client.Board.Fellowship.Open();
                    yield return Settle();
                    var panel = client.Board.Fellowship.Root;
                    // UI-110: the progress track's symbol is the move; without a die it neither glows nor clicks.
                    var move = panel.Q<Button>("fellowship-progress");
                    Assert.AreSame(panel.Q("journey-track"), move.parent);
                    Assert.IsFalse(move.ClassListContains(FellowshipTrackRow.MoveClass));
                    Assert.IsFalse(move.enabledInHierarchy);
                    Assert.IsFalse(panel.Query<Button>().ToList().Any(b => b.text.StartsWith("Move") || b.text.StartsWith("Attempt move")),
                        "No separate Move button.");
                    StringAssert.Contains("Select a Character die", panel.Q<Label>("fellowship-action-hint").text);
                    Assert.AreEqual("11", client.Board.Fellowship.Status.Q<Label>("fellowship-status-corruption").text);
                    // UI-98: hovering the plaque unrolls the panel, so the plaque has no tooltip; the track shows 11.
                    Assert.IsEmpty(client.Board.Fellowship.Status.tooltip);
                    Assert.AreEqual("11", panel.Q<Label>("corruption-label").text);
                    StringAssert.Contains("Last known position", panel.Q<Label>("fellowship-location").tooltip);
                    Assert.IsNotEmpty(panel.Q<Label>("fellowship-location").text);
                    // UI-90: the condition and position row is the header's locate button.
                    var locate = panel.Q<Button>("focus-fellowship");
                    Assert.IsTrue(locate.Contains(panel.Q<Label>("fellowship-location")));
                    Assert.IsTrue(locate.parent.ClassListContains("window-header"));
                    StringAssert.Contains("Click to center the map", locate.tooltip);
                    var condition = panel.Q<Label>("fellowship-condition");
                    Assert.LessOrEqual(condition.MeasureTextSize(condition.text, 0, VisualElement.MeasureMode.Undefined, 0,
                        VisualElement.MeasureMode.Undefined).x, condition.contentRect.width + 1, "The condition must fit the locate button.");
                    foreach (var element in panel.Query<VisualElement>().ToList().Where(Visible).Where(e => e is Button || e is Label))
                    {
                        Assert.GreaterOrEqual(element.worldBound.xMin, panel.worldBound.xMin - 1, element.name);
                        Assert.LessOrEqual(element.worldBound.xMax, panel.worldBound.xMax + 1, element.name);
                        Assert.LessOrEqual(element.worldBound.yMax, client.Board.Map.worldBound.yMax + 1, element.name);
                        if (element is TextElement text && text.text.Length > 0)
                        {
                            var measured = text.MeasureTextSize(text.text, text.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                            Assert.LessOrEqual(measured.y, text.contentRect.height + 1, "Clipped: " + text.text);
                        }
                    }
                    Check(client.Board);
                    var stable = panel.Q<Label>("corruption-label");
                    client.Board.Apply(view);
                    yield return Settle();
                    Assert.AreSame(stable, panel.Q<Label>("corruption-label"));
                    Assert.AreSame(move, panel.Q<Button>("fellowship-progress"));
                    LayoutTests.Save(texture, $"ui34-panel-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        [UnityTest]
        public IEnumerator FellowshipGroupsHuntAndMordorFitBothSizesAndScalesWithoutScrolling()
        {
            // Protocol/layout fixtures. Rule outcomes are verified by FellowshipNetworkTests.
            using (var client = new MountedMatchClient("http://localhost", "layout"))
            {
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var kind in new[] { CommandKind.SeparateCompanions, CommandKind.MoveCompanions,
                            CommandKind.RevealFellowship, CommandKind.RerollHunt, CommandKind.ResolveHuntDamage, CommandKind.RespondEffect })
                        {
                            var view = Screen(kind);
                            client.Apply(view);
                            client.SetOnline();
                            client.Board.Fellowship.Open();
                            yield return Settle();
                            if (kind == CommandKind.SeparateCompanions)
                            {
                                FellowshipGestures.Portrait(client, view.OwnCompanions[0].Id);
                                yield return Settle();
                            }
                            if (kind == CommandKind.MoveCompanions)
                                yield return AllGroups(client, view);
                            LayoutTests.Save(target, $"fellowship-{kind}-{width}-{scale:0.00}.png");
                            Check(client.Board);
                            foreach (var portrait in client.Board.Fellowship.Root.Query<Button>(className: "companion-token").ToList())
                                Assert.AreEqual(view.You == Side.FreePeoples, portrait.tooltip.Contains("Level"), "Only projected private companion statistics may be shown.");
                            if (kind == CommandKind.ResolveHuntDamage)
                            {
                                Assert.AreEqual(0, client.Board.Tray.Query<Button>(className: "companion-portrait").ToList().Count);
                                Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"));
                                Assert.IsTrue(client.Board.Fellowship.Root.Q<Label>("mordor-step-4").ClassListContains("selected"));
                                Assert.AreEqual(DisplayStyle.None, client.Board.Victory.Banner.style.display.value);
                                LayoutTests.Save(target, $"mordor-{width}-{scale:0.00}.png");
                            }
                            if (kind == CommandKind.MoveCompanions)
                                LayoutTests.Save(target, $"companions-{width}-{scale:0.00}.png");
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
            }
        }

        [UnityTest]
        public IEnumerator RevealRouteKeepsDistinctPathsAndHuntContextWhileInspectingTheMap()
        {
            var view = Screen(CommandKind.RevealFellowship);
            using (var client = new MountedMatchClient("http://localhost", view.MatchId))
            {
                client.Apply(view);
                client.Board.Fellowship.Open();
                yield return Settle();
                MapContourProbe.ClickRegion(client.Board.Map, "dimrill-dale");
                yield return Settle();
                StringAssert.Contains("Dimrill Dale", client.Board.Tray.Q<Label>("travel-summary").text);
                MapContourProbe.ClickRegion(client.Board.Map, "north-anduin-vale");
                yield return Settle();
                MapContourProbe.ClickRegion(client.Board.Map, "dol-guldur");
                yield return Settle();
                StringAssert.Contains("Moria → Dimrill Dale → North Anduin Vale → Dol Guldur", client.Board.Tray.Q<Label>("travel-summary").tooltip);
                // UI-219: the whole Progress is used, so no step is left (online this step would already have moved).
                Assert.IsEmpty(client.Board.Map.DraftOverlay.Targets);
                Assert.IsNotNull(client.Board.Tray.Q<Label>("hunt-tile"));
                client.Board.Cards.Show(view.Hand[0], true);
                var camera = client.Board.Map.Camera;
                camera.Pan += new Vector2(70, 30);
                client.Board.Map.Refresh();
                yield return Settle();
                Assert.IsTrue(client.Board.Cards.Pinned);
                Assert.IsNotNull(client.Board.Tray.Q<Button>("confirm-decision"), "UI-219: a drawn route is sent with ✓.");
                PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>("route-undo"));
                yield return Settle();
                StringAssert.Contains("North Anduin Vale", client.Board.Tray.Q<Label>("travel-summary").text);
                Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains("dol-guldur"));
            }
        }

        [UnityTest]
        public IEnumerator RegionalRouteKeepsAllEightNeighborsReachableBesidePinnedCard()
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var view = Screen(CommandKind.RevealFellowship);
                    view.Fellowship.RegionId = "old-forest-road";
                    var travel = view.Options.Single().Travel.Single();
                    travel.Origin = view.Fellowship.RegionId;
                    travel.Range = 1;
                    using (var client = new MountedMatchClient("http://localhost", view.MatchId))
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 0);
                        client.Panel.targetTexture = target;
                        client.Interface.Preferences.SetScale(scale);
                        var neighbors = client.Board.Presentation.Regions[travel.Origin].Neighbors;
                        travel.Targets = neighbors.Concat(new[] { travel.Origin }).ToList();
                        Assert.AreEqual(9, travel.Targets.Count);
                        client.Apply(view);
                        client.Board.Fellowship.Open();
                        client.Board.Cards.Show(view.Hand[0], true);
                        yield return Settle();
                        client.Board.Map.Fit();
                        yield return SettlePinnedCard(client.Board, width, scale);
                        var cardBounds = client.Board.Cards.Root.worldBound;
                        foreach (var region in travel.Targets)
                        {
                            Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains(region), region);
                            var point = MapContourProbe.ReachRegion(client.Board.Map, region);
                            yield return Settle();
                            point = MapContourProbe.Terrain(client.Board.Map, region);
                            Assert.AreSame(client.Board.Map, client.Board.Root.panel.Pick(point));
                            Assert.AreEqual(cardBounds, client.Board.Cards.Root.worldBound,
                                $"Navigation to {region} at {width}/{scale} must preserve the settled pinned card.");
                            Assert.IsTrue(client.Board.Cards.Pinned);
                            CollectionAssert.AreEquivalent(travel.Targets, client.Board.Map.DraftOverlay.Targets);
                        }
                        var endpoint = MapContourProbe.ReachRegion(client.Board.Map, travel.Origin);
                        yield return Settle();
                        var pan = client.Board.Map.Camera.Pan;
                        MapContourProbe.Click(client.Board.Map, MapContourProbe.Terrain(client.Board.Map, travel.Origin));
                        yield return Settle();
                        Assert.IsTrue(client.Board.Map.DraftOverlay.IsRouteEnd(travel.Origin));
                        Assert.IsEmpty(client.Board.Map.Query<Button>(className: "map-target").ToList());
                        Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                        Check(client.Board);
                        LayoutTests.Save(target, $"ui34-route-{width}-{scale:0.00}.png");
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        private static IEnumerator SettlePinnedCard(BoardScreen board, int width, float scale)
        {
            var card = board.Cards.Root;
            var previousCard = card.worldBound;
            var previousViewport = card.parent.worldBound;
            var stableFrames = 0;
            for (var frame = 0; frame < 90; frame++)
            {
                yield return null;
                var currentCard = card.worldBound;
                var currentViewport = card.parent.worldBound;
                stableFrames = previousCard == currentCard && previousViewport == currentViewport
                    ? stableFrames + 1 : 0;
                previousCard = currentCard;
                previousViewport = currentViewport;
                if (stableFrames >= 6)
                    yield break;
            }
            Assert.Fail($"Pinned card did not finish reflow at {width}/{scale}: " +
                $"card={previousCard}, viewport={previousViewport}.");
        }

        private static IEnumerator AllGroups(MountedMatchClient client, PlayerView view)
        {
            FellowshipGestures.Die(client);
            yield return Settle();
            Assert.AreEqual("character", client.Board.ActionDie.DieId);
            client.Board.Hover.Open("moria");
            yield return Settle();
            foreach (var character in view.Characters)
            {
                var portrait = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + character.Id);
                Assert.IsTrue(portrait.enabledInHierarchy, character.Id);
                PointerGestureEvents.ClickAt(portrait);
                yield return Settle();
                Assert.IsTrue(portrait.ClassListContains("selected"), character.Id);
                Assert.AreEqual(DisplayStyle.None, client.Board.Characters.Inspector.Root.resolvedStyle.display,
                    "Choosing a travel participant must not open an inspector over the remaining figures.");
            }
            Assert.AreEqual(7, client.Board.Hover.Panel.Root.Query<Button>().ToList()
                .Count(b => b.name.StartsWith("hover-figure-") && b.ClassListContains("selected")));
            Assert.IsNull(client.Board.Tray.Q<Button>("new-companion-group"), "A committed group is replaced by the saved budget, not another draft page.");
            Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains("moria"));
        }

        internal static void Check(BoardScreen board)
        {
            foreach (var root in new[] { board.Components.Root, board.Hover.Panel.Components.Root })
            {
                var components = root.Query<Button>().ToList().Where(Visible).ToArray();
                foreach (var button in components)
                {
                    Assert.GreaterOrEqual(button.worldBound.xMin, board.Root.worldBound.xMin, button.name);
                    Assert.LessOrEqual(button.worldBound.xMax, board.Root.worldBound.xMax + 1, button.name);
                    Assert.GreaterOrEqual(button.worldBound.yMin, board.Root.worldBound.yMin, button.name);
                    Assert.LessOrEqual(button.worldBound.yMax, board.Root.worldBound.yMax + 1, button.name);
                }
                for (var first = 0; first < components.Length; first++)
                    for (var second = first + 1; second < components.Length; second++)
                        Assert.IsFalse(components[first].worldBound.Overlaps(components[second].worldBound), components[first].name);
            }
            var buttons = board.Tray.Query<Button>().ToList().Where(Visible);
            foreach (var button in buttons)
            {
                Assert.GreaterOrEqual(button.worldBound.xMin, board.Tray.worldBound.xMin, button.name);
                Assert.LessOrEqual(button.worldBound.xMax, board.Tray.worldBound.xMax + 1, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, board.Root.worldBound.yMax, button.name);
                Assert.GreaterOrEqual(button.resolvedStyle.height, 32, button.name);
                var size = button.MeasureTextSize(button.text, button.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(size.y, button.contentRect.height + 1, "Text clipped: " + button.text);
            }
            // UI-137: a Combat card's hover preview overlays the map beside its tile, outside the tray by design.
            foreach (var label in board.Tray.Query<Label>().ToList().Where(Visible).Where(label => !InCombatPreview(label)))
                Assert.LessOrEqual(label.worldBound.xMax, board.Tray.worldBound.xMax + 1, label.text);
            Assert.LessOrEqual(board.Fellowship.Root.worldBound.yMax, board.Map.worldBound.yMax);
            Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2);
            LayoutTests.AssertNoDecisionScroll(board);
            Assert.IsEmpty(board.Tray.Query<TextField>().ToList());
        }

        private static bool InCombatPreview(VisualElement element)
        {
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.ClassListContains(CombatCardPreview.PreviewName))
                    return true;
            return false;
        }

        private static bool Visible(VisualElement element)
        {
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.resolvedStyle.display == DisplayStyle.None || ancestor.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }

        private static PlayerView Screen(CommandKind kind)
        {
            var view = new BoardFixture().View;
            view.MatchId = "layout-" + kind;
            view.Hand = new List<string> { "base.fp.character.19", "base.fp.character.22", "base.fp.character.04", "base.fp.character.18", "base.fp.character.17", "base.fp.strategy.17" };
            view.Fellowship = new FellowshipView { RegionId = "moria", Progress = 3, Corruption = 11, Hidden = true, Guide = "base.character.gandalf-grey", CompanionCount = 7 };
            view.OwnCompanions = new[] { "gimli", "boromir", "legolas", "gandalf-grey", "strider", "merry", "pippin" }
                .Select(id => new CharacterView { Id = "base.character." + id, Location = "fellowship", Level = 3 }).ToList();
            if (kind == CommandKind.ResolveHuntDamage)
            {
                view.Configuration.Lords = true;
                view.Fellowship.Guide = "lords.character.smeagol";
                view.OwnCompanions.Add(new CharacterView { Id = view.Fellowship.Guide, Location = "fellowship", Level = 0 });
                view.Fellowship.CompanionCount = 8;
            }
            view.Fellowship.CompanionIdentities = view.OwnCompanions.Select(c => CharacterLibrary.Identity(c.Id)).ToList();
            view.Dice.Add(new DieView { Id = "character", Face = DieFace.Character, Owner = Side.FreePeoples, Location = DieLocation.Available });
            view.Options.Clear();
            var option = new ActionOption { Kind = kind };
            view.Options.Add(option);
            view.Decision = new DecisionView { Id = "decision-" + kind, Kind = DecisionKind.Action };
            if (kind is CommandKind.SeparateCompanions or CommandKind.MoveCompanions)
            {
                option.EligibleIds = view.OwnCompanions.Select(c => c.Id).ToList();
                option.Minimum = 1;
                option.Maximum = 7;
                option.Travel = option.EligibleIds.Select(id => new TravelOption { Id = id, Origin = "moria", Range = 3, Targets = { "moria", "dimrill-dale", "hollin" } }).ToList();
                if (kind == CommandKind.MoveCompanions)
                    option.EligibleDice.Add("character");
                if (kind == CommandKind.MoveCompanions)
                    view.Characters = view.OwnCompanions.Select(c => new CharacterView { Id = c.Id, Location = "moria", Level = 3 }).ToList();
                return view;
            }
            view.Hunt = new HuntView { SourceId = "fellowship-move", TileId = "base.hunt.standard.2.1.1", Damage = 2, Reveal = true, PoolCount = 0 };
            if (kind == CommandKind.RevealFellowship)
            {
                view.Decision.Kind = DecisionKind.FellowshipRoute;
                option.Travel.Add(new TravelOption { Id = "fellowship", Origin = "moria", Range = 3, Targets = { "moria", "dol-guldur", "hollin", "dimrill-dale" } });
            }
            if (kind == CommandKind.ResolveHuntDamage)
            {
                view.Fellowship.RegionId = "mordor-track";
                view.Fellowship.MordorStep = 4;
                view.Hunt.Mordor = true;
                view.Decision.Kind = DecisionKind.HuntDamage;
                option.HuntResponses.AddRange(new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion });
            }
            if (kind == CommandKind.RerollHunt)
            {
                view.You = Side.Shadow;
                view.DecisionOwner = Side.Shadow;
                view.OwnCompanions.Clear();
                view.Hunt.TileId = "";
                view.Decision.Kind = DecisionKind.HuntReroll;
                for (var i = 1; i <= 5; i++)
                    view.Hunt.Dice.Add(new HuntDieView { Id = "roll-" + i, First = i, Result = i });
                option.EligibleIds = view.Hunt.Dice.Select(d => d.Id).ToList();
                option.Maximum = 3;
            }
            if (kind == CommandKind.RespondEffect)
            {
                view.Decision.Kind = DecisionKind.Reaction;
                view.Decision.Window = "hunt.guide";
                option.EligibleIds.Add("fixture-guide-response");
                option.EffectChoices.Add(new EffectChoiceView { Id = "fixture-guide-response", SourceId = view.Fellowship.Guide });
                view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction });
            }
            return view;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 3; i++)
                yield return null;
        }
    }
}
