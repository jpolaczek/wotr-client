using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-532 (owner, 2026-11-08: "more symbols, a small graphic as in Characters — less text, more useful information,
    // a better arrangement"; the owner chose a picture of the place with emblems): the region window's header. The
    // place's picture (Art/Places, UI-436) stands beside the region's name and one line of symbols: the settlement's gem
    // as the map draws it, in its controller's colours, with its kind; the nation's emblem with its name; the Victory
    // points as the VP icon and a number. The words the symbols stand for are the line's tooltip.
    internal static class RegionHeader
    {
        public const string ScoredClass = "scored";
        // The gem is drawn at the map's size (MapSettlementGems.Box) and shown at this share of it.
        private const float GemScale = .56f;

        public static VisualElement Create(RegionDefinition region, Side? control)
        {
            var header = Named(BoardScreen.Row("hover-header"), "hover-header");
            var picture = PlacePicture.Picture(region.Id);
            if (picture != null)
            {
                var place = new VisualElement { name = "hover-place", pickingMode = PickingMode.Ignore };
                place.style.backgroundImage = new StyleBackground(picture);
                header.Add(place);
            }
            var heading = new VisualElement { name = "hover-heading" };
            heading.Add(BoardScreen.Title(region.Name));
            heading.Add(Facts(region, control));
            header.Add(heading);
            return header;
        }

        // The settlement, the nation and the Victory points as symbols, in one line.
        private static VisualElement Facts(RegionDefinition region, Side? control)
        {
            var facts = Named(BoardScreen.Row("hover-region-facts"), "hover-region-facts");
            if (region.Settlement != null)
                facts.Add(Gem(region.Settlement, control));
            facts.Add(new Label(region.Settlement ?? "Wilderness") { name = "hover-region-state", pickingMode = PickingMode.Ignore });
            var nation = region.Nation == null ? "Neutral" : FigureGrid.Name(region.Nation);
            if (region.Nation != null)
            {
                facts.Add(Emblem(region.Nation));
                facts.Add(new Label(nation) { name = "hover-region-nation", pickingMode = PickingMode.Ignore });
            }
            var points = RegionVictoryText.For(region, control);
            if (points.Length > 0)
                facts.Add(Points(region, control));
            var controller = control.HasValue ? BoardScreen.SideName(control.Value) + " control" : "Uncontrolled";
            facts.tooltip = (region.Settlement ?? "Wilderness") + " · " + nation + " · " + controller +
                (points.Length > 0 ? "\n" + points : "");
            return facts;
        }

        // The settlement's gem, as its map gem in its controller's colours (SettlementGem, MapSettlementVisual).
        private static VisualElement Gem(string settlement, Side? control)
        {
            var stone = SettlementGem.For(settlement, control);
            var gem = new VisualElement { name = "hover-region-gem", pickingMode = PickingMode.Ignore };
            gem.style.width = MapSettlementGems.Box.x;
            gem.style.height = MapSettlementGems.Box.y;
            gem.style.scale = new Scale(Vector3.one * GemScale);
            var margin = -MapSettlementGems.Box.x * (1 - GemScale) / 2;
            gem.style.marginLeft = margin;
            gem.style.marginRight = margin;
            gem.style.marginTop = margin;
            gem.style.marginBottom = margin;
            gem.generateVisualContent += context => MapSettlementVisual.Draw(context.painter2D, MapSettlementGems.Box / 2, stone);
            return gem;
        }

        // A nation's emblem, the left rail's picture (Art/UI/icons/nations), loaded once per nation: the rail's own
        // stylesheet does not reach the region window.
        public static VisualElement Emblem(string nation)
        {
            var emblem = new VisualElement { pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("nation-emblem-" + nation);
            emblem.AddToClassList("hover-emblem");
            if (!Emblems.TryGetValue(nation, out var picture))
                Emblems[nation] = picture = Resources.Load<Texture2D>("Wotr/Art/UI/icons/nations/" + nation);
            if (picture != null)
                emblem.style.backgroundImage = new StyleBackground(picture);
            return emblem;
        }

        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> Emblems =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        private static VisualElement Points(RegionDefinition region, Side? control)
        {
            var points = Named(BoardScreen.Row("hover-region-points"), "hover-region-points");
            points.pickingMode = PickingMode.Ignore;
            points.Add(Icon("hover-points-icon"));
            points.Add(new Label(region.VictoryPoints.ToString()) { pickingMode = PickingMode.Ignore });
            var captor = region.OriginalOwner == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            points.EnableInClassList(ScoredClass, control == captor);
            return points;
        }

        private static VisualElement Named(VisualElement element, string name)
        {
            element.name = name;
            return element;
        }

        // A small picture from the stylesheet (Wotr.uss, UI-532).
        public static VisualElement Icon(string className)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("hover-icon");
            icon.AddToClassList(className);
            return icon;
        }
    }
}
