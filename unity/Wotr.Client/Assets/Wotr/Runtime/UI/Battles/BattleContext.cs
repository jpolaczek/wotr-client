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
        // UI-247: sends a complete choice at once (a character's reaction on its portrait).
        private readonly Action<Action> complete;
        // UI-322: the live window's last stacks; null where nothing is compared (a report, a presented state).
        private readonly BattleStackMemory stacks;
        private readonly bool moving;

        public BattleContext(VisualElement root, BattleView battle, PlayerView view, TurnDraft draft,
            Action<Action> edit, Action<string> inspect, CardInspector cards = null, Action<Action> complete = null,
            BattleStackMemory stacks = null)
        {
            this.complete = complete;
            this.stacks = stacks;
            moving = Appearance.Moving(root) && !ValuePulse.Quiet;
            var row = BoardScreen.Row("battle-forces");
            row.name = "battle-forces";
            root.Add(row);
            Force(row, battle.Attacker, "attacker", battle, view, draft, edit, inspect, cards);
            Force(row, battle.Defender, "defender", battle, view, draft, edit, inspect, cards);
            // UI-245: the dice and figures a combat card may choose pulse together.
            var pulse = new ChoicePulse(row);
            refresh.Add(() => pulse.Run(row.Query(className: ChoiceGlow.ClassName).ToList().Count > 0));
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
                Characters(column, force, view, draft, edit);
            column.Add(Composition(force, battle, view, draft, edit));
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
        private void Characters(VisualElement column, BattleForceView force, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var portraits = BoardScreen.Row("battle-portraits");
            portraits.name = "battle-portraits";
            foreach (var character in force.Characters)
            {
                var portrait = new PortraitMedallion(character, CharacterLibrary.Name(character.DefinitionId),
                    CharacterLibrary.ShortName(view, character.Id), force.CancelledAbilities.Contains(character.Id));
                // One click handler per portrait; a reaction or Leadership points refresh after the choice target.
                Action other = null;
                ChoiceTarget(portrait, view, draft, edit, () => character.Id, otherwise: () => other?.Invoke());
                other = ReactionSource(portrait, character, view, draft) ?? Points(portrait, view, draft, edit, new[] { character.Id });
                portraits.Add(portrait);
            }
            column.Add(portraits);
            AddLine(column, string.Join(", ", force.Characters.Select(c =>
                (force.CancelledAbilities.Contains(c.Id) ? "⦸ " : "") + CharacterLibrary.ShortName(view, c.Id))),
                "battle-characters", string.Join("\n", force.Characters.Select(c => CharacterLibrary.Name(c.DefinitionId))));
        }

        private VisualElement Composition(BattleForceView force, BattleView battle, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var row = BoardScreen.Row("battle-composition");
            row.name = "battle-composition";
            var shown = new List<BattleStack>();
            foreach (var nation in force.Units.GroupBy(u => u.Nation))
            {
                var group = new VisualElement { name = "battle-nation-" + nation.Key };
                group.AddToClassList("battle-nation-group");
                group.tooltip = FigureGrid.Name(nation.Key) + "\n" + string.Join("\n", nation.GroupBy(u => u.Kind).Select(g => g.Key + " × " + g.Count()));
                group.Add(Emblem(nation.Key));
                foreach (var kind in nation.GroupBy(u => u.Kind))
                {
                    var figure = Figure(kind.First(), kind.Count());
                    var stack = kind.ToList();
                    Action points = null;
                    ChoiceTarget(figure, view, draft, edit, () => BattleObjectChoices.NextInStack(view, draft, stack),
                        () => stack.Any(u => draft.Payload.Ids.Contains(u.Id)), () => points?.Invoke());
                    points = Points(figure, view, draft, edit, stack.Select(u => u.Id).ToList());
                    shown.Add(new BattleStack(stack[0], stack.Count, row.childCount, group.childCount));
                    group.Add(figure);
                }
                row.Add(group);
            }
            if (stacks != null)
                Changes(row, stacks.Exchange(battle.Id, force.Side, shown), shown);
            return row;
        }

        // UI-322: a stack whose count changed since the window last showed it pulses; one that left fades out where it
        // stood (a renamed copy, so no query finds it) and its neighbours close up after it. Under Skip animations and in
        // a quiet view the window simply shows the new stacks.
        private void Changes(VisualElement row, IReadOnlyList<BattleStack> before, IReadOnlyList<BattleStack> now)
        {
            if (!moving)
                return;
            foreach (var old in before)
            {
                var current = now.FirstOrDefault(stack => stack.Same(old));
                if (current == null)
                    Ghost(row, old);
                else if (current.Count != old.Count)
                    ValuePulse.Pulse(row.Q("battle-figure-" + old.Unit.Nation + "-" + old.Unit.Kind).Q("battle-figure-count"));
            }
        }

        private static void Ghost(VisualElement row, BattleStack stack)
        {
            var ghost = Figure(stack.Unit, stack.Count);
            ghost.name = "leaving-" + ghost.name;
            var group = row.Q("battle-nation-" + stack.Unit.Nation);
            if (group != null)
            {
                group.Insert(Math.Min(stack.Place, group.childCount), ghost);
                Appearance.Depart(ghost);
                return;
            }
            group = new VisualElement { name = "leaving-battle-nation-" + stack.Unit.Nation, pickingMode = PickingMode.Ignore };
            group.AddToClassList("battle-nation-group");
            var emblem = Emblem(stack.Unit.Nation);
            emblem.name = "leaving-" + emblem.name;
            group.Add(emblem);
            group.Add(ghost);
            row.Insert(Math.Min(stack.Group, row.childCount), group);
            Appearance.Depart(group);
        }

        private static VisualElement Emblem(string nation)
        {
            var emblem = new VisualElement { name = "battle-nation-emblem", pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("battle-nation-emblem");
            emblem.AddToClassList("nation-emblem-" + nation);
            return emblem;
        }

        // UI-245: a portrait or figure stack the current combat card may choose glows and chooses on a click; a stack's
        // figures of one nation and kind are equivalent, so its next eligible figure is taken.
        private void ChoiceTarget(VisualElement element, PlayerView view, TurnDraft draft, Action<Action> edit, Func<string> next,
            Func<bool> chosen = null, Action otherwise = null)
        {
            if (draft == null || edit == null)
                return;
            ObjectPress.Register(element, () =>
            {
                var id = next();
                if (id.Length > 0 && BattleObjectChoices.Offered(view, id))
                    edit(() => BattleObjectChoices.Choose(view, draft, id));
                else
                    otherwise?.Invoke();
            });
            var tooltip = element.tooltip;
            refresh.Add(() =>
            {
                var id = next();
                var offered = id.Length > 0 && BattleObjectChoices.CanChoose(view, draft, id);
                var selected = draft.Kind == CommandKind.ChooseEffect && (chosen?.Invoke() ?? draft.Payload.Ids.Contains(id));
                element.pickingMode = offered || selected ? PickingMode.Position : element.pickingMode;
                ChoiceGlow.Apply(element, offered && !selected);
                element.EnableInClassList("selected", selected);
                element.tooltip = offered ? BattleObjectChoices.Detail(view, id) + "\n" + tooltip : tooltip;
            });
        }

        // UI-247: a character's reaction (White Rider, the Witch-king's draw) is used on its portrait with one click;
        // returns that click, or null when the character has no reaction now.
        private Action ReactionSource(VisualElement portrait, CharacterView character, PlayerView view, TurnDraft draft)
        {
            var reaction = draft == null || complete == null ? null : BattleReactions.Of(view, character);
            if (reaction == null)
                return null;
            portrait.pickingMode = PickingMode.Position;
            var tooltip = BattleReactions.Offer(reaction) + "\n" + portrait.tooltip;
            refresh.Add(() =>
            {
                ChoiceGlow.Apply(portrait, true);
                portrait.tooltip = tooltip;
            });
            return () => complete(() => BattleReactions.Choose(draft, reaction.Id));
        }

        // UI-246: the Leadership points a Nazgûl card may forfeit belong to the figure or portrait that has them, so the
        // Witch-king's points stay his own and the player sees which figures pay. A click takes the source's next point
        // while one can be added, and gives its points back once none can; a badge counts them. Returns that click.
        private Action Points(VisualElement element, PlayerView view, TurnDraft draft, Action<Action> edit, ICollection<string> figures)
        {
            if (draft == null || edit == null)
                return null;
            var points = BattleObjectChoices.PointsOf(view, figures);
            if (points.Count == 0)
                return null;
            var badge = new Label { name = "battle-leadership-points", pickingMode = PickingMode.Ignore, enableRichText = false };
            badge.AddToClassList("battle-leadership-points");
            element.Add(badge);
            var tooltip = element.tooltip;
            refresh.Add(() =>
            {
                var chosen = BattleObjectChoices.ChosenPoints(draft, points);
                var more = BattleObjectChoices.NextPoint(view, draft, points).Length > 0;
                badge.text = "◆ " + chosen + "/" + points.Count;
                element.pickingMode = PickingMode.Position;
                ChoiceGlow.Apply(element, more);
                element.EnableInClassList("selected", chosen > 0);
                element.tooltip = (more ? "Click to forfeit one Leadership point" : "Click to take these points back") +
                    " · " + chosen + " of " + points.Count + " chosen\n" + BattleObjectChoices.Detail(view, points[0]) + "\n" + tooltip;
            });
            return () => edit(() => BattleObjectChoices.ClickPoints(view, draft, points));
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
