using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-232: the Fellowship's Ring icon is the Hide control. While hiding is legal with the chosen die it glows and
    // pulses (ChoiceGlow) and says what a click does; otherwise it is a plain picture and a click reaches its host.
    public sealed class FellowshipRingControl
    {
        public const string ClassName = "fellowship-ring-button";
        public Button Root { get; }
        public bool Offered { get; private set; }

        public FellowshipRingControl(string name, Image icon, Action hide)
        {
            Root = new Button(() =>
            {
                if (Offered)
                    hide();
            }) { name = name };
            Root.AddToClassList(ClassName);
            Root.Add(icon);
            Offer(false, "");
        }

        public void Offer(bool offered, string tooltip)
        {
            Offered = offered;
            Root.pickingMode = offered ? PickingMode.Position : PickingMode.Ignore;
            Root.focusable = offered;
            Root.EnableInClassList(FellowshipStatus.HideClass, offered);
            ChoiceGlow.Apply(Root, offered);
            Root.tooltip = offered ? tooltip : "";
        }
    }
}
