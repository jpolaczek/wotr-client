using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // Exercise Unity 6000.6's screen-panel selection before event dispatch. Unlike
    // MapContourProbe, a missing target panel does not receive a forced Move.
    internal sealed class EngineScreenPointer : IDisposable
    {
        private readonly object eventSystem;
        private readonly MethodInfo send;
        private readonly MethodInfo find;
        private readonly PanelSettings settings;
        private readonly VisualElement root;
        private readonly MapCanvas map;
        private readonly GameObject samplerHost;
        private readonly Func<Vector3, int, EventBase> factory;
        private bool dispatching;
        private bool sampling;
        private bool focused = true;
        private bool present = true;
        private int buttons;
        // UI-174: a full-screen host holds a cursor beyond the picture at its edge.
        public bool FillsScreen { get; set; }
        public Vector2 ScreenSize => new Vector2(settings.targetTexture.width, settings.targetTexture.height);
        public Vector2 ScreenPosition { get; private set; }
        public Vector3 RoutedPosition { get; private set; }
        public IPanel RoutedPanel { get; private set; }
        public int DeliveredMoves { get; private set; }
        public int ExternalMoves { get; private set; }
        public string LastPointerEvent { get; private set; } = "none";

        public EngineScreenPointer(PanelSettings settings, MapCanvas map)
        {
            this.settings = settings;
            this.map = map;
            root = map.panel.visualTree;
            var type = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.DefaultEventSystem");
            Assert.IsNotNull(type, "The pinned Unity screen event system must exist.");
            eventSystem = Activator.CreateInstance(type, true);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            find = type.GetMethod("FindTargetAtPosition", flags);
            send = type.GetMethods(flags).Single(method => method.Name == "SendPositionBasedEvent" &&
                method.IsGenericMethodDefinition && method.GetParameters().Length == 7 &&
                method.GetParameters()[4].ParameterType.GetGenericTypeDefinition() == typeof(Func<,,>))
                .MakeGenericMethod(typeof(int));
            Assert.IsNotNull(find);
            factory = (position, _) => PointerMoveEvent.GetPooled(new Event
                { type = EventType.MouseMove, mousePosition = position });
            root.RegisterCallback<PointerMoveEvent>(Moved, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerEnterEvent>(Entered);
            root.RegisterCallback<PointerLeaveEvent>(Left);
            samplerHost = new GameObject("Screen input sample driver");
            samplerHost.AddComponent<EngineScreenPointerPump>().Tick = Observe;
        }

        public void Move(Vector2 screenPosition)
        {
            var delta = screenPosition - ScreenPosition;
            ScreenPosition = screenPosition;
            // These are physical top-left screen coordinates, the convention used
            // by InputForUI before DefaultEventSystem performs ScreenToPanel.
            var findArguments = new object[] { screenPosition, delta, PointerId.mousePointerId,
                (int?)settings.targetDisplay, null, null, Vector3.zero, null, null };
            find.Invoke(eventSystem, findArguments);
            RoutedPanel = findArguments[5] as IPanel;
            RoutedPosition = (Vector3)findArguments[6];
            dispatching = true;
            try
            {
                send.Invoke(eventSystem, new object[] { (Vector3)screenPosition, (Vector3)delta,
                    PointerId.mousePointerId, (int?)settings.targetDisplay, factory, 0, false });
            }
            finally
            {
                dispatching = false;
            }
            Sample(screenPosition);
        }

        public void Sample(Vector2 topLeft, bool hasFocus = true, bool mousePresent = true, int pressedButtons = 0)
        {
            ScreenPosition = topLeft;
            focused = hasFocus;
            present = mousePresent;
            buttons = pressedButtons;
            sampling = true;
            Observe();
        }

        private void Observe()
        {
            if (!sampling)
                return;
            // Legacy Input uses bottom-left, unlike the engine route above. Keep
            // sampling stationary input in Update, just like the real screen host.
            var size = ScreenSize;
            var bottomLeft = new Vector2(ScreenPosition.x, size.y - ScreenPosition.y);
            MapScreenPointer.Observe(map, bottomLeft, size, focused, present, buttons, FillsScreen);
        }

        public void Pause()
        {
            sampling = false;
            map.EdgePan.Stop();
        }

        private void Moved(PointerMoveEvent e)
        {
            DeliveredMoves++;
            if (!dispatching)
                ExternalMoves++;
            Record("move", e);
        }

        private void Entered(PointerEnterEvent e)
        {
            if (e.target == root)
                Record("enter", e);
        }

        private void Left(PointerLeaveEvent e)
        {
            if (e.target == root)
                Record("leave", e);
        }

        private void Record(string kind, IPointerEvent e) => LastPointerEvent =
            $"{kind}:{e.position}:buttons={e.pressedButtons}:driver={dispatching}:frame={Time.frameCount}";

        public void Dispose()
        {
            Pause();
            samplerHost.GetComponent<EngineScreenPointerPump>().Tick = null;
            UnityEngine.Object.Destroy(samplerHost);
            root.UnregisterCallback<PointerMoveEvent>(Moved, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerEnterEvent>(Entered);
            root.UnregisterCallback<PointerLeaveEvent>(Left);
        }
    }
}
