using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class CardPack04LayoutTests
    {
        [UnityTest]
        public IEnumerator EventLossesTravelAndOptionalRecruitsFitWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "card-pack-04-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var mode in new[] { "losses", "journey", "recruits", "company" })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(Screen(mode));
                            for (var frame = 0; frame < 12; frame++)
                                yield return null;
                            Assert.AreEqual(mode != "company", client.Board.Tray.Q<Button>("confirm-decision") != null,
                                "A single upgrade commits on the chosen figure; sets keep one confirmation.");
                            if (mode == "journey")
                            {
                                // UI-215: the companions are lit in their region's panel, the destinations on the map.
                                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
                                var companions = client.Board.View.Options[0].EffectChoices.Where(c => c.SelectionGroup == "companions").Select(c => c.Id).ToList();
                                client.Board.Map.Select(client.Board.View.Characters.First(c => companions.Contains(c.Id)).Location);
                                for (var frame = 0; frame < 4; frame++)
                                    yield return null;
                                var lit = client.Board.Hover.Panel.Root.Query<Button>(className: "eligible-figure").ToList();
                                Assert.AreEqual(companions.Count, lit.Count(b => companions.Contains(b.name.Substring("hover-figure-".Length))));
                                MapContourProbe.ClickRegion(client.Board.Map, "pelargir");
                            }
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"pack04-{mode}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static PlayerView Screen(string mode)
        {
            var view = CardPackLayoutTests.Screen();
            view.MatchId = "card-pack-04-layout";
            view.Battle = null;
            view.Decision = new DecisionView { Id = mode, Kind = mode == "losses" ? DecisionKind.Losses : DecisionKind.EffectChoice, SourceId = "base.fp.character.22" };
            var option = new ActionOption { Kind = mode == "losses" ? CommandKind.TakeEventLosses : CommandKind.ChooseEffect };
            if (mode == "journey")
            {
                view.Decision.Presentation = "CompanionDestination";
                option.Minimum = 1;
                option.Maximum = 7;
                foreach (var region in new[] { "erech", "lamedon", "pelargir" })
                    option.EffectChoices.Add(new EffectChoiceView { Id = region, TargetId = region, SelectionGroup = "destination", Title = "Dead Men" });
                foreach (var character in view.Characters.Where(c => c.DefinitionId != "base.character.aragorn"))
                    option.EffectChoices.Add(new EffectChoiceView { Id = character.Id, SourceId = character.DefinitionId, SelectionGroup = "companions", GroupMinimum = 0, GroupMaximum = 6 });
                option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
            }
            else
            {
                view.Decision.Presentation = mode == "losses" ? "HitsUnits" : mode == "company" ? "ArmyFigures" : "ReserveFigures";
                option.Minimum = mode == "losses" ? 3 : mode == "company" ? 1 : 0;
                option.Maximum = mode == "company" ? 1 : 3;
                for (var i = 0; i < (mode is "losses" or "company" ? 10 : 3); i++)
                {
                    var id = "pack04-unit-" + i;
                    view.Units.Add(new UnitView { Id = id, Nation = "gondor", Side = Side.FreePeoples, Kind = i % 2 == 0 && mode == "losses" ? UnitKind.Elite : UnitKind.Regular, Location = mode == "losses" ? "pelargir" : "reserve" });
                    option.EligibleIds.Add(id);
                    option.EffectChoices.Add(new EffectChoiceView { Id = id, TargetId = "pelargir", SelectionGroup = mode == "company" ? "" : "recruits", GroupMinimum = mode == "company" ? 1 : 0, GroupMaximum = mode == "company" ? 1 : 3, Title = mode == "company" ? "Regular → Elite" : "" });
                }
                if (mode == "losses")
                    view.EventLoss = new EventLossView { Owner = Side.FreePeoples, SourceId = "base.fp.character.22", RegionId = "pelargir", Hits = 3, Rolls = { 3 }, ParticipantIds = option.EligibleIds };
            }
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }
    }
}
