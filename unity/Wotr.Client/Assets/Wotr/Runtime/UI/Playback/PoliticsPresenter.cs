using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-307 (OPPONENT_ACTIONS_UX §8 politics): a recorded political change of one nation. The Politics panel opens read-only
    // — never while it is live for the player's own decision, and not again once the player closed it during this
    // presentation; a panel the player had open stays open. The nation's token is traced; an activation lights its crest
    // first, then the token moves its recorded steps (eased, a jump under Reduced motion); reaching war adds the short
    // caption "At war". Values the record lacks come from the presented state.
    public sealed class PoliticsPresenter : ICuePresenter
    {
        public const double TraceSeconds = .35;
        public const double StepSeconds = .6;
        public const string CaptionName = "presentation-caption";
        public const string MovingClass = "presenting-politics";
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
        private bool Visible => Panel.Drawer.style.display == DisplayStyle.Flex;

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
            OpenPanel();
            return new Beat(this, change.Id, wasActive, steps, change.Active, change.StepsToWar, now);
        }

        // The presentation ended: a panel it opened closes again.
        public void Released()
        {
            if (opened && Visible)
                Panel.Close();
            opened = false;
            dismissed = false;
        }

        private void OpenPanel()
        {
            if (Visible || dismissed || Live)
                return;
            Panel.Open();
            opened = Visible;
        }

        private VisualElement Token(string id) => Panel.Drawer.Q<Button>("nation-status-" + id);

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
            private readonly double start;
            private readonly LayerRing ring;
            private Label caption;
            private int phase;

            public Beat(PoliticsPresenter owner, string id, bool wasActive, int fromSteps, bool active, int toSteps, double now)
            {
                this.owner = owner;
                this.id = id;
                this.active = active;
                this.fromSteps = fromSteps;
                this.toSteps = toSteps;
                activation = !wasActive && active;
                moves = fromSteps != toSteps;
                start = now;
                ring = new LayerRing(owner.layer, "presentation-politics-ring");
                Follow();
            }

            public bool Advance(double now)
            {
                Follow();
                var elapsed = now - start;
                if (phase == 0 && elapsed >= TraceSeconds)
                {
                    phase = 1;
                    if (activation)
                        owner.Stage(id, true, fromSteps);
                }
                var moveAt = TraceSeconds + (activation ? StepSeconds : 0);
                if (phase == 1 && elapsed >= moveAt)
                {
                    phase = 2;
                    owner.Panel.Drawer.AddToClassList(MovingClass);
                    owner.Stage(id, active, toSteps);
                    if (toSteps == 0 && moves)
                        Caption();
                }
                return phase == 2 && elapsed >= moveAt + StepSeconds * (moves ? 2 : 1) && End();
            }

            public void Finish() => End();

            private bool End()
            {
                ring.End();
                caption?.RemoveFromHierarchy();
                owner.Panel.Drawer.RemoveFromClassList(MovingClass);
                return true;
            }

            private void Follow()
            {
                ring.Follow(owner.Anchor(id));
                if (caption != null)
                    PlaceCaption();
            }

            // "At war" beside the war station of the nation's row.
            private void Caption()
            {
                caption = new Label("At war") { name = CaptionName, pickingMode = PickingMode.Ignore };
                caption.AddToClassList(CaptionName);
                owner.layer.Root.Add(caption);
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
