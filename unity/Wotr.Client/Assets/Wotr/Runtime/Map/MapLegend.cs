using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class MapLegend
    {
        private readonly MapCanvas map;
        private const int ColumnWidth = 336;
        private const int ColumnGap = 16;
        private const int PopupPadding = 10;
        private VisualElement overlayHost;
        private VisualElement column;
        public VisualElement Popup { get; } = new VisualElement { name = "map-legend", focusable = true };
        public Button Toggle { get; }
        // UI-218: switches the political view; it sits above the legend's button.
        public Button PoliticalToggle { get; }

        public MapLegend(MapCanvas map)
        {
            this.map = map;
            // UI-138: the legend opens from the help icon; the tooltip names it.
            Toggle = IconButton.Create("help", "Map legend · What the map's colours, symbols and markers mean",
                () => SetOpen(Popup.resolvedStyle.display == DisplayStyle.None));
            Toggle.name = "map-legend-toggle";
            Toggle.style.position = Position.Absolute;
            Toggle.style.left = 12;
            Toggle.style.bottom = 12;
            Toggle.style.width = Toggle.style.height = 32;
            Toggle.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            PoliticalToggle = IconButton.Create("political",
                "Political view (P) · Regions in their nations' colours; stripes mark settlements held by the other side",
                () => map.SetPoliticalView(!map.PoliticalView));
            PoliticalToggle.name = "map-political-toggle";
            PoliticalToggle.style.position = Position.Absolute;
            // UI-518: in the bottom row beside the chat, so the column above the legend is free for the rail's Victory plate.
            PoliticalToggle.style.left = 92;
            PoliticalToggle.style.bottom = 12;
            PoliticalToggle.style.width = PoliticalToggle.style.height = 32;
            PoliticalToggle.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            map.PoliticalViewChanged += SyncPoliticalToggle;
            SyncPoliticalToggle();
            Popup.style.position = Position.Absolute;
            Popup.style.width = 2 * ColumnWidth + ColumnGap + 2 * PopupPadding;
            Popup.style.paddingLeft = Popup.style.paddingRight = PopupPadding;
            Popup.style.paddingTop = Popup.style.paddingBottom = 8;
            Popup.style.fontSize = 12;
            Popup.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Popup.RegisterCallback<WheelEvent>(e => e.StopPropagation());
            AddContents();
            Popup.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape && Popup.resolvedStyle.display != DisplayStyle.None)
                {
                    SetOpen(false);
                    e.StopPropagation();
                }
            });
            map.RegisterCallback<GeometryChangedEvent>(_ => PositionPopup());
            map.RegisterCallback<DetachFromPanelEvent>(_ => Popup.RemoveFromHierarchy());
            SetOpen(false);
            map.Add(Toggle);
            map.Add(PoliticalToggle);
            map.Add(Popup);
        }

        public void SetOpen(bool open)
        {
            Popup.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
            {
                var host = overlayHost ?? map.parent ?? map;
                if (Popup.parent != host)
                    host.Add(Popup);
                Popup.BringToFront();
                PositionPopup();
                Popup.Focus();
            }
        }

        public void SetOverlayHost(VisualElement host)
        {
            if (overlayHost == host)
                return;
            if (overlayHost != null)
                overlayHost.UnregisterCallback<GeometryChangedEvent>(HostGeometryChanged);
            overlayHost = host;
            host.Add(Toggle);
            host.Add(PoliticalToggle);
            host.Add(Popup);
            host.RegisterCallback<GeometryChangedEvent>(HostGeometryChanged);
            PositionPopup();
        }

        private void HostGeometryChanged(GeometryChangedEvent e) => PositionPopup();

        private void SyncPoliticalToggle() => PoliticalToggle.EnableInClassList("selected", map.PoliticalView);

        private void PositionPopup()
        {
            if (Popup.parent == null)
                return;
            var source = overlayHost ?? map;
            var area = source.ChangeCoordinatesTo(Popup.parent, source.contentRect);
            Popup.style.left = area.xMin + 52;
            Popup.style.bottom = Popup.parent.contentRect.yMax - area.yMax + 12;
        }

        private void AddContents()
        {
            // TEST-21: two columns, so the whole legend stands above the bottom row at 1366×768 and 125 %.
            var columns = new VisualElement { name = "map-legend-columns", style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };
            Popup.Add(columns);
            column = Column(columns);
            var heading = Text("Map legend", "map-legend-heading");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            column.Add(heading);
            column.Add(Text("White — geographic passage\nBlack — impassable mountains\nColored band — nation\nWater — outside the regions"));
            var nations = new VisualElement { style = { flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, marginTop = 6, marginBottom = 6 } };
            foreach (var nation in map.Data.Borders.Palette.Style.Nations)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row,
                    alignItems = Align.Center, width = Length.Percent(50), height = 20 } };
                // UI-143: the swatch shows the band as drawn: the nation's wash with its darker edge.
                var palette = map.Data.Borders.Palette;
                var edge = palette.NationEdges[nation.Id];
                row.Add(new VisualElement { name = "map-legend-swatch-" + nation.Id,
                    style = { width = 12, height = 12, marginRight = 5, flexShrink = 0,
                        backgroundColor = palette.NationWashes[nation.Id],
                        borderTopWidth = 1, borderRightWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1,
                        borderTopColor = edge, borderRightColor = edge, borderBottomColor = edge, borderLeftColor = edge } });
                row.Add(Text(nation.Name, "map-legend-nation-" + nation.Id));
                nations.Add(row);
            }
            column.Add(nations);
            // UI-195: control is the settlement gem's colour; its cut tells the kind of settlement.
            column.Add(Text("Settlement gem: round — Town, emerald cut — City, gold setting — Stronghold, triangle — Fortification\n" +
                "Sapphire — Free Peoples control   Ruby — Shadow control\nHighlighted terrain — current option"));
            column = Column(columns);
            column.style.marginLeft = ColumnGap;
            AddPolitical();
            AddArmies();
            AddMordor();
        }

        private static VisualElement Column(VisualElement columns)
        {
            var added = new VisualElement { style = { width = ColumnWidth, flexShrink = 0 } };
            columns.Add(added);
            return added;
        }

        // UI-218: what the political view shows.
        private void AddPolitical()
        {
            var heading = Text("Political view (P)", "map-legend-political");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            column.Add(heading);
            column.Add(Text("Every region in its nation's colour. Stripes and an outline in sapphire or ruby: a settlement " +
                "held by the other side."));
        }

        // UI-154: the Mordor track, drawn with the same art as on the map.
        private void AddMordor()
        {
            var heading = Text("Mordor track", "map-legend-mordor");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            column.Add(heading);
            var entries = new VisualElement { name = "map-legend-mordor-entries", style = { flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, marginTop = 4 } };
            entries.Add(Entry("mordor/gate", "Entrance from Morannon or Minas Morgul"));
            entries.Add(Entry("mordor/slab", "Step of the Fellowship"));
            entries.Add(Entry("mordor/slab-next", "Next step for the die"));
            entries.Add(Entry("mordor/crack-of-doom", "Crack of Doom"));
            column.Add(entries);
        }

        // UI-126: the army markers, drawn with the same art as on the map.
        private void AddArmies()
        {
            var heading = Text("Armies — zoom in to see the figures", "map-legend-armies");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            column.Add(heading);
            var entries = new VisualElement { name = "map-legend-army-entries", style = { flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, marginTop = 4 } };
            // UI-406: the token is a puck, the state a rim drawn round it.
            entries.Add(Entry("armies/token-puck-free", "Field army: emblem, count"));
            entries.Add(Entry("armies/token-puck-garrison-shadow", "Garrison on the walls"));
            entries.Add(Entry("armies/token-puck-free", "Can act with the die", "armies/rim-token-rim-eligible"));
            entries.Add(Entry("armies/token-puck-free", "Selected force", "armies/rim-token-rim-selected"));
            entries.Add(Entry("armies/token-puck-free", "Already marched", "armies/rim-token-rim-moved"));
            column.Add(entries);
        }

        // An entry of the art kit: armies/<name> keeps its UI-126 element name, other art its category's. UI-406: a state
        // rim lies over a puck as on the map (the rim art is the puck's 40 × 32 outline 2 points clear on every side).
        private static VisualElement Entry(string art, string text, string rim = null)
        {
            var category = art.Substring(0, art.IndexOf('/'));
            var shown = rim ?? art;
            var row = new VisualElement { name = (category == "armies" ? "map-legend-army-" : "map-legend-" + category + "-") +
                shown.Substring(shown.IndexOf('/') + 1), style = { flexDirection = FlexDirection.Row,
                alignItems = Align.Center, width = Length.Percent(50), height = 24 } };
            var icon = new VisualElement { style = { width = 20, height = 20, marginRight = 5, flexShrink = 0,
                backgroundImage = Resources.Load<Texture2D>("Wotr/Art/UI/" + art),
                backgroundSize = new BackgroundSize(BackgroundSizeType.Contain) } };
            if (rim != null)
                icon.Add(Rim(rim));
            row.Add(icon);
            row.Add(Text(text));
            return row;
        }

        // The rim round a 20-point puck drawn 16 tall in its icon (20 × 16 inside 20 × 20, centred).
        private static VisualElement Rim(string art) => new VisualElement { pickingMode = PickingMode.Ignore, style =
        {
            position = Position.Absolute, left = -1, top = 1, width = 22, height = 18,
            backgroundImage = Resources.Load<Texture2D>("Wotr/Art/UI/" + art),
            backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100))
        } };

        private static Label Text(string value, string name = "")
        {
            return new Label(value) { name = name, style = { fontSize = 12, whiteSpace = WhiteSpace.Normal,
                marginLeft = 0, marginRight = 0, marginTop = 0, marginBottom = 0,
                paddingLeft = 0, paddingRight = 0, paddingTop = 0, paddingBottom = 0 } };
        }
    }
}
