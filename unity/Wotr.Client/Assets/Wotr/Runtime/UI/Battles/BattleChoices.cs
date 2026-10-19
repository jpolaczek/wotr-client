using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly BattleLossChoices losses;

        public BattleChoices(VisualElement root, VisualElement footer, PlayerView view, TurnDraft draft, Action<Action> edit,
            Action<Action> complete, Action<CommandKind> direct, BoardPresentation data)
        {
            var option = view.Options.First(o => o.Kind == draft.Kind);
            if (draft.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses)
            {
                losses = new BattleLossChoices(ChoiceHost(root, view, view.Battle?.LossOwner), view, draft, edit);
                refresh.Add(losses.Refresh);
                if (draft.Kind == CommandKind.TakeBattleLosses && view.Battle?.VoluntaryLosses == true && option.Minimum == 0)
                {
                    var keep = BoardScreen.Button("Keep all", () => edit(() => draft.Change(p => p.Losses.Clear())));
                    keep.name = "battle-keep-all";
                    footer.Add(keep);
                }
            }
            if (draft.Kind is CommandKind.AdvanceBattle or CommandKind.ExtendSiege)
            {
                // UI-142: one figure completes a siege extension, so its click extends at once; an advance toggles figures.
                void Choose(string[] ids)
                {
                    if (draft.Kind == CommandKind.ExtendSiege)
                        complete(() => draft.Change(p =>
                        {
                            p.Ids.Clear();
                            p.Ids.Add(ids[0]);
                        }));
                    else
                        edit(() => draft.Change(p =>
                        {
                            foreach (var id in ids)
                                if (!p.Ids.Remove(id))
                                    p.Ids.Add(id);
                        }));
                }
                var grid = new FigureGrid(view.Units.Where(u => option.EligibleIds.Contains(u.Id)), id => draft.Payload.Ids.Contains(id),
                    _ => false, Choose, view.Characters.Where(c => option.EligibleIds.Contains(c.Id)), Choose, false);
                ChoiceHost(root, view, view.You).Add(grid.Root);
                foreach (var id in option.EligibleIds)
                {
                    var figure = grid.Root.Q<Button>("figure-" + id);
                    if (figure != null)
                        figure.userData = new DraftToken(DraftObject.Figure, id, action: draft.Kind);
                }
                refresh.Add(grid.Refresh);
            }
            if (draft.Kind == CommandKind.RetreatBattle)
            {
                var target = new Label { name = "retreat-destination" };
                root.Add(target);
                refresh.Add(() => target.text = draft.Payload.RegionId.Length == 0 ? "Choose a retreat region on the map." : "Retreat to " + data.Regions[draft.Payload.RegionId].Name);
            }
            if (draft.Kind == CommandKind.CommitCombatCard)
                AddDirect(footer, "No card", "battle-no-card", draft.Kind.Value, direct);
            if (draft.Kind == CommandKind.AdvanceBattle)
            {
                AddSelectAll(footer, option, draft, edit);
                AddDirect(footer, "Stay", "battle-stay", draft.Kind.Value, direct);
            }
            foreach (var choice in view.Options.Where(o => o.Kind != draft.Kind && BattleDraft.IsImmediate(o.Kind)))
                AddDirect(footer, DecisionTray.Label(choice.Kind), "action-" + choice.Kind, choice.Kind, direct);
        }

        // UI-146: as in the region panel (UI-140), one toggle selects every figure that can advance, or, once all of
        // them are selected, leaves them all behind. One gesture is one undo step and sends nothing.
        private void AddSelectAll(VisualElement footer, ActionOption option, TurnDraft draft, Action<Action> edit)
        {
            var eligible = option.EligibleIds.ToList();
            bool All() => eligible.Count > 0 && eligible.All(draft.Payload.Ids.Contains);
            var toggle = IconButton.Create("select-all", "Select all · Every figure that can advance", () => edit(() => draft.Change(p =>
            {
                var select = !All();
                p.Ids.Clear();
                if (select)
                    p.Ids.AddRange(eligible);
            })));
            toggle.name = "battle-select-all";
            footer.Add(toggle);
            refresh.Add(() =>
            {
                var all = All();
                IconButton.SetIcon(toggle, all ? "deselect-all" : "select-all");
                toggle.EnableInClassList("all-selected", all);
                toggle.tooltip = all ? "Deselect all · Every figure stays" : "Select all · Every figure that can advance";
            });
        }

        // UI-208: a battle's figure choice (losses, advance, siege extension) stands in the column of the side it concerns,
        // in place of that side's composition: the same units, now as figures to choose. Event losses keep the tray.
        private static VisualElement ChoiceHost(VisualElement root, PlayerView view, Side? side)
        {
            var battle = view.Battle;
            if (battle == null || side == null)
                return root;
            var column = root.Q("battle-" + (side == battle.Attacker.Side ? "attacker" : "defender"));
            if (column == null)
                return root;
            column.AddToClassList("battle-force-choosing");
            return column;
        }

        internal static Button AddDirect(VisualElement root, string title, string name, CommandKind kind, Action<CommandKind> direct)
        {
            var button = BoardScreen.Button(title, () => direct(kind));
            button.name = name;
            // UI-116/UI-119: a framed direct choice carries its icon; the width lives in Battle.uss per button.
            var icon = name == "battle-no-card" ? "no-card" : name == "battle-stay" ? "hold" : IconFor(kind);
            if (icon.Length > 0)
            {
                button.AddToClassList("battle-direct");
                button.Add(DirectIcon(icon));
            }
            button.tooltip = DirectTooltip(kind);
            root.Add(button);
            return button;
        }

        // UI-116: a framed direct choice may carry an icon; the text stays the control.
        public static VisualElement DirectIcon(string kind = "")
        {
            var icon = new VisualElement { name = "battle-direct-icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("battle-direct-icon");
            if (kind.Length > 0)
                icon.AddToClassList("battle-direct-icon-" + kind);
            return icon;
        }

        // UI-271: the Mouth of Sauron's Evil Knowledge says what its click does, as the label names only the ability.
        public static string DirectTooltip(CommandKind kind) => kind switch
        {
            CommandKind.UseEvilKnowledge => "Click to extend the siege by one round without downgrading an Elite · Evil Knowledge, once per action",
            _ => ""
        };

        public static string IconFor(CommandKind kind) => kind switch
        {
            CommandKind.ContinueBattle or CommandKind.FightFieldBattle => "continue",
            CommandKind.StopBattle => "stop",
            CommandKind.HoldBattle => "hold",
            CommandKind.RetreatBattle or CommandKind.RetreatIntoSiege => "retreat",
            CommandKind.AdvanceBattle => "advance",
            CommandKind.ExtendSiege => "extend-siege",
            CommandKind.UseEvilKnowledge => "evil-knowledge",
            _ => ""
        };

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }

        public void Preview(ActionPreview preview) => losses?.Preview(preview);
    }
}
