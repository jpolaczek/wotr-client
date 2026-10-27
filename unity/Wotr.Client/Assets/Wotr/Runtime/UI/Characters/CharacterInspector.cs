using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One instance serves the roster, public map figures and awakened sovereigns. UI-139: the card speaks in
    // symbols (status, statistics, ability chips) with tooltips; the full ability text unrolls on a click.
    public sealed class CharacterInspector
    {
        public VisualElement Root { get; } = new VisualElement { name = "character-inspector" };
        public VisualElement Actions { get; } = new VisualElement { name = "character-actions" };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "character-decision-host" };
        // UI-369: the chips light the abilities usable now and offer Click to use.
        public CharacterAbilityChips Abilities => abilities;
        public string FigureId { get; private set; } = "";
        public string DefinitionId => version;
        public event Action Changed;
        // UI-371: the player is about to change the window's content (an ability's text, a page). A version the decision
        // selects is not such a change: the window still moves clear of the panels the decision opens.
        public event Action ContentChanging;
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label edition = Text("character-edition");
        private readonly VisualElement statusIcon = new VisualElement { name = "character-status-icon" };
        private readonly Label state = Text("character-state");
        private readonly CharacterFactStrip stats = new CharacterFactStrip();
        private readonly CharacterAbilityChips abilities = new CharacterAbilityChips();
        private readonly Label limits = Text("character-limits");
        private readonly VisualElement limitsRow = BoardScreen.Row("character-limits-row");
        private readonly Label entry = Text("character-entry");
        private readonly VisualElement versions = BoardScreen.Row();
        private readonly Button locate;
        private CharacterRosterView row;
        private PlayerView view;
        private string version = "";
        private string status = "";

        public CharacterInspector(BoardPresentation data, Action<string> focus, Action close, Action pin)
        {
            this.data = data;
            Root.AddToClassList("character-inspector");
            Root.style.display = DisplayStyle.None;
            var header = BoardScreen.Row("character-header");
            FloatingWindow.MarkHandle(header);
            header.Add(title);
            edition.AddToClassList("character-edition");
            header.Add(edition);
            var pinButton = IconButton.Create("pin", "Pin", pin);
            pinButton.name = "pin-character";
            pinButton.tooltip = "Pin this character";
            header.Add(pinButton);
            var dismiss = IconButton.Create("close", "Close", close);
            dismiss.name = "close-character";
            header.Add(dismiss);
            Root.Add(header);
            var location = BoardScreen.Row("character-status");
            statusIcon.AddToClassList("character-status-icon");
            location.Add(statusIcon);
            location.Add(state);
            locate = IconButton.Create("locate", "Show on the map", () => focus(row.RegionId));
            locate.name = "locate-character";
            location.Add(locate);
            Root.Add(location);
            Root.Add(stats.Root);
            abilities.Changing += () => ContentChanging?.Invoke();
            Root.Add(abilities.Root);
            Root.Add(abilities.Pages);
            limits.AddToClassList("character-limits");
            var limitsIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            limitsIcon.AddToClassList("character-limits-icon");
            limitsRow.Add(limitsIcon);
            limitsRow.Add(limits);
            Root.Add(limitsRow);
            Root.Add(versions);
            Root.Add(entry);
            Root.Add(Actions);
            Root.Add(DecisionHost);
        }

        public bool Apply(PlayerView latest, string id)
        {
            var current = CharacterRosterPresentation.Inspect(latest, id);
            if (current == null)
                return false;
            view = latest;
            row = current;
            if (FigureId != id)
                version = CharacterRosterPresentation.Section(row) == CharacterRosterSection.NotInPlay
                    ? row.FutureVersions.FirstOrDefault()?.DefinitionId ?? "" : "";
            FigureId = id;
            if (version != row.CurrentDefinitionId && row.FutureVersions.All(v => v.DefinitionId != version))
                version = row.CurrentDefinitionId;
            if (version.Length == 0)
                version = row.FutureVersions.FirstOrDefault()?.DefinitionId ?? "";
            Refresh();
            return true;
        }

        private void Refresh()
        {
            title.text = version.Length > 0 ? CharacterLibrary.Name(version) : row.Title;
            edition.text = version.Length > 0 ? CharacterLibrary.Edition(version) : "";
            edition.tooltip = edition.text.Length > 0 ? "Card version · " + edition.text : "";
            edition.style.display = edition.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var region = data.Regions.TryGetValue(row.RegionId, out var definition) ? definition.Name : "";
            RefreshStatus(CharacterCardFacts.Status(row, region));
            locate.style.display = region.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var future = row.FutureVersions.FirstOrDefault(v => v.DefinitionId == version);
            stats.Apply(CharacterCardFacts.For(row, version, future == null));
            abilities.Apply(version);
            var explanation = row.Side == view.You ? ResourcePresentation.Explain(view, CharacterRosterVersions.Figure(row.FigureId)) : "";
            limits.text = explanation.Length > 0 ? "Limits" : "";
            limits.tooltip = explanation;
            ShowLimits(explanation.Length > 0);
            RefreshVersions();
            entry.text = future == null ? "" : "Cost · " + future.Entry.Cost +
                (future.ExtraDiceNextRecovery > 0 ? "\n+" + future.ExtraDiceNextRecovery + " action die at Recover" : "");
            entry.tooltip = future == null ? "" : CharacterRosterPresentation.Conditions(future.Entry) +
                (future.CouncilEntry == null ? "" : "\n\n" + CharacterRosterPresentation.Conditions(future.CouncilEntry));
            entry.EnableInClassList("legal-target", future?.Entry.CanAct == true || future?.CouncilEntry?.CanAct == true);
            entry.style.display = future == null ? DisplayStyle.None : DisplayStyle.Flex;
            Changed?.Invoke();
        }

        public void SelectVersion(string id)
        {
            if (row == null || id == version || id != row.CurrentDefinitionId && row.FutureVersions.All(item => item.DefinitionId != id))
                return;
            version = id;
            Refresh();
        }

        public void SetDecisionContext(bool active)
        {
            Root.EnableInClassList("character-decision-active", active);
            ShowLimits(!active && !string.IsNullOrEmpty(limits.text));
            abilities.Pages.style.display = !active && abilities.Paged ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowLimits(bool shown)
        {
            var display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            limitsRow.style.display = display;
            limits.style.display = display;
        }

        private void RefreshStatus(CharacterStatus next)
        {
            statusIcon.RemoveFromClassList("character-status-" + status);
            statusIcon.AddToClassList("character-status-" + next.Key);
            status = next.Key;
            statusIcon.tooltip = next.Tooltip;
            state.text = next.Text;
            state.tooltip = next.Tooltip;
        }

        private void RefreshVersions()
        {
            var ids = row.FutureVersions.Select(v => v.DefinitionId).ToList();
            if (row.CurrentDefinitionId.Length > 0)
                ids.Insert(0, row.CurrentDefinitionId);
            var buttons = versions.Children().OfType<Button>().ToList();
            var focusedVersion = buttons.FirstOrDefault(b => b == Root.panel?.focusController.focusedElement)?.userData as string;
            if (!buttons.Select(b => b.userData as string).SequenceEqual(ids))
            {
                versions.Clear();
                foreach (var id in ids)
                {
                    var button = BoardScreen.Button(CharacterLibrary.Edition(id) + " · " + CharacterLibrary.ShortName(id), () =>
                    {
                        version = id;
                        Refresh();
                    });
                    button.name = "character-version-" + id;
                    button.userData = id;
                    button.tooltip = CharacterLibrary.Name(id);
                    versions.Add(button);
                }
            }
            versions.style.display = ids.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var button in versions.Children().OfType<Button>())
                button.EnableInClassList("selected", (string)button.userData == version);
            if (focusedVersion != null)
            {
                var replacement = ids.Count > 1 ? versions.Children().OfType<Button>()
                    .FirstOrDefault(b => (string)b.userData == focusedVersion) : null;
                (replacement ?? (locate.style.display.value != DisplayStyle.None ? locate : Root.Q<Button>("pin-character"))).Focus();
            }
        }

        private static Label Text(string name) => new Label { name = name, enableRichText = false };
    }
}
