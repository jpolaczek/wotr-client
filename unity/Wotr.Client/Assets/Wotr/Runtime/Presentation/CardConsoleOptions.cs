using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum ConsoleOptionKind
    {
        Effect,
        Reaction,
        Action,
        // UI-233: after View board, the console reopens the end of the game (result, log, Save replay).
        // UI-455 (owner, 2026-11-03): UI-448's Cancel option is the console's fixed Cancel (ConsoleActions, DraftCancel).
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
        // UI-431: the region a region panel's force section belongs to, kept in the payload as the region's click did.
        public string Region = "";
    }

    // UI-214 (CARD_CHOICES_UX rule 4): a card's modes and options, and the object-less responses of a reaction outside
    // the Hunt and battles, are at most three buttons in the console. A choice with an object stays on it: a table
    // card in the Effects panel, a reaction card at the card in the hand. A reaction with more object-less choices
    // than fit keeps the decision window. UI-430 (owner's rule): a button stands only for an option no object draws — a
    // red table card, the glowing Hunt Box and the console's ✓ (a Finish) take no button. UI-431: nor do a region's
    // forces (their region panel's sections), a card army event's steps (the map and the ✓), the Black Captain's orders
    // (the Reserve's and the map's Nazgûl), Lure of the Ring's (the companion's portrait and the corruption track),
    // Monsters Roused's halves (their regions) and a reaction's draw from a deck (the deck).
    public static class CardConsoleOptions
    {
        public const int Limit = 3;
        public const string TableCardsPresentation = "TableCards";
        private static readonly IReadOnlyList<ConsoleOption> None = new List<ConsoleOption>();
        private static readonly IReadOnlyDictionary<string, string> NoTableCards = new Dictionary<string, string>();
        private static readonly HashSet<string> Modes = new HashSet<string>
        {
            "CaptainOrder", "LureCompanion", FellowshipCardActions.Presentation, NazgulStrikeChoice.Presentation,
            // UI-264: the expansions' two-way card modes (Fate's Monsters, the Kings cards' branches).
            "EffectTargets", "SovereignCardBranch",
            // UI-271: the Balrog card's order; UI-347 (D11): on the Balrog's figure and his army, Finish is the Pass.
            BalrogCardOrder.Presentation,
            // UI-272: the Lore of the Elven Rings: its die and result are chosen on the pool; Keep results is a button.
            // Wind from the West and Safe Paths in the Dark discard Shadow table cards with their × in the Effects
            // panel; Three Rings for the Elven-Kings recovers Rings on their sockets.
            LoreChoice.Presentation, TableCardsPresentation, RingRecovery.Presentation,
            // UI-274: a card's choice of a king, the cleanse and the inspection of a king's tiles.
            SovereignChoices.Sovereign, SovereignChoices.Removal, SovereignChoices.Inspection,
            // UI-278: the Kings Nation step on the nation tokens.
            KingsCardChoices.NationStep
        };

        // The decision's choices belong to the console and their objects; the tray builds none of them.
        public static bool Owns(PlayerView view, TurnDraft draft) =>
            Card(view, draft) && !RegionChoice(view) || Reaction(view, draft) && ReactionOptions(view).Count <= Limit ||
            NamedRings.VilyaDecision(view) || SovereignAttempt.OwnAttempt(view);

        public static IReadOnlyList<ConsoleOption> For(PlayerView view, TurnDraft draft)
        {
            // UI-431: a card army event's Move and Attack are its lit targets on the map, its Skip maneuver the ✓.
            if (CardArmyEvent.Active(view, draft))
                return None;
            if (Card(view, draft))
                return CardOptions(view, draft);
            // UI-268: Vilya is kept on its socket; UI-347 (D7): spending the die is the console's Pass.
            if (NamedRings.VilyaDecision(view))
                return None;
            // UI-274: Sequential corruption's Draw again; the tiles stand beside the king.
            if (SovereignAttempt.OwnAttempt(view))
                return SovereignAttempt.ConsoleOptions(view);
            return Owns(view, draft) ? ReactionOptions(view) : None;
        }

        private static bool RegionChoice(PlayerView view) => view.Decision.Presentation == "Region";

        // The Nazgûl Strike's (and Wind from the West's, Safe Paths in the Dark's) table cards, red in the Effects panel,
        // where a click on the tile or its × discards them: source id → tooltip (UI-430: the effect that offers them).
        public static IReadOnlyDictionary<string, string> TableCards(PlayerView view, TurnDraft draft)
        {
            var option = Card(view, draft) ? DraftOptions.For(view, draft) : null;
            if (option == null || view.Decision.Presentation is not (NazgulStrikeChoice.Presentation or TableCardsPresentation))
                return NoTableCards;
            var effect = ConfirmSummary.Source(view);
            var reason = view.Decision.Presentation == NazgulStrikeChoice.Presentation ? NazgulStrikeChoice.Reason : "";
            return option.EffectChoices.Where(TableCard).ToDictionary(c => c.Id, c => TableDiscardText.Offered(c.SourceId, effect, reason));
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
            // UI-431: a region's two forces are its region panel's glowing sections (RegionForceChoices).
            if (option == null || RegionChoice(view))
                return None;
            // UI-347 (D7): the Lore's Keep results is the console's Pass; its die and result stay on the pool. D11: the
            // Balrog card's order stands on his figure and his army (BalrogCardOrder).
            if (view.Decision.Presentation is LoreChoice.Presentation or RingRecovery.Presentation or BalrogCardOrder.Presentation)
                return None;
            // UI-274: the kings and their tiles are chosen in the Characters panel; UI-278 (D7): Keep tiles is the Pass.
            // The Kings Nation step stands on the nation tokens.
            if (SovereignChoices.Owns(view) || view.Decision.Presentation == KingsCardChoices.NationStep)
                return None;
            // UI-249: There Is Another Way's Move and Hide are made on the Fellowship's track and Ring.
            // UI-278 (D15): a Kings card's branch on its object (a nation token, a king, the Reserve) stays there.
            // UI-430 (owner): a table card is red in the Effects panel, The Nazgûl Strike!'s Hunt roll is the Hunt Box and a
            // Finish (Stay) is the console's ✓, so none of them is a button.
            var options = option.EffectChoices.Where(c => !TableCard(c) && !FellowshipCardActions.OnObject(view, c.Id) &&
                    !OptionalDecline.Declines(view, c.Id) && !KingsCardChoices.OnObject(view, c.Id) &&
                    !NazgulStrikeChoice.OnObject(c.Id) && !FinishingOption.Finishes(c.Id) && !OnBoardObject(view, c.Id))
                .Select(c => Effect(view, draft, option, c)).ToList();
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

        // UI-431: an option a board object makes — the Black Captain's Nazgûl, Lure of the Ring's companion and corruption
        // track, Monsters Roused's regions — has no button; one without its object (a recruit with no Nazgûl in the
        // reserve) keeps it (R16).
        private static bool OnBoardObject(PlayerView view, string id) =>
            BlackCaptainOrder.OnObject(view, id) || LureChoice.OnObject(view, id) || MonstersHalves.OnObject(view, id);

        private static ConsoleOption Effect(PlayerView view, TurnDraft draft, ActionOption option, EffectChoiceView choice) => new ConsoleOption
        {
            Id = choice.Id,
            Name = "effect-choice-" + choice.Id,
            Label = EffectLabel(view, choice),
            Tooltip = choice.Detail.Length > 0 ? choice.Detail : TooltipText.Parts(ConfirmSummary.Source(view), choice.Title),
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

        // UI-247: a table card's own reaction outside a battle or the Hunt (The Balrog of Moria) is used at that card in the
        // Effects panel — the reaction discards the card; UI-430: the card is red and a click on its tile or its × uses it:
        // source id → tooltip.
        public static IReadOnlyDictionary<string, string> ReactionTableCards(PlayerView view)
        {
            var respond = TableReactionOption(view);
            if (respond == null)
                return NoTableCards;
            return respond.EffectChoices.Where(c => respond.EligibleIds.Contains(c.Id) && view.Effects.Any(e => e.SourceId == c.SourceId))
                .GroupBy(c => c.SourceId).Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => TableDiscardText.Reaction(g.Key, g.First().Detail));
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
            // UI-430 (owner): a table card's response is that red card in the Effects panel, with no button of its own.
            var tableCards = ReactionTableCards(view);
            // UI-431: a response that draws from a deck is that lit deck (ReactionDecks).
            var options = respond?.EffectChoices.Where(c => !view.Hand.Contains(c.SourceId) && !FellowshipActions.ReactionOnRoster(view, c.Id) &&
                    !tableCards.ContainsKey(c.SourceId) && !ChiefReactionObjects.OnObject(c.Id) && !ReactionDecks.OnObject(view, c.Id))
                .Select(Response).ToList() ??
                new List<ConsoleOption>();
            // UI-291: the console's Pass declines the window, so no Decline option takes the hint's place. UI-418: a single
            // response is lit.
            if (options.Count == 1 && options[0].Kind == ConsoleOptionKind.Reaction)
                options[0].Lit = true;
            return options;
        }

        private static ConsoleOption Response(EffectChoiceView choice)
        {
            var name = ReactionSkip.Owner(choice);
            return new ConsoleOption
            {
                Id = choice.Id,
                Name = "effect-choice-" + choice.Id,
                Label = choice.Title.Length > 0 ? choice.Title : name,
                Tooltip = choice.Detail.Length > 0 ? choice.Detail : "Use " + name + "'s response",
                Kind = ConsoleOptionKind.Reaction
            };
        }
    }
}
