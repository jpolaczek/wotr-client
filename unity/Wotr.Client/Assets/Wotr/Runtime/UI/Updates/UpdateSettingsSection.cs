using System;
using UnityEngine.UIElements;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client
{
    // DIST-07: the Updates row of Settings — this copy's version, platform and build, the updater's state, Check now,
    // the action of the moment (Download update, Cancel download, Restart and update, Game page) and Repair
    // installation (DS23), which asks once before it downloads the running version again. A withdrawn version
    // cannot be repaired; the row offers the newer release instead.
    public sealed class UpdateSettingsSection
    {
        public VisualElement Root { get; } = new VisualElement { name = "update-settings" };
        public event Action Check;
        public event Action Accept;
        public event Action Cancel;
        public event Action Install;
        public event Action GamePage;
        public event Action Repair;
        private readonly Label build = new Label { name = "update-build" };
        private readonly Label status = new Label { name = "update-status" };
        private readonly Button check;
        private readonly Button primary;
        private readonly Button repair;
        private readonly VisualElement confirmation = new VisualElement { name = "update-repair-confirmation" };
        private readonly Label question = new Label { name = "update-repair-question" };
        private Action primaryAction;

        public UpdateSettingsSection(ClientBuildInfo identity, string installedVersion)
        {
            Root.AddToClassList("update-settings");
            Root.Add(new Label("Game version"));
            build.text = Describe(identity, installedVersion);
            Root.Add(build);
            status.AddToClassList("update-status");
            Root.Add(status);
            var actions = BoardScreen.Row("update-actions");
            check = Control(actions, "update-check", "Check now", () => Check?.Invoke());
            primary = Control(actions, "update-primary", "", () => primaryAction?.Invoke());
            repair = Control(actions, "update-repair", "Repair installation", Ask);
            Root.Add(actions);
            confirmation.AddToClassList("update-repair-confirmation");
            confirmation.style.display = DisplayStyle.None;
            question.AddToClassList("update-repair-question");
            confirmation.Add(question);
            var answers = BoardScreen.Row("update-actions");
            Control(answers, "update-repair-confirm", "Repair", () =>
            {
                confirmation.style.display = DisplayStyle.None;
                Repair?.Invoke();
            });
            Control(answers, "update-repair-cancel", "Cancel", () => confirmation.style.display = DisplayStyle.None);
            confirmation.Add(answers);
            Root.Add(confirmation);
        }

        public void Render(UpdateSnapshot update, string ownVersion, bool exiting, string exitError)
        {
            var withdrawn = update.Plan != null && update.Plan.RepairOffer == null && update.Plan.Mode != UpdateMode.ServerBehind;
            status.text = exitError.Length > 0 ? exitError : UpdateTexts.Status(update) +
                (withdrawn && update.Plan.Offer != null ? $" Version {ownVersion} was withdrawn; update to {update.Version} instead." : "");
            var busy = exiting || update.Status == UpdateStatus.Applying;
            check.SetEnabled(!busy && update.Status != UpdateStatus.Checking && update.Status != UpdateStatus.Downloading);
            if (!update.CanInstall && update.Version.Length > 0 && update.Status != UpdateStatus.Idle)
                Primary("Game page", () => GamePage?.Invoke());
            else if (update.Status == UpdateStatus.Available || (update.Status == UpdateStatus.Failed && update.Version.Length > 0))
                Primary("Download update", () => Accept?.Invoke());
            else if (update.Status == UpdateStatus.Downloading && (!update.Required || update.Repair))
                Primary("Cancel download", () => Cancel?.Invoke());
            else if (update.Status == UpdateStatus.Ready)
                Primary(update.Repair ? "Restart and repair" : "Restart and update", () => Install?.Invoke());
            else
                Primary(null, null);
            primary.SetEnabled(!busy);
            repair.SetEnabled(!busy && update.CanInstall && !withdrawn && update.Plan?.RepairOffer != null &&
                update.Status != UpdateStatus.Downloading);
            repair.tooltip = !update.CanInstall ? "This copy was not installed by the game's installer. Install it from the game page."
                : withdrawn ? "This version was withdrawn. Update to the newest version instead."
                : "Downloads this version again and reinstalls it. Settings and saved games stay.";
            question.text = $"Download version {ownVersion} again and reinstall it? The game restarts when you choose " +
                "Restart and repair. Settings and saved games stay.";
            if (!repair.enabledSelf)
                confirmation.style.display = DisplayStyle.None;
        }

        private void Primary(string label, Action action)
        {
            primaryAction = action;
            primary.text = label ?? "";
            primary.style.display = label == null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void Ask()
        {
            confirmation.style.display = DisplayStyle.Flex;
            confirmation.Q<Button>("update-repair-cancel").Focus();
        }

        private static string Describe(ClientBuildInfo identity, string installedVersion)
        {
            var commit = identity.SourceCommit.Length >= 7 ? " · build " + identity.SourceCommit.Substring(0, 7) : "";
            var kind = identity.Development ? " · development build"
                : installedVersion == null ? " · not installed by the game's installer" : "";
            return $"{identity.Version} · {UpdateTexts.Platform(identity.Platform)}{commit}{kind}";
        }

        private static Button Control(VisualElement row, string name, string text, Action action)
        {
            var button = BoardScreen.Button(text, action);
            button.name = name;
            button.AddToClassList("update-control");
            row.Add(button);
            return button;
        }
    }
}
