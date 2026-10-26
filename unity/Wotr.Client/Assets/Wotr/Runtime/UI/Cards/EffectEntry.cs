using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    internal sealed class EffectEntry
    {
        public VisualElement Root { get; } = new VisualElement();
        public EffectSource Source { get; private set; }
        private readonly Button title, removal;
        private readonly Label owner = new Label();
        private string inspectTooltip = "";
        // UI-272: what a click on the card itself does now (Lore before Dismay), or "" when the click inspects it.
        private string use = "";

        // UI-94: a click selects the source; its details, including a card's complete text, open below the list.
        // UI-221: hovering or focusing the tile unrolls its text over the other tiles (EffectUnroll).
        public EffectEntry(EffectSource source, Action<EffectSource, bool> inspect, Action<string> remove,
            Action<EffectEntry, bool> hover, Action<string> used)
        {
            Source = source;
            Root.AddToClassList("effect-entry");
            title = BoardScreen.Button(source.Title, () =>
            {
                if (use.Length > 0)
                    used(Source.SourceId);
                else
                    inspect(Source, true);
            });
            title.AddToClassList("effect-source");
            title.name = "effect-source-" + source.SourceId;
            if (CardLibrary.Cards.ContainsKey(source.SourceId))
                CardTile(source.SourceId);
            Root.name = "effect-entry-" + source.Key;
            Root.Add(owner);
            Root.Add(title);
            removal = IconButton.Create("close", "Close", () => remove(Source.SourceId));
            removal.name = "remove-table-" + source.SourceId;
            removal.AddToClassList("effect-remove");
            removal.style.display = DisplayStyle.None;
            Root.Add(removal);
            Root.RegisterCallback<PointerEnterEvent>(e =>
            {
                if (e.pressedButtons == 0)
                    hover(this, true);
            });
            Root.RegisterCallback<PointerLeaveEvent>(_ => hover(this, false));
            title.RegisterCallback<FocusInEvent>(_ => hover(this, true));
            title.RegisterCallback<FocusOutEvent>(_ => hover(this, false));
        }

        // UI-288: a card is a small parchment card, as in the hand, with the symbol printed on its corner.
        private void CardTile(string card)
        {
            title.AddToClassList("effect-card-tile");
            var symbol = new ActionSymbol(CardLibrary.Face(card));
            symbol.AddToClassList(ActionSymbol.InkClass);
            title.Add(symbol);
        }

        public void Apply(EffectSource source)
        {
            Source = source;
            title.text = source.Title;
            inspectTooltip = source.Kind == "Ability" ? "Show this public ability below"
                : CardLibrary.Cards.ContainsKey(source.SourceId) ? "Show the complete Event and Combat text below" : "Show this effect below";
            title.tooltip = use.Length > 0 ? use : inspectTooltip;
            owner.text = source.Owner == Wotr.Contracts.Side.FreePeoples ? "FP ◆" : "SH ◆";
            owner.tooltip = BoardScreen.SideName(source.Owner);
            Root.EnableInClassList("shadow-effect", source.Owner == Wotr.Contracts.Side.Shadow);
        }

        public void Context(ComponentAction action, bool enabled, Wotr.Contracts.PlayerView view)
        {
            removal.style.display = action == null ? DisplayStyle.None : DisplayStyle.Flex;
            removal.SetEnabled(enabled && action?.Enabled == true);
            removal.tooltip = action == null ? "" : "Remove " + Source.Title + " · " + EffectsPresentation.RemovalCost(view, action) +
                (action.Enabled ? "" : " Select an eligible die.");
            title.EnableInClassList("legal-target", action?.Enabled == true);
        }

        // UI-272: a decision uses this card itself (The Lore of the Elven Rings before Dismay): the card glows, its tooltip
        // says what the click does, and the click uses it instead of showing its text.
        // Applied after the removal context, whose lit state a card without a use keeps.
        public void Use(string tooltip)
        {
            var next = tooltip ?? "";
            if (next.Length == 0 && use.Length == 0)
                return;
            use = next;
            title.tooltip = use.Length > 0 ? use : inspectTooltip;
            ChoiceGlow.Apply(title, use.Length > 0);
            if (use.Length > 0)
                title.EnableInClassList("legal-target", true);
        }

        // UI-214: a card's effect offers this source (The Nazgûl Strike! discards a table card); its × chooses it.
        public void Offer(string tooltip, bool enabled)
        {
            removal.style.display = DisplayStyle.Flex;
            removal.SetEnabled(enabled);
            removal.tooltip = tooltip;
            title.EnableInClassList("legal-target", enabled);
        }
    }
}
