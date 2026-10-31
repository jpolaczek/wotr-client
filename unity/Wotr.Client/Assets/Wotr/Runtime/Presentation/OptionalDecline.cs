using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-432 (owner's rule, UI-430; the expansions' UI-347 D7 pattern): an optional decline of a base card has no button
    // of its own. The console's Pass sends it, as it declines a reaction window (UI-291), and lights while it can; the
    // battle and Hunt windows' Skip effect sends the same Pass (UI-419, UI-423). Their tooltips and the console's hint
    // name the effect it skips and what passing does: Black Breath's and Heroic Death's Keep all, Fateful Strike's Keep
    // Minions, Andúril's Keep misses and Worn with Sorrow and Toil's Keep cards.
    public static class OptionalDecline
    {
        public const string BlackBreathKeep = "black-breath.decline";
        public const string FatefulStrikeKeep = "fateful-strike.decline";
        public const string HeroicDeathKeep = "heroic-death.decline";
        public const string AndurilKeep = "anduril.keep-misses";
        public const string WornKeep = "worn.decline";

        // What passing does, completing "Pass to …".
        private static readonly IReadOnlyDictionary<string, string> Skips = new Dictionary<string, string>
        {
            [BlackBreathKeep] = "eliminate no Leader or Companion",
            [FatefulStrikeKeep] = "eliminate no Minion",
            [HeroicDeathKeep] = "eliminate no figure and take every hit",
            // The server still forfeits Aragorn's Leadership when he converts no miss (AndurilStep), so passing says so.
            [AndurilKeep] = "convert no miss; Aragorn still forfeits his Leadership",
            [WornKeep] = "discard no Free Peoples Character card"
        };

        // This effect choice is sent by the console's Pass, so its old host (the battle or Hunt window's list) leaves it
        // out.
        public static bool Declines(PlayerView view, string id) => Skip(id).Length > 0;

        private static string Skip(string id) => Skips.TryGetValue(id, out var skip) ? skip : "";

        // The decline the console's Pass sends now, or null.
        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
            var option = Choice(view);
            var id = option?.EligibleIds.FirstOrDefault(candidate => Declines(view, candidate));
            if (id == null)
                return null;
            return new ConsoleOption
            {
                Id = id,
                Name = "decline-" + id,
                Label = "Pass",
                Tooltip = PassTooltip(view, Skip(id)),
                Kind = ConsoleOptionKind.Effect
            };
        }

        // The battle and Hunt windows' Skip effect names the effect it skips and what skipping does: "Skip Black Breath ·
        // Eliminate no Leader or Companion".
        public static string SkipTooltip(PlayerView view, TurnDraft draft)
        {
            var decline = Option(view, draft);
            if (decline == null)
                return "";
            return "Skip " + (EffectName(view) ?? "this effect") + " · " + TooltipText.Capital(Skip(decline.Id));
        }

        // The console's hint ends with what Pass does whenever it sends a decline.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var decline = Option(view, draft);
            if (decline == null || !decline.Enabled)
                return "";
            return "Or Pass to " + Skip(decline.Id) + ".";
        }

        // The card a decline skips — its combat side in a battle ("Black Breath"), otherwise its title — or null.
        private static string EffectName(PlayerView view)
        {
            var source = view?.Decision?.SourceId ?? "";
            if (CardLibrary.Cards.TryGetValue(source, out var card))
                return view.Battle != null && card.CombatTitle.Length > 0 ? card.CombatTitle : card.Title;
            return null;
        }

        // UI-420 (owner): Pass names what it skips — "Pass · Skip Black Breath · Eliminate no Leader or Companion".
        private static string PassTooltip(PlayerView view, string skip)
        {
            var name = EffectName(view);
            return name == null ? "Pass · " + TooltipText.Capital(skip) : TooltipText.Parts("Pass", "Skip " + name, TooltipText.Capital(skip));
        }

        private static ActionOption Choice(PlayerView view) =>
            view?.Decision?.Kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent && view.DecisionOwner == view.You &&
            !view.Winner.HasValue ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
