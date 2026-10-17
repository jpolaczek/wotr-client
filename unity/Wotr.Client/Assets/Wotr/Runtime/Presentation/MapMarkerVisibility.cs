namespace Wotr.Client
{
    // Local presentation only. Selection and legal actions remain in the draft.
    // UI-126: armies are always shown; the zoom only chooses tokens or figures on a base, with hysteresis.
    public sealed class MapMarkerVisibility
    {
        public const float FiguresFrom = 1.3f;
        public const float TokensBelow = 1.2f;
        public bool Detail { get; private set; }
        public string InspectedRegion { get; set; } = "";

        public void UpdateZoom(float relativeZoom)
        {
            if (relativeZoom >= FiguresFrom)
                Detail = true;
            else if (relativeZoom < TokensBelow)
                Detail = false;
        }

        // Figures on a base: zoomed in, the region inspected by a click, or a selected force.
        public bool Figures(string region, bool selected = false) =>
            selected || Detail || region.Length > 0 && region == InspectedRegion;
    }
}
