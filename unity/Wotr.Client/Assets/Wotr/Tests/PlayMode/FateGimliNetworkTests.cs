using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    // UI-266: Gimli's abilities are used through the die in his region's panel and his Guide response in the Hunt;
    // the generic decision window never opens.
    public class FateGimliNetworkTests
    {
        private const string Figure = "base.character.gimli";
        [UnityTest] public IEnumerator RenownAdvancesWithoutActivatingAfterReconnect() => Wait(Politics(false));
        [UnityTest] public IEnumerator MessengerActivatesAndAdvancesAfterReconnect() => Wait(Politics(true));
        [UnityTest] public IEnumerator GuideLeavesDuringHuntAndMovesWithThePrintedRange() => Wait(Guide());

        private static async Task Politics(bool alternate)
        {
            using (var room = await Connect(alternate ? "fate-gimli-messenger" : "fate-gimli-renown", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                watch.OnItsObject(room.Fp);
                var ability = alternate ? "messenger-dwarves" : "politics-dwarves-at-fate-homelands";
                var region = alternate ? "ered-luin" : "iron-hills";
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.CharacterId == Figure && c.AbilityId == ability);
                var die = option.Dice[0];
                await ComponentGestures.Cost(room, room.Fp, die);
                Assert.AreEqual(region, option.Targets.Single());
                await Layout(room, room.Fp, alternate ? "messenger" : "renown", "fate-gimli");
                await Reconnect(room, room.Fp);
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Select(room.Fp, ComponentGestures.Character(option));
                await room.At(revision + 1);
                var nation = room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves");
                Assert.AreEqual(alternate, nation.Active);
                Assert.AreEqual(1, nation.StepsToWar);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(alternate ? "" : "dwarves", room.Sh.Board.View.Characters.Single(c => c.Id == Figure).NationIcon);
                watch.NeverOpened();
            }
        }

        private static async Task Guide()
        {
            using (var room = await Connect("fate-gimli-guide", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                await room.Settle();
                watch.OnItsObject(room.Fp);
                Click(room.Fp, "effect-choice-fate.gimli-guide");
                await Layout(room, room.Fp, "guide", "fate-gimli");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                await room.Direct(room.Fp, "action-DeclineReaction");
                Assert.AreEqual(DecisionKind.CompanionSeparation, room.Fp.Board.View.Decision.Kind);
                watch.OnItsObject(room.Fp);
                Assert.AreEqual(3, room.Fp.Board.View.Options.Single().Travel.Single().Range);
                // UI-105: South Dunland lies two regions away; one click on it sends Gimli there.
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("south-dunland"));
                // UI-382 (owner): the separation's portrait in the Fellowship panel is no larger than its companion icons.
                var portrait = room.Fp.Board.Tray.Q<Button>("companion-" + Figure);
                Assert.IsNotNull(portrait, "Gimli's portrait stands in the separation");
                Assert.LessOrEqual(portrait.resolvedStyle.width, 54.5f);
                Assert.LessOrEqual(portrait.resolvedStyle.height, 44.5f);
                // UI-375 (owner's report): the console asks for his region, not for highlighted characters.
                StringAssert.EndsWith("· Click a highlighted region to move there.", room.Fp.Board.Console.Status.Hint.text);
                StringAssert.StartsWith("Gimli", room.Fp.Board.Console.Status.Hint.text);
                await Layout(room, room.Fp, "departure", "fate-gimli");
                await Reconnect(room, room.Fp);
                var departure = room.Fp.Board.View.Revision;
                FellowshipGestures.FinishRoute(room.Fp, "south-dunland");
                await room.At(departure + 1);
                var gimli = room.Sh.Board.View.Characters.Single(c => c.Id == Figure);
                Assert.AreEqual("fate.character.gimli-dwarven-lord", gimli.DefinitionId);
                Assert.AreEqual("south-dunland", gimli.Location);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(0, room.Sh.Board.View.Fellowship.CompanionCount);
                watch.NeverOpened();
            }
        }
    }
}
