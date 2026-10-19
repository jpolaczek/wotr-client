using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum ConsoleOptionKind
    {
        Effect,
        Reaction,
        Decline,
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
        private static readonly HashSet<string> Modes = new HashSet<string>
        {
            "CaptainOrder", "LureCompanion", "FellowshipAction", "NazgulStrike",
            // UI-264: the expansions' two-way card modes (Fate's Monsters, the Kings cards' branches).
            "EffectTargets", "SovereignCardBranch",
            // UI-271: the Balrog card's order: move him alone, move his army, or finish.
            "BalrogOrder",
            // UI-272: the Lore of the Elven Rings: its die and result are chosen on the pool; Keep results is a button.
            // Wind from the West and Safe Paths in the Dark discard Shadow table cards with their × in the Effects
            // panel; Three Rings for the Elven-Kings recovers Rings on their sockets.
            LoreChoice.Presentation, "TableCards", RingRecovery.Presentation,
            // UI-274: a card's choice of a king, the cleanse and the inspection of a king's tiles.
            SovereignChoices.Sovereign, SovereignChoices.Removal, SovereignChoices.Inspection
        };

        // The decision's choices belong to the console and their objects; the tray builds none of them.
        public static bool Owns(PlayerView view, TurnDraft draft) =>
            Card(view, draft) && !RegionChoice(view) || Reaction(view, draft) && ReactionOptions(view).Count <= Limit ||
            NamedRings.VilyaDecision(view) || SovereignAttempt.OwnAttempt(view);

        // A region's alternatives (Dreadful Spells) are console buttons; the region itself stays a map click.
        public static bool HostsAlternatives(PlayerView view, TurnDraft draft) => Card(view, draft) && RegionChoice(view);

        public static IReadOnlyList<ConsoleOption> For(PlayerView view, TurnDraft draft)
        {
            if (CardArmyEvent.Active(view, draft))
                return CardArmyEvent.Alternatives(view, draft);
            if (Card(view, draft))
                return CardOptions(view, draft);
            // UI-268: Vilya is kept on its socket; spending the die is the console's button.
            if (NamedRings.VilyaDecision(view))
                return NamedRings.SpendTheDie(view) is { } spend ? new List<ConsoleOption> { spend } : None;
            // UI-274: Sequential corruption's Draw again; the tiles stand beside the king.
            if (SovereignAttempt.OwnAttempt(view))
                return SovereignAttempt.ConsoleOptions(view);
            return Owns(view, draft) ? ReactionOptions(view) : None;
        }

        private static bool RegionChoice(PlayerView view) => view.Decision.Presentation == "Region";

        // The Nazgûl Strike's table cards, chosen with their × in the Effects panel: source id → tooltip.
        public static IReadOnlyDictionary<string, string> TableCards(PlayerView view, TurnDraft draft)
        {
            var option = Card(view, draft) ? DraftOptions.For(view, draft) : null;
            if (option == null || view.Decision.Presentation is not ("NazgulStrike" or "TableCards"))
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
            if (view.Decision.Presentation == LoreChoice.Presentation)
                return option.EffectChoices.Where(LoreChoice.KeepOption).Select(c => Effect(view, draft, option, c)).ToList();
            if (view.Decision.Presentation == RingRecovery.Presentation)
                return None;
            // UI-274: the kings and their tiles are chosen in the Characters panel; Keep tiles is a button.
            if (SovereignChoices.Owns(view))
                return SovereignChoices.ConsoleOptions(view, draft);
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
            return options;
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
            // A reaction from a card in the hand is used at that card (UI-164), the Guide's on its portrait (UI-247), the
            // Chief's at the Character deck and at his figure (UI-271).
            var tableCards = ReactionTableCards(view);
            var options = respond?.EffectChoices.Where(c => !view.Hand.Contains(c.SourceId) && !FellowshipActions.ReactionOnRoster(view, c.Id) &&
                    !tableCards.ContainsKey(c.SourceId) && !ChiefReactionObjects.OnObject(c.Id))
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
            var decline = view.Options.FirstOrDefault(o => o.Kind == CommandKind.DeclineReaction);
            if (decline != null)
                options.Add(new ConsoleOption
                {
                    Id = "decline",
                    Name = "action-DeclineReaction",
                    Label = "Decline",
                    Tooltip = decline.Enabled ? "Continue without using a reaction in this window" : decline.Reason,
                    Kind = ConsoleOptionKind.Decline,
                    Enabled = decline.Enabled
                });
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
