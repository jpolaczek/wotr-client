#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Newtonsoft.Json;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class HuntProfileScene
    {
        private static readonly string[] Responses = { "Ring", "Guide", "RandomCompanion" };
        private readonly BoardScreen board;
        private readonly string kind;
        private readonly Action<Button> click;
        private readonly Action<PlayerView> apply;
        private readonly PlayerView[] refresh;
        private readonly VisualElement stable;
        private int step;
        private long revision;
        private string expectedText;

        public HuntProfileScene(BoardScreen board, PlayerView view, string kind, Action<Button> click, Action<PlayerView> apply)
        {
            this.board = board;
            this.kind = kind;
            this.click = click;
            this.apply = apply;
            revision = view.Revision;
            if (kind == "mordor-refresh")
            {
                board.Fellowship.Open();
                refresh = new[] { view, JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view)) };
                refresh[1].Fellowship.Corruption = 5;
                refresh[1].Fellowship.MordorAttemptedThisTurn = true;
                stable = board.Root.Q<Label>("mordor-activity");
            }
            else
            {
                board.Hunt.Open();
                stable = board.Root.Q(kind switch
                {
                    "hunt-allocation" => "hunt-allocation-eye",
                    "hunt-reroll" => "hunt-die-roll-0",
                    "hunt-response" => "hunt-response-Ring",
                    _ => "hunt-report-entry"
                });
            }
        }

        public bool Tick()
        {
            var pan = board.Map.Camera.Pan;
            switch (kind)
            {
                case "hunt-allocation": Allocate(); break;
                case "hunt-reroll": Toggle("hunt-die-roll-0"); break;
                case "hunt-response": Response(); break;
                case "hunt-report": Report(); break;
                case "mordor-refresh": Refresh(); break;
                default: throw new InvalidOperationException("Unknown Hunt workload: " + kind);
            }
            step++;
            if (board.Root.Q(stable.name) != stable)
                throw new InvalidOperationException("The benchmark control was unnecessarily rebuilt: " + stable.name);
            if (board.Map.Camera.Pan != pan)
                throw new InvalidOperationException("A Hunt gesture moved the camera.");
            return true;
        }

        private void Allocate()
        {
            // UI-88: a pool click allocates the die and a click on its Eye returns it.
            var phase = step % 2;
            click(board.Root.Q<Button>(phase == 0 ? "allocate-0" : "allocated-allocate-0"));
            var allocated = board.Root.Q<Button>("allocated-allocate-0");
            if ((allocated.style.display.value == DisplayStyle.Flex) != (phase == 0))
                throw new InvalidOperationException("Allocation did not update the selected dice.");
        }

        private void Toggle(string name)
        {
            var button = board.Root.Q<Button>(name);
            var before = button.ClassListContains("selected");
            click(button);
            if (before == button.ClassListContains("selected"))
                throw new InvalidOperationException("The reroll selection did not change.");
        }

        private void Response()
        {
            var response = Responses[step % Responses.Length];
            var button = board.Root.Q<Button>("hunt-response-" + response);
            click(button);
            if (!button.ClassListContains("selected"))
                throw new InvalidOperationException("The Hunt response did not become selected.");
        }

        private void Report()
        {
            var label = (Label)stable;
            var before = label.text;
            click(board.Root.Q<Button>(step % 2 == 0 ? "hunt-report-previous" : "hunt-report-next"));
            expectedText = label.text;
            if (before == expectedText)
                throw new InvalidOperationException("The report entry did not change.");
        }

        private void Refresh()
        {
            var view = refresh[(step + 1) % 2];
            view.Revision = ++revision;
            apply(view);
            expectedText = view.Fellowship.MordorAttemptedThisTurn == true
                ? "✓ Move or hide attempted this turn" : "Move or hide this turn · Otherwise +1 Corruption";
            if (board.Root.Q<Label>("corruption-label").text != view.Fellowship.Corruption.ToString())
                throw new InvalidOperationException("The projection did not update Corruption.");
        }

        public void VerifyRendered()
        {
            if (stable.resolvedStyle.display == DisplayStyle.None || stable.worldBound.width <= 0 ||
                expectedText != null && ((Label)stable).text != expectedText)
                throw new InvalidOperationException("The Player did not render the expected Hunt state.");
        }
    }
}
#endif
