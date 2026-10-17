using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class BoardCompletionEntryChecks
    {
        internal const string Serpent = "kings.character.black-serpent";
        internal const string Council = ExpansionDraft.CouncilPolitics;
        internal const string Region = "near-harad";

        internal static CharacterActionOption Choice(BoardScreen board) => board.View.Options
            .Single(option => option.Kind == CommandKind.EnterCharacter).CharacterActions
            .Single(choice => choice.CharacterId == Serpent);

        internal static async Task Prepare(BoardScreen board, string die, CancellationToken token)
        {
            var choice = Choice(board);
            CollectionAssert.Contains(choice.Dice, die);
            CollectionAssert.Contains(choice.CouncilTokens, Council);
            await Press(board.Root, "pool-" + Council, token);
            await Press(board.Root, "characters-tab", token);
            // UI-108/UI-142: the Serpent enters by a click on its row; no inspector window or entry button.
            await Press(board.Root, "character-roster-" + Serpent, token);
            await Press(board.OwnDice, die, token);
            Assert.AreNotSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display);
            Assert.AreEqual(die, board.ActionDie.DieId);
        }

        internal static async Task<Vector2> Target(BoardScreen board, CancellationToken token)
        {
            board.Characters.Close();
            board.Map.Focus(Region);
            await Task.Delay(100, token);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(Region));
            return MapContourProbe.ReachRegion(board.Map, Region);
        }

        internal static async Task Press(VisualElement root, string name, CancellationToken token)
        {
            await ReturnTestFixture.Until(() => root.Q<Button>(name)?.enabledInHierarchy == true &&
                root.Q<Button>(name).worldBound.height > 0, token);
            await Task.Delay(80, token);
            PointerGestureEvents.ClickAt(root.Q<Button>(name));
            await Task.Delay(80, token);
        }

        internal static async Task Games(MountedFlowClient client, CancellationToken token)
        {
            if (!client.Board.Menu.IsOpen)
                await Press(client.Root, "open-board-menu", token);
            await Press(client.Root, "back-to-games", token);
            await ReturnTestFixture.Until(() => client.Board == null, token);
            await client.LocalWrites;
        }

        internal static void Entered(PlayerView view, GameCommand command)
        {
            Assert.AreEqual(command.ExpectedRevision + 1, view.Revision);
            Assert.AreEqual(1, view.Characters.Count(character => character.Id == Serpent));
            Assert.AreEqual(Region, view.Characters.Single(character => character.Id == Serpent).Location);
            Assert.IsTrue(view.CouncilTokens.Single(value => value.Id == Council).Spent);
            Assert.AreEqual(1, view.CouncilTokens.Count(value => value.Spent));
            Assert.AreEqual(DieLocation.Spent, view.Dice.Single(value => value.Id == command.Payload.DieId).Location);
            Assert.AreEqual(1, view.Dice.Count(value => value.Owner == Side.Shadow && value.Location == DieLocation.Spent));
            Assert.AreEqual(DieLocation.Removed, view.Dice.Single(value => value.Owner == Side.Shadow && value.Source == "ruler").Location,
                "The extra Ruler becomes available only at a later Recover.");
        }
    }
}
