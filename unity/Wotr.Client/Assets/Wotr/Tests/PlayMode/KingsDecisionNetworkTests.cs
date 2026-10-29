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
    // UI-348 (EXP-15 D7, D17): Brand's Combat card replacement is a gilt chip beside the own card after the reveal, whose
    // click cancels the opponent's card; Keep card effects and the Palantír of Anárion's Skip extra card are the console's
    // Pass. No text button and no generic window remain.
    public class KingsDecisionNetworkTests
    {
        private const string KeepTheCard = "Pass · Keep your card's effects";

        [UnityTest] public IEnumerator HeirOfBardCancelsTheShadowCardFromItsChip() => Wait(Replacement("ui348-brand-heir", Side.FreePeoples));
        [UnityTest] public IEnumerator ShadowsInTheNorthCancelsTheFreePeoplesCardFromItsChip() => Wait(Replacement("ui348-brand-shadows", Side.Shadow));
        [UnityTest] public IEnumerator KeepingTheCardsEffectsIsThePass() => Wait(Keep());
        [UnityTest] public IEnumerator AChieftainSubstitutesFromHisChipBeforeTheReveal() => Wait(Chieftain());
        [UnityTest] public IEnumerator KeepingTheShadowCardsEffectsIsThePass() => Wait(ChieftainKeep());
        [UnityTest] public IEnumerator AChieftainEntersFromHisRowWithoutACouncilToken() => Wait(ChieftainEntry());
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
                await Applied(room, own);
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
                // UI-396 (owner): the battle window skips the replacement too, from its footer.
                StringAssert.Contains("or Keep card effects to fight on as usual", client.Board.Console.Status.Hint.text);
                var skip = client.Board.Tray.Q<Button>("battle-keep-card-effects");
                Assert.IsNotNull(skip, "Keep card effects stands in the battle window's footer.");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(skip);
                await room.At(revision + 1);
                Assert.AreNotEqual(CombatReplacementChoice.Presentation, client.Board.View.Decision?.Presentation ?? "");
                Assert.IsFalse(room.Fp.Board.View.Battle.Defender.CardCancelled);
                watch.NeverOpened();
            }
        }

        // UI-277 (D17): the Black Serpent's substitution is the Shadow's chip beside its own card before the reveal; the
        // Free Peoples see the applied marker without the Shadow card's name.
        private static async Task Chieftain()
        {
            using (var room = await Connect("ui277-chieftain", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                Assert.IsFalse(client.Board.View.Battle.CardsRevealed);
                Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-kings.character.black-serpent"));
                Assert.AreEqual(KeepTheCard, client.Board.Console.Actions.Pass.tooltip);
                var chip = client.Board.Tray.Q("battle-defender").Q<Button>("battle-replacement-kings.character.black-serpent");
                Assert.IsNotNull(chip, "The chip stands beside the Shadow's own card.");
                Assert.IsTrue(chip.Q("battle-replacement-arms").ClassListContains("nation-emblem-southrons"));
                StringAssert.StartsWith("Click to use The Black Serpent instead of ", chip.tooltip);
                await Layout(room, client, "ui277-chieftain", "ui277");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(client.Board.Tray.Q("battle-defender").Q<Button>("battle-replacement-kings.character.black-serpent"));
                await room.At(revision + 1);
                await Applied(room, "battle-defender");
                StringAssert.Contains("replaces the Combat card's effects", room.Fp.Board.Tray.Q("battle-defender").Q("battle-replacement-applied").tooltip,
                    "The Free Peoples do not learn the Shadow card before the reveal.");
                watch.NeverOpened();
            }
        }

        private static async Task ChieftainKeep()
        {
            using (var room = await Connect("ui277-chieftain-keep", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                await room.Settle();
                await room.Pass(room.Sh, KeepTheCard);
                Assert.IsEmpty(room.Sh.Board.View.CombatReplacements);
                watch.NeverOpened();
            }
        }

        // UI-277: without the Council a Chieftain enters from his Characters row with a die alone; no token is asked for.
        private static async Task ChieftainEntry()
        {
            using (var room = await Connect("ui277-chieftain-entry", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                    .Single(c => c.CharacterId == "kings.character.black-serpent");
                Assert.IsEmpty(choice.CouncilTokens);
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                await ComponentGestures.Enter(room, client, choice);
                Assert.IsFalse(client.Board.Console.Status.Hint.text.Contains("Council token"));
                var revision = client.Board.View.Revision;
                client.Board.Map.Select(choice.Targets[0]);
                await room.At(revision + 1);
                Assert.AreEqual(choice.Targets[0], room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Location);
                watch.NeverOpened();
            }
        }

        // Both players see the applied replacement beside the replaced card.
        private static async Task Applied(MatchTestRoom room, string column)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
                await room.Until(() => client.Board.Tray.Q(column)?.Q("battle-replacement-applied") != null);
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
