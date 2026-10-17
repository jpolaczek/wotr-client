#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine.UIElements;

namespace Wotr.Client.Diagnostics
{
    // A benchmark owns its input stream. Runtime pointer updates and physical
    // input must not replace the synthetic gesture whose feedback is measured.
    internal sealed class DesktopProfileInput : IDisposable
    {
        private readonly VisualElement root;
        private bool dispatching;

        public DesktopProfileInput(VisualElement root)
        {
            this.root = root;
            root.RegisterCallback<PointerMoveEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerEnterEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(Intercept, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(Intercept, TrickleDown.TrickleDown);
        }

        public bool Tick(DesktopProfileScene scene, int frame)
        {
            dispatching = true;
            try
            {
                return scene.Tick(frame);
            }
            finally
            {
                dispatching = false;
            }
        }

        public void Dispatch(Action action)
        {
            dispatching = true;
            try
            {
                action();
            }
            finally
            {
                dispatching = false;
            }
        }

        private void Intercept<T>(T evt) where T : EventBase<T>, new()
        {
            if (!dispatching)
                evt.StopImmediatePropagation();
        }

        public void Dispose()
        {
            root.UnregisterCallback<PointerMoveEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerDownEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerEnterEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerLeaveEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCancelEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<WheelEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(Intercept, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(Intercept, TrickleDown.TrickleDown);
        }
    }
}
#endif
