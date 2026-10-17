using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CompanionSetupChoices
    {
        private readonly TurnDraft draft;
        private readonly ActionOption option;
        private readonly Action<Action> edit;
        private readonly CompanionSetupPanel host;
        private readonly Dictionary<string, Button> portraits = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> guides = new Dictionary<string, Button>();
        private readonly VisualElement comparison = BoardScreen.Row("setup-comparison");
        private readonly Label missing = new Label { name = "setup-missing" };
        private CompanionSetupCard original;
        private CompanionSetupCard alternate;
        private string compared = "";

        public CompanionSetupChoices(VisualElement root, ActionOption option, TurnDraft draft,
            Action<Action> edit, CompanionSetupPanel host)
        {
            this.option = option;
            this.draft = draft;
            this.edit = edit;
            this.host = host;
            var choices = new VisualElement { name = "companion-setup-choices" };
            root.Add(choices);
            AddPortraits(choices);
            choices.Add(comparison);
            AddGuides(choices);
            choices.Add(missing);
            Compare(option.CompanionVersions.Any(group => group.FigureId == host?.ComparedFigure)
                ? host.ComparedFigure : option.CompanionVersions.FirstOrDefault()?.FigureId ?? "");
        }

        private void AddPortraits(VisualElement parent)
        {
            var row = BoardScreen.Row("setup-portraits");
            parent.Add(row);
            foreach (var group in option.CompanionVersions)
            {
                var id = group.FigureId;
                var button = BoardScreen.Button("", () => Compare(id));
                button.name = "setup-portrait-" + id;
                button.Add(new Label(CharacterLibrary.ShortName(id)));
                var version = new Label { name = "setup-portrait-version" };
                version.AddToClassList("setup-portrait-version");
                button.Add(version);
                portraits.Add(id, button);
                row.Add(button);
            }
        }

        private void AddGuides(VisualElement parent)
        {
            var row = BoardScreen.Row("setup-guides");
            row.Add(new Label("Starting Guide"));
            foreach (var id in option.Targets)
            {
                var button = BoardScreen.Button(CharacterLibrary.ShortName(id),
                    () => edit(() => draft.Change(payload => payload.GuideId = id)));
                button.name = "starting-guide-" + id;
                guides.Add(id, button);
                row.Add(button);
            }
            parent.Add(row);
        }

        private void Compare(string figure)
        {
            if (compared == figure && comparison.childCount > 0)
                return;
            var group = option.CompanionVersions.FirstOrDefault(candidate => candidate.FigureId == figure);
            if (group == null)
                return;
            compared = figure;
            if (host != null)
                host.ComparedFigure = figure;
            comparison.Clear();
            original = Card(group, group.BaseId, "Original");
            alternate = Card(group, group.AlternateId, "Alternate");
            comparison.Add(original.Root);
            comparison.Add(alternate.Root);
            Refresh();
        }

        private CompanionSetupCard Card(CompanionVersionOption group, string id, string caption) =>
            new CompanionSetupCard(id, CharacterLibrary.Edition(id) + " · " + caption,
                () => edit(() => CouncilDraft.Version(draft, group, id)));

        public void Refresh()
        {
            foreach (var group in option.CompanionVersions)
            {
                var button = portraits[group.FigureId];
                var version = CouncilDraft.Version(draft.Payload, group);
                button.EnableInClassList("selected", compared == group.FigureId);
                button.EnableInClassList("setup-version-chosen", version.Length > 0);
                button.Q<Label>("setup-portrait-version").text = version.Length == 0 ? "○ Choose version"
                    : "✓ " + (version == group.BaseId ? "Original" : "Alternate");
                button.tooltip = version.Length == 0 ? "Compare versions · A choice is required"
                    : "Compare versions · Chosen: " + CharacterLibrary.Name(version);
            }
            var selected = option.CompanionVersions.FirstOrDefault(group => group.FigureId == compared);
            var selectedVersion = selected == null ? "" : CouncilDraft.Version(draft.Payload, selected);
            original?.Select(original.DefinitionId == selectedVersion);
            alternate?.Select(alternate.DefinitionId == selectedVersion);
            foreach (var entry in guides)
            {
                entry.Value.EnableInClassList("selected", draft.Payload.GuideId == entry.Key);
                var version = CompanionSetupPresentation.Definition(draft, option, entry.Key);
                var ability = CharacterAbilityLibrary.For(version).FirstOrDefault(candidate => candidate.Id.Contains("guide"));
                entry.Value.tooltip = "Start with " + CharacterLibrary.ShortName(entry.Key) + " as Guide" +
                    (ability == null ? "" : "\n" + ability.Text + "\n" + ability.Source);
            }
            missing.text = CompanionSetupPresentation.Missing(draft, option);
            missing.style.display = missing.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
