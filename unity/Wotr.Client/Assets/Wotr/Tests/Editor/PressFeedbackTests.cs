using NUnit.Framework;
using UnityEngine.UIElements;

namespace Wotr.Client.Tests
{
    // UI-114: which element under the pointer shows the pressed state.
    public sealed class PressFeedbackTests
    {
        [Test]
        public void ButtonUnderThePointerIsPressableThroughItsChildren()
        {
            var root = new VisualElement();
            var button = new Button();
            var label = new Label("inside");
            button.Add(label);
            root.Add(button);
            Assert.AreSame(button, PressFeedback.Pressable(label, root));
            Assert.AreSame(button, PressFeedback.Pressable(button, root));
        }

        [Test]
        public void DisabledButtonsAndPlainElementsDoNotPress()
        {
            var root = new VisualElement();
            var disabled = new Button();
            disabled.SetEnabled(false);
            var plain = new Label("terrain");
            root.Add(disabled);
            root.Add(plain);
            Assert.IsNull(PressFeedback.Pressable(disabled, root));
            Assert.IsNull(PressFeedback.Pressable(plain, root));
            Assert.IsNull(PressFeedback.Pressable(root, root), "The root itself never presses.");
        }

        [Test]
        public void OptedInElementsPressAndTextFieldsNeverDo()
        {
            var root = new VisualElement();
            var pressable = new VisualElement();
            pressable.AddToClassList(PressFeedback.PressableClass);
            var child = new Label("face");
            pressable.Add(child);
            var button = new Button();
            var field = new TextField();
            button.Add(field);
            root.Add(pressable);
            root.Add(button);
            Assert.AreSame(pressable, PressFeedback.Pressable(child, root));
            Assert.IsNull(PressFeedback.Pressable(field, root), "Typing into a field is not a press of the button around it.");
        }
    }
}
