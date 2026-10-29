using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum ConsoleOptionKind
    {
        Effect,
        Reaction,
        OpenEffects,
        Action,
        // UI-233: after View board, the console reopens the end of the game (result, log, Save replay).
        OpenGameEnd
    }

    // UI-214: one object-less choice of a card or a reaction, shown as a button in the turn console's status zone.
    public sealed class ConsoleOption
    {
        public string Id = "";
        public string Name = "";
        public string Label = "";
        public string Tooltip = "";
        public ConsoleOptionKind Kind;
        // An Action option chooses this command (UI-216: an army event's alternatives).
        public CommandKind? Command;
        public bool Enabled = true;
        // UI-418: the decision's only option, lit and pulsing; on its card's button (SourceId), whose committed button
        // then leaves the trail.
        public bool Lit;
        public string SourceId = "";
    }

    // UI-214 (CARD_CHOICES_UX rule 4): a card's modes and options, and the object-less responses of a reaction outside
    // the Hunt and battles, are at most three buttons in the console. A choice with an object stays on it: a table
    // card in the Effects panel, a reaction card at the card in the hand. A reaction with more object-less choices
    // than fit keeps the decision window.
    public static class CardConsoleOptions
    {
        public const int Limit = 3;
        private static readonly IReadOnlyList<ConsoleOption> None = new List<ConsoleOption>();
        private static readonly IReadOnlyDictionary<string, string> NoTableCards = new Dictionary<string, string>();
        private static readonly HashSet<string> Modes = new HashSet<string> { "CaptainOrder", "LureCompanion", "FellowshipAction", "NazgulStrike" };

        // The decision's choices belong to the console and their objects; the tray builds none of them.
        public static bool Owns(PlayerView view, TurnDraft draft) =>
            Card(view, draft) && !RegionChoice(view) || Reaction(view, draft) && ReactionOptions(view).Count <= Limit;

        // A region's alternatives (Dreadful Spells) are console buttons; the region itself stays a map click.
        public static bool HostsAlternatives(PlayerView view, TurnDraft draft) => Card(view, draft) && RegionChoice(view);

        public static IReadOnlyList<ConsoleOption> For(PlayerView view, TurnDraft draft)
        {
            if (CardArmyEvent.Active(view, draft))
                return CardArmyEvent.Alternatives(view, draft);
            if (Card(view, draft))
                return CardOptions(view, draft);
            return Owns(view, draft) ? ReactionOptions(view) : None;
        }

        private static bool RegionChoice(PlayerView view) => view.Decision.Presentation == "Region";

        // The Nazgûl Strike's table cards, chosen with their × in the Effects panel: source id → tooltip.
        public static IReadOnlyDictionary<string, string> TableCards(PlayerView view, TurnDraft draft)
        {
            var option = Card(view, draft) ? DraftOptions.For(view, draft) : null;
            if (option == null || view.Decision.Presentation != "NazgulStrike")
                return NoTableCards;
            return option.EffectChoices.Where(TableCard).ToDictionary(c => c.Id,
                c => "Discard " + CardLibrary.Title(c.SourceId) + " · " + c.Detail);
        }

        private static bool Card(PlayerView view, TurnDraft draft) =>
            CardDecisions.Owns(view, draft) && view.Decision.Kind == DecisionKind.EffectChoice && draft.Kind == CommandKind.ChooseEffect &&
            (Modes.Contains(view.Decision.Presentation) || view.Decision.Presentation == "Region");

        private static bool Reaction(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.Reaction && view.DecisionOwner == view.You && view.Battle == null && view.Hunt == null &&
            view.EventLoss == null && (draft.Kind is null or CommandKind.RespondEffect) &&
            view.Options.All(o => o.Kind is CommandKind.RespondEffect or CommandKind.DeclineReaction or CommandKind.PlayEvent);

        private static IReadOnlyList<ConsoleOption> CardOptions(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return None;
            if (RegionChoice(view))
                return Alternatives(view, draft, option);
            // UI-249: There Is Another Way's Move and Hide are made on the Fellowship's track and Ring; Stay stays here.
            var options = option.EffectChoices.Where(c => !TableCard(c) && !FellowshipCardActions.OnObject(view, c.Id))
                .Select(c => Effect(view, draft, option, c)).ToList();
            if (option.EffectChoices.Any(TableCard))
                options.Insert(0, new ConsoleOption
                {
                    Id = "table-cards",
                    Name = "console-option-table-cards",
                    Label = "Discard a table card",
                    Tooltip = "Choose a lit table card in the Effects panel",
                    Kind = ConsoleOptionKind.OpenEffects
                });
            if (options.Count == 1 && options[0].Kind == ConsoleOptionKind.Effect)
                Single(options[0], view.Decision.SourceId);
            return options;
        }

        // UI-418 (owner): a card decision with one option besides its decline (the console's Pass) is one lit, pulsing
        // button named after the card, whose tooltip says what it does (Wind from the West · Inspect Shadow hand); two
        // buttons, the committed card and its option, said the same.
        private static void Single(ConsoleOption option, string source)
        {
            option.Lit = true;
            if (!CardLibrary.Cards.ContainsKey(source))
                return;
            var does = option.Tooltip.Length > 0 && option.Tooltip != option.Label ? option.Label + " · " + option.Tooltip : option.Label;
            option.Label = CardLibrary.Title(source);
            option.Tooltip = does;
            option.SourceId = source;
        }

        // Dreadful Spells: a field army and a garrison in the chosen region are two buttons.
        private static IReadOnlyList<ConsoleOption> Alternatives(PlayerView view, TurnDraft draft, ActionOption option)
        {
            var regions = option.EffectChoices.Select(c => c.TargetId).Distinct().ToList();
            var region = draft.Payload.RegionId.Length > 0 ? draft.Payload.RegionId : regions.Count == 1 ? regions[0] : "";
            var alternatives = option.EffectChoices.Where(c => region.Length > 0 && c.TargetId == region).ToList();
            return alternatives.Count > 1 ? alternatives.Select(c => Effect(view, draft, option, c)).ToList() : None;
        }

        private static ConsoleOption Effect(PlayerView view, TurnDraft draft, ActionOption option, EffectChoiceView choice) => new ConsoleOption
        {
            Id = choice.Id,
            Name = "effect-choice-" + choice.Id,
            Label = EffectLabel(view, choice),
            Tooltip = choice.Detail.Length > 0 ? choice.Detail : choice.Title,
            Kind = ConsoleOptionKind.Effect,
            Enabled = EffectChoiceDraft.CanChoose(draft, option, choice.Id)
        };

        // Lure of the Ring names the Companion it concerns, which the card's title in the headline does not.
        private static string EffectLabel(PlayerView view, EffectChoiceView choice)
        {
            var title = choice.Title.Length > 0 ? choice.Title : choice.Id;
            return view.Decision.Presentation == "LureCompanion" && choice.SourceId.Length > 0
                ? CharacterLibrary.ShortName(view, choice.SourceId) + " · " + title : title;
        }

        // UI-247: a table card's own reaction outside a battle or the Hunt (The Balrog of Moria) is used at that card's ×
        // in the Effects panel — the reaction discards the card: source id → tooltip.
        public static IReadOnlyDictionary<string, string> ReactionTableCards(PlayerView view)
        {
            var respond = TableReactionOption(view);
            if (respond == null)
                return NoTableCards;
            return respond.EffectChoices.Where(c => respond.EligibleIds.Contains(c.Id) && view.Effects.Any(e => e.SourceId == c.SourceId))
                .GroupBy(c => c.SourceId).Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => "Discard " + CardLibrary.Title(g.Key) + " to use it · " + g.First().Detail);
        }

        // The reaction a table card's × uses, or "".
        public static string TableReaction(PlayerView view, string sourceId) =>
            ReactionTableCards(view).ContainsKey(sourceId)
                ? TableReactionOption(view).EffectChoices.First(c => c.SourceId == sourceId).Id : "";

        private static ActionOption TableReactionOption(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.Reaction && view.DecisionOwner == view.You && view.Battle == null && view.Hunt == null &&
            view.EventLoss == null ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect) : null;

        private static bool TableCard(EffectChoiceView choice) => choice.SourceId.Length > 0 && CardLibrary.Cards.ContainsKey(choice.SourceId);

        private static IReadOnlyList<ConsoleOption> ReactionOptions(PlayerView view)
        {
            var respond = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            // A reaction from a card in the hand is used at that card (UI-164), the Guide's on its portrait (UI-247).
            var tableCards = ReactionTableCards(view);
            var options = respond?.EffectChoices.Where(c => !view.Hand.Contains(c.SourceId) && !FellowshipActions.ReactionOnRoster(view, c.Id) &&
                    !tableCards.ContainsKey(c.SourceId))
                .Select(Response).ToList() ??
                new List<ConsoleOption>();
            if (tableCards.Count > 0)
                options.Insert(0, new ConsoleOption
                {
                    Id = "table-cards",
                    Name = "console-option-table-cards",
                    Label = "Use a table card",
                    Tooltip = "Click the lit table card's × in the Effects panel",
                    Kind = ConsoleOptionKind.OpenEffects
                });
            // UI-291: the console's Pass declines the window, so no Decline option takes the hint's place. UI-418: a single
            // response is lit.
            if (options.Count == 1 && options[0].Kind == ConsoleOptionKind.Reaction)
                options[0].Lit = true;
            return options;
        }

        private static ConsoleOption Response(EffectChoiceView choice)
        {
            var source = choice.SourceId.Length > 0 ? choice.SourceId : choice.Id;
            var name = source.Contains(".character.") && !CardLibrary.Cards.ContainsKey(source)
                ? CharacterLibrary.Name(source) : CardLibrary.Title(source);
            return new ConsoleOption
            {
                Id = choice.Id,
                Name = "effect-choice-" + choice.Id,
                Label = choice.Title.Length > 0 ? choice.Title : name,
                Tooltip = choice.Detail.Length > 0 ? choice.Detail : "Use this response",
                Kind = ConsoleOptionKind.Reaction
            };
        }
    }
}
