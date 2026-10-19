using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-244: where the player's own decision is made — the object host that owns its choices — read from the
    // decision's kind and presentation only. The families follow the base coverage matrix
    // (wotr-server:docs/ui/interaction-inventory.json, base_coverage); UI-245–UI-251 move more presentations onto
    // objects by changing the host here and adding the gesture there. Window is the generic list, the fallback for a
    // choice no object owns yet.
    // UI-264: every expansion decision has a host. The dice pool takes the special-die pick and the Ruler discard; the
    // panels the expansion UI already uses (Resources, Sovereigns) stay hosts until the expansion tasks move their
    // decisions onto objects (UI-274, UI-276) — then they leave this list. UI-268–UI-270 moved Vilya to its ring, the
    // Council's setup to the Fellowship panel and its token choice to the Shadow pool.
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
        Resources,
        Sovereigns,
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
                DecisionKind.FellowshipPhase or DecisionKind.Guide or DecisionKind.FellowshipRoute or
                    DecisionKind.CompanionSeparation => DecisionHost.Fellowship,
                DecisionKind.AllocateHunt or DecisionKind.HuntDamage or DecisionKind.HuntReroll => DecisionHost.Hunt,
                DecisionKind.DiscardCards => DecisionHost.Hand,
                DecisionKind.ArmyEvent => DecisionHost.Map,
                DecisionKind.RemoveExcess or DecisionKind.Losses => DecisionHost.RegionPanel,
                DecisionKind.CombatCard or DecisionKind.BattleReroll or DecisionKind.BattleContinue or DecisionKind.BattleRetreat or
                    DecisionKind.BattleAdvance or DecisionKind.StrongholdDefense or DecisionKind.SiegeContinuation => DecisionHost.Battle,
                DecisionKind.EffectChoice or DecisionKind.CharacterEvent or DecisionKind.Reaction => Presented(decision.Presentation),
                _ => Expansion(decision.Kind)
            };
        }

        // UI-264: the hosts of the expansion decisions (Lords, Council, Kings).
        public static DecisionHost Expansion(DecisionKind kind) => kind switch
        {
            DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard => DecisionHost.Dice,
            DecisionKind.KeepActionDie => DecisionHost.Rings,
            DecisionKind.CouncilToken => DecisionHost.Dice,
            DecisionKind.Dismay => DecisionHost.Resources,
            DecisionKind.CouncilVersions => DecisionHost.Fellowship,
            DecisionKind.CouncilHomelands => DecisionHost.Fellowship,
            DecisionKind.CorruptionTile => DecisionHost.Sovereigns,
            _ => DecisionHost.Window
        };

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
                "InspectedHand" or "EffectTargets" or "SovereignCardBranch" => DecisionHost.Console,
            "Sovereign" => DecisionHost.Sovereigns,
            "Nation" => DecisionHost.Map,
            _ => DecisionHost.Window
        };

        private static DecisionHost Battle(DecisionView decision) =>
            decision.Kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent or DecisionKind.Reaction
                ? Presented(decision.Presentation) is DecisionHost.Window ? DecisionHost.Battle : Presented(decision.Presentation)
                : DecisionHost.Battle;
    }
}
