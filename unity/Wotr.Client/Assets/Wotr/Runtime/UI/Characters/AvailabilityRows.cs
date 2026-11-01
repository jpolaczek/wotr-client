using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-436: a window's actions in words (AvailabilityLines): per action a line naming it and what it takes, and a line
    // saying it is available now or what it still needs; the tooltip keeps the full conditions. Rows are made once and
    // reused, so a refresh only changes their text.
    public sealed class AvailabilityRows
    {
        public const string RowClass = "availability-row";
        public const string ReadyClass = "availability-ready";
        public VisualElement Root { get; }
        private readonly List<Row> rows = new List<Row>();

        public AvailabilityRows(string name)
        {
            Root = new VisualElement { name = name };
            Root.AddToClassList("availability-rows");
        }

        public int Count { get; private set; }

        public void Apply(IReadOnlyList<AvailabilityLine> lines)
        {
            Count = lines.Count;
            while (rows.Count < lines.Count)
                Add();
            for (var index = 0; index < rows.Count; index++)
                rows[index].Apply(index < lines.Count ? lines[index] : null);
        }

        private void Add()
        {
            var row = new Row(Root.name + "-" + rows.Count);
            rows.Add(row);
            Root.Add(row.Root);
        }

        private sealed class Row
        {
            public VisualElement Root { get; } = new VisualElement();
            private readonly Label title = new Label { enableRichText = false };
            private readonly Label state = new Label { enableRichText = false };

            public Row(string name)
            {
                Root.name = name;
                Root.AddToClassList(RowClass);
                title.name = name + "-title";
                state.name = name + "-state";
                title.AddToClassList("availability-title");
                state.AddToClassList("availability-state");
                Root.Add(title);
                Root.Add(state);
            }

            public void Apply(AvailabilityLine line)
            {
                Root.style.display = line == null ? DisplayStyle.None : DisplayStyle.Flex;
                if (line == null)
                    return;
                title.text = line.Title;
                state.text = line.State;
                Root.tooltip = line.Tooltip;
                Root.EnableInClassList(ReadyClass, line.Ready);
            }
        }
    }
}
