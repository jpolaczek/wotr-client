using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleDieButton : Button
    {
        public BattleDieButton(BattleDieView die, string source, BattleView battle, Action choose) : base(choose)
        {
            name = "battle-die-" + die.Id;
            userData = new DraftToken(DraftObject.Die, die.Id, action: CommandKind.RerollBattle);
            AddToClassList("action");
            AddToClassList("battle-die");
            EnableInClassList("battle-hit", die.Hit || die.RerollHit || die.ConvertedHit);
            tooltip = BattleRollText.Describe(die, source, battle);
            var results = BoardScreen.Row("battle-result-row");
            results.pickingMode = PickingMode.Ignore;
            // UI-117: the first roll and its re-roll are two ivory dice; the one that counts carries the hit glow.
            var first = Face(die.Result, "battle-first-roll");
            first.EnableInClassList("battle-pips-rerolled", die.Reroll.HasValue);
            first.EnableInClassList("battle-pips-hit", !die.Reroll.HasValue && (die.Hit || die.ConvertedHit));
            results.Add(first);
            if (die.Reroll.HasValue)
            {
                var arrow = new VisualElement { name = "battle-reroll-arrow", pickingMode = PickingMode.Ignore };
                arrow.AddToClassList("battle-reroll-arrow");
                results.Add(arrow);
                var reroll = Face(die.Reroll.Value, "battle-reroll-result");
                reroll.EnableInClassList("battle-pips-hit", die.RerollHit || die.ConvertedHit);
                results.Add(reroll);
            }
            var outcome = new Label(die.ConvertedHit ? "↗✓" : die.Reroll.HasValue
                ? die.RerollHit ? "✓" : "×" : die.Hit ? "✓" : "×")
            {
                name = "battle-die-outcome", pickingMode = PickingMode.Ignore
            };
            results.Add(outcome);
            Add(results);
            var modifier = new Label(BattleRollText.Modifier(die)) { name = "battle-die-modifier", pickingMode = PickingMode.Ignore };
            modifier.AddToClassList("battle-die-modifier");
            Add(modifier);
        }

        private static DiePips Face(int value, string name)
        {
            var pips = new DiePips { name = name, Illustrated = true };
            pips.AddToClassList("battle-pips");
            pips.Apply(value);
            return pips;
        }
    }
}
