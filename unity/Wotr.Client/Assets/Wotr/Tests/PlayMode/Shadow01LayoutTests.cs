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
    public class Shadow01LayoutTests
    {
        [UnityTest]
        public IEnumerator FullNazgulLeadershipAndDirectCorruptionFitBothSizesAndScales()
        {
            using (var client = new MountedMatchClient("http://localhost", "shadow-01-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var combat in new[] { true, false })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            var view = Screen(combat);
                            client.Apply(view);
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            if (combat)
                            {
                                foreach (var choice in view.Options[0].EffectChoices.TakeLast(2))
                                    CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-choice-" + choice.Id));
                                Assert.AreEqual(view.Options[0].EffectChoices.Count,
                                    client.Board.Tray.Q<VisualElement>("effect-group-Leadership").Query<Button>().ToList().Count);
                            }
                            else
                            {
                                for (var frame = 0; frame < 4; frame++)
                                    yield return null;
                                Assert.That(client.Board.Hunt.Root.Q<Label>("hunt-source").text, Does.Contain("Isildur"));
                                Assert.That(client.Board.Hunt.Root.Q<Label>("hunt-damage").text, Does.Contain("Corruption"));
                            }
                            for (var frame = 0; frame < 8; frame++)
                                yield return null;
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            LayoutTests.Save(target, $"shadow01-{(combat ? "cruel" : "bane")}-{width}-{scale:0.00}.png");
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        internal static PlayerView Screen(bool combat)
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            view.MatchId = "shadow-01-layout";
            view.Options.Clear();
            view.Fellowship.Guide = "base.character.gandalf-grey";
            if (!combat)
            {
                view.Fellowship.RegionId = "mordor-track";
                view.Fellowship.MordorStep = 3;
                view.Fellowship.Corruption = 6;
                view.Hunt = new HuntView { SourceId = "base.sh.character.06", TileId = "base.hunt.special-sh.die.0.1", DamagePending = true, CorruptionOnly = true, Mordor = true, PoolCount = 8 };
                view.Decision = new DecisionView { Id = "bane-replace", Kind = DecisionKind.Reaction, Window = "hunt.replace" };
                view.Options.Add(new ActionOption
                {
                    Kind = CommandKind.RespondEffect,
                    EligibleIds = { "card.mithril-replace" },
                    EffectChoices = { new EffectChoiceView { Id = "card.mithril-replace", SourceId = "base.fp.character.05", Cost = "DiscardTableCard" } }
                });
                view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction });
                return view;
            }
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Hand = Enumerable.Range(1, 6).Select(i => "base.sh.character." + i.ToString("D2")).ToList();
            fixture.Put("sauron", UnitKind.Nazgul, "lossarnach", 8);
            fixture.Put("sauron", UnitKind.Regular, "lossarnach", 3);
            var king = new CharacterView { Id = "base.character.witch-king", DefinitionId = "base.character.witch-king", Side = Side.Shadow, Leadership = 2, Location = "lossarnach" };
            view.Characters.Add(king);
            view.Decision = new DecisionView { Id = "cruel", Kind = DecisionKind.EffectChoice, SourceId = "base.sh.character.05", Presentation = "NazgulLeadership" };
            view.Battle = new BattleView
            {
                Id = "cruel-battle",
                Round = 1,
                CardsRevealed = true,
                RegionId = "lossarnach",
                Attacker = new BattleForceView { Side = Side.FreePeoples, RegionId = "minas-tirith", HitTarget = 5, CardCommitted = true, Units = view.Units.Where(u => u.Location == "minas-tirith").ToList() },
                Defender = new BattleForceView { Side = Side.Shadow, RegionId = "lossarnach", HitTarget = 5, CardCommitted = true, CardId = "base.sh.character.05", Leadership = 10, Characters = { king }, Units = view.Units.Where(u => u.Location == "lossarnach").ToList() }
            };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var unit in view.Units.Where(u => u.Kind == UnitKind.Nazgul))
                option.EffectChoices.Add(Point(unit.Id, 1, "Nazgûl"));
            option.EffectChoices.Add(Point(king.Id, 1, "Witch-king"));
            option.EffectChoices.Add(Point(king.Id, 2, "Witch-king"));
            option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
            view.Options.Add(option);
            return view;
        }

        private static EffectChoiceView Point(string figure, int number, string name) => new EffectChoiceView
        {
            Id = figure + ":leadership:" + number,
            SourceId = figure,
            Title = name + " ◆",
            Detail = "Forfeit one Leadership for this round.",
            SelectionGroup = "Leadership",
            GroupMinimum = 2,
            GroupMaximum = 2
        };
    }
}
