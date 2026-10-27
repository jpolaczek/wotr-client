using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-278: an effect choice made on a board object (a nation token, a portrait, a Reserve figure) starts the choice when
    // the draft has not, then takes the object's id.
    public static class EffectChoiceSelection
    {
        public static void Choose(PlayerView view, TurnDraft draft, ActionOption option, string id)
        {
            if (draft.Kind != CommandKind.ChooseEffect)
                DraftGesture.Choose(draft, view, CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, id);
        }
    }
}
