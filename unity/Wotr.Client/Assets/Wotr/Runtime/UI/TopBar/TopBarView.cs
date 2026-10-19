using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Composes the illustrated top bar from existing board controls; Styles/TopBar.uss supplies the artwork.
    public sealed class TopBarView
    {
        // Narrower bars tighten their spacing and hide the dice owner label; icons, values and tooltips remain.
        // Panel points follow the 1366 x 768 reference resolution, so 16:9 screens are 1366 points wide at 100%
        // and 1093 at 125%. Budget: phase, 10 dice with 2 special dice, four counters, status and Menu.
        public const float CompactBelow = 1340f;
        public const string CompactClass = "topbar-compact";
        public VisualElement Root { get; } = new VisualElement { name = "topbar" };
        public VisualElement Frame { get; } = new VisualElement { name = "topbar-frame", pickingMode = PickingMode.Ignore };
        // Cast onto the top of the map area, beneath every docked panel; never takes the pointer.
        public VisualElement Shadow { get; } = new VisualElement { name = "topbar-shadow", pickingMode = PickingMode.Ignore };
        public OpponentSummary Opponent { get; } = new OpponentSummary();
        // UI-263: the game's expansion crests after the phase, the variants in their tooltip.
        public VisualElement Crests { get; } = ExpansionCrests.Row("topbar-crests");

        public TopBarView(Label phase, VisualElement opponentDice, Label connection, Button menu)
        {
            Root.AddToClassList("topbar");
            // UI-98: at 44 points the whole bar lies in the 26-point edge-pan band, so like the other HUD
            // edges it pans only on the outer rim (MapEdgePan) and reading it never moves the map.
            Root.AddToClassList("map-edge-control");
            Frame.AddToClassList("topbar-frame");
            Shadow.AddToClassList("topbar-shadow");
            Root.Add(Frame);
            phase.AddToClassList("phase");
            Root.Add(phase);
            Root.Add(Crests);
            Root.Add(Divider());
            opponentDice.AddToClassList("row");
            Root.Add(opponentDice);
            Root.Add(Opponent.Root);
            connection.AddToClassList("muted");
            Root.Add(connection);
            Root.Add(menu);
            Root.RegisterCallback<GeometryChangedEvent>(e => UpdateCompact(e.newRect.width));
        }

        public bool Compact => Root.ClassListContains(CompactClass);

        private void UpdateCompact(float width)
        {
            var compact = width > 0 && width < CompactBelow;
            if (compact != Compact)
                Root.EnableInClassList(CompactClass, compact);
        }

        private static VisualElement Divider()
        {
            var divider = new VisualElement { name = "topbar-divider", pickingMode = PickingMode.Ignore };
            divider.AddToClassList("topbar-divider");
            return divider;
        }
    }
}
