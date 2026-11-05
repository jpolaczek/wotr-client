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
        // UI-347 (D12): sends an immediate battle command (Evil Knowledge on the Mouth of Sauron's portrait).
        private readonly Action<CommandKind> direct;
        // UI-322: the live window's last stacks; null where nothing is compared (a report, a presented state).
        private readonly BattleStackMemory stacks;
        private readonly bool moving;
        private static readonly IReadOnlyList<EffectChoiceView> NoChips = new EffectChoiceView[0];

        public BattleContext(VisualElement root, BattleView battle, PlayerView view, TurnDraft draft,
            Action<Action> edit, Action<string> inspect, CardInspector cards = null, Action<Action> complete = null,
            BattleStackMemory stacks = null, Action<CommandKind> direct = null)
        {
            this.complete = complete;
            this.direct = direct;
            this.stacks = stacks;
            moving = Appearance.Moving(root) && !ValuePulse.Quiet;
            var row = BoardScreen.Row("battle-forces");
            row.name = "battle-forces";
            root.Add(row);
            var rows = new BattleRowPlan(battle, view, draft, edit);
            Force(row, battle.Attacker, "attacker", battle, view, draft, edit, inspect, cards, rows);
            Force(row, battle.Defender, "defender", battle, view, draft, edit, inspect, cards, rows);
            // UI-245: the dice and figures a combat card may choose pulse together.
            var pulse = new ChoicePulse(row);
            refresh.Add(() => pulse.Run(row.Query(className: ChoiceGlow.ClassName).ToList().Count > 0));
        }

        // UI-437: every column holds the same rows in the same order — heading, force, statistics, the ledge when either
        // side has one, card, roll — so a row of one side stands level with the other side's (BattleRowPlan).
        private void Force(VisualElement root, BattleForceView force, string role, BattleView battle, PlayerView view, TurnDraft draft,
            Action<Action> edit, Action<string> inspect, CardInspector cards, BattleRowPlan rows)
        {
            var column = Column(force, role, battle);
            root.Add(column);
            column.Add(ForceRow(force, battle, view, draft, edit));
            column.Add(Spacer());
            column.Add(Statistics(force, battle, view));
            if (rows.Shelf)
                column.Add(Shelf(force, battle, view, rows.ShelfOf(force, battle)));
            AddCard(column, force, role, battle, view, draft, inspect, cards);
            var dice = new BattleDiceStrip(column, force, battle, view, draft, edit, rows);
            refresh.Add(dice.Refresh);
        }

        private static VisualElement Column(BattleForceView force, string role, BattleView battle)
        {
            var column = new VisualElement { name = "battle-" + role };
            column.AddToClassList("battle-force");
            column.AddToClassList(force.Side == Side.FreePeoples ? "fp" : "sh");
            var heading = new Label((role == "attacker" ? "ATTACKER" : "DEFENDER") + " · " + BoardScreen.SideName(force.Side).ToUpperInvariant() +
                (force.Garrison ? " · GARRISON" : "")) { name = role + "-strength", enableRichText = false };
            heading.AddToClassList("battle-force-heading");
            heading.tooltip = BattleForceText.Notes(force, battle);
            column.Add(heading);
            return column;
        }

        // UI-437 (owner): a character stands in the force row beside the figures, as the other side's figures do, rather than
        // in a row of his own above them. The portraits stay outside the composition, which a figure choice hides (UI-208).
        private VisualElement ForceRow(BattleForceView force, BattleView battle, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var row = BoardScreen.Row("battle-force-row");
            row.name = "battle-force-row";
            if (force.Characters.Count > 0)
                row.Add(Characters(force, view, draft, edit));
            row.Add(Composition(force, battle, view, draft, edit));
            return row;
        }

        // UI-437: the room under a force row that is shorter than the other side's, so the rows below stand level; while a
        // column chooses figures it takes none (Battle.uss).
        private static VisualElement Spacer()
        {
            var spacer = new VisualElement { name = "battle-force-spacer", pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("battle-force-spacer");
            return spacer;
        }

        // The Combat card; beside it, in a row of the card's height, a replacement's chip (UI-348) or the replacement made
        // (UI-277).
        private void AddCard(VisualElement column, BattleForceView force, string role, BattleView battle, PlayerView view, TurnDraft draft,
            Action<string> inspect, CardInspector cards)
        {
            var chips = draft != null && complete != null && force.Side == view.You ? CombatReplacementChoice.Chips(view) : NoChips;
            var applied = chips.Count == 0 ? CombatReplacementChoice.Applied(view, force.Side) : null;
            var host = chips.Count > 0 || applied != null ? BoardScreen.Row("battle-card-row") : column;
            if (host != column)
                column.Add(host);
            Card(host, force, role, inspect, CardProhibition(force, battle), cards);
            foreach (var choice in chips)
                host.Add(ReplacementChip(view, draft, choice));
            if (applied != null)
                host.Add(AppliedReplacement(view, applied));
        }

        private static string CardProhibition(BattleForceView force, BattleView battle) => force.Side != Side.FreePeoples ? "" :
            battle.DenethorBlocksCards ? "Denethor · No card" :
            battle.CompanionRequiredForCard && force.Characters.Count == 0 ? "No Companion · No card this round" : "";

        // UI-437: medallions with their names under them, as a figure has its count; the historical version of a character
        // stays. More characters than a row holds named keep their medallions alone (their tooltips name them).
        private VisualElement Characters(BattleForceView force, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var portraits = BoardScreen.Row("battle-portraits");
            portraits.name = "battle-portraits";
            portraits.EnableInClassList(CrowdedClass, force.Characters.Count > NamedCharacters);
            foreach (var character in force.Characters)
                portraits.Add(Character(character, force, view, draft, edit));
            return portraits;
        }

        private VisualElement Character(CharacterView character, BattleForceView force, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            var cancelled = force.CancelledAbilities.Contains(character.Id);
            var cell = new VisualElement { name = "battle-character-" + character.Id, pickingMode = PickingMode.Ignore };
            cell.AddToClassList("battle-character");
            var portrait = new PortraitMedallion(character, CharacterLibrary.Name(character.DefinitionId),
                CharacterLibrary.ShortName(view, character.Id), cancelled);
            // One click handler per portrait; a reaction or Leadership points refresh after the choice target.
            Action other = null;
            ChoiceTarget(portrait, view, draft, edit, () => character.Id, otherwise: () => other?.Invoke());
            other = ReactionSource(portrait, character, view, draft) ?? EvilKnowledgeSource(portrait, character, view, draft) ??
                Points(portrait, view, draft, edit, new[] { character.Id });
            cell.Add(portrait);
            var name = new Label((cancelled ? "⦸ " : "") + CharacterLibrary.ShortName(view, character.Id))
                { name = "battle-character-name", pickingMode = PickingMode.Ignore, enableRichText = false };
            name.AddToClassList("battle-character-name");
            cell.Add(name);
            return cell;
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
                group.tooltip = BattleForceText.Composition(nation.Key, nation.Select(u => u.Kind));
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
                element.tooltip = offered ? TooltipText.Lines(BattleObjectChoices.Detail(view, id), tooltip) : tooltip;
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
            OfferedAbility(portrait, AbilityArt.Find(character.DefinitionId, reaction.Id));
            var tooltip = TooltipText.Lines(BattleReactions.Offer(reaction), portrait.tooltip);
            refresh.Add(() =>
            {
                ChoiceGlow.Apply(portrait, true);
                portrait.tooltip = tooltip;
            });
            return () => complete(() => BattleReactions.Choose(draft, reaction.Id));
        }

        // UI-439: a portrait whose click uses an ability now (a reaction, Evil Knowledge) wears that ability's icon.
        public const string OfferedAbilityName = "portrait-ability";

        private static void OfferedAbility(VisualElement portrait, string icon)
        {
            if (icon.Length == 0)
                return;
            var badge = new VisualElement { name = OfferedAbilityName, pickingMode = PickingMode.Ignore };
            badge.AddToClassList(OfferedAbilityName);
            badge.style.backgroundImage = IconButton.Background(icon);
            portrait.Add(badge);
        }

        // UI-348 (D17): a Combat card's replacement (Brand, a Dark Chieftain) stands beside the player's own card, with the
        // ability's own icon since UI-439; its tooltip names the card it cancels and its click sends the replacement.
        // UI-440 (owner): it is the bare skill icon (SkillIcon), glowing while it can be used, no gilt chip.
        public const string ReplacementClass = "battle-replacement-chip";

        private Button ReplacementChip(PlayerView view, TurnDraft draft, EffectChoiceView choice)
        {
            var chip = SkillIcon.Create(CombatReplacementChoice.Icon(view, choice.SourceId), CombatReplacementChoice.Tooltip(view, choice),
                () => complete(() => BattleObjectChoices.Choose(view, draft, choice.Id)));
            chip.name = "battle-replacement-" + choice.Id;
            chip.AddToClassList(ReplacementClass);
            ChoiceGlow.Apply(chip, true);
            return chip;
        }

        // UI-277: a replacement already made stands in the same place beside the replaced card, for both players; UI-440:
        // the same bare icon, marked as chosen.
        private static VisualElement AppliedReplacement(PlayerView view, CombatReplacementView applied)
        {
            var marker = SkillIcon.Marker(CombatReplacementChoice.Icon(view, applied.SourceId));
            marker.name = "battle-replacement-applied";
            marker.AddToClassList(ReplacementClass);
            marker.AddToClassList("battle-replacement-applied");
            marker.tooltip = CombatReplacementChoice.AppliedTooltip(view, applied);
            return marker;
        }

        // UI-347 (D12): the Mouth of Sauron's Evil Knowledge is his glowing portrait while the siege may go on; its click
        // extends the siege without downgrading an Elite. Returns that click, or null.
        private Action EvilKnowledgeSource(VisualElement portrait, CharacterView character, PlayerView view, TurnDraft draft)
        {
            if (draft == null || direct == null || !EvilKnowledgeChoice.On(view, character))
                return null;
            portrait.pickingMode = PickingMode.Position;
            OfferedAbility(portrait, AbilityArt.For(CommandKind.UseEvilKnowledge));
            var tooltip = TooltipText.Lines(EvilKnowledgeChoice.Offer, portrait.tooltip);
            refresh.Add(() =>
            {
                ChoiceGlow.Apply(portrait, true);
                portrait.tooltip = tooltip;
            });
            return () => direct(CommandKind.UseEvilKnowledge);
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
                var action = more ? "Click to forfeit one Leadership point" : "Click to take these points back";
                element.tooltip = TooltipText.Lines(TooltipText.Parts(action, chosen + " of " + points.Count + " chosen"),
                    BattleObjectChoices.Detail(view, points[0]), tooltip);
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

        // UI-420 (owner): each medallion explains its own number; the row between them has no tooltip of its own.
        private static VisualElement Statistics(BattleForceView force, BattleView battle, PlayerView view)
        {
            var row = BoardScreen.Row("battle-statistics");
            row.name = "battle-statistics";
            row.Add(new BattleStatMedallion("strength", force.Strength.ToString(), BattleForceText.Strength(force)));
            row.Add(new BattleStatMedallion("leadership", force.Leadership.ToString(), BattleForceText.Leadership(force, battle, view)));
            row.Add(new BattleStatMedallion("dice", force.CombatDice.ToString(), BattleForceText.CombatDice(force)));
            row.Add(new BattleStatMedallion("hit", force.HitTarget + "+", BattleForceText.HitTarget(force)));
            return row;
        }

        // UI-437: a side without figures outside the fight keeps the other side's ledge as an empty, unseen row.
        private static VisualElement Shelf(BattleForceView force, BattleView battle, PlayerView view, string text)
        {
            if (text.Length == 0)
                return ReservedShelf();
            var shelf = BoardScreen.Row("battle-garrison-shelf");
            shelf.name = "battle-garrison-shelf";
            var icon = new VisualElement { name = "battle-garrison-icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("battle-garrison-icon");
            shelf.Add(icon);
            var details = BattleForceText.NonparticipantDetails(force, battle, view);
            var label = new Label(text) { name = "battle-nonparticipants", tooltip = details, enableRichText = false };
            label.AddToClassList("battle-nonparticipants");
            shelf.Add(label);
            return shelf;
        }

        private static VisualElement ReservedShelf()
        {
            var shelf = BoardScreen.Row("battle-garrison-shelf");
            shelf.name = "battle-garrison-shelf-reserved";
            shelf.pickingMode = PickingMode.Ignore;
            shelf.AddToClassList(ReservedClass);
            var label = new Label(" ") { name = "battle-nonparticipants-reserved", pickingMode = PickingMode.Ignore };
            label.AddToClassList("battle-nonparticipants");
            shelf.Add(label);
            return shelf;
        }

        // UI-437: a row kept only so both columns hold the same rows; it takes its room and shows nothing.
        public const string ReservedClass = "battle-row-reserved";
        public const string CrowdedClass = "battle-portraits-crowded";
        public const int NamedCharacters = 4;

        private static void Card(VisualElement root, BattleForceView force, string role, Action<string> inspect, string prohibition, CardInspector cards)
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
            button.tooltip = CardTooltip(label, force, visible, faceDown);
            root.Add(button);
            if (visible && cards != null)
                button.AddToClassList(RuntimeTooltip.RichHoverClass);
        }

        // UI-420 (owner): the card slot names the card and what its click does; the force's modifiers stay on the
        // medallions. A slot whose text says everything (choosing, no card) has no tooltip.
        private static string CardTooltip(string label, BattleForceView force, bool visible, bool faceDown)
        {
            if (visible)
                return TooltipText.Lines(TooltipText.Parts(label, force.CardCancelled ? "Cancelled" : ""), "Click to inspect");
            return faceDown ? "Combat card chosen face down · Revealed once both players have chosen" : "";
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
