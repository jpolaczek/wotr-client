using Wotr.Contracts;

namespace Wotr.Client
{
    // A reaction chosen on its source object: the draft responds with that reaction's id.
    public static class ReactionDraft
    {
        public static void Choose(TurnDraft draft, string id)
        {
            if (draft.Kind != CommandKind.RespondEffect)
                draft.Choose(CommandKind.RespondEffect);
            draft.Change(p => p.ReactionId = id);
        }
    }
}
