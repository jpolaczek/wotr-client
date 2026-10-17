#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class CompanionSetupProfileScene : IBoardProfileWorkload
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private readonly CompanionVersionOption[] groups;
        private readonly Button[] portraits;
        private readonly string[] guides;
        private int step;
        private string figure, selected = "", guide = "";
        public string DiagnosticState => $"step={step}, compared={figure}, version={selected}, guide={guide}";

        public CompanionSetupProfileScene(BoardScreen board, Action<Button> click)
        {
            this.board = board;
            this.click = click;
            var option = board.View.Options.Single(candidate => candidate.Kind == CommandKind.ChooseCompanionVersions);
            groups = option.CompanionVersions.ToArray();
            guides = option.Targets.ToArray();
            portraits = groups.Select(group => board.CompanionSetup.Root.Q<Button>("setup-portrait-" + group.FigureId)).ToArray();
            figure = groups[0].FigureId;
        }

        public void Tick()
        {
            var index = step / 4 % groups.Length;
            var group = groups[index];
            switch (step++ % 4)
            {
                case 0:
                    figure = group.FigureId;
                    selected = "";
                    click(portraits[index]);
                    break;
                case 1:
                    selected = group.AlternateId;
                    click(board.CompanionSetup.Root.Q<Button>("version-" + selected));
                    break;
                case 2:
                    guide = guides[(step / 4) % guides.Length];
                    click(board.CompanionSetup.Root.Q<Button>("starting-guide-" + guide));
                    break;
                default:
                    selected = group.BaseId;
                    click(board.CompanionSetup.Root.Q<Button>("version-" + selected));
                    break;
            }
        }

        public void Verify()
        {
            BoardProfileCheck.Require(board.CompanionSetup.IsOpen && board.Tray.parent == board.CompanionSetup.DecisionHost,
                "The private setup lost its sole decision host.");
            BoardProfileCheck.Require(board.CompanionSetup.ComparedFigure == figure, "Wrong compared companion.");
            BoardProfileCheck.Selected(board.CompanionSetup.Root.Q<Button>("setup-portrait-" + figure), true);
            if (selected.Length > 0)
                BoardProfileCheck.Selected(board.CompanionSetup.Root.Q<Button>("version-" + selected), true);
            if (guide.Length > 0)
                BoardProfileCheck.Selected(board.CompanionSetup.Root.Q<Button>("starting-guide-" + guide), true);
            for (var index = 0; index < groups.Length; index++)
                BoardProfileCheck.Require(ReferenceEquals(portraits[index], board.CompanionSetup.Root.Q<Button>(
                    "setup-portrait-" + groups[index].FigureId)), "A setup selection rebuilt its unchanged portraits.");
        }
    }
}
#endif
