using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-462 (owner, 2026-11-03: "we need a graphical animation presenting Sméagol joining the Fellowship"): a Hunt's new
    // Guide the staged roster does not hold yet — Sméagol, whose tile brings him in as the Guide (LORDS pp. 21-22) — joins
    // the Fellowship before he takes the badge. The Fellowship panel opens; the drawn tile glows and his portrait, painted
    // with his artwork, rises out of it and flies, eased, into his new place in the roster, which stands empty until it
    // lands; then the portrait shows with the Guide's badge and glows. Both players see it, the one whose move drew the
    // tile too (a Hunt is a shared resolution, PresentationSelection). Reduced motion: no flight — he stands in the roster
    // with the badge and the mark at once. Skip or a rebase ends it: the portrait shows and the release draws the latest
    // roster. Gollum, who has no portrait, never joins.
    public sealed class FellowshipJoin
    {
        public const string PortraitClass = "presentation-join-portrait";
        private const string Gollum = "gollum";
        private static readonly Vector2 PortraitSize = new Vector2(56, 56);
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly FellowshipReading reading;
        private readonly PresentationMark mark;
        private string joining = "";

        public FellowshipJoin(BoardScreen board, StagedBoard staged, FellowshipReading reading, PresentationMark mark)
        {
            this.board = board;
            this.staged = staged;
            this.reading = reading;
            this.mark = mark;
        }

        // A new Guide who is no Companion of the staged roster yet.
        public bool Joins(string guide) => guide.Length > 0 && CharacterLibrary.Identity(guide) != Gollum && !staged.InRoster(guide);

        public IPresentationBeat Begin(string guide, double now)
        {
            joining = guide;
            if (board.ReducedMotion())
                return new StepsBeat(now, End, new List<(Action Start, double Seconds)>
                {
                    (() => reading.Open(), PresentationPace.PanelSettleSeconds),
                    (() => Arrive(guide), PresentationPace.HoldSeconds)
                });
            var tile = PresentationTargets.First(board.Hunt.Tile.Root, board.Hunt.Tab);
            return new DrawFlight(now, board.Dissolve.Root, () => reading.Portrait(guide), new[] { tile },
                () => Enter(guide, tile), _ => Arrive(guide), () => { }, End, PortraitClass, PortraitSize, Dress(guide));
        }

        // He stands in the roster, unseen, while the tile he comes from glows.
        private void Enter(string guide, VisualElement tile)
        {
            reading.Open();
            staged.Join(guide);
            board.StageRoster(staged.View);
            Show(guide, false);
            if (tile != null)
                mark.Show(() => tile);
        }

        // He is in the roster with the Guide's badge, marked.
        private void Arrive(string guide)
        {
            staged.Join(guide);
            staged.Guide(guide);
            board.StageRoster(staged.View);
            Show(guide, true);
            mark.Show(() => reading.Portrait(guide));
        }

        // The flying portrait wears his artwork.
        private static Action<VisualElement> Dress(string guide) => portrait =>
            portrait.Add(FigureArtwork.Image(FigureAppearances.Character(guide, Side.FreePeoples)));

        private void Show(string guide, bool shown)
        {
            var portrait = reading.Roster(guide);
            if (portrait == null)
                return;
            if (shown)
                portrait.style.opacity = StyleKeyword.Null;
            else
                portrait.style.opacity = 0;
        }

        public void End()
        {
            if (joining.Length > 0)
                Show(joining, true);
            joining = "";
            mark.End();
        }
    }
}
