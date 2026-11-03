using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-461 (owner, 2026-11-03): the small menu at the start of the console's line that turns the chosen Will of the West
    // die into another result (WillFaceChoices): four fixed medallion slots — Character, Army, Muster, Event — beside the
    // hint, which keeps saying what the die lights. A slot with no use now is disabled, never hidden; the chosen result
    // stays pressed in. The menu stands only while the die waits for its use, so the line's height never changes.
    public sealed class ConsoleFaceMenu
    {
        public const string ChosenClass = "will-face-chosen";
        public VisualElement Root { get; } = new VisualElement { name = "console-will-faces" };
        public event Action<DieFace> Chosen;
        private readonly Dictionary<DieFace, Button> slots = new Dictionary<DieFace, Button>();
        private readonly ChoicePulse pulse;

        public ConsoleFaceMenu()
        {
            Root.AddToClassList("console-will-faces");
            Root.style.display = DisplayStyle.None;
            foreach (var face in WillOfTheWestUses.Faces)
                Root.Add(Slot(face));
            pulse = new ChoicePulse(Root);
        }

        public bool Shown => Root.style.display.value == DisplayStyle.Flex;

        public Button this[DieFace face] => slots[face];

        // Rebuilds nothing: the slots are fixed and only their state follows the draft.
        public void Apply(IReadOnlyList<WillFaceSlot> faces, bool enabled)
        {
            Root.style.display = faces.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var waiting = false;
            foreach (var slot in faces)
            {
                var button = slots[slot.Face];
                button.SetEnabled(enabled && slot.Enabled);
                button.EnableInClassList(ChosenClass, slot.Chosen);
                button.tooltip = slot.Tooltip;
                waiting |= slot.Chosen;
            }
            // UI-423's pattern: the results a click may give the die glow until one is chosen.
            foreach (var slot in faces)
                ChoiceGlow.Apply(slots[slot.Face], enabled && slot.Enabled && !waiting);
            pulse.Run(Shown && enabled && !waiting);
        }

        private Button Slot(DieFace face)
        {
            var button = new Button(() => Chosen?.Invoke(face)) { name = "will-face-" + DieFaceIcon.Key(face) };
            button.AddToClassList("ring-face");
            button.AddToClassList("die-free");
            button.AddToClassList("will-face");
            button.Add(new DieFaceIcon(face));
            slots[face] = button;
            return button;
        }
    }
}
