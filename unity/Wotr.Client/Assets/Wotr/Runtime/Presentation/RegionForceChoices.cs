using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-251: when a card's region holds two eligible forces — Dreadful Spells' field army and garrison — the force is
    // chosen on its own section of the region panel, so the region alone never collapses the alternatives. The
    // server's ids name the force ("region:field", "region:garrison"); a single force is the region click itself.
    // UI-431 (owner's rule, UI-430): the force sections are the only way to choose — no console or tray buttons. The lit
    // region's click opens its panel, where both sections glow and say why; the region stays marked until a force is chosen.
    public static class RegionForceChoices
    {
        public const string Presentation = "Region";
        private const string Garrison = ":garrison";

        public static string For(PlayerView view, TurnDraft draft, string region, bool garrison)
        {
            var option = Option(view, draft);
            if (option == null || string.IsNullOrEmpty(region) || !Alternatives(option, region))
                return "";
            var id = region + (garrison ? Garrison : ":field");
            return option.EligibleIds.Contains(id) && option.EffectChoices.Any(c => c.Id == id) ? id : "";
        }

        // The region holds two eligible forces, so its click opens its panel instead of choosing.
        public static bool HasAlternatives(PlayerView view, TurnDraft draft, string region)
        {
            var option = Option(view, draft);
            return option != null && !string.IsNullOrEmpty(region) && Alternatives(option, region);
        }

        // The region whose forces wait for their click — the region chosen on the map, or the choice's only region — while
        // nothing is chosen yet; "" otherwise.
        public static string Waiting(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (option == null || draft.Payload.Ids.Count > 0)
                return "";
            var regions = option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id)).Select(c => c.TargetId).Distinct().ToList();
            var region = draft.Payload.RegionId.Length > 0 ? draft.Payload.RegionId : regions.Count == 1 ? regions[0] : "";
            return region.Length > 0 && Alternatives(option, region) ? region : "";
        }

        // TOOLTIP_AUDIT rule 1 (UI-431): "Target · Garrison · Click to choose the garrison because of Work of the Enemy · …".
        public static string Tooltip(PlayerView view, TurnDraft draft, string id)
        {
            var choice = Option(view, draft)?.EffectChoices.FirstOrDefault(c => c.Id == id);
            if (choice == null)
                return "";
            var force = id.EndsWith(Garrison) ? "the garrison" : "the field army";
            var card = ConfirmSummary.Source(view);
            return TooltipText.Parts(choice.Title.Length > 0 ? choice.Title : TooltipText.Capital(force),
                "Click to choose " + force + " because of " + (card.Length > 0 ? card : "this effect"), choice.Detail);
        }

        // The console's hint while a region's forces wait for their click.
        public static string Hint(PlayerView view, TurnDraft draft) => Waiting(view, draft).Length == 0 ? ""
            : draft.Payload.RegionId.Length > 0 ? "Click the glowing field army or garrison in the region's panel"
            : "Click the lit region, then its glowing field army or garrison";

        private static bool Alternatives(ActionOption option, string region) =>
            option.EffectChoices.Count(c => c.TargetId == region && option.EligibleIds.Contains(c.Id)) >= 2;

        private static ActionOption Option(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Presentation == Presentation && view.DecisionOwner == view.You && draft?.Kind == CommandKind.ChooseEffect
                ? DraftOptions.For(view, draft) : null;
    }
}
