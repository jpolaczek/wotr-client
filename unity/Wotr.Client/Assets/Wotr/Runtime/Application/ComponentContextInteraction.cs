using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Places server-projected options at their owner; never creates legal actions.
    public sealed class ComponentContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<ComponentAction> select;
        // The components of the last refresh, for the contexts that place some of them on their objects (UI-271).
        public IReadOnlyList<ComponentAction> Current { get; private set; } = new List<ComponentAction>();

        public ComponentContextInteraction(BoardScreen board, Action<ComponentAction> select)
        {
            this.board = board;
            this.select = select;
            board.Components.Selected += select;
            board.Hover.Panel.Components.Selected += select;
            board.TableEffects.RemovalSelected += select;
            board.Characters.Actions.Selected += select;
        }

        public void Refresh(PlayerView view, TurnDraft draft, bool enabled)
        {
            var components = ComponentActions.For(view, draft).ToList();
            Current = components;
            var idle = draft.Kind == null;
            var council = ResourcePresentation.CouncilPending(draft);
            // UI-108: a character enters from its row in the Characters list, never from a button in the decision dock.
            // UI-160: Pass and discarding a die are permanent actions of the turn console. UI-347: a Council token's use
            // starts on its medallion (D13), and Narya on its nation's row and Gandalf's Characters row.
            board.Components.Apply(components.Where(c => c.CardId.Length == 0 && c.Region.Length == 0 && idle &&
                c.Kind is not (CommandKind.UseCouncilToken or CommandKind.EnterCharacter or CommandKind.Pass or CommandKind.DiscardDie or
                    CommandKind.UseNarya) &&
                !council), enabled);
            // UI-271: an ability used on its own object (the Balrog's figure, Gothmog's Reserve row) has no button here.
            board.Hover.Panel.Context(components.Where(c => c.Region.Length > 0 && idle && !CharacterAbilityPlaces.OwnPlace(c)), enabled);
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
            board.Characters.Actions.Selected -= select;
        }
    }
}
