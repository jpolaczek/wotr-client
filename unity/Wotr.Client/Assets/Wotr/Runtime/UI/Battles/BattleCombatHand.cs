using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-116: the Combat cards of the deciding player as parchment tiles inside the battle window. UI-147: one
    // click plays a card face down through the same path as a drop, and no window with its text opens; the
    // whole card shows while the pointer rests on its tile (UI-137). Cards whose Combat condition is not met stay
    // behind, dimmed (UI-111).
    public sealed class BattleCombatHand
    {
        public VisualElement Root { get; } = BoardScreen.Row("battle-combat-hand");
        private readonly Action<DraftToken> finalize;
        private readonly CombatCardPreview preview = new CombatCardPreview();

        public BattleCombatHand(PlayerView view, ActionOption option, Action<DraftToken> finalize = null)
        {
            this.finalize = finalize;
            Root.name = "battle-combat-hand";
            if (view.Decision?.Kind != DecisionKind.CombatCard || view.DecisionOwner != view.You) return;
            foreach (var id in option.EligibleIds.Where(CardLibrary.Cards.ContainsKey))
                Root.Add(Tile(id, option.Enabled));
            if (Root.childCount == 0) Root.Add(new Label("No eligible Combat cards") { name = "combat-choice-none" });
            foreach (var id in view.Hand.Where(id => !option.EligibleIds.Contains(id) && CardLibrary.Cards.ContainsKey(id)))
                Root.Add(Unavailable(id));
            Root.Add(preview.Root);
        }

        // UI-137: the full card shows while the pointer rests on its tile, as in the hand.
        private void Previewed(Button tile, string id, string text)
        {
            tile.RegisterCallback<PointerEnterEvent>(e =>
            {
                if (e.pressedButtons == 0)
                    preview.Show(tile, id, text);
            });
            tile.RegisterCallback<PointerLeaveEvent>(_ => preview.Hide());
            tile.RegisterCallback<PointerDownEvent>(_ => preview.Hide(), TrickleDown.TrickleDown);
        }

        private Button Tile(string id, bool enabled)
        {
            var card = CardLibrary.Cards[id];
            var button = new Button { name = "combat-choice-" + id };
            button.clicked += () => Choose(id);
            button.userData = new DraftToken(DraftObject.Card, id, CommandKind.CommitCombatCard);
            button.AddToClassList("combat-choice");
            button.EnableInClassList("playable-card", enabled);
            button.SetEnabled(enabled);
            button.tooltip = "Click to play face down";
            Face(button, card, card.Combat, "combat-choice-text");
            Previewed(button, id, card.Combat);
            button.AddToClassList(RuntimeTooltip.RichHoverClass);
            return button;
        }

        private Button Unavailable(string id)
        {
            var card = CardLibrary.Cards[id];
            var condition = CombatCardCondition.Text(id);
            var button = new Button { name = "combat-unavailable-" + id, tooltip = card.CombatTitle + " · " + condition };
            Previewed(button, id, card.Combat + "\n" + condition);
            button.AddToClassList("combat-choice");
            button.AddToClassList("combat-choice-unavailable");
            button.SetEnabled(false);
            Face(button, card, condition, "combat-choice-condition");
            return button;
        }

        // Parchment face: the deck symbol in ink, initiative and the Combat title, then the Combat text or the
        // unmet condition.
        private static void Face(Button button, CardText card, string body, string bodyName)
        {
            var heading = BoardScreen.Row("combat-choice-heading");
            heading.pickingMode = PickingMode.Ignore;
            var face = Enum.TryParse<DieFace>(card.EventType, out var type) ? type : DieFace.Event;
            var symbol = new ActionSymbol(face) { pickingMode = PickingMode.Ignore };
            symbol.AddToClassList(ActionSymbol.InkClass);
            heading.Add(symbol);
            heading.Add(new Label(string.Join("/", card.Initiative) + " · " + card.CombatTitle)
                { name = "combat-choice-title", pickingMode = PickingMode.Ignore, enableRichText = false });
            button.Add(heading);
            button.Add(new Label(body) { name = bodyName, pickingMode = PickingMode.Ignore, enableRichText = false });
        }

        // The tile's click plays the card. Public so the gesture can be driven without a panel.
        public void Choose(string id)
        {
            preview.Hide();
            finalize?.Invoke(new DraftToken(DraftObject.Card, id, CommandKind.CommitCombatCard));
        }
    }
}
