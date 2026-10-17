using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-246: a small gilt button for a choice that sits on a battle object without being one — an amount of hits
    // Heroic Death cancels, on the incoming hits. It glows while it can be chosen and is selected once chosen; its
    // size (a full 32 px click target) and place never change.
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
