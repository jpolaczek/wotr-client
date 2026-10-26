using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-307 (OPPONENT_ACTIONS_UX §8 politics): a recorded political change of one nation. The Politics panel opens read-only
    // — never while it is live for the player's own decision, and not again once the player closed it during this
    // presentation; a panel the player had open stays open. UI-315 (owner review): at the shared pace — a panel the beat
    // opened settles first; then the nation's row and token glow before anything changes; an activation lights its crest; the
    // token slides its recorded steps (eased, a jump under Reduced motion) and stays lit at its new station; reaching war
    // adds the short caption "At war". Values the record lacks come from the presented state.
    public sealed class PoliticsPresenter : ICuePresenter
    {
        public const string CaptionName = "presentation-caption";
        public const string MovingClass = "presenting-politics";
        public const string FlareClass = "presentation-flare";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationLayer layer;
        private bool opened;
        private bool dismissed;

        public PoliticsPresenter(BoardScreen board, StagedBoard staged, PresentationLayer layer)
        {
            this.board = board;
            this.staged = staged;
            this.layer = layer;
        }

        private PoliticalPanel Panel => board.Nations;
        private bool Visible => !Appearance.Closed(Panel.Drawer);

        // The player's own decision drives the panel: its tab or a station offers a legal target.
        private bool Live => Panel.Root.ClassListContains("legal-target") || Panel.Drawer.Q(className: "legal-target") != null;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var change = cue.Nation;
            var nation = change == null ? null : staged.View.Nations.FirstOrDefault(n => n.Id == change.Id);
            if (cue.Kind != PresentationCueKind.Politics || nation == null)
                return null;
            var wasActive = change.WasActive ?? nation.Active;
            var steps = change.PreviousStepsToWar ?? nation.StepsToWar;
            if (wasActive == change.Active && steps == change.StepsToWar)
                return null;
            var settle = OpenPanel();
            return new Beat(this, change.Id, wasActive, steps, change.Active, change.StepsToWar, settle, now);
        }

        // UI-342: the next scene begins — a panel the presentation opened closes, unless that scene's step changes politics
        // as well (its die's cost comes first), so the panel does not close and open again within one step.
        public void Retire(PresentationCue cue, IEnumerable<PresentationCue> upcoming)
        {
            if (!opened || cue.Kind == PresentationCueKind.Politics ||
                upcoming.Any(next => next.Kind == PresentationCueKind.Politics && next.StepId == cue.StepId))
                return;
            if (Visible)
                Panel.Close();
            opened = false;
        }

        // The presentation ended: a panel it opened closes again.
        public void Released()
        {
            if (opened && Visible)
                Panel.Close();
            opened = false;
            dismissed = false;
        }

        // True when the panel opened now, so the beat lets it settle before anything in it changes.
        private bool OpenPanel()
        {
            if (Visible || dismissed || Live)
                return false;
            Panel.Open();
            opened = Visible;
            return opened;
        }

        private VisualElement Token(string id) => Panel.Drawer.Q<Button>("nation-status-" + id);

        // UI-315: the chosen nation's whole track row, while its row shows.
        private VisualElement Row(string id)
        {
            var track = Panel.Drawer.Q("nation-track-" + id);
            return Visible && PresentationTargets.Shown(track) ? track : null;
        }

        // The token when its row shows, else the Politics tab (opponent rows hidden, or the panel closed).
        private VisualElement Anchor(string id)
        {
            if (opened && !Visible)
                dismissed = true;
            var token = Token(id);
            return Visible && token != null && token.worldBound.width > 0 ? token : Panel.Root;
        }

        private void Stage(string id, bool active, int steps)
        {
            staged.Nation(id, active, steps);
            staged.ShowNations();
        }

        private sealed class Beat : IPresentationBeat
        {
            private readonly PoliticsPresenter owner;
            private readonly string id;
            private readonly bool activation;
            private readonly bool moves;
            private readonly bool active;
            private readonly int fromSteps;
            private readonly int toSteps;
            private readonly double markAt;
            private readonly double activateAt;
            private readonly double moveAt;
            private readonly double endAt;
            private readonly ObjectGlow glow = new ObjectGlow();
            private readonly ObjectGlow row = new ObjectGlow();
            private Label caption;
            private bool flared;
            private int phase;

            public Beat(PoliticsPresenter owner, string id, bool wasActive, int fromSteps, bool active, int toSteps, bool settle, double now)
            {
                this.owner = owner;
                this.id = id;
                this.active = active;
                this.fromSteps = fromSteps;
                this.toSteps = toSteps;
                activation = !wasActive && active;
                moves = fromSteps != toSteps;
                markAt = now + (settle ? PresentationPace.PanelSettleSeconds : 0);
                activateAt = markAt + PresentationPace.MarkSeconds;
                moveAt = activateAt + (activation ? PresentationPace.ChangeSeconds : 0);
                endAt = moveAt + (moves ? PresentationPace.ChangeSeconds : 0) + PresentationPace.HoldSeconds;
                Advance(now);
            }

            // UI-319: the settled panel, then the token flares (its glow and a swell), then it settles back as its crest
            // crossfades or it slides, then it holds and fades out with its row.
            public bool Advance(double now)
            {
                if (now >= markAt)
                    Follow();
                if (!flared && now >= markAt)
                {
                    flared = true;
                    owner.Panel.Drawer.AddToClassList(MovingClass);
                    Flare(true);
                }
                if (phase == 0 && now >= activateAt)
                {
                    phase = 1;
                    Flare(false);
                    if (activation)
                        owner.Stage(id, true, fromSteps);
                }
                if (phase == 1 && now >= moveAt)
                {
                    phase = 2;
                    owner.Stage(id, active, toSteps);
                    if (toSteps == 0 && moves)
                        Caption();
                }
                return phase == 2 && now >= endAt && End();
            }

            public void Finish() => End();

            private bool End()
            {
                Flare(false);
                glow.End();
                row.End();
                caption?.RemoveFromHierarchy();
                owner.Panel.Drawer.RemoveFromClassList(MovingClass);
                return true;
            }

            private void Flare(bool on) => owner.Token(id)?.EnableInClassList(FlareClass, on);

            private void Follow()
            {
                glow.Follow(owner.Anchor(id));
                row.Follow(owner.Row(id));
                if (caption != null)
                    PlaceCaption();
            }

            // "At war" beside the war station of the nation's row.
            private void Caption()
            {
                caption = new Label("At war") { name = CaptionName, pickingMode = PickingMode.Ignore };
                caption.AddToClassList(CaptionName);
                owner.layer.Root.Add(caption);
                Appearance.Arrive(caption);
                PlaceCaption();
            }

            private void PlaceCaption()
            {
                var station = owner.Panel.Drawer.Q("political-slot-" + id + "-0");
                var anchor = station != null && station.worldBound.width > 0 && owner.Visible ? station : owner.Panel.Root;
                var bounds = owner.layer.Local(anchor.worldBound);
                caption.style.left = bounds.xMax + 6;
                caption.style.top = bounds.y + bounds.height / 2 - 10;
            }
        }
    }
}
