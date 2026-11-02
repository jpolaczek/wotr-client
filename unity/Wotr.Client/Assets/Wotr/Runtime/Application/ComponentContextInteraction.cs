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
        // UI-441: the characters with a skill usable now, built once per refresh and lit wherever they are drawn.
        public IReadOnlyDictionary<string, UsableSkills.Skill> Skills { get; private set; } = UsableSkills.None;

        public ComponentContextInteraction(BoardScreen board, Action<ComponentAction> select)
        {
            this.board = board;
            this.select = select;
            board.Components.Selected += select;
            board.Hover.Panel.Components.Selected += select;
            board.TableEffects.RemovalSelected += select;
            board.ResourceDecision.CouncilChoices.Selected += select;
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
            Skills = UsableSkills.Characters(view, draft, components, enabled);
            board.Characters.Actions.Apply(view, draft, components, enabled, Skills);
            LightSkills(view);
        }

        // UI-441 (owner): "the character always lights up whenever its skill can be used" — on the map (the force it
        // stands in), in the Fellowship panel and among the kings, as on its Characters row (CharacterRosterContext).
        private void LightSkills(PlayerView view)
        {
            var figures = UsableSkills.Lines(view, Skills, view.Characters.Select(c => c.Id), MapSkillHint);
            board.Map.LightSkills(view.Characters.Where(c => figures.ContainsKey(c.Id))
                .GroupBy(c => (c.Location, c.Side, c.Garrison)).ToDictionary(group => group.Key, group => figures[group.First().Id]));
            board.Fellowship.LightSkills(UsableSkills.Lines(view, Skills, view.OwnCompanions.Select(c => c.Id), PortraitSkillHint));
            board.Sovereigns.LightSkills(UsableSkills.Lines(view, Skills, view.Sovereigns.Select(king => king.Id), PortraitSkillHint));
        }

        public const string MapSkillHint = "Click its glowing icon in Characters";
        public const string PortraitSkillHint = "Click to use it in the character's window";

        public void Dispose()
        {
            board.Components.Selected -= select;
            board.Hover.Panel.Components.Selected -= select;
            board.TableEffects.RemovalSelected -= select;
            board.ResourceDecision.CouncilChoices.Selected -= select;
            board.Characters.Actions.Selected -= select;
            board.Characters.Actions.Selected -= PresentPolitics;
        }
    }
}
