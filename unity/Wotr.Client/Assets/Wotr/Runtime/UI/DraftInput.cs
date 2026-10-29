using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Input only: delegates decide whether a completed gesture edits or submits.
    public sealed class DraftInput : IDisposable
    {
        // AUDIO-04: the last press was the quick second click after a committed step (UI-142), ignored and silent.
        public bool LastPressSuppressed { get; private set; }

        public event Action<DraftToken> DragChanged;
        public VisualElement DragSource => dragging ? sourceElement : null;
        private readonly VisualElement root;
        private readonly Label ghost = new Label { pickingMode = PickingMode.Ignore };
        // UI-393 (owner): a dragged reserve figure travels as its own icon, centred under the pointer, and its slot shows
        // that it was lifted; other sources keep the label.
        private readonly Image lifted = new Image { name = "drag-figure", pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
        public const string LiftedClass = "drag-lifted";
        private Vector2 liftedSize;
        private readonly Action confirm;
        private readonly Action back;
        private readonly Func<DraftToken, DraftToken, bool> drop;
        private readonly Func<string> scope;
        private string pressedScope;
        private readonly DraftClickSequence clicks = new DraftClickSequence();
        private DraftClickKind clickKind;
        private DraftToken source;
        private VisualElement sourceElement;
        private PointerCancelEvent pressedCancellation;
        private Vector2 origin;
        private bool dragging;
        private bool enterHeld;
        private bool escapeHeld;
        private int pointer = -1;

        public DraftInput(VisualElement root, Action confirm, Action back, Func<DraftToken, DraftToken, bool> drop,
            Func<string> scope = null)
        {
            this.root = root;
            this.confirm = confirm;
            this.back = back;
            this.drop = drop;
            this.scope = scope ?? (() => "");
            root.focusable = true;
            ghost.AddToClassList("drag-ghost");
            ghost.style.display = DisplayStyle.None;
            root.Add(ghost);
            lifted.AddToClassList("drag-figure");
            lifted.style.display = DisplayStyle.None;
            root.Add(lifted);
            root.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(CancelPointer);
            root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusOutEvent>(FocusOut);
            Application.focusChanged += WindowFocusChanged;
        }

        private void Down(PointerDownEvent evt)
        {
            LastPressSuppressed = false;
            if (BoardMenu.ContainsInput(evt.target as VisualElement))
                return;
            if (evt.button != 0)
                return;
            if (FloatingWindow.IsHandle(evt.target as VisualElement) || FloatingWindow.IsDragging(evt.target as VisualElement))
            {
                ResetDrag(true);
                clicks.Cancel();
                return;
            }
            ResetDrag(true);
            pressedScope = scope();
            source = Token(evt.target as VisualElement, evt.position);
            origin = evt.position;
            pointer = evt.pointerId;
            clickKind = clicks.Begin(source, pressedScope, pointer, origin, evt.timestamp, evt.clickCount);
            LastPressSuppressed = clickKind == DraftClickKind.Suppressed;
            if (clickKind == DraftClickKind.Suppressed)
            {
                evt.StopImmediatePropagation();
                return;
            }
            sourceElement = evt.target as VisualElement;
            while (sourceElement != null && sourceElement is not Button && sourceElement is not MapCanvas &&
                !(sourceElement.userData is DraftToken))
                sourceElement = sourceElement.parent;
            if (sourceElement != null)
                pressedCancellation = PointerCancelEvent.GetPooled(evt);
            // Captured pointer events skip ancestors' trickle-down callbacks in UI Toolkit.
            // Observe the pressed button until the drag takes ownership of the capture.
            sourceElement?.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            sourceElement?.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
        }

        private void Move(PointerMoveEvent evt)
        {
            if (pointer != evt.pointerId || clickKind == DraftClickKind.Suppressed ||
                Vector2.Distance(origin, evt.position) < DraftClickSequence.MovementTolerance)
                return;
            // UI-105: armies, figures and characters move by clicks on the map only, so they are never dragged.
            // UI-237: action dice are chosen by clicks only, too.
            if (source == null ||
                source.Object is DraftObject.Action or DraftObject.Deck or DraftObject.Tray or DraftObject.Region or DraftObject.Fellowship
                    or DraftObject.Army or DraftObject.Figure or DraftObject.FellowshipPortrait or DraftObject.Character or DraftObject.Die)
                return;
            if (!dragging)
            {
                dragging = true;
                clicks.Cancel();
                DragChanged?.Invoke(source);
            }
            root.CapturePointer(pointer);
            var local = root.WorldToLocal(evt.position);
            if (!Lift(local))
            {
                ghost.style.display = DisplayStyle.Flex;
                ghost.text = source.Object == DraftObject.FellowshipPortrait ? "Companion" : source.Object.ToString();
                ghost.style.left = local.x + 12;
                ghost.style.top = local.y + 12;
            }
            evt.StopImmediatePropagation();
        }

        private void Up(PointerUpEvent evt)
        {
            if (BoardMenu.ContainsInput(evt.target as VisualElement))
                return;
            if (FloatingWindow.IsHandle(evt.target as VisualElement) || FloatingWindow.IsDragging(evt.target as VisualElement))
                return;
            if (evt.button != 0)
                return;
            if (evt.pointerId != pointer)
            {
                if (evt.button == 0)
                    evt.StopImmediatePropagation();
                return;
            }
            if (pressedScope != scope())
            {
                ResetDrag(true);
                clicks.Cancel();
                evt.StopImmediatePropagation();
                return;
            }
            if (clickKind == DraftClickKind.Suppressed)
            {
                clicks.Complete();
                ResetDrag();
                evt.StopImmediatePropagation();
                return;
            }
            if (dragging)
            {
                var target = Token(root.panel.Pick(evt.position), evt.position);
                var picked = source;
                ResetDrag();
                if (pressedScope == scope())
                    drop(picked, target);
                evt.StopImmediatePropagation();
            }
            else
            {
                clicks.Complete();
                ResetDrag();
            }
        }

        // UI-393: the figure's own art follows the pointer, a little larger, and its slot dims while it is away.
        private bool Lift(Vector2 local)
        {
            var art = source.Object == DraftObject.Reserve ? sourceElement?.Q<Image>(className: "reserve-token-figure") : null;
            if (art?.sprite == null)
                return false;
            if (lifted.style.display.value != DisplayStyle.Flex)
            {
                lifted.sprite = art.sprite;
                liftedSize = art.worldBound.size * 1.15f;
                lifted.style.width = liftedSize.x;
                lifted.style.height = liftedSize.y;
                lifted.style.display = DisplayStyle.Flex;
                lifted.BringToFront();
                sourceElement.AddToClassList(LiftedClass);
            }
            lifted.style.left = local.x - liftedSize.x / 2;
            lifted.style.top = local.y - liftedSize.y / 2;
            return true;
        }

        private static DraftToken Token(VisualElement element, Vector2? position = null)
        {
            while (element != null)
            {
                if (element.userData is DraftToken token)
                    return token;
                if (element is MapCanvas map && position.HasValue)
                {
                    var region = map.HitTest(map.WorldToLocal(position.Value));
                    return region == null ? null : new DraftToken(DraftObject.Region, region);
                }
                element = element.parent;
            }
            return null;
        }

        private void KeyDown(KeyDownEvent evt)
        {
            if (FloatingWindow.IsDragging(evt.target as VisualElement) || ActionCard.ContainsInput(evt.target as VisualElement) ||
                CharacterRosterPanel.ContainsInput(evt.target as VisualElement) ||
                CompanionSetupChoices.ContainsInput(evt.target as VisualElement) || BoardMenu.ContainsInput(evt.target as VisualElement) ||
                GameChatPanel.ContainsInput(evt.target as VisualElement))
                return;
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                var freshPress = !enterHeld;
                enterHeld = true;
                if (freshPress)
                    confirm();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                if (!escapeHeld)
                {
                    if (dragging)
                        ResetDrag(true);
                    else
                        back();
                }
                escapeHeld = true;
            }
            else
                return;
            evt.StopImmediatePropagation();
        }

        private void KeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                enterHeld = false;
            if (evt.keyCode == KeyCode.Escape)
                escapeHeld = false;
        }

        private void FocusOut(FocusOutEvent evt)
        {
            if (evt.relatedTarget == null)
                CancelPress();
        }

        private void WindowFocusChanged(bool focused)
        {
            if (!focused)
            {
                // A focus change is not a release of a held confirmation key.
                escapeHeld = false;
                CancelPress();
            }
        }

        private void CancelPointer(PointerCancelEvent evt)
        {
            ResetDrag();
            clicks.Cancel();
        }

        private void CancelPress()
        {
            ResetDrag(true);
            clicks.Cancel();
        }

        public bool CancelGesture()
        {
            if (!dragging && pointer < 0)
                return false;
            CancelPress();
            return true;
        }

        private void ResetDrag(bool cancelPress = false)
        {
            var wasDragging = dragging;
            var pressed = sourceElement;
            var cancellation = pressedCancellation;
            var captured = pointer;
            pressed?.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            pressed?.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            sourceElement = null;
            pressedCancellation = null;
            source = null;
            pointer = -1;
            dragging = false;
            ghost.style.display = DisplayStyle.None;
            lifted.style.display = DisplayStyle.None;
            lifted.sprite = null;
            pressed?.RemoveFromClassList(LiftedClass);
            // Releasing capture alone can defer Clickable's active-state reset until a
            // later event. Cancel the original press before another controller mounts.
            if (cancelPress && pressed?.panel != null && cancellation != null)
                pressed.SendEvent(cancellation);
            cancellation?.Dispose();
            if (cancelPress && captured >= 0 && pressed?.HasPointerCapture(captured) == true)
                pressed.ReleasePointer(captured);
            if (captured >= 0 && root.HasPointerCapture(captured))
                root.ReleasePointer(captured);
            if (wasDragging)
                DragChanged?.Invoke(null);
        }

        public void Dispose()
        {
            CancelPress();
            root.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCancelEvent>(CancelPointer);
            root.UnregisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<FocusOutEvent>(FocusOut);
            Application.focusChanged -= WindowFocusChanged;
            ghost.RemoveFromHierarchy();
        }
    }
}
