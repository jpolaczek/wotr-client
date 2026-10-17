using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-91: one card of the hand. The slot keeps its place in the grid; the parchment face is anchored
    // to the slot's bottom and unrolls upward over the cards above without moving any of them.
    public sealed class HandCard
    {
        public const string ExpandedClass = "card-expanded";
        public const string UnplayableClass = "card-unplayable";
        public Button Slot { get; }
        // An unrolled face takes the pointer, so the card stays readable and clickable over the cards it covers.
        public VisualElement Face { get; } = new VisualElement { name = "hand-card-face", pickingMode = PickingMode.Ignore };
        public string Id { get; }
        public bool Expanded => Slot.ClassListContains(ExpandedClass);
        public float ExpandedHeight { get; private set; }
        private readonly VisualElement compact = new VisualElement { name = "hand-card-compact", pickingMode = PickingMode.Ignore };
        private readonly VisualElement full = new VisualElement { name = "hand-card-full", pickingMode = PickingMode.Ignore };
        private readonly Label title = Text("hand-card-title");
        private readonly Label fullTitle = Text("hand-card-full-title");
        private readonly Label kind = Text("hand-card-kind");
        private readonly Label eventText = Text("hand-card-event");
        private readonly VisualElement combat = new VisualElement { name = "hand-card-combat", pickingMode = PickingMode.Ignore };
        private readonly Label combatHeading = Text("hand-card-combat-heading");
        private readonly Label combatText = Text("hand-card-combat-text");
        private readonly Label hint = Text("hand-card-hint");
        private readonly Label ready = Text("card-ready");
        private float collapsedHeight;
        // UI-217: the stylesheet size of the tile title, read while no fitted size overrides it.
        private float normalTitleSize;
        private bool titleShrunk;

        public HandCard(string id, Action clicked)
        {
            Id = id;
            var card = CardLibrary.Cards[id];
            Slot = BoardScreen.Button("", clicked);
            Slot.name = id;
            Slot.userData = new DraftToken(DraftObject.Card, id);
            Slot.AddToClassList("hand-card");
            Slot.AddToClassList(card.Deck == "character" ? "character-card" : "strategy-card");
            var face = Enum.TryParse<DieFace>(card.EventType, out var type) ? type : DieFace.Event;
            compact.Add(Symbol(face));
            compact.Add(title);
            full.Add(Row(Symbol(face), fullTitle));
            full.Add(kind);
            full.Add(eventText);
            combat.Add(combatHeading);
            combat.Add(combatText);
            full.Add(combat);
            full.Add(hint);
            Face.Add(full);
            Face.Add(compact);
            Face.Add(ready);
            Slot.Add(Face);
            title.RegisterCallback<GeometryChangedEvent>(_ => FitTitle());
            // Inside its slot the button's own click handles the press; above it only the face can.
            // UI-142: every click counts; a fast second click on an armed card plays it like a slow one.
            Face.RegisterCallback<ClickEvent>(e =>
            {
                if (!Slot.worldBound.Contains(e.position))
                    clicked();
            });
        }

        public void Apply(MatchConfiguration configuration, bool battle, bool fromTable)
        {
            var card = CardLibrary.Cards[Id];
            var name = battle && card.HasCombat ? card.CombatTitle : card.Title;
            if (title.text != name)
            {
                title.text = name;
                // Another name of the same width does not change the label's geometry.
                title.schedule.Execute(FitTitle);
            }
            fullTitle.text = card.Title;
            kind.text = CardTextFormat.Kind(card) + (fromTable ? " · FROM THE TABLE" : "");
            eventText.text = CardLibrary.EventText(card, configuration);
            combat.style.display = card.HasCombat ? DisplayStyle.Flex : DisplayStyle.None;
            combatHeading.text = CardTextFormat.CombatHeading(card);
            combatText.text = card.Combat;
            combat.EnableInClassList("hand-card-combat-focus", battle);
        }

        public void State(bool eligible, bool discard, string action)
        {
            Slot.EnableInClassList("playable-card", eligible);
            ready.style.visibility = eligible ? Visibility.Visible : Visibility.Hidden;
            ready.text = discard ? "×" : "✓";
            hint.text = action;
        }

        // The full layer always keeps the expanded width, so its height is known and text never reflows while the face moves.
        public void Place(float left, float top, float width, float height, float gridWidth)
        {
            // The compact board changes the tile and its title's stylesheet size; fit again from that size.
            if (!Mathf.Approximately(height, collapsedHeight))
                ResetTitle();
            collapsedHeight = height;
            Slot.style.left = left;
            Slot.style.top = top;
            Slot.style.width = width;
            Slot.style.height = height;
            full.style.width = gridWidth;
            compact.style.height = height;
            if (!Expanded)
                Rest();
        }

        public void Expand(float gridWidth, float maxHeight)
        {
            Slot.AddToClassList(ExpandedClass);
            Face.pickingMode = PickingMode.Position;
            Slot.BringToFront();
            var natural = full.layout.height;
            ExpandedHeight = Mathf.Min(maxHeight, float.IsNaN(natural) || natural <= 0 ? 220 : natural);
            Face.style.left = -Slot.layout.x;
            Face.style.width = gridWidth;
            Face.style.height = Mathf.Max(collapsedHeight, ExpandedHeight);
        }

        public void Collapse()
        {
            Slot.RemoveFromClassList(ExpandedClass);
            // A face that is still rolling back never hides the card being uncovered.
            Face.pickingMode = PickingMode.Ignore;
            Rest();
        }

        private void Rest()
        {
            Face.style.left = 0;
            Face.style.width = Slot.style.width;
            Face.style.height = collapsedHeight;
        }

        private void FitTitle()
        {
            var available = title.contentRect.width;
            if (title.panel == null || float.IsNaN(available) || available <= 0 || string.IsNullOrWhiteSpace(title.text))
                return;
            var current = title.resolvedStyle.fontSize;
            if (!titleShrunk)
                normalTitleSize = current;
            var words = title.text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(TitleWidth).ToArray();
            var space = TitleWidth("I I") - 2 * TitleWidth("I");
            var size = CardTitleFit.Size(words, space, current, normalTitleSize, available);
            if (size >= normalTitleSize)
                ResetTitle();
            // A slightly larger result can come from rounding of the measured widths; only a real gain changes the size.
            else if (size < current - .1f || size >= current + .5f)
            {
                title.style.fontSize = size;
                titleShrunk = true;
            }
        }

        private float TitleWidth(string text) =>
            title.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        private void ResetTitle()
        {
            if (!titleShrunk)
                return;
            title.style.fontSize = StyleKeyword.Null;
            titleShrunk = false;
        }

        private static ActionSymbol Symbol(DieFace face)
        {
            var symbol = new ActionSymbol(face);
            symbol.AddToClassList(ActionSymbol.InkClass);
            return symbol;
        }

        private static VisualElement Row(VisualElement first, VisualElement second)
        {
            var row = BoardScreen.Row("hand-card-heading");
            row.pickingMode = PickingMode.Ignore;
            row.Add(first);
            row.Add(second);
            return row;
        }

        private static Label Text(string name) => new Label { name = name, pickingMode = PickingMode.Ignore };
    }
}
