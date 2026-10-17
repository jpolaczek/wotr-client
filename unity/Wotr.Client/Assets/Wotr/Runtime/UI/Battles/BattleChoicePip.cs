using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-246: a small gilt pip for a choice that sits on a battle object without being one — a Leadership point
    // beside its Nazgûl, a hit Heroic Death cancels on the incoming hits. It glows while it can be chosen and fills
    // once chosen; its size and place never change.
    public static class BattleChoicePip
    {
        public const string ClassName = "battle-choice-pip";

        public static Button Create(string name, string text, Action click)
        {
            var pip = new Button(click) { name = name, text = text, enableRichText = false };
            pip.AddToClassList(ClassName);
            return pip;
        }

        public static void Show(Button pip, bool offered, bool chosen, string tooltip)
        {
            pip.SetEnabled(offered || chosen);
            ChoiceGlow.Apply(pip, offered && !chosen);
            pip.EnableInClassList("selected", chosen);
            pip.tooltip = tooltip;
        }
    }
}
