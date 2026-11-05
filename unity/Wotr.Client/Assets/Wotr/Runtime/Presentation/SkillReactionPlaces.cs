namespace Wotr.Client
{
    // UI-441: where a character's reaction offered now is used — on the object that offers it (UI-247, UI-271, UI-360,
    // UI-431) — for the tooltip of the character it lights (UsableSkills.Line).
    public static class SkillReactionPlaces
    {
        public const string Portrait = "Click the glowing portrait in the battle window";
        public const string HuntResponse = "Click the glowing response in the Hunt window";
        public const string GuidePortrait = "Click the glowing Guide portrait in the Fellowship panel";
        public const string Deck = "Click the glowing deck";
        public const string Ring = "Click the glowing Ring in the console";
        public const string Figure = "Click the glowing figure on the map";
        public const string Decision = "Use it where the decision offers it";

        public static string For(Wotr.Contracts.PlayerView view, string reaction)
        {
            if (BattleReactions.OnPortrait(view, reaction))
                return Portrait;
            if (HuntGuideResponses.InRow(view, reaction))
                return HuntResponse;
            if (FellowshipActions.ReactionOnRoster(view, reaction))
                return GuidePortrait;
            if (ReactionDecks.OnObject(view, reaction) || reaction == ChiefReactionObjects.Spear)
                return Deck;
            if (NamedRings.OnSocket(view, reaction))
                return Ring;
            return reaction == ChiefReactionObjects.Follow ? Figure : Decision;
        }
    }
}
