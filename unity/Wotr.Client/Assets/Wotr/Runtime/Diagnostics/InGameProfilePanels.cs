#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client.Diagnostics
{
    internal sealed class InGameProfilePanels
    {
        private readonly BoardScreen board;
        private readonly Action<Button> click;
        private bool open;
        private string inspected = "";
        private bool freePeoplesPage, shadowPage;
        private const string Protection = "base.fp.strategy.02";

        public InGameProfilePanels(BoardScreen board, Action<Button> click)
        {
            this.board = board;
            this.click = click;
        }

        public void Effects(int step)
        {
            switch (step % 6)
            {
                case 0: click(board.TableEffects.Tab); open = true; break;
                case 1: Inspect(Protection); break;
                case 2: Inspect(OtherSource()); break;
                // UI-221: every source is in view at once; the steps read sources instead of turning pages.
                case 3: Inspect(Protection); break;
                case 4: Inspect(OtherSource()); break;
                default: click(board.TableEffects.Root.Q<Button>("close-effects")); open = false; break;
            }
        }

        // UI-230: the resource inspector is gone; the workload renders the own resource panel, whose location counts
        // are information only.
        public void Resources(int step)
        {
            open = true;
        }

        // The opened Menu, and each turned page, lays out before the next step clicks.
        public void Victory(int step)
        {
            switch (step % 7)
            {
                case 0: click(board.Menu.Toggle); break;
                case 1: click(board.Menu.Panel.Q<Button>("board-menu-victory")); open = true; break;
                case 2: click(VictoryButton("next-FreePeoples")); freePeoplesPage = true; break;
                case 3: click(VictoryButton("next-Shadow")); shadowPage = true; break;
                case 4: click(VictoryButton("previous-FreePeoples")); freePeoplesPage = false; break;
                case 5: click(VictoryButton("previous-Shadow")); shadowPage = false; break;
                default: click(board.Victory.Root.Q<Button>("close-victory")); open = false; break;
            }
        }

        // UI-94: selecting a source shows its card text at the bottom of the panel instead of a card window.
        private void Inspect(string source)
        {
            click(board.TableEffects.Root.Q<Button>("effect-source-" + source));
            inspected = source;
        }

        private string OtherSource() => board.TableEffects.Root.Query<Button>().ToList()
            .Where(button => button.name.StartsWith("effect-source-", StringComparison.Ordinal) &&
                button.parent.resolvedStyle.display != DisplayStyle.None)
            .Select(button => button.name.Substring("effect-source-".Length))
            .First(id => id != Protection);

        private void VerifyInspected(VisualElement root)
        {
            if (board.TableEffects.SelectedSource != inspected)
                throw new InvalidOperationException("Effects did not select the requested source.");
            if (!Appearance.Closed(board.Cards.Root) && board.Cards.Root.resolvedStyle.display != DisplayStyle.None)
                throw new InvalidOperationException("An Effects source opened a separate card window.");
            if (CardLibrary.Cards.ContainsKey(inspected))
                InGameProfileScene.Visible(root.Q("effect-card"));
        }

        public void VerifyRendered(string kind)
        {
            var root = kind == "effects" ? board.TableEffects.Root : kind == "victory" ? board.Victory.Root : board.OwnDice;
            if (open != (!Appearance.Closed(root) && root.resolvedStyle.display != DisplayStyle.None))
                throw new InvalidOperationException("The requested inspection visibility was not rendered: " + kind);
            if (!open)
                return;
            InGameProfileScene.Visible(root);
            if (kind == "effects")
            {
                if (inspected.Length > 0)
                    VerifyInspected(root);
                var visible = root.Query<Button>().ToList().Count(button => button.name.StartsWith("effect-source-", StringComparison.Ordinal) &&
                    button.parent.resolvedStyle.display != DisplayStyle.None);
                if (visible != 6)
                    throw new InvalidOperationException("Every Effects source must be in view.");
            }
            else if (kind == "victory")
            {
                VerifyVictoryPage(freePeoplesPage, "angmar", "umbar");
                VerifyVictoryPage(shadowPage, "dol-amroth", "pelargir");
            }
            else if (kind.StartsWith("resources-", StringComparison.Ordinal) && root.Q<Button>("pool-Spent") == null)
                throw new InvalidOperationException("The resource panel's location counts were not rendered.");
        }

        private void VerifyVictoryPage(bool second, string first, string last)
        {
            var firstVisible = board.Victory.Root.Q("victory-region-" + first).resolvedStyle.display != DisplayStyle.None;
            var lastVisible = board.Victory.Root.Q("victory-region-" + last).resolvedStyle.display != DisplayStyle.None;
            if (firstVisible == second || lastVisible != second)
                throw new InvalidOperationException("The captured settlement page did not render.");
        }

        private Button VictoryButton(string suffix) => board.Victory.Root.Q<Button>("victory-" + suffix);
    }
}
#endif
