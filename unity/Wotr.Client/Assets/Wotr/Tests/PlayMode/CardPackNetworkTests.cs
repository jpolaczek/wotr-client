using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class CardPackNetworkTests
    {
        [UnityTest]
        public IEnumerator MithrilAndAxeRestoreBeforeShelobRollAndReachBothClients() => Wait(Protection());
        [UnityTest]
        public IEnumerator MightySelectsTwoGroupsAndConfirmsOneCommandAfterReconnect() => Wait(Mighty());
        [UnityTest]
        public IEnumerator BladeNamesMinionsAndSpendsTheHitOnTheChosenFigure() => Wait(Blade());

        private static async Task Protection()
        {
            using (var room = await Connect("hunt-protection", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Fp.Board.View.Hunt.DamagePending);
                Assert.IsNull(room.Fp.Board.View.Hunt.TileDie);
                StringAssert.Contains("?", room.Fp.Board.Hunt.Root.Q<Label>("hunt-damage").text);
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                await Respond(room, "card.mithril-replace");
                Assert.AreEqual("hunt.table", room.Fp.Board.View.Decision.Window);
                Assert.AreEqual(3, room.Sh.Board.View.Hunt.Damage);
                Assert.IsFalse(room.Sh.Board.View.Hunt.DamagePending);
                await Respond(room, "card.axe-and-bow");
                var damageRevision = room.Fp.Board.View.Revision;
                Click(room.Fp, "hunt-response-Ring");
                await room.At(damageRevision + 1);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(1, room.Sh.Board.View.Fellowship.MordorStep);
                Assert.IsNull(room.Fp.Board.View.Hunt);
                Assert.IsTrue(room.Fp.Board.View.HuntReport.Completed);
                Assert.IsTrue(room.Sh.Board.View.HuntReport.Entries.Any(e => e.Kind == HuntReportKind.TileReplaced));
                Assert.IsTrue(room.Sh.Board.View.HuntReport.Entries.Any(e => e.Kind == HuntReportKind.TileDrawn && e.Hunt.DamagePending && !e.Hunt.TileDie.HasValue));
            }
        }

        private static async Task Mighty()
        {
            using (var room = await Connect("mighty-attack", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var die = option.EffectChoices.First(c => c.SelectionGroup == "Miss").Id;
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                // UI-245: the paying Companion is its portrait in the Battle window.
                var aragorn = Choice(room.Fp, "effect-choice-base.character.strider");
                Assert.IsTrue(aragorn.ClassListContains(ChoiceGlow.ClassName));
                StringAssert.Contains("Aragorn", aragorn.tooltip);
                Click(room.Fp, "effect-choice-base.character.strider");
                // The Battle window lays its objects out again after a choice, as a player's next click would find them.
                await room.Settle();
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Fp, "effect-choice-" + die);
                await room.Settle();
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                Assert.IsTrue(Choice(room.Fp, "effect-choice-" + die).ClassListContains("selected"));
                await room.Confirm(room.Fp);
                Assert.AreEqual(1, room.Sh.Board.View.Battle.Attacker.Leadership);
                Assert.AreEqual(1, room.Sh.Board.View.Battle.Attacker.Hits);
                Assert.IsTrue(room.Sh.Board.View.Battle.Attacker.Dice.Single(d => d.Id == die).ConvertedHit);
                Assert.AreEqual(DecisionKind.BattleReroll, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Blade()
        {
            using (var room = await Connect("blade", ScenarioKind.CardPack))
            {
                var button = Choice(room.Fp, "effect-choice-base.character.witch-king");
                StringAssert.Contains("Witch", button.tooltip);
                Click(room.Fp, "effect-choice-base.character.witch-king");
                await room.Confirm(room.Fp);
                Assert.AreEqual(0, room.Sh.Board.View.Battle.Attacker.Hits);
                Assert.AreEqual(1, room.Sh.Board.View.Battle.Attacker.SpentHits);
                Assert.IsFalse(room.Sh.Board.View.Characters.Any(c => c.DefinitionId == "base.character.witch-king" && c.Location == "lossarnach"));
            }
        }

        private static async Task Respond(MatchTestRoom room, string id)
        {
            await room.Settle();
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            board.Cards.Close();
            board.Hunt.Close();
            await ComponentGestures.Hover(board.Hunt.Tab);
            Assert.IsFalse(board.Hunt.IsOpen, "UI-162: hovering the plaque does not reopen a closed Hunt window.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            await room.Settle();
            Assert.IsTrue(board.Hunt.IsOpen, "The console's Hunt plaque opens the window.");

            var reaction = board.Tray.Q<Button>("effect-choice-" + id);
            await ComponentGestures.Hover(reaction);
            Assert.IsEmpty(board.Cards.CardId, "Hovering a final reaction must not open a card.");
            var sourceId = board.View.Options.SelectMany(option => option.EffectChoices)
                .Single(choice => choice.Id == id).SourceId;
            PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            await room.Settle();
            var source = board.TableEffects.Root.Q<Button>("effect-source-" + sourceId);
            await ComponentGestures.Hover(source);
            Assert.IsEmpty(board.Cards.CardId);
            PointerGestureEvents.ClickAt(source);
            await room.Settle();
            Assert.AreEqual(sourceId, board.TableEffects.SelectedSource);
            EffectsNetworkTests.AssertCardInPanel(board, sourceId);
            Assert.IsTrue(board.Hunt.IsOpen, "Reading a source preserves the explicitly opened Hunt.");
            Assert.AreEqual(revision, board.View.Revision, "Inspecting a source cannot resolve its reaction.");
            LayoutTests.Save(room.Fp.Viewport, "ui74-reaction-source-" + id + ".png");

            board.TableEffects.Close();
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("effect-choice-" + id));
            await room.At(revision + 1);
        }
    }
}
