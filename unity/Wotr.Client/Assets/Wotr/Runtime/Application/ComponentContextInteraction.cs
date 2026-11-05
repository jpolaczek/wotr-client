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
        // UI-441: the characters with a skill usable now, built once per refresh and lit wherever they are drawn.
        public IReadOnlyDictionary<string, UsableSkills.Skill> Skills { get; private set; } = UsableSkills.None;

        public ComponentContextInteraction(BoardScreen board, Action<ComponentAction> select)
        {
            this.board = board;
            this.select = select;
            board.Components.Selected += select;
            board.Hover.Panel.Components.Selected += select;
            board.TableEffects.RemovalSelected += select;
            board.Characters.Actions.Selected += select;
            board.Characters.Actions.Selected += PresentPolitics;
        }

        // UI-366: a political ability used from its chip shows its step on the nation's track.
        private void PresentPolitics(ComponentAction item)
        {
            if (CharacterPolitics.IsPolitical(item.Character?.AbilityId))
                board.Nations.PresentOwnStep();
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
            var owned = components.Where(c => c.Enabled && idle).ToList();
            board.Map.HighlightOwners(owned.Select(c => c.Region), owned.Select(c => c.OwnerId));
            Skills = UsableSkills.Characters(view, draft, components, enabled);
            board.Characters.Actions.Apply(view, draft, components, enabled, Skills);
            LightSkills(view);
        }

        // UI-441 (owner): "the character always lights up whenever its skill can be used" — on the map (its miniature, or
        // the force an Initial king leads), in the Fellowship panel and among the kings, as on its Characters row
        // (CharacterRosterContext).
        private void LightSkills(PlayerView view)
        {
            var kings = view.Sovereigns.Select(king => king.Id).ToList();
            board.Map.LightSkills(UsableSkills.Lines(view, Skills, view.Characters.Select(c => c.Id).Concat(kings), MapSkillHint));
            board.Fellowship.LightSkills(UsableSkills.Lines(view, Skills, view.OwnCompanions.Select(c => c.Id), PortraitSkillHint));
            board.Sovereigns.LightSkills(UsableSkills.Lines(view, Skills, kings, PortraitSkillHint));
        }

        public const string MapSkillHint = "Click its glowing icon in Characters";
        public const string PortraitSkillHint = "Click to use it in the character's window";

        public void Dispose()
        {
            board.Components.Selected -= select;
            board.Hover.Panel.Components.Selected -= select;
            board.TableEffects.RemovalSelected -= select;
            board.Characters.Actions.Selected -= select;
            board.Characters.Actions.Selected -= PresentPolitics;
        }
    }
}
