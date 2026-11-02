using System.Collections;
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
    public sealed class EffectsLayoutTests
    {
        private const string Protection = "base.fp.strategy.02";

        [UnityTest]
        public IEnumerator MultipleSourcesAbilityEmptyAndExpiredViewsFitFourLayoutsWithoutScrolling()
        {
            using var client = new MountedMatchClient("http://localhost", "effects-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Interface.Windows.Clear();
                    var view = Screen();
                    client.Apply(view);
                    client.SetOnline();
                    yield return Frames();
                    var board = client.Board;
                    PointerGestureEvents.ClickAt(board.TableEffects.Tab);
                    yield return Frames();
                    Assert.AreEqual("Effects · 6", board.TableEffects.Tab.text);
                    Assert.IsEmpty(board.TableEffects.Root.Query<ScrollView>().ToList());
                    var layout = $"{width} {scale:0.00}";
                    EffectsPanelChecks.TitlesInsideTiles(board, layout);
                    var panelWidth = board.TableEffects.Root.layout.width;
                    var grid = EffectsPanelChecks.Grid(board);
                    var openings = 0;
                    System.Action opened = () => openings++;
                    board.Cards.Opening += opened;
                    PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("effect-source-" + Protection));
                    board.Cards.Opening -= opened;
                    yield return Frames();
                    Assert.AreEqual(0, openings, "UI-94: a source click opens no separate card window.");
                    LayoutTests.Save(target, $"ui94-multiple-{width}-{scale:0.00}.png");
                    // UI-446 (owner): the click opens nothing beside or in the panel, which keeps its width and its grid; the
                    // pointer over the card unrolls only its part in effect, the Event text, though the card has a Combat part.
                    EffectsPanelChecks.ClickOpensNothing(board, Protection, layout);
                    // UI-446 (owner) with UI-288: a read-only card tile keeps its parchment, so its dark-ink title stays readable.
                    Assert.That(board.TableEffects.Root.Q<Button>("effect-source-" + Protection).resolvedStyle.backgroundImage.texture?.name,
                        Does.StartWith("parchment"), "A read-only card keeps its parchment · " + layout);
                    Assert.That(board.TableEffects.Root.layout.width, Is.EqualTo(panelWidth).Within(.5f), "The panel keeps its width · " + layout);
                    Assert.That(panelWidth, Is.EqualTo(board.TableEffects.Arrangement.PanelWidth).Within(1f), layout);
                    EffectsPanelChecks.SameGrid(grid, EffectsPanelChecks.Grid(board), layout);
                    Check(board);
                    EffectsPanelChecks.UnrolledText(board, Protection, layout);
                    Assert.IsTrue(CardLibrary.Cards[Protection].HasCombat, "The card has a Combat part to leave out.");
                    Assert.AreEqual(CardLibrary.Cards[Protection].Event, board.TableEffects.Root.Q("effect-unroll").Q<Label>("effect-card-event").text);
                    Assert.AreEqual(DisplayStyle.None, board.TableEffects.Root.Q("effect-unroll").Q("effect-card-combat").resolvedStyle.display);
                    // The scopes and their places stand in a removal's details only (PaymentStaysAtTheSource…).
                    Assert.IsEmpty(board.TableEffects.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-locate-")));
                    Assert.AreEqual("◇ 2", board.Map.Q<Label>("map-effects-lorien").text);
                    Assert.IsNull(board.Map.Q("map-effects-orthanc"));
                    // UI-221: no pages; every source is in view at once.
                    var visibleSources = board.TableEffects.Root.Query<Button>().ToList()
                        .Where(b => b.name.StartsWith("effect-source-") && b.parent.resolvedStyle.display != DisplayStyle.None).ToList();
                    Assert.AreEqual(6, visibleSources.Count);
                    // UI-221: hovering a source unrolls its text over the others, like a hand card.
                    CardGestures.Move(visibleSources[0]);
                    yield return Frames();
                    var unroll = board.TableEffects.Root.Q("effect-unroll");
                    Assert.AreEqual(DisplayStyle.Flex, unroll.resolvedStyle.display);
                    Assert.Greater(unroll.worldBound.height, 20);
                    Assert.AreEqual(PickingMode.Ignore, unroll.pickingMode, "The unrolled text takes no pointer.");
                    // UI-424: a tile low on the board unrolls upward, so its text stays above the bottom row.
                    Assert.GreaterOrEqual(unroll.worldBound.yMin + 1, board.TableEffects.Root.parent.worldBound.yMin, "unrolled text on the board · " + layout);
                    Assert.LessOrEqual(unroll.worldBound.yMax - 1, EffectsPanelChecks.BottomRow(board), "unrolled text above the bottom row · " + layout);
                    EffectsPanelChecks.UnrolledText(board, visibleSources[0].name.Substring("effect-source-".Length), layout);
                    foreach (var source in visibleSources)
                        Inside(source.worldBound, board.TableEffects.Root.worldBound);
                    // UI-446 (owner): Inspect unrolls an ability's descriptions as plain text, as a hover does; it opens no details.
                    board.TableEffects.Inspect("kings.character.theoden.corrupted");
                    yield return Frames();
                    Check(board);
                    EffectsPanelChecks.UnrolledText(board, "kings.character.theoden.corrupted", layout);
                    Assert.AreEqual("Shadow-controlled Helm’s Deep is an Isengard stronghold.",
                        board.TableEffects.Root.Q<Label>("effect-unroll-text").text);
                    Assert.IsNull(board.TableEffects.Root.Q("effect-card"), "An ability has no card text.");
                    Assert.AreEqual(DisplayStyle.None, board.TableEffects.Details.resolvedStyle.display);
                    LayoutTests.Save(target, $"ui57-ability-{width}-{scale:0.00}.png");
                    board.TableEffects.Close();
                    board.Hover.Open("lorien");
                    yield return Frames();
                    foreach (var effect in board.Hover.Panel.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("hover-effect-")))
                    {
                        Inside(effect.worldBound, board.Hover.Panel.Root.worldBound);
                        var measured = effect.MeasureTextSize(effect.text, effect.contentRect.width, VisualElement.MeasureMode.Exactly,
                            0, VisualElement.MeasureMode.Undefined);
                        Assert.LessOrEqual(measured.y, effect.contentRect.height + 1, "Full effect title fits its clickable area.");
                    }
                    LayoutTests.Save(target, $"ui57-region-effects-{width}-{scale:0.00}.png");
                    board.Hover.Close();
                    board.TableEffects.Inspect(Protection);
                    yield return Frames();
                    Assert.AreEqual(Protection, board.TableEffects.UnrolledSource, "Inspect unrolls the card's text.");
                    view.Effects.Clear();
                    view.Players.Clear();
                    view.Options.Clear();
                    view.Revision++;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.AreEqual("Effects · 0", board.TableEffects.Tab.text);
                    Assert.AreEqual(DisplayStyle.Flex, board.TableEffects.Root.Q<Label>("effects-empty").resolvedStyle.display);
                    // UI-446 (owner): an ended source leaves nothing pinned — its unrolled text goes with its tile, and outside a
                    // removal being paid no "Effect ended" details stay (UI-94's pinned text no longer exists).
                    Assert.IsEmpty(board.TableEffects.UnrolledSource);
                    Assert.AreEqual(DisplayStyle.None, board.TableEffects.Root.Q("effect-unroll").resolvedStyle.display);
                    Assert.AreEqual(DisplayStyle.None, board.TableEffects.Details.resolvedStyle.display);
                    Assert.IsNull(board.Map.Q("map-effects-lorien"));
                    Check(board);
                    LayoutTests.Save(target, $"ui94-empty-ended-{width}-{scale:0.00}.png");
                    board.TableEffects.Close();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        [UnityTest]
        public IEnumerator PaymentStaysAtTheSourceAndTheDrawerCanMoveWithoutLosingIt()
        {
            using var client = new MountedMatchClient("http://localhost", "effects-payment-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Interface.Windows.Clear();
                    var view = Screen();
                    client.Apply(view);
                    client.SetOnline();
                    yield return Frames();
                    PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>("cost"));
                    PointerGestureEvents.ClickAt(client.Board.TableEffects.Tab);
                    yield return Frames();
                    // UI-446 (owner): the lit tile prepares the removal exactly as its × does (UI-430's board rule); the layouts
                    // take the two ways in turn.
                    var tile = client.Board.TableEffects.Root.Q<Button>("effect-source-" + Protection);
                    var remove = client.Board.TableEffects.Root.Q<Button>("remove-table-" + Protection);
                    Assert.IsTrue(tile.ClassListContains("legal-target"), "The removable card is lit.");
                    Assert.IsFalse(ReadOnlyControl.Is(tile), "Its click prepares the removal.");
                    Assert.AreEqual(remove.tooltip, tile.tooltip);
                    PointerGestureEvents.ClickAt(scale == 1f ? remove : tile);
                    yield return Frames();
                    Assert.AreSame(client.Board.TableEffects.DecisionHost, client.Board.Tray.parent);
                    Assert.AreEqual(Protection, client.Board.TableEffects.SelectedSource);
                    Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                    LayoutTests.Save(target, $"ui94-payment-{width}-{scale:0.00}.png");
                    Check(client.Board);
                    PaymentDetails(client.Board);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    Assert.IsNull(client.Board.TableEffects.Root.Q(className: FloatingWindow.HandleClass),
                        "Effects is docked in the left column.");
                    // The details follow the source's update and keep the focused place's button.
                    client.Board.TableEffects.Root.Q<Button>("effect-locate-lorien").Focus();
                    view.Effects.First(e => e.Id == "lorien").ExpiryDescription = "At the end of this battle.";
                    var moved = client.Board.TableEffects.Root.layout.position;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.Less(Vector2.Distance(moved, client.Board.TableEffects.Root.layout.position), 2);
                    Assert.AreSame(client.Board.TableEffects.DecisionHost, client.Board.Tray.parent);
                    Assert.AreEqual("effect-locate-lorien", (client.Board.Root.panel.focusController.focusedElement as VisualElement)?.name);
                    Assert.IsTrue(client.Board.TableEffects.Details.Query<Label>().ToList().Any(l => l.text == "At the end of this battle."));
                    var camera = client.Board.Map.Camera;
                    var previousPan = camera.Pan;
                    PointerGestureEvents.ClickAt(client.Board.TableEffects.Root.Q<Button>("effect-locate-lorien"));
                    yield return Frames();
                    Assert.AreNotEqual(previousPan, camera.Pan);
                    Assert.AreEqual("lorien", client.Board.Map.Selected);
                    Assert.AreSame(client.Board.TableEffects.DecisionHost, client.Board.Tray.parent, "Locating a place keeps the payment.");
                    client.Board.TableEffects.Close();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        private static PlayerView Screen()
        {
            var view = new BoardFixture().View;
            view.MatchId = "effects-layout";
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Options.Clear();
            view.Hand.AddRange(new[] { "base.sh.strategy.01", "base.sh.character.01", "base.sh.strategy.05" });
            view.Dice.Add(new DieView { Id = "cost", Owner = Side.Shadow, Face = DieFace.Muster, Location = DieLocation.Available });
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.DiscardProtection, Minimum = 2, Maximum = 2, Targets = { Protection }, EligibleDice = { "cost" },
                EligibleIds = { "base.sh.strategy.01", "base.sh.character.01" },
                EffectChoices = { new EffectChoiceView { Id = "base.sh.strategy.01", SelectionGroup = "Army" },
                    new EffectChoiceView { Id = "base.sh.character.01", SelectionGroup = "Character" } }
            });
            foreach (var region in new[] { "rivendell", "lorien", "grey-havens" })
                view.Effects.Add(new ActiveEffectView
                {
                    Id = region, SourceId = Protection, SourceKind = "Card", Title = "A Power too Great", Owner = Side.FreePeoples,
                    Modifier = "PreventShadowArmyEntry", ExpiryDescription = region == "lorien" ? "At the end of this turn." : "Until removed by its specified cost.",
                    Targets = { new EffectTargetView { Kind = "Region", Id = region, Label = FigureGrid.Name(region) } }
                });
            view.Effects.Add(new ActiveEffectView
            {
                Id = "second-local", SourceId = "base.fp.strategy.03", SourceKind = "Card", Title = "Tom Bombadil", Owner = Side.FreePeoples,
                Targets = { new EffectTargetView { Kind = "Region", Id = "lorien", Label = "Lórien" } }, ExpiryDescription = "Until removed."
            });
            view.Effects.Add(new ActiveEffectView
            {
                Id = "ability", SourceId = "kings.character.theoden.corrupted", SourceKind = "Ability", Title = "Théoden", Owner = Side.Shadow,
                Description = "Shadow-controlled Helm’s Deep is an Isengard stronghold.",
                Targets = { new EffectTargetView { Kind = "Region", Id = "helms-deep", Label = "Helm’s Deep" } },
                ExpiryDescription = "While Théoden remains Corrupted and Shadow controls Helm’s Deep."
            });
            view.Effects.Add(new ActiveEffectView
            {
                Id = "nation", SourceId = "base.sh.character.22", SourceKind = "Card", Title = "Wormtongue", Owner = Side.Shadow,
                TargetId = "base.character.saruman", Targets = { new EffectTargetView { Kind = "Nation", Id = "rohan", Label = "Rohan" } },
                ExpiryDescription = "Until Rohan activates or Saruman leaves play."
            });
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { Protection, "base.fp.strategy.03", "base.fp.character.04" } });
            view.Players.Add(new PlayerSummary { Side = Side.Shadow, TableCards = { "base.sh.character.22", "lords.sh.special.05" } });
            return view;
        }

        private static void Check(BoardScreen board)
        {
            var panel = board.TableEffects.Root;
            Inside(panel.worldBound, board.Map.worldBound);
            // UI-94: the stage ends where the bottom bar starts; nothing of the panel may hide under it.
            Inside(panel.worldBound, panel.parent.worldBound);
            Assert.IsEmpty(panel.Query<ScrollView>().ToList());
            // UI-446 (owner): nothing stands beside the panel — a removal's details stand inside it, above the payment; a
            // hovered source's unrolled text lies over the panel and the board (UI-221) and is checked where it is hovered
            // (EffectsPanelChecks.UnrolledText).
            var unroll = panel.Q("effect-unroll");
            foreach (var element in panel.Query<VisualElement>().ToList())
                if (element.visible && element.worldBound.width > 0 && element.worldBound.height > 0 &&
                    element != unroll && !unroll.Contains(element))
                {
                    Assert.LessOrEqual(element.worldBound.yMax, panel.worldBound.yMax + 1, element.name + " " + string.Join(" ", element.GetClasses()));
                    Inside(element.worldBound, panel.worldBound);
                }
        }

        // UI-446 (owner): while a removal is paid its source's details stand inside the panel above the payment — title, state,
        // scopes and places, each place once; the card's text is left out so the payment has the room (UI-94).
        private static void PaymentDetails(BoardScreen board)
        {
            var details = board.TableEffects.Details;
            Assert.AreEqual(DisplayStyle.Flex, details.resolvedStyle.display, "The payment shows the source's details.");
            Assert.AreEqual("A Power too Great", details.Q<Label>("effect-detail-title").text);
            Assert.AreEqual("Free Peoples · In play", details.Q<Label>("effect-detail-state").text);
            Assert.AreEqual(DisplayStyle.None, details.Q("effect-card").resolvedStyle.display, "No card text during the payment.");
            Assert.AreEqual(2, details.Query<Label>().ToList().Count(l => l.name.StartsWith("effect-expiry-")));
            Assert.AreEqual(3, details.Query<Button>().ToList().Count(b => b.name.StartsWith("effect-locate-")));
            Inside(details.worldBound, board.TableEffects.Root.worldBound);
            Assert.LessOrEqual(details.worldBound.yMax, board.TableEffects.DecisionHost.worldBound.yMin + 1, "The details stand above the payment.");
        }

        // UI-424: twenty-two table cards with the catalogue's longest names, four of them removable (×), and the longest card
        // text: every title inside its tile, the panel above the bottom row in its arrangement's width, and the longest
        // text unrolled whole without changing the panel's width or columns. UI-446 (owner): a table card unrolls only its
        // Event text, so the longest text is the longest Event.
        [UnityTest]
        public IEnumerator ManySourcesAndTheLongestCardTextFitWithoutWideningThePanel()
        {
            var longest = CardLibrary.Cards.Values.OrderByDescending(card => card.Event.Length).ThenBy(card => card.Id).First();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var layout = $"{width} {scale:0.00}";
                    var view = ManySources(longest.Id, $"effects-many-{width}-{scale:0.00}");
                    using var client = new MountedMatchClient("http://localhost", view.MatchId);
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    client.Apply(view);
                    client.SetOnline();
                    yield return Frames();
                    var board = client.Board;
                    PointerGestureEvents.ClickAt(board.TableEffects.Tab);
                    yield return Frames();
                    Assert.AreEqual("Effects · 22", board.TableEffects.Tab.text);
                    Assert.AreEqual(22, EffectsPanelChecks.Sources(board).Count, "Every source is in view · " + layout);
                    Assert.AreEqual(4, board.TableEffects.Root.Query<Button>(className: "effect-remove").ToList()
                        .Count(button => button.resolvedStyle.display == DisplayStyle.Flex), layout);
                    EffectsPanelChecks.TitlesInsideTiles(board, layout);
                    EffectsPanelChecks.PanelAboveBottomRow(board, layout);
                    Assert.IsEmpty(board.TableEffects.Root.Query<ScrollView>().ToList());
                    var panelWidth = board.TableEffects.Root.layout.width;
                    Assert.That(panelWidth, Is.EqualTo(board.TableEffects.Arrangement.PanelWidth).Within(1f), layout);
                    var grid = EffectsPanelChecks.Grid(board);
                    LayoutTests.Save(target, $"ui424-many-{width}-{scale:0.00}.png");
                    // UI-446 (owner): a click on the card opens nothing; the pointer over it unrolls its part in effect whole,
                    // above the bottom row, and the panel keeps its width and its columns.
                    PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("effect-source-" + longest.Id));
                    yield return Frames();
                    EffectsPanelChecks.ClickOpensNothing(board, longest.Id, layout);
                    EffectsPanelChecks.UnrolledText(board, longest.Id, layout);
                    Assert.That(board.TableEffects.Root.layout.width, Is.EqualTo(panelWidth).Within(.5f), "The panel keeps its width · " + layout);
                    EffectsPanelChecks.SameGrid(grid, EffectsPanelChecks.Grid(board), layout);
                    EffectsPanelChecks.PanelAboveBottomRow(board, layout);
                    LayoutTests.Save(target, $"ui424-many-details-{width}-{scale:0.00}.png");
                    board.TableEffects.Close();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // The catalogue's longest card names on both sides' tables, with the card of the longest text among them.
        private static PlayerView ManySources(string longest, string match)
        {
            var view = new BoardFixture().View;
            view.MatchId = match;
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Options.Clear();
            var cards = CardLibrary.Cards.Values.Where(card => card.Id != longest).OrderByDescending(card => card.Title.Length)
                .ThenBy(card => card.Id).Select(card => card.Id).Take(21).Append(longest).ToList();
            var free = new PlayerSummary { Side = Side.FreePeoples };
            free.TableCards.AddRange(cards.Where(id => id.Contains(".fp.")));
            var shadow = new PlayerSummary { Side = Side.Shadow };
            shadow.TableCards.AddRange(cards.Where(id => !id.Contains(".fp.")));
            view.Players.Add(free);
            view.Players.Add(shadow);
            view.Dice.Add(new DieView { Id = "cost", Owner = Side.FreePeoples, Face = DieFace.Muster, Location = DieLocation.Available });
            var removal = new ActionOption { Kind = CommandKind.DiscardProtection, Minimum = 2, Maximum = 2, EligibleDice = { "cost" } };
            removal.Targets.AddRange(shadow.TableCards.Take(4));
            view.Options.Add(removal);
            return view;
        }

        private static void Inside(Rect inner, Rect outer)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), $"{inner} in {outer}");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), $"{inner} in {outer}");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), $"{inner} in {outer}");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), $"{inner} in {outer}");
        }

        private static IEnumerator Frames()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }

    }
}
