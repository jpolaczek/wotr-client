using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntReportStrip
    {
        public VisualElement Root { get; } = BoardScreen.Row("hunt-report-strip");
        private readonly Label[] entries = new Label[3];
        private readonly Button previous;
        private readonly Button next;
        private readonly BoardPresentation data;
        private readonly Dictionary<string, string> faces;
        private HuntReportView report;
        private int index;
        private int observedCount;

        public HuntReportStrip(BoardPresentation data)
        {
            this.data = data;
            faces = data.Board.HuntTiles.ToDictionary(t => t.Id, t => FigureGrid.Name(t.Value));
            previous = IconButton.Create("back", "Previous", () => { index--; Refresh(); });
            previous.name = "hunt-report-previous";
            previous.tooltip = "Previous known Hunt event";
            next = IconButton.Create("next", "Next", () => { index++; Refresh(); });
            next.name = "hunt-report-next";
            next.tooltip = "Next known Hunt event";
            Root.Add(previous);
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = new Label { name = i == 2 ? "hunt-report-entry" : "hunt-report-entry-" + i, focusable = true };
                entry.AddToClassList("hunt-report-event");
                entries[i] = entry;
                Root.Add(entry);
            }
            Root.Add(next);
        }

        public void Apply(HuntReportView latest)
        {
            // Follow additions only while reading the latest steps. An older page keeps its place.
            if (report?.Id != latest?.Id || index >= observedCount - 1)
                index = (latest?.Entries.Count ?? 1) - 1;
            report = latest;
            observedCount = latest?.Entries.Count ?? 0;
            Refresh();
        }

        private void Refresh()
        {
            var count = report?.Entries.Count ?? 0;
            Root.style.display = count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (count == 0)
                return;
            index = System.Math.Max(0, System.Math.Min(count - 1, index));
            var first = System.Math.Max(0, index - 2);
            for (var i = 0; i < entries.Length; i++)
            {
                var position = index - 2 + i;
                entries[i].style.display = position < first ? DisplayStyle.None : DisplayStyle.Flex;
                if (position >= first)
                    Show(entries[i], report.Entries[position], position, count);
            }
            Root.tooltip = index == count - 1 ? "Latest known Hunt steps" : "Earlier Hunt steps · Current choices are unchanged";
            previous.SetEnabled(first > 0);
            next.SetEnabled(index < count - 1);
        }

        private void Show(Label entry, HuntReportEntryView item, int position, int count)
        {
            var face = item.Hunt != null && faces.TryGetValue(item.Hunt.TileId, out var value) ? value : "";
            var detail = HuntReportText.Entry(item, data, face);
            entry.text = Symbol(item.Kind) + " " + detail;
            entry.tooltip = detail + "\n" + HuntReportText.Source(item.SourceId, data) +
                " · Event " + (position + 1) + "/" + count;
            if (item.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled && item.Hunt != null)
                entry.tooltip += "\n" + string.Join(", ", item.Hunt.Dice.Select(d => d.Rerolled ? d.First + " → " + d.Result : d.Result.ToString())) +
                    " · Bonus +" + item.Hunt.Bonus + " · Reroll bonus +" + item.Hunt.RerollBonus;
        }

        private static string Symbol(HuntReportKind kind) => kind switch
        {
            HuntReportKind.Rolled or HuntReportKind.Rerolled => "●",
            HuntReportKind.TileDrawn or HuntReportKind.TileReplaced or HuntReportKind.DamageDetermined or HuntReportKind.DamageChanged => "◇",
            HuntReportKind.GuideChanged or HuntReportKind.CompanionLeft or HuntReportKind.CompanionPlaced or HuntReportKind.CharacterEliminated => "★",
            HuntReportKind.Revealed => "◆",
            HuntReportKind.Progressed => "→",
            HuntReportKind.HuntCompleted => "✓",
            _ => "○"
        };
    }
}
