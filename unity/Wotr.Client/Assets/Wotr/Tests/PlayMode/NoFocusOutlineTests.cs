using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-191 (user decision, 2026-10-16): focus never draws a white outline, on any object, whether a click,
    // the keyboard or code gave it. Focus itself, keys and typing keep working.
    public sealed class NoFocusOutlineTests
    {
        [UnityTest]
        public IEnumerator FocusFromAClickTheKeyboardOrCodeDrawsNoOutline()
        {
            using var client = new MountedMatchClient("http://localhost", "no-focus-outline");
            client.Apply(new BoardFixture().View);
            var button = Place(new Button { name = "focus-button", text = "Focus" }, 350);
            var field = Place(new TextField { name = "focus-field", maxLength = 40 }, 500);
            client.Board.Root.Add(button);
            client.Board.Root.Add(field);
            yield return Settle();
            var tree = button.panel.visualTree;
            var focus = button.panel.focusController;

            PointerGestureEvents.ClickAt(button);
            button.Focus();
            yield return Settle();
            Assert.AreSame(button, focus.focusedElement, "A click still gives focus.");
            NoOutline(tree, "after a click");

            Key(button, KeyCode.A);
            yield return Settle();
            Assert.AreSame(button, focus.focusedElement);
            NoOutline(tree, "after a key on a button");

            field.Focus();
            Key(field, KeyCode.H);
            field.value = "hello";
            yield return Settle();
            Assert.IsTrue(field.Contains(focus.focusedElement as VisualElement) || focus.focusedElement == field,
                "The text field takes focus for typing.");
            NoOutline(tree, "while typing in a text field");

            Key(tree, KeyCode.Tab);
            button.Focus();
            yield return Settle();
            NoOutline(tree, "for focus given by code after keyboard input");
            LogAssert.NoUnexpectedReceived();
        }

        internal static void Key(VisualElement target, KeyCode key)
        {
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = key }))
                target.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = key }))
                target.SendEvent(up);
        }

        internal static void NoOutline(VisualElement tree, string when)
        {
            Assert.IsNull(tree.Q("keyboard-focus"), "No focus outline " + when + ".");
            Assert.IsNull(tree.Q(className: "focus-indicator"), "No focus outline " + when + ".");
        }

        private static T Place<T>(T control, float left) where T : VisualElement
        {
            control.style.position = Position.Absolute;
            control.style.left = left;
            control.style.top = 200;
            control.style.width = 140;
            control.style.height = 40;
            return control;
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.08f);
            yield return null;
        }
    }
}
