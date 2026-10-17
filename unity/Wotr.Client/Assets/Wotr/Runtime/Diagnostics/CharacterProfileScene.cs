#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class CharacterProfileScene : IBoardProfileWorkload
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private readonly Action<VisualElement> hover;
        private readonly CharacterRosterView[] roster;
        private readonly Button[] portraits;
        private bool prepared, opened, inspecting;
        private int index;
        private string expected = "";
        public string DiagnosticState => $"character={expected}, inspected={inspecting}, index={index}";

        public CharacterProfileScene(BoardScreen board, Action<Button> click, Action<VisualElement> hover)
        {
            this.board = board;
            this.click = click;
            this.hover = hover;
            roster = CharacterRosterPresentation.Rows(board.View).Where(row =>
                CharacterRosterPresentation.Section(row) != CharacterRosterSection.Removed).ToArray();
            portraits = roster.Select(row => board.Characters.Root.Q<Button>("character-roster-" + row.FigureId)).ToArray();
            BoardProfileCheck.Require(portraits.Length > 0 && portraits.All(button => button != null), "Missing own character roster.");
        }

        public void Tick()
        {
            if (!prepared)
            {
                prepared = true;
                if (board.Fellowship.IsOpen && !board.Fellowship.Docked)
                {
                    click(board.Fellowship.Root.Q<Button>("close-fellowship"));
                    return;
                }
            }
            if (!opened)
            {
                click(board.Characters.Tab);
                opened = true;
            }
            else if (inspecting)
            {
                click(board.Characters.Inspector.Root.Q<Button>("close-character"));
                inspecting = false;
                // The inspector kept the hover panel open. Return to its tab before that hold ends.
                hover(board.Characters.Tab);
            }
            else
            {
                var selected = index++ % portraits.Length;
                expected = roster[selected].FigureId;
                click(portraits[selected]);
                inspecting = true;
            }
        }

        public void Verify()
        {
            if (!opened)
            {
                BoardProfileCheck.Require(!board.Fellowship.IsOpen, "Initial Fellowship panel did not close.");
                return;
            }
            BoardProfileCheck.Require(BoardProfileCheck.Visible(board.Characters.Root), "The character roster was not rendered.");
            BoardProfileCheck.Require(BoardProfileCheck.Visible(board.Characters.Inspector.Root) == inspecting,
                "Character inspector visibility differs from its gesture.");
            if (inspecting)
            {
                var inspector = board.Characters.Inspector;
                BoardProfileCheck.Require(inspector.FigureId == expected && inspector.DefinitionId.Length > 0,
                    "The requested character/version was not inspected.");
                // UI-139: the title and the edition are separate labels of the character card.
                var name = CharacterLibrary.Name(inspector.DefinitionId);
                var edition = inspector.Root.Q<Label>("character-edition");
                BoardProfileCheck.Require(inspector.Root.Query<Label>().ToList().Any(label => label.text == name) &&
                    edition?.text == CharacterLibrary.Edition(inspector.DefinitionId),
                    "Full character title and edition are missing.");
            }
            for (var position = 0; position < roster.Length; position++)
                BoardProfileCheck.Require(ReferenceEquals(portraits[position], board.Characters.Root.Q<Button>(
                    "character-roster-" + roster[position].FigureId)), "Inspection rebuilt an unchanged roster row.");
        }
    }
}
#endif
