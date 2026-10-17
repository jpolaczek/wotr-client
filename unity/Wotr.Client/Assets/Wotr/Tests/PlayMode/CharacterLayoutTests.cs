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
    public class CharacterLayoutTests
    {
        [UnityTest]
        public IEnumerator EntriesAbilitiesAndAllShadowFiguresFitWithoutScrolling()
        {
            // Protocol fixtures exercise the densest layouts; CharacterNetworkTests verify real decisions.
            using (var client = new MountedMatchClient("http://localhost", "characters-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var kind in new[] { CommandKind.EnterCharacter, CommandKind.UseCharacterAbility, CommandKind.MoveShadowCharacters })
                        {
                            var view = Screen(kind);
                            client.Apply(view);
                            client.SetOnline();
                            yield return Settle();
                            if (kind != CommandKind.MoveShadowCharacters)
                            {
                                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("shadow-character"));
                                if (kind == CommandKind.UseCharacterAbility)
                                    client.Board.Hover.Open("orthanc");
                            }
                            yield return Settle();
                            if (kind == CommandKind.MoveShadowCharacters)
                                yield return AllShadowGroups(client, view);
                            LayoutTests.Save(target, $"characters-{kind}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        private static IEnumerator AllShadowGroups(MountedMatchClient client, PlayerView view)
        {
            FellowshipGestures.Die(client);
            foreach (var id in view.Options.Single().EligibleIds.Where(id => !id.EndsWith("mouth-of-sauron")))
            {
                FellowshipGestures.Figure(client, "minas-morgul", id);
                yield return Settle();
            }
            Assert.AreEqual(10, client.Board.Hover.Panel.Root.Query<Button>().ToList()
                .Count(b => b.name.StartsWith("hover-figure-") && b.ClassListContains("selected")));
            Assert.IsNull(client.Board.Tray.Q<Button>("new-companion-group"));
            Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Count > 8);
            Assert.IsEmpty(client.Board.Map.Query<Button>().ToList().Where(b => b.name.StartsWith("target-") && b.resolvedStyle.display != DisplayStyle.None),
                "Large flight range uses the map regions directly; cached labels must stay hidden.");
        }

        private static PlayerView Screen(CommandKind kind)
        {
            var view = new BoardFixture().View;
            view.MatchId = "characters-layout-" + kind;
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Dice.Add(new DieView { Id = "shadow-character", Owner = Side.Shadow, Face = DieFace.Character, Location = DieLocation.Available });
            view.Decision = new DecisionView { Id = kind.ToString(), Kind = DecisionKind.Action };
            view.Options.Clear();
            var option = new ActionOption { Kind = kind, EligibleDice = view.Dice.Where(d => d.Owner == Side.Shadow).Select(d => d.Id).ToList() };
            view.Options.Add(option);
            if (kind == CommandKind.MoveShadowCharacters)
            {
                view.Characters.Clear();
                foreach (var id in new[] { "witch-king", "mouth-of-sauron" })
                    view.Characters.Add(new CharacterView { Id = "base.character." + id, DefinitionId = "base.character." + id, Location = "minas-morgul", Side = Side.Shadow });
                view.Units.RemoveAll(u => u.Kind == UnitKind.Nazgul);
                for (var index = 0; index < 9; index++)
                    view.Units.Add(new UnitView { Id = "nazgul-" + index, Nation = "sauron", Kind = UnitKind.Nazgul, Location = "minas-morgul", Side = Side.Shadow });
                option.EligibleIds = view.Characters.Select(c => c.Id).Concat(view.Units.Where(u => u.Kind == UnitKind.Nazgul).Select(u => u.Id)).ToList();
                foreach (var id in option.EligibleIds)
                    option.Travel.Add(new TravelOption { Id = id, Origin = "minas-morgul", Range = 3, Flight = !id.EndsWith("mouth-of-sauron"), Targets = view.Control.Keys.ToList() });
                return view;
            }
            if (kind == CommandKind.EnterCharacter)
                foreach (var id in new[] { "saruman", "witch-king", "mouth-of-sauron" })
                    option.CharacterActions.Add(new CharacterActionOption { CharacterId = "base.character." + id, Title = CharacterLibrary.Name("base.character." + id), Dice = option.EligibleDice, Targets = { "orthanc", "minas-morgul" } });
            else
            {
                view.Characters.Add(new CharacterView { Id = "base.character.saruman", DefinitionId = "base.character.saruman", Location = "orthanc", Side = Side.Shadow });
                foreach (var ability in new[] { "recruit", "upgrade" })
                    option.CharacterActions.Add(new CharacterActionOption { CharacterId = "base.character.saruman", AbilityId = "voice-" + ability, Title = "Voice · " + ability, Dice = option.EligibleDice, Targets = { "orthanc" } });
            }
            option.EligibleIds = option.CharacterActions.SelectMany(c => c.Targets).Distinct().ToList();
            return view;
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 8; frame++)
                yield return null;
        }
    }
}
