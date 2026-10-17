using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-94: the complete text of an Effects source card on parchment, under the source's details,
    // so reading a source needs no separate card window. The title stays in the details heading.
    internal sealed class EffectCardText
    {
        public VisualElement Root { get; } = new VisualElement { name = "effect-card" };
        private readonly VisualElement heading = BoardScreen.Row("effect-card-heading");
        private readonly Label kind = new Label { name = "effect-card-kind" };
        private readonly Label eventText = new Label { name = "effect-card-event" };
        private readonly VisualElement combat = new VisualElement { name = "effect-card-combat" };
        private readonly Label combatHeading = new Label { name = "effect-card-combat-heading" };
        private readonly Label combatText = new Label { name = "effect-card-combat-text" };
        private ActionSymbol symbol;
        private DieFace? symbolFace;

        public EffectCardText()
        {
            heading.Add(kind);
            Root.Add(heading);
            Root.Add(eventText);
            combat.Add(combatHeading);
            combat.Add(combatText);
            Root.Add(combat);
        }

        public void Show(CardText card, string text, bool ended)
        {
            Symbol(Enum.TryParse<DieFace>(card.EventType, out var face) ? face : DieFace.Event);
            kind.text = CardTextFormat.Kind(card);
            eventText.text = text;
            combat.style.display = card.HasCombat ? DisplayStyle.Flex : DisplayStyle.None;
            combatHeading.text = CardTextFormat.CombatHeading(card);
            combatText.text = card.Combat;
            Root.EnableInClassList("effect-card-ended", ended);
        }

        private void Symbol(DieFace face)
        {
            if (symbolFace == face)
                return;
            symbol?.RemoveFromHierarchy();
            symbolFace = face;
            symbol = new ActionSymbol(face);
            symbol.AddToClassList(ActionSymbol.InkClass);
            heading.Insert(0, symbol);
        }
    }
}
