using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-232: the Fellowship's Ring icon is the Hide control. While hiding is legal with the chosen die it glows and
    // pulses (ChoiceGlow) and says what a click does; otherwise it looks like a plain picture and a click does what
    // its host does (the plaque opens the panel, the panel's row locates the Fellowship).
    public sealed class FellowshipRingControl
    {
        public const string ClassName = "fellowship-ring-button";
        public Button Root { get; }
        public bool Offered { get; private set; }
        // While it does not hide, the icon still explains itself: the Fellowship's condition.
        private string description = "";
        private string offer = "";

        public FellowshipRingControl(string name, Image icon, Action hide, Action host)
        {
            Root = new Button(() =>
            {
                if (Offered)
                    hide();
                else
                    host();
            }) { name = name };
            Root.AddToClassList(ClassName);
            Root.Add(icon);
            // The host is a button too; it never takes the Ring's press as its own.
            Root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Root.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            Offer(false, "");
        }

        public void Offer(bool offered, string tooltip)
        {
            Offered = offered;
            offer = tooltip;
            Root.focusable = offered;
            Root.EnableInClassList(FellowshipStatus.HideClass, offered);
            ChoiceGlow.Apply(Root, offered);
            ShowTooltip();
        }

        public void Describe(string text)
        {
            description = text;
            ShowTooltip();
        }

        private void ShowTooltip() => Root.tooltip = Offered ? offer : description;
    }
}
