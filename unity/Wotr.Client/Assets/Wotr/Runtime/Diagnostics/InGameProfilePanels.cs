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
        private int page;
        private string resourceTitle;
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
                case 0: click(board.TableEffects.Tab); open = true; page = 0; break;
                case 1: Inspect(Protection); break;
                case 2: Inspect(OtherSource()); break;
                case 3: click(board.TableEffects.Root.Q<Button>("effects-next")); page = 1; break;
                case 4: click(board.TableEffects.Root.Q<Button>("effects-previous")); page = 0; break;
                default: click(board.TableEffects.Root.Q<Button>("close-effects")); open = false; break;
            }
        }

        public void Resources(int step)
        {
            switch (step % 4)
            {
                case 0:
                    click(board.OwnDice.Q<Button>("pool-Spent"));
                    resourceTitle = "Dice · Spent";
                    open = true;
                    break;
                case 1:
                    click(ResourceButton("Hunt"));
                    resourceTitle = "Dice · Hunt";
                    break;
                case 2:
                    click(ResourceButton("Tokens"));
                    resourceTitle = "Public resources";
                    break;
                default:
                    click(ResourceButton("Close"));
                    open = false;
                    break;
            }
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
            if (board.Cards.Root.resolvedStyle.display != DisplayStyle.None)
                throw new InvalidOperationException("An Effects source opened a separate card window.");
            if (CardLibrary.Cards.ContainsKey(inspected))
                InGameProfileScene.Visible(root.Q("effect-card"));
        }

        public void VerifyRendered(string kind)
        {
            var root = kind == "effects" ? board.TableEffects.Root : kind == "victory" ? board.Victory.Root : board.Resources.Root;
            if (open != (root.resolvedStyle.display != DisplayStyle.None))
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
                if (visible != (page == 0 ? 4 : 2))
                    throw new InvalidOperationException("The requested Effects page was not rendered.");
            }
            else if (kind == "victory")
            {
                VerifyVictoryPage(freePeoplesPage, "angmar", "umbar");
                VerifyVictoryPage(shadowPage, "dol-amroth", "pelargir");
            }
            else if (kind.StartsWith("resources-", StringComparison.Ordinal) &&
                !root.Query<Label>().ToList().Any(label => label.text == resourceTitle))
                throw new InvalidOperationException("The requested resource location was not rendered.");
        }

        private void VerifyVictoryPage(bool second, string first, string last)
        {
            var firstVisible = board.Victory.Root.Q("victory-region-" + first).resolvedStyle.display != DisplayStyle.None;
            var lastVisible = board.Victory.Root.Q("victory-region-" + last).resolvedStyle.display != DisplayStyle.None;
            if (firstVisible == second || lastVisible != second)
                throw new InvalidOperationException("The captured settlement page did not render.");
        }

        private Button VictoryButton(string suffix) => board.Victory.Root.Q<Button>("victory-" + suffix);
        // An icon button (UI-138, e.g. Close) names itself only in its tooltip.
        private Button ResourceButton(string text) => board.Resources.Root.Query<Button>().ToList()
            .Single(button => button.text == text || button.text.Length == 0 && button.tooltip == text);
    }
}
#endif
