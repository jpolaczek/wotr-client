using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): The Black Captain Commands' order stands on its objects instead of two console buttons.
    // Recruit is the Reserve's Nazgûl (they glow in the Reinforcements panel; a click recruits them with the Witch-king) and
    // Move Nazgûl the Nazgûl on the map (their forces and the Witch-king glow; a click moves them and arms that stack in the
    // flight that follows). With no Nazgûl in the reserve Recruit has no object, so it keeps its console button (R16).
    // Server: BlackCaptainStep (captain.recruit recruits up to two reserve Nazgûl in the Witch-king's region; captain.fly
    // opens the Nazgûl's flight, MoveNazgulStep).
    public static class BlackCaptainOrder
    {
        public const string Presentation = "CaptainOrder";
        public const string Recruit = "captain.recruit";
        public const string Fly = "captain.fly";
        private const string WitchKing = "base.character.witch-king";
        private static readonly IReadOnlyDictionary<string, string> NoForces = new Dictionary<string, string>();
        // The server's places off the map (the reserve, removed figures, a battle's casualties, the Fellowship).
        private static readonly HashSet<string> OffMap = new HashSet<string> { "reserve", "removed", "casualties", "fellowship", "eliminated" };

        // This order stands on its object, so the console leaves it out.
        public static bool OnObject(PlayerView view, string id) => id switch
        {
            Recruit => RecruitOffered(view),
            Fly => FlyOffered(view),
            _ => false
        };

        // Recruit waits on the Reserve's Nazgûl while the reserve holds one.
        public static bool RecruitOffered(PlayerView view) => Offered(view, Recruit) && ReserveNazgul(view) != null;

        // The first reserve Nazgûl, whose token's click chooses Recruit, or null.
        public static string ReserveNazgul(PlayerView view) =>
            view.Units.FirstOrDefault(u => u.Side == view.You && u.Kind == UnitKind.Nazgul && u.Location == "reserve")?.Id;

        public static bool FlyOffered(PlayerView view) => Offered(view, Fly) && FlyRegions(view).Count > 0;

        // Region → what a click on its Nazgûl does: the regions of the player's Nazgûl and the Witch-king on the map.
        public static IReadOnlyDictionary<string, string> FlyRegions(PlayerView view)
        {
            if (!Offered(view, Fly))
                return NoForces;
            var figures = view.Units.Where(u => u.Side == view.You && u.Kind == UnitKind.Nazgul && OnMap(u.Location)).Select(u => u.Location)
                .Concat(view.Characters.Where(c => c.Side == view.You && IsWitchKing(c) && OnMap(c.Location)).Select(c => c.Location));
            return figures.Distinct().ToDictionary(region => region, _ => FlyTooltip(view));
        }

        // The Witch-king's miniature lights with the Nazgûl when Move Nazgûl waits on them.
        public static IEnumerable<string> FlyFigures(PlayerView view) => FlyRegions(view).Count == 0
            ? Enumerable.Empty<string>()
            : view.Characters.Where(c => c.Side == view.You && IsWitchKing(c) && OnMap(c.Location)).Select(c => c.Id);

        // TOOLTIP_AUDIT rule 1: "Recruit · 2 Nazgûl · Click to recruit them with the Witch-king because of The Black Captain
        // Commands".
        public static string RecruitTooltip(PlayerView view) => TooltipText.Parts(Title(view, Recruit),
            "Click to recruit them with the Witch-king because of " + Card(view));

        public static string FlyTooltip(PlayerView view) => TooltipText.Parts(Title(view, Fly),
            "Click to move these Nazgûl and the others, the Witch-king among them, because of " + Card(view));

        // The console's hint names the objects that hold the order.
        public static string Hint(PlayerView view)
        {
            var parts = new List<string>();
            if (RecruitOffered(view))
                parts.Add("a glowing Nazgûl in the Reserve to recruit");
            if (FlyOffered(view))
                parts.Add("a glowing Nazgûl on the map to move them");
            return parts.Count == 0 ? "" : "Click " + string.Join(", or ", parts);
        }

        private static string Title(PlayerView view, string id)
        {
            var title = Option(view)?.EffectChoices.FirstOrDefault(c => c.Id == id)?.Title ?? "";
            return title.Length > 0 ? title : id == Recruit ? "Recruit Nazgûl" : "Move Nazgûl";
        }

        private static bool IsWitchKing(CharacterView character) => character.Id == WitchKing || character.DefinitionId == WitchKing ||
            CharacterLibrary.Identity(character.DefinitionId) == WitchKing;

        private static bool OnMap(string location) => !string.IsNullOrEmpty(location) && !OffMap.Contains(location);

        private static string Card(PlayerView view)
        {
            var card = ConfirmSummary.Source(view);
            return card.Length > 0 ? card : "this effect";
        }

        private static bool Offered(PlayerView view, string id) => Option(view)?.EligibleIds.Contains(id) == true;

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
