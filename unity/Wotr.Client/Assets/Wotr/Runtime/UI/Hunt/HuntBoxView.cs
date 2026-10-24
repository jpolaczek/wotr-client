using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-98: public contents of the Hunt Box — the Shadow's Eyes and the Free Peoples movement dice.
    // Counts shows them on the Hunt plaque (UI-160: in the turn console); Root lists them in the Hunt panel while no
    // Hunt is resolved.
    // UI-109: the panel's own header names the Hunt, and Free Peoples dice appear only once there are any.
    public sealed class HuntBoxView
    {
        public VisualElement Counts { get; } = new VisualElement { name = "hunt-box-counts", pickingMode = PickingMode.Ignore };
        public VisualElement Root { get; } = new VisualElement { name = "hunt-box" };
        private readonly Label eyes = new Label { name = "hunt-eyes", pickingMode = PickingMode.Ignore };
        private readonly Label freeDice = new Label { name = "hunt-free-dice", pickingMode = PickingMode.Ignore };
        private readonly VisualElement freeCount;
        private readonly Label eyeEntry = new Label { name = "hunt-box-eyes" };
        private readonly Label freeEntry = new Label { name = "hunt-box-free" };
        private string shown;

        // UI-318: the plaque's Eye medallion and its number, the one part a Shadow allocation changes.
        public VisualElement EyeCount => eyes.parent;

        public HuntBoxView()
        {
            Counts.AddToClassList("hunt-box-counts");
            Counts.Add(EdgePlaque.Value(Medallion(), eyes));
            freeCount = EdgePlaque.Value(FellowshipArtwork.Image("journey"), freeDice);
            freeCount.name = "hunt-free-count";
            Counts.Add(freeCount);
            Root.AddToClassList("hunt-box");
            Root.Add(Entry(Medallion(), eyeEntry));
            Root.Add(Entry(FellowshipArtwork.Image("journey"), freeEntry));
        }

        public static int Eyes(PlayerView view) => view.Dice.Count(d => d.Location == DieLocation.Hunt && d.Owner == Side.Shadow);

        public static int FreeDice(PlayerView view) => view.Dice.Count(d => d.Location == DieLocation.Hunt && d.Owner == Side.FreePeoples);

        public void Apply(PlayerView view)
        {
            var eyeCount = Eyes(view);
            var freeTotal = FreeDice(view);
            var key = eyeCount + ":" + freeTotal;
            if (shown == key)
                return;
            shown = key;
            eyes.text = eyeCount.ToString();
            freeDice.text = freeTotal.ToString();
            // UI-160: both counters keep their place in the console, zero included.
            eyeEntry.text = "Eyes · " + eyeCount;
            eyeEntry.tooltip = "Shadow dice in the Hunt Box";
            freeEntry.text = "Free Peoples dice · " + freeTotal;
            freeEntry.tooltip = "Free Peoples movement dice in the Hunt Box";
            freeEntry.parent.style.display = freeTotal > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The Eye on the Shadow's enamel, as in the dice panel (UI-97).
        private static VisualElement Medallion()
        {
            var medallion = new VisualElement { pickingMode = PickingMode.Ignore };
            medallion.AddToClassList("hunt-eye-medallion");
            medallion.Add(new DieFaceIcon(DieFace.Eye));
            return medallion;
        }

        private static VisualElement Entry(VisualElement symbol, Label text)
        {
            var row = new VisualElement();
            row.AddToClassList("hunt-box-entry");
            symbol.AddToClassList("hunt-box-symbol");
            row.Add(symbol);
            row.Add(text);
            return row;
        }
    }
}
