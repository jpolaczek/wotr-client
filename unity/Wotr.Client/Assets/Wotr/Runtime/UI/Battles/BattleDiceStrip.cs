using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleDiceStrip
    {
        private readonly List<Action> refresh = new List<Action>();
        // The heading's controls outlive a page switch, which rebuilds only the dice.
        private readonly List<Action> headingRefresh = new List<Action>();

        // UI-437: rows (both columns' plan) keeps this roll level with the other side's: an empty roll's room while only the
        // other side rolled, and an unseen copy of the pager or hit buttons only the other side's heading shows.
        public BattleDiceStrip(VisualElement root, BattleForceView force, BattleView battle, PlayerView view, TurnDraft draft, Action<Action> edit,
            BattleRowPlan rows = null)
        {
            var pages = Pages(force, battle);
            if (pages.Count == 0)
            {
                if (rows?.Roll == true)
                    root.Add(Reserved(rows));
                return;
            }
            var selected = Math.Max(0, pages.FindLastIndex(p => p.Source == battle.LossSource && battle.LossSource != "combat"));
            var row = new VisualElement();
            root.Add(row);
            var heading = BoardScreen.Row("battle-roll-heading");
            var source = new Label { name = "battle-roll-source" };
            heading.Add(source);
            row.Add(heading);
            var content = BoardScreen.Row("battle-dice");
            var switcher = Pager("battle-roll-page-" + force.Side);
            var cancels = HitCancels(heading, force, view, draft, edit);
            if (!cancels && rows?.CancelsHits == true)
                heading.Add(ReservedHitCancels());
            if (pages.Count > 1)
                heading.Add(switcher);
            else if (rows?.Paged == true)
                heading.Add(Reserve(Pager("battle-roll-page-reserved")));
            row.Add(content);
            void Show()
            {
                refresh.Clear();
                content.Clear();
                var page = pages[selected];
                switcher.tooltip = page.Title + " · Show next roll (" + (selected + 1) + "/" + pages.Count + ")";
                source.text = page.Title + " · Hits " + page.Hits;
                source.tooltip = RollTooltip(page, force);
                var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.RerollBattle);
                foreach (var die in page.Dice)
                    AddDie(content, die, page.Title, force, battle, view, draft, option, edit);
                Refresh();
            }
            switcher.clicked += () => { selected = (selected + 1) % pages.Count; Show(); };
            Show();
        }

        private void AddDie(VisualElement root, BattleDieView die, string source, BattleForceView force, BattleView battle,
            PlayerView view, TurnDraft draft, ActionOption option, Action<Action> edit)
        {
            var viewer = view.You;
            var tooltip = BattleRollText.Describe(die, source, battle);
            // UI-245: a combat card's choice of this die is made on the die itself.
            var button = new BattleDieButton(die, source, battle, () => edit?.Invoke(() =>
            {
                if (BattleObjectChoices.Offered(view, die.Id))
                    BattleObjectChoices.Choose(view, draft, die.Id);
                else
                    draft.Toggle(die.Id);
            }));
            if (draft == null) button.userData = null;
            root.Add(button);
            refresh.Add(() =>
            {
                var target = draft != null && BattleObjectChoices.Offered(view, die.Id);
                var targetChosen = target && draft.Kind == CommandKind.ChooseEffect && draft.Payload.Ids.Contains(die.Id);
                var selectable = draft?.Kind == CommandKind.RerollBattle && force.Side == viewer && option?.Enabled == true && option.EligibleIds.Contains(die.Id);
                var chosen = selectable && draft.Payload.Ids.Contains(die.Id);
                var open = selectable && !chosen && draft.Payload.Ids.Count < option.Maximum;
                button.SetEnabled(open || chosen ||
                    target && (targetChosen || BattleObjectChoices.CanChoose(view, draft, die.Id)));
                button.EnableInClassList("selected", chosen || targetChosen || force.OwnRerollSelection.Contains(die.Id));
                // UI-423 (owner's playtest pattern, like UI-419): a failed die a click may still add to the rerolls glows
                // until the budget is full, as a combat card's target die does; BattleContext pulses every glowing die.
                ChoiceGlow.Apply(button, target && !targetChosen || open);
                button.tooltip = target ? TooltipText.Lines(BattleObjectChoices.Detail(view, die.Id), tooltip) : tooltip;
            });
        }

        // UI-420 (owner): the roll's heading explains only what changed the Combat roll's hits (non-zero modifiers, hits
        // spent on a card); without such a fact it has no tooltip, as its text says the rest.
        private static string RollTooltip(RollPage page, BattleForceView force)
        {
            var details = page.Source.Length == 0 ? BattleForceText.Roll(force) : "";
            return details.Length == 0 ? "" : TooltipText.Lines(page.Title, details);
        }

        // UI-246: Heroic Death cancels the opponent's hits on their own indicator: one button per amount it may cancel
        // (−1, −2, …); the chosen amount is selected. Keep hits (zero) stays a button with Keep all. UI-347 (D10): Orderly
        // Withdrawal's one hit is the same "−1"; its click sends it, and the console's Pass keeps the hit. Returns whether the
        // heading shows them.
        private bool HitCancels(VisualElement heading, BattleForceView force, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            if (draft == null || edit == null || force.Side == view.You)
                return false;
            var amounts = BattleObjectChoices.HitAmounts(view);
            if (amounts.Count == 0)
                return false;
            var row = BoardScreen.Row("battle-hit-cancels");
            row.name = "battle-hit-cancels";
            // UI-419 (owner): a pip a click may choose pulses, as the board's choices do.
            var pulse = new ChoicePulse(row);
            foreach (var (id, amount) in amounts)
            {
                var pip = BattleChoicePip.Create("battle-hit-cancel-" + amount, "−" + amount, () => edit(() => BattleObjectChoices.Choose(view, draft, id)));
                row.Add(pip);
                headingRefresh.Add(() => BattleChoicePip.Show(pip, BattleObjectChoices.CanChoose(view, draft, id),
                    BattleObjectChoices.ChosenHits(view, draft) == amount, BattleObjectChoices.Detail(view, id)));
            }
            headingRefresh.Add(() => pulse.Run(amounts.Any(choice => BattleObjectChoices.CanChoose(view, draft, choice.Item1) &&
                BattleObjectChoices.ChosenHits(view, draft) != choice.Item2)));
            heading.Add(row);
            return true;
        }

        private static Button Pager(string name)
        {
            var pager = IconButton.Create("next", "Show next roll", null);
            pager.name = name;
            pager.AddToClassList("battle-roll-page");
            return pager;
        }

        // UI-437: an unseen heading and row of one die, so the other side's roll stands level with nothing moved.
        private static VisualElement Reserved(BattleRowPlan rows)
        {
            var slot = new VisualElement { name = "battle-roll-reserved", pickingMode = PickingMode.Ignore };
            slot.AddToClassList(BattleContext.ReservedClass);
            var heading = BoardScreen.Row("battle-roll-heading");
            heading.Add(new Label(" ") { name = "battle-roll-source-reserved", pickingMode = PickingMode.Ignore });
            if (rows.CancelsHits)
                heading.Add(ReservedHitCancels());
            if (rows.Paged)
                heading.Add(Reserve(Pager("battle-roll-page-reserved")));
            slot.Add(heading);
            var dice = BoardScreen.Row("battle-dice");
            var die = Reserve(new Button { name = "battle-die-reserved" });
            die.AddToClassList("battle-die");
            dice.Add(die);
            slot.Add(dice);
            return slot;
        }

        private static VisualElement ReservedHitCancels()
        {
            var row = BoardScreen.Row("battle-hit-cancels");
            row.name = "battle-hit-cancels-reserved";
            row.Add(Reserve(BattleChoicePip.Create("battle-hit-cancel-reserved", "−1", null)));
            return row;
        }

        // An inert, unseen copy of a control only the other side's roll shows: it takes the control's room in every layout.
        private static T Reserve<T>(T control) where T : VisualElement
        {
            control.pickingMode = PickingMode.Ignore;
            control.focusable = false;
            control.SetEnabled(false);
            control.AddToClassList(BattleContext.ReservedClass);
            return control;
        }

        // Whether this side shows a roll (its Combat dice or a special attack's) and whether it has more than one page.
        public static bool Rolled(BattleForceView force, BattleView battle) => Pages(force, battle).Count > 0;
        public static bool Paged(BattleForceView force, BattleView battle) => Pages(force, battle).Count > 1;

        private static List<RollPage> Pages(BattleForceView force, BattleView battle)
        {
            var pages = new List<RollPage>();
            if (force.Dice.Count > 0)
                pages.Add(new RollPage("Combat", "", force.Dice, force.Hits));
            foreach (var attack in battle.SpecialAttacks.Where(a => a.Side == force.Side))
            {
                var hits = attack.Dice.Count(d => d.Hit);
                for (var offset = 0; offset < attack.Dice.Count; offset += 5)
                    pages.Add(new RollPage("Special · " + SourceName(attack.SourceId), attack.SourceId, attack.Dice.Skip(offset).Take(5).ToList(), hits));
            }
            return pages;
        }

        private static string SourceName(string id) => CardLibrary.Cards.TryGetValue(id, out var card) && card.CombatTitle.Length > 0
            ? card.CombatTitle : CardLibrary.Title(id);

        public void Refresh()
        {
            foreach (var update in headingRefresh)
                update();
            foreach (var update in refresh)
                update();
        }

        private sealed class RollPage
        {
            public readonly string Title;
            public readonly string Source;
            public readonly List<BattleDieView> Dice;
            public readonly int Hits;

            public RollPage(string title, string source, List<BattleDieView> dice, int hits)
            {
                Title = title;
                Source = source;
                Dice = dice;
                Hits = hits;
            }
        }
    }
}
