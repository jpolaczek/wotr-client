using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-154: the stones of the Mordor track over the map. The ink path lies in the renderer's vector layer; the steps
    // and gates are laid out in screen space like markers, so they keep their size, carry their numbers and tooltips,
    // and reserve their place before the army markers are laid out.
    public sealed class MapMordorTrack
    {
        public const string SlabClass = "mordor-slab";
        public const string CrackClass = "mordor-crack";
        public const string GateClass = "mordor-gate";
        public const string PassedClass = "mordor-passed";
        public const string CurrentClass = "mordor-current";
        public const string NextClass = "mordor-next";
        public const string InactiveClass = "mordor-track-inactive";
        public const string DetailClass = "mordor-track-detail";
        public const string PulseClass = "mordor-track-pulse";
        private const long PulseMilliseconds = 1200;
        // UI-157: sized to read over the ash plain at both levels of detail.
        private static readonly Vector2 SlabDetail = new Vector2(34, 25), SlabToken = new Vector2(22, 17);
        private static readonly Vector2 CrackDetail = new Vector2(46, 38), CrackToken = new Vector2(30, 25);
        private static readonly Vector2 GateDetail = new Vector2(23, 23), GateToken = new Vector2(17, 17);
        public VisualElement Root { get; } = new VisualElement { name = "mordor-track-stones", pickingMode = PickingMode.Ignore };
        public MordorTrackState State { get; private set; } = MordorTrackState.Outside;
        // UI-155: the step a click would play (0 enters, the next one attempts a move), and the entering gate.
        public int OfferedStep { get; private set; } = -1;
        public string OfferedGate { get; private set; } = "";
        public event Action<int> StepClicked;
        public event Action<string> GateClicked;
        // The Fellowship stepped onto the track in this match, after the board had seen it elsewhere.
        public event Action Entered;
        private bool seen;
        private readonly MapCanvas map;
        private readonly MordorTrackGeometry geometry;
        private readonly Button[] steps = new Button[MordorTrackGeometry.LastStep + 1];
        private readonly Dictionary<string, Button> gates = new Dictionary<string, Button>();

        public MapMordorTrack(MapCanvas map)
        {
            this.map = map;
            geometry = map.Data.MordorTrack;
            Root.StretchToParentSize();
            for (var step = 0; step <= MordorTrackGeometry.LastStep; step++)
                steps[step] = Step(step);
            foreach (var entrance in geometry.Entrances)
                gates.Add(entrance.Region, Gate(entrance.Region));
            map.Add(Root);
            Apply(MordorTrackState.Outside);
        }

        public Button StepButton(int step) => steps[step];
        public Button GateButton(string region) => gates[region];

        // A live view may announce an entry; the fixture and the first sight of a match never do.
        public void Apply(MordorTrackState state, bool live = true)
        {
            var entered = live && seen && !State.Active && state.Active;
            seen = live;
            State = state;
            Root.EnableInClassList(InactiveClass, !state.Active);
            for (var step = 0; step < steps.Length; step++)
            {
                var slab = state.Slab(step);
                steps[step].EnableInClassList(PassedClass, slab == MordorSlabState.Passed);
                steps[step].EnableInClassList(CurrentClass, slab == MordorSlabState.Current);
            }
            map.Renderer.MordorTrackActive(state.Active);
            RefreshOffer();
            if (entered)
                Entered?.Invoke();
        }

        public void Offer(int step, string gate)
        {
            OfferedStep = step;
            OfferedGate = gate ?? "";
            RefreshOffer();
        }

        // A short swell of the stones, for the player who did not enter: the camera stays where it is.
        public void Pulse()
        {
            Root.AddToClassList(PulseClass);
            Root.schedule.Execute(() => Root.RemoveFromClassList(PulseClass)).StartingIn(PulseMilliseconds);
        }

        private void RefreshOffer()
        {
            for (var step = 0; step < steps.Length; step++)
            {
                var offered = step == OfferedStep;
                steps[step].EnableInClassList(NextClass, offered);
                steps[step].tooltip = State.SlabTooltip(step) + (!offered ? "" : step == 0 && OfferedGate.Length > 0
                    ? "\nClick to enter Mordor." : "\nClick to attempt the move · A Hunt tile is drawn first and may stop it.");
            }
            foreach (var gate in gates)
            {
                var offered = gate.Key == OfferedGate;
                gate.Value.EnableInClassList(NextClass, offered);
                gate.Value.tooltip = MordorTrackState.GateTooltip(gate.Key) + (offered ? "\nClick to enter Mordor." : "");
            }
        }

        public void Layout()
        {
            var detail = map.MarkerVisibility.Detail;
            Root.EnableInClassList(DetailClass, detail);
            for (var step = 0; step < steps.Length; step++)
                Place(steps[step], geometry.Step(step), step == MordorTrackGeometry.LastStep
                    ? detail ? CrackDetail : CrackToken : detail ? SlabDetail : SlabToken);
            foreach (var entrance in geometry.Entrances)
                Place(gates[entrance.Region], entrance.Gate, detail ? GateDetail : GateToken);
        }

        private void Place(VisualElement element, Vector2 point, Vector2 size)
        {
            var centre = map.Camera.ToScreen(point);
            var visible = map.contentRect.Contains(centre);
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            var rect = new Rect(centre - size / 2, size);
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
            map.ReserveMarker(rect);
        }

        private Button Step(int step)
        {
            var last = step == MordorTrackGeometry.LastStep;
            var button = new Button(() => { if (step == OfferedStep) StepClicked?.Invoke(step); })
                { name = "mordor-track-step-" + step, focusable = false };
            button.AddToClassList(last ? CrackClass : SlabClass);
            // UI-157: the Crack of Doom carries no caption on the map; its tooltip and the legend name it.
            if (!last)
                button.Add(new Label(step.ToString()) { name = "mordor-step-number", pickingMode = PickingMode.Ignore });
            Root.Add(button);
            return button;
        }

        private Button Gate(string region)
        {
            var button = new Button(() => { if (region == OfferedGate) GateClicked?.Invoke(region); })
                { name = "mordor-track-gate-" + region, focusable = false, tooltip = MordorTrackState.GateTooltip(region) };
            button.AddToClassList(GateClass);
            Root.Add(button);
            return button;
        }
    }
}
