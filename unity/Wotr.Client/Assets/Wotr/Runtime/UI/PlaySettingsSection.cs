using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-571 (owner, 2026-11-22): the Play part of Settings, under the figures' size in the display column (the interface
    // column has no room at 1366 × 768, 125 %): the confirmations the player may switch on or off.
    public sealed class PlaySettingsSection
    {
        public VisualElement Root { get; } = new VisualElement { name = "play-settings" };
        private readonly InterfacePreferences preferences;
        private readonly Toggle unusedMoves = new Toggle("Ask before ending with moves left") { name = "confirm-unused-moves" };
        // UI-572: once a game, when the Fellowship may first step onto the Mordor track.
        private readonly Toggle mordor = new Toggle("Ask when the Fellowship can enter Mordor") { name = "ask-enter-mordor" };
        // UI-570: the player's own rolls and draws wait for a click before their result shows.
        private readonly Toggle draws = new Toggle("Roll my own dice and draws") { name = "confirm-own-draws" };

        public PlaySettingsSection(InterfacePreferences preferences)
        {
            this.preferences = preferences;
            Root.AddToClassList("play-settings");
            Root.Add(new Label("Play") { name = "play-heading" });
            unusedMoves.RegisterValueChangedCallback(e => preferences.SetConfirmUnusedMoves(e.newValue));
            unusedMoves.tooltip = "The ✓ that ends an action asks first while an army, a figure or a recruitment point is unused";
            Root.Add(unusedMoves);
            mordor.RegisterValueChangedCallback(e => preferences.SetAskToEnterMordor(e.newValue));
            mordor.tooltip = "Once a game, the first time the Fellowship stands where it may step onto the Mordor track; later its gate glows on the map";
            Root.Add(mordor);
            draws.RegisterValueChangedCallback(e => preferences.SetConfirmOwnDraws(e.newValue));
            draws.tooltip = "Your combat rolls, your cards' dice and draws and the Hunt tiles you draw wait for your click before their " +
                "result shows, and every Hunt waits for Start the Hunt (not the action dice at a turn's start)";
            Root.Add(draws);
            Render();
        }

        public void Render()
        {
            unusedMoves.SetValueWithoutNotify(preferences.ConfirmUnusedMoves);
            mordor.SetValueWithoutNotify(preferences.AskToEnterMordor);
            draws.SetValueWithoutNotify(preferences.ConfirmOwnDraws);
        }
    }
}
