using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ResourceInspector
    {
        public VisualElement Root { get; } = new VisualElement { name = "resource-inspector" };
        private PlayerView view;
        private DieLocation location;
        private bool tokens;
        private readonly Action<string> choose;

        public ResourceInspector(Action<string> choose)
        {
            this.choose = choose;
            Root.AddToClassList("expansion-inspector");
            Close();
        }

        public void Apply(PlayerView latest)
        {
            view = latest;
            if (Root.style.display != DisplayStyle.None)
                Render();
        }

        public void Show(DieLocation selected)
        {
            location = selected;
            tokens = false;
            Root.style.display = DisplayStyle.Flex;
            Render();
            Root.BringToFront();
        }

        public void Close() => Root.style.display = DisplayStyle.None;

        private void Render()
        {
            Root.Clear();
            var heading = BoardScreen.Row("information-heading");
            FloatingWindow.MarkHandle(heading);
            heading.Add(BoardScreen.Title(tokens ? "Public resources" : "Dice · " + location));
            heading.Add(IconButton.Create("close", "Close", Close));
            Root.Add(heading);
            var tabs = BoardScreen.Row();
            Root.Add(tabs);
            foreach (var place in new[] { DieLocation.Spent, DieLocation.Hunt, DieLocation.Discarded, DieLocation.FellowshipBox, DieLocation.Removed })
            {
                var button = BoardScreen.Button(place == DieLocation.FellowshipBox ? "Fellowship" : place.ToString(), () => Show(place));
                button.EnableInClassList("selected", place == location);
                tabs.Add(button);
            }
            tabs.Add(BoardScreen.Button("Tokens", () => { tokens = true; Render(); }));
            if (tokens)
            {
                RenderTokens();
                return;
            }
            var dice = BoardScreen.Row("resource-detail-dice");
            Root.Add(dice);
            foreach (var die in view.Dice.Where(d => d.Location == location))
            {
                var button = BoardScreen.Button(ExpansionLabels.DieFace(die) + " " + (die.Owner == Side.FreePeoples ? "FP" : "SH") +
                    "\n" + ExpansionLabels.DieSource(die), () => choose(die.Id));
                button.name = "resource-die-" + die.Id;
                button.userData = new DraftToken(DraftObject.Die, die.Id);
                button.tooltip = DieHints.Describe(view, die);
                button.AddToClassList("expansion-die");
                button.SetEnabled(view.Options.Any(o => o.Enabled && (o.EligibleDice.Contains(die.Id) ||
                    o.Kind == CommandKind.ChooseEffect && view.Decision?.Presentation is "ActionDie" or "DieAndResult" &&
                    o.EligibleIds.Contains(die.Id))));
                dice.Add(button);
            }
            if (dice.childCount == 0)
                dice.Add(new Label("No dice here"));
        }

        private void RenderTokens()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                Root.Add(BoardScreen.Title(BoardScreen.SideName(side)));
                var row = BoardScreen.Row("resource-public-tokens");
                Root.Add(row);
                foreach (var ring in view.Rings.Where(r => r.Owner == side))
                    AddToken(row, ExpansionLabels.Ring(ring.Id), ring.Id, side);
                foreach (var token in view.RulerTokens.Where(t => t.Location ==
                    (side == Side.FreePeoples ? "free-peoples" : "dismay")))
                    AddToken(row, (side == Side.FreePeoples ? "♛ " : "◈ ") + ExpansionLabels.Token(view, token.Id), token.Id, side);
                if (side == Side.Shadow)
                    foreach (var token in view.CouncilTokens)
                        AddToken(row, ExpansionLabels.CouncilToken(token.Id) + (token.Spent ? " · Spent" : ""), token.Id, side);
                if (row.childCount == 0)
                    row.Add(new Label("No tokens"));
            }
        }

        private void AddToken(VisualElement row, string title, string id, Side side)
        {
            var label = new Label(title)
            {
                tooltip = side == view.You ? ResourcePresentation.WithExplanation(view, id, title) : title,
                focusable = true
            };
            row.Add(label);
        }
    }
}
