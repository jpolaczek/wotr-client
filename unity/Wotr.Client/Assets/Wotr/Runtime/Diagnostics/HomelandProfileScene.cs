#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class HomelandProfileScene : IBoardProfileWorkload
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private readonly CharacterActionOption companion;
        private readonly Button[] portraits;
        private int step;
        private string region = "";
        private string destination = "";
        private bool open = true;
        public bool NavigationPerformed { get; private set; }
        public Vector2 ExpectedCamera { get; private set; }
        public string DiagnosticState => $"step={step}, homeland={region}, panel={open}";

        public HomelandProfileScene(BoardScreen board, Action<Button> click, Action<VisualElement, KeyCode> key)
        {
            this.board = board;
            this.click = click;
            var option = board.View.Options.Single(candidate => candidate.Kind == CommandKind.ChooseCouncilHomelands);
            companion = option.CharacterActions.Single(character => character.CharacterId == "base.character.gimli");
            // UI-270: the homelands are chosen on the Fellowship panel's portraits and the map.
            portraits = option.CharacterActions.Select(character => board.Fellowship.Root.Q<Button>(
                "fellowship-portrait-" + character.CharacterId)).ToArray();
        }

        public void Tick()
        {
            NavigationPerformed = false;
            var cycle = step / 6;
            switch (step++ % 6)
            {
                case 0:
                    click(board.Fellowship.Root.Q<Button>("fellowship-portrait-" + companion.CharacterId));
                    break;
                case 1:
                    // A second click on the selected companion keeps its selection; Escape would be the board's Back.
                    click(board.Fellowship.Root.Q<Button>("fellowship-portrait-" + companion.CharacterId));
                    break;
                case 2:
                    destination = companion.Targets[cycle % companion.Targets.Count];
                    ExpectedCamera = BoardTerrainInput.PanTo(board.Map, destination);
                    NavigationPerformed = true;
                    break;
                case 3:
                    region = destination;
                    BoardProfileCheck.Require(board.Map.DraftOverlay.Targets.Contains(region), "Missing legal homeland target.");
                    BoardTerrainInput.Click(board.Map, region);
                    break;
                case 4:
                    click(board.Fellowship.Root.Q<Button>("fellowship-portrait-" + companion.CharacterId));
                    break;
                default:
                    click(board.Fellowship.Root.Q<Button>("homeland-stay"));
                    region = "";
                    break;
            }
        }

        public void Verify()
        {
            if (NavigationPerformed)
                BoardProfileCheck.Require(board.Map.Camera.Pan == ExpectedCamera, "The explicit homeland pan did not keep its resulting camera.");
            BoardProfileCheck.Require(board.Fellowship.IsOpen == open && board.Tray.parent == board.Fellowship.Root,
                "Homeland inspection lost its panel or decision host.");
            var portrait = board.Fellowship.Root.Q<Button>("fellowship-portrait-" + companion.CharacterId);
            BoardProfileCheck.Selected(portrait, true);
            var location = region.Length == 0 ? "" : "→ " + board.Presentation.Regions[region].Name;
            BoardProfileCheck.Require(portrait.Q<Label>("homeland-location").text == location, "Homeland draft did not render its location.");
            var tokens = board.Fellowship.Root.Q<Label>("homeland-token-effect").text;
            BoardProfileCheck.Require(tokens == CouncilHomelandPresentation.TokenEffect(region.Length == 0 ? 0 : 1),
                "Homeland draft did not render its exact token consequence.");
            foreach (var button in portraits)
                BoardProfileCheck.Require(ReferenceEquals(button, board.Fellowship.Root.Q<Button>(button.name)),
                    "A homeland edit rebuilt unchanged portraits.");
        }
    }
}
#endif
