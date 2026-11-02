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
        // UI-430: why an effect offers this card's discard now (its tile's and its ×'s tooltip), or "".
        private string offer = "";
        // UI-424: the grid the entry stands in, and its title's words measured once per text (null until measured).
        private EffectsGridArrangement arrangement = EffectsGridArrangement.Regular;
        private EffectTitleFit fit;
        private FontDefinition measuredFont;
        private float fittedSize;
        private readonly IVisualElementScheduledItem measuring;

        // UI-94: a click selects the source; its details, including a card's complete text, open beside the list (UI-424).
        // UI-221: hovering or focusing the tile unrolls its text over the other tiles (EffectUnroll).
        public EffectEntry(EffectSource source, Action<EffectSource, bool> inspect, Action<string> remove,
            Action<EffectEntry, bool> hover, Action<string> used)
        {
            Source = source;
            Root.AddToClassList("effect-entry");
            title = BoardScreen.Button(source.Title, () =>
            {
                if (offer.Length > 0)
                    remove(Source.SourceId);
                else if (use.Length > 0)
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
            // UI-424: the words are measured once the title has its style, outside layout callbacks.
            measuring = title.schedule.Execute(MeasureTitle);
            measuring.Pause();
            title.RegisterCallback<AttachToPanelEvent>(_ => RequestMeasure());
            title.RegisterCallback<GeometryChangedEvent>(_ => RequestMeasure());
        }

        // UI-424: the entry's size, its title's height and type follow the grid's arrangement; the title fits its tile.
        public void Arrange(EffectsGridArrangement next)
        {
            arrangement = next;
            Root.style.width = next.EntryWidth;
            Root.style.height = next.EntryHeight;
            Root.style.marginBottom = next.RowGap;
            title.style.height = next.TitleHeight;
            FitTitle();
        }

        // A title measured before its style was resolved (another font) is measured again.
        private void RequestMeasure()
        {
            if (fit != null && !measuredFont.Equals(title.resolvedStyle.unityFontDefinition))
                fit = null;
            if (fit == null && title.panel != null)
                measuring.ExecuteLater(0);
        }

        private void MeasureTitle()
        {
            var size = title.resolvedStyle.fontSize;
            if (fit != null || title.panel == null || float.IsNaN(size) || size <= 0)
                return;
            var runs = EffectTitleFit.Measure(EffectTitleFit.Split(title.text), TextWidth);
            fit = new EffectTitleFit(runs, TextHeight("A"), TextHeight("A\nA"), size);
            measuredFont = title.resolvedStyle.unityFontDefinition;
            FitTitle();
        }

        private float TextWidth(string text) =>
            title.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        private float TextHeight(string text) =>
            title.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;

        // The arrangement's type size, or the largest smaller one whose wrap fits the tile (EffectTitleFit).
        private void FitTitle()
        {
            var width = arrangement.TitleTextWidth(removal.style.display == DisplayStyle.Flex, title.ClassListContains("effect-card-tile"));
            var size = fit?.Size(arrangement.TitleSize, width, arrangement.TitleTextHeight) ?? arrangement.TitleSize;
            if (Mathf.Approximately(size, fittedSize))
                return;
            fittedSize = size;
            title.style.fontSize = size;
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
            if (title.text != source.Title)
            {
                title.text = source.Title;
                fit = null;
                RequestMeasure();
            }
            inspectTooltip = source.Kind == "Ability" ? "Show this public ability beside the list"
                : CardLibrary.Cards.ContainsKey(source.SourceId) ? "Show the complete Event and Combat text beside the list" : "Show this effect beside the list";
            title.tooltip = TitleTooltip();
            owner.text = source.Owner == Wotr.Contracts.Side.FreePeoples ? "FP ◆" : "SH ◆";
            owner.tooltip = BoardScreen.SideName(source.Owner);
            Root.EnableInClassList("shadow-effect", source.Owner == Wotr.Contracts.Side.Shadow);
        }

        public void Context(ComponentAction action, bool enabled, Wotr.Contracts.PlayerView view)
        {
            WithdrawOffer();
            removal.style.display = action == null ? DisplayStyle.None : DisplayStyle.Flex;
            removal.SetEnabled(enabled && action?.Enabled == true);
            removal.tooltip = action == null ? "" : "Remove " + Source.Title + " · " + EffectsPresentation.RemovalCost(view, action) +
                (action.Enabled ? "" : " Select an eligible die.");
            title.EnableInClassList("legal-target", action?.Enabled == true);
            FitTitle();
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
            title.tooltip = TitleTooltip();
            ChoiceGlow.Apply(title, use.Length > 0);
            if (use.Length > 0)
                title.EnableInClassList("legal-target", true);
        }

        // UI-214: a card's effect offers this source (The Nazgûl Strike! discards a table card); its × chooses it. UI-430
        // (owner): the card is red instead of lit, and a click on the card itself chooses it too; both say why.
        public void Offer(string tooltip, bool enabled)
        {
            offer = tooltip ?? "";
            removal.style.display = DisplayStyle.Flex;
            removal.SetEnabled(enabled);
            removal.tooltip = offer;
            title.EnableInClassList("legal-target", false);
            title.EnableInClassList(TableEffectsView.DiscardClass, enabled);
            title.tooltip = TitleTooltip();
            FitTitle();
        }

        private void WithdrawOffer()
        {
            if (offer.Length == 0)
                return;
            offer = "";
            title.RemoveFromClassList(TableEffectsView.DiscardClass);
            title.tooltip = TitleTooltip();
        }

        // What a click on the card itself does: discard it for an effect (UI-430), use it (UI-272), or show its text.
        private string TitleTooltip() => offer.Length > 0 ? offer : use.Length > 0 ? use : inspectTooltip;
    }
}
