using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One response object in the Hunt window's response row: what it is called, what it does and how it is sent.
    public sealed class HuntCardResponse
    {
        public string Id = "";
        public string CardId = "";
        public string Name = "";
        public string Consequence = "";
        public string Tooltip = "";
        // A reaction is sent as RespondEffect; a card's cost choice (We Won't Go Back) as ChooseEffect.
        public bool Reaction;
    }

    // UI-272: the Lords Shadow table cards that answer in the Hunt stand as response objects in the Hunt window's
    // response row, beside the damage and Guide responses. We Shall Get It! (replace the drawn tile) and The Gates are
    // Closed (an extra tile after a reveal) are reactions whose use discards the card; Skip declines. We Won't Go Back's
    // cost offers the table card and No Corruption as objects, and the Character cards it accepts glow in the hand.
    public static class HuntCardResponses
    {
        public const string WeWontGoBack = "lords.sh.character.25";
        public const string NoCorruption = "smeagol.no-corruption";
        public const string Smeagol = "SmeagolCorruption";
        private static readonly IReadOnlyList<HuntCardResponse> None = new HuntCardResponse[0];

        public static IReadOnlyList<HuntCardResponse> Offered(PlayerView view)
        {
            if (view?.Hunt == null || view.Decision == null || view.DecisionOwner != view.You || view.Winner.HasValue)
                return None;
            if (view.Decision.Kind == DecisionKind.Reaction)
                return Reactions(view);
            return view.Decision.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Smeagol ? Costs(view) : None;
        }

        public static bool InRow(PlayerView view, string id) => Offered(view).Any(r => r.Id == id);

        public static HuntCardResponse Find(PlayerView view, string id) => Offered(view).FirstOrDefault(r => r.Id == id);

        private static IReadOnlyList<HuntCardResponse> Reactions(PlayerView view)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            if (option == null)
                return None;
            return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && c.Id.StartsWith("lords.") && OnTable(view, c.SourceId))
                .Select(c => new HuntCardResponse
                {
                    Id = c.Id, CardId = c.SourceId, Name = CardLibrary.Title(c.SourceId), Reaction = true,
                    Consequence = c.Title.Length > 0 ? c.Title : "Use this card",
                    Tooltip = "Click to discard " + CardLibrary.Title(c.SourceId) + " · " + (c.Title.Length > 0 ? c.Title : "Use this card") +
                        (c.Detail.Length > 0 ? "\n" + c.Detail : "") + "\nOr Skip to continue without it."
                }).ToList();
        }

        private static bool OnTable(PlayerView view, string card) => card.Length > 0 &&
            (view.Players.Any(p => p.TableCards.Contains(card)) || view.Effects.Any(e => e.SourceId == card));

        private static IReadOnlyList<HuntCardResponse> Costs(PlayerView view)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect);
            if (option == null)
                return None;
            var responses = new List<HuntCardResponse>();
            if (option.EligibleIds.Contains(WeWontGoBack))
                responses.Add(new HuntCardResponse
                {
                    Id = WeWontGoBack, CardId = WeWontGoBack, Name = CardLibrary.Title(WeWontGoBack), Consequence = "+2 Corruption",
                    Tooltip = "Click to discard " + CardLibrary.Title(WeWontGoBack) + " from the table · The Fellowship takes 2 Corruption" +
                        "\nOr click a glowing Character card in your hand to discard it instead."
                });
            if (option.EligibleIds.Contains(NoCorruption))
                responses.Add(new HuntCardResponse
                {
                    Id = NoCorruption, Name = "No Corruption", Consequence = "Keep the cards",
                    Tooltip = "Click to discard nothing · The Fellowship takes no Corruption from Sméagol's tile."
                });
            return responses;
        }
    }
}
