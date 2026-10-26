using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class Shadow02LayoutTests
    {
        [UnityTest]
        public IEnumerator FlightStrikeVariableLeadershipAndAllBlackBreathTargetsFitWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "shadow02-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var mode in new[] { "dread", "terrible", "strike", "flight", "black", "weather" })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            var view = Screen(mode);
                            client.Apply(view);
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            if ((view.Battle != null || view.EventLoss != null) && !client.Board.Battles.IsOpen)
                            {
                                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                            }
                            if (mode == "flight")
                                yield return FlightClicks(client, view);
                            if (mode == "weather")
                            {
                                MapContourProbe.ClickRegion(client.Board.Map, "north-dunland");
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                Assert.IsTrue(client.Board.Map.DraftOverlay.PlannedRegions.Contains("north-dunland"));
                                Assert.IsNull(client.Board.Map.Q<Label>("fellowship-ghost"));
                                Assert.AreEqual("hollin", client.Board.View.Fellowship.RegionId);
                            }
                            if (mode == "black")
                            {
                                // UI-245/UI-289: the figures are chosen on the Battle window; the tray keeps Keep all.
                                FellowshipLayoutTests.Check(client.Board);
                                var decline = client.Board.Tray.Q<Button>("effect-choice-black-breath.decline");
                                var size = decline.MeasureTextSize(decline.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                                Assert.LessOrEqual(size.x, decline.contentRect.width + 1, "Keep all fits its button.");
                            }
                            if (mode is "dread" or "terrible")
                                foreach (var id in view.Options[0].EligibleIds.Take(mode == "dread" ? 10 : 1))
                                {
                                    // UI-246: a point of a figure in the battle is a pip on that figure.
                                    CardNetworkTests.Click(MatchTestRoom.Choice(client, "effect-choice-" + id));
                                    for (var frame = 0; frame < 2; frame++)
                                        yield return null;
                                }
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.Save(target, $"shadow02-{mode}-{width}-{scale:0.00}.png");
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        // UI-212: one click arms Lossarnach and every click on Hollin flies one Nazgûl; the Witch-king flies once he is
        // the only one left and his region is clicked again. The window stays closed and the console counts the flights.
        private static IEnumerator FlightClicks(MountedMatchClient client, PlayerView view)
        {
            var flights = view.Options[0].EligibleIds.Count;
            for (var click = 0; click < flights + 2; click++)
            {
                var armed = client.Board.Map.DraftOverlay.Targets.Contains("hollin");
                MapContourProbe.ClickRegion(client.Board.Map, armed ? "hollin" : "lossarnach");
                for (var frame = 0; frame < 3; frame++)
                    yield return null;
                if (client.Board.Console.Status.Hint.text.StartsWith("Nazgûl moved " + flights))
                    break;
            }
            Assert.That(client.Board.Console.Status.Hint.text, Does.StartWith("Nazgûl moved " + flights));
            CollectionAssert.AreEquivalent(new[] { "hollin" }, client.Board.Map.PlannedFlights.Regions);
            Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
            Assert.IsNull(client.Board.Tray.Q<Button>("new-companion-group"));
        }

        private static PlayerView Screen(string mode)
        {
            var view = mode == "black" ? CardPackLayoutTests.Screen() : Shadow01LayoutTests.Screen(true);
            view.MatchId = "shadow02-layout";
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Decision.Id = mode;
            if (mode is "dread" or "terrible")
            {
                view.Decision.SourceId = mode == "dread" ? "base.sh.character.08" : "base.sh.character.07";
                view.Battle.Defender.CardId = view.Decision.SourceId;
                view.Options[0].Minimum = 1;
                view.Options[0].Maximum = mode == "dread" ? 10 : 1;
                foreach (var choice in view.Options[0].EffectChoices)
                {
                    choice.GroupMinimum = 1;
                    choice.GroupMaximum = view.Options[0].Maximum;
                }
                return view;
            }
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1 };
            view.Options.Clear();
            view.Options.Add(option);
            if (mode == "black")
            {
                view.Decision.SourceId = "base.sh.character.11";
                view.Decision.Presentation = "LeaderOrCompanion";
                view.Battle.Attacker.CardId = "";
                view.Battle.Defender.CardId = view.Decision.SourceId;
                foreach (var character in view.Characters)
                    option.EffectChoices.Add(new EffectChoiceView { Id = character.Id, SelectionGroup = "Companion" });
                // As the server offers them: the leaders of the battle's forces.
                foreach (var unit in view.Battle.Attacker.Units.Concat(view.Battle.Defender.Units).Where(u => u.Kind == UnitKind.Leader))
                    option.EffectChoices.Add(new EffectChoiceView { Id = unit.Id });
                option.EffectChoices.Add(new EffectChoiceView { Id = "black-breath.decline", Title = "Keep all", Exclusive = true });
            }
            else
            {
                view.Battle = null;
                view.Decision.SourceId = "base.sh.character.11";
                view.Fellowship.RegionId = "hollin";
                view.Fellowship.Progress = 2;
                if (mode == "weather")
                {
                    view.Decision.SourceId = "base.sh.character.10";
                    view.Decision.Presentation = "Region";
                    option.EligibleIds.Add("north-dunland");
                    option.EffectChoices.Add(new EffectChoiceView { Id = "north-dunland", SourceId = "fellowship", TargetId = "north-dunland", Title = "Fellowship" });
                    return view;
                }
                if (mode == "flight")
                {
                    view.Decision.Kind = DecisionKind.CharacterEvent;
                    view.Decision.Presentation = "PortraitRoute";
                    option.Kind = CommandKind.MoveShadowCharacters;
                    option.AllowEmptySelection = true;
                    foreach (var figure in view.Units.Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Id).Concat(new[] { "base.character.witch-king" }))
                        option.Travel.Add(new TravelOption { Id = figure, Origin = "lossarnach", Flight = true, Targets = { "hollin", "lossarnach" } });
                    option.EligibleIds = option.Travel.Select(t => t.Id).ToList();
                    option.Targets.AddRange(new[] { "hollin", "lossarnach" });
                    return view;
                }
                view.Decision.Presentation = "NazgulStrike";
                foreach (var id in new[] { "base.fp.character.05", "base.fp.character.06", "base.fp.character.07", "base.fp.character.08" })
                    option.EffectChoices.Add(new EffectChoiceView { Id = id, SourceId = id, Title = CardLibrary.Title(id), Detail = "Discard this Free Peoples Character card from the table." });
                option.EffectChoices.Add(new EffectChoiceView { Id = "nazgul-strike.hunt", Title = "Roll for the Hunt" });
                option.EffectChoices.Add(new EffectChoiceView { Id = "nazgul-strike.decline", Title = "Finish" });
            }
            option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
            return view;
        }
    }
}
