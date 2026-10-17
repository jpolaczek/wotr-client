using System;
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
            board.Console.Status.SetOptions(CardConsoleOptions.For(view, draft), enabled);
            board.TableEffects.Offer(CardConsoleOptions.TableCards(view, draft), enabled);
        }

        private void Chosen(ConsoleOption option)
        {
            if (option.Kind == ConsoleOptionKind.OpenEffects)
            {
                board.TableEffects.Open();
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
