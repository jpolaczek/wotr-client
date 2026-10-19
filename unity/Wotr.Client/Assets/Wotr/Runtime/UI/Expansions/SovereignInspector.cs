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
            title.text = sovereign.Title + " · " + SovereignPresentation.Status(sovereign.Status);
            region = sovereign.RegionId;
            location.text = data.Regions.TryGetValue(region, out var definition) ? definition.Name : "Out of play";
            location.SetEnabled(definition != null);
            state.text = SovereignPresentation.Corruption(sovereign) + (sovereign.Garrison ? " · Garrison" : "") +
                (sovereign.AttemptedThisTurn ? " · Attempt used" : "");
            state.tooltip = "Resistance " + sovereign.Resistance + (sovereign.Corruption.HasValue ? "" : " · Attached tile values are not public.");
            Tiles(sovereign);
            var abilityTitle = sovereign.Status == SovereignStatus.Initial ? "After awakening · Abilities" :
                sovereign.Status == SovereignStatus.Corrupted ? "Weaknesses · Hover" : "Abilities · Hover";
            Show(ability, sovereign.DefinitionId.Length > 0 ? abilityTitle : "");
            ability.tooltip = sovereign.DefinitionId.Length > 0 ? CharacterLibrary.Description(sovereign.DefinitionId) : "";
            var explanation = ResourcePresentation.Explain(view, sovereign.Id);
            availability.style.display = explanation.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            availability.tooltip = explanation;
            Attempt(view, sovereign.Id);
            Show(cost, SovereignPresentation.Cost(view, draft, sovereign.Id));
            Show(consequence, ownsDecision ? SovereignPresentation.Consequence(view, draft) : "");
        }

        private void Tiles(SovereignView sovereign)
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
                }
                face.text = ExpansionLabels.Tile(tile);
                face.tooltip = tile.Kings ? "Kings tile · Corruption 0" : tile.Value.HasValue
                    ? "Corruption " + tile.Value + (tile.Reveal ? " · Reveal" : "") : "Face-down Corruption tile";
            }
            tiles.style.display = faces.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
            label.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
