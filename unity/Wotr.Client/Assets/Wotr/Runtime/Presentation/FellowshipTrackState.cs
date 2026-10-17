using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Printed track limits constrain presentation, never the authoritative value.
    public readonly struct FellowshipTrackState
    {
        public int Value { get; }
        public int Maximum { get; }
        public int Position => Math.Max(0, Math.Min(Maximum, Value));
        public bool BeyondTrack => Value > Maximum;
        public string Symbol { get; }
        public string Description { get; }

        private FellowshipTrackState(int value, int maximum, string symbol, string description)
        {
            Value = value;
            Maximum = maximum;
            Symbol = symbol;
            Description = description;
        }

        public static FellowshipTrackState Journey(FellowshipView view)
        {
            var value = view.MordorStep ?? view.Progress;
            var name = view.MordorStep.HasValue ? "Mordor" : "Progress";
            var explanation = view.MordorStep.HasValue
                ? "Resolve each Hunt tile before advancing. The tile may stop the move."
                : "Distance from the last known position. Hidden progress does not select a secret route.";
            return new FellowshipTrackState(value, view.MordorStep.HasValue ? 5 : 12,
                view.Hidden ? "hidden" : "revealed", name + " " + value + " · " + explanation);
        }

        public static FellowshipTrackState Corruption(FellowshipView view) => new FellowshipTrackState(
            view.Corruption, 12, "corruption", "Corruption " + view.Corruption + "/12 · At 12 or more, the Shadow wins immediately.");
    }
}
