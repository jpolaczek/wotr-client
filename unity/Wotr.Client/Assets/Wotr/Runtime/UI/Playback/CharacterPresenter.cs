using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-309 (OPPONENT_ACTIONS_UX §8 characters; storyboard B): who a character presentation is about.
    // - A character entering, transforming, leaving the Fellowship or removed from play gets a short larger moment — its
    //   portrait and title above its force — before the figure change lands (FigureChangePresenter). The moment stays
    //   beside the figure until the next cue. A transformation shows the new version once: one identity, never two.
    // - An ability's source — a reaction's character, or a die ability the public catalog gives to one character — is
    //   highlighted on its existing portrait (the battle window's or the Fellowship roster's). On the map the same moment
    //   stands at its figure (the camera frames it, never after the player's own click) and stays there as the action's
    //   compact source while its effects play through their own presenters.
    // Nothing here takes the pointer or adds a control; the inspector stays reachable through the figure itself.
    public sealed class CharacterPresenter : ICuePresenter
    {
        public const double MomentSeconds = 1;

        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly MapObservedTrace trace;
        private readonly PresentationCamera camera;

        private MapObservedTrace.TraceSet moment;
        private MapObservedTrace.TraceSet source;
        private readonly ObjectGlow sourcePortrait = new ObjectGlow();
        private string sourceAction = "";

        public CharacterPresenter(BoardScreen board, StagedBoard staged, MapObservedTrace trace, PresentationCamera camera)
        {
            this.board = board;
            this.staged = staged;
            this.trace = trace;
            this.camera = camera;
        }

        public static bool Shows(PresentationCue cue) => cue.BattleId.Length == 0 &&
            cue.Kind is PresentationCueKind.Entry or PresentationCueKind.Transformation or PresentationCueKind.Relocation or
                PresentationCueKind.Removal &&
            cue.Figures.Any(change => CharacterMarks.Named(change.After ?? change.Before));

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            moment?.End();
            moment = null;
            if (cue.ActionId != sourceAction)
                EndSource();
            if (cue.Kind == PresentationCueKind.Cost && cue.Cost != null)
                return Source(cue, now);
            return Shows(cue) ? Moment(cue, now) : null;
        }

        // The presentation ended; its map traces are cleared with the rest.
        public void Released()
        {
            moment = null;
            EndSource();
        }

        private IPresentationBeat Moment(PresentationCue cue, double now)
        {
            var region = cue.Regions.FirstOrDefault();
            if (region == null || !board.Map.Data.Shapes.ContainsKey(region))
                return null;
            var removed = cue.Kind == PresentationCueKind.Removal;
            // A removal names the character that left; every other change the version that now stands.
            var figures = cue.Figures.Select(change => removed ? change.Before ?? change.After : change.After ?? change.Before)
                .Where(CharacterMarks.Named).ToList();
            var marks = figures.Select(figure => CharacterMarks.Moment(figure, removed));
            moment = trace.Begin().Portraits(region, figures[0].Side, figures.Any(f => f.Garrison), marks, camera.SafeArea());
            return new TimedBeat(now, MomentSeconds);
        }

        private IPresentationBeat Source(PresentationCue cue, double now)
        {
            var identity = SourceIdentity(cue.Cost);
            if (identity == null)
                return null;
            var owner = cue.Cost.Owner ?? cue.Actor;
            var figure = staged.View.Characters.FirstOrDefault(c => (owner == null || c.Side == owner) &&
                (c.DefinitionId == cue.Cost.Id || CharacterLibrary.Identity(c.DefinitionId) == identity));
            sourceAction = cue.ActionId;
            var portrait = ShownPortrait(figure, identity);
            if (portrait != null)
            {
                sourcePortrait.Follow(portrait);
                return new TimedBeat(now, MomentSeconds);
            }
            if (figure == null || !board.Map.Data.Shapes.ContainsKey(figure.Location))
                return null;
            var framing = cue.OwnCommand ? null : camera.Frame(new[] { figure.Location }, now);
            return new SourceBeat(this, figure, framing);
        }

        // A reaction records its character; a die ability only its ability id, which names a character when the public
        // catalog lists it for one identity (CharacterLibrary.AbilityHolder). Anything else names no one.
        private static string SourceIdentity(ActionCost cost) => cost.Kind switch
        {
            ActionCostKind.Ability when CharacterLibrary.Find(cost.Id) != null => CharacterLibrary.Identity(cost.Id),
            ActionCostKind.Die when cost.AbilityId.Length > 0 => CharacterLibrary.AbilityHolder(cost.AbilityId),
            _ => null
        };

        // The character's existing portrait where the player sees it now: a figure on the board in the battle window, a
        // Companion still in the Fellowship in its roster (the opponent's Companions are named by identity there).
        private VisualElement ShownPortrait(CharacterView figure, string identity)
        {
            var portrait = board.Root.Q(figure == null ? "fellowship-portrait-" + identity : "portrait-" + figure.Id);
            return PresentationTargets.Shown(portrait) ? portrait : null;
        }

        private void Mark(CharacterView figure) => source = trace.Begin().Force(figure.Location, figure.Side, figure.Garrison)
            .Portraits(figure.Location, figure.Side, figure.Garrison, new[] { CharacterMarks.Moment(figure) }, camera.SafeArea());

        private void EndSource()
        {
            source?.End();
            source = null;
            sourcePortrait.End();
            sourceAction = "";
        }

        // The camera frames the source first (when it needs to and may); then its moment holds.
        private sealed class SourceBeat : IPresentationBeat
        {
            private readonly CharacterPresenter owner;
            private readonly CharacterView figure;
            private readonly IPresentationBeat framing;
            private double end = -1;

            public SourceBeat(CharacterPresenter owner, CharacterView figure, IPresentationBeat framing)
            {
                this.owner = owner;
                this.figure = figure;
                this.framing = framing;
            }

            public bool Advance(double now)
            {
                if (end < 0 && framing != null && !framing.Advance(now))
                    return false;
                if (end < 0)
                {
                    owner.Mark(figure);
                    end = now + MomentSeconds;
                }
                return now >= end;
            }

            public void Finish() => framing?.Finish();
        }
    }
}
