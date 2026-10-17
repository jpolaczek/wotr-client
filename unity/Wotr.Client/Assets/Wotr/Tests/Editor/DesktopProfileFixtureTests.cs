using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.Tests
{
    public class DesktopProfileFixtureTests
    {
        [Test]
        public void ExportPresentationWorkloadsWithFullMapAndCrowdedBattle()
        {
            var map = new BoardFixture();
            var printed = map.Data.Board.Units.ToDictionary(u => u.Id);
            foreach (var unit in map.View.Units)
                unit.Location = printed[unit.Id].Location;
            map.Refresh();
            for (var index = 1; index <= 8; index++)
                map.View.Hand.Add("base.fp.character." + index.ToString("D2"));
            for (var index = 0; index < 10; index++)
                map.View.Dice.Add(new DieView { Id = "profile-shadow-" + index, Owner = Side.Shadow,
                    Face = DieFace.Army, Location = DieLocation.Available });
            var battle = BattleScreenFixture.Create(CommandKind.TakeBattleLosses, true);
            battle.Battle.Round = 12;
            Assert.Greater(map.View.Units.Where(u => u.Location != "reserve").Select(u => u.Location).Distinct().Count(), 20);
            Assert.AreEqual(8, map.View.Hand.Count);
            Assert.GreaterOrEqual(battle.Battle.Attacker.Units.Count, 10);
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/ui13-profile-views.json"));
            var views = new Dictionary<string, PlayerView>
            {
                ["map"] = map.View,
                ["battle"] = battle
            };
            DesktopHuntProfileViews.AddTo(views, map.View);
            Assert.AreEqual(7, views.Count);
            Assert.AreEqual(7, views["hunt-allocation"].Options[0].EligibleIds.Count);
            Assert.AreEqual(2, views["hunt-reroll"].Hunt.MovementBonus);
            Assert.AreEqual(0, views["hunt-reroll"].Hunt.EffectBonus);
            Assert.AreEqual(3, views["hunt-response"].Options[0].HuntResponses.Count);
            Assert.IsTrue(views["hunt-report"].HuntReport.Completed);
            Assert.AreEqual(false, views["mordor-refresh"].Fellowship.MordorAttemptedThisTurn);
            File.WriteAllText(path, JsonConvert.SerializeObject(views));
            ExportActivityWorkload(Path.GetDirectoryName(path));
        }

        private static void ExportActivityWorkload(string output)
        {
            var wire = File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor/ActionLogWire.json"));
            var history = new ActionLogPage { MatchId = "profile-activity" };
            for (var index = 0; index < 5000; index++)
            {
                var entry = JsonConvert.DeserializeObject<ActionLogPage>(wire);
                var id = "profile-action-" + index;
                var step = entry.Steps[0];
                step.Id = step.ActionId = id;
                step.Revision = index;
                step.Turn = index / 20 + 1;
                var action = entry.Actions[0];
                action.Id = action.Version = id;
                action.Turn = step.Turn;
                history.Actions.Add(action);
                history.Steps.Add(step);
            }
            Assert.AreEqual(5000, history.Steps.Select(s => s.Id).Distinct().Count());
            File.WriteAllText(Path.Combine(output, "ui47-profile-actions.json"), JsonConvert.SerializeObject(history));
        }
    }
}
