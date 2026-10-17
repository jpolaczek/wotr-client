using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed partial class InGameContextNetworkTests
    {
        [UnityTest]
        public IEnumerator EffectsResourcesSovereignsAndVictoryOpenSavedFactsForBothSides() => Wait(HistoryFamilies());

        [UnityTest]
        public IEnumerator ClosedEffectCostReopensFromGoldAndHistoryKeepTheExactDraft() => Wait(EffectDraft());

        private static async Task HistoryFamilies()
        {
            var cases = new[]
            {
                ("effect", CommandKind.DiscardPalantir, InGameActionHost.Effects),
                ("ring", CommandKind.UseRing, InGameActionHost.Resources),
                ("council", CommandKind.UseCouncilToken, InGameActionHost.Resources),
                ("dismay", CommandKind.UseDismay, InGameActionHost.Resources),
                ("ruler", CommandKind.DrawCard, InGameActionHost.Resources),
                ("sovereign", CommandKind.AwakenSovereign, InGameActionHost.Sovereign),
                ("cleanse", CommandKind.ChooseEffect, InGameActionHost.Sovereign),
                ("victory", CommandKind.DiscardDie, InGameActionHost.Victory)
            };
            foreach (var (name, command, expected) in cases)
            {
                using var room = await Connect("ui62-" + name, ScenarioKind.Interaction, openPanels: false);
                await ActionNoticeNetworkTests.Loaded(room);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    var before = JObject.FromObject(board.View);
                    var parent = board.Tray.parent;
                    var pan = board.Map.Camera.Pan;
                    var page = await Page(room, client);
                    var step = page.Steps.Where(item => item.Command == command).OrderByDescending(item => item.Revision).First();
                    var group = page.Actions.Single(item => item.Id == step.ActionId);
                    board.ActionHistory.Open(new ActionOpenRequest(group.Id, group.Version, step.Id));
                    await room.Until(() => board.ActionHistory.Context != null);
                    await room.Settle();
                    Assert.AreEqual(expected, board.ActionHistory.Context.InGame.Host, name);
                    Assert.IsFalse(board.Log.IsOpen, "A notice opens its own summary without opening the history list.");
                    Assert.AreSame(parent, board.Tray.parent, "Historical inspection must not rehost the live decision.");
                    Assert.IsTrue(JToken.DeepEquals(before, JObject.FromObject(board.View)));
                    if (expected == InGameActionHost.Resources)
                    {
                        var pool = step.Actor == board.View.You ? board.OwnDice : board.OpponentDice;
                        Assert.IsTrue(pool.ClassListContains("history-context-host"), name);
                        Assert.IsEmpty(board.ActionHistory.Context.Regions, name);
                        Assert.AreEqual(pan, board.Map.Camera.Pan, name);
                        Assert.IsTrue(board.Log.Card.Displayed.Steps.SelectMany(item => item.Costs)
                            .Any(cost => cost.Kind is ActionCostKind.Ring or ActionCostKind.RulerToken or ActionCostKind.CouncilToken));
                    }
                    if (expected == InGameActionHost.Victory)
                    {
                        Assert.IsTrue(board.Victory.Tab.ClassListContains("history-context-host"));
                        StringAssert.Contains("Military victory", board.ActionHistory.Context.InGame.Title);
                    }
                    var enemy = client == room.Fp ? room.Sh : room.Fp;
                    var recorded = JToken.FromObject(board.Log.Card.Displayed).ToString();
                    foreach (var privateCard in enemy.Board.View.Hand)
                        StringAssert.DoesNotContain(privateCard, recorded, "Current enemy hand is not a historical fact.");
                    LayoutTests.Save(client.Viewport, "ui62-" + name + "-" + board.View.You + ".png");
                    board.Log.CloseCard();
                }
            }
        }

        private static async Task EffectDraft()
        {
            using var room = await Connect("ui62-effect-cost", ScenarioKind.Interaction, openPanels: false);
            var actor = room.Sh;
            var board = actor.Board;
            var before = board.View.Revision;
            var die = board.View.Options.Single(option => option.Kind == CommandKind.DiscardProtection).EligibleDice[0];
            await ComponentGestures.Cost(room, actor, die);
            board.TableEffects.Open();
            await room.Settle();
            PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("remove-table-base.fp.strategy.02"));
            await room.Settle();
            await CardGestures.Open(actor);
            PointerGestureEvents.ClickAt(board.Hand.Q<Button>("base.sh.strategy.01"));
            await room.Settle();
            board.HandDrawer.Close();
            board.Cards.Close();
            board.TableEffects.Close();
            await KingsCardNetworkChecks.Reconnect(room, actor);
            Assert.IsFalse(board.TableEffects.IsOpen);
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(board.TableEffects.IsOpen);
            Assert.AreSame(board.TableEffects.DecisionHost, board.Tray.parent);
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            StringAssert.Contains("Army ✓", board.Tray.Q<Label>("protection-cost").text);
            Assert.IsFalse(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);

            var page = await Page(room, actor);
            var old = page.Actions.First();
            board.ActionHistory.Open(new ActionOpenRequest(old.Id, old.Version));
            await room.Until(() => board.ActionHistory.Context != null);
            await room.Settle();
            Assert.AreSame(board.TableEffects.DecisionHost, board.Tray.parent);
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            StringAssert.Contains("Army ✓", board.Tray.Q<Label>("protection-cost").text);
            Assert.AreEqual(before, board.View.Revision);
            board.Log.CloseCard();
            await CardGestures.Open(actor);
            PointerGestureEvents.ClickAt(board.Hand.Q<Button>("base.sh.character.01"));
            await room.Settle();
            board.HandDrawer.Close();
            board.Cards.Close();
            board.Notice.Open();
            await room.Settle();
            await room.Until(() => board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("confirm-decision"));
            await room.At(before + 1);
            Assert.IsFalse(board.View.Effects.Any(effect => effect.SourceId == "base.fp.strategy.02"));
            Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(value => value.Id == die).Location);
        }

        private static Task<ActionLogPage> Page(MatchTestRoom room, MountedMatchClient client) =>
            client.Api.Get<ActionLogPage>("api/matches/" + client.Board.View.MatchId + "/actions?filter=All&limit=50", room.Token);
    }
}
