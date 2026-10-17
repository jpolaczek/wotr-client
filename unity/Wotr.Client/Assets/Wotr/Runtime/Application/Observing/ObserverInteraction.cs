using System;

namespace Wotr.Client
{
    // OBS-03: the board of a game one watches — clicks only show: a region's forces and its battle, the region of an
    // army. Cards, tabs and panels open as on every board. Nothing is drafted, sent or decided, so nothing here can
    // change the game.
    public sealed class ObserverInteraction : IDisposable
    {
        private readonly BoardScreen board;

        public ObserverInteraction(BoardScreen board)
        {
            this.board = board;
            board.Map.RegionSelected += ShowRegion;
            board.Map.ArmySelected += ShowArmy;
        }

        private void ShowRegion(string region)
        {
            if (!board.Battles.SelectRegion(region))
                board.Hover.Toggle(region);
        }

        private void ShowArmy(DraftToken army) => ShowRegion(army.Id);

        public void Dispose()
        {
            board.Map.RegionSelected -= ShowRegion;
            board.Map.ArmySelected -= ShowArmy;
        }
    }
}
