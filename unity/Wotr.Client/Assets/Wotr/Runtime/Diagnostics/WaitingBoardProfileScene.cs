#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class WaitingBoardProfileScene : IBoardProfileWorkload
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private int step;
        private bool roster, menu;
        public string DiagnosticState => $"step={step}, waiting=Shadow, roster={roster}, menu={menu}";

        public WaitingBoardProfileScene(BoardScreen board, Action<Button> click)
        {
            this.board = board;
            this.click = click;
        }

        public void Tick()
        {
            switch (step++ % 4)
            {
                case 0:
                    click(board.Characters.Tab);
                    roster = true;
                    break;
                case 1:
                    click(board.Characters.Root.Q<Button>("close-characters"));
                    roster = false;
                    break;
                case 2:
                    click(board.Menu.Toggle);
                    menu = true;
                    break;
                default:
                    click(board.Menu.Root.Q<Button>("board-menu-resume"));
                    menu = false;
                    break;
            }
        }

        public void Verify()
        {
            BoardProfileCheck.Require(board.View.You == Side.Shadow && board.View.Decision == null && board.View.Options.Count == 0 &&
                board.View.OwnCompanionVersions.Count == 0 && board.View.OwnCompanions.Count == 0,
                "The waiting projection contains private Free Peoples setup data.");
            BoardProfileCheck.Require(board.View.OwnCharacterRoster.All(row => row.Side == Side.Shadow), "The roster exposes another side.");
            BoardProfileCheck.Require(!CompanionSetupPresentation.Setup(board.View) &&
                board.Root.Q("companion-setup-choices") == null && board.Root.Q("council-homeland-choices") == null,
                "The waiting player received the private setup controls.");
            BoardProfileCheck.Require(BoardProfileCheck.Visible(board.Characters.Root) == roster && board.Menu.IsOpen == menu,
                "Waiting-player inspection did not render its requested state.");
        }
    }
}
#endif
