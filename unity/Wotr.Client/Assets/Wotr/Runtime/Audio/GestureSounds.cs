using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // AUDIO-04: a completed gesture (PressFeedback: released inside its enabled control, or Enter/Space) sounds once,
    // after its control has acted, so the sound can follow what it did:
    // - it sent a command — silence; the result sounds when it comes (AUDIO-05/06);
    // - it opened, changed or closed a card look — the paper rustle instead of the click;
    // - the board ignored it as the quick second click after a step (UI-142) — silence;
    // - anything else — the wooden click.
    // Hover, focus, a cancelled or dragged press and a disabled control never complete, so they stay silent.
    public sealed class GestureSounds : IDisposable
    {
        private readonly PressFeedback presses;
        private readonly SoundModule sounds;
        private readonly VisualElement root;
        private int commands;
        private string look = "";

        public GestureSounds(PressFeedback presses, SoundModule sounds, VisualElement root)
        {
            this.presses = presses;
            this.sounds = sounds;
            this.root = root;
            presses.Began += Began;
            presses.Completed += Completed;
        }

        // What the game looked like before the control could act.
        private void Began(VisualElement control)
        {
            commands = sounds.CommandsSent;
            look = sounds.CardLook?.Invoke() ?? "";
        }

        private void Completed(VisualElement control)
        {
            var ignored = sounds.GestureIgnored?.Invoke() == true;
            var before = (commands, look);
            // The next scheduler tick sees what the control's own click did.
            root.schedule.Execute(() => Sound(before.commands, before.look, ignored));
        }

        private void Sound(int commands, string look, bool ignored)
        {
            if (ignored || sounds.CommandsSent != commands)
                return;
            var cue = (sounds.CardLook?.Invoke() ?? "") != look ? SoundCue.CardInspect : SoundCue.UiClick;
            sounds.Request(new SoundRequest(cue));
        }

        public void Dispose()
        {
            presses.Began -= Began;
            presses.Completed -= Completed;
        }
    }
}
