using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-214 (CARD_CHOICES_UX): a card's object-less options and a reaction's responses as buttons in the console, and
    // The Nazgûl Strike's table cards in the Effects panel. A click that completes the decision sends it. UI-430 (owner):
    // the table cards are red and their tiles' click discards them, and The Nazgûl Strike!'s Hunt roll is the console's
    // glowing Hunt Box; each sends what its old console button sent. UI-432: Worn with Sorrow and Toil's table cards are red
    // there too, and its random hand card is the opponent's red card counter in the top bar.
    public sealed class ConsoleOptionsInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<ConsoleOption> choose;
        private readonly Action convert;
        private PlayerView view;
        private TurnDraft draft;
        private ConsoleOption huntRoll;

        public ConsoleOptionsInteraction(BoardScreen board, Action<ConsoleOption> choose, Action convert)
        {
            this.board = board;
            this.choose = choose;
            this.convert = convert;
            board.Console.Status.OptionChosen += Chosen;
            board.TableEffects.OfferChosen += TableCard;
            board.TableEffects.UseChosen += CardUse;
            board.Hunt.OfferChosen += HuntRoll;
            board.Hover.Panel.ForceChosen += Force;
            board.TopBar.Opponent.CardsChosen += RandomHandCard;
        }

        public void Refresh(PlayerView view, TurnDraft draft, bool enabled)
        {
            if (view.Winner.HasValue)
            {
                ShowGameEnd(view, draft);
                return;
            }
            this.view = view;
            this.draft = draft;
            var panel = board.Hover.Panel;
            panel.ForceChoice = (side, garrison) => enabled ? RegionForceChoices.For(view, draft, panel.Region, garrison) : "";
            panel.ForceTooltip = id => RegionForceChoices.Tooltip(view, draft, id);
            panel.RefreshForces();
            board.Console.Status.SetOptions(RouteCancel.Append(CardConsoleOptions.For(view, draft), view, draft), enabled);
            board.TableEffects.Offer(TableOffers(view, draft), enabled);
            board.TableEffects.Uses(enabled ? CardUses(view, draft) : new Dictionary<string, string>());
            OfferHuntRoll(enabled ? NazgulStrikeChoice.Roll(view, draft) : null);
            board.TopBar.Opponent.OfferCards(enabled ? WornDiscardChoice.RandomCard(view, draft) : "");
        }

        // UI-430: the Hunt Box glows and pulses while its click rolls for the Hunt (The Nazgûl Strike!).
        private void OfferHuntRoll(ConsoleOption roll)
        {
            huntRoll = roll;
            board.Hunt.Offer(roll?.Tooltip ?? "");
        }

        // UI-272: the Lore before Dismay; UI-278: the Palantír of Barad-dûr's Muster-as-Event.
        private static IReadOnlyDictionary<string, string> CardUses(PlayerView view, TurnDraft draft)
        {
            var palantir = PalantirConversion.CardUses(view, draft);
            if (palantir.Count == 0)
                return LoreChoice.CardUses(view);
            var uses = new Dictionary<string, string>(LoreChoice.CardUses(view));
            foreach (var use in palantir)
                uses[use.Key] = use.Value;
            return uses;
        }

        // The Nazgûl Strike's table cards, Worn with Sorrow and Toil's (UI-432) and, outside a battle or the Hunt, a table
        // card's own reaction (UI-247).
        private static IReadOnlyDictionary<string, string> TableOffers(PlayerView view, TurnDraft draft) =>
            Merge(Merge(CardConsoleOptions.TableCards(view, draft), WornDiscardChoice.TableCards(view, draft)),
                CardConsoleOptions.ReactionTableCards(view));

        // Both offers, the second winning a shared card; an empty side returns the other as it is.
        private static IReadOnlyDictionary<string, string> Merge(IReadOnlyDictionary<string, string> first,
            IReadOnlyDictionary<string, string> second)
        {
            if (second.Count == 0)
                return first;
            if (first.Count == 0)
                return second;
            var offers = new Dictionary<string, string>(first);
            foreach (var offer in second)
                offers[offer.Key] = offer.Value;
            return offers;
        }

        // UI-233: a finished game keeps one button in the console that reopens its end; it glows while the replay
        // can still be saved.
        private void ShowGameEnd(PlayerView view, TurnDraft draft)
        {
            board.Console.Status.SetOptions(GameEndConsole.Options, true);
            var reopen = board.Console.Status.Options.Q<Button>(GameEndConsole.Name);
            ChoiceGlow.Apply(reopen, board.GameEnd.Replay is ReplaySaveState.Ready or ReplaySaveState.Failed);
            board.TableEffects.Offer(CardConsoleOptions.TableCards(view, draft), false);
            OfferHuntRoll(null);
            board.TopBar.Opponent.OfferCards("");
        }

        private void Chosen(ConsoleOption option)
        {
            if (option.Kind == ConsoleOptionKind.OpenGameEnd)
            {
                board.GameEnd.Open();
                return;
            }
            choose(option);
        }

        // UI-431: a force section's click sends its choice with its region, as the region's click and the old button did.
        private void Force(string id) =>
            choose(new ConsoleOption { Id = id, Kind = ConsoleOptionKind.Effect, Region = board.Hover.Panel.Region });

        private void HuntRoll()
        {
            if (huntRoll != null)
                choose(huntRoll);
        }

        // UI-432: the red hand counter discards a random Free Peoples card for Worn with Sorrow and Toil.
        private void RandomHandCard() => choose(new ConsoleOption { Id = WornDiscardChoice.RandomHand, Kind = ConsoleOptionKind.Effect });

        // UI-272: the Lore card's click starts Lore before Dismay; UI-278: the Palantír's converts a Muster die.
        private void CardUse(string id)
        {
            if (PalantirConversion.CardUses(view, draft).ContainsKey(id))
                convert();
            else if (LoreChoice.CardUses(view).ContainsKey(id))
                choose(new ConsoleOption { Id = id, Kind = ConsoleOptionKind.Action, Command = CommandKind.UseLoreBeforeActions });
        }

        private void TableCard(string id)
        {
            var reaction = CardConsoleOptions.TableReaction(view, id);
            choose(reaction.Length > 0
                ? new ConsoleOption { Id = reaction, Kind = ConsoleOptionKind.Reaction }
                : new ConsoleOption { Id = id, Kind = ConsoleOptionKind.Effect });
        }

        public void Dispose()
        {
            board.Console.Status.OptionChosen -= Chosen;
            board.TableEffects.OfferChosen -= TableCard;
            board.TableEffects.UseChosen -= CardUse;
            board.Hunt.OfferChosen -= HuntRoll;
            board.Hover.Panel.ForceChosen -= Force;
            board.TopBar.Opponent.CardsChosen -= RandomHandCard;
        }
    }
}
