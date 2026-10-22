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
    public class Strategy01LayoutTests
    {
        [UnityTest]
        public IEnumerator ProtectionBookAndScoutsFitWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "strategy-01-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var mode in new[] { "protection", "book", "scouts" })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            var view = Screen(mode);
                            client.Apply(view);
                            client.SetOnline();
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            if (mode == "protection")
                            {
                                client.Board.TableEffects.Open();
                                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(view.Options.Single().EligibleDice[0]));
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                CardNetworkTests.Click(client.Board.TableEffects.Root.Q<Button>("remove-table-base.fp.strategy.02"));
                            }
                            if (mode == "book")
                            {
                                // UI-294: the companion is chosen in its region's panel and moved by its destination's
                                // click; the window lists none of the journey's controls.
                                var gimli = view.Characters.Single(c => c.Id == "base.character.gimli");
                                FellowshipGestures.Figure(client, gimli.Location, gimli.Id);
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                MapContourProbe.ClickRegion(client.Board.Map, "lossarnach");
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                foreach (var name in new[] { "companion-base.character.gimli", "travel-stay-all", "travel-stay" })
                                    Assert.IsNull(client.Board.Root.Q<Button>(name), name);
                                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
                                Assert.IsTrue(client.Board.Map.PlannedJourneys.Regions.Contains("lossarnach"));
                            }
                            if (mode == "scouts")
                                MatchTestRoom.Click(client, "effect-choice-scouts.shelter");
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"strategy01-{mode}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            if (mode == "protection")
                                Assert.AreEqual(2, client.Board.TableEffects.Root.Query<Button>().ToList().Count(b => b.name.StartsWith("effect-source-")));
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static PlayerView Screen(string mode)
        {
            var view = CardPackLayoutTests.Screen();
            view.MatchId = "strategy-01-layout";
            view.Decision.Id = mode;
            view.Effects.Clear();
            view.Options.Clear();
            if (mode == "scouts")
            {
                view.Decision.Presentation = "RegionOrShelter";
                view.Decision.SourceId = "base.fp.strategy.05";
                var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1 };
                foreach (var region in new[] { "westemnet", "gap-of-rohan", "edoras" })
                    option.EffectChoices.Add(new EffectChoiceView { Id = region, TargetId = region, Title = "Retreat" });
                option.EffectChoices.Add(new EffectChoiceView { Id = "scouts.shelter", Title = "Withdraw into siege" });
                option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
                view.Options.Add(option);
                return view;
            }
            view.Battle = null;
            if (mode == "book")
            {
                view.Decision.Kind = DecisionKind.CompanionSeparation;
                view.Decision.SourceId = "base.fp.strategy.04";
                var option = new ActionOption { Kind = CommandKind.MoveCompanions, AllowEmptySelection = true };
                foreach (var character in view.Characters)
                {
                    option.EligibleIds.Add(character.Id);
                    option.Travel.Add(new TravelOption { Id = character.Id, Origin = character.Location, Range = 2, Targets = { character.Location, "lossarnach" } });
                }
                view.Options.Add(option);
                return view;
            }
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Decision.Kind = DecisionKind.Action;
            view.Dice.Add(new DieView { Id = "protection-cost", Owner = Side.Shadow, Face = DieFace.Army, Location = DieLocation.Available });
            view.Hand.Clear();
            view.Hand.AddRange(new[] { "base.sh.strategy.01", "base.sh.strategy.02", "base.sh.strategy.05", "base.sh.character.01", "base.sh.character.02", "base.sh.character.03" });
            var removal = new ActionOption { Kind = CommandKind.DiscardProtection, Minimum = 2, Maximum = 2, Targets = { "base.fp.strategy.02", "base.fp.strategy.03" } };
            removal.EligibleDice = view.Dice.Where(d => d.Owner == Side.Shadow).Select(d => d.Id).ToList();
            foreach (var card in view.Hand.Where(id => id != "base.sh.strategy.05"))
                removal.EffectChoices.Add(new EffectChoiceView { Id = card, SelectionGroup = card.Contains(".strategy.") ? "Army" : "Character" });
            removal.EligibleIds = removal.EffectChoices.Select(c => c.Id).ToList();
            view.Options.Add(removal);
            foreach (var region in new[] { "lorien", "rivendell", "grey-havens", "the-shire", "old-forest", "buckland" })
                view.Effects.Add(new ActiveEffectView
                {
                    Id = region,
                    SourceId = region is "lorien" or "rivendell" or "grey-havens" ? removal.Targets[0] : removal.Targets[1],
                    Owner = Side.FreePeoples,
                    TargetId = region,
                    Modifier = "PreventShadowArmyEntry",
                    Duration = "UntilRemoved"
                });
            return view;
        }
    }
}
