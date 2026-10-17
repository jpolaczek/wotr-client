using System;
using UnityEngine.UIElements;
using Wotr.Client.Updates;

namespace Wotr.Client
{
    // DIST-07: the non-blocking update bar over every screen (DS15/DS22). An optional release offers Download update
    // / Later; a download shows its progress with Cancel; a scheduled minimum counts down to its deadline; a ready
    // package offers Restart and update. Root is a lane that never takes the pointer, so a gesture on the board
    // passes beside the bar; nothing here opens or closes on its own while the player decides.
    public sealed class UpdateNotice
    {
        public VisualElement Root { get; } = new VisualElement { name = "update-lane", pickingMode = PickingMode.Ignore };
        public VisualElement Bar { get; } = new VisualElement { name = "update-notice" };
        public event Action Accept;
        public event Action Later;
        public event Action Cancel;
        public event Action Install;
        public event Action Retry;
        public event Action GamePage;
        private readonly Label text = new Label { name = "update-notice-text" };
        private readonly Button first;
        private readonly Button second;
        private Choice firstChoice;
        private Choice secondChoice;

        private enum Choice
        {
            None,
            Accept,
            Later,
            Cancel,
            Install,
            Retry,
            GamePage
        }

        public UpdateNotice()
        {
            Root.AddToClassList("update-lane");
            Root.style.display = DisplayStyle.None;
            Bar.AddToClassList("update-notice");
            Root.Add(Bar);
            text.AddToClassList("update-notice-text");
            Bar.Add(text);
            first = Control("update-notice-first", () => Raise(firstChoice));
            second = Control("update-notice-second", () => Raise(secondChoice));
        }

        public bool IsShown => Root.style.display.value == DisplayStyle.Flex;

        public void Render(UpdateSnapshot update, DateTimeOffset now, string exitError)
        {
            var deadline = update.Plan?.Deadline;
            if (update.Blocked)
                Hide();
            else if (update.Mode == UpdateMode.RequiredSoon && deadline.HasValue)
            {
                var detail = update.Status == UpdateStatus.Downloading ? " · Downloading " + UpdateTexts.Percent(update.Progress)
                    : update.Status == UpdateStatus.Ready ? " · Ready" : "";
                Show($"Update required {UpdateTexts.Deadline(deadline.Value, now)}{detail}.", exitError,
                    update.Status == UpdateStatus.Ready ? Choice.Install
                    : update.Status == UpdateStatus.Failed ? Choice.Retry
                    : !update.CanInstall ? Choice.GamePage : Choice.None,
                    Choice.None);
                text.tooltip = update.Status == UpdateStatus.Failed ? update.Message
                    : "Your game continues until then. The update installs only when you choose Restart and update.";
            }
            else if (update.Status == UpdateStatus.Available && update.Mode == UpdateMode.Optional && !update.Dismissed)
            {
                var size = UpdateTexts.Size(update.Manifest?.Package.Size ?? 0);
                Show($"Version {update.Version} is available{(size.Length > 0 ? " · " + size : "")}.", "",
                    update.CanInstall ? Choice.Accept : Choice.GamePage, Choice.Later);
                text.tooltip = update.Manifest?.Notes ?? "";
            }
            else if (update.Status == UpdateStatus.Downloading && !update.Required)
            {
                Show(UpdateTexts.Status(update), "", Choice.Cancel, Choice.None);
                text.tooltip = "The download continues in the background. Cancel keeps what was downloaded.";
            }
            else if (update.Status == UpdateStatus.Ready && !update.Dismissed)
            {
                Show(update.Repair ? "The repair is ready." : $"Version {update.Version} is ready.", exitError,
                    Choice.Install, Choice.Later);
                first.text = update.Repair ? "Restart and repair" : "Restart and update";
                text.tooltip = "Your game is saved first. The game restarts with the new version.";
            }
            else if (update.Status == UpdateStatus.Failed && update.Requested && !update.Required && !update.Dismissed)
            {
                Show(update.Message, "", Choice.Retry, Choice.Later);
                text.tooltip = "";
            }
            else
                Hide();
        }

        private void Show(string message, string exitError, Choice one, Choice two)
        {
            text.text = exitError.Length > 0 ? exitError : message;
            firstChoice = one;
            secondChoice = two;
            Label(first, one);
            Label(second, two);
            Root.style.display = DisplayStyle.Flex;
        }

        private static void Label(Button button, Choice choice)
        {
            button.text = choice switch
            {
                Choice.Accept => "Download update",
                Choice.Later => "Later",
                Choice.Cancel => "Cancel",
                Choice.Install => "Restart and update",
                Choice.Retry => "Retry",
                Choice.GamePage => "Game page",
                _ => ""
            };
            button.style.display = choice == Choice.None ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void Raise(Choice choice)
        {
            switch (choice)
            {
                case Choice.Accept: Accept?.Invoke(); break;
                case Choice.Later: Later?.Invoke(); break;
                case Choice.Cancel: Cancel?.Invoke(); break;
                case Choice.Install: Install?.Invoke(); break;
                case Choice.Retry: Retry?.Invoke(); break;
                case Choice.GamePage: GamePage?.Invoke(); break;
            }
        }

        private void Hide() => Root.style.display = DisplayStyle.None;

        private Button Control(string name, Action action)
        {
            var button = BoardScreen.Button("", action);
            button.name = name;
            button.AddToClassList("update-notice-control");
            Bar.Add(button);
            return button;
        }
    }
}
