using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-271 (UX_SPEC): a character's ability or reaction used on the character's own figure: the Balrog's activation
    // and deactivation with the paying die selected, the Chief's He Sees, He Knows in its reaction window. The figure's
    // region glows on the map, its panel lights the figure and says what the click does, and the click uses it at once.
    // UI-347 (D11): so does A Balrog is Come!'s order, on the Balrog (alone) and the figures of his army (with it).
    public sealed class FigureAbilityInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<ComponentAction> select;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Action<string> carryArmy;
        private readonly Dictionary<string, (string Offer, Action Use)> uses = new Dictionary<string, (string, Action)>();
        private PlayerView view;
        private TurnDraft draft;

        // carryArmy: the region whose army a click sent "with the army", so its move starts when the card's army step opens.
        public FigureAbilityInteraction(BoardScreen board, Action<ComponentAction> select, Action<Action, bool> edit, Func<bool> busy,
            Action<string> carryArmy)
        {
            this.board = board;
            this.select = select;
            this.edit = edit;
            this.busy = busy;
            this.carryArmy = carryArmy;
        }

        public void Refresh(PlayerView latest, TurnDraft selection, IEnumerable<ComponentAction> components)
        {
            view = latest;
            draft = selection;
            uses.Clear();
            if (draft.Kind == null)
                foreach (var ability in components.Where(item => item.Enabled && CharacterAbilityPlaces.OnFigure(item)))
                    uses[ability.OwnerId] = (CharacterAbilityPlaces.FigureOffer(ability), () => select(ability));
            var chief = ChiefReactionObjects.FollowFigure(view);
            if (chief.Length > 0 && draft.Kind is null or CommandKind.RespondEffect)
            {
                uses[chief] = (ChiefReactionObjects.FollowOffer, () => edit(() => Respond(ChiefReactionObjects.Follow), true));
                // A reaction window has no component to light the Chief's region; the figure's own use does.
                board.Map.HighlightOwners(view.Characters.Where(c => c.Id == chief).Select(c => c.Location));
            }
            BalrogOrder();
            if (uses.Count == 0)
                return;
            var previous = board.Hover.Panel.Offer;
            board.Hover.Panel.Offer = id => uses.TryGetValue(id, out var use) ? use.Offer : previous(id);
            PanelFigureHighlight.Add(board.Hover.Panel, uses.Keys.ToHashSet(), new HashSet<string>(), busy);
        }

        // A lit figure clicked in the region panel.
        public bool Figure(string id)
        {
            if (busy() || !uses.TryGetValue(id, out var use))
                return false;
            use.Use();
            return true;
        }

        private void BalrogOrder()
        {
            var figures = BalrogCardOrder.Figures(view);
            if (figures.Count == 0 || draft.Kind is not (null or CommandKind.ChooseEffect))
                return;
            var region = BalrogCardOrder.Region(view);
            foreach (var figure in figures)
            {
                var choice = figure.Value;
                uses[figure.Key] = (BalrogCardOrder.Offer(choice), () => Order(choice, region));
            }
            board.Map.HighlightOwners(new[] { region });
        }

        private void Order(string choice, string region)
        {
            edit(() =>
            {
                if (draft.Kind != CommandKind.ChooseEffect)
                    DraftGesture.Choose(draft, view, CommandKind.ChooseEffect);
                CardDraft.Choose(draft, choice);
            }, true);
            if (choice == BalrogCardOrder.Army)
                carryArmy(region);
        }

        private void Respond(string reaction)
        {
            if (draft.Kind != CommandKind.RespondEffect)
                draft.Choose(CommandKind.RespondEffect);
            draft.Change(p => p.ReactionId = reaction);
        }
    }
}
