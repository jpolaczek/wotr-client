using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-269: the two version cards of the companion the Fellowship panel compares, side by side under its portraits;
    // a click on a card chooses that version. The portraits (two-faces badge) choose which companion is compared and
    // their crowns the starting Guide; the console's ✓ sends the choice once it is complete.
    public sealed class CompanionSetupChoices
    {
        private readonly TurnDraft draft;
        private readonly ActionOption option;
        private readonly Action<Action> edit;
        private readonly VersionComparison versions;
        private readonly VisualElement comparison = BoardScreen.Row("setup-comparison");
        // UI-345: the version card not chosen glows (CompanionSetupCard) and pulses in the shared rhythm.
        private readonly ChoicePulse pulse;
        private CompanionSetupCard original;
        private CompanionSetupCard alternate;
        private string compared = "";

        public CompanionSetupChoices(VisualElement root, ActionOption option, TurnDraft draft,
            Action<Action> edit, VersionComparison versions)
        {
            this.option = option;
            this.draft = draft;
            this.edit = edit;
            this.versions = versions;
            pulse = new ChoicePulse(comparison);
            var choices = new VisualElement { name = "companion-setup-choices" };
            root.Add(choices);
            var hint = new Label("Click a portrait marked with two faces to compare its versions; a crown makes the starting Guide.")
                { name = "setup-hint" };
            hint.AddToClassList("draft-status");
            choices.Add(hint);
            choices.Add(comparison);
            Compare(First(versions?.Figure ?? ""));
            if (versions != null)
                versions.Show = Compare;
        }

        // UI-269/UI-270: the version cards and the homelands' panel part keep the keyboard to themselves; Enter there never
        // sends the private setup.
        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name is "companion-setup-choices" or "council-homeland-choices")
                    return true;
            return false;
        }

        // The compared companion: the one asked for, else the first whose version is still required, else the first.
        private string First(string asked)
        {
            if (option.CompanionVersions.Any(group => group.FigureId == asked))
                return asked;
            var required = option.CompanionVersions.FirstOrDefault(group => CouncilDraft.Version(draft.Payload, group).Length == 0);
            return (required ?? option.CompanionVersions.FirstOrDefault())?.FigureId ?? "";
        }

        private void Compare(string figure)
        {
            if (compared == figure && comparison.childCount > 0)
                return;
            var group = option.CompanionVersions.FirstOrDefault(candidate => candidate.FigureId == figure);
            if (group == null)
                return;
            compared = figure;
            versions?.Showing(figure);
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
            var selected = option.CompanionVersions.FirstOrDefault(group => group.FigureId == compared);
            var selectedVersion = selected == null ? "" : CouncilDraft.Version(draft.Payload, selected);
            original?.Select(original.DefinitionId == selectedVersion);
            alternate?.Select(alternate.DefinitionId == selectedVersion);
            pulse.Run(original != null);
        }
    }
}
