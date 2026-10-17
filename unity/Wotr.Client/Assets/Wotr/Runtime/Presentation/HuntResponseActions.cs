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

        public static string Describe(PlayerView view, HuntResponse response)
        {
            var hunt = view.Hunt;
            if (hunt == null || hunt.DamagePending)
                return "Damage is not determined yet.";
            if (response == HuntResponse.RandomCompanion)
                return "Draw one companion, including the Guide · Casualty and remaining damage unknown.";
            if (response == HuntResponse.Guide)
            {
                var guide = view.OwnCompanions.FirstOrDefault(c => c.Id == view.Fellowship.Guide);
                var remaining = guide == null ? "?" : Math.Max(0, hunt.Damage - guide.Level).ToString();
                return (hunt.GuideReturns ? "Guide casualty · Hobbit returns to the map" : "Lose the Guide") +
                    " · Damage " + hunt.Damage + " → " + remaining + " · New Guide before Corruption.";
            }
            var corruption = Math.Max(0, view.Fellowship.Corruption + hunt.Damage - hunt.Healing);
            return "Corruption " + view.Fellowship.Corruption + " → " + corruption + " · Resolve before any advance.";
        }
    }
}
