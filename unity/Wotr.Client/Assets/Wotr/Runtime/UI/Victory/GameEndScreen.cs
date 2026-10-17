using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-169 (SIGNALS_UX D): a full overlay over the darkened board in the frame of the battle window. It opens by
    // itself for both players when the game has a winner — once per match on this board, so also when a finished
    // game is opened — and the console's GAME OVER status opens it again. Its buttons keep their places: View board
    // closes it (the board stays to read), Game log opens the Log and Main menu leaves through the board menu.
    public sealed class GameEndScreen
    {
        public VisualElement Root { get; } = new VisualElement { name = "game-end" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public GameEndState State { get; private set; }
        public event Action LogRequested;
        public event Action MenuRequested;
        // REPLAY-04: Save replay keeps its place; its state says what a click does.
        public event Action SaveReplayRequested;
        public ReplaySaveState Replay { get; private set; } = ReplaySaveState.Unavailable;
        private readonly VisualElement scene = new VisualElement { name = "game-end-scene", pickingMode = PickingMode.Ignore };
        private readonly VisualElement emblem = new VisualElement { name = "game-end-emblem", pickingMode = PickingMode.Ignore };
        private readonly VisualElement titlePlate = new VisualElement { name = "game-end-title-plate", pickingMode = PickingMode.Ignore };
        private readonly Label title = new Label { name = "game-end-title", pickingMode = PickingMode.Ignore };
        private readonly Label subtitle = new Label { name = "game-end-subtitle", pickingMode = PickingMode.Ignore };
        private readonly Label[] summary = new Label[4];
        private readonly Button viewBoard;
        private readonly Button saveReplay;
        private readonly Label replayStatus = new Label { name = "game-end-replay-status", pickingMode = PickingMode.Ignore };
        private string sceneClass = "";
        private string shownFor = "";

        public GameEndScreen()
        {
            Root.AddToClassList("game-end");
            var window = new VisualElement { name = "game-end-window" };
            window.AddToClassList("game-end-window");
            Root.Add(window);
            scene.AddToClassList("game-end-scene");
            window.Add(scene);
            window.Add(Heading());
            subtitle.AddToClassList("game-end-subtitle");
            window.Add(subtitle);
            window.Add(Summary());
            viewBoard = Button("game-end-view-board", "View board", Close);
            saveReplay = Button("game-end-save-replay", "Save replay", RequestSave);
            window.Add(Buttons());
            replayStatus.AddToClassList("game-end-replay-status");
            window.Add(replayStatus);
            SetReplay(ReplaySaveState.Unavailable, "Replays are saved from finished online games.");
            Root.style.display = DisplayStyle.None;
            // Esc is View board: the screen closes and the board stays to read.
            Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != UnityEngine.KeyCode.Escape || !IsOpen)
                    return;
                Close();
                e.StopPropagation();
            });
        }

        private VisualElement Heading()
        {
            var heading = new VisualElement { name = "game-end-heading", pickingMode = PickingMode.Ignore };
            heading.AddToClassList("game-end-heading");
            emblem.AddToClassList("game-end-emblem");
            titlePlate.AddToClassList("game-end-title-plate");
            title.AddToClassList("game-end-title");
            titlePlate.Add(title);
            heading.Add(emblem);
            heading.Add(titlePlate);
            return heading;
        }

        private VisualElement Summary()
        {
            var lines = new VisualElement { name = "game-end-summary", pickingMode = PickingMode.Ignore };
            lines.AddToClassList("game-end-summary");
            for (var index = 0; index < summary.Length; index++)
            {
                summary[index] = new Label { name = "game-end-line-" + (index + 1), pickingMode = PickingMode.Ignore };
                summary[index].AddToClassList("game-end-line");
                lines.Add(summary[index]);
            }
            return lines;
        }

        private VisualElement Buttons()
        {
            var row = new VisualElement { name = "game-end-buttons" };
            row.AddToClassList("game-end-buttons");
            row.Add(viewBoard);
            row.Add(Button("game-end-log", "Game log", () =>
            {
                Close();
                LogRequested?.Invoke();
            }));
            row.Add(saveReplay);
            row.Add(Button("game-end-menu", "Main menu", () => MenuRequested?.Invoke()));
            return row;
        }

        public void RequestSave()
        {
            if (Replay is ReplaySaveState.Ready or ReplaySaveState.Failed)
                SaveReplayRequested?.Invoke();
        }

        // REPLAY-04: ready, saving, saved, already saved, failed with Retry, or unavailable — always in its place.
        public void SetReplay(ReplaySaveState state, string detail = "")
        {
            Replay = state;
            saveReplay.text = state switch
            {
                ReplaySaveState.Saving => "Saving…",
                ReplaySaveState.Saved => "Saved",
                ReplaySaveState.AlreadySaved => "Already saved",
                ReplaySaveState.Failed => "Retry saving",
                _ => "Save replay"
            };
            saveReplay.SetEnabled(state is ReplaySaveState.Ready or ReplaySaveState.Failed);
            replayStatus.text = state switch
            {
                ReplaySaveState.Saved => "Saved to Replays in the main menu.",
                ReplaySaveState.AlreadySaved => "This replay is already in Replays.",
                _ => detail
            };
            saveReplay.tooltip = detail.Length > 0 ? detail : replayStatus.text;
        }

        private static Button Button(string name, string text, Action click)
        {
            var button = BoardScreen.Button(text, click);
            button.name = name;
            button.AddToClassList("game-end-button");
            return button;
        }

        // The first view with a winner opens the screen; later views of the same match only refresh it.
        // OBS-03: set on an observer's board; the screen then names the winner instead of Victory or Defeat.
        public bool Observing { get; set; }

        public void Apply(PlayerView view)
        {
            State = GameEndState.For(view, Observing);
            if (State == null)
            {
                Close();
                return;
            }
            Render(State);
            if (shownFor == view.MatchId)
                return;
            shownFor = view.MatchId;
            Open();
        }

        public void Open()
        {
            if (State == null)
                return;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.schedule.Execute(() => { if (IsOpen) viewBoard.Focus(); });
        }

        public void Close() => Root.style.display = DisplayStyle.None;

        private void Render(GameEndState state)
        {
            title.text = state.Title;
            subtitle.text = state.Subtitle;
            Root.EnableInClassList("won", state.Won);
            Root.EnableInClassList("lost", !state.Won && !state.Observed);
            emblem.EnableInClassList("side-free", state.Winner == Side.FreePeoples);
            emblem.EnableInClassList("side-shadow", state.Winner == Side.Shadow);
            SetScene("game-end-scene-" + state.Scene);
            for (var index = 0; index < summary.Length; index++)
                summary[index].text = index < state.Summary.Count ? state.Summary[index] : "";
        }

        private void SetScene(string next)
        {
            if (sceneClass == next)
                return;
            if (sceneClass.Length > 0)
                scene.RemoveFromClassList(sceneClass);
            sceneClass = next;
            scene.AddToClassList(next);
        }
    }
}
