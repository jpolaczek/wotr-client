using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-160: the Hunt Box in the console. The Hunt plaque (Eyes and Free Peoples movement dice, the drop target of
    // the allocation) moves here from the map corner; a click opens the Hunt panel with its pool list and report.
    public sealed class ConsoleHunt
    {
        public VisualElement Root { get; } = new VisualElement { name = "console-hunt" };
        public VisualElement Well { get; } = new VisualElement { name = "console-hunt-well" };

        public ConsoleHunt()
        {
            Root.AddToClassList("console-hunt");
            Well.AddToClassList("console-hunt-well");
            Root.Add(Well);
        }

        // The Shadow's own allocation lights the well, where its dice go (UI-162).
        public void SetAllocating(bool allocating) => Root.EnableInClassList("hunt-allocating", allocating);

        public void Attach(Button plaque)
        {
            plaque.AddToClassList("console-hunt-plaque");
            plaque.tooltip = "Hunt Box · Eyes and Free Peoples movement dice · Click to open the Hunt";
            Well.Add(plaque);
        }
    }
}
