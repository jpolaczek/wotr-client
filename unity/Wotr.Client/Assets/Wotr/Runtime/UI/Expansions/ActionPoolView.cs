using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionPoolView
    {
        // The own panel's header band in red lacquer names the side (UI-173: without Help links).
        private static VisualElement Header(Side side)
        {
            var header = BoardScreen.Row("dice-header");
            header.name = "dice-header";
            var title = BoardScreen.Title(BoardScreen.SideName(side).ToUpperInvariant());
            title.AddToClassList("dice-side-title");
            header.Add(title);
            // UI-238: while an armed Ring waits for its die, the band asks for it (RingInteraction.ChoosingClass).
            var prompt = new Label("CHOOSE A DIE") { name = "dice-ring-prompt", pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("title");
            prompt.AddToClassList("dice-ring-prompt");
            header.Add(prompt);
            return header;
        }

        private readonly Action<string> dieChosen;
        private readonly Action<string> tokenChosen;
        public const string LordsDieClass = "lords-die";
        public const string SetAsideClass = "set-aside";
        // UI-273: the Kings Ruler die's crown, and a die discarded after the roll dimmed in its slot.
        public const string RulerDieClass = "ruler-die";
        public const string DiscardedClass = "ruler-discarded";
        public const string CouncilTokenClass = "council-token";
        public Action<string> CouncilChosen { get; set; }
        private readonly Dictionary<VisualElement, string> snapshots = new Dictionary<VisualElement, string>();

        public ActionPoolView(Action<string> dieChosen, Action<string> tokenChosen)
        {
            this.dieChosen = dieChosen;
            this.tokenChosen = tokenChosen;
        }

        public void Apply(VisualElement root, PlayerView view, Side side, bool own)
        {
            var snapshot = Snapshot(view, side, own);
            if (snapshots.TryGetValue(root, out var previous) && previous == snapshot && root.childCount > 0)
            {
                RefreshDetails(root, view, own);
                return;
            }
            snapshots[root] = snapshot;
            root.Clear();
            root.EnableInClassList("expanded-resources", view.Configuration.Lords || view.Configuration.Kings);
            // UI-267: a special die not kept this turn keeps its slot, dimmed, until the next roll; UI-273: so does a die
            // discarded after the roll in a Kings game, until the next Recover.
            var visibleDice = view.Dice.Where(d => d.Owner == side && (DiceOutsidePool.InPool(d) || SpecialDice.SetAside(d) ||
                RulerDice.Discarded(view, d) || own && DiceOutsidePool.Offered(view, d))).ToArray();
            root.EnableInClassList("dense-resources", visibleDice.Length > 8);
            // Opponent counters live in the top bar's OpponentSummary.
            root.Add(own ? Header(side) : OwnerLabel(side));
            var available = BoardScreen.Row("available-dice");
            foreach (var die in visibleDice)
                available.Add(Die(die, own, RulerDice.Discarded(view, die)));
            if (!own)
            {
                root.Add(available);
                return;
            }
            OwnResources(root, available, view, side);
            RefreshDetails(root, view, own);
        }

        public static Label OwnerLabel(Side side)
        {
            var label = new Label(BoardScreen.SideName(side).ToUpperInvariant()) { tooltip = "Opponent's action dice" };
            label.AddToClassList("opponent-side");
            return label;
        }

        private void OwnResources(VisualElement root, VisualElement available, PlayerView view, Side side)
        {
            var columns = new VisualElement { name = "own-resource-columns" };
            columns.AddToClassList("own-resource-columns");
            root.Add(columns);
            columns.Add(available);
            var reserves = new VisualElement { name = "own-resource-reserves" };
            reserves.AddToClassList("own-resource-reserves");
            columns.Add(reserves);
            Locations(reserves, view, side);
            Tokens(reserves, view, side);
        }

        // Compare the small public resource projection; unrelated board updates retain controls and focus.
        private static string Snapshot(PlayerView view, Side side, bool own) =>
            side + "/" + own + "/" + view.Configuration.Lords + "/" + view.Configuration.Kings + "/" +
            string.Join("|", view.Dice.Select(d => d.Id + ":" + d.Owner + ":" + ExpansionLabels.DieDetail(d))) + "/" +
            string.Join("|", view.Dice.Where(d => own && DiceOutsidePool.Offered(view, d)).Select(d => d.Id)) + "/" +
            (own ? string.Join("|", view.RulerTokens.Select(t => t.Id + ":" + t.Location + ":" + ExpansionLabels.Token(view, t.Id) + ":" + DraftOptions.CanSelectToken(view, t.Id))) + "/" +
                string.Join("|", view.CouncilTokens.Select(t => t.Id + ":" + t.Spent + ":" + t.UsedTokenThisTurn))
                : "");

        private Button Die(DieView die, bool own, bool discarded)
        {
            var lordsOrigin = SpecialDice.Origin(die);
            var special = ExpansionLabels.Special(die) && lordsOrigin.Length == 0;
            var button = BoardScreen.Die("", () => dieChosen(die.Id));
            button.Add(new DieFaceIcon(die.Face));
            button.name = die.Id;
            button.tooltip = DieDescriptions.Tooltip(die);
            button.AddToClassList(die.Owner == Side.FreePeoples ? "die-free" : "die-shadow");
            button.EnableInClassList("special-die", special);
            button.EnableInClassList("paid-die", die.Location == DieLocation.Resolving);
            button.userData = new DraftToken(DraftObject.Die, die.Id);
            if (lordsOrigin.Length > 0)
                LordsDie(button, die, lordsOrigin);
            else if (special)
            {
                var origin = new Label(ExpansionLabels.DieSource(die)) { pickingMode = PickingMode.Ignore };
                origin.AddToClassList("die-origin");
                button.Add(origin);
            }
            if (die.Location == DieLocation.Resolving || die.RemoveSymbol && lordsOrigin.Length == 0)
            {
                var badge = new Label(die.Location == DieLocation.Resolving ? "•" : "×") { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("die-badge");
                button.Add(badge);
            }
            if (RulerDice.IsRuler(die))
                Crown(button);
            button.EnableInClassList(DiscardedClass, discarded);
            if (discarded)
                button.tooltip = RulerDice.DiscardedTooltip(die);
            // An opponent's retained Ruler die can be the target of Dismay.
            button.SetEnabled(die.Location != DieLocation.Resolving && !SpecialDice.SetAside(die) && !discarded);
            return button;
        }

        // UI-273: the Ruler die wears its crown on its top edge.
        private static void Crown(Button button)
        {
            button.AddToClassList(RulerDieClass);
            var crown = new VisualElement { name = "ruler-crown", pickingMode = PickingMode.Ignore };
            crown.AddToClassList("ruler-crown");
            button.Add(crown);
        }

        // UI-267: a Lords special die wears its origin's art and emblem and, for a Remove result, the star; a die not
        // kept this turn stays dimmed in its slot.
        private static void LordsDie(Button button, DieView die, string origin)
        {
            button.AddToClassList(LordsDieClass);
            button.AddToClassList(LordsDieClass + "-" + origin);
            var emblem = new VisualElement { name = "die-origin-emblem", pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("die-origin-emblem");
            emblem.AddToClassList("die-origin-" + origin);
            button.Add(emblem);
            if (die.RemoveSymbol)
            {
                var star = new VisualElement { name = "die-remove-star", pickingMode = PickingMode.Ignore };
                star.AddToClassList("die-remove-star");
                button.Add(star);
            }
            var setAside = SpecialDice.SetAside(die);
            button.EnableInClassList(SetAsideClass, setAside);
            if (setAside)
                button.tooltip = SpecialDice.SetAsideTooltip(die);
            else if (die.RemoveSymbol)
                button.tooltip += "\n" + SpecialDice.RemoveLine(die, true);
        }

        private void Locations(VisualElement root, PlayerView view, Side side)
        {
            var row = BoardScreen.Row("resource-locations");
            root.Add(row);
            var locations = new List<DieLocation> { DieLocation.Spent, DieLocation.Hunt, DieLocation.Discarded };
            // UI-270: in a Council game the Free Peoples' dice in the Fellowship Box (Strider, Dúnadan) have a fourth count.
            if (side == Side.FreePeoples && view.Configuration.Council)
                locations.Add(DieLocation.FellowshipBox);
            foreach (var location in locations)
            {
                // UI-267: a special die set aside this turn stands in the pool's row, not among the dice out of play.
                var dice = view.Dice.Where(d => location == DieLocation.Spent
                    ? d.Location == location && (d.SpentWith ?? d.Owner) == side
                    : d.Location == location && d.Owner == side && !SpecialDice.SetAside(d) && !RulerDice.Discarded(view, d)).ToList();
                var label = location == DieLocation.Discarded ? "Out" : location == DieLocation.FellowshipBox ? "Fellowship Box"
                    : location.ToString();
                // UI-97: icon and count, as in the top bar; the name leads the description. UI-230: the count is
                // information only; a die there that a decision may choose stands in the pool's row instead.
                var button = BoardScreen.Button(dice.Count.ToString(), null);
                button.focusable = false;
                ReadOnlyControl.Apply(button, false);
                button.name = "pool-" + location;
                button.tooltip = dice.Count == 0 ? label + " · none" : label + "\n" + string.Join("\n", dice.Select(DieDescriptions.Summary));
                button.AddToClassList("pool-count-" + location.ToString().ToLowerInvariant());
                button.Add(new VisualElement { name = "pool-count-icon", pickingMode = PickingMode.Ignore });
                row.Add(button);
            }
        }

        private void Tokens(VisualElement root, PlayerView view, Side side)
        {
            var row = BoardScreen.Row("pool-tokens");
            root.Add(row);
            foreach (var token in view.RulerTokens.Where(t => t.Location == (side == Side.FreePeoples ? "free-peoples" : "dismay")))
            {
                var name = ExpansionLabels.Token(view, token.Id).Split(' ')[0];
                var button = BoardScreen.Button((side == Side.FreePeoples ? "♛ " : "◈ ") + name.Substring(0, Math.Min(3, name.Length)), () => tokenChosen(token.Id));
                button.name = "ruler-token-" + token.Id;
                button.AddToClassList("ruler-token");
                button.tooltip = ResourcePresentation.WithExplanation(view, token.Id,
                    (side == Side.FreePeoples ? "Ruler token · " : "Dismay · ") + ExpansionLabels.Token(view, token.Id));
                button.EnableInClassList("resource-unavailable", !DraftOptions.CanSelectToken(view, token.Id));
                button.userData = new DraftToken(DraftObject.RulerToken, token.Id);
                row.Add(button);
            }
            if (!CouncilTokenSlots.Shown(view, side))
                return;
            // UI-270: the two Council Action Tokens in fixed slots, as medallions; RefreshDetails gives their state.
            foreach (var id in CouncilTokenSlots.Ids)
            {
                var token = id;
                var button = new Button(() => CouncilChosen?.Invoke(token)) { name = "pool-" + token };
                button.AddToClassList(CouncilTokenClass);
                button.AddToClassList(CouncilTokenClass + "-" + CouncilTokenSlots.Key(token));
                row.Add(button);
            }
        }

        // UI-270: a Council token's slot shows whether it was received, is offered, ready, usable now or spent.
        private static void CouncilToken(Button button, PlayerView view)
        {
            var id = button.name.Substring("pool-".Length);
            var state = CouncilTokenSlots.State(view, id);
            button.tooltip = CouncilTokenSlots.Tooltip(view, id);
            button.EnableInClassList("legal-target", state is CouncilTokenState.Offered or CouncilTokenState.Usable);
            button.EnableInClassList("council-spent", state is CouncilTokenState.Spent or CouncilTokenState.Missing);
            button.SetEnabled(state is CouncilTokenState.Offered or CouncilTokenState.Usable or CouncilTokenState.Ready);
        }

        private static void RefreshDetails(VisualElement root, PlayerView view, bool own)
        {
            if (!own)
                return;
            foreach (var button in root.Query<Button>().ToList())
            {
                if (button.userData is DraftToken ruler && ruler.Object == DraftObject.RulerToken)
                {
                    button.tooltip = ResourcePresentation.WithExplanation(view, ruler.Id,
                        (view.You == Side.FreePeoples ? "Ruler token · " : "Dismay · ") + ExpansionLabels.Token(view, ruler.Id));
                    button.EnableInClassList("resource-unavailable", !DraftOptions.CanSelectToken(view, ruler.Id));
                }
                else if (button.ClassListContains(CouncilTokenClass))
                    CouncilToken(button, view);
            }
        }
    }
}
