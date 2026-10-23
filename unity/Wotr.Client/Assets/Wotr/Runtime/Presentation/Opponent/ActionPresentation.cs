using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303: one recorded action as a presentation source: who started it, the public card that drives it (if any), its
    // cost, and every cue released for it so far, in order. A continuation in a later revision adds cues to the same
    // action, so the source is introduced once (OPPONENT_ACTIONS_UX §6.1–§6.2).
    public sealed class ActionPresentation
    {
        private readonly List<PresentationCue> cues = new List<PresentationCue>();
        public string ActionId { get; }
        public Side? Initiator { get; internal set; }
        public string CardId { get; private set; } = "";
        public PresentationCue Cost { get; private set; }
        public IReadOnlyList<PresentationCue> Cues => cues;

        internal ActionPresentation(string actionId) => ActionId = actionId;

        internal void Add(PresentationCue cue)
        {
            cues.Add(cue);
            if (Cost == null && cue.Kind == PresentationCueKind.Cost)
                Cost = cue;
            if (CardId.Length == 0 && cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.Event)
                CardId = cue.Card.Id;
        }
    }
}
