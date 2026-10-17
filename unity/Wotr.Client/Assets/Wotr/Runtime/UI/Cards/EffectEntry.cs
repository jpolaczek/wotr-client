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

        // UI-94: a click selects the source; its details, including a card's complete text, open below the list.
        public EffectEntry(EffectSource source, Action<EffectSource, bool> inspect, Action<string> remove)
        {
            Source = source;
            Root.AddToClassList("effect-entry");
            title = BoardScreen.Button(source.Title, () => inspect(Source, true));
            title.AddToClassList("effect-source");
            title.name = "effect-source-" + source.SourceId;
            Root.name = "effect-entry-" + source.Key;
            Root.Add(owner);
            Root.Add(title);
            removal = IconButton.Create("close", "Close", () => remove(Source.SourceId));
            removal.name = "remove-table-" + source.SourceId;
            removal.AddToClassList("effect-remove");
            removal.style.display = DisplayStyle.None;
            Root.Add(removal);
        }

        public void Apply(EffectSource source)
        {
            Source = source;
            title.text = source.Title;
            title.tooltip = source.Kind == "Ability" ? "Show this public ability below"
                : CardLibrary.Cards.ContainsKey(source.SourceId) ? "Show the complete Event and Combat text below" : "Show this effect below";
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
