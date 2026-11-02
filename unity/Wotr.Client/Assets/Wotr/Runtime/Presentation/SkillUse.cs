using System;

namespace Wotr.Client
{
    // UI-391/UI-420/UI-440: what a skill icon's click does now, the last line of its tooltip — "Click to use your Muster die:
    // Move the Dwarves…" — or what it waits for. One wording for the window's icons, the roster's and the component buttons.
    public static class SkillUse
    {
        public const string ChooseDie = "Choose an eligible die first";
        public const string WithDie = "Usable with an eligible die or token";
        public const string Several = "Usable now · Click to choose how in the character's window";
        public const string OpenWindow = "Click to open the character's window";

        // The click uses the action with its die (the chosen one, or the one die that can pay for it).
        public static string Ready(ComponentAction action, string dieName) =>
            ClickToUse(DieUse.Tooltip(dieName ?? "", Effect(action)));

        public static string Ready(ComponentAction action, Func<string, string> dieName) =>
            Ready(action, dieName?.Invoke(action.DieId) ?? "");

        // UI-420 (owner): "Click to use your Muster die: Move the Dwarves…", not "Click to use · Use your Muster die: …".
        public static string ClickToUse(string use)
        {
            const string spend = "Use your ";
            return use.StartsWith(spend, StringComparison.Ordinal) ? "Click to use your " + use.Substring(spend.Length) : "Click to use · " + use;
        }

        private static string Effect(ComponentAction action) => action.Detail.Length > 0 ? action.Detail : "Use this ability.";
    }
}
