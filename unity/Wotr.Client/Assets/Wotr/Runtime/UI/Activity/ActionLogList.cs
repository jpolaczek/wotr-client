using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class ActionLogList
    {
        public const float RowHeight = 94;
        public ScrollView Root { get; } = new ScrollView(ScrollViewMode.Vertical) { name = "action-log-list" };
        public event Action<string, string, bool> Inspect;
        public event Action<string> Expand;
        public event Action NearEnd;
        public event Action Left;
        private readonly VisualElement top = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly VisualElement visible = new VisualElement();
        private readonly VisualElement bottom = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        private readonly BoardPresentation data;
        private ActionLogModel model;
        private bool refreshing;
        private float restoringOffset;
        private int layoutVersion;
        public int RenderedCount => rows.Count;

        public ActionLogList(BoardPresentation data)
        {
            this.data = data;
            Root.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(top);
            Root.Add(visible);
            Root.Add(bottom);
            Root.verticalScroller.valueChanged += _ =>
            {
                if (refreshing || Hidden()) return;
                SaveAnchor();
                Render();
                if (Root.verticalScroller.highValue > 0 && Root.verticalScroller.value >= Root.verticalScroller.highValue - RowHeight)
                    NearEnd?.Invoke();
            };
            Root.RegisterCallback<GeometryChangedEvent>(_ => Render());
        }

        public void SaveAnchor()
        {
            if (model == null || model.Rows.Count == 0 || refreshing || Hidden())
                return;
            var position = Root.scrollOffset.y;
            var index = Mathf.Clamp(Mathf.FloorToInt(position / RowHeight), 0, model.Rows.Count - 1);
            model.Anchor = model.Rows[index].Id;
            model.AnchorOffset = position - index * RowHeight;
        }

        public void Apply(ActionLogModel next)
        {
            var version = ++layoutVersion;
            model = next;
            refreshing = true;
            if (model.Anchor.Length == 0 && model.Rows.Count > 0)
                model.Anchor = model.Rows[0].Id;
            var offset = model.Anchor.Length == 0 ? 0 : model.AnchorIndex() * RowHeight + model.AnchorOffset;
            restoringOffset = offset;
            Render();
            Root.schedule.Execute(() => Restore(next, version, offset, 0));
        }

        private bool Hidden() => Root.parent?.resolvedStyle.display == DisplayStyle.None;

        private void Restore(ActionLogModel next, int version, float offset, int attempt)
        {
            if (model != next || version != layoutVersion) return;
            if (!Hidden() && Root.verticalScroller.highValue + 1 < offset && attempt < 10)
            {
                Root.schedule.Execute(() => Restore(next, version, offset, attempt + 1)).StartingIn(16);
                return;
            }
            if (!Hidden()) Root.scrollOffset = new Vector2(0, offset);
            refreshing = false;
            Render();
        }

        public void Latest()
        {
            if (model == null) return;
            model.Anchor = "";
            model.AnchorOffset = 0;
            model.HasNew = false;
            Apply(model);
        }

        private void Render()
        {
            if (model == null) return;
            var height = Root.contentViewport.layout.height;
            if (float.IsNaN(height) || height <= 0) height = 400;
            var focused = Root.panel?.focusController.focusedElement as VisualElement;
            var preserveFocus = focused != null && visible.Contains(focused);
            var offset = refreshing ? restoringOffset : Root.scrollOffset.y;
            var first = Mathf.Max(0, Mathf.FloorToInt(offset / RowHeight) - 1);
            var last = Mathf.Min(model.Rows.Count, first + Mathf.CeilToInt(height / RowHeight) + 3);
            first = Mathf.Min(first, last);
            top.style.height = first * RowHeight;
            bottom.style.height = (model.Rows.Count - last) * RowHeight;
            var keys = new HashSet<string>();
            for (var i = first; i < last; i++)
            {
                var item = model.Rows[i];
                keys.Add(item.Id);
                if (!rows.TryGetValue(item.Id, out var row))
                    rows[item.Id] = row = new Row(this);
                row.Apply(item, data, model.Expanded(item.Group.Action.Id));
                if (visible.IndexOf(row.Root) != i - first)
                    visible.Insert(i - first, row.Root);
            }
            foreach (var key in rows.Keys.Where(key => !keys.Contains(key)).ToArray())
            {
                rows[key].Root.RemoveFromHierarchy();
                rows.Remove(key);
            }
            if (preserveFocus && visible.Contains(focused) && Root.panel?.focusController.focusedElement != focused)
                focused.Focus();
        }

        private sealed class Row
        {
            public VisualElement Root { get; } = new VisualElement();
            private readonly Button open;
            private readonly Button expand;
            private readonly Label heading = new Label { enableRichText = false };
            private readonly Label content = new Label { enableRichText = false };
            private readonly Label cost = new Label { enableRichText = false };
            private ActionLogRow item;

            public Row(ActionLogList owner)
            {
                Root.AddToClassList("action-log-row");
                open = new Button(() => owner.Inspect?.Invoke(item.Group.Action.Id, item.Step?.Id ?? "", true));
                open.AddToClassList("action-log-open");
                heading.AddToClassList("action-log-heading");
                content.AddToClassList("action-log-content");
                cost.AddToClassList("action-log-cost");
                open.Add(heading);
                open.Add(cost);
                open.Add(content);
                open.AddToClassList(RuntimeTooltip.RichHoverClass);
                expand = IconButton.Create("expand", "Show more", () => owner.Expand?.Invoke(item.Group.Action.Id));
                expand.AddToClassList("action-log-expand");
                expand.tooltip = "Show chronological steps";
                Root.Add(open);
                Root.Add(expand);

            }

            public void Apply(ActionLogRow next, BoardPresentation data, bool expanded)
            {
                item = next;
                Root.name = "log-row-" + next.Id;
                open.name = "log-open-" + next.Id;
                expand.name = "log-expand-" + next.Group.Action.Id;
                var group = next.Group;
                heading.text = next.Step == null
                    ? (next.TurnHeading && group.Action.Turn.HasValue ? "Turn " + group.Action.Turn + " · " : "") + ActionCardText.Heading(group.Action)
                    : ActionCardText.Actor(next.Step.Actor) + " · " + (next.Step.Command.HasValue
                        ? ActionCardText.Words(next.Step.Command.Value.ToString()) : ActionCardText.Words(next.Step.Kind));
                content.text = next.Step == null ? ActionCardText.Compact(group, data) : next.Step.Text;
                cost.text = string.Join(" · ", (next.Step == null ? group.Action.Costs : ActionCardText.ExtraCosts(group.Action, next.Step))
                    .Select(ActionCardText.Cost));
                cost.style.display = cost.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                open.tooltip = heading.text + "\n" + content.text + "\n" + string.Join(" · ", group.Action.Costs.Select(ActionCardText.Cost));
                Root.EnableInClassList("action-log-child", next.Step != null);
                expand.style.display = next.Step == null && group.Action.StepCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                IconButton.SetIcon(expand, expanded ? "collapse" : "expand");
                expand.tooltip = expanded ? "Hide the chronological steps" : "Show chronological steps";
            }
        }
    }
}
