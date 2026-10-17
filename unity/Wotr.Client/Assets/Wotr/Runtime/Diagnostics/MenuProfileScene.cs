#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class MenuProfileScene : IBoardProfileWorkload
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private readonly bool battle;
        private readonly string choiceName;
        private Button choice;
        private string budget;
        private int step;
        private bool prepared, menu;
        public string DiagnosticState => $"step={step}, menu={menu}, selection={choiceName}";

        public MenuProfileScene(BoardScreen board, Action<Button> click, bool battle)
        {
            this.board = board;
            this.click = click;
            this.battle = battle;
            choiceName = battle ? "loss-" + board.View.Battle.Attacker.Units.Single(unit => unit.Kind == UnitKind.Elite).Id
                : "hunt-die-" + board.View.Options.Single(option => option.Kind == CommandKind.RerollHunt).EligibleIds.First();
        }

        public void Tick()
        {
            if (!prepared)
            {
                if (battle ? !board.Battles.IsOpen : !board.Hunt.IsOpen)
                {
                    click(battle ? board.Console.Status.Plaque : board.Hunt.Tab);
                    return;
                }
                choice = board.Root.Q<Button>(choiceName);
                click(choice);
                budget = Budget();
                prepared = true;
                return;
            }
            if (step++ % 2 == 0)
            {
                click(board.Menu.Toggle);
                menu = true;
            }
            else
            {
                click(board.Menu.Root.Q<Button>("board-menu-resume"));
                menu = false;
            }
        }

        public void Verify()
        {
            if (!prepared)
                return;
            BoardProfileCheck.Require(board.Menu.IsOpen == menu,
                "The menu did not render the requested open or close gesture.");
            BoardProfileCheck.Require(ReferenceEquals(choice, board.Root.Q<Button>(choiceName)),
                "Menu navigation rebuilt the pending decision control.");
            BoardProfileCheck.Selected(choice, true);
            BoardProfileCheck.Require(Budget() == budget, "Menu navigation changed the selected loss/reroll cost.");
            BoardProfileCheck.Require(battle ? board.Battles.IsOpen : board.Hunt.IsOpen,
                "Menu navigation closed the underlying decision host.");
        }

        private string Budget() => board.Root.Q<Label>(battle ? "battle-loss-budget" : "hunt-reroll-budget").text;
    }
}
#endif
