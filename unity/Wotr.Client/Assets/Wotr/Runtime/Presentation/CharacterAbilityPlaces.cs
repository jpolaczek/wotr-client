using System.Collections.Generic;
using System.Linq;

namespace Wotr.Client
{
    // UI-271: a character ability used on its object instead of a button in the region panel: the Balrog's activation
    // and deactivation on his glowing figure, Gothmog's reinforcement in the Reserve panel.
    public static class CharacterAbilityPlaces
    {
        public const string ActivateBalrog = "activate-balrog";
        public const string DeactivateBalrog = "deactivate-balrog";
        public const string GothmogRecruit = "gothmog-recruit";
        public const string GothmogUpgrade = "gothmog-upgrade";

        public static bool OnFigure(ComponentAction item) => item.Character?.AbilityId is ActivateBalrog or DeactivateBalrog;
        public static bool InReserve(ComponentAction item) => item.Character?.AbilityId is GothmogRecruit or GothmogUpgrade;
        public static bool OwnPlace(ComponentAction item) => OnFigure(item) || InReserve(item);

        // The ability a click on this figure uses with the selected die.
        public static ComponentAction Figure(IEnumerable<ComponentAction> components, string figure) =>
            components.FirstOrDefault(item => item.Enabled && OnFigure(item) && item.OwnerId == figure);

        public static string FigureOffer(ComponentAction item)
        {
            if (item.Character.AbilityId == ActivateBalrog)
                return "Click to activate the Balrog · Level 2, without moving";
            return item.Region == "moria" ? "Click to deactivate the Balrog · He returns to Level 0"
                : "Click to deactivate the Balrog · Outside Moria he is eliminated";
        }
    }
}
