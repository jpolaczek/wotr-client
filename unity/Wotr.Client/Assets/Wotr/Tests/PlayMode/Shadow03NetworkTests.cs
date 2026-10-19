using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class Shadow03NetworkTests
    {
        [UnityTest] public IEnumerator LureChoosesCorruptionForTheServerSelectedCompanion() => Wait(Lure());
        [UnityTest] public IEnumerator BreakingSelectsPortraitsAndPreviewsTheirSharedRegion() => Wait(Breaking());
        [UnityTest] public IEnumerator LidlessSelectsThreeDiceAndRestoresDraftOnReconnect() => Wait(Lidless());
        [UnityTest] public IEnumerator WornDiscardsAVisibleTableCard() => Wait(Worn());
        [UnityTest] public IEnumerator CrebainIsChosenBeforeAnyRoll() => Wait(Crebain());
        [UnityTest] public IEnumerator BalrogReturnsToFellowshipPhaseAfterProtectedDeclaration() => Wait(Balrog());
        [UnityTest] public IEnumerator WordsShowsTheSuppressedCompanionToBothPlayers() => Wait(Words());
        [UnityTest] public IEnumerator TableBalrogIsAvailableInHandButCommitRemainsPrivate() => Wait(TableBalrog());

        private static async Task Lure()
        {
            using (var room = await Connect("shadow-lure", ScenarioKind.CardPack))
            {
                // UI-214: Lure of the Ring's two options are console buttons naming the Companion.
                AssertConsoleOptions(room.Fp, "effect-choice-lure.corruption", "effect-choice-lure.eliminate");
                Assert.That(TrayOrConsole(room.Fp, "effect-choice-lure.corruption").text, Does.StartWith("Gimli"));
                Click(room.Fp, "effect-choice-lure.corruption");
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(7, room.Sh.Board.View.Fellowship.CompanionCount);
            }
        }

        private static async Task Breaking()
        {
            using (var room = await Connect("shadow-breaking", ScenarioKind.CardPack))
            {
                // UI-249: the Companions are chosen on the Hunt panel's roster portraits.
                await room.Settle();
                foreach (var id in new[] { "merry", "pippin", "boromir" })
                {
                    var portrait = room.Fp.Board.Hunt.Guides.Root.Q<Button>("fellowship-portrait-base.character." + id);
                    Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), id + " glows on the roster.");
                    PointerGestureEvents.ClickAt(portrait);
                    await room.Settle();
                }
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("effect-choice-base.character.merry"), "No second list of names.");
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("hollin"));
                Assert.AreEqual(7, room.Fp.Board.View.Fellowship.CompanionCount);
                await room.Confirm(room.Fp);
                Assert.AreEqual(4, room.Sh.Board.View.Fellowship.CompanionCount);
                Assert.AreEqual(0, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual("hollin", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.merry").Location);
                Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Lidless()
        {
            using (var room = await Connect("shadow-lidless", ScenarioKind.CardPack))
            {
                var ids = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds.Take(3).ToArray();
                foreach (var id in ids)
                    CardNetworkTests.Click(room.Sh.Board.Root.Q<Button>(id));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                Assert.That(room.Sh.Board.Tray.Q<Label>("effect-board-choice").text, Does.Contain("3 → Hunt"));
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Dice.Where(d => ids.Contains(d.Id)).All(d => d.Face == DieFace.Eye && d.Location == DieLocation.Hunt));
            }
        }

        private static async Task Worn()
        {
            using (var room = await Connect("shadow-worn", ScenarioKind.CardPack))
            {
                Click(room.Sh, "effect-choice-base.fp.character.05");
                await room.Confirm(room.Sh);
                Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == "base.fp.character.05"));
                Assert.IsTrue(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.15"));
                Assert.AreEqual(1, room.Sh.Board.View.Fellowship.Corruption);
            }
        }

        private static async Task Crebain()
        {
            using (var room = await Connect("shadow-crebain", ScenarioKind.CardPack))
            {
                Assert.IsEmpty(room.Sh.Board.View.Hunt.Dice);
                await room.Settle();
                Click(room.Sh, "effect-choice-card.flocks-of-crebain");
                await room.Confirm(room.Sh);
                Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.16"));
                var roll = room.Sh.Board.View.History.Single(e => e.Kind == "HuntRolled");
                Assert.That(roll.Text, Does.Contain("bonus 1"));
            }
        }

        private static async Task Balrog()
        {
            using (var room = await Connect("shadow-balrog", ScenarioKind.CardPack))
            {
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                await room.Settle();
                // UI-247: the Balrog's reaction is its table card's × in the Effects panel; the click sends it.
                var reacted = room.Sh.Board.View.Revision;
                await ReactionGestures.TableCard(room, room.Sh, "base.sh.character.17");
                await room.At(reacted + 1);
                var damageRevision = room.Fp.Board.View.Revision;
                Click(room.Fp, "hunt-response-Ring");
                await room.At(damageRevision + 1);
                Assert.IsTrue(room.Sh.Board.View.Fellowship.Hidden);
                Assert.AreEqual(3, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(DecisionKind.FellowshipPhase, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Words()
        {
            using (var room = await Connect("shadow-words", ScenarioKind.CardPack))
            {
                // UI-245: Words of Power takes the Companion on its portrait in the Battle window.
                await room.Settle();
                PointerGestureEvents.ClickAt(room.Sh.Board.Tray.Q("portrait-base.character.gimli"));
                await room.Confirm(room.Sh);
                Assert.Contains("base.character.gimli", room.Fp.Board.View.Battle.Defender.CancelledAbilities);
                Assert.IsTrue(room.Fp.Board.Tray.Query<Label>("battle-characters").ToList().Any(label => label.text.Contains("⦸ Gimli")));
                Assert.AreEqual(3, room.Sh.Board.View.Battle.Defender.Leadership);
            }
        }

        private static async Task TableBalrog()
        {
            using (var room = await Connect("shadow-table-balrog", ScenarioKind.CardPack))
            {
                Assert.IsFalse(room.Sh.Board.View.Hand.Contains("base.sh.character.17"));
                var button = room.Sh.Board.Root.Q<Button>("base.sh.character.17");
                Assert.That(button.Q<Label>("hand-card-kind").text, Does.Contain("FROM THE TABLE"));
                await CardGestures.Open(room.Sh);
                var revision = room.Sh.Board.View.Revision;
                var choice = CardGestures.Source(room.Sh, "base.sh.character.17");
                LayoutTests.Save(room.Sh.Viewport, "ui47-balrog-combat.png");
                // UI-147: one click on the table card in the battle window plays it, without a card window. The Free
                // Peoples see the private commitment in the first view after it.
                PlayerView hidden = null;
                void Seen(PlayerView view)
                {
                    if (hidden == null && view.Battle?.Attacker.CardCommitted == true && !view.Battle.CardsRevealed)
                        hidden = view;
                }
                room.Fp.Connection.Updated += Seen;
                PointerGestureEvents.ClickAt(choice);
                await room.Until(() => room.Sh.Board.View.Revision > revision);
                Assert.IsEmpty(room.Sh.Board.Cards.CardId);
                await room.NoCard(room.Fp);
                room.Fp.Connection.Updated -= Seen;
                Assert.IsNotNull(hidden);
                Assert.IsTrue(hidden.Effects.Any(e => e.SourceId == "base.sh.character.17"));
                Assert.IsNull(hidden.Battle.Attacker.CardId);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.17"));
                Assert.AreEqual(3, room.Fp.Board.View.Battle.SpecialAttacks.Single().Dice.Count);
            }
        }
    }
}
