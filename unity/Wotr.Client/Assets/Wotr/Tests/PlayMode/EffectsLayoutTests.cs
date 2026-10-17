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
                    var openings = 0;
                    System.Action opened = () => openings++;
                    board.Cards.Opening += opened;
                    PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("effect-source-" + Protection));
                    board.Cards.Opening -= opened;
                    yield return Frames();
                    Assert.AreEqual(0, openings, "UI-94: a source click opens no separate card window.");
                    LayoutTests.Save(target, $"ui94-multiple-{width}-{scale:0.00}.png");
                    Check(board);
                    Assert.AreEqual(2, board.TableEffects.Root.Query<Label>().ToList().Count(l => l.name.StartsWith("effect-expiry-")));
                    Assert.AreEqual(3, board.TableEffects.Root.Query<Button>().ToList().Count(b => b.name.StartsWith("effect-locate-")));
                    Assert.AreEqual(CardLibrary.Cards[Protection].Event, board.TableEffects.Root.Q<Label>("effect-card-event").text);
                    Assert.AreEqual(CardLibrary.Cards[Protection].Combat, board.TableEffects.Root.Q<Label>("effect-card-combat-text").text);
                    TextFits(board.TableEffects.Root.Q("effect-card"));
                    board.TableEffects.Root.Q<Button>("effect-locate-lorien").Focus();
                    view.Effects.First(e => e.Id == "lorien").ExpiryDescription = "At the end of this battle.";
                    view.Revision++;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.AreEqual("effect-locate-lorien", (board.Root.panel.focusController.focusedElement as VisualElement)?.name);
                    var camera = board.Map.Camera;
                    var previousPan = camera.Pan;
                    PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("effect-locate-lorien"));
                    yield return Frames();
                    Assert.AreNotEqual(previousPan, camera.Pan);
                    Assert.AreEqual("lorien", board.Map.Selected);
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
                    foreach (var source in visibleSources)
                        Inside(source.worldBound, board.TableEffects.Root.worldBound);
                    board.TableEffects.Inspect("kings.character.theoden.corrupted");
                    yield return Frames();
                    Check(board);
                    Assert.AreEqual("Shadow-controlled Helm’s Deep is an Isengard stronghold.",
                        board.TableEffects.Root.Q<Label>("effect-description-ability").text);
                    Assert.IsNull(board.TableEffects.Root.Q("effect-card"), "An ability has no card text.");
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
                    view.Effects.Clear();
                    view.Players.Clear();
                    view.Options.Clear();
                    view.Revision++;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.AreEqual("Effects · 0", board.TableEffects.Tab.text);
                    Assert.AreEqual("Effect ended", board.TableEffects.Root.Q<Label>("effect-detail-state").text);
                    Assert.IsNull(board.Map.Q("map-effects-lorien"));
                    Check(board);
                    TextFits(board.TableEffects.Root.Q("effect-card"));
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
                    PointerGestureEvents.ClickAt(client.Board.TableEffects.Root.Q<Button>("remove-table-" + Protection));
                    yield return Frames();
                    Assert.AreSame(client.Board.TableEffects.DecisionHost, client.Board.Tray.parent);
                    Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                    LayoutTests.Save(target, $"ui94-payment-{width}-{scale:0.00}.png");
                    Check(client.Board);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    Assert.IsNull(client.Board.TableEffects.Root.Q(className: FloatingWindow.HandleClass),
                        "Effects is docked in the left column.");
                    var moved = client.Board.TableEffects.Root.layout.position;
                    client.Refresh(view);
                    yield return Frames();
                    Assert.Less(Vector2.Distance(moved, client.Board.TableEffects.Root.layout.position), 2);
                    Assert.AreSame(client.Board.TableEffects.DecisionHost, client.Board.Tray.parent);
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
            Inside(board.TableEffects.Root.worldBound, board.Map.worldBound);
            // UI-94: the stage ends where the bottom bar starts; nothing of the panel may hide under it.
            Inside(board.TableEffects.Root.worldBound, board.TableEffects.Root.parent.worldBound);
            Assert.IsEmpty(board.TableEffects.Root.Query<ScrollView>().ToList());
            foreach (var element in board.TableEffects.Root.Query<VisualElement>().ToList())
                if (element.visible && element.worldBound.width > 0 && element.worldBound.height > 0)
                {
                    Assert.LessOrEqual(element.worldBound.yMax, board.TableEffects.Root.worldBound.yMax + 1, element.name + " " + string.Join(" ", element.GetClasses()));
                    Inside(element.worldBound, board.TableEffects.Root.worldBound);
                }
        }

        // Every line of the card text is visible: no label is clipped by the parchment or the panel.
        private static void TextFits(VisualElement card)
        {
            Assert.IsNotNull(card);
            foreach (var label in card.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None && l.text.Length > 0))
            {
                // QUAL-19: MeasureTextSize floors its width to physical pixels while rendering keeps the fractional width
                // (as in BoardCompletionLayoutChecks.FullText); measure at that pixel boundary so a line is not invented.
                var scale = label.scaledPixelsPerPoint;
                var width = Mathf.Ceil(label.contentRect.width * scale) / scale;
                var size = label.MeasureTextSize(label.text, width, VisualElement.MeasureMode.Exactly,
                    0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(size.y, label.contentRect.height + 1, label.name + " clipped");
                Inside(label.worldBound, card.worldBound);
            }
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
