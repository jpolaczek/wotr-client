using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Stable public inspector. UI-274: it informs only; the decisions are made on the portraits and their tiles.
    public sealed class SovereignInspector
    {
        public VisualElement Root { get; } = new VisualElement { name = "sovereign-inspector" };
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label state = Text("sovereign-state");
        private readonly Label ability = Text("sovereign-ability");
        private readonly Label attempt = Text("sovereign-attempt");
        private readonly Label revealed = Text("sovereign-revealed");
        private readonly Label cost = Text("sovereign-cost");
        private readonly Label consequence = Text("sovereign-consequence");
        // The explanation of an unavailable sovereign action is the tooltip of this note (no Help panel, UI-173).
        private readonly Label availability = new Label("Unavailable") { name = "sovereign-availability" };
        private readonly Button location;
        private readonly VisualElement tiles = BoardScreen.Row("sovereign-tiles");
        private readonly Dictionary<string, Label> faces = new Dictionary<string, Label>();
        private string region = "";
        // UI-345: the sovereign shown, so a changed value of the same one pulses and another one's values do not.
        private string shown = "";

        public SovereignInspector(BoardPresentation data, Action<string> focus, Action close)
        {
            this.data = data;
            Root.AddToClassList("expansion-inspector");
            Root.AddToClassList("sovereign-inspector");
            var header = BoardScreen.Row("information-heading");
            FloatingWindow.MarkHandle(header);
            header.Add(title);
            header.Add(cost);
            var dismiss = IconButton.Create("close", "Close", close);
            dismiss.name = "sovereign-close";
            dismiss.tooltip = "Close this panel · Keep your selection";
            header.Add(dismiss);
            Root.Add(header);
            location = BoardScreen.Button("", () => focus(region));
            location.name = "sovereign-location";
            var status = BoardScreen.Row("sovereign-status");
            status.Add(location);
            status.Add(state);
            status.Add(tiles);
            Root.Add(status);
            var details = BoardScreen.Row("sovereign-details");
            availability.AddToClassList("availability-reason");
            details.Add(ability);
            details.Add(availability);
            Root.Add(details);
            var progress = BoardScreen.Row("sovereign-progress");
            progress.Add(attempt);
            progress.Add(revealed);
            Root.Add(progress);
            Root.Add(consequence);
        }

        public void Apply(PlayerView view, SovereignView sovereign, TurnDraft draft, bool ownsDecision)
        {
            var same = sovereign.Id == shown;
            shown = sovereign.Id;
            Set(title, sovereign.Title + " · " + SovereignPresentation.Status(sovereign.Status), same);
            region = sovereign.RegionId;
            Set(location, data.Regions.TryGetValue(region, out var definition) ? definition.Name : "Out of play", same);
            location.SetEnabled(definition != null);
            Set(state, SovereignPresentation.Corruption(sovereign) + (sovereign.Garrison ? " · Garrison" : "") +
                (sovereign.AttemptedThisTurn ? " · Attempt used" : ""), same);
            state.tooltip = "Resistance " + sovereign.Resistance + (sovereign.Corruption.HasValue ? "" : " · Attached tile values are not public.");
            Tiles(sovereign, same);
            var abilityTitle = sovereign.Status == SovereignStatus.Initial ? "After awakening · Abilities" :
                sovereign.Status == SovereignStatus.Corrupted ? "Weaknesses · Hover" : "Abilities · Hover";
            Show(ability, sovereign.DefinitionId.Length > 0 ? abilityTitle : "");
            ability.tooltip = sovereign.DefinitionId.Length > 0 ? CharacterLibrary.Description(sovereign.DefinitionId) : "";
            var explanation = ResourcePresentation.Explain(view, sovereign.Id);
            Appearance.Display(availability, explanation.Length > 0);
            availability.tooltip = explanation;
            Attempt(view, sovereign.Id);
            Show(cost, SovereignPresentation.Cost(view, draft, sovereign.Id));
            Show(consequence, ownsDecision ? SovereignPresentation.Consequence(view, draft) : "");
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
            Appearance.Display(tiles, faces.Count > 0);
        }

        private void Attempt(PlayerView view, string id)
        {
            var current = view.CorruptionAttempt?.SovereignId == id ? view.CorruptionAttempt : null;
            Show(attempt, current == null ? "" : (current.Sequential ? "Sequential" : "Corruption attempt") + " · " +
                current.DrawnCount + " / " + current.DrawLimit + " drawn" + (view.You == Side.FreePeoples ? " · Shadow chooses" : ""));
            Show(revealed, current == null || current.RevealedTiles.Count == 0 ? "" : "Public tiles · " +
                string.Join(" · ", current.RevealedTiles.Select(TileFace)));
            revealed.tooltip = "Revealed by the server · Replaced tiles remain outside the pool until this attempt ends.";
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
