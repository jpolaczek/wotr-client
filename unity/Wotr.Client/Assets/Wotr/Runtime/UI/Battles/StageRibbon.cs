using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-327 (owner 2026-10-25, of the Hunt's Roll › Re-roll › Tile › Damage › Result: "on this track the marker is to move
    // smoothly"): the stage ribbon of the battle and Hunt windows as one lasting row. Its stages are built once for a list
    // of stages; a change of stage glides one gilt marker from the old stage to the new one (eased, 0.35 s) while the labels
    // crossfade their colour (Battle.uss), instead of drawing a new row with the marker already at its new place. A window
    // that redraws its content may take the ribbon out and put it back: the marker goes on from where it stood. Under Skip
    // animations it moves at once.
    public sealed class StageRibbon
    {
        public const double GlideSeconds = .35;
        private const float MarkerLeft = -5;
        private const float MarkerTop = -1;
        private readonly string prefix;
        private readonly List<VisualElement> stages = new List<VisualElement>();
        private string shape = "";
        private int current = -1;
        private float? left;
        private float origin;
        private float target;
        private double start;
        private IVisualElementScheduledItem glide;
        public VisualElement Root { get; } = BoardScreen.Row("battle-stages");
        public VisualElement Marker { get; } = new VisualElement { name = "stage-glide", pickingMode = PickingMode.Ignore };

        public StageRibbon(string name, string prefix)
        {
            this.prefix = prefix;
            Root.name = name;
            Root.AddToClassList("stage-ribbon");
            Marker.AddToClassList("stage-glide");
            Root.Add(Marker);
            Root.RegisterCallback<GeometryChangedEvent>(_ => Place());
        }

        // `icons` prefixes each stage's icon class; `index` is the current stage, or −1 for none.
        public void Apply(IReadOnlyList<(string Key, string Title)> list, int index, string icons, string tooltip)
        {
            var key = string.Join("|", list.Select(stage => stage.Key));
            if (key != shape)
            {
                Build(list, icons);
                shape = key;
            }
            for (var i = 0; i < stages.Count; i++)
                stages[i].EnableInClassList("current-stage", i == index);
            current = index;
            Root.tooltip = tooltip;
            Place();
        }

        private void Build(IReadOnlyList<(string Key, string Title)> list, string icons)
        {
            foreach (var stage in stages)
                stage.RemoveFromHierarchy();
            stages.Clear();
            left = null;
            foreach (var (key, title) in list)
            {
                var stage = new VisualElement { name = prefix + key, pickingMode = PickingMode.Ignore };
                stage.AddToClassList("battle-stage");
                var icon = new VisualElement { name = prefix + "icon", pickingMode = PickingMode.Ignore };
                icon.AddToClassList("battle-stage-icon");
                icon.AddToClassList(icons + key);
                stage.Add(icon);
                stage.Add(new Label(title) { pickingMode = PickingMode.Ignore, enableRichText = false });
                // A stage that moves inside an unchanged row (its label laid out late) moves the marker with it.
                stage.RegisterCallback<GeometryChangedEvent>(_ => Place());
                Root.Add(stage);
                stages.Add(stage);
            }
        }

        // Behind the current stage's icon, once the row is laid out: a new stage glides there, a resize or a first place jumps.
        private void Place()
        {
            if (current < 0 || current >= stages.Count)
            {
                Marker.style.display = DisplayStyle.None;
                return;
            }
            var stage = stages[current].layout;
            if (stage.width <= 0 || float.IsNaN(stage.x))
                return;
            Marker.style.display = DisplayStyle.Flex;
            Marker.style.top = stage.y + MarkerTop;
            var to = stage.x + MarkerLeft;
            if (glide?.isActive == true && Mathf.Abs(to - target) < .5f)
                return;
            if (!left.HasValue || Reduced() || Mathf.Abs(to - left.Value) < .5f)
            {
                Jump(to);
                return;
            }
            origin = left.Value;
            target = to;
            start = Time.realtimeSinceStartupAsDouble;
            glide ??= Marker.schedule.Execute(Step).Every(16);
            glide.Resume();
        }

        private void Jump(float to)
        {
            glide?.Pause();
            left = target = to;
            Marker.style.left = to;
        }

        private void Step()
        {
            var t = Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - start) / GlideSeconds));
            var eased = t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            left = Mathf.Lerp(origin, target, eased);
            Marker.style.left = left.Value;
            if (t >= 1)
                glide.Pause();
        }

        private bool Reduced()
        {
            for (var element = Root; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }
    }
}
