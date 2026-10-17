using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-130/UI-179: one nation of the Reinforcements tab: its emblem, its name and the reserves it can give as
    // reinforcements; nothing of its political track. During a recruitment a nation that can recruit has a lit row,
    // the others stay plain and inert; the server's reason is the row's tooltip. A token is a draft token: a click
    // chooses it, a drop works like the region palette.
    public sealed class ReinforcementRowView
    {
        public VisualElement Root { get; } = BoardScreen.Row("reinforcement-row");
        private readonly Label name = new Label { name = "reinforcement-name" };
        private readonly VisualElement figures = BoardScreen.Row("reinforcement-figures");
        private readonly Dictionary<UnitKind, Token> tokens = new Dictionary<UnitKind, Token>();
        private readonly HashSet<UnitKind> shown = new HashSet<UnitKind>();
        private readonly Action<string> chosen;
        private readonly string nation;

        public ReinforcementRowView(string nation, Action<string> chosen)
        {
            this.nation = nation;
            this.chosen = chosen;
            Root.name = "reinforcement-" + nation;
            Root.AddToClassList("reinforcement-row");
            Root.Add(Badge(nation));
            name.AddToClassList("reinforcement-name");
            Root.Add(name);
            figures.AddToClassList("reinforcement-figures");
            Root.Add(figures);
        }

        // UI-197: the nation's crest over a soft gilt glow.
        private static VisualElement Badge(string nation)
        {
            var badge = new VisualElement { name = "reinforcement-badge", pickingMode = PickingMode.Ignore };
            badge.AddToClassList("reinforcement-badge");
            badge.Add(IconGlow.Create("reinforcement-glow"));
            var emblem = new VisualElement { name = "reinforcement-emblem", pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("reinforcement-emblem");
            emblem.AddToClassList("nation-emblem-" + nation);
            badge.Add(emblem);
            return badge;
        }

        public void Apply(ReinforcementRow row, ReinforcementsState state)
        {
            name.text = row.Name;
            Root.tooltip = Tooltip(row);
            Root.EnableInClassList("recruitable", state.Open && state.Interactive && row.CanRecruit);
            Root.EnableInClassList("reinforcement-closed", state.Open && !row.CanRecruit);
            shown.Clear();
            foreach (var figure in row.Figures)
            {
                TokenFor(figure.Kind).Apply(figure, state, nation);
                shown.Add(figure.Kind);
            }
            // A kind the current source does not offer (a Muster token during a card plan) is hidden, not left stale.
            foreach (var pair in tokens)
                pair.Value.Button.style.display = shown.Contains(pair.Key) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The card's limits of a plan and the server's reason why the nation cannot recruit; no political status.
        private static string Tooltip(ReinforcementRow row)
        {
            var lines = new List<string> { row.Name };
            if (row.Status.Length > 0)
                lines.Add(row.Status);
            if (row.Reason.Length > 0)
                lines.Add(row.Reason);
            return string.Join("\n", lines);
        }

        private Token TokenFor(UnitKind kind)
        {
            if (tokens.TryGetValue(kind, out var token))
                return token;
            token = new Token(chosen);
            figures.Add(token.Button);
            tokens.Add(kind, token);
            return token;
        }

        private sealed class Token
        {
            public Button Button { get; }
            private readonly Image image = FigureArtwork.Image(FigureAppearance.FreeRegular);
            private readonly Label count = new Label { name = "reserve-token-count", pickingMode = PickingMode.Ignore };

            public Token(Action<string> chosen)
            {
                Button = BoardScreen.Button("", () =>
                {
                    if (Button.userData is DraftToken draft)
                        chosen(draft.Id);
                });
                Button.AddToClassList("reserve-token");
                image.AddToClassList("reserve-token-figure");
                count.AddToClassList("reserve-token-count");
                // UI-197: the figure stands in a soft gilt glow.
                Button.Add(IconGlow.Create("reserve-token-glow"));
                Button.Add(image);
                Button.Add(count);
            }

            public void Apply(ReinforcementFigure figure, ReinforcementsState state, string nation)
            {
                Button.name = state.TokenPrefix + nation + "-" + figure.Kind;
                image.sprite = FigureArtwork.Sprite(FigureAppearances.Unit(nation, figure.Kind, state.Side));
                count.text = figure.Reserve.ToString();
                Button.userData = figure.ReserveId == null ? null : new DraftToken(DraftObject.Reserve, figure.ReserveId);
                Button.SetEnabled(state.Interactive && figure.ReserveId != null);
                // Outside a recruitment the reserves are there to read, not greyed as refused.
                Button.EnableInClassList("reserve-readonly", !state.Open);
                var selected = figure.ReserveId != null && figure.ReserveId == state.Selected;
                Button.EnableInClassList("selected", selected);
                // UI-213: a token the player may choose now is lit, as figures are in the region panel.
                Button.EnableInClassList("eligible-token", state.Open && state.Interactive && figure.ReserveId != null && !selected);
                Button.tooltip = figure.Kind + " · " + figure.Reserve + " in reserve" + (!state.Open ? "" : " · " + (figure.ReserveId == null
                    ? figure.Reason.Length > 0 ? figure.Reason : "Not available now"
                    : state.TokenHint)) + (figure.Warning.Length > 0 ? "\n⚠ " + figure.Warning : "");
                Button.EnableInClassList("token-warning", figure.Warning.Length > 0);
            }
        }
    }
}
