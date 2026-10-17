using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // One key press has one owner. Inspection never mutates the local draft.
    public sealed class BoardEscape
    {
        private readonly BoardScreen board;
        private readonly List<Window> windows = new List<Window>();
        private VisualElement keyboardRoot;
        private bool held;
        public Func<bool> CancelGesture { get; set; }
        public Func<bool> CancelChoice { get; set; }
        public Func<bool> ClearSelection { get; set; }
        public IEnumerable<VisualElement> Roots => windows.Select(window => window.Root);

        public BoardEscape(BoardScreen board)
        {
            this.board = board;
            board.Root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            board.Root.RegisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            board.Root.RegisterCallback<FocusOutEvent>(e => { if (e.relatedTarget == null) held = false; });
            board.Root.RegisterCallback<AttachToPanelEvent>(_ => ObserveKeyboard());
            board.Root.RegisterCallback<DetachFromPanelEvent>(_ => ReleaseKeyboard());
        }

        private void ObserveKeyboard()
        {
            ReleaseKeyboard();
            keyboardRoot = board.Root.panel?.visualTree;
            keyboardRoot?.RegisterCallback<KeyDownEvent>(SettingsKeyDown, TrickleDown.TrickleDown);
            keyboardRoot?.RegisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
        }

        private void ReleaseKeyboard()
        {
            keyboardRoot?.UnregisterCallback<KeyDownEvent>(SettingsKeyDown, TrickleDown.TrickleDown);
            keyboardRoot?.UnregisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            keyboardRoot = null;
            held = false;
        }

        private void SettingsKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape)
                return;
            // Settings is a sibling of the board. Observe its press without owning it,
            // so a repeat after focus returns cannot close another board window.
            for (var element = e.target as VisualElement; element != null; element = element.parent)
                if (element.name == "interface-settings" && element.parent?.Contains(board.Root) == true)
                {
                    held = true;
                    return;
                }
        }

        private void KeyUp(KeyUpEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
                held = false;
        }

        public void Attach()
        {
            Add(board.CompanionSetup.Root, board.CompanionSetup.Close, board.CompanionSetup.Tab);
            Add(board.Characters.Inspector.Root, board.Characters.Close, board.Characters.Tab);
            Add(board.Characters.Root, board.Characters.Close, board.Characters.Tab);
            Add(board.Log.Popup, board.Log.CloseCard, board.Log.Toggle);
            Add(board.Log.Root, board.Log.Close, board.Log.Toggle);
            Add(board.Cards.Root, board.Cards.Close, board.HandDrawer.Tab);
            Add(board.Nations.Drawer, board.Nations.Close, board.Nations.Root);
            Add(board.Fellowship.Root, board.Fellowship.Close, board.Fellowship.Status);
            Add(board.Hunt.Root, board.Hunt.Close, board.Hunt.Tab);
            Add(board.TableEffects.Root, board.TableEffects.Close, board.TableEffects.Tab);
            Add(board.Victory.Root, board.Victory.Close, board.Menu.Toggle);
            Add(board.ResourceDecision.Root, board.ResourceDecision.Close, board.ResourceDecision.Tab);
            Add(board.Reinforcements.Root, board.Reinforcements.Close, board.Reinforcements.Tab);
            Add(board.Sovereigns.Inspector, board.Sovereigns.Close, board.Characters.Tab);
            Add(board.Information.Root, board.Information.Close, board.Root);
            Add(board.Hover.Panel.Root, board.Hover.Close, board.Root);
            Add(board.Hand, board.HandDrawer.Close, board.HandDrawer.Tab);
            Add(board.Battles.Root, board.Battles.Close, board.Root);
            Add(board.BattleReport.Root, board.BattleReport.Close, board.BattleReport.Open);
            Add(board.Map.Legend.Popup, () => board.Map.Legend.SetOpen(false), board.Map.Legend.Toggle);
            Add(board.Notifications.Overflow, board.Notifications.Close, board.Root);
        }

        private void Add(VisualElement root, Action close, VisualElement fallback = null) =>
            windows.Add(new Window { Root = root, Close = close, Fallback = fallback });

        private void KeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape)
                return;
            if (!held)
            {
                Handle(e.target as VisualElement);
                held = true;
            }
            e.StopImmediatePropagation();
        }

        private void Handle(VisualElement target)
        {
            // MENU-14/MENU-22: a question (quit, delete game) is modal; Escape answers it before anything behind it.
            if (board.Menu.OpenQuestion != null)
            {
                board.Menu.OpenQuestion.Close();
                return;
            }
            // MENU-22: a deleted game keeps its notice until Main menu.
            if (board.GameDeleted.IsOpen)
                return;
            if (FloatingWindow.TryCancelDrag(target) || board.Map.CancelPan())
                return;
            if (CancelGesture?.Invoke() == true)
                return;
            if (board.Menu.IsOpen)
            {
                board.Menu.Close();
                return;
            }
            var visible = windows.Where(w => !w.Root.ClassListContains("edge-permanent-panel") && !(w.Root == board.Hand && board.HandDrawer.Docked) &&
                Visible(w.Root)).ToList();
            var window = visible.FirstOrDefault(w => w.Root == target || w.Root.Contains(target)) ??
                visible.OrderByDescending(w => w.Root.parent?.IndexOf(w.Root) ?? -1).FirstOrDefault();
            // UI-127: while the region panel is open, the first Escape drops the unsent army selection.
            if (Visible(board.Hover.Panel.Root) && ClearSelection?.Invoke() == true)
                return;
            if (window != null)
            {
                window.Close();
                board.Edges.Preview.SuppressRestoredFocus(window.Fallback);
                window.Fallback?.Focus();
                return;
            }
            if (CancelChoice?.Invoke() != true)
                board.Menu.Open();
        }

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.resolvedStyle.visibility == Visibility.Visible && element.worldBound.width > 0 && element.worldBound.height > 0;

        private sealed class Window
        {
            public VisualElement Root;
            public Action Close;
            public VisualElement Fallback;
        }
    }
}
