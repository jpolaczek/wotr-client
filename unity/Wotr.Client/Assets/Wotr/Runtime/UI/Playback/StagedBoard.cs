using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306: the presented revision as one presentation stages it — a copy of the board's view in which only recorded
    // facts change (movers relocated, a region's control). It drives the map's markers and settlements
    // (BoardScreen.Stage), and becomes the map's figure baseline, so the release moves only what the presentation did not
    // show. The board's view, decision and windows stay as they were. It is copied once per presentation and discarded
    // when the presentation ends.
    public sealed class StagedBoard
    {
        private readonly BoardScreen board;
        private PlayerView staged;
        public bool Changed { get; private set; }
        public PlayerView View => staged ??= JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(board.View));

        public StagedBoard(BoardScreen board)
        {
            this.board = board;
        }

        // The recorded participants standing on the staged board, as the map draws them now.
        public IEnumerable<ActionParticipant> Present(IEnumerable<ActionParticipant> participants) =>
            participants.Where(p => View.Units.Any(u => u.Id == p.Id) || View.Characters.Any(c => c.Id == p.Id));

        public void Move(IEnumerable<ActionParticipant> movers, string region)
        {
            foreach (var mover in movers)
            {
                var unit = View.Units.FirstOrDefault(u => u.Id == mover.Id);
                if (unit != null)
                {
                    unit.Location = region;
                    unit.Garrison = mover.Garrison;
                }
                var character = View.Characters.FirstOrDefault(c => c.Id == mover.Id);
                if (character != null)
                {
                    character.Location = region;
                    character.Garrison = mover.Garrison;
                }
            }
            Changed = true;
        }

        public void Control(string region, Side? controller)
        {
            View.Control[region] = controller;
            Changed = true;
        }

        // Draw the staged markers and settlements on the presented board.
        public void Show() => board.Stage(View);

        // The staged positions are the map's figure baseline from here on.
        public void Settle()
        {
            if (Changed)
                board.Map.Figures.Rebase(View);
        }

        public void Reset()
        {
            staged = null;
            Changed = false;
        }
    }
}
