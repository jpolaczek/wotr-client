using System;
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
        private const string ClickableClass = "stage-clickable";
        private const string PreviewedClass = "previewed-stage";
        private const float MarkerLeft = -5;
        private const float MarkerTop = -1;
        private readonly string prefix;
        private readonly List<VisualElement> stages = new List<VisualElement>();
        private readonly List<string> titles = new List<string>();
        private bool clickable;
        private Func<int, bool> reachable = _ => false;
        private int previewed = -1;
        private string shape = "";
        private string scope = "";
        private int current = -1;
        // UI-335: the stage the marker last set off for; only a new one glides.
        private int placed = -1;
        private float? left;
        private float origin;
        private float target;
        private double start;
        private IVisualElementScheduledItem glide;
        public VisualElement Root { get; } = BoardScreen.Row("battle-stages");
        public VisualElement Marker { get; } = new VisualElement { name = "stage-glide", pickingMode = PickingMode.Ignore };
        // UI-509: a stage already reached, clicked.
        public event Action<int> StageClicked;

        // UI-509: its stages take clicks from here on; a ribbon left alone stays informational.
        public bool Clickable
        {
            get => clickable;
            set
            {
                clickable = value;
                shape = "";
            }
        }

        public StageRibbon(string name, string prefix)
        {
            this.prefix = prefix;
            Root.name = name;
            Root.AddToClassList("stage-ribbon");
            Marker.AddToClassList("stage-glide");
            Root.Add(Marker);
            Root.RegisterCallback<GeometryChangedEvent>(_ => Place());
        }

        // `icons` prefixes each stage's icon class; `index` is the current stage, or −1 for none. UI-339: `scope` names the
        // Hunt or battle the row shows; a new one starts the row afresh, so its marker stands at the new stage at once
        // instead of gliding back from where the last one ended.
        public void Apply(IReadOnlyList<(string Key, string Title)> list, int index, string icons, string tooltip, string scope)
        {
            if (scope != this.scope)
            {
                this.scope = scope;
                Forget();
            }
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
            ApplyOffer();
            Place();
        }

        // UI-509: which stages take a click, and which one a preview shows.
        public void Offer(Func<int, bool> reachable, int previewed)
        {
            this.reachable = reachable;
            this.previewed = previewed;
            ApplyOffer();
        }

        private void ApplyOffer()
        {
            if (!clickable)
                return;
            for (var i = 0; i < stages.Count; i++)
            {
                var open = reachable(i);
                stages[i].SetEnabled(open);
                stages[i].EnableInClassList(PreviewedClass, i == previewed);
                stages[i].tooltip = titles[i] + (open ? " · See this stage as it stood" : " · Not reached yet");
            }
        }

        private void MakeClickable(VisualElement stage, int index)
        {
            stage.pickingMode = PickingMode.Position;
            stage.AddToClassList(ClickableClass);
            ObjectPress.Register(stage, () => StageClicked?.Invoke(index));
        }

        private void Build(IReadOnlyList<(string Key, string Title)> list, string icons)
        {
            foreach (var stage in stages)
                stage.RemoveFromHierarchy();
            stages.Clear();
            titles.Clear();
            left = null;
            placed = -1;
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
                if (clickable)
                    MakeClickable(stage, stages.Count);
                Root.Add(stage);
                stages.Add(stage);
                titles.Add(title);
            }
        }

        // Behind the current stage's icon, once the row is laid out: a new stage glides there; a first place, a resize, a
        // redraw or a new parent at the same stage (UI-335: the presented round handing the row to the decision) puts it
        // there at once, or re-aims a glide under way.
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
            // A clickable stage is taller than its row, for its hit area, and stands centred on it.
            Marker.style.top = stage.y + (stage.height - Root.contentRect.height) / 2 + MarkerTop;
            var to = stage.x + MarkerLeft;
            var same = placed == current;
            placed = current;
            if (same && glide?.isActive == true)
            {
                target = to;
                return;
            }
            if (same || !left.HasValue || Reduced() || Mathf.Abs(to - left.Value) < .5f)
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

        // The marker has no place yet: its next stage is a first place.
        private void Forget()
        {
            glide?.Pause();
            left = null;
            placed = -1;
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
