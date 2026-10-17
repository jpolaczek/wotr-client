using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // Public presentation inputs for the offline Player benchmark, not rule fixtures.
    internal static class DesktopHuntProfileViews
    {
        public static void AddTo(Dictionary<string, PlayerView> views, PlayerView map)
        {
            views.Add("hunt-allocation", Allocation(Copy(map)));
            views.Add("hunt-reroll", Reroll(Copy(map)));
            views.Add("hunt-response", Response(Copy(map)));
            views.Add("hunt-report", Report(Copy(map)));
            views.Add("mordor-refresh", Mordor(Copy(map)));
        }

        private static PlayerView Copy(PlayerView map)
        {
            var view = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(map));
            view.Options.Clear();
            view.Decision = new DecisionView { Id = "profile-decision" };
            view.Fellowship.Guide = "base.character.gandalf-grey";
            view.OwnCompanions = new[] { "gandalf-grey", "strider", "gimli", "legolas", "boromir", "merry", "pippin" }
                .Select(id => new CharacterView { Id = "base.character." + id, Location = "fellowship", Level = 3 }).ToList();
            view.Fellowship.CompanionIdentities = view.OwnCompanions.Select(c => CharacterLibrary.Identity(c.Id)).ToList();
            return view;
        }

        private static PlayerView Allocation(PlayerView view)
        {
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Phase = GamePhase.HuntAllocation;
            view.Decision.Kind = DecisionKind.AllocateHunt;
            view.Dice.Clear();
            for (var i = 0; i < 7; i++)
                view.Dice.Add(new DieView { Id = "allocate-" + i, Owner = Side.Shadow, Location = DieLocation.Unrolled });
            view.Options.Add(new ActionOption { Kind = CommandKind.AllocateHunt, Maximum = 7,
                EligibleIds = view.Dice.Select(d => d.Id).ToList() });
            view.OwnCompanions.Clear();
            return view;
        }

        private static PlayerView Reroll(PlayerView view)
        {
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Decision.Kind = DecisionKind.HuntReroll;
            view.Hunt = new HuntView { SourceId = "fellowship-move", Bonus = 2, MovementBonus = 2, EffectBonus = 0, PoolCount = 12,
                RerollSources = { "army", "nazgul", "stronghold" } };
            for (var i = 0; i < 5; i++)
                view.Hunt.Dice.Add(new HuntDieView { Id = "roll-" + i, First = i + 1, Result = i + 1, Success = i >= 3 });
            view.Options.Add(new ActionOption { Kind = CommandKind.RerollHunt, Maximum = 3,
                EligibleIds = { "roll-0", "roll-1", "roll-2" } });
            view.OwnCompanions.Clear();
            return view;
        }

        private static PlayerView Response(PlayerView view)
        {
            view.Decision.Kind = DecisionKind.HuntDamage;
            view.Hunt = new HuntView { SourceId = "fellowship-move", TileId = "base.hunt.standard.3.0.1", Damage = 3, PoolCount = 12 };
            view.Options.Add(new ActionOption { Kind = CommandKind.ResolveHuntDamage,
                HuntResponses = { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion } });
            return view;
        }

        private static PlayerView Report(PlayerView view)
        {
            view.Decision = null;
            view.HuntReport = new HuntReportView { Id = "profile-report", Completed = true, Entries =
            {
                new HuntReportEntryView { Kind = HuntReportKind.Started, SourceId = "fellowship-move" },
                new HuntReportEntryView { Kind = HuntReportKind.Rolled, Hunt = new HuntView
                    { Dice = { new HuntDieView { First = 6, Result = 6, Success = true } } } },
                new HuntReportEntryView { Kind = HuntReportKind.TileDrawn, Hunt = new HuntView
                    { TileId = "base.hunt.standard.3.0.1", Damage = 3 } },
                new HuntReportEntryView { Kind = HuntReportKind.Corruption, Outcome = new HuntOutcomeView { Corruption = 3 } },
                new HuntReportEntryView { Kind = HuntReportKind.HuntCompleted }
            } };
            return view;
        }

        private static PlayerView Mordor(PlayerView view)
        {
            view.Decision = null;
            view.Fellowship.RegionId = "mordor-track";
            view.Fellowship.MordorStep = 4;
            view.Fellowship.Corruption = 4;
            view.Fellowship.MordorAttemptedThisTurn = false;
            return view;
        }
    }
}
