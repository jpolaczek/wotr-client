using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-03: one test match played from one client. The owner's session plays OwnerSide and the seat
    // session the other side; switching only changes which session opens the board.
    public sealed class TestSession : IDisposable
    {
        private readonly GameApi owner;
        private readonly GameApi seat;

        public TestSession(GameApi owner, TestSessionView view)
        {
            this.owner = owner;
            View = view;
            seat = owner.WithSession(view.Seat);
            Side = view.Scenario.StartingSide;
        }

        public TestSessionView View { get; }
        public Side Side { get; private set; }
        public GameApi Api => Side == View.OwnerSide ? owner : seat;

        public void SwitchSide() => Side = Side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;

        public void Dispose() => seat.Dispose();
    }
}
