using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-98: the Fellowship and Hunt plaques at the right end of the top bar. Hovering a plaque unrolls
    // its panel, so plaques carry no tooltip; Styles/TopBar.uss draws them in the left rail's tab frame.
    public static class EdgePlaque
    {
        public const string ClassName = "edge-plaque";
        public const string EmblemClass = "plaque-emblem";
        // Set while the plaque's panel is unrolled, whether by hovering or by its context.
        public const string OpenClass = "edge-plaque-open";

        // A main line over a small-caps caption, e.g. the last known position over Hidden.
        public static VisualElement Text(Label main, Label caption)
        {
            var text = Column("plaque-text");
            main.AddToClassList("plaque-main");
            caption.AddToClassList("plaque-caption");
            text.Add(main);
            text.Add(caption);
            return text;
        }

        public static VisualElement Column(string name)
        {
            var column = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            column.AddToClassList("plaque-column");
            return column;
        }

        // A symbol followed by its number.
        public static VisualElement Value(VisualElement symbol, Label number)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("plaque-value");
            symbol.AddToClassList("plaque-value-symbol");
            number.AddToClassList("plaque-number");
            row.Add(symbol);
            row.Add(number);
            return row;
        }
    }
}
