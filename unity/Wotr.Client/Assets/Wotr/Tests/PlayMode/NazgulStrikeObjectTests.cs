using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-430 (owner, 2026-10-30): The Nazgûl Strike!'s options are its board's objects, on a real server (the Testing
    // catalog's layout-console-options: four Free Peoples Character cards on the table, the Witch-king and three Nazgûl with
    // the Fellowship, one Eye in the Hunt Box). No console option stands; a red table card's tile, the glowing Hunt Box and
    // the console's ✓ each send ChooseEffect with exactly the payload its old console button sent. Every layout of the
    // table-card test leaves .artifacts/screenshots/ui430-strike-<width>-<scale>.png.
    public sealed class NazgulStrikeObjectTests
    {
        private const string Scenario = "layout-console-options";
        private const string Staff = "base.fp.character.08";

        [UnityTest] public IEnumerator ARedTableCardsTileSendsTheOldDiscardButtonsPayload() => Wait(Sends(Staff, TableCard));
        [UnityTest] public IEnumerator TheGlowingHuntBoxSendsTheOldHuntRollButtonsPayload() => Wait(Sends(NazgulStrikeChoice.HuntRoll, HuntBox));
        [UnityTest] public IEnumerator TheConsolesConfirmSendsTheOldFinishButtonsPayload() => Wait(Sends(FinishingOption.StrikeFinish, Confirm));

        // The payload the old console buttons ("Discard a table card" and the card's ×, "Roll for the Hunt", "Finish") sent,
        // recorded on a real server at 7ddddb8e (the option's id, every other field at its default).
        private static string OldButtonPayload(string option) =>
            "{\"Stepwise\":false,\"DieId\":\"\",\"RingId\":\"\",\"RulerTokenId\":\"\",\"CouncilTokenId\":\"\",\"Deck\":\"\",\"Face\":null," +
            "\"Ids\":[\"" + option + "\"],\"NationId\":\"\",\"Placements\":[],\"MovementCost\":1,\"Moves\":[],\"CardId\":\"\",\"CardCost\":4," +
            "\"ReactionId\":\"\",\"GuideId\":\"\",\"Route\":[],\"CompanionMoves\":[],\"HuntResponse\":0,\"Losses\":[],\"RegionId\":\"\"," +
            "\"AutoDecision\":null,\"CharacterId\":\"\",\"AbilityId\":\"\"}";

        private static async Task Sends(string option, Func<TestScenarioRoom, MountedMatchClient, Task> click)
        {
            var room = await TestScenarioRoom.Start(Scenario);
            try
            {
                var client = room.Client(Side.Shadow);
                GameCommand sent = null;
                CommandReceipt answer = null;
                client.Interaction.CommandAnswered += (command, receipt, _) =>
                {
                    sent = command;
                    answer = receipt;
                };
                await room.Settle();
                AssertObjectsLit(client.Board);
                await click(room, client);
                await room.Until(() => sent != null);
                Assert.AreEqual(CommandKind.ChooseEffect, sent.Kind);
                Assert.AreEqual(OldButtonPayload(option), JsonConvert.SerializeObject(sent.Payload), "The object sends what the button sent.");
                Assert.IsTrue(answer.Accepted, answer.Error);
            }
            finally
            {
                await room.End();
            }
        }

        // No option strip: the card's chip, the lit Effects tab, the glowing Hunt Box and the lit ✓ carry the choice.
        internal static void AssertObjectsLit(BoardScreen board)
        {
            Assert.AreEqual(NazgulStrikeChoice.Presentation, board.View.Decision.Presentation);
            CollectionAssert.IsEmpty(board.Console.Status.Options.Query<Button>().ToList().Select(button => button.name), "No console option.");
            CollectionAssert.IsEmpty(board.Console.Status.RaisedOptions.Query<Button>(className: "console-option").ToList().Where(Shown)
                .Select(button => button.name), "No option strip above the console.");
            // The card's name chip stays, at its full text: where the line has no room for it, it stands above the console.
            var chip = board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList().Single(Shown);
            Assert.AreEqual("✓ The Nazgûl Strike!", chip.text);
            Assert.IsFalse(chip.isElided, "The card's name is not cut.");
            Assert.IsFalse(board.DecisionWindow.IsOpen);
            Assert.IsTrue(board.TableEffects.Tab.ClassListContains("legal-target"), "The Effects tab lights.");
            var confirm = board.Console.Actions.Confirm;
            Assert.IsTrue(confirm.enabledSelf && confirm.ClassListContains(ChoiceGlow.ClassName), "The ✓ is lit.");
            Assert.AreEqual("Finish · End The Nazgûl Strike! without discarding a table card or rolling for the Hunt", confirm.tooltip);
            var hunt = board.Hunt.Tab;
            Assert.IsTrue(hunt.ClassListContains(ChoiceGlow.ClassName), "The Hunt Box glows.");
            Assert.AreEqual("Roll for the Hunt · Click to start a Hunt roll now because of The Nazgûl Strike!: a Nazgûl is in the " +
                "Fellowship's region · The Fellowship does not move and no die is spent · 1 Eye in the Hunt Box", hunt.tooltip);
            Assert.AreEqual("Discard: red card in Effects · Roll: Hunt Box · ✓ finishes",
                board.Console.Status.Hint.text);
        }

        // The Effects tab's click opens the panel, where the offered cards are red and say why.
        internal static async Task<Button> RedTile(TestScenarioRoom room, BoardScreen board, string card)
        {
            if (!board.TableEffects.Pinned)
                PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            var tile = board.TableEffects.Root.Q<Button>("effect-source-" + card);
            await room.Until(() => tile.enabledInHierarchy && tile.worldBound.width > 0);
            await room.Settle();
            Assert.IsTrue(tile.ClassListContains(TableEffectsView.DiscardClass), card + " is red.");
            Assert.IsFalse(tile.ClassListContains("legal-target"), "No green or gold look beats the red.");
            StringAssert.StartsWith("Discard " + CardLibrary.Title(card) + " · Click to discard it because of The Nazgûl Strike!", tile.tooltip);
            return tile;
        }

        private static async Task TableCard(TestScenarioRoom room, MountedMatchClient client)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    using var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, width, scale);
                    await room.Settle();
                    await RedTile(room, client.Board, Staff);
                    AssertObjectsLit(client.Board);
                    LayoutTests.Save(viewport.Texture, $"ui430-strike-{width}-{scale:0.00}.png");
                }
            await room.Settle();
            PointerGestureEvents.ClickAt(await RedTile(room, client.Board, Staff));
        }

        private static async Task HuntBox(TestScenarioRoom room, MountedMatchClient client)
        {
            await room.Settle();
            PointerGestureEvents.ClickAt(client.Board.Hunt.Tab);
            Assert.IsFalse(client.Board.Hunt.IsOpen, "The offered click rolls; it does not open the Hunt window.");
        }

        private static async Task Confirm(TestScenarioRoom room, MountedMatchClient client)
        {
            await room.Settle();
            PointerGestureEvents.ClickAt(client.Board.Console.Actions.Confirm);
        }

        private static bool Shown(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return element.worldBound.width > 0;
        }
    }
}
