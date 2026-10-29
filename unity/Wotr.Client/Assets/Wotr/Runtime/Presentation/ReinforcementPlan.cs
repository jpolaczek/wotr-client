using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: a reinforcement planned by a card or an ability, not yet sent.
    public sealed class PlannedReinforcement
    {
        public string UnitId = "";
        public string Nation = "";
        public UnitKind Kind;
        // UI-271: a moved character's definition, so its ghost shows its own figure; "" for a unit.
        public string CharacterId = "";
        public string RegionId = "";
        public string RegionName = "";
    }

    // UI-131: what the Reinforcements panel shows for a recruitment planned by a card or an ability. The plan is
    // committed with one ✓, like the tray's confirmation; the rules stay in the source's own draft logic.
    public sealed class ReinforcementPlan
    {
        // Element names keep the source's prefix ("event", "effect", "character"), as the tray palettes did.
        public string Prefix = "";
        public string Title = "";
        // How a token is used with this source, for its tooltip.
        public string Hint = "Click, then click a highlighted region · Or drag to it";
        public int Slots;
        public bool Optional;
        public bool Ready;
        // UI-392 (owner): an ability's recruitment starts on the map: the panel opens when a lit settlement is clicked.
        public bool RegionFirst;
        public List<ReinforcementRow> Rows = new List<ReinforcementRow>();
        public List<PlannedReinforcement> Placements = new List<PlannedReinforcement>();
    }

    // One kind of recruitment that the panel can show: it builds the plan and edits the draft with the source's
    // existing rules.
    public interface IReinforcementSource
    {
        bool Owns(PlayerView view, TurnDraft draft);
        ReinforcementPlan Build(PlayerView view, TurnDraft draft, BoardPresentation data);
        bool Choose(TurnDraft draft, PlayerView view, string id);
        bool Remove(TurnDraft draft, PlayerView view, string id);
    }
}
