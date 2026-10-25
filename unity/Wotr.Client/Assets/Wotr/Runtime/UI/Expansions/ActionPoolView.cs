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
        public Action<string> CouncilChosen { get; set; }
        private readonly Dictionary<VisualElement, string> snapshots = new Dictionary<VisualElement, string>();
        // UI-322: the last shown count of each side's spent, Hunt and out dice, so a rebuilt count that changed pulses.
        private readonly Dictionary<(string, Side, DieLocation), int> shownCounts = new Dictionary<(string, Side, DieLocation), int>();

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
            var visibleDice = view.Dice.Where(d => d.Owner == side && (DiceOutsidePool.InPool(d) || own && DiceOutsidePool.Offered(view, d))).ToArray();
            root.EnableInClassList("dense-resources", visibleDice.Length > 8);
            // Opponent counters live in the top bar's OpponentSummary.
            root.Add(own ? Header(side) : OwnerLabel(side));
            var available = BoardScreen.Row("available-dice");
            foreach (var die in visibleDice)
                available.Add(Die(die, own));
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

        private Button Die(DieView die, bool own)
        {
            var special = ExpansionLabels.Special(die);
            var button = BoardScreen.Die("", () => dieChosen(die.Id));
            button.Add(new DieFaceIcon(die.Face));
            button.name = die.Id;
            button.tooltip = DieDescriptions.Tooltip(die);
            button.AddToClassList(die.Owner == Side.FreePeoples ? "die-free" : "die-shadow");
            button.EnableInClassList("special-die", special);
            button.EnableInClassList("paid-die", die.Location == DieLocation.Resolving);
            if (special)
            {
                var origin = new Label(ExpansionLabels.DieSource(die)) { pickingMode = PickingMode.Ignore };
                origin.AddToClassList("die-origin");
                button.Add(origin);
            }
            if (die.Location == DieLocation.Resolving || die.RemoveSymbol)
            {
                var badge = new Label(die.Location == DieLocation.Resolving ? "•" : "×") { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("die-badge");
                button.Add(badge);
            }
            button.userData = new DraftToken(DraftObject.Die, die.Id);
            // An opponent's retained Ruler die can be the target of Dismay.
            button.SetEnabled(die.Location != DieLocation.Resolving);
            return button;
        }

        private void Locations(VisualElement root, PlayerView view, Side side)
        {
            var row = BoardScreen.Row("resource-locations");
            root.Add(row);
            foreach (var location in new[] { DieLocation.Spent, DieLocation.Hunt, DieLocation.Discarded })
            {
                var dice = view.Dice.Where(d => location == DieLocation.Spent
                    ? d.Location == location && (d.SpentWith ?? d.Owner) == side : d.Location == location && d.Owner == side).ToList();
                var label = location == DieLocation.Discarded ? "Out" : location.ToString();
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
                var key = (view.MatchId, side, location);
                if (shownCounts.TryGetValue(key, out var shown) && shown != dice.Count)
                    ValuePulse.Pulse(button);
                shownCounts[key] = dice.Count;
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
            if (side == Side.Shadow)
                foreach (var token in view.CouncilTokens)
                {
                    var button = BoardScreen.Button((token.Spent ? "×" : "") +
                        (token.Id == ExpansionDraft.CouncilPolitics ? "C·P" : "C·T"), () => CouncilChosen?.Invoke(token.Id));
                    button.name = "pool-" + token.Id;
                    button.tooltip = ResourcePresentation.WithExplanation(view, token.Id, ExpansionLabels.CouncilToken(token.Id));
                    button.EnableInClassList("resource-unavailable", token.Spent || token.UsedTokenThisTurn);
                    row.Add(button);
                }
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
                else if (button.name.StartsWith("pool-council."))
                {
                    var id = button.name.Substring("pool-".Length);
                    button.tooltip = ResourcePresentation.WithExplanation(view, id, ExpansionLabels.CouncilToken(id));
                    var ready = ResourcePresentation.CanSelectCouncil(view, id);
                    button.EnableInClassList("legal-target", ready);
                }
            }
        }
    }
}
