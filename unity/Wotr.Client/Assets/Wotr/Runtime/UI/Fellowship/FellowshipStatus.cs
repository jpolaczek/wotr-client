using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Public state only; private companion versions belong to FellowshipRoster.
    public sealed class FellowshipStatus
    {
        public Button Summary { get; }
        public VisualElement Root { get; } = new VisualElement { name = "fellowship-details" };
        // UI-90: condition and last known position, hosted by the panel header's locate button.
        public VisualElement StateRow { get; } = BoardScreen.Row("fellowship-state-row");
        public string Description => state.tooltip + "\n" + location.tooltip;
        // UI-110: the glowing track symbol moves the Fellowship instead of a separate Move button.
        public const string MoveName = "fellowship-progress";
        public event Action MoveRequested;
        // UI-155: a click on the Mordor track row (not its move symbol) centres the map on the track.
        public event Action TrackFocusRequested;
        // UI-293 (owner's decision, after UI-232): a click on the Fellowship's marker on its track while hiding is legal.
        // The Ring on the plaque and in the panel's header is a picture of the condition only.
        public event Action HideRequested;
        public VisualElement HideMarker => inMordor ? mordor.Marker : journey.Marker;
        private readonly Label state = new Label { name = "fellowship-condition", pickingMode = PickingMode.Ignore };
        private readonly Label location = new Label { name = "fellowship-location", pickingMode = PickingMode.Ignore };
        private readonly Label guide = new Label { name = "fellowship-guide" };
        private readonly Label activity = new Label { name = "mordor-activity" };
        private readonly VisualElement guideRow = BoardScreen.Row("fellowship-guide-row");
        private readonly Image condition;
        private readonly Image summaryCondition;
        private readonly Image summaryJourney;
        private readonly Label summaryLocation = new Label { name = "fellowship-status-location", pickingMode = PickingMode.Ignore };
        private readonly Label summaryState = new Label { name = "fellowship-status-condition", pickingMode = PickingMode.Ignore };
        private readonly Label summaryProgress = new Label { name = "fellowship-status-progress", pickingMode = PickingMode.Ignore };
        private readonly Label summaryCorruption = new Label { name = "fellowship-status-corruption", pickingMode = PickingMode.Ignore };
        private readonly FellowshipTrackRow journey = new FellowshipTrackRow("journey-track", "journey", 12, "journey-step-", "journey-label");
        private readonly FellowshipTrackRow mordor = new FellowshipTrackRow("mordor-track", "mordor-gate", 5, "mordor-step-", "mordor-label");
        private readonly FellowshipTrackRow corruption = new FellowshipTrackRow("corruption-track", "corruption", 12, "corruption-step-", "corruption-label");
        private bool inMordor;

        public const string HideClass = "fellowship-hide-target";
        private string summaryStateText = "";

        public FellowshipStatus(Action open)
        {
            // UI-98: the top bar's plaque — condition, last known position, Progress and Corruption.
            Summary = BoardScreen.Button("", open);
            Summary.name = "fellowship-status";
            Summary.AddToClassList("fellowship-status");
            Summary.AddToClassList(EdgePlaque.ClassName);
            Summary.style.display = DisplayStyle.None;
            summaryCondition = FellowshipArtwork.Image("hidden");
            summaryCondition.name = "fellowship-ring";
            summaryCondition.AddToClassList(EdgePlaque.EmblemClass);
            summaryCondition.pickingMode = PickingMode.Ignore;
            Summary.Add(summaryCondition);
            Summary.Add(EdgePlaque.Text(summaryLocation, summaryState));
            summaryJourney = FellowshipArtwork.Image("journey");
            var values = EdgePlaque.Column("fellowship-status-values");
            values.Add(EdgePlaque.Value(summaryJourney, summaryProgress));
            values.Add(EdgePlaque.Value(FellowshipArtwork.Image("corruption"), summaryCorruption));
            Summary.Add(values);
            condition = FellowshipArtwork.Image("hidden");
            condition.name = "fellowship-panel-ring";
            condition.AddToClassList("fellowship-state-symbol");
            condition.pickingMode = PickingMode.Ignore;
            StateRow.Add(condition);
            StateRow.Add(state);
            StateRow.Add(location);
            Root.Add(journey.Root);
            Root.Add(mordor.Root);
            journey.Activated += () => MoveRequested?.Invoke();
            mordor.Activated += () => MoveRequested?.Invoke();
            journey.MarkerActivated += () => HideRequested?.Invoke();
            mordor.MarkerActivated += () => HideRequested?.Invoke();
            mordor.Root.RegisterCallback<ClickEvent>(e =>
            {
                if (!(e.target is VisualElement target && mordor.Controls(target)))
                    TrackFocusRequested?.Invoke();
            });
            Root.Add(corruption.Root);
            Root.Add(activity);
            guideRow.Add(FellowshipArtwork.Image("guide"));
            guideRow.Add(guide);
            Root.Add(guideRow);
        }

        public void Apply(FellowshipView fellowship, BoardPresentation data, bool finished = false)
        {
            var travel = FellowshipTrackState.Journey(fellowship);
            var damage = FellowshipTrackState.Corruption(fellowship);
            var hidden = fellowship.Hidden ? "Hidden" : "Revealed";
            Summary.style.display = DisplayStyle.Flex;
            summaryCondition.sprite = FellowshipArtwork.Sprite(travel.Symbol);
            summaryStateText = hidden;
            summaryState.text = hidden;
            summaryJourney.sprite = FellowshipArtwork.Sprite(fellowship.MordorStep.HasValue ? "mordor-gate" : "journey");
            summaryProgress.text = fellowship.MordorStep.HasValue ? travel.Value + "/5" : travel.Value.ToString();
            summaryCorruption.text = damage.Value.ToString();
            state.text = hidden;
            condition.sprite = FellowshipArtwork.Sprite(travel.Symbol);
            state.tooltip = fellowship.Hidden ? "The Fellowship is hidden." : "The Fellowship is revealed. Hide before moving again.";
            var regionName = data.Regions.TryGetValue(fellowship.RegionId, out var region) ? region.Name : fellowship.RegionId;
            location.text = fellowship.MordorStep.HasValue ? "Mordor" : regionName;
            summaryLocation.text = location.text;
            location.tooltip = fellowship.MordorStep.HasValue ? "Mordor track · Resolve each Hunt tile before advancing."
                : "Last known position · " + regionName + ". Hidden progress does not select a secret route.";
            inMordor = fellowship.MordorStep.HasValue;
            journey.Root.style.display = fellowship.MordorStep.HasValue ? DisplayStyle.None : DisplayStyle.Flex;
            mordor.Root.style.display = fellowship.MordorStep.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (fellowship.MordorStep.HasValue)
                mordor.Apply(travel);
            else
                journey.Apply(travel);
            corruption.Apply(damage);
            ApplyActivity(fellowship, finished);
            var version = FellowshipGuideText.PublicVersion(fellowship, fellowship.Guide);
            guide.text = "Guide · " + CharacterLibrary.Name(version);
            guide.tooltip = FellowshipGuideText.Ability(version);
        }

        private void ApplyActivity(FellowshipView fellowship, bool finished)
        {
            activity.style.display = fellowship.MordorStep.HasValue && !finished ? DisplayStyle.Flex : DisplayStyle.None;
            activity.text = fellowship.MordorAttemptedThisTurn switch
            {
                true => "✓ Move or hide attempted this turn",
                false => "Move or hide this turn · Otherwise +1 Corruption",
                _ => "Turn activity unknown"
            };
            activity.EnableInClassList("mordor-inactive", fellowship.MordorAttemptedThisTurn == false);
            activity.tooltip = fellowship.MordorAttemptedThisTurn.HasValue
                ? MordorTrackState.ActivityHint
                : "This server view does not report whether the Fellowship attempted to move or hide this turn.";
        }

        // UI-293: while hiding is legal with the chosen die, the marker on the visible track is the control; the plaque
        // keeps opening the panel.
        public void OfferHide(bool offered, string tooltip)
        {
            journey.OfferMarker(offered && !inMordor, tooltip);
            mordor.OfferMarker(offered && inMordor, tooltip);
        }

        public void OfferMove(bool offered, string tooltip)
        {
            journey.Offer(offered && !inMordor, inMordor ? "journey-track-move" : MoveName, tooltip);
            mordor.Offer(offered && inMordor, inMordor ? MoveName : "mordor-track-move", tooltip);
        }

        // The roster's Guide badge and tooltip already name the Guide and its ability; no second row then.
        public void Compact(bool route, bool hunt, bool rosterShowsGuide)
        {
            guideRow.style.display = route || hunt || rosterShowsGuide ? DisplayStyle.None : DisplayStyle.Flex;
            // Both public tracks remain readable while planning a route or resolving Hunt.
        }
    }
}
