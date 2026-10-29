using System.Collections;
using System.Linq;
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
    // UI-347 (EXP-15 D10, D12): the Lords choices of a battle stand on its objects. Orderly Withdrawal's cancelled hit is a
    // "−1" on the Shadow's hits and the console's Pass keeps the hit; the Mouth of Sauron's Evil Knowledge is his portrait
    // in the besieging column. No button repeats either.
    public class LordsBattleNetworkTests
    {
        private const string Mouth = "lords.character.mouth-of-sauron";
        private const string KeepTheHit = "Pass · Keep the hit and fight on";

        [UnityTest] public IEnumerator OrderlyWithdrawalCancelsAHitFromItsPip() => Wait(Withdrawal(true));
        [UnityTest] public IEnumerator ThePassKeepsTheHitWithoutWithdrawing() => Wait(Withdrawal(false));
        [UnityTest] public IEnumerator EvilKnowledgeIsTheMouthsPortrait() => Wait(EvilKnowledge());

        private static async Task Withdrawal(bool cancel)
        {
            using (var room = await Connect(cancel ? "ui347-orderly-withdrawal" : "ui347-orderly-withdrawal-pass", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                Assert.AreEqual("CombatEffect", client.Board.View.Decision.Presentation);
                foreach (var id in new[] { BattleObjectChoices.WithdrawalCancel, OptionalDecline.WithdrawalKeep })
                    Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-" + id), id);
                Assert.AreEqual(KeepTheHit, client.Board.Console.Actions.Pass.tooltip);
                var pip = client.Board.Tray.Q<Button>("battle-hit-cancel-1");
                Assert.IsNotNull(pip, "The −1 stands on the Shadow's hits.");
                Assert.IsTrue(pip.ClassListContains(ChoiceGlow.ClassName));
                StringAssert.StartsWith("Cancel hit · withdraw", pip.tooltip);
                // UI-419 (owner): the −1 pulses, and Skip effect in the battle window names what it skips.
                await room.Until(() => pip.parent.ClassListContains(ChoiceGlow.PulseClass));
                var skip = client.Board.Tray.Q<Button>("battle-skip-effect");
                Assert.IsNotNull(skip, "Skip effect stands in the battle window.");
                Assert.AreEqual("Skip effect", skip.text);
                Assert.AreEqual("Skip Orderly Withdrawal · Keep the hit and fight on", skip.tooltip);
                var revision = client.Board.View.Revision;
                if (cancel)
                {
                    await Layout(room, client, "orderly-withdrawal", "ui347");
                    CardNetworkTests.Click(client.Board.Tray.Q<Button>("battle-hit-cancel-1"));
                }
                else
                    CardNetworkTests.Click(skip);
                await room.At(revision + 1);
                Assert.AreNotEqual("CombatEffect", client.Board.View.Decision?.Presentation ?? "");
                watch.NeverOpened();
            }
        }

        private static async Task EvilKnowledge()
        {
            using (var room = await Connect("ui347-evil-knowledge", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                Assert.AreEqual(DecisionKind.SiegeContinuation, client.Board.View.Decision.Kind);
                // Without an Elite to downgrade Extend is disabled: Evil Knowledge is the only way on, on the portrait.
                Assert.IsFalse(client.Board.View.Options.Single(o => o.Kind == CommandKind.ExtendSiege).Enabled);
                Assert.IsNull(client.Board.Tray.Q<Button>("action-" + CommandKind.UseEvilKnowledge), "No button beside Extend / Stop.");
                var mouth = client.Board.View.Battle.Attacker.Characters.Single(c => c.DefinitionId == Mouth);
                var portrait = client.Board.Tray.Q<VisualElement>("portrait-" + mouth.Id);
                Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), "The Mouth's portrait glows.");
                StringAssert.StartsWith(EvilKnowledgeChoice.Offer, portrait.tooltip);
                await Layout(room, client, "evil-knowledge", "ui347");
                var round = client.Board.View.Battle.Round;
                var revision = client.Board.View.Revision;
                PointerGestureEvents.ClickAt(client.Board.Tray.Q<VisualElement>("portrait-" + mouth.Id));
                await room.At(revision + 1);
                Assert.AreEqual(round + 1, room.Fp.Board.View.Battle.Round, "The siege goes on without downgrading an Elite.");
                watch.NeverOpened();
            }
        }
    }
}
