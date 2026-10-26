using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-305/UI-306: the production presenters of the opponent's actions on one mounted board, in beat order — the cost
    // die, the camera, the revealed card, the character's moment or ability source (UI-309), then the families (army
    // movement, attack, the battle window's rounds (UI-311), control, figure changes, politics, the Hunt and the
    // Fellowship, UI-310) — with the Skip target, Esc (only when nothing else owns it) and the pause while the player
    // reads a card opened during the presentation, and last the opponent's resources (Rings, dice, cards drawn, UI-312).
    // One staged board and one map trace serve every presenter of a presentation; none of them reads or marks a notice.
    public sealed class OpponentPresenters : IDisposable
    {
        private readonly BoardScreen board;
        private readonly PlaybackBinding binding;
        private readonly PresentationLayer layer;
        private readonly CostEmphasis cost;
        private readonly PresentationCamera camera;
        private readonly StagedBoard staged;
        private readonly PoliticsPresenter politics;
        private readonly CardRevealPresenter cards;
        private readonly CharacterPresenter characters;
        private readonly FellowshipReading fellowshipPanel;
        private readonly HuntPresenter hunt;
        private readonly FellowshipPresenter fellowship;
        private readonly BattlePresenter battles;
        private readonly ResourcePresenter resources;
        public MapObservedTrace Trace { get; }
        public SkipControl Skip { get; }
        public PresentationCamera Camera => camera;
        // UI-310: the open game's sounds, for the beats that sound their own fact (bound by the session).
        public PresentationSoundSink Sounds { get; } = new PresentationSoundSink();
        private PresentationPlayback Playback => binding.Playback;
        private bool reading;

        public OpponentPresenters(BoardScreen board, PlaybackBinding binding)
        {
            this.board = board;
            this.binding = binding;
            layer = new PresentationLayer(board);
            cost = new CostEmphasis(board, layer);
            camera = new PresentationCamera(board);
            staged = new StagedBoard(board);
            Trace = new MapObservedTrace(board.Map);
            Skip = new SkipControl(board, layer, Playback, SkipNow);
            // UI-333: before any scene, a finished battle's window steps aside for the next one; UI-342: so does the
            // Politics panel the presentation opened.
            Playback.Register(new BattleWindowRetirement(board, Playback));
            // UI-343: and so does the Hunt window once its Hunt is over and the next action begins.
            Playback.Register(new HuntWindowRetirement(board, Playback));
            politics = new PoliticsPresenter(board, staged, layer);
            Playback.Register(new PoliticsRetirement(politics, Playback));
            Playback.Register(cost);
            Playback.Register(camera);
            cards = new CardRevealPresenter(board, Playback, camera, layer);
            Playback.Register(cards);
            characters = new CharacterPresenter(board, staged, Trace, camera);
            Playback.Register(characters);
            Playback.Register(new ArmyMovementPresenter(board, staged, Trace));
            Playback.Register(new AttackPresenter(board, Trace));
            battles = new BattlePresenter(board, staged, Playback, layer, Sounds);
            Playback.Register(battles);
            Playback.Register(new ControlPresenter(board, staged, Trace));
            Playback.Register(new FigureChangePresenter(board, staged, Trace));
            Playback.Register(politics);
            fellowshipPanel = new FellowshipReading(board);
            hunt = new HuntPresenter(board, staged, layer, fellowshipPanel, Sounds);
            Playback.Register(hunt);
            fellowship = new FellowshipPresenter(board, staged, layer, fellowshipPanel, Sounds);
            Playback.Register(fellowship);
            resources = new ResourcePresenter(board, staged, Playback, layer);
            Playback.Register(resources);
            Playback.Paused = Reading;
            Playback.Released += Released;
            binding.Ticked += Skip.Refresh;
            board.Escape.SkipPresentation = Escape;
            board.Cards.Opening += CardOpening;
        }

        public void Dispose()
        {
            board.Cards.Opening -= CardOpening;
            board.Escape.SkipPresentation = null;
            binding.Ticked -= Skip.Refresh;
            Playback.Released -= Released;
            Playback.Paused = null;
            camera.Dispose();
            Trace.Root.RemoveFromHierarchy();
            layer.Root.RemoveFromHierarchy();
        }

        // OPPONENT_ACTIONS_UX §6.2: a card the player opens while a presentation plays holds it until the card closes.
        private void CardOpening()
        {
            if (Playback.Presenting)
                reading = true;
        }

        private bool Reading()
        {
            reading &= board.Cards.Pinned && board.Cards.Root.resolvedStyle.display != DisplayStyle.None;
            return reading || cards.Reading;
        }

        private bool Escape()
        {
            if (!Playback.Presenting)
                return false;
            SkipNow();
            return true;
        }

        // Skip shows the latest state: nothing that was skipped moves afterwards.
        private void SkipNow()
        {
            if (!Playback.Presenting)
                return;
            board.Map.Figures.Quiet();
            board.QuietTracks();
            board.Dissolve.Clear();
            board.HuntDrops.Clear();
            board.Map.EndEmerging();
            Playback.Skip();
        }

        private void Released()
        {
            cost.Clear();
            camera.Released();
            Trace.Clear();
            politics.Released();
            cards.Released();
            characters.Released();
            hunt.Released();
            battles.Released();
            resources.Released();
            fellowship.Released();
            fellowshipPanel.Released();
            staged.Reset();
            reading = false;
        }
    }
}
