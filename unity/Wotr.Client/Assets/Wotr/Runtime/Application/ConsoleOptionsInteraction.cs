using System;
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

        public ConsoleOptionsInteraction(BoardScreen board, Action<ConsoleOption> choose)
        {
            this.board = board;
            this.choose = choose;
            board.Console.Status.OptionChosen += Chosen;
            board.TableEffects.OfferChosen += TableCard;
        }

        public void Refresh(PlayerView view, TurnDraft draft, bool enabled)
        {
            if (view.Winner.HasValue)
            {
                ShowGameEnd(view, draft);
                return;
            }
            board.Console.Status.SetOptions(CardConsoleOptions.For(view, draft), enabled);
            board.TableEffects.Offer(CardConsoleOptions.TableCards(view, draft), enabled);
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

        private void TableCard(string id) => choose(new ConsoleOption { Id = id, Kind = ConsoleOptionKind.Effect });

        public void Dispose()
        {
            board.Console.Status.OptionChosen -= Chosen;
            board.TableEffects.OfferChosen -= TableCard;
        }
    }
}
