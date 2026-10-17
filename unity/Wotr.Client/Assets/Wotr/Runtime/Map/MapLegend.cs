using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class MapLegend
    {
        private readonly MapCanvas map;
        private VisualElement overlayHost;
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
            Toggle.style.backgroundColor = new Color(.22f, .21f, .19f);
            Toggle.style.color = new Color(.98f, .92f, .76f);
            Toggle.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            PoliticalToggle = IconButton.Create("political",
                "Political view (P) · Regions in their nations' colours; stripes mark settlements held by the other side",
                () => map.SetPoliticalView(!map.PoliticalView));
            PoliticalToggle.name = "map-political-toggle";
            PoliticalToggle.style.position = Position.Absolute;
            PoliticalToggle.style.left = 12;
            PoliticalToggle.style.bottom = 52;
            PoliticalToggle.style.width = PoliticalToggle.style.height = 32;
            PoliticalToggle.style.backgroundColor = Toggle.style.backgroundColor;
            PoliticalToggle.style.color = Toggle.style.color;
            PoliticalToggle.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            map.PoliticalViewChanged += SyncPoliticalToggle;
            SyncPoliticalToggle();
            Popup.style.position = Position.Absolute;
            Popup.style.width = 356;
            Popup.style.paddingLeft = Popup.style.paddingRight = 10;
            Popup.style.paddingTop = Popup.style.paddingBottom = 8;
            Popup.style.backgroundColor = new Color(.13f, .126f, .117f, .98f);
            Popup.style.color = new Color(.95f, .94f, .89f);
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
            var heading = Text("Map legend", "map-legend-heading");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            Popup.Add(heading);
            Popup.Add(Text("White — geographic passage\nBlack — impassable mountains\nColored band — nation\nWater — outside the regions"));
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
            Popup.Add(nations);
            // UI-195: control is the settlement gem's colour; its cut tells the kind of settlement.
            Popup.Add(Text("Settlement gem: round — Town, emerald cut — City, gold setting — Stronghold, triangle — Fortification\n" +
                "Sapphire — Free Peoples control   Ruby — Shadow control\nHighlighted terrain — current option"));
            AddPolitical();
            AddArmies();
            AddMordor();
        }

        // UI-218: what the political view shows.
        private void AddPolitical()
        {
            var heading = Text("Political view (P)", "map-legend-political");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            Popup.Add(heading);
            Popup.Add(Text("Every region in its nation's colour. Stripes and an outline in sapphire or ruby: a settlement " +
                "held by the other side."));
        }

        // UI-154: the Mordor track, drawn with the same art as on the map.
        private void AddMordor()
        {
            var heading = Text("Mordor track", "map-legend-mordor");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            Popup.Add(heading);
            var entries = new VisualElement { name = "map-legend-mordor-entries", style = { flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, marginTop = 4 } };
            entries.Add(Entry("mordor/gate", "Entrance from Morannon or Minas Morgul"));
            entries.Add(Entry("mordor/slab", "Step of the Fellowship"));
            entries.Add(Entry("mordor/slab-next", "Next step for the die"));
            entries.Add(Entry("mordor/crack-of-doom", "Crack of Doom"));
            Popup.Add(entries);
        }

        // UI-126: the army markers, drawn with the same art as on the map.
        private void AddArmies()
        {
            var heading = Text("Armies — zoom in to see the figures", "map-legend-armies");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginTop = 6;
            Popup.Add(heading);
            var entries = new VisualElement { name = "map-legend-army-entries", style = { flexDirection = FlexDirection.Row,
                flexWrap = Wrap.Wrap, marginTop = 4 } };
            entries.Add(Entry("armies/token-field-free", "Field army: emblem, count"));
            entries.Add(Entry("armies/token-garrison-shadow", "Garrison on the walls"));
            entries.Add(Entry("armies/token-field-free-eligible", "Can act with the die"));
            entries.Add(Entry("armies/token-field-free-selected", "Selected force"));
            entries.Add(Entry("armies/token-field-free-moved", "Already marched"));
            Popup.Add(entries);
        }

        // An entry of the art kit: armies/<name> keeps its UI-126 element name, other art its category's.
        private static VisualElement Entry(string art, string text)
        {
            var category = art.Substring(0, art.IndexOf('/'));
            var row = new VisualElement { name = (category == "armies" ? "map-legend-army-" : "map-legend-" + category + "-") +
                art.Substring(category.Length + 1), style = { flexDirection = FlexDirection.Row,
                alignItems = Align.Center, width = Length.Percent(50), height = 24 } };
            var texture = Resources.Load<Texture2D>("Wotr/Art/UI/" + art);
            row.Add(new VisualElement { style = { width = 20, height = 20, marginRight = 5, flexShrink = 0,
                backgroundImage = texture, backgroundSize = new BackgroundSize(BackgroundSizeType.Contain) } });
            row.Add(Text(text));
            return row;
        }

        private static Label Text(string value, string name = "")
        {
            return new Label(value) { name = name, style = { fontSize = 12, whiteSpace = WhiteSpace.Normal,
                marginLeft = 0, marginRight = 0, marginTop = 0, marginBottom = 0,
                paddingLeft = 0, paddingRight = 0, paddingTop = 0, paddingBottom = 0 } };
        }
    }
}
