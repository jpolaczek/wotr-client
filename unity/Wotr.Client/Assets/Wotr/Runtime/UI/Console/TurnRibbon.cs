using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-167: the ribbon that crosses the top of the map when the action passes to the player, in the enamel of
    // their side, for about 1.5 s: it slides in (200 ms), holds and fades out (300 ms). It never takes the pointer,
    // blocks nothing and moves no camera. With reduced motion the console's plaque flashes once instead.
    public sealed class TurnRibbon
    {
        public const long InMilliseconds = 200;
        public const long HoldMilliseconds = 1000;
        public const long OutMilliseconds = 300;
        public VisualElement Root { get; } = new VisualElement { name = "turn-ribbon", pickingMode = PickingMode.Ignore };
        public Label Text { get; } = new Label { name = "turn-ribbon-text", pickingMode = PickingMode.Ignore };
        public int Shown { get; private set; }
        public bool IsShowing => Root.style.display.value == DisplayStyle.Flex;
        private readonly VisualElement band = new VisualElement { name = "turn-ribbon-band", pickingMode = PickingMode.Ignore };
        private IVisualElementScheduledItem leave;
        private IVisualElementScheduledItem hide;

        public TurnRibbon()
        {
            Root.AddToClassList("turn-ribbon");
            band.AddToClassList("turn-ribbon-band");
            Text.AddToClassList("turn-ribbon-text");
            band.Add(Text);
            Root.Add(band);
            Root.style.display = DisplayStyle.None;
        }

        public void Show(string text, Side side)
        {
            Shown++;
            Text.text = text;
            band.EnableInClassList("side-free", side == Side.FreePeoples);
            band.EnableInClassList("side-shadow", side == Side.Shadow);
            leave?.Pause();
            hide?.Pause();
            band.RemoveFromClassList("leaving");
            band.RemoveFromClassList("shown");
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            // The entry transition needs one drawn frame without the class.
            Root.schedule.Execute(() => band.AddToClassList("shown"));
            leave = Root.schedule.Execute(Leave).StartingIn(InMilliseconds + HoldMilliseconds);
        }

        private void Leave()
        {
            band.AddToClassList("leaving");
            band.RemoveFromClassList("shown");
            hide = Root.schedule.Execute(Hide).StartingIn(OutMilliseconds);
        }

        public void Hide()
        {
            leave?.Pause();
            hide?.Pause();
            band.RemoveFromClassList("shown");
            band.RemoveFromClassList("leaving");
            Root.style.display = DisplayStyle.None;
        }
    }
}
