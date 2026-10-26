namespace Wotr.Client
{
    // UI-307: one entry of a delta badge — the figures of one kind and nation that arrived (+), left (−) or were replaced
    // (From → Art, an upgrade or a new version of a character).
    public sealed class FigureDelta
    {
        public FigureArt? From { get; set; }
        public FigureArt Art { get; set; }
        public string Nation { get; set; } = "";
        public int Count { get; set; }

        // "+3" or "−1"; a replacement shows its count only when several were replaced ("×2").
        public string Text => From.HasValue ? Count > 1 ? "×" + Count : "" : Count > 0 ? "+" + Count : "−" + -Count;
    }
}
