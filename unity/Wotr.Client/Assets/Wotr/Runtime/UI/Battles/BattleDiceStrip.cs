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
                    AddDie(content, die, page.Title, force, battle, view.You, draft, option, edit);
                Refresh();
            }
            switcher.clicked += () => { selected = (selected + 1) % pages.Count; Show(); };
            Show();
        }

        private void AddDie(VisualElement root, BattleDieView die, string source, BattleForceView force, BattleView battle,
            Side viewer, TurnDraft draft, ActionOption option, Action<Action> edit)
        {
            var button = new BattleDieButton(die, source, battle, () => edit?.Invoke(() => draft.Toggle(die.Id)));
            if (draft == null) button.userData = null;
            root.Add(button);
            refresh.Add(() =>
            {
                var selectable = draft?.Kind == CommandKind.RerollBattle && force.Side == viewer && option?.Enabled == true && option.EligibleIds.Contains(die.Id);
                var chosen = selectable && draft.Payload.Ids.Contains(die.Id);
                button.SetEnabled(selectable && (chosen || draft.Payload.Ids.Count < option.Maximum));
                button.EnableInClassList("selected", chosen || force.OwnRerollSelection.Contains(die.Id));
            });
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
