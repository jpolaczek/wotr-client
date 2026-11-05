using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntResponseActions
    {
        public static ActionOption Option(PlayerView view) => view?.DecisionOwner == view?.You &&
            view?.Decision?.Kind == DecisionKind.HuntDamage
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ResolveHuntDamage) : null;

        public static bool Select(PlayerView view, TurnDraft draft, HuntResponse response)
        {
            if (draft?.Kind != CommandKind.ResolveHuntDamage || Option(view)?.HuntResponses.Contains(response) != true)
                return false;
            FellowshipDraft.Respond(draft, response);
            return true;
        }

        public static bool Read(DraftToken token, out HuntResponse response)
        {
            response = default;
            return token?.Object == DraftObject.HuntResponse && Enum.TryParse(token.Id, out response);
        }

        public static string Consequence(PlayerView view, HuntResponse response)
        {
            var hunt = view?.Hunt;
            if (hunt == null || hunt.DamagePending)
                return "Damage ?\nNot determined yet";
            if (response == HuntResponse.RandomCompanion)
                return "Random casualty\nDamage left ?";
            if (response == HuntResponse.Guide)
            {
                var guide = view.OwnCompanions.FirstOrDefault(c => c.Id == view.Fellowship.Guide);
                var remaining = guide == null ? "?" : Math.Max(0, hunt.Damage - guide.Level).ToString();
                return (hunt.GuideReturns ? "Hobbit returns" : "Lose Guide") +
                    "\nDamage " + hunt.Damage + " → " + remaining;
            }
            var corruption = Math.Max(0, view.Fellowship.Corruption + hunt.Damage - hunt.Healing);
            return "Corruption\n" + view.Fellowship.Corruption + "/12 → " + corruption + "/12";
        }

        // UI-420 (owner): the response by name, then what it does with the Hunt damage — "Ring · Take 2 Hunt damage as
        // Corruption (3 → 5)". The Hunt window's own line already says a new Guide comes before Corruption.
        public static string Describe(PlayerView view, HuntResponse response)
        {
            var hunt = view.Hunt;
            if (hunt == null || hunt.DamagePending)
                return "Hunt damage is not determined yet.";
            if (response == HuntResponse.RandomCompanion)
                return "Random companion · Draw one companion, including the Guide, as the casualty · Remaining damage unknown";
            if (response == HuntResponse.Guide)
                return GuideCasualty(view, hunt);
            return "Ring · " + RingDamage(view, hunt);
        }

        private static string GuideCasualty(PlayerView view, HuntView hunt)
        {
            var guide = view.OwnCompanions.FirstOrDefault(c => c.Id == view.Fellowship.Guide);
            var remaining = guide == null ? "?" : Math.Max(0, hunt.Damage - guide.Level).ToString();
            var loss = hunt.GuideReturns ? "Guide casualty; the Hobbit returns to the map" : "Lose the Guide";
            return TooltipText.Parts(CharacterLibrary.Name(view, view.Fellowship.Guide), loss,
                "Hunt damage " + hunt.Damage + " → " + remaining);
        }

        // "Take 2 Hunt damage as Corruption (3 → 5)"; a healing in the same Hunt is named.
        private static string RingDamage(PlayerView view, HuntView hunt)
        {
            var before = view.Fellowship.Corruption;
            var after = Math.Max(0, before + hunt.Damage - hunt.Healing);
            var healing = hunt.Healing > 0 ? ", heal " + hunt.Healing : "";
            return "Take " + hunt.Damage + " Hunt damage as Corruption" + healing + " (" + before + " → " + after + ")";
        }
    }
}
