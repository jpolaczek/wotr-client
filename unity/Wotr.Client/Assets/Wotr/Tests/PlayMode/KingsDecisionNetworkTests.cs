using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    // UI-348 (EXP-15 D7, D17): Brand's Combat card replacement is a gilt chip beside the own card after the reveal, whose
    // click cancels the opponent's card; Keep card effects and the Palantír of Anárion's Skip extra card are the console's
    // Pass. No text button and no generic window remain.
    public class KingsDecisionNetworkTests
    {
        private const string KeepTheCard = "Pass · Keep your card's effects";

        [UnityTest] public IEnumerator HeirOfBardCancelsTheShadowCardFromItsChip() => Wait(Replacement("ui348-brand-heir", Side.FreePeoples));
        [UnityTest] public IEnumerator ShadowsInTheNorthCancelsTheFreePeoplesCardFromItsChip() => Wait(Replacement("ui348-brand-shadows", Side.Shadow));
        [UnityTest] public IEnumerator KeepingTheCardsEffectsIsThePass() => Wait(Keep());
        [UnityTest] public IEnumerator AnarionsSkipIsThePass() => Wait(Anarion());

        private static async Task Replacement(string scenario, Side owner)
        {
            using (var room = await Connect(scenario, ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = owner == Side.FreePeoples ? room.Fp : room.Sh;
                await room.Settle();
                Assert.AreEqual(CombatReplacementChoice.Presentation, client.Board.View.Decision.Presentation);
                foreach (var id in new[] { "replace", CombatReplacementChoice.Keep })
                    Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-" + id), id);
                Assert.AreEqual(KeepTheCard, client.Board.Console.Actions.Pass.tooltip);
                // The North's army attacks from Dale, so the Free Peoples' card is the attacker's.
                var own = owner == Side.FreePeoples ? "battle-attacker" : "battle-defender";
                var chip = client.Board.Tray.Q(own).Q<Button>("battle-replacement-replace");
                Assert.IsNotNull(chip, "The chip stands beside the own Combat card.");
                Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName));
                Assert.IsTrue(chip.Q("battle-replacement-arms").ClassListContains("nation-emblem-north"));
                StringAssert.StartsWith(owner == Side.FreePeoples ? "Click to use Heir of Bard · Cancels" : "Click to use Shadows in the North · Cancels",
                    chip.tooltip);
                await Layout(room, client, scenario, "ui348");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(client.Board.Tray.Q(own).Q<Button>("battle-replacement-replace"));
                await room.At(revision + 1);
                var battle = room.Fp.Board.View.Battle;
                var opponent = owner == Side.FreePeoples ? battle.Defender : battle.Attacker;
                Assert.IsTrue(opponent.CardCancelled, "The opponent's card is cancelled at initiative 0.");
                watch.NeverOpened();
            }
        }

        private static async Task Keep()
        {
            using (var room = await Connect("ui348-brand-heir-keep", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                StringAssert.Contains("Pass to keep your card's effects", client.Board.Console.Status.Hint.text);
                await room.Pass(client, KeepTheCard);
                Assert.AreNotEqual(CombatReplacementChoice.Presentation, client.Board.View.Decision?.Presentation ?? "");
                Assert.IsFalse(room.Fp.Board.View.Battle.Defender.CardCancelled);
                watch.NeverOpened();
            }
        }

        private static async Task Anarion()
        {
            using (var room = await Connect("ui348-anarion", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                Assert.AreEqual("Deck", client.Board.View.Decision.Presentation);
                Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-decline"), "Skip extra card is the console's Pass.");
                var hand = client.Board.View.Hand.Count;
                await room.Pass(client, "Pass · Complete the action without an extra card");
                Assert.AreEqual(hand, client.Board.View.Hand.Count);
                Assert.AreNotEqual("Deck", client.Board.View.Decision?.Presentation ?? "");
                watch.NeverOpened();
            }
        }
    }
}
