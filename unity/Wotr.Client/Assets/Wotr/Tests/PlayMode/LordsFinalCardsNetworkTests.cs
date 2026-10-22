using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class LordsFinalCardsNetworkTests
    {
        [UnityTest] public IEnumerator MadnessRegionHighlightPreservesChoiceAndFinalMapClickCommits() => Wait(Target());
        [UnityTest] public IEnumerator MadnessLossesAndRecruitmentPassControlBetweenRealClients() => Wait(Losses());
        [UnityTest] public IEnumerator SpecialCardIsInspectableAndReplacesTheTileBeforeProtection() => Wait(Special(false));
        [UnityTest] public IEnumerator SpecialCardCanBeKeptForALaterHunt() => Wait(Special(true));

        private static async Task Target()
        {
            using (var room = await Connect("lords-madness-target", ScenarioKind.CardPack))
            {
                var revision = room.Sh.Board.View.Revision;
                Assert.IsNull(room.Fp.Board.View.Decision);
                CardNetworkTests.Click(room.Sh.Board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList().First());
                await room.Settle();
                var region = MapContourProbe.ReachRegion(room.Sh.Board.Map, "eagles-eyrie");
                MapContourProbe.Hover(room.Sh.Board.Root, region);
                await room.Settle();
                Assert.AreEqual(DisplayStyle.None, room.Sh.Board.Hover.Panel.Root.resolvedStyle.display);
                Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Contains("eagles-eyrie"));
                StringAssert.Contains("Roll 3 dice", room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EffectChoices.Single(c => c.TargetId == "eagles-eyrie" || c.Id == "eagles-eyrie").Detail);
                Assert.AreEqual(revision, room.Sh.Board.View.Revision);
                Assert.AreEqual("lords.sh.strategy.26", room.Sh.Board.Cards.CardId);
                Assert.AreEqual("Resolving Event", room.Sh.Board.Cards.Root.Q<Label>("card-window").text);
                Assert.IsNull(room.Fp.Board.View.EventLoss);
                await Reconnect(room, room.Sh);
                await Layout(room, room.Sh, "madness-target");
                Assert.AreEqual(revision, room.Sh.Board.View.Revision);
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("confirm-decision"));
                await ArmyGestures.Target(room, room.Sh, "eagles-eyrie");
                Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
            }
        }

        private static async Task Losses()
        {
            using (var room = await Connect("lords-madness-losses", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                var loss = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.TakeEventLosses).EligibleIds[0];
                Click(room.Fp, "loss-" + loss);
                await Layout(room, room.Fp, "madness-losses");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == loss).Location);
                Assert.IsNull(room.Fp.Board.View.Decision);
                await Layout(room, room.Sh, "madness-recruit");
                Assert.IsFalse(room.Sh.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Carrock → Carrock")));
                await ArmyGestures.Target(room, room.Sh, "carrock");
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "carrock"));
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
            }
        }

        private static async Task Special(bool decline)
        {
            using (var room = await Connect(decline ? "lords-special-decline" : "lords-special-replace", ScenarioKind.CardPack))
            {
                const string card = "lords.sh.special.05";
                var board = room.Sh.Board;
                var inspectionRevision = board.View.Revision;
                board.Cards.Close();
                // UI-285: the table card's Hunt response is a card object in the Hunt window's response row.
                board.Hunt.Open();
                await room.Settle();
                await ComponentGestures.Hover(HuntCard(room.Sh, "lords.we-shall-get-it"));
                Assert.IsEmpty(board.Cards.CardId, "Hovering a final reaction must not inspect or execute it.");
                PointerGestureEvents.ClickAt(board.TableEffects.Tab);
                await room.Settle();
                var source = board.TableEffects.Root.Q<Button>("effect-source-" + card);
                await ComponentGestures.Hover(source);
                Assert.IsEmpty(board.Cards.CardId);
                PointerGestureEvents.ClickAt(source);
                await room.Settle();
                Assert.AreEqual(card, board.TableEffects.SelectedSource);
                EffectsNetworkTests.AssertCardInPanel(board, card);
                StringAssert.Contains("automatically", board.TableEffects.Root.Q<Label>("effect-card-event").text);
                Assert.AreEqual(inspectionRevision, board.View.Revision, "Reading a source keeps the reaction pending.");
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.IsTrue(room.Fp.Board.View.Hunt.DamagePending);
                await Layout(room, room.Sh, "special-card");
                board.TableEffects.Close();
                await Reconnect(room, room.Sh);
                if (decline)
                    await room.Direct(room.Sh, "action-DeclineReaction");
                else
                {
                    await room.Settle();
                    var revision = room.Sh.Board.View.Revision;
                    room.Sh.Board.Hunt.Open();
                    await room.Settle();
                    PointerGestureEvents.ClickAt(HuntCard(room.Sh, "lords.we-shall-get-it"));
                    await room.At(revision + 1);
                    await Layout(room, room.Sh, "special-replace");
                }
                Assert.AreEqual("hunt.replace", room.Fp.Board.View.Decision.Window);
                Assert.AreEqual(decline ? "base.hunt.standard.3.0.1" : "base.hunt.standard.eye.1.1", room.Fp.Board.View.Hunt.TileId);
                Assert.AreEqual(decline, room.Fp.Board.View.Players.Single(p => p.Side == Side.Shadow).TableCards.Contains(card));
                Assert.AreEqual(0, room.Fp.Board.View.Fellowship.Corruption);
                await Layout(room, room.Fp, decline ? "special-kept" : "special-fp-protection");
            }
        }

        private static void Map(MountedMatchClient client, string id) =>
            MapContourProbe.ClickRegion(client.Board.Map, id);

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var previous = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > previous);
            await room.Settle();
        }

        private static async Task Layout(MatchTestRoom room, MountedMatchClient client, string name)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    await room.Settle();
                    FellowshipLayoutTests.Check(client.Board);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                    var inspector = client.Board.Cards.Root;
                    if (inspector.resolvedStyle.display != DisplayStyle.None)
                    {
                        Assert.LessOrEqual(inspector.worldBound.xMax, client.Board.Root.worldBound.xMax + 1);
                        Assert.LessOrEqual(inspector.worldBound.yMax, client.Board.Root.worldBound.yMax + 1);
                        var rules = inspector.Q<Label>("card-text");
                        var size = rules.MeasureTextSize(rules.text, rules.contentRect.width,
                            VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                        Assert.LessOrEqual(size.y, rules.contentRect.height + 1, "Card rules clipped");
                    }
                    var effectCard = client.Board.TableEffects.Root.Q("effect-card");
                    if (client.Board.TableEffects.IsOpen && effectCard != null)
                    {
                        Assert.LessOrEqual(effectCard.worldBound.yMax, client.Board.Root.worldBound.yMax + 1, "UI-94: card text stays on screen");
                        Assert.LessOrEqual(effectCard.worldBound.yMax, client.Board.TableEffects.Root.worldBound.yMax + 1);
                    }
                    LayoutTests.Save(target, $"lords03-{name}-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Panel.scale = 1f;
            await room.Settle();
        }
    }
}
