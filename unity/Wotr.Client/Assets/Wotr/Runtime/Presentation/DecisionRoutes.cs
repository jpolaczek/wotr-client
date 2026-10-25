using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-244: where the player's own decision is made — the object host that owns its choices — read from the
    // decision's kind and presentation only. The families follow the base coverage matrix
    // (wotr-server:docs/ui/interaction-inventory.json, base_coverage); UI-245–UI-251 move more presentations onto
    // objects by changing the host here and adding the gesture there. Window is the generic list, the fallback for a
    // choice no object owns yet.
    public enum DecisionHost
    {
        None,
        Map,
        RegionPanel,
        Reserve,
        Effects,
        Dice,
        Rings,
        Hand,
        Battle,
        Hunt,
        Fellowship,
        Characters,
        Console,
        Window
    }

    public static class DecisionRoutes
    {
        public static DecisionHost Of(PlayerView view)
        {
            var decision = view?.Decision;
            if (decision == null || view.DecisionOwner != view.You)
                return DecisionHost.None;
            if (view.Battle != null || view.EventLoss != null)
                return Battle(decision);
            if (view.Hunt != null)
                return DecisionHost.Hunt;
            return decision.Kind switch
            {
                DecisionKind.Action or DecisionKind.ActionSequence => DecisionHost.Map,
                // UI-294: a journey of companions outside the Fellowship is made on the map.
                DecisionKind.CompanionSeparation when CompanionJourneyDraft.OnMap(view) => DecisionHost.Map,
                DecisionKind.FellowshipPhase or DecisionKind.Guide or DecisionKind.FellowshipRoute or
                    DecisionKind.CompanionSeparation => DecisionHost.Fellowship,
                DecisionKind.AllocateHunt or DecisionKind.HuntDamage or DecisionKind.HuntReroll => DecisionHost.Hunt,
                DecisionKind.DiscardCards => DecisionHost.Hand,
                DecisionKind.ArmyEvent => DecisionHost.Map,
                DecisionKind.RemoveExcess or DecisionKind.Losses => DecisionHost.RegionPanel,
                DecisionKind.CombatCard or DecisionKind.BattleReroll or DecisionKind.BattleContinue or DecisionKind.BattleRetreat or
                    DecisionKind.BattleAdvance or DecisionKind.StrongholdDefense or DecisionKind.SiegeContinuation => DecisionHost.Battle,
                DecisionKind.EffectChoice or DecisionKind.CharacterEvent or DecisionKind.Reaction => Presented(decision.Presentation),
                _ => DecisionHost.Window
            };
        }

        // The host of a card's step, by the server's presentation of it.
        public static DecisionHost Presented(string presentation) => presentation switch
        {
            "Region" or "RegionOrShelter" => DecisionHost.Map,
            "ArmyFigures" or "ArmyFigure" => DecisionHost.RegionPanel,
            "ReserveFigures" or "RecruitmentPlan" => DecisionHost.Reserve,
            "TableCards" => DecisionHost.Effects,
            "Deck" => DecisionHost.Map,
            "ActionDie" or "DieAndResult" => DecisionHost.Dice,
            "ElvenRings" => DecisionHost.Rings,
            "CharacterCardDiscard" => DecisionHost.Hand,
            "FellowshipCompanions" or "Companion" or "CorruptionRemoval" or "SmeagolCorruption" or "InspectedCorruption" or
                "CompanionDestination" => DecisionHost.Fellowship,
            "CombatReplacement" or "CombatEffect" or "ConvertMisses" or "LeadershipAndMiss" or "NazgulOrMinion" or
                "LeaderOrCompanion" or "Minion" or "CharacterLeadership" or "NazgulLeadership" or "MinionLeadership" or
                "SacrificeAndHits" => DecisionHost.Battle,
            "LureCompanion" or "CaptainOrder" or "BalrogOrder" or "FellowshipAction" or "NazgulStrike" or
                "InspectedHand" => DecisionHost.Console,
            "Nation" => DecisionHost.Map,
            _ => DecisionHost.Window
        };

        private static DecisionHost Battle(DecisionView decision) =>
            decision.Kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent or DecisionKind.Reaction
                ? Presented(decision.Presentation) is DecisionHost.Window ? DecisionHost.Battle : Presented(decision.Presentation)
                : DecisionHost.Battle;
    }
}
