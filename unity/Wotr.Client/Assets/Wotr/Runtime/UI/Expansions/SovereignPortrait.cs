using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-274: one sovereign's fixed slot in the Characters panel: the painted portrait with its status badge and name, the
    // corruption meter (a pip per point of resistance), the Shadow's attempt button beside an Initial king, and the tiles
    // drawn for an attempt on him or shown by an inspection.
    public sealed class SovereignPortrait
    {
        public const string SlotClass = "sovereign-slot";
        public const string LitClass = "legal-target";
        public VisualElement Root { get; } = new VisualElement();
        public Button Portrait { get; }
        public Button Attempt { get; }
        private readonly string id;
        private readonly Image image = new Image { name = "sovereign-portrait-image", pickingMode = PickingMode.Ignore, scaleMode = UnityEngine.ScaleMode.ScaleToFit };
        private readonly VisualElement badge = new VisualElement { name = "sovereign-status", pickingMode = PickingMode.Ignore };
        private readonly Label title = new Label { name = "sovereign-name", pickingMode = PickingMode.Ignore, enableRichText = false };
        private readonly VisualElement meter = new VisualElement { name = "sovereign-meter" };
        private readonly VisualElement tiles = new VisualElement { name = "sovereign-tiles" };
        private readonly Action<string, string> tileChosen;
        private string status = "";
        private string tileKey = "";
        private string art = "";

        public SovereignPortrait(string id, Action portrait, Action attempt, Action<string, string> tileChosen)
        {
            this.id = id;
            this.tileChosen = tileChosen;
            Root.name = "sovereign-slot-" + id;
            Root.AddToClassList(SlotClass);
            Portrait = BoardScreen.Button("", portrait);
            Portrait.name = "sovereign-" + id;
            Portrait.AddToClassList("sovereign-portrait");
            Portrait.AddToClassList(RuntimeTooltip.RichHoverClass);
            Portrait.userData = new DraftToken(DraftObject.Sovereign, id);
            image.AddToClassList("sovereign-portrait-image");
            badge.AddToClassList("sovereign-status");
            Portrait.Add(image);
            Portrait.Add(badge);
            Attempt = new Button(attempt) { name = "sovereign-attempt-" + id };
            Attempt.AddToClassList("sovereign-attempt");
            title.AddToClassList("sovereign-name");
            meter.AddToClassList("sovereign-meter");
            tiles.AddToClassList("sovereign-tiles");
            Root.Add(Portrait);
            Root.Add(Attempt);
            Root.Add(title);
            Root.Add(meter);
            Root.Add(tiles);
        }

        // The hint says what a click on the portrait does now; without one the click only informs.
        public void Apply(PlayerView view, SovereignView king, TurnDraft draft, BoardPresentation data, string hint)
        {
            if (Art(king) != art)
            {
                art = Art(king);
                image.sprite = PortraitArtwork.Sprite(art);
            }
            Status(king.Status);
            title.text = king.Title.Split(' ')[0];
            var region = data.Regions.TryGetValue(king.RegionId, out var definition) ? definition.Name : "Out of play";
            var description = king.Title + " · " + SovereignPresentation.Status(king.Status) + " · " + region + (king.Garrison ? " · Garrison" : "") +
                "\n" + SovereignPresentation.Corruption(king);
            Portrait.tooltip = hint != null ? hint + "\n" + description : description;
            Portrait.EnableInClassList(LitClass, hint != null);
            ChoiceGlow.Apply(Portrait, hint != null);
            Meter(king);
            AttemptButton(view, draft, king);
            Tiles(SovereignAttempt.Tiles(view, king));
        }

        // An Initial king shows his awakened portrait, dimmed; a Corrupted one his corrupted portrait.
        private string Art(SovereignView king) => king.DefinitionId.Length > 0 ? king.DefinitionId
            : "kings.character." + id.Substring(id.LastIndexOf('.') + 1) + ".awakened";

        private void Status(SovereignStatus value)
        {
            var key = value.ToString().ToLowerInvariant();
            if (key == status)
                return;
            badge.RemoveFromClassList("sovereign-status-" + status);
            Portrait.RemoveFromClassList("sovereign-" + status);
            status = key;
            badge.AddToClassList("sovereign-status-" + status);
            Portrait.AddToClassList("sovereign-" + status);
        }

        // A pip per point of resistance: filled for the known corruption, hatched for tiles whose values are hidden.
        private void Meter(SovereignView king)
        {
            while (meter.childCount < king.Resistance)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("sovereign-pip");
                meter.Add(pip);
            }
            while (meter.childCount > king.Resistance)
                meter.RemoveAt(meter.childCount - 1);
            var known = king.Corruption ?? 0;
            var hidden = king.Corruption.HasValue ? 0 : king.Tiles.Count(t => !t.Value.HasValue && !t.Kings);
            for (var index = 0; index < meter.childCount; index++)
            {
                meter[index].EnableInClassList("filled", index < known);
                meter[index].EnableInClassList("hidden", !king.Corruption.HasValue && index < hidden);
            }
            meter.tooltip = SovereignPresentation.Corruption(king) + (hidden > 0 ? " · " + hidden + " hidden tile" + (hidden == 1 ? "" : "s") : "") +
                " · Resistance " + king.Resistance;
            meter.style.display = king.Status == SovereignStatus.Initial ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Beside an Initial king for the Shadow; it keeps its place, disabled with its reason, when unusable.
        private void AttemptButton(PlayerView view, TurnDraft draft, SovereignView king)
        {
            var shown = SovereignAttempt.Shown(view, king);
            Attempt.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            var enabled = shown && SovereignAttempt.Enabled(view, draft, king.Id);
            Attempt.SetEnabled(enabled);
            Attempt.EnableInClassList(LitClass, enabled);
            ChoiceGlow.Apply(Attempt, enabled);
            Attempt.tooltip = shown ? SovereignAttempt.Tooltip(view, draft, king) : "";
        }

        private void Tiles(IReadOnlyList<SovereignTile> shown)
        {
            var key = string.Join("|", shown.Select(t => t.Id + ":" + t.Value + ":" + t.Back + ":" + t.Selectable));
            if (key != tileKey)
            {
                tileKey = key;
                tiles.Clear();
                foreach (var tile in shown)
                    tiles.Add(TileButton(tile));
            }
            tiles.style.visibility = shown.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private Button TileButton(SovereignTile tile)
        {
            var tileId = tile.Id;
            var button = new Button(() => tileChosen(id, tileId)) { name = "corruption-choice-" + tileId, tooltip = tile.Tooltip };
            button.AddToClassList("sovereign-tile");
            button.AddToClassList(tile.Back ? "sovereign-tile-back" : tile.Kings ? "sovereign-tile-kings" : "sovereign-tile-face");
            if (!tile.Back)
                button.Add(new Label(tile.Kings ? "K" : tile.Value?.ToString() ?? "?") { pickingMode = PickingMode.Ignore });
            ReadOnlyControl.Apply(button, tile.Selectable);
            button.EnableInClassList(LitClass, tile.Selectable);
            ChoiceGlow.Apply(button, tile.Selectable);
            return button;
        }
    }
}
