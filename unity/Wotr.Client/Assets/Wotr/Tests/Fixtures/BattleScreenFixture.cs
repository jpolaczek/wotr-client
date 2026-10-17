using System.Linq;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Fixtures
{
    // Presentation-only fixtures. Network tests use domain-generated scenarios.
    public static class BattleScreenFixture
    {
        public static PlayerView Create(CommandKind kind, bool stress = false, BattleMode mode = BattleMode.Field)
        {
            var fixture = new BoardFixture(stress);
            var view = fixture.View;
            view.MatchId = "battle-layout-" + kind + "-" + mode;
            view.Hand.Clear();
            view.Hand.AddRange(new[] { "base.fp.character.01", "base.fp.character.02", "base.fp.character.03", "base.fp.character.04", "base.fp.strategy.01", "base.fp.strategy.02" });
            var elite = fixture.Put("gondor", UnitKind.Elite, "minas-tirith");
            if (stress)
                view.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Location = "reserve";
            fixture.Put("sauron", UnitKind.Regular, "lossarnach", 5);
            fixture.Refresh();
            if (kind == CommandKind.AttackArmy)
            {
                var attack = view.Options.First(o => o.Kind == CommandKind.MoveArmies);
                attack.Kind = kind;
                attack.Maximum = 1;
                attack.EligibleIds = attack.Armies.SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToList();
                foreach (var army in attack.Armies)
                    foreach (var target in army.Targets)
                        target.BattleMode = mode;
                return view;
            }
            view.Battle = new BattleView { Id = "fixture-battle", RegionId = "lossarnach", Mode = mode, Round = 2, CardsRevealed = kind != CommandKind.CommitCombatCard };
            view.Battle.Attacker = Force(view, Side.FreePeoples, "minas-tirith", "base.fp.strategy.01");
            view.Battle.Defender = Force(view, Side.Shadow, "lossarnach", view.Battle.CardsRevealed ? "base.sh.strategy.01" : null);
            if (kind == CommandKind.CommitCombatCard)
            {
                view.You = Side.Shadow;
                view.DecisionOwner = Side.Shadow;
                view.Hand = view.Hand.Select(id => id.Replace(".fp.", ".sh.")).ToList();
                view.Battle.Attacker.CardId = null;
                view.Battle.Defender.CardCommitted = false;
                view.Battle.Defender.CardId = "";
                view.Battle.Attacker.Dice.Clear();
                view.Battle.Defender.Dice.Clear();
            }
            view.Options.Clear();
            var option = new ActionOption { Kind = kind, Maximum = 2 };
            view.Options.Add(option);
            view.Decision = new DecisionView { Id = view.MatchId, Kind = DecisionKind.BattleContinue };
            switch (kind)
            {
                case CommandKind.CommitCombatCard:
                    view.Decision.Kind = DecisionKind.CombatCard;
                    option.EligibleIds = view.Hand.ToList();
                    break;
                case CommandKind.RerollBattle:
                    view.Decision.Kind = DecisionKind.BattleReroll;
                    option.EligibleIds = view.Battle.Attacker.Dice.Where(d => !d.Hit && !d.Reroll.HasValue).Select(d => d.Id).ToList();
                    for (var i = 0; i < 10; i++)
                        view.Dice.Add(new DieView { Id = "shadow-die-" + i, Owner = Side.Shadow, Face = DieFace.ArmyMuster, Source = "standard", Location = DieLocation.Available });
                    break;
                case CommandKind.TakeBattleLosses:
                    view.Decision.Kind = DecisionKind.Losses;
                    option.Minimum = 2;
                    view.Battle.PendingHits = 2;
                    view.Battle.LossOwner = view.You;
                    option.EligibleIds = view.Battle.Attacker.Units.Where(ArmyDraft.IsCombat).Select(u => u.Id).ToList();
                    foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                        view.Battle.SpecialAttacks.Add(new BattleAttackView
                        {
                            Side = side,
                            SourceId = side == Side.FreePeoples ? "base.fp.strategy.01" : "base.sh.strategy.01",
                            Dice = Enumerable.Range(0, 6).Select(i => new BattleDieView { Id = "special-" + side + "-" + i, Result = 6, Hit = true, HitTarget = 5 }).ToList()
                        });
                    break;
                case CommandKind.AdvanceBattle:
                    view.Decision.Kind = DecisionKind.BattleAdvance;
                    option.EligibleIds = view.Battle.Attacker.ParticipantIds.ToList();
                    option.Targets.Add("lossarnach");
                    break;
                case CommandKind.ExtendSiege:
                    view.Decision.Kind = DecisionKind.SiegeContinuation;
                    option.EligibleIds.Add(elite.Id);
                    view.Options.Add(new ActionOption { Kind = CommandKind.StopBattle });
                    break;
                case CommandKind.RetreatBattle:
                    view.Decision.Kind = DecisionKind.BattleRetreat;
                    option.Targets.Add("pelargir");
                    view.Options.Add(new ActionOption { Kind = CommandKind.HoldBattle });
                    view.Options.Add(new ActionOption { Kind = CommandKind.RetreatIntoSiege });
                    break;
                case CommandKind.FightFieldBattle:
                    view.Decision.Kind = DecisionKind.StrongholdDefense;
                    view.Options.Add(new ActionOption { Kind = CommandKind.RetreatIntoSiege });
                    break;
                case CommandKind.ContinueBattle:
                    view.Options.Add(new ActionOption { Kind = CommandKind.StopBattle });
                    break;
            }
            return view;
        }

        private static BattleForceView Force(PlayerView view, Side side, string region, string card)
        {
            var force = new BattleForceView { Side = side, RegionId = region, CardCommitted = true, CardId = card, CardBack = "strategy", HitTarget = 5, CombatDice = 5, Leadership = 2 };
            force.Units = view.Units.Where(u => u.Side == side && u.Location == region).ToList();
            force.Characters = view.Characters.Where(c => c.Side == side && c.Location == region).ToList();
            force.ParticipantIds = force.Units.Select(u => u.Id).Concat(force.Characters.Select(c => c.Id)).ToList();
            force.Strength = force.Units.Count(ArmyDraft.IsCombat);
            for (var i = 1; i <= 5; i++)
                force.Dice.Add(new BattleDieView { Id = side + "-roll-" + i, Result = i, Hit = i >= 5, HitTarget = 5, Reroll = i == 2 ? 6 : (int?)null, RerollHit = i == 2 });
            return force;
        }
    }
}
