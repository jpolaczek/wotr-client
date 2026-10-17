using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Places server-projected options at their owner; never creates legal actions.
    public sealed class ComponentContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<ComponentAction> select;

        public ComponentContextInteraction(BoardScreen board, Action<ComponentAction> select)
        {
            this.board = board;
            this.select = select;
            board.Components.Selected += select;
            board.Hover.Panel.Components.Selected += select;
            board.TableEffects.RemovalSelected += select;
            board.ResourceDecision.CouncilChoices.Selected += select;
            board.Characters.Actions.Selected += select;
        }

        public void Refresh(PlayerView view, TurnDraft draft, bool enabled)
        {
            var components = ComponentActions.For(view, draft).ToList();
            var idle = draft.Kind == null;
            var council = ResourcePresentation.CouncilPending(draft);
            board.ResourceDecision.CouncilChoices.Apply(council ? components.Where(c => c.Kind != CommandKind.EnterCharacter)
                : Enumerable.Empty<ComponentAction>(), enabled);
            // UI-108: a character enters from its row in the Characters list, never from a button in the decision dock.
            // UI-160: Pass and discarding a die are permanent actions of the turn console.
            board.Components.Apply(components.Where(c => c.CardId.Length == 0 && c.Region.Length == 0 && idle &&
                c.Kind is not (CommandKind.UseCouncilToken or CommandKind.EnterCharacter or CommandKind.Pass or CommandKind.DiscardDie) &&
                !council), enabled);
            board.Hover.Panel.Context(components.Where(c => c.Region.Length > 0 && idle), enabled);
            board.TableEffects.Context(components.Where(c => c.CardId.Length > 0), enabled);
            board.TableEffects.Draft(view, draft);
            board.Map.HighlightOwners(components.Where(c => c.Enabled && idle).Select(c => c.Region));
            board.Characters.Actions.Apply(view, draft, components, enabled);
        }

        public void Dispose()
        {
            board.Components.Selected -= select;
            board.Hover.Panel.Components.Selected -= select;
            board.TableEffects.RemovalSelected -= select;
            board.ResourceDecision.CouncilChoices.Selected -= select;
            board.Characters.Actions.Selected -= select;
        }
    }
}
