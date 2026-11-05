using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-247: a reaction is used at its source — a battle portrait, the Guide's portrait in the Fellowship panel, a table
    // card in the Effects panel (UI-430: red, its tile's click uses it), a Guide's Hunt response in the Hunt window's
    // response row (UI-360) — or, without one, at its console or tray button.
    internal static class ReactionGestures
    {
        private const string PortraitPrefix = "fellowship-portrait-";

        public static async Task Use(MatchTestRoom room, MountedMatchClient client, string reactionId)
        {
            var view = client.Board.View;
            var fighter = view.Battle == null ? null : new[] { view.Battle.Attacker, view.Battle.Defender }
                .Where(f => f != null).SelectMany(f => f.Characters).FirstOrDefault(c => BattleReactions.Of(view, c)?.Id == reactionId);
            if (fighter != null)
            {
                var portrait = client.Board.Tray.Q("portrait-" + fighter.Id);
                Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), "The reaction's portrait glows.");
                PointerGestureEvents.ClickAt(portrait);
                return;
            }
            if (FellowshipActions.ReactionOnRoster(view, reactionId))
            {
                client.Board.Fellowship.Open();
                await room.Settle();
                var guide = client.Board.Fellowship.Root.Query<Button>().ToList().First(b => b.name.StartsWith(PortraitPrefix) &&
                    FellowshipGuideText.IsGuide(view.Fellowship, b.name.Substring(PortraitPrefix.Length)));
                Assert.IsTrue(guide.ClassListContains(ChoiceGlow.ClassName), "The Guide's portrait glows.");
                PointerGestureEvents.ClickAt(guide);
                return;
            }
            if (HuntGuideResponses.InRow(view, reactionId))
            {
                await GuideResponse(room, client, reactionId);
                return;
            }
            PointerGestureEvents.ClickAt(MatchTestRoom.TrayOrConsole(client, "effect-choice-" + reactionId));
        }

        // UI-360: a Guide's Hunt response is its portrait in the Hunt window's response row, not a tray choice.
        public static async Task GuideResponse(MatchTestRoom room, MountedMatchClient client, string reactionId)
        {
            var board = client.Board;
            if (!board.Hunt.IsOpen)
                board.Hunt.Open();
            await room.Settle();
            var response = board.Hunt.Responses.Root.Q<Button>("effect-choice-" + reactionId);
            Assert.IsNotNull(response, "The Guide's response stands in the Hunt window's response row.");
            Assert.IsTrue(response.ClassListContains(ChoiceGlow.ClassName), "The Guide's response glows.");
            Assert.IsNull(board.Tray.Q<Button>("effect-choice-" + reactionId), "The tray lists no Guide response.");
            // UI-423: the Hunt window's Skip effect, beside the console's Pass.
            StringAssert.EndsWith("Or click Skip effect (or Pass) to skip it.", response.tooltip);
            PointerGestureEvents.ClickAt(response);
        }

        // UI-430 (owner): no "Use a table card" button; the lit Effects tab opens the panel, where the card is red and says
        // why, and its tile's click uses it (RespondEffect).
        public static async Task TableCard(MatchTestRoom room, MountedMatchClient client, string card)
        {
            var board = client.Board;
            Assert.IsNull(board.Console.Status.Options.Q<Button>("console-option-table-cards"), "No console button for the table card.");
            Assert.IsTrue(board.TableEffects.Tab.ClassListContains("legal-target"), "The Effects tab lights.");
            PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            var tile = board.TableEffects.Root.Q<Button>("effect-source-" + card);
            await room.Until(() => tile.enabledInHierarchy && tile.worldBound.width > 0);
            await room.Settle();
            Assert.IsTrue(tile.ClassListContains(TableEffectsView.DiscardClass), "The card is red.");
            StringAssert.StartsWith("Discard " + CardLibrary.Title(card) + " · Click to discard it and use its reaction", tile.tooltip);
            PointerGestureEvents.ClickAt(tile);
        }
    }
}
