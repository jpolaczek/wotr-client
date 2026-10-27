using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-271: the Chief of the Ringwraiths' reactions on their objects: Spear of Terror at the Shadow's Character deck
    // (its click draws the card), He Sees, He Knows at the Chief's figure (its click moves him to the Fellowship).
    public static class ChiefReactionObjects
    {
        public const string Spear = "lords.chief.spear-of-terror";
        public const string Follow = "lords.chief.he-sees-he-knows";
        public const string SpearOffer = "Click to draw a Character card · Spear of Terror · It replaces any Palantír draw for this Event";
        public const string FollowOffer = "Click to move the Chief of the Ringwraiths to the Fellowship · He Sees, He Knows";
        private const string Chief = "lords.character.witch-king";

        public static bool OnObject(string reaction) => reaction is Spear or Follow;

        public static string DeckReaction(PlayerView view, string deck) => deck == "character" && Offered(view, Spear) ? Spear : "";

        // The Chief's figure while He Sees, He Knows is offered, or "".
        public static string FollowFigure(PlayerView view) => Offered(view, Follow)
            ? view.Characters.FirstOrDefault(c => c.Side == view.You && c.DefinitionId == Chief)?.Id ?? "" : "";

        private static bool Offered(PlayerView view, string reaction) =>
            view?.Decision?.Kind == DecisionKind.Reaction && view.DecisionOwner == view.You &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.RespondEffect && o.EffectChoices.Any(c => c.Id == reaction));
    }
}
