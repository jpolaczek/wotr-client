using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // A read-only track. UI-110: its symbol is also the Fellowship's move, glowing while a click on it can move.
    public sealed class FellowshipTrackRow
    {
        // UI-95: the marker lives in the lane and glides between cells instead of jumping.
        public const string GlidingClass = "track-marker-gliding";
        public const string MoveClass = "track-move";
        // UI-231: steps behind the marker; the Mordor track darkens them as cooled stones.
        public const string PassedClass = "track-passed";
        private const string BrightClass = "track-move-bright";
        private const long PulseMilliseconds = 700;
        private const float OverflowWidth = 9;
        public VisualElement Root { get; }
        public Label Value { get; }
        // UI-110: the symbol's button; enabled and glowing only while the chosen die can move the Fellowship.
        public Button Move { get; }
        public event Action Activated;
        // UI-293 (owner's decision): the Fellowship's marker on the track is the Hide control.
        public VisualElement Marker => marker;
        public bool MarkerOffered { get; private set; }
        public event Action MarkerActivated;
        private readonly Label[] cells;
        private readonly VisualElement lane;
        private readonly Image marker;
        private readonly Label overflow = new Label("+") { pickingMode = PickingMode.Ignore };
        private int selected = -1;
        private readonly IVisualElementScheduledItem pulse;
        private readonly ChoicePulse markerPulse;

        public FellowshipTrackRow(string name, string icon, int maximum, string cellPrefix, string valueName)
        {
            Root = new VisualElement { name = name, focusable = true };
            Root.AddToClassList("fellowship-track");
            Root.AddToClassList(name);
            var symbol = FellowshipArtwork.Image(icon);
            symbol.AddToClassList("fellowship-track-symbol");
            symbol.pickingMode = PickingMode.Ignore;
            Move = new Button(() => Activated?.Invoke()) { name = name + "-move" };
            Move.AddToClassList("fellowship-track-move");
            var glow = new VisualElement { name = name + "-glow", pickingMode = PickingMode.Ignore };
            glow.AddToClassList("track-move-glow");
            Move.Add(glow);
            Move.Add(symbol);
            Move.SetEnabled(false);
            Root.Add(Move);
            pulse = Move.schedule.Execute(() => Move.ToggleInClassList(BrightClass)).Every(PulseMilliseconds);
            pulse.Pause();
            lane = new VisualElement { name = name + "-lane", pickingMode = PickingMode.Ignore };
            lane.AddToClassList("fellowship-track-lane");
            Root.Add(lane);
            if (maximum == MordorTrackGeometry.LastStep)
                lane.Add(MordorPath());
            cells = new Label[maximum + 1];
            for (var i = 0; i < cells.Length; i++)
            {
                var checkpoint = i == 0 || i == maximum || maximum == 12 && i % 3 == 0;
                var cell = new Label { name = cellPrefix + i, pickingMode = PickingMode.Ignore };
                cell.AddToClassList("fellowship-track-cell");
                var tick = new Label(checkpoint ? i.ToString() : "") { pickingMode = PickingMode.Ignore };
                tick.AddToClassList("fellowship-track-tick");
                cell.Add(tick);
                if (maximum == 5 && i == maximum)
                {
                    var fire = FellowshipArtwork.Image("crack-of-doom");
                    fire.AddToClassList("fellowship-track-end");
                    cell.Add(fire);
                }
                cells[i] = cell;
                lane.Add(cell);
            }
            marker = FellowshipArtwork.Image(icon);
            marker.name = name + "-marker";
            marker.AddToClassList("fellowship-track-marker");
            marker.AddToClassList(PressFeedback.PressableClass);
            marker.AddManipulator(new Clickable(() =>
            {
                if (MarkerOffered)
                    MarkerActivated?.Invoke();
            }));
            lane.Add(marker);
            markerPulse = new ChoicePulse(lane);
            lane.RegisterCallback<GeometryChangedEvent>(_ => Place(false));
            overflow.AddToClassList("fellowship-track-overflow");
            lane.Add(overflow);
            Value = new Label { name = valueName, pickingMode = PickingMode.Ignore };
            Value.AddToClassList("fellowship-track-value");
            Root.Add(Value);
        }

        // UI-231: the Mordor track's steps are stones on a line of embers, unlike the progress and corruption cells.
        private static VisualElement MordorPath()
        {
            var path = new VisualElement { name = "mordor-track-path", pickingMode = PickingMode.Ignore };
            path.AddToClassList("mordor-track-path");
            return path;
        }

        // Only the visible track carries the move; its name stays the Fellowship's progress action.
        public void Offer(bool offered, string name, string tooltip)
        {
            Move.name = name;
            Move.SetEnabled(offered);
            Move.EnableInClassList(MoveClass, offered);
            Move.tooltip = offered ? tooltip : "";
            if (offered)
                pulse.Resume();
            else
            {
                pulse.Pause();
                Move.RemoveFromClassList(BrightClass);
            }
        }

        // UI-293: while hiding is legal the marker glows and pulses (ChoiceGlow) and says what a click does; otherwise it is
        // a picture that takes no pointer.
        public void OfferMarker(bool offered, string tooltip)
        {
            MarkerOffered = offered;
            marker.pickingMode = offered ? PickingMode.Position : PickingMode.Ignore;
            marker.focusable = offered;
            marker.EnableInClassList(FellowshipStatus.HideClass, offered);
            ChoiceGlow.Apply(marker, offered);
            marker.tooltip = offered ? tooltip : "";
            markerPulse.Run(offered);
        }

        // The row's own controls: the symbol's move and, while it hides, the marker.
        public bool Controls(VisualElement target) => target == Move || Move.Contains(target) || MarkerOffered && target == marker;

        public void Apply(FellowshipTrackState state)
        {
            Root.tooltip = state.Description;
            Value.text = state.Value.ToString();
            Value.tooltip = state.Description;
            Root.EnableInClassList("track-overflow", state.BeyondTrack);
            if (selected != state.Position)
            {
                if (selected >= 0)
                    cells[selected].RemoveFromClassList("selected");
                var moved = selected >= 0;
                selected = state.Position;
                cells[selected].AddToClassList("selected");
                for (var i = 0; i < cells.Length; i++)
                    cells[i].EnableInClassList(PassedClass, i < selected);
                Place(true);
                if (moved)
                    Flash();
            }
            marker.sprite = FellowshipArtwork.Sprite(state.Symbol);
            overflow.style.display = state.BeyondTrack ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // AUDIO-06: a moved marker is briefly marked; with Reduced motion it stands for the glide (Wotr.uss).
        public const string ChangedClass = "track-changed";

        private void Flash()
        {
            Root.AddToClassList(ChangedClass);
            Root.schedule.Execute(() => Root.RemoveFromClassList(ChangedClass)).StartingIn(300);
        }

        // Only a new position glides; a first or resized layout places the marker at once,
        // so a loaded board or a scale change never replays travel.
        private void Place(bool glide)
        {
            if (selected < 0)
                return;
            var cell = cells[selected].layout;
            if (float.IsNaN(cell.width) || cell.width <= 0)
                return;
            if (!glide)
                marker.RemoveFromClassList(GlidingClass);
            // An absolute marker counts from inside the lane's border (UI-231: the Mordor band has one); a cell's layout
            // counts from outside it. On the bordered band the marker sits one pixel inside its round stone on both
            // axes — the band's height differs between the panel's layouts, and scaled sub-pixel rounding cannot push
            // the marker past the stone's edge.
            var border = lane.resolvedStyle.borderLeftWidth;
            var inset = border > 0 ? 1 : 0;
            marker.style.left = cell.x - border + inset;
            marker.style.width = cell.width - 2 * inset;
            if (border > 0)
            {
                marker.style.top = cell.y - lane.resolvedStyle.borderTopWidth + inset;
                marker.style.height = cell.height - 2 * inset;
            }
            overflow.style.left = cell.xMax - border - OverflowWidth;
            if (!marker.ClassListContains(GlidingClass))
                marker.schedule.Execute(() => marker.AddToClassList(GlidingClass));
        }
    }
}
