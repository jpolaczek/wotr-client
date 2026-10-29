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
    // UI-417 (owner): fewer clicks: his lit army's marker moves that army with him at once, and "alone" arms him in the
    // move that follows, so the next click is his destination.
    public sealed class FigureAbilityInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<ComponentAction> select;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Action<string> carryArmy;
        private readonly Func<string, bool> armFigure;
        private readonly Dictionary<string, (string Offer, Action Use)> uses = new Dictionary<string, (string, Action)>();
        private PlayerView view;
        private TurnDraft draft;
        // UI-417: the Balrog's region after "alone", so his move starts armed when the card's move step opens.
        private string carriedAlone = "";

        // carryArmy: the region whose army a click sent "with the army", so its move starts when the card's army step opens.
        // armFigure: arms the figures of a region in a card's character move (the Balrog alone).
        public FigureAbilityInteraction(BoardScreen board, Action<ComponentAction> select, Action<Action, bool> edit, Func<bool> busy,
            Action<string> carryArmy, Func<string, bool> armFigure)
        {
            this.board = board;
            this.select = select;
            this.edit = edit;
            this.busy = busy;
            this.carryArmy = carryArmy;
            this.armFigure = armFigure;
        }

        public void Refresh(PlayerView latest, TurnDraft selection, IEnumerable<ComponentAction> components)
        {
            view = latest;
            draft = selection;
            uses.Clear();
            StartCarriedAlone();
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

        // UI-374: the Balrog's lit region opens his panel, also while the card's choice is drafted; its figures choose.
        public bool Region(string region)
        {
            if (busy() || view == null || !BalrogCardOrder.Active(view) || BalrogCardOrder.Figures(view).Count == 0 ||
                BalrogCardOrder.Region(view) != region)
                return false;
            board.Hover.OpenDecision(region, view.Decision.Id);
            return true;
        }

        // UI-417 (owner): the Balrog's lit army on the map moves with him at once (his army without other figures moves him
        // alone); his region's panel still offers "alone" on his figure.
        public bool Army(DraftToken army)
        {
            if (busy() || view == null || army.Side != view.You || !BalrogCardOrder.Active(view) || BalrogCardOrder.Region(view) != army.Id)
                return false;
            var choices = BalrogCardOrder.Figures(view).Values.ToHashSet();
            if (choices.Count == 0)
                return false;
            Order(choices.Contains(BalrogCardOrder.Army) ? BalrogCardOrder.Army : BalrogCardOrder.Alone, army.Id);
            return true;
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
            else
                carriedAlone = region;
        }

        private void StartCarriedAlone()
        {
            if (carriedAlone.Length == 0)
                return;
            if (view?.Decision?.Kind == DecisionKind.CharacterEvent &&
                view.Options.Any(o => o.Enabled && o.Kind == CommandKind.MoveShadowCharacters))
            {
                var region = carriedAlone;
                carriedAlone = "";
                board.Root.schedule.Execute(() => armFigure(region));
            }
            else if (!BalrogCardOrder.Active(view))
                carriedAlone = "";
        }

        private void Respond(string reaction)
        {
            if (draft.Kind != CommandKind.RespondEffect)
                draft.Choose(CommandKind.RespondEffect);
            draft.Change(p => p.ReactionId = reaction);
        }
    }
}
