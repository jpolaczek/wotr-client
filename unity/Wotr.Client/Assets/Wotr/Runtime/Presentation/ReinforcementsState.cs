using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-130/UI-131: everything the Reinforcements tab shows for one view: a Muster action, a recruitment
    // planned by a card or an ability, or (UI-179) the reserves alone.
    public sealed class ReinforcementsState
    {
        public static readonly ReinforcementsState Closed = new ReinforcementsState();

        public bool Open;
        public bool Interactive;
        // A recruitment sequence of this player is open, so ✓ can end the action early.
        public bool Finishable;
        // A card or ability plan: ✓ commits every planned figure at once.
        public bool Planning;
        public bool Confirmable;
        public bool Optional;
        // UI-181: the tab and its header read Reserve.
        public string Title = "Reserve";
        public string TokenPrefix = "reserve-token-";
        public string TokenHint = "Click, then click a highlighted settlement · With one legal settlement the click recruits there · Or drag to it";
        public string PlacementPrefix = "";
        public Side Side = Side.FreePeoples;
        public string Selected = "";
        public (int Spent, int Total) Points = (0, 2);
        public IReadOnlyList<ReinforcementRow> Rows = new List<ReinforcementRow>();
        public IReadOnlyList<PlannedReinforcement> Placements = new List<PlannedReinforcement>();

        // UI-179: outside a recruitment the tab lists the player's reserves, every row plain and inert.
        public static ReinforcementsState Browse(PlayerView view) =>
            new ReinforcementsState { Side = view.You, Rows = ReinforcementRows.For(view, null) };

        public static ReinforcementsState For(PlayerView view, ActionOption recruit, string selected, bool interactive) =>
            For(view, recruit, selected, interactive, RecruitFocus.None);

        // UI-412 (owner): a clicked settlement lights only its own recruits in the Muster's Reserve and names it in the header.
        public static ReinforcementsState For(PlayerView view, ActionOption recruit, string selected, bool interactive, RecruitFocus focus) =>
            new ReinforcementsState
            {
                Open = true,
                Interactive = interactive,
                Title = focus.Active ? "Reserve · " + focus.Name : "Reserve",
                Finishable = view.Decision?.Kind == DecisionKind.ActionSequence &&
                    view.ActionSequence is { Kind: CommandKind.Recruit } sequence && sequence.Owner == view.You,
                Side = view.You,
                Selected = selected,
                Points = ReinforcementRows.Budget(view),
                Rows = ReinforcementRows.For(view, recruit, focus)
            };

        public static ReinforcementsState For(PlayerView view, ReinforcementPlan plan, string selected, bool interactive) =>
            new ReinforcementsState
            {
                Open = true,
                Interactive = interactive,
                Planning = true,
                Confirmable = plan.Ready,
                Optional = plan.Optional,
                Title = plan.Title.Length > 0 ? "Reserve · " + plan.Title : "Reserve",
                TokenPrefix = plan.Prefix + "-reserve-",
                TokenHint = plan.Hint,
                PlacementPrefix = plan.Prefix + "-placement-",
                Side = view.You,
                Selected = selected,
                Points = (plan.Placements.Count, System.Math.Max(plan.Slots, plan.Placements.Count)),
                Rows = plan.Rows,
                Placements = plan.Placements
            };
    }
}
