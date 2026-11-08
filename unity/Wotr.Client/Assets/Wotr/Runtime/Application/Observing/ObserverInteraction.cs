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
            board.Map.MiniatureSelected += ShowMiniature;
        }

        private void ShowRegion(string region)
        {
            if (!board.Battles.SelectRegion(region))
                board.Hover.Toggle(region);
        }

        // UI-528: a click on figures opens their region's panel and never closes it.
        private void ShowArmy(DraftToken army) => ShowFigures(army.Id);

        private void ShowMiniature(DraftToken figure) => ShowFigures(figure.RegionId);

        private void ShowFigures(string region)
        {
            if (!board.Battles.SelectRegion(region))
                board.Hover.OpenOrKeep(region);
        }

        public void Dispose()
        {
            board.Map.RegionSelected -= ShowRegion;
            board.Map.ArmySelected -= ShowArmy;
            board.Map.MiniatureSelected -= ShowMiniature;
        }
    }
}
