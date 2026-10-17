using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-242: hints never say "components"; they name what the player clicks for the step: dice, armies, units,
    // characters, cards, nations, regions or Hunt tiles.
    public static class DecisionObjects
    {
        public static string Prompt(PlayerView view, TurnDraft draft) => "Select the highlighted " + Objects(view, draft);

        public static string Confirm(PlayerView view, TurnDraft draft) => Prompt(view, draft) + ", then confirm.";

        public static string Objects(PlayerView view, TurnDraft draft) => draft.Kind switch
        {
            CommandKind.MoveArmies or CommandKind.AttackArmy or CommandKind.FinishArmyEvent => "armies and regions",
            CommandKind.Recruit => "reinforcements and settlements",
            CommandKind.RemoveExcess or CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses => "units",
            CommandKind.RerollBattle or CommandKind.RerollHunt or CommandKind.UseRing or CommandKind.DiscardDie
                or CommandKind.ChooseSpecialDie or CommandKind.DiscardRulerDie => "dice",
            CommandKind.PlayEvent or CommandKind.CommitCombatCard or CommandKind.DiscardCards => "cards",
            CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or CommandKind.SeparateCompanions or CommandKind.SelectGuide
                or CommandKind.EnterCharacter or CommandKind.UseCharacterAbility or CommandKind.ChooseCompanionVersions => "characters",
            CommandKind.AdvancePolitics => "nation",
            CommandKind.DeclareFellowship or CommandKind.RevealFellowship or CommandKind.ChooseCouncilHomelands => "regions",
            CommandKind.ChooseEffect or CommandKind.RespondEffect => Presented(view.Decision?.Presentation),
            _ => "choices"
        };

        // What a refused drop names: the object the player tried to use.
        public static string Unusable(DraftToken source) => (source?.Object switch
        {
            DraftObject.Die => "That die",
            DraftObject.Ring => "That Ring",
            DraftObject.Card => "That card",
            DraftObject.Army => "That army",
            DraftObject.Figure or DraftObject.Reserve => "That unit",
            DraftObject.Character or DraftObject.FellowshipPortrait or DraftObject.CouncilPortrait => "That character",
            DraftObject.RulerToken => "That token",
            DraftObject.Nation => "That nation",
            DraftObject.HuntTile => "That Hunt tile",
            _ => "That"
        }) + " cannot be used here.";

        private static string Presented(string presentation) => presentation switch
        {
            "Region" => "regions",
            "ActionDie" or "DieAndResult" => "dice",
            "Card" or "Cards" => "cards",
            "FellowshipCompanions" => "characters",
            _ => "choices"
        };
    }
}
