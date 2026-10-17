using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntDiceStrip
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-roll" };
        public event Action<string> Selected;
        private readonly Dictionary<string, Button> dice = new Dictionary<string, Button>();
        private readonly VisualElement results = BoardScreen.Row("hunt-roll-results");
        private readonly Label bonus = new Label { name = "hunt-roll-bonus" };
        private readonly Label movement = new Label { name = "hunt-movement-bonus" };
        private readonly Label effects = new Label { name = "hunt-effect-bonus" };
        private readonly Label sources = new Label { name = "hunt-reroll-sources" };
        private readonly Label rerollBonus = new Label { name = "hunt-reroll-bonus" };
        private readonly Label budget = new Label { name = "hunt-reroll-budget" };
        private string roster = "";

        public HuntDiceStrip()
        {
            var overview = BoardScreen.Row("hunt-roll-overview");
            overview.Add(results);
            var totals = BoardScreen.Row("hunt-roll-totals");
            totals.Add(bonus);
            totals.Add(budget);
            overview.Add(totals);
            Root.Add(overview);
            var modifiers = BoardScreen.Row("hunt-roll-modifiers");
            modifiers.Add(movement);
            modifiers.Add(effects);
            modifiers.Add(rerollBonus);
            Root.Add(modifiers);
            Root.Add(sources);
            movement.tooltip = "Earlier Free Peoples movement dice only. The current Resolving cost is not included.";
            rerollBonus.tooltip = "Applied only to a legal reroll. A bonus does not grant a reroll or increase its limit.";
        }

        public void Apply(HuntView hunt)
        {
            Root.style.display = hunt != null && !hunt.Mordor && hunt.TileId.Length == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (hunt == null)
                return;
            ReconcileDice(hunt);
            foreach (var die in hunt.Dice)
            {
                var button = dice[die.Id];
                button.Q<HuntDieFace>("hunt-pips").Apply(die.Result);
                button.Q<Label>("hunt-hit").text = die.Success ? "✓" : "×";
                var first = button.Q<Label>("hunt-first-result");
                first.text = die.First + " → " + die.Result;
                first.style.display = die.Rerolled ? DisplayStyle.Flex : DisplayStyle.None;
                button.EnableInClassList("hunt-hit", die.Success);
                button.tooltip = "Natural " + die.Result + (die.Rerolled ? " · First " + die.First + " → Reroll " + die.Result : "") +
                    "\nBonus +" + hunt.Bonus + (die.Rerolled ? " · Reroll bonus +" + hunt.RerollBonus : "") +
                    (die.Success ? " · Hit" : " · Miss") + "\nNatural 1 always misses; 6 or more after bonuses hits.";
            }
            var hits = hunt.Dice.Count(d => d.Success);
            bonus.text = hunt.Dice.Count + (hunt.Dice.Count == 1 ? " die · " : " dice · ") + hits + (hits == 1 ? " hit" : " hits");
            bonus.tooltip = "Hunt roll bonus +" + hunt.Bonus + " · Reroll bonus +" + hunt.RerollBonus +
                "\nRerolls: " + (hunt.RerollSources.Count == 0 ? "None" : string.Join(", ", hunt.RerollSources.Select(Source)));
            movement.text = hunt.MovementBonus.HasValue ? "Earlier FP moves +" + hunt.MovementBonus.Value :
                "Roll bonus +" + hunt.Bonus + " · Breakdown unavailable";
            movement.tooltip = hunt.MovementBonus.HasValue ? "Earlier Free Peoples movement dice only. The current Resolving cost is not included." :
                "This older report contains only the total bonus; movement and effect contributions are unknown.";
            effects.text = "Effects +" + hunt.EffectBonus;
            effects.tooltip = "Public effect bonus applied to the roll and its rerolls.";
            effects.style.display = hunt.EffectBonus.HasValue && hunt.EffectBonus.Value != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            sources.text = hunt.RerollSources.Count == 0 ? "No reroll sources" :
                "Rerolls · " + string.Join(" · ", hunt.RerollSources.Select(Source));
            sources.tooltip = "One reroll per named source, up to the number of failed dice. Natural 1 always misses.";
            rerollBonus.text = "Reroll bonus +" + hunt.RerollBonus;
            rerollBonus.style.display = hunt.RerollBonus != 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ReconcileDice(HuntView hunt)
        {
            var key = string.Join("|", hunt.Dice.Select(d => d.Id));
            if (key == roster)
                return;
            roster = key;
            results.Clear();
            dice.Clear();
            foreach (var die in hunt.Dice)
            {
                var id = die.Id;
                var button = BoardScreen.Button("", () => Selected?.Invoke(id));
                button.name = "hunt-die-" + id;
                button.AddToClassList("hunt-roll-die");
                var face = BoardScreen.Row("hunt-natural-result");
                face.pickingMode = PickingMode.Ignore;
                face.Add(new HuntDieFace());
                face.Add(new Label { name = "hunt-hit", pickingMode = PickingMode.Ignore });
                button.Add(face);
                button.Add(new Label { name = "hunt-first-result", pickingMode = PickingMode.Ignore });
                results.Add(button);
                dice.Add(id, button);
            }
        }

        public void Context(PlayerView view, TurnDraft draft, bool enabled)
        {
            var option = view.DecisionOwner == view.You && draft.Kind == CommandKind.RerollHunt
                ? DraftOptions.For(view, draft) : null;
            budget.style.display = option == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (option != null)
                budget.text = "Rerolls " + draft.Payload.Ids.Count + "/" + option.Maximum;
            foreach (var pair in dice)
            {
                var selected = option != null && draft.Payload.Ids.Contains(pair.Key);
                var legal = enabled && option?.EligibleIds.Contains(pair.Key) == true &&
                    (selected || draft.Payload.Ids.Count < option.Maximum);
                pair.Value.EnableInClassList("legal-target", legal);
                pair.Value.EnableInClassList("selected", selected);
                // Keep public results inspectable for either player; the controller guards choices.
            }
        }

        private static string Source(string source) => source switch
        {
            "stronghold" => "Shadow stronghold", "army" => "Shadow army", "nazgul" => "Nazgûl", _ => source
        };
    }
}
