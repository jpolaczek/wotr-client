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
        // UI-322: the last shown count of each side's spent, Hunt and out dice, so a rebuilt count that changed pulses.
        private readonly Dictionary<(string, Side, DieLocation), int> shownCounts = new Dictionary<(string, Side, DieLocation), int>();
        // UI-322: the faces each pool showed (by match and die), so a die whose face changed in place crossfades to its new
        // face; a die that joins the pool shows its face at once.
        // UI-345: the tokens each pool showed (by match), so a token that joins it arrives; and each Council medallion's
        // last state, so a change pulses.
        private readonly Dictionary<VisualElement, (string Match, HashSet<string> Ids)> shownTokens =
            new Dictionary<VisualElement, (string, HashSet<string>)>();
        private readonly Dictionary<string, CouncilTokenState> councilStates = new Dictionary<string, CouncilTokenState>();
        private readonly ShownBefore<VisualElement, IReadOnlyDictionary<string, DieFace?>> shownFaces =
            new ShownBefore<VisualElement, IReadOnlyDictionary<string, DieFace?>>(new Dictionary<string, DieFace?>());

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
            // UI-274: a Free Peoples Keeper Eye in the Hunt Box that pays a corruption attempt stands in the Shadow's row.
            var visibleDice = view.Dice.Where(d => d.Owner == side && (DiceOutsidePool.InPool(d) || SpecialDice.SetAside(d) ||
                RulerDice.Discarded(view, d) || own && DiceOutsidePool.Offered(view, d)) ||
                own && d.Owner != side && d.Location == DieLocation.Hunt && DiceOutsidePool.Offered(view, d)).ToArray();
            root.EnableInClassList("dense-resources", visibleDice.Length > 8);
            // Opponent counters live in the top bar's OpponentSummary.
            root.Add(own ? Header(side) : OwnerLabel(side));
            var available = BoardScreen.Row("available-dice");
            var faces = new Dictionary<string, DieFace?>();
            var shown = shownFaces.Exchange(root, faces);
            var before = Appearance.Moving(root) && !ValuePulse.Quiet ? shown : null;
            foreach (var die in visibleDice)
            {
                var key = view.MatchId + "/" + die.Id;
                available.Add(Die(die, own, RulerDice.Discarded(view, die), key, before));
                faces[key] = die.Face;
            }
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

        private Button Die(DieView die, bool own, bool discarded, string key, IReadOnlyDictionary<string, DieFace?> before)
        {
            var lordsOrigin = SpecialDice.Origin(die);
            // ART-07: the Ruler die has its own round body and crown (no framed tile, no caption).
            var special = ExpansionLabels.Special(die) && lordsOrigin.Length == 0 && !RulerDice.IsRuler(die);
            var button = BoardScreen.Die("", () => dieChosen(die.Id));
            ShowFace(button, die.Face, key, before);
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

        // A rolled die, a turned one (a Ring, a card) or a die that became unrolled again changes its face where it stands.
        private static void ShowFace(VisualElement button, DieFace? face, string key, IReadOnlyDictionary<string, DieFace?> before)
        {
            var icon = new DieFaceIcon(face);
            button.Add(icon);
            if (before == null || !before.TryGetValue(key, out var old) || old == face)
                return;
            var previous = new DieFaceIcon(old);
            button.Insert(button.IndexOf(icon), previous);
            Appearance.Crossfade(previous, icon);
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
            var before = shownTokens.TryGetValue(root, out var shown) && shown.Match == view.MatchId ? shown.Ids : null;
            var now = new HashSet<string>();
            shownTokens[root] = (view.MatchId, now);
            foreach (var token in view.RulerTokens.Where(t => t.Location == (side == Side.FreePeoples ? "free-peoples" : "dismay")))
            {
                var button = RulerToken(view, token, side);
                row.Add(button);
                now.Add(token.Id);
                if (before != null && !before.Contains(token.Id))
                    Appearance.Arrive(button);
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

        // UI-276 (D19): a Ruler token is a crowned coin with its king's nation emblem: blue while the Free Peoples hold it,
        // lit while a die can use it now; a Dismay token is the red coin the Shadow holds, information only, since a Ruler
        // die's click spends one (D20).
        private Button RulerToken(PlayerView view, RulerTokenView token, Side side)
        {
            var free = side == Side.FreePeoples;
            var button = new Button(() => tokenChosen(token.Id)) { name = "ruler-token-" + token.Id };
            button.AddToClassList("ruler-token");
            button.AddToClassList(free ? "ruler-token-free" : "ruler-token-dismay");
            var emblem = new VisualElement { name = "ruler-token-emblem", pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("ruler-token-emblem");
            var nation = view.Sovereigns.FirstOrDefault(s => s.Id == token.SovereignId)?.Nation ?? "";
            if (nation.Length > 0)
                emblem.AddToClassList("nation-emblem-" + nation);
            button.Add(emblem);
            if (free)
                button.userData = new DraftToken(DraftObject.RulerToken, token.Id);
            else
                ReadOnlyControl.Apply(button, false);
            TokenDetails(button, view, token.Id, free);
            return button;
        }

        private static void TokenDetails(Button button, PlayerView view, string id, bool free)
        {
            var usable = free && DraftOptions.CanSelectToken(view, id);
            button.tooltip = free
                ? ResourcePresentation.WithExplanation(view, id, "Ruler token · " + ExpansionLabels.Token(view, id))
                : "Dismay · " + ExpansionLabels.Token(view, id) + "'s token\nSpent by clicking a glowing Ruler die after the roll";
            button.EnableInClassList("ruler-token-lit", usable);
            button.EnableInClassList("resource-unavailable", free && !usable);
        }

        // UI-270: a Council token's slot shows whether it was received, is offered, ready, usable now or spent.
        private void CouncilToken(Button button, PlayerView view)
        {
            var id = button.name.Substring("pool-".Length);
            var state = CouncilTokenSlots.State(view, id);
            var key = view.MatchId + "/" + id;
            if (councilStates.TryGetValue(key, out var previous) && previous != state)
                ValuePulse.Pulse(button);
            councilStates[key] = state;
            button.tooltip = CouncilTokenSlots.Tooltip(view, id);
            button.EnableInClassList("legal-target", state is CouncilTokenState.Offered or CouncilTokenState.Usable);
            button.EnableInClassList("council-spent", state is CouncilTokenState.Spent or CouncilTokenState.Missing);
            button.SetEnabled(state is CouncilTokenState.Offered or CouncilTokenState.Usable or CouncilTokenState.Ready);
        }

        private void RefreshDetails(VisualElement root, PlayerView view, bool own)
        {
            if (!own)
                return;
            foreach (var button in root.Query<Button>().ToList())
            {
                if (button.ClassListContains("ruler-token"))
                    TokenDetails(button, view, button.name.Substring("ruler-token-".Length), view.You == Side.FreePeoples);
                else if (button.ClassListContains(CouncilTokenClass))
                    CouncilToken(button, view);
            }
        }
    }
}
