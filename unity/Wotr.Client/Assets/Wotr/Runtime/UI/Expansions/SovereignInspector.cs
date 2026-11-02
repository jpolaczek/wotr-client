using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Stable public inspector. UI-274: it informs only; the decisions are made on the portraits and their tiles.
    // UI-436 (owner: "the window is too big, the information is vague"): a compact card framed like a character's window.
    // Under the title and status badge, the picture of his place (clicked, it shows the region on the map) beside his
    // corruption on a small meter and what it means; then his action in words (what it takes, available now or what it
    // still needs); then his abilities under a heading saying when they apply, as the character window's (UI-440: a row
    // of bare skill icons named in their tooltips; a click unrolls the printed text). Every king's window has the same
    // size at rest; an attempt's progress and a decision's consequence add their lines below.
    public sealed class SovereignInspector
    {
        public const string RulerWindowClass = "ruler-window";
        public VisualElement Root { get; } = new VisualElement { name = "sovereign-inspector" };
        public PlacePicture Place { get; }
        public AvailabilityRows Actions { get; } = new AvailabilityRows("sovereign-actions");
        public CharacterAbilityChips Abilities { get; } = new CharacterAbilityChips();
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label abilityHeading = Text("sovereign-ability");
        private readonly VisualElement badge = new VisualElement { name = "sovereign-window-status" };
        private readonly VisualElement badgeIcon = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Label badgeText = Text("sovereign-window-status-text");
        private readonly Label state = Text("sovereign-state");
        private readonly VisualElement meter = new VisualElement { name = "sovereign-window-meter" };
        private readonly Label meaning = Text("sovereign-meaning");
        private readonly Label flags = Text("sovereign-flags");
        private readonly Label attempt = Text("sovereign-attempt");
        private readonly Label revealed = Text("sovereign-revealed");
        private readonly Label cost = Text("sovereign-cost");
        private readonly Label consequence = Text("sovereign-consequence");
        private readonly VisualElement tiles = BoardScreen.Row("sovereign-tiles");
        private readonly Dictionary<string, Label> faces = new Dictionary<string, Label>();
        private string status = "";
        // UI-345: the sovereign shown, so a changed value of the same one pulses and another one's values do not.
        private string shown = "";

        public SovereignInspector(BoardPresentation data, Action<string> focus, Action close)
        {
            this.data = data;
            Root.AddToClassList("character-window");
            Root.AddToClassList("sovereign-inspector");
            Root.AddToClassList(RulerWindowClass);
            Place = new PlacePicture("sovereign-location", focus);
            Root.Add(Header(close));
            Root.Add(PlaceRow());
            Actions.Root.AddToClassList("character-window-actions");
            Root.Add(Actions.Root);
            abilityHeading.AddToClassList("ability-lines-heading");
            Root.Add(abilityHeading);
            Root.Add(Abilities.Root);
            var progress = BoardScreen.Row("sovereign-progress");
            progress.Add(attempt);
            progress.Add(revealed);
            Root.Add(progress);
            Root.Add(consequence);
        }

        private VisualElement Header(Action close)
        {
            var header = BoardScreen.Row("character-header");
            header.AddToClassList("information-heading");
            FloatingWindow.MarkHandle(header);
            header.Add(title);
            badge.AddToClassList("sovereign-window-status");
            badgeIcon.AddToClassList("sovereign-window-status-icon");
            badge.Add(badgeIcon);
            badge.Add(badgeText);
            header.Add(badge);
            header.Add(cost);
            var dismiss = IconButton.Create("close", "Close", close);
            dismiss.name = "sovereign-close";
            dismiss.tooltip = "Close this panel · Keep your selection";
            header.Add(dismiss);
            return header;
        }

        private VisualElement PlaceRow()
        {
            var row = BoardScreen.Row("character-place-row");
            row.Add(Place.Root);
            var facts = new VisualElement { name = "sovereign-facts" };
            facts.AddToClassList("character-place-facts");
            meter.AddToClassList("sovereign-window-meter");
            var corruption = BoardScreen.Row("sovereign-corruption");
            corruption.Add(state);
            corruption.Add(meter);
            facts.Add(corruption);
            meaning.AddToClassList("character-place-meaning");
            facts.Add(meaning);
            flags.AddToClassList("character-place-meaning");
            facts.Add(flags);
            facts.Add(tiles);
            row.Add(facts);
            return row;
        }

        public void Apply(PlayerView view, SovereignView sovereign, TurnDraft draft, bool ownsDecision)
        {
            var same = sovereign.Id == shown;
            shown = sovereign.Id;
            Set(title, sovereign.Title, same);
            Status(sovereign.Status, same);
            Place.Apply(PlacePresentation.For(sovereign, data.Regions));
            Corruption(sovereign, same);
            Tiles(sovereign, same);
            Actions.Apply(SovereignPresentation.Actions(view, sovereign));
            ShowAbilities(sovereign);
            Attempt(view, sovereign.Id);
            Show(cost, SovereignPresentation.Cost(view, draft, sovereign.Id));
            Show(consequence, ownsDecision ? SovereignPresentation.Consequence(view, draft) : "");
        }

        // His card's abilities under a heading saying when they apply; a removed king has none.
        private void ShowAbilities(SovereignView sovereign)
        {
            abilityHeading.text = SovereignPresentation.AbilityHeading(sovereign.Status);
            Abilities.Apply(sovereign.Status == SovereignStatus.Removed ? "" : sovereign.DefinitionId);
        }

        // The status badge: its icon and word, and in its tooltip what that state means.
        private void Status(SovereignStatus value, bool same)
        {
            var key = value.ToString().ToLowerInvariant();
            badgeIcon.RemoveFromClassList("sovereign-status-" + status);
            badgeIcon.AddToClassList("sovereign-status-" + key);
            if (same && status.Length > 0 && status != key)
                ValuePulse.Pulse(badge);
            status = key;
            badgeText.text = SovereignPresentation.StatusWord(value);
            badge.tooltip = SovereignPresentation.StatusMeaning(value);
        }

        // UI-383: a corrupted or removed king has no corruption count or resistance left to show.
        private void Corruption(SovereignView sovereign, bool same)
        {
            var initial = sovereign.Status == SovereignStatus.Initial;
            Set(state, SovereignPresentation.WindowCorruption(sovereign), same);
            state.tooltip = initial ? "Resistance " + sovereign.Resistance +
                (sovereign.Corruption.HasValue ? "" : " · Attached tile values are not public.") : SovereignPresentation.Corruption(sovereign);
            Meter(sovereign);
            meter.style.display = initial ? DisplayStyle.Flex : DisplayStyle.None;
            meaning.text = SovereignPresentation.CorruptionMeaning(sovereign);
            flags.text = SovereignPresentation.Flags(sovereign);
        }

        // A pip per point of resistance: filled for the known corruption, hatched for tiles whose values are hidden.
        private void Meter(SovereignView sovereign)
        {
            while (meter.childCount < sovereign.Resistance)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("sovereign-pip");
                meter.Add(pip);
            }
            while (meter.childCount > sovereign.Resistance)
                meter.RemoveAt(meter.childCount - 1);
            var known = sovereign.Corruption ?? 0;
            var hidden = sovereign.Corruption.HasValue ? 0 : sovereign.Tiles.Count(t => !t.Value.HasValue && !t.Kings);
            for (var index = 0; index < meter.childCount; index++)
            {
                meter[index].EnableInClassList("filled", index < known);
                meter[index].EnableInClassList("hidden", !sovereign.Corruption.HasValue && index < hidden);
            }
            meter.tooltip = TooltipText.Parts(SovereignPresentation.Corruption(sovereign), hidden > 0 ? TooltipText.Count(hidden, "hidden tile") : "");
        }

        // UI-345: a tile newly attached to the same sovereign fades in.
        private void Tiles(SovereignView sovereign, bool same)
        {
            var visible = sovereign.Tiles.Select(t => t.Id).ToHashSet();
            foreach (var id in faces.Keys.Where(id => !visible.Contains(id)).ToArray())
            {
                faces[id].RemoveFromHierarchy();
                faces.Remove(id);
            }
            foreach (var tile in sovereign.Tiles)
            {
                if (!faces.TryGetValue(tile.Id, out var face))
                {
                    face = Text("attached-tile-" + tile.Id);
                    face.AddToClassList("corruption-tile");
                    faces.Add(tile.Id, face);
                    tiles.Add(face);
                    if (same)
                        Appearance.Arrive(face);
                }
                face.text = ExpansionLabels.Tile(tile);
                face.tooltip = tile.Kings ? "Kings tile · Corruption 0" : tile.Value.HasValue
                    ? "Corruption " + tile.Value + (tile.Reveal ? " · Reveal" : "") : "Face-down Corruption tile";
            }
            tiles.style.visibility = faces.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void Attempt(PlayerView view, string id)
        {
            var current = view.CorruptionAttempt?.SovereignId == id ? view.CorruptionAttempt : null;
            Show(attempt, current == null ? "" : (current.Sequential ? "Sequential" : "Corruption attempt") + " · " +
                current.DrawnCount + " / " + current.DrawLimit + " drawn" + (view.You == Side.FreePeoples ? " · Shadow chooses" : ""));
            Show(revealed, current == null || current.RevealedTiles.Count == 0 ? "" : "Public tiles · " +
                string.Join(" · ", current.RevealedTiles.Select(TileFace)));
            revealed.tooltip = "Revealed in this attempt · Replaced tiles stay out of the Hunt pool until it ends.";
        }

        private string TileFace(string id)
        {
            data.HuntTiles.TryGetValue(id, out var tile);
            return tile == null ? "Tile" : FigureGrid.Name(tile.Value) + (tile.Reveal ? " ◉" : "");
        }

        private static Label Text(string name) => new Label { name = name, enableRichText = false };

        private static void Show(Label label, string text)
        {
            label.text = text;
            Appearance.Display(label, text.Length > 0);
        }

        private static void Set(TextElement label, string text, bool same)
        {
            if (same)
                ValuePulse.Set(label, text);
            else
                label.text = text;
        }
    }
}
