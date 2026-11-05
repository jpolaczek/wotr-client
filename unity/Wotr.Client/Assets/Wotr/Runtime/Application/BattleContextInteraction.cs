using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleContextInteraction
    {
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;

        public BattleContextInteraction(Action<Action, bool> edit, Func<bool> busy)
        {
            this.edit = edit;
            this.busy = busy;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
        }

        public bool Target(string region)
        {
            if (busy() || view?.Decision == null)
                return false;
            var option = DraftOptions.For(view, draft);
            if (draft.Kind == CommandKind.RetreatBattle && option?.Targets.Contains(region) == true)
            {
                edit(() => draft.Change(p => p.RegionId = region), true);
                return true;
            }
            if (draft.Kind == CommandKind.AdvanceBattle && region == view.Battle?.RegionId && option != null && draft.Ready(option))
            {
                edit(() => { }, true);
                return true;
            }
            return Shelter(region, option);
        }

        // UI-432 (owner's rule, UI-430): the battle region's click withdraws into siege for Scouts, as its old button did.
        private bool Shelter(string region, ActionOption option)
        {
            if (option == null || region != ScoutsShelter.Region(view, draft))
                return false;
            edit(() => CardDraft.Choose(draft, option, ScoutsShelter.Shelter), true);
            return true;
        }
    }
}
