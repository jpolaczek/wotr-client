using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One table card's response object in the Hunt window's response row: what it is called, what it does and its
    // tooltip.
    public sealed class HuntCardResponse
    {
        public string Id = "";
        public string CardId = "";
        public string Name = "";
        public string Consequence = "";
        public string Tooltip = "";
    }

    // UI-285 (owner's decision after the object controls audit): the table cards that answer in the Hunt — Axe and Bow,
    // Mithril Coat and Sting, Horn of Gondor, Wizard's Staff, Crebain… — stand as card objects in the Hunt window's
    // response row, beside the Ring, Guide and random-companion responses; one click uses the card (discarding it when
    // that is its cost), and Skip declines. They no longer fall to the generic choice list.
    public static class HuntCardResponses
    {
        private static readonly IReadOnlyList<HuntCardResponse> None = new HuntCardResponse[0];

        public static IReadOnlyList<HuntCardResponse> Offered(PlayerView view)
        {
            if (view?.Hunt == null || view.Decision?.Kind != DecisionKind.Reaction || view.DecisionOwner != view.You ||
                view.Winner.HasValue)
                return None;
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            if (option == null)
                return None;
            return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && OnTable(view, c.SourceId))
                .Select(Response).ToList();
        }

        public static bool InRow(PlayerView view, string id) => Offered(view).Any(r => r.Id == id);

        private static HuntCardResponse Response(EffectChoiceView choice)
        {
            var title = CardLibrary.Title(choice.SourceId);
            var consequence = choice.Title.Length > 0 ? choice.Title : "Use this card";
            var cost = choice.Cost == "DiscardTableCard" ? "Click to discard " + title : "Click to use " + title;
            return new HuntCardResponse
            {
                Id = choice.Id, CardId = choice.SourceId, Name = title, Consequence = consequence,
                Tooltip = cost + " · " + consequence + (choice.Detail.Length > 0 ? "\n" + choice.Detail : "") +
                    "\nOr Skip to continue without it."
            };
        }

        private static bool OnTable(PlayerView view, string card) => card.Length > 0 &&
            (view.Players.Any(p => p.TableCards.Contains(card)) || view.Effects.Any(e => e.SourceId == card));
    }
}
