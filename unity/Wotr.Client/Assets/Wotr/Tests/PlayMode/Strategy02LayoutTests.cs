using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class Strategy02LayoutTests
    {
        [UnityTest]
        public IEnumerator EventArmyGroupsAndRoutesFitWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "strategy-02-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var mode in new[] { "march", "woses", "relief" })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(Screen(mode));
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            client.Board.Map.Select("minas-tirith");
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            client.Board.Map.Select("lossarnach");
                            for (var frame = 0; frame < 3; frame++)
                                yield return null;
                            if (mode == "march")
                                client.Board.Map.Select("pelargir");
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"strategy02-{mode}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            foreach (var tab in client.Board.Tray.Query<Button>(className: "figure-tab").ToList())
                            {
                                CardNetworkTests.Click(tab);
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                FellowshipLayoutTests.Check(client.Board);
                            }
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            Assert.IsNull(client.Board.Tray.Q<Button>("move-cost-Army"));
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static PlayerView Screen(string mode)
        {
            var view = new BoardFixture(true).View;
            view.MatchId = "strategy-02-layout-" + mode;
            view.Decision = new DecisionView { Id = mode, Kind = DecisionKind.ArmyEvent, SourceId = "base.fp.strategy.12" };
            view.Options.Clear();
            var army = new ArmyOption { RegionId = "minas-tirith", Garrison = mode == "woses" };
            army.UnitIds = view.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList();
            army.CharacterIds = view.Characters.Select(c => c.Id).ToList();
            foreach (var unit in view.Units.Where(u => army.UnitIds.Contains(u.Id)))
                unit.Garrison = army.Garrison;
            foreach (var character in view.Characters)
                character.Garrison = army.Garrison;
            var ids = army.UnitIds.Concat(army.CharacterIds).ToList();
            army.Targets.Add(new ArmyTargetOption
            {
                RegionId = "lossarnach",
                BattleMode = mode == "relief" ? BattleMode.Relief : (BattleMode?)null,
                Route = mode == "relief" ? new System.Collections.Generic.List<string>() : new System.Collections.Generic.List<string> { "lossarnach" },
                EligibleIds = ids
            });
            if (mode == "march")
                army.Targets.Add(new ArmyTargetOption { RegionId = "pelargir", Route = { "lossarnach", "pelargir" }, EligibleIds = ids });
            view.Options.Add(new ActionOption
            {
                Kind = mode == "relief" ? CommandKind.AttackArmy : CommandKind.MoveArmies,
                ArmyEvent = new ArmyEventOption { MaximumSteps = mode == "march" ? 2 : 1, RequiresCompanion = mode == "march", AllowLeaderLoss = mode == "woses" },
                Armies = { army },
                EligibleIds = ids,
                Targets = { "lossarnach", "pelargir" }
            });
            return view;
        }
    }
}
