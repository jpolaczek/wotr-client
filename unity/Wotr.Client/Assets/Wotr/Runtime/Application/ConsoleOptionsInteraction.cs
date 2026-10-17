using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-214 (CARD_CHOICES_UX): a card's object-less options and a reaction's responses as buttons in the console, and
    // The Nazgûl Strike's table cards at their × in the Effects panel. A click that completes the decision sends it.
    public sealed class ConsoleOptionsInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<ConsoleOption> choose;
        private PlayerView view;

        public ConsoleOptionsInteraction(BoardScreen board, Action<ConsoleOption> choose)
        {
            this.board = board;
            this.choose = choose;
            board.Console.Status.OptionChosen += Chosen;
            board.TableEffects.OfferChosen += TableCard;
            board.Hover.Panel.ForceChosen += Force;
        }

        public void Refresh(PlayerView view, TurnDraft draft, bool enabled)
        {
            if (view.Winner.HasValue)
            {
                ShowGameEnd(view, draft);
                return;
            }
            this.view = view;
            var panel = board.Hover.Panel;
            panel.ForceChoice = (side, garrison) => enabled ? RegionForceChoices.For(view, draft, panel.Region, garrison) : "";
            panel.RefreshForces();
            board.Console.Status.SetOptions(CardConsoleOptions.For(view, draft), enabled);
            board.TableEffects.Offer(TableOffers(view, draft), enabled);
        }

        // The Nazgûl Strike's table cards and, outside a battle or the Hunt, a table card's own reaction (UI-247).
        private static IReadOnlyDictionary<string, string> TableOffers(PlayerView view, TurnDraft draft)
        {
            var reactions = CardConsoleOptions.ReactionTableCards(view);
            if (reactions.Count == 0)
                return CardConsoleOptions.TableCards(view, draft);
            var offers = new Dictionary<string, string>(CardConsoleOptions.TableCards(view, draft));
            foreach (var reaction in reactions)
                offers[reaction.Key] = reaction.Value;
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
        }

        private void Chosen(ConsoleOption option)
        {
            if (option.Kind == ConsoleOptionKind.OpenEffects)
            {
                board.TableEffects.Open();
                return;
            }
            if (option.Kind == ConsoleOptionKind.OpenGameEnd)
            {
                board.GameEnd.Open();
                return;
            }
            choose(option);
        }

        private void Force(string id) => choose(new ConsoleOption { Id = id, Kind = ConsoleOptionKind.Effect });

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
            board.Hover.Panel.ForceChosen -= Force;
        }
    }
}
