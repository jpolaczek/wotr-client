using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-04: the saved replays — newest games first, each with Play, Export and Delete in fixed places, and Import
    // at the foot (Back too when the page stands before sign-in). A damaged file is its own row with the reason; only
    // Delete works on it. Rows are rebuilt only when the list changes.
    public sealed class ReplayLibraryPage
    {
        public VisualElement Root { get; } = new VisualElement { name = "replay-library" };
        public Label Status { get; } = new Label { name = "replay-status" };
        public event Action<ReplayEntry> Play;
        public event Action<ReplayEntry> Export;
        public event Action<ReplayEntry> Delete;
        public event Action Import;
        public event Action Back;
        private readonly ScrollView scroll = new ScrollView(ScrollViewMode.Vertical) { name = "replay-scroll" };
        private readonly Label empty = new Label("No saved replays yet. Save one at the end of a game, or import a replay file.") { name = "replay-empty" };
        private readonly List<Button> actions = new List<Button>();
        private readonly Button import;
        private string shown = "";

        public ReplayLibraryPage(bool standalone)
        {
            Root.AddToClassList("replay-library");
            scroll.AddToClassList("menu-scroll");
            scroll.AddToClassList("replay-scroll");
            empty.AddToClassList("menu-empty");
            Root.Add(empty);
            Root.Add(scroll);
            var footer = new VisualElement { name = "replay-footer" };
            footer.AddToClassList("replay-footer");
            if (standalone)
                footer.Add(FooterButton("replay-back", "Back", () => Back?.Invoke()));
            import = FooterButton("replay-import", "Import", () => Import?.Invoke());
            import.tooltip = "Add a replay file from this computer to the library.";
            footer.Add(import);
            Root.Add(footer);
            Status.AddToClassList("replay-status");
            Root.Add(Status);
        }

        public float ScrollOffset
        {
            get => scroll.scrollOffset.y;
            set => scroll.schedule.Execute(() => scroll.scrollOffset = new Vector2(0, value));
        }

        public void Show(IReadOnlyList<ReplayEntry> entries)
        {
            var key = string.Join("\n", entries.Select(e => e.Path + "|" + e.Error));
            empty.style.display = entries.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (key == shown)
                return;
            shown = key;
            scroll.Clear();
            actions.Clear();
            foreach (var entry in entries)
                scroll.Add(Row(entry));
        }

        public void Busy(bool value)
        {
            import.SetEnabled(!value);
            foreach (var action in actions)
                action.SetEnabled(!value && (bool)action.userData);
        }

        private VisualElement Row(ReplayEntry entry)
        {
            var key = Key(entry);
            var row = new VisualElement { name = "replay-" + key };
            row.AddToClassList("menu-row");
            row.AddToClassList("replay-row");
            var emblem = new VisualElement { pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("menu-row-emblem");
            if (entry.Usable && entry.Manifest.Winner is Side winner)
                emblem.AddToClassList(winner == Side.FreePeoples ? "side-free" : "side-shadow");
            row.Add(emblem);
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("menu-row-text");
            var title = new Label(entry.Usable ? entry.Manifest.Name : "Damaged replay · " + Path.GetFileName(entry.Path)) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("menu-row-title");
            var detail = new Label(entry.Usable ? Detail(entry.Manifest) : entry.Error) { pickingMode = PickingMode.Ignore };
            detail.AddToClassList("menu-row-detail");
            text.Add(title);
            text.Add(detail);
            row.Add(text);
            row.tooltip = title.text + "\n" + detail.text;
            row.Add(Action("replay-play-" + key, "Play", () => Play?.Invoke(entry), entry.Usable));
            row.Add(Action("replay-export-" + key, "Export", () => Export?.Invoke(entry), entry.Usable));
            var delete = Action("replay-delete-" + key, "Delete", () => Delete?.Invoke(entry), true);
            delete.tooltip = "Delete this copy from this computer.";
            row.Add(delete);
            return row;
        }

        private Button Action(string name, string text, Action click, bool usable)
        {
            var button = BoardScreen.Button(text, click);
            button.name = name;
            button.userData = usable;
            button.SetEnabled(usable);
            button.AddToClassList("replay-action");
            actions.Add(button);
            return button;
        }

        private static Button FooterButton(string name, string text, Action click)
        {
            var button = BoardScreen.Button(text, click);
            button.name = name;
            button.AddToClassList("replay-footer-action");
            return button;
        }

        public static string Key(ReplayEntry entry) => Path.GetFileNameWithoutExtension(entry.Path);

        // Who played which side, when, who won and how long — the row's second line.
        public static string Detail(ReplayManifest manifest)
        {
            string Player(Side side) => manifest.Players.Find(p => p.Side == side)?.Name ?? "";
            var result = manifest.Winner is Side winner
                ? BoardScreen.SideName(winner) + " win · " + GameEndState.Reason(manifest.Victory)
                : "No winner";
            return "Free Peoples: " + Player(Side.FreePeoples) + " · Shadow: " + Player(Side.Shadow) + " · " +
                manifest.FinishedAt.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture) + " · " + result +
                " · " + manifest.Turns + (manifest.Turns == 1 ? " turn" : " turns");
        }
    }
}
