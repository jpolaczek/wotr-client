using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-310: the Fellowship panel as a presentation reads it (the Politics panel's rule, UI-307). It opens read-only for
    // a beat, is never taken from the player's own decision it hosts, and is not opened again once the player closed it
    // during this presentation; at the release it closes again only when the presentation opened it.
    public sealed class FellowshipReading
    {
        private readonly BoardScreen board;
        private bool opened;
        private bool dismissed;

        public FellowshipReading(BoardScreen board)
        {
            this.board = board;
        }

        private FellowshipPanel Panel => board.Fellowship;

        // Whether the panel shows now.
        public bool Open()
        {
            if (opened && !Panel.IsOpen)
                dismissed = true;
            if (!Panel.IsOpen && !dismissed)
            {
                Panel.Open();
                opened = Panel.IsOpen;
                // The open Hunt window steps aside once the panel is laid out, as it avoids every open panel.
                if (opened)
                    Panel.Root.schedule.Execute(board.Hunt.Position);
            }
            return Panel.IsOpen;
        }

        public void Released()
        {
            if (opened && Panel.IsOpen && !Panel.Held)
                Panel.Close();
            opened = false;
            dismissed = false;
        }

        // A Companion's portrait in the roster (the Free Peoples name its rows by figure, the Shadow by identity), else
        // the Fellowship's plaque.
        public VisualElement Portrait(string id)
        {
            var root = Panel.Root;
            return PresentationTargets.First(root.Q("fellowship-portrait-" + id),
                root.Q("fellowship-portrait-" + CharacterLibrary.Identity(id)), Panel.Status);
        }
    }
}
