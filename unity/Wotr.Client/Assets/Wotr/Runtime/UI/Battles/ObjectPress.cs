using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // A click on a battle object that is not a button (a portrait or a figure stack): pressed on mouse-down, acting on
    // mouse-up inside it, cancelled when the pointer leaves (UI-113). It takes no pointer capture, because its action
    // rebuilds the Battle window and a capture held by an element leaving the panel would keep the pointer from the
    // next object clicked.
    public static class ObjectPress
    {
        public const string PressedClass = "pressed";

        public static void Register(VisualElement element, Action act)
        {
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0)
                    return;
                element.AddToClassList(PressedClass);
                e.StopPropagation();
            });
            element.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button != 0 || !element.ClassListContains(PressedClass))
                    return;
                element.RemoveFromClassList(PressedClass);
                e.StopPropagation();
                act();
            });
            element.RegisterCallback<PointerLeaveEvent>(_ => element.RemoveFromClassList(PressedClass));
            element.RegisterCallback<PointerCancelEvent>(_ => element.RemoveFromClassList(PressedClass));
        }
    }
}
