using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-115: the two forces of a battle in the enamel frames of their sides: medallions of named characters,
    // one figure per nation and kind with its count, four statistic medallions, the Combat card slot, the dice
    // and a ledge for figures that stay out of the fight. Values come from the server's BattleForceView.
    public sealed class BattleContext
    {
        private readonly List<Action> refresh = new List<Action>();

        public BattleContext(VisualElement root, BattleView battle, PlayerView view, TurnDraft draft,
            Action<Action> edit, Action<string> inspect, CardInspector cards = null)
        {
            var row = BoardScreen.Row("battle-forces");
            row.name = "battle-forces";
            root.Add(row);
            Force(row, battle.Attacker, "attacker", battle, view, draft, edit, inspect, cards);
            Force(row, battle.Defender, "defender", battle, view, draft, edit, inspect, cards);
        }

        private void Force(VisualElement root, BattleForceView force, string role, BattleView battle, PlayerView view, TurnDraft draft,
            Action<Action> edit, Action<string> inspect, CardInspector cards)
        {
            var column = new VisualElement { name = "battle-" + role };
            column.AddToClassList("battle-force");
            column.AddToClassList(force.Side == Side.FreePeoples ? "fp" : "sh");
            root.Add(column);
            var modifiers = BattleForceText.Modifiers(force, battle, view);
            var heading = new Label((role == "attacker" ? "ATTACKER" : "DEFENDER") + " · " + BoardScreen.SideName(force.Side).ToUpperInvariant() +
                (force.Garrison ? " · GARRISON" : "")) { name = role + "-strength", enableRichText = false };
            heading.AddToClassList("battle-force-heading");
            heading.tooltip = modifiers;
            column.Add(heading);
            if (force.Characters.Count > 0)
                Characters(column, force, view);
            column.Add(Composition(force));
            column.Add(Statistics(force, modifiers));
            var outside = draft == null ? "" : BattleForceText.Nonparticipants(force, battle, view);
            if (outside.Length > 0)
                column.Add(Shelf(outside));
            Card(column, force, role, heading, inspect, force.Side != Side.FreePeoples ? "" :
                battle.DenethorBlocksCards ? "Denethor · No card" :
                battle.CompanionRequiredForCard && force.Characters.Count == 0 ? "No Companion · No card this round" : "", cards);
            var dice = new BattleDiceStrip(column, force, battle, view, draft, edit);
            refresh.Add(dice.Refresh);
        }

        // Medallions first, then the names on one small line; the historical version of a character stays.
        private static void Characters(VisualElement column, BattleForceView force, PlayerView view)
        {
            var portraits = BoardScreen.Row("battle-portraits");
            portraits.name = "battle-portraits";
            foreach (var character in force.Characters)
                portraits.Add(new PortraitMedallion(character, CharacterLibrary.Name(character.DefinitionId),
                    CharacterLibrary.ShortName(view, character.Id), force.CancelledAbilities.Contains(character.Id)));
            column.Add(portraits);
            AddLine(column, string.Join(", ", force.Characters.Select(c =>
                (force.CancelledAbilities.Contains(c.Id) ? "⦸ " : "") + CharacterLibrary.ShortName(view, c.Id))),
                "battle-characters", string.Join("\n", force.Characters.Select(c => CharacterLibrary.Name(c.DefinitionId))));
        }

        private static VisualElement Composition(BattleForceView force)
        {
            var row = BoardScreen.Row("battle-composition");
            row.name = "battle-composition";
            foreach (var nation in force.Units.GroupBy(u => u.Nation))
            {
                var group = new VisualElement { name = "battle-nation-" + nation.Key };
                group.AddToClassList("battle-nation-group");
                group.tooltip = FigureGrid.Name(nation.Key) + "\n" + string.Join("\n", nation.GroupBy(u => u.Kind).Select(g => g.Key + " × " + g.Count()));
                var emblem = new VisualElement { name = "battle-nation-emblem", pickingMode = PickingMode.Ignore };
                emblem.AddToClassList("battle-nation-emblem");
                emblem.AddToClassList("nation-emblem-" + nation.Key);
                group.Add(emblem);
                foreach (var kind in nation.GroupBy(u => u.Kind))
                    group.Add(Figure(kind.First(), kind.Count()));
                row.Add(group);
            }
            return row;
        }

        private static VisualElement Figure(UnitView unit, int count)
        {
            var figure = new VisualElement { name = "battle-figure-" + unit.Nation + "-" + unit.Kind, pickingMode = PickingMode.Ignore };
            figure.AddToClassList("battle-figure");
            // UI-207: a soft gilt glow behind the figure, as in the panels (UI-201), so dark figures read on the frame.
            figure.Add(IconGlow.Create("battle-figure-glow"));
            figure.Add(FigureArtwork.Image(FigureAppearances.Art(unit)));
            var number = new Label("×" + count) { name = "battle-figure-count", pickingMode = PickingMode.Ignore, enableRichText = false };
            number.AddToClassList("battle-figure-count");
            figure.Add(number);
            return figure;
        }

        private static VisualElement Statistics(BattleForceView force, string modifiers)
        {
            var row = BoardScreen.Row("battle-statistics");
            row.name = "battle-statistics";
            row.tooltip = modifiers;
            row.Add(new BattleStatMedallion("strength", force.Strength.ToString(), "Strength " + force.Strength + "\n" + modifiers));
            row.Add(new BattleStatMedallion("leadership", force.Leadership.ToString(), "Leadership " + force.Leadership + "\n" + modifiers));
            row.Add(new BattleStatMedallion("dice", force.CombatDice.ToString(), "Combat dice " + force.CombatDice + "\n" + modifiers));
            row.Add(new BattleStatMedallion("hit", force.HitTarget + "+", "Hits on " + force.HitTarget + "+\n" + modifiers));
            return row;
        }

        private static VisualElement Shelf(string text)
        {
            var shelf = BoardScreen.Row("battle-garrison-shelf");
            shelf.name = "battle-garrison-shelf";
            var icon = new VisualElement { name = "battle-garrison-icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("battle-garrison-icon");
            shelf.Add(icon);
            var label = new Label(text) { name = "battle-nonparticipants", tooltip = text, enableRichText = false };
            label.AddToClassList("battle-nonparticipants");
            shelf.Add(label);
            return shelf;
        }

        private static void AddLine(VisualElement root, string text, string name, string tooltip)
        {
            var label = new Label(text) { name = name, tooltip = tooltip, enableRichText = false };
            label.AddToClassList(name);
            root.Add(label);
        }

        private static void Card(VisualElement root, BattleForceView force, string role, Label heading, Action<string> inspect, string prohibition, CardInspector cards)
        {
            var visible = !string.IsNullOrEmpty(force.CardId);
            CardText card = null;
            var revealed = visible && CardLibrary.Cards.TryGetValue(force.CardId, out card);
            var faceDown = !visible && force.CardCommitted && force.CardBack.Length > 0 && prohibition.Length == 0;
            var label = revealed ? card.CombatTitle + " · Initiative " + string.Join("/", card.Initiative) :
                prohibition.Length > 0 ? prohibition : !force.CardCommitted ? "Choosing card…" : faceDown ? FigureGrid.Name(force.CardBack) + " · Face down" : "No card";
            var button = BoardScreen.Button(label + (force.CardCancelled ? " · Cancelled" : ""), () =>
            {
                if (!visible) return;
                if (cards != null) cards.ShowCombat(force.CardId, true, allowPlay: false);
                else inspect?.Invoke(force.CardId);
            });
            button.name = "battle-card-" + role;
            button.AddToClassList("battle-card");
            // UI-116: a committed secret shows the deck's leather back; a revealed card lies on parchment.
            button.EnableInClassList("battle-card-back", faceDown);
            button.EnableInClassList("battle-card-revealed", revealed);
            button.SetEnabled(visible);
            button.tooltip = heading.tooltip + "\n" + (visible ? "Inspect " + label : label);
            root.Add(button);
            if (visible && cards != null)
                button.AddToClassList(RuntimeTooltip.RichHoverClass);
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
