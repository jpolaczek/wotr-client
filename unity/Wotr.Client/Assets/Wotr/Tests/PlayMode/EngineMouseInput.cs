using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.PlayTests
{
    // Unity 6000.6.0f1 InputManagerProvider uses this PointerState and these
    // event factories. The engine computes click counts; callers never supply one.
    // This exercises engine input conversion, picking, capture and ClickDetector,
    // but does not claim to inject operating-system mouse events.
    // https://github.com/Unity-Technologies/UnityCsReference/blob/6000.6.0f1/Modules/InputForUI/Provider/PointerState.cs
    internal sealed class EngineMouseInput : IDisposable
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly VisualElement root;
        private readonly Type eventType;
        private readonly object state;
        private readonly Type stateType;
        private readonly object leftButton;
        private readonly Type timeType;
        private readonly Dictionary<Type, MethodInfo> factories = new Dictionary<Type, MethodInfo>();
        private Vector2 position;
        private bool hasPosition;
        private bool pressed;
        public List<int> PressCounts { get; } = new List<int>();
        public List<int> GeneratedClickCounts { get; } = new List<int>();

        public EngineMouseInput(VisualElement element)
        {
            root = element.panel.visualTree;
            eventType = RequireType("UnityEngine.InputForUI.PointerEvent");
            stateType = RequireType("UnityEngine.InputForUI.PointerState");
            state = Activator.CreateInstance(stateType);
            stateType.GetMethod("Reset", Members).Invoke(state, null);
            leftButton = Enum.Parse(eventType.GetNestedType("Button"), "MouseLeft");
            timeType = eventType.GetProperty("timestamp").PropertyType;
            root.RegisterCallback<ClickEvent>(ObserveClick, TrickleDown.TrickleDown);
        }

        public void Click(VisualElement expected, Vector2 offset = default)
        {
            Press(expected, offset);
            Release();
        }

        public void Press(VisualElement expected, Vector2 offset = default)
        {
            Assert.IsNotNull(expected);
            Move(expected.worldBound.center + offset);
            AssertPicked(expected);
            PressCurrent();
        }

        public void ClickAtPoint(Vector2 point)
        {
            PressAtPoint(point);
            Release();
        }

        public void PressAtPoint(Vector2 point)
        {
            Move(point);
            Assert.IsNotNull(root.panel.Pick(point), "The original pointer position must still belong to the mounted panel.");
            PressCurrent();
        }

        public void Move(Vector2 point)
        {
            if (hasPosition && position == point)
                return;
            position = point;
            hasPosition = true;
            stateType.GetMethod("OnMove", Members).Invoke(state, new[] { Now(), (object)point, 0 });
            Send<PointerMoveEvent>("PointerMoved", false);
        }

        private void PressCurrent()
        {
            stateType.GetMethod("OnButtonDown", Members).Invoke(state, new[] { Now(), leftButton });
            pressed = true;
            PressCounts.Add((int)stateType.GetProperty("ClickCount").GetValue(state));
            Send<PointerDownEvent>("ButtonPressed", true);
        }

        public void Release()
        {
            stateType.GetMethod("OnButtonUp", Members).Invoke(state, new[] { Now(), leftButton });
            pressed = false;
            Send<PointerUpEvent>("ButtonReleased", true);
        }

        private void Send<T>(string kind, bool button) where T : EventBase
        {
            var input = Activator.CreateInstance(eventType);
            Set(input, "type", Enum.Parse(eventType.GetNestedType("Type"), kind));
            Set(input, "position", position);
            Set(input, "pointerIndex", 0);
            Set(input, "button", button ? leftButton : Enum.ToObject(leftButton.GetType(), 0));
            Set(input, "buttonsState", stateType.GetProperty("ButtonsState").GetValue(state));
            Set(input, "clickCount", button ? stateType.GetProperty("ClickCount").GetValue(state) : 0);
            Set(input, "timestamp", Now());
            Set(input, "eventSource", Enum.Parse(eventType.GetProperty("eventSource").PropertyType, "Mouse"));
            if (!factories.TryGetValue(typeof(T), out var factory))
            {
                factory = typeof(T).BaseType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .SingleOrDefault(method => method.Name == "GetPooled" && method.GetParameters().Length == 4 &&
                        method.GetParameters()[0].ParameterType == eventType);
                Assert.IsNotNull(factory, "Unity 6000.6.0f1 InputForUI event factory changed for " + typeof(T).Name);
                factories.Add(typeof(T), factory);
            }
            using var evt = (EventBase)factory.Invoke(null, new[] { input, (object)(Vector3)position, PointerId.mousePointerId, 0f });
            var target = root.panel.Pick(position) ?? root;
            target.SendEvent(evt);
        }

        private void AssertPicked(VisualElement expected)
        {
            var picked = root.panel.Pick(position);
            Assert.IsTrue(picked == expected || expected.Contains(picked),
                $"{expected.name} at {position} is covered by {picked?.name} ({picked?.GetType().Name}).");
        }

        private object Now() => Activator.CreateInstance(timeType, new object[] { Time.realtimeSinceStartupAsDouble });

        private void Set(object input, string name, object value)
        {
            var field = eventType.GetField(name, Members);
            if (field != null)
                field.SetValue(input, value);
            else
                eventType.GetProperty(name, Members).SetValue(input, value);
        }

        private static Type RequireType(string name)
        {
            var type = Type.GetType(name + ", UnityEngine.InputForUIModule");
            Assert.IsNotNull(type, "The test requires the pinned Unity 6000.6.0f1 input type " + name);
            return type;
        }

        private void ObserveClick(ClickEvent evt) => GeneratedClickCounts.Add(evt.clickCount);

        public void Dispose()
        {
            if (pressed && root.panel != null)
                Release();
            root.UnregisterCallback<ClickEvent>(ObserveClick, TrickleDown.TrickleDown);
        }
    }
}
