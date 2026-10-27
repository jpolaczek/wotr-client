using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One inspector hosts the existing draft; browsing another figure never retargets it.
    public sealed class CharacterRosterContext
    {
        public event Action<ComponentAction> Selected;
        public Action OpenReaction { get; set; }
        public string DecisionFigure { get; private set; } = "";
        // UI-243: the figure whose entry (or change into its next version) waits for a region on the map.
        public string EnteringFigure { get; private set; } = "";
        private readonly CharacterRosterPanel panel;
        private readonly ComponentActionView actions = new ComponentActionView();
        private readonly Button reaction;
        private readonly Button resume;
        private List<ComponentAction> components = new List<ComponentAction>();
        private PlayerView view;
        private TurnDraft draft;
        private VisualElement tray, dock;
        private string observed = "";
        private string decisionVersion = "";
        private bool enabled;

        public CharacterRosterContext(CharacterRosterPanel panel)
        {
            this.panel = panel;
            actions.Label = item => item.Kind == CommandKind.EnterCharacter ? EntryLabel(item) :
                item.Character?.Title ?? (item.Kind switch
                {
                    CommandKind.MoveArmies => "Move armies",
                    CommandKind.AttackArmy => "Attack",
                    CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters => "Move characters",
                    CommandKind.MoveFellowship => "Move Fellowship",
                    CommandKind.SeparateCompanions => "Separate companions",
                    CommandKind.UseNarya => "Narya",
                    _ => item.Owner
                });
            actions.Selected += item => Selected?.Invoke(item);
            panel.Inspector.Abilities.Use += item => Selected?.Invoke(item);
            reaction = BoardScreen.Button("Open decision", () => OpenReaction?.Invoke());
            reaction.name = "character-reaction-link";
            resume = BoardScreen.Button("Resume character action", OpenDecision);
            resume.name = "character-resume-action";
            panel.Inspector.Changed += RefreshInspector;
            RefreshInspector();
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void Apply(PlayerView latest, TurnDraft selection, IEnumerable<ComponentAction> choices, bool canEdit)
        {
            view = latest;
            draft = selection;
            // UI-271: an ability used on its own object is not repeated as an inspector button.
            components = choices.Where(item => !CharacterAbilityPlaces.OwnPlace(item)).ToList();
            enabled = canEdit;
            DecisionFigure = CharacterRosterActions.OwnsDecision(view, draft)
                ? CharacterRosterActions.DecisionFigure(view, draft) : "";
            EnteringFigure = CharacterRosterActions.EnteringFigure(view, draft);
            decisionVersion = DecisionFigure.Length == 0 ? "" : draft.Kind == CommandKind.EnterCharacter
                ? draft.Payload.CharacterId
                : view.OwnCharacterRoster.First(row => row.FigureId == DecisionFigure).CurrentDefinitionId;
            if (DecisionFigure.Length > 0 && tray != null && tray.parent != panel.DecisionHost)
                panel.DecisionHost.Add(tray);
            else if (DecisionFigure.Length == 0 && tray?.parent == panel.DecisionHost)
                dock.Add(tray);
            var key = DecisionFigure.Length == 0 ? "" : view.MatchId + "/" + view.Decision.Id + "/" +
                draft.Kind + "/" + draft.Payload.CharacterId + "/" + draft.Payload.AbilityId;
            if (key.Length > 0 && observed != key)
                OpenDecision();
            observed = key;
            RefreshInspector();
            panel.RefreshSelection();
        }

        public bool Available(string id) => components.Any(item => item.Enabled && CharacterRosterActions.Row(view, item) == id);

        // UI-108: the one entry the chosen cost allows for this figure. Several versions or entries stay an explicit
        // choice in the inspector.
        public ComponentAction Entry(string id)
        {
            var entries = Entries(id);
            return entries.Count == 1 ? entries[0] : null;
        }

        private List<ComponentAction> Entries(string id) => view == null ? new List<ComponentAction>() : components
            .Where(item => item.Enabled && item.Kind == CommandKind.EnterCharacter && CharacterRosterActions.Row(view, item) == id)
            .ToList();

        private string EntryLabel(ComponentAction item) => view?.OwnCharacterRoster
            .SelectMany(row => row.FutureVersions).FirstOrDefault(version => version.DefinitionId == item.Character?.CharacterId)?.Mode switch
        {
            CharacterEntryMode.Transformation => "Transform",
            CharacterEntryMode.ReturnAsNewVersion => "Return as " + item.Owner,
            _ => "Enter"
        };

        public void OpenDecision()
        {
            if (DecisionFigure.Length == 0)
                return;
            panel.Show(DecisionFigure, true);
            panel.Inspector.SelectVersion(decisionVersion);
        }

        private void RefreshInspector()
        {
            var inspector = panel.Inspector;
            var active = DecisionFigure.Length > 0 && inspector.FigureId == DecisionFigure &&
                inspector.DefinitionId == decisionVersion;
            panel.DecisionHost.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            inspector.SetDecisionContext(active);
            var choices = (view == null || draft?.Kind != null ? Enumerable.Empty<ComponentAction>() :
                CharacterRosterActions.For(view, inspector.FigureId, inspector.DefinitionId, components))
                .Where(item => item.Kind != CommandKind.EnterCharacter || Entries(inspector.FigureId).Count > 1).ToList();
            // UI-369: an ability with a chip is used from its chip; only the other actions keep a button.
            var buttons = inspector.Abilities.Usable(choices, enabled, id => DieUse.Name(view, id));
            actions.Apply(buttons, enabled);
            Present(actions.Root, buttons.Count > 0);
            Present(resume, DecisionFigure.Length > 0 && !active);
            Present(reaction, view != null && CharacterRosterActions.HasReaction(view, inspector.FigureId));
        }

        private void Present(VisualElement element, bool visible)
        {
            if (visible && element.parent == null)
                panel.Inspector.Actions.Add(element);
            else if (!visible)
                element.RemoveFromHierarchy();
        }
    }
}
