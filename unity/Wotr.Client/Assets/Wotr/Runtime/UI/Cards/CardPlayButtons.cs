using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class CardPlayButtons
    {
        public VisualElement Root { get; } = BoardScreen.Row("card-play-buttons");
        private readonly Button primary;
        private readonly Button alternate;
        private readonly Action<DieFace?> play;
        private readonly Action react;
        private DieFace? firstFace;
        private DieFace? secondFace;
        private bool reaction;

        public CardPlayButtons(Action<DieFace?> play, Action react)
        {
            this.play = play;
            this.react = react;
            primary = MakeButton(Primary);
            primary.name = "play-card";
            alternate = MakeButton(() => this.play(secondFace));
            alternate.name = "play-card-alternate";
            Root.Add(primary);
            Root.Add(alternate);
        }

        // UI-164: a card whose reaction the current decision offers is used here, at the card, in one click.
        public void Reaction(bool enabled)
        {
            reaction = true;
            Caption("Use");
            primary.SetEnabled(enabled);
            primary.tooltip = "Use this card's reaction";
            Symbol(primary, null);
            alternate.style.display = DisplayStyle.None;
        }

        public void Refresh(bool combat, bool freeEvent, bool enabled, IEnumerable<EventCostOption> costs)
        {
            reaction = false;
            Caption("Play");
            var faces = costs.Select(c => c.Face).Distinct().ToArray();
            firstFace = combat || faces.Length == 0 ? null : faces[0];
            secondFace = faces.Length > 1 ? faces[1] : null;
            primary.SetEnabled(enabled);
            primary.tooltip = combat ? "Commit this Combat card face down" : freeEvent ? "Play this Event · No action die"
                : firstFace.HasValue
                ? "Play using " + firstFace + (firstFace == DieFace.Event ? " · Event die bonuses may open a reaction" : " · No Event die bonus")
                : "Select a matching die to play this Event";
            Symbol(primary, freeEvent ? null : firstFace);
            alternate.style.display = !combat && secondFace.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            alternate.SetEnabled(enabled);
            alternate.tooltip = "Play using " + secondFace + " · No Event die bonus";
            Symbol(alternate, secondFace);
        }

        private void Primary()
        {
            if (reaction)
                react();
            else
                play(firstFace);
        }

        private void Caption(string text) => primary.Q<Label>("card-play-caption").text = text;

        private static Button MakeButton(Action clicked)
        {
            var button = BoardScreen.Button("", clicked);
            button.Add(new Label("Play") { name = "card-play-caption", pickingMode = PickingMode.Ignore });
            return button;
        }

        private static void Symbol(Button button, DieFace? face)
        {
            if (Equals(button.userData, face))
                return;
            button.Q<ActionSymbol>()?.RemoveFromHierarchy();
            if (face.HasValue)
                button.Insert(0, new ActionSymbol(face));
            button.userData = face;
        }
    }
}
