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
    public class CardPack03LayoutTests
    {
        [UnityTest]
        public IEnumerator RegionAndDieChoicesKeepMapAndConfirmationVisibleWithoutLists()
        {
            using (var client = new MountedMatchClient("http://localhost", "card-pack-03-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var presentation in new[] { "Region", "ActionDie" })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(Screen(presentation));
                            for (var frame = 0; frame < 12; frame++)
                                yield return null;
                            Assert.IsNotNull(client.Board.Tray.Q<Label>("effect-board-choice"));
                            Assert.IsFalse(client.Board.Tray.Query<Button>().ToList().Any(b => b.name.StartsWith("effect-choice-")));
                            if (presentation == "Region")
                                MapContourProbe.ClickRegion(client.Board.Map, "dol-guldur");
                            else
                                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(client.Board.View.Options.Single().EligibleIds[1]));
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"pack03-{presentation}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static PlayerView Screen(string presentation)
        {
            var view = CardPackLayoutTests.Screen();
            view.MatchId = "card-pack-03-layout";
            view.Battle = null;
            view.Decision.Id = presentation;
            view.Decision.Presentation = presentation;
            view.Decision.SourceId = presentation == "Region" ? "base.fp.character.18" : "base.fp.character.13";
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1 };
            if (presentation == "Region")
                option.EligibleIds.AddRange(new[] { "dol-guldur", "minas-morgul", "barad-dur", "morannon", "moria", "mount-gundabad", "angmar" });
            else
                foreach (var die in view.Dice.Where(d => d.Owner == Side.FreePeoples))
                {
                    die.Location = DieLocation.Available;
                    die.Face = DieFace.Character;
                    option.EligibleIds.Add(die.Id);
                }
            foreach (var id in option.EligibleIds)
                option.EffectChoices.Add(new EffectChoiceView { Id = id, TargetId = presentation == "Region" ? id : "", Title = presentation == "Region" ? "Retreat" : "C → W" });
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }
    }
}
