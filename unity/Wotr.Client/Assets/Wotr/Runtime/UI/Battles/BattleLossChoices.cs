using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-118: the loss set as illustrated figure tokens. A click cycles keep → downgrade → remove; the overlay
    // and the order badge show the choice, wax seals show the hits assigned so far. The server's preview stays
    // in the tooltips and the budget line; Apply losses is the single confirmation.
    public sealed class BattleLossChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private readonly PlayerView view;
        private readonly TurnDraft draft;
        private readonly Label budget = new Label { name = "battle-loss-budget" };
        private readonly VisualElement seals = new VisualElement { name = "battle-hit-seals", pickingMode = PickingMode.Ignore };

        public BattleLossChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            this.view = view;
            this.draft = draft;
            var eligible = view.Options.First(o => o.Kind == draft.Kind).EligibleIds.ToHashSet();
            var units = view.Units.Where(u => eligible.Contains(u.Id)).OrderBy(u => u.Nation).ThenBy(u => u.Kind).ToList();
            var heading = BoardScreen.Row("battle-loss-heading");
            heading.Add(Legend());
            seals.AddToClassList("battle-hit-seals");
            heading.Add(seals);
            heading.Add(budget);
            root.Add(heading);
            var row = BoardScreen.Row("loss-figures");
            root.Add(row);
            foreach (var unit in units)
            {
                var button = BoardScreen.Button("", () => edit(() => BattleDraft.CycleLoss(draft, unit)));
                button.name = "loss-" + unit.Id;
                button.userData = new DraftToken(DraftObject.Figure, unit.Id, action: draft.Kind);
                button.AddToClassList("loss-figure");
                FigureArtwork.Apply(button, unit);
                var emblem = new VisualElement { name = "loss-figure-nation", pickingMode = PickingMode.Ignore };
                emblem.AddToClassList("loss-figure-nation");
                emblem.AddToClassList("nation-emblem-" + unit.Nation);
                button.Add(emblem);
                var overlay = new VisualElement { name = "loss-overlay", pickingMode = PickingMode.Ignore };
                overlay.AddToClassList("loss-overlay");
                button.Add(overlay);
                row.Add(button);
                buttons.Add(unit.Id, button);
                refresh.Add(() =>
                {
                    var loss = draft.Payload.Losses.FirstOrDefault(l => l.UnitId == unit.Id);
                    var order = loss == null ? "" : (draft.Payload.Losses.IndexOf(loss) + 1) + ".";
                    // The badge keeps the order and the operation glyph; the overlay repeats the operation in art.
                    button.text = order + (loss == null ? "" : loss.Kind == BattleLossKind.Remove ? "×" : "↓");
                    button.EnableInClassList("selected", loss != null);
                    button.EnableInClassList("loss-removed", loss?.Kind == BattleLossKind.Remove);
                    overlay.EnableInClassList("loss-overlay-downgrade", loss?.Kind == BattleLossKind.Downgrade);
                    overlay.EnableInClassList("loss-overlay-remove", loss?.Kind == BattleLossKind.Remove);
                    button.tooltip = FigureGrid.Name(unit.Nation) + " " + unit.Kind + (loss == null ? " · Keep" : " · " + loss.Kind + " · Order " + (draft.Payload.Losses.IndexOf(loss) + 1)) +
                        (unit.Kind == UnitKind.Elite ? "\nClick: keep → downgrade (1 hit) → remove (2 hits) → keep" : "\nClick: keep ↔ remove (1 hit)") +
                        "\nOrder controls replacement availability. Clear and reselect a figure to move it last.";
                });
            }
        }

        // Keep / downgrade / remove as icons with their hit costs; the full rule stays in the tooltip.
        private static VisualElement Legend()
        {
            var legend = BoardScreen.Row("battle-loss-legend");
            legend.name = "battle-loss-legend";
            legend.tooltip = "Regular: keep ↔ remove (1 hit). Elite: keep → downgrade (1 hit) → remove (2 hits) → keep. Clear and reselect to move an operation last.";
            legend.Add(LegendItem("downgrade", "↓ 1"));
            legend.Add(LegendItem("remove", "× 1 · ◆ 2"));
            return legend;
        }

        private static VisualElement LegendItem(string kind, string text)
        {
            var item = BoardScreen.Row("battle-loss-legend-item");
            item.pickingMode = PickingMode.Ignore;
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("loss-overlay");
            icon.AddToClassList("loss-overlay-" + kind);
            icon.AddToClassList("battle-loss-legend-icon");
            item.Add(icon);
            item.Add(new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false });
            return item;
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
            budget.text = BattleLossText.Budget(view, draft);
            budget.tooltip = "Choice order matters: an earlier regular loss can supply a later elite replacement.";
            Seals(BattleDraft.Damage(draft, view), view.EventLoss?.Hits ?? view.Battle?.PendingHits ?? 0);
        }

        // One wax seal per hit to assign; assigned hits are pressed flat. Voluntary losses have no fixed count.
        private void Seals(int assigned, int hits)
        {
            var wanted = view.Battle?.VoluntaryLosses == true ? 0 : Math.Min(hits, 12);
            while (seals.childCount > wanted)
                seals.RemoveAt(seals.childCount - 1);
            while (seals.childCount < wanted)
            {
                var seal = new VisualElement { pickingMode = PickingMode.Ignore };
                seal.AddToClassList("battle-hit-seal");
                seals.Add(seal);
            }
            for (var i = 0; i < seals.childCount; i++)
                seals[i].EnableInClassList("battle-hit-seal-spent", i < assigned);
            seals.tooltip = hits + (hits == 1 ? " hit" : " hits") + " to assign";
        }

        public void Preview(ActionPreview preview)
        {
            if (preview?.Losses == null) return;
            Refresh();
            budget.text = BattleLossText.Budget(view, draft, preview.Losses);
            budget.tooltip = BattleLossText.Details(preview.Losses);
            foreach (var loss in preview.Losses.Operations)
                if (buttons.TryGetValue(loss.Figure.Id, out var button))
                    button.tooltip += "\nServer preview: " + BattleLossText.Operation(loss);
        }
    }
}
