using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntReactionView
    {
        public static VisualElement Panel(VisualElement source)
        {
            for (var current = source; current != null; current = current.parent)
                if (current.ClassListContains("hunt-panel"))
                    return current;
            return source;
        }

        public static void AddDetails(Button button, PlayerView view, EffectChoiceView choice)
        {
            var source = HuntReactionPresentation.Source(view, choice);
            button.text = "";
            button.AddToClassList("hunt-reaction");
            AddLabel(button, "source", source);
            AddLabel(button, "title", choice.Title.Length > 0 ? choice.Title : "Use response");
            AddLabel(button, "detail", choice.Detail.Length > 0 ? choice.Detail : "Inspect the source for its effect.");
            AddLabel(button, "cost", HuntReactionPresentation.Cost(choice));
            button.tooltip = HuntReactionPresentation.Inspect(view, choice);
        }

        private static void AddLabel(VisualElement parent, string kind, string text)
        {
            var label = new Label(text) { name = "hunt-reaction-" + kind, pickingMode = PickingMode.Ignore };
            label.AddToClassList(label.name);
            parent.Add(label);
        }
    }
}
