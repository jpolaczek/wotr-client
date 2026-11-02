namespace Wotr.Client
{
    // UI-409/UI-440: the formatted tooltips' rich text (RuntimeTooltip.FormattedClass): the shared colours and tags.
    public static class RichText
    {
        public const string Gold = "#F2D488";
        public const string Bright = "#FFD670";
        public const string Dim = "#BEB49A";
        public const string Faint = "#8E8672";
        public const string Met = "#8FD18B";
        // A thin empty line between parts: a few points high, so the tooltip hardly grows.
        public const string Gap = "<size=5> </size>";

        public static string Bold(string text) => "<b>" + text + "</b>";

        public static string Color(string text, string color) => "<color=" + color + ">" + text + "</color>";

        public static string Small(string text) => "<size=11>" + text + "</size>";

        // Game text never carries a tag; a stray '<' is shown as a guillemet rather than read as one.
        public static string Text(string text) => (text ?? "").Replace("<", "‹");
    }
}
