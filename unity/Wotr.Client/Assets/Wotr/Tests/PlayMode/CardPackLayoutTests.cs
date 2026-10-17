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
    public class CardPackLayoutTests
    {
        [UnityTest]
        public IEnumerator AllCompanionsAndFiveMissesFitOneTrayAtBothSizesAndScales()
        {
            using (var client = new MountedMatchClient("http://localhost", "card-pack-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        client.Apply(Screen());
                        for (var frame = 0; frame < 12; frame++)
                            yield return null;
                        Assert.AreEqual(7, client.Board.Tray.Q<VisualElement>("effect-group-Companion").Query<Button>().ToList().Count);
                        Assert.AreEqual(5, client.Board.Tray.Q<VisualElement>("effect-group-Miss").Query<Button>().ToList().Count);
                        CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-choice-base.character.aragorn"));
                        CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-choice-miss-0"));
                        for (var frame = 0; frame < 8; frame++)
                            yield return null;
                        LayoutTests.Save(target, $"mighty-attack-{width}-{scale:0.00}.png");
                        FellowshipLayoutTests.Check(client.Board);
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        internal static PlayerView Screen()
        {
            var view = new BoardFixture().View;
            view.MatchId = "card-pack-layout";
            view.Decision = new DecisionView { Id = "mighty-attack", Kind = DecisionKind.EffectChoice, SourceId = "base.fp.character.06" };
            view.Characters.Clear();
            foreach (var name in new[] { "aragorn", "gandalf-grey", "gimli", "legolas", "boromir", "merry", "pippin" })
                view.Characters.Add(new CharacterView
                {
                    Id = "base.character." + name,
                    DefinitionId = "base.character." + name,
                    Side = Side.FreePeoples,
                    Location = "minas-tirith",
                    Leadership = name == "aragorn" ? 2 : 1
                });
            view.Battle = new BattleView
            {
                Id = "battle",
                RegionId = "lossarnach",
                Round = 1,
                CardsRevealed = true,
                Attacker = new BattleForceView
                {
                    Side = Side.FreePeoples,
                    RegionId = "minas-tirith",
                    Leadership = 8,
                    CombatDice = 5,
                    Strength = 2,
                    HitTarget = 5,
                    CardId = "base.fp.character.06",
                    CardCommitted = true,
                    Characters = view.Characters,
                    Units = view.Units.Where(u => u.Location == "minas-tirith").ToList()
                },
                Defender = new BattleForceView { Side = Side.Shadow, RegionId = "lossarnach", HitTarget = 5, CardCommitted = true }
            };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var character in view.Characters)
                option.EffectChoices.Add(new EffectChoiceView { Id = character.Id, SelectionGroup = "Companion" });
            for (var i = 0; i < 5; i++)
            {
                var die = new BattleDieView { Id = "miss-" + i, Result = i + 1, HitTarget = 6 };
                view.Battle.Attacker.Dice.Add(die);
                option.EffectChoices.Add(new EffectChoiceView { Id = die.Id, SelectionGroup = "Miss", Title = die.Result + " → Hit" });
            }
            option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }
    }
}
