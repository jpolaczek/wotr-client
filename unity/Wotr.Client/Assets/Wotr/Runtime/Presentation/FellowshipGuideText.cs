using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Presentation of already projected versions; candidacy and effects remain server rules.
    public static class FellowshipGuideText
    {
        // UI-229: the console's hint while the next Guide is chosen from the glowing portraits.
        public const string ChooseHint = "Click a glowing companion in the Fellowship panel to make it the Guide";

        public static bool IsGuide(FellowshipView fellowship, string id) => fellowship.Guide == id ||
            CharacterLibrary.Identity(fellowship.Guide) == id;

        public static string PublicVersion(FellowshipView fellowship, string id)
        {
            var version = fellowship.RevealedCompanionVersions.FirstOrDefault(p => p.Key == id || CharacterLibrary.Identity(p.Key) == id).Value;
            return version ?? (IsGuide(fellowship, id) ? fellowship.Guide : "");
        }

        public static string Ability(string version) => version switch
        {
            "base.character.gandalf-grey" => "Guide: after resolving an Event die used to play a card, you may draw a card from the same deck.",
            "base.character.strider" => "Guide: use any Action die to hide the Fellowship.",
            "base.character.merry" or "base.character.pippin" =>
                "Guide: when Hunt damage remains, you may separate this Hobbit to reduce it by 1. Separated companions cannot rejoin.",
            "base.character.gollum" =>
                "Guide: ignore Reveal on standard numbered tiles. When legal, you may reveal a hidden Fellowship to reduce Hunt damage by 1.",
            "lords.character.gandalf-grey" =>
                "Guide: add the Narya die at Recover if a Free Peoples movement die returns from the Hunt, or as the starting Guide in turn 1. Changing guide now does not add a die this turn.",
            "lords.character.strider" =>
                "Guide: on the first Fellowship move this turn, if progress was at least 1, place the used die in the Fellowship Box after resolving the Hunt.",
            "lords.character.merry" or "lords.character.pippin" =>
                "Guide: if both alternate Hobbits are in the Fellowship, you may eliminate both to cancel a standard Hunt tile.",
            "lords.character.boromir" or "lords.character.gimli" or "lords.character.legolas" or "fate.character.gimli-dwarven-lord" =>
                "Guide: you may separate to reduce Hunt damage by 1, to a minimum of 0. Companions leaving against the same tile travel as one group.",
            "lords.character.smeagol" =>
                "Must be the Guide. His level equals the highest other companion. Separation removes him from play; he has no figure on the map.",
            "base.character.gimli" or "base.character.boromir" or "base.character.legolas" or "fate.character.gimli" => "No Guide ability.",
            _ => ""
        };
    }
}
