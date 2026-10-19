using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-273: with Lords and Kings together, the steps between the roll and the first action come in a fixed order;
    // the console's hint numbers the current one: special dice, Ruler discards, Dismay, Lore of the Elven Rings.
    public static class PreActionSteps
    {
        public const int Count = 4;

        public static int Step(PlayerView view)
        {
            if (view?.Configuration == null || !view.Configuration.Lords || !view.Configuration.Kings || view.Decision == null)
                return 0;
            return view.Decision.Kind switch
            {
                DecisionKind.SpecialDieChoice => 1,
                DecisionKind.RulerDiscard => 2,
                DecisionKind.Dismay => 3,
                DecisionKind.EffectChoice when view.Decision.Presentation == LoreChoice.Presentation => 4,
                _ => 0
            };
        }

        // A step whose objects say what to do keeps its name in the hint.
        public static string Label(PlayerView view, string hint)
        {
            var step = Step(view);
            return step == 0 ? hint : "Step " + step + " of " + Count + " · " + (hint.Length > 0 ? hint : Name(step));
        }

        private static string Name(int step) => step switch
        {
            1 => "Special dice",
            2 => "Ruler discards",
            3 => "Dismay",
            _ => "The Lore of the Elven Rings"
        };
    }
}
