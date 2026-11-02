using System;
using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-424: the Effects panel's grid — columns, entry size and title type — is chosen from the number of sources and the
    // fixed height between the panel's highest place (the rail's top) and the bottom row, before layout, and never from the
    // panel's own geometry (TEST-15's three-column attempt did that and the layout failed to stabilize). The narrowest grid
    // whose rows fit wins: two columns before three before four, regular rows before dense ones. Each arrangement has one
    // fixed panel width, so nothing a source shows (its details, its × or a lit state) changes the panel's width or columns.
    // The sizes the stylesheet sets mirror the UI-424 ".effects-panel" rules of LeftRail.uss.
    public sealed class EffectsGridArrangement
    {
        // The panel's padding (6 + 7), its 32-point header and the header's 4-point margin.
        public const float Chrome = 49;
        // PlaceBesideTab keeps this much space between a left panel and the bottom of the map.
        public const float BottomMargin = 8;
        private const float EntryGap = 2;
        // The panel's side padding (8 + 8).
        private const float PanelPadding = 16;
        // Inside an entry: a 2-point border (gilt when selected), 3 points of side padding and 1 at top and bottom.
        private const float EntryBorder = 2;
        private const float EntryPaddingX = 3;
        private const float EntryPaddingY = 1;
        // The owner label "FP ◆" with its gap, the × with its gap, and a title tile's own side padding (the card symbol and
        // the parchment's frame; a plain tile's frame).
        private const float OwnerWidth = 20;
        private const float RemovalWidth = 25;
        private const float CardTilePadding = 28;
        private const float PlainTilePadding = 8;
        // The title's text stays this far inside its tile's top and bottom, within the parchment's frame.
        private const float TileInsetY = 4;

        // Every arrangement, the narrowest first; one instance each, so arrangements compare by reference.
        private static readonly EffectsGridArrangement[] Candidates =
        {
            new EffectsGridArrangement(2, false), new EffectsGridArrangement(2, true),
            new EffectsGridArrangement(3, false), new EffectsGridArrangement(3, true),
            new EffectsGridArrangement(4, false), new EffectsGridArrangement(4, true)
        };

        public static IReadOnlyList<EffectsGridArrangement> All => Candidates;
        public static EffectsGridArrangement Regular => Candidates[0];

        public int Columns { get; }
        // Dense rows keep the 32-point title target with less spacing and slightly smaller type.
        public bool Dense { get; }

        private EffectsGridArrangement(int columns, bool dense)
        {
            Columns = columns;
            Dense = dense;
        }

        // The narrowest arrangement whose panel fits the height; the densest one when none does.
        public static EffectsGridArrangement Choose(int sources, float availableHeight)
        {
            if (float.IsNaN(availableHeight) || availableHeight <= 0)
                return Regular;
            foreach (var candidate in Candidates)
                if (candidate.PanelHeight(sources) <= availableHeight)
                    return candidate;
            return Candidates[Candidates.Length - 1];
        }

        public string ClassName => "effects-columns-" + Columns + (Dense ? "-dense" : "");
        public float EntryWidth => Columns == 2 ? 165 : Columns == 3 ? 160 : 152;
        // A point more per entry and one for the row, so rounding each entry to the screen's pixels never wraps a row's last one.
        public float PanelWidth => Columns * (EntryWidth + EntryGap + 1) + PanelPadding + 1;
        public float TitleHeight => Dense ? 38 : 42;
        // The height a title's text has inside its tile's frame: two lines of 11.5 (regular) or 10 points (dense).
        public float TitleTextHeight => TitleHeight - 2 * TileInsetY;
        public float EntryHeight => TitleHeight + 2 * (EntryBorder + EntryPaddingY);
        public float RowGap => Dense ? 1 : EntryGap;
        public float TitleSize => Dense ? 11 : 12;

        public int Rows(int sources) => Math.Max(1, (sources + Columns - 1) / Columns);

        public float PanelHeight(int sources) => Chrome + Rows(sources) * (EntryHeight + RowGap);

        // The width a title's text wraps in: the entry less its border, padding, owner label, × and the tile's padding.
        public float TitleTextWidth(bool removable, bool card)
        {
            var tile = EntryWidth - 2 * (EntryBorder + EntryPaddingX) - OwnerWidth - (removable ? RemovalWidth : 0);
            return tile - (card ? CardTilePadding : PlainTilePadding);
        }

        public override string ToString() => ClassName;
    }
}
