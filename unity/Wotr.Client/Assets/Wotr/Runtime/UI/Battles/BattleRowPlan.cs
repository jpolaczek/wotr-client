using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-437: the rows the two columns of the Battle window share, so each side's statistics, card and roll stand level
    // with the other side's. A row or control only one side shows — the ledge of figures outside the fight, a roll, the
    // roll's pager, the hit buttons a card adds to it (Heroic Death, Orderly Withdrawal) — keeps an unseen copy on the
    // other side. The spacer under the shorter force row takes what is left (Battle.uss).
    public sealed class BattleRowPlan
    {
        public string AttackerShelf { get; }
        public string DefenderShelf { get; }
        public bool Shelf => AttackerShelf.Length > 0 || DefenderShelf.Length > 0;
        public bool Roll { get; }
        public bool Paged { get; }
        public bool CancelsHits { get; }

        public BattleRowPlan(BattleView battle, PlayerView view, TurnDraft draft, Action<Action> edit)
        {
            AttackerShelf = ShelfText(battle.Attacker, battle, view, draft);
            DefenderShelf = ShelfText(battle.Defender, battle, view, draft);
            Roll = BattleDiceStrip.Rolled(battle.Attacker, battle) || BattleDiceStrip.Rolled(battle.Defender, battle);
            Paged = BattleDiceStrip.Paged(battle.Attacker, battle) || BattleDiceStrip.Paged(battle.Defender, battle);
            CancelsHits = draft != null && edit != null && BattleObjectChoices.HitAmounts(view).Count > 0;
        }

        public string ShelfOf(BattleForceView force, BattleView battle) => force == battle.Attacker ? AttackerShelf : DefenderShelf;

        // The ledge names the figures outside the fight only while the player decides (draft); a report shows none.
        private static string ShelfText(BattleForceView force, BattleView battle, PlayerView view, TurnDraft draft) =>
            draft == null ? "" : BattleForceText.Nonparticipants(force, battle, view);
    }
}
