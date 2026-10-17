using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUDIO-04–AUDIO-07: the sounds of one open game.
    // - It tells the application's sound module what the board's gestures did (commands sent, the card look, ignored
    //   clicks) and starts the game with nothing heard; closing the game drops whatever of its presentation was due.
    // - AUDIO-05: one resolution is a new projection together with the accepted answer to an own command: its most
    //   important cue sounds, the plain acceptance only when nothing more particular happened. A rejected command
    //   sounds its failure once. The first view, a catch-up after a reconnect and a recovered receipt only set the
    //   point of reference; nothing old is played.
    public sealed class GameSounds : IDisposable
    {
        private readonly SoundModule sounds;
        private readonly Func<string> cardLook;
        private readonly ICommandEvents turns;
        private PlayerView seen;
        private bool catchingUp = true;
        private (string Id, long Revision)? accepted;
        private long lastResolved = -1;
        private bool lastResolvedSounded;
        private int played;
        // REPLAY-06: an observer or a replay hears the facts but no alert of its own decision and no personal result.
        private readonly bool personal;

        public GameSounds(SoundModule sounds, BoardScreen board, ICommandEvents turns, bool personal = true)
            : this(sounds, () => board.Cards.Root.style.display.value + "/" + board.Cards.CardId, turns, personal)
        {
        }

        // The card look as the player sees it — shown or not, and which card — comes from the board.
        public GameSounds(SoundModule sounds, Func<string> cardLook, ICommandEvents turns, bool personal = true)
        {
            this.personal = personal;
            this.sounds = sounds;
            this.cardLook = cardLook;
            this.turns = turns;
            sounds.Policy.BeginGame();
            sounds.CardLook = CardLook;
            sounds.GestureIgnored = () => turns.GestureSuppressed;
            turns.CommandSending += sounds.CommandSent;
            turns.CommandAnswered += Answered;
            sounds.Policy.Played += Count;
        }

        private string CardLook() => cardLook();

        private void Count(SoundCue cue) => played++;

        // A new projection of this game, after the board applied it.
        public void Update(PlayerView view)
        {
            var before = seen;
            seen = view;
            if (before == null || catchingUp || view.Revision <= before.Revision)
            {
                catchingUp = false;
                return;
            }
            var requests = SoundFacts.Between(before, view);
            if (!personal)
                requests.RemoveAll(r => r.Cue is SoundCue.DecisionRequired or SoundCue.Victory or SoundCue.Defeat);
            if (accepted.HasValue && view.Revision >= accepted.Value.Revision)
            {
                requests.Add(new SoundRequest(SoundCue.ActionAccepted, "accepted/" + accepted.Value.Id));
                accepted = null;
            }
            // AUDIO-06: a newer state ends what was left of an earlier pair; a new pair is this resolution's sound.
            sounds.Policy.CancelSequence();
            if (TrackSequence.Both(before.Fellowship, view.Fellowship))
            {
                Pair(view, requests);
                return;
            }
            Resolve(view.Revision, requests);
        }

        // REPLAY-06: a jump, a change of side or a first view: the next projection only sets the point of reference, and
        // whatever of an earlier pair was still due goes.
        public void Rebase()
        {
            catchingUp = true;
            sounds.Policy.CancelSequence();
        }

        // Anything but a live connection means the next projection may jump ahead: it is a new point of reference.
        public void SetConnection(string state)
        {
            if (state != "Connected")
                catchingUp = true;
        }

        private void Answered(GameCommand command, CommandReceipt receipt, bool recovered)
        {
            if (recovered)
                return;
            if (!receipt.Accepted)
            {
                sounds.Request(new SoundRequest(SoundCue.ActionFailed, "failed/" + command.CommandId));
                return;
            }
            // The projection usually follows the answer; when it came first, the acceptance sounds only if that
            // resolution had nothing to play.
            if (seen != null && seen.Revision >= receipt.Revision && lastResolved >= receipt.Revision)
            {
                if (!lastResolvedSounded)
                    sounds.Request(new SoundRequest(SoundCue.ActionAccepted, "accepted/" + command.CommandId));
                return;
            }
            accepted = (command.CommandId, receipt.Revision);
        }

        // Both tracks in their order; the click, acceptance, cards and dice of the same resolution add nothing, and one
        // important cue of it may follow the pair (AUDIO-07).
        private void Pair(PlayerView view, List<SoundRequest> requests)
        {
            sounds.Policy.Sequence(TrackSequence.Sounds("tracks/" + view.Revision, view.Fellowship, Trailing(requests)));
            lastResolved = view.Revision;
            lastResolvedSounded = true;
        }

        private static SoundRequest? Trailing(List<SoundRequest> requests)
        {
            SoundRequest? best = null;
            foreach (var request in requests)
                if ((request.Cue is SoundCue.DecisionRequired or SoundCue.Victory or SoundCue.Defeat) &&
                    (best == null || SoundCues.Priority(request.Cue) > SoundCues.Priority(best.Value.Cue)))
                    best = request;
            return best;
        }

        private void Resolve(long revision, IReadOnlyCollection<SoundRequest> requests)
        {
            var before = played;
            if (requests.Count > 0)
                sounds.Policy.Resolve(requests);
            lastResolved = revision;
            lastResolvedSounded = played != before;
        }

        public void Dispose()
        {
            turns.CommandSending -= sounds.CommandSent;
            turns.CommandAnswered -= Answered;
            sounds.Policy.Played -= Count;
            if (sounds.CardLook == CardLook)
                sounds.CardLook = null;
            sounds.GestureIgnored = null;
            sounds.Policy.CancelSequence();
        }
    }
}
