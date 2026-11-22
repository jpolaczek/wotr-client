using System.Collections;
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
    // UI-564 (owner, 2026-11-22: "during a battle, a click on a hero taking part in it should show information about him,
    // skills and so on, as in the Characters tab"): a battle portrait whose click chooses nothing opens the character's
    // window, the one the Characters tab opens, and its tooltip says so. Frame: ui564-battle-character.
    public sealed class BattleCharacterDetailsTests
    {
        private const string Gandalf = "base.character.gandalf-grey";

        [UnityTest]
        public IEnumerator ABattlePortraitOpensItsCharactersWindow()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var gandalf = new CharacterView
            {
                Id = Gandalf, DefinitionId = Gandalf, Side = Side.FreePeoples, Location = "minas-tirith", Level = 3, Leadership = 1
            };
            view.Characters.Add(gandalf);
            view.Battle.Attacker.Characters.Add(gandalf);
            view.Battle.Attacker.ParticipantIds.Add(Gandalf);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            var target = new RenderTexture(1920, 1080, 24);
            target.Create();
            client.Panel.targetTexture = target;
            client.Apply(view);
            yield return Frames(4);
            var battles = client.Board.Battles;
            if (!battles.IsOpen)
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
            yield return Frames(6);
            var portrait = battles.Root.Q("portrait-" + Gandalf);
            Assert.IsNotNull(portrait, "Gandalf's portrait stands in his force.");
            StringAssert.Contains(BattleContext.DetailsHint, portrait.tooltip);
            var inspector = client.Board.Characters.Inspector;
            Assert.AreEqual(DisplayStyle.None, inspector.Root.style.display.value, "No window before the click.");
            PointerGestureEvents.ClickAt(portrait);
            yield return Frames(6);
            Assert.AreEqual(DisplayStyle.Flex, inspector.Root.style.display.value, "The click opens the character's window.");
            Assert.AreEqual(Gandalf, inspector.FigureId);
            LayoutTests.Save(target, "ui564-battle-character.png");
            client.Panel.targetTexture = client.Viewport;
            target.Release();
            Object.Destroy(target);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }
    }
}
