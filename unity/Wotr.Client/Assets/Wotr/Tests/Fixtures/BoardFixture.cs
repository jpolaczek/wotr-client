using System;
using System.Linq;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Fixtures
{
    // Explicit UI fixtures. These options are not a substitute for server rules or integration tests.
    public sealed class BoardFixture
    {
        public BoardPresentation Data { get; } = BoardPresentation.Load();
        public PlayerView View { get; }
        public BoardFixture(bool stress = false)
        {
            View = new PlayerView
            {
                MatchId = "board-ui-fixture",
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Turn = 2,
                Phase = GamePhase.ActionResolution,
                Decision = new DecisionView { Id = "board-action", Kind = DecisionKind.Action },
                Fellowship = new FellowshipView { RegionId = "rivendell", Hidden = true, CompanionCount = 7 },
                Control = Data.Board.Regions.ToDictionary(r => r.Id, r => r.OriginalOwner)
            };
            View.Units = Data.Board.Units.Select(u => new UnitView
            {
                Id = u.Id,
                Nation = u.Nation,
                Kind = u.Kind,
                Location = "reserve",
                Side = Data.Board.Nations.Single(n => n.Id == u.Nation).Side
            }).ToList();
            View.Nations = Data.Board.Nations.Select(n => new NationView { Id = n.Id, Active = true }).ToList();
            View.Nations.First(n => n.Id == "elves").StepsToWar = 2;
            View.Dice.Add(new DieView { Id = "army", Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
            View.Dice.Add(new DieView { Id = "muster", Face = DieFace.Muster, Source = "standard", Location = DieLocation.Available });
            Put("gondor", UnitKind.Regular, "minas-tirith", stress ? 4 : 2);
            Put("rohan", UnitKind.Regular, "minas-tirith", stress ? 3 : 0);
            Put("north", UnitKind.Regular, "minas-tirith", stress ? 3 : 0);
            Put("gondor", UnitKind.Leader, "minas-tirith", 1);
            Put("north", UnitKind.Regular, "dale", 2);
            Put("gondor", UnitKind.Regular, "osgiliath", 1);
            if (stress)
            {
                foreach (var leader in View.Units.Where(u => u.Side == View.You && u.Kind == UnitKind.Leader))
                    leader.Location = "minas-tirith";
                for (var i = 0; i < 8; i++)
                    View.Characters.Add(new CharacterView { Id = "fixture-" + i, DefinitionId = "companion-" + i, Location = "minas-tirith", Level = 2, Leadership = 1 });
                for (var i = 1; i <= 8; i++)
                    View.Hand.Add("base.fp.character." + i.ToString("D2"));
            }
            Refresh();
        }

        public UnitView Put(string nation, UnitKind kind, string region, int count = 1)
        {
            UnitView last = null;
            foreach (var unit in View.Units.Where(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve").Take(count))
            {
                unit.Location = region;
                last = unit;
            }
            return last;
        }

        public void Refresh()
        {
            View.Options.Clear();
            var movement = new ActionOption { Kind = CommandKind.MoveArmies, Minimum = 1, Maximum = 2 };
            foreach (var die in View.Dice)
                foreach (var face in new[] { DieFace.Army, DieFace.Character })
                    if (die.Face == face || die.Face == DieFace.WillOfTheWest || die.Face == DieFace.ArmyMuster && face == DieFace.Army)
                    {
                        movement.MovementCosts.Add(new MovementCostOption { DieId = die.Id, Face = face, MaximumMoves = face == DieFace.Army ? 2 : 1, RequiresCommander = face == DieFace.Character });
                        if (!movement.EligibleDice.Contains(die.Id))
                            movement.EligibleDice.Add(die.Id);
                    }
            foreach (var group in View.Units.Where(u => u.Side == View.You && !u.Garrison && Data.Regions.ContainsKey(u.Location)).GroupBy(u => u.Location))
            {
                var army = new ArmyOption
                {
                    RegionId = group.Key,
                    UnitIds = group.Select(u => u.Id).ToList(),
                    CharacterIds = View.Characters.Where(c => c.Location == group.Key).Select(c => c.Id).ToList()
                };
                army.CommanderIds = group.Where(u => u.Kind == UnitKind.Leader).Select(u => u.Id).Concat(army.CharacterIds).ToList();
                foreach (var region in Data.Regions[group.Key].Neighbors)
                    army.Targets.Add(new ArmyTargetOption { RegionId = region, EligibleIds = army.UnitIds.Concat(army.CharacterIds).ToList() });
                movement.Armies.Add(army);
            }
            View.Options.Add(movement);
            var muster = new ActionOption
            {
                Kind = CommandKind.Recruit,
                Minimum = 1,
                Maximum = 2,
                EligibleDice = View.Dice.Where(d => d.Face is DieFace.Muster or DieFace.ArmyMuster or DieFace.WillOfTheWest).Select(d => d.Id).ToList()
            };
            foreach (var group in View.Units.Where(u => u.Location == "reserve" && u.Side == View.You).GroupBy(u => (u.Nation, u.Kind)))
            {
                if (View.Nations.First(n => n.Id == group.Key.Nation).StepsToWar > 0)
                    continue;
                muster.Recruitment.Add(new RecruitmentOption
                {
                    Nation = group.Key.Nation,
                    Kind = group.Key.Kind,
                    ReserveIds = group.Select(u => u.Id).ToList(),
                    Targets = Data.Board.Regions.Where(r => r.Nation == group.Key.Nation &&
                        r.Settlement is "Town" or "City" or "Stronghold").Select(r => r.Id).ToList()
                });
            }
            View.Options.Add(muster);
            View.Options.Add(new ActionOption
            {
                Kind = CommandKind.AdvancePolitics,
                EligibleDice = muster.EligibleDice.ToList(),
                Targets = View.Nations.Where(n => n.StepsToWar > 0).Select(n => n.Id).ToList()
            });
        }
    }
}
