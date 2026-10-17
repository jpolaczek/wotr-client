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

        public BattleDiceStrip(VisualElement root, BattleForceView force, BattleView battle, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var pages = Pages(force, battle);
            if (pages.Count == 0)
                return;
            var selected = Math.Max(0, pages.FindLastIndex(p => p.Source == battle.LossSource && battle.LossSource != "combat"));
            var row = new VisualElement();
            root.Add(row);
            var heading = BoardScreen.Row("battle-roll-heading");
            var source = new Label { name = "battle-roll-source" };
            heading.Add(source);
            row.Add(heading);
            var content = BoardScreen.Row("battle-dice");
            var switcher = IconButton.Create("next", "Show next roll", null);
            switcher.name = "battle-roll-page-" + force.Side;
            switcher.AddToClassList("battle-roll-page");
            HitCancels(heading, force, view, draft, edit);
            if (pages.Count > 1)
                heading.Add(switcher);
            row.Add(content);
            void Show()
            {
                refresh.Clear();
                content.Clear();
                var page = pages[selected];
                switcher.tooltip = page.Title + " · Show next roll (" + (selected + 1) + "/" + pages.Count + ")";
                source.text = page.Title + " · Hits " + page.Hits;
                source.tooltip = page.Title + "\n" + BattleForceText.Modifiers(force, battle, view);
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
                button.SetEnabled(selectable && (chosen || draft.Payload.Ids.Count < option.Maximum) ||
                    target && (targetChosen || BattleObjectChoices.CanChoose(view, draft, die.Id)));
                button.EnableInClassList("selected", chosen || targetChosen || force.OwnRerollSelection.Contains(die.Id));
                ChoiceGlow.Apply(button, target && !targetChosen);
                button.tooltip = target ? BattleObjectChoices.Detail(view, die.Id) + "\n" + tooltip : tooltip;
            });
        }

        // UI-246: Heroic Death cancels the opponent's hits on their own indicator: one button per amount it may cancel
        // (−1, −2, …); the chosen amount is selected. Keep hits (zero) stays a button with Keep all.
        private void HitCancels(VisualElement heading, BattleForceView force, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            if (draft == null || edit == null || force.Side == view.You)
                return;
            var amounts = BattleObjectChoices.HitAmounts(view);
            if (amounts.Count == 0)
                return;
            var row = BoardScreen.Row("battle-hit-cancels");
            row.name = "battle-hit-cancels";
            foreach (var (id, amount) in amounts)
            {
                var pip = BattleChoicePip.Create("battle-hit-cancel-" + amount, "−" + amount, () => edit(() => BattleObjectChoices.Choose(view, draft, id)));
                row.Add(pip);
                headingRefresh.Add(() => BattleChoicePip.Show(pip, BattleObjectChoices.CanChoose(view, draft, id),
                    BattleObjectChoices.ChosenHits(view, draft) == amount, BattleObjectChoices.Detail(view, id)));
            }
            heading.Add(row);
        }

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
