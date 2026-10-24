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
    // UI-308 with two real clients on fresh test scenarios: the Shadow plays a public Event and the Free Peoples see the
    // actual card read-only before its effects; a card kept on the table settles at the Effects tab.
    public sealed class OpponentCardPresentationNetworkTests
    {
        private const string Orcs = "base.sh.strategy.20";
        private const string Crebain = "base.sh.character.16";

        [UnityTest]
        public IEnumerator OrcsMultiplyingAgainIsReadThenDocksAndBothRegionsFollow() => Wait(Recruiting());

        [UnityTest]
        public IEnumerator FlocksOfCrebainIsReadThenSettlesAtTheEffectsTab() => Wait(Table());

        private static async Task Recruiting()
        {
            var session = await TestScenarioRoom.Start("sh-muster-event");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await session.Send(Side.Shadow, CommandKind.PlayEvent, Payload(sh.Board.View, Orcs));
                await session.Until(() => fp.Playback.Holding && fp.Board.Cards.CardId == Orcs);
                Assert.IsFalse(fp.Board.Cards.Pinned);
                Assert.AreEqual(CardRevealPresenter.PlayedNote, fp.Board.Cards.Root.Q<Label>("card-window").text);
                await session.Until(() => fp.Board.Console.Status.Trail.Q("console-presented-source") != null);
                await session.Until(() => fp.Board.Map.Q("observed-delta") != null);
                Assert.IsNotNull(fp.Board.Console.Status.Trail.Q("console-presented-source"), "The source stays while its effects play.");
                await session.Until(() => !fp.Playback.Holding);
                foreach (var region in new[] { "dol-guldur", "mount-gundabad" })
                    Assert.AreEqual(fp.Board.View.Units.Count(u => u.Location == region && u.Side == Side.Shadow),
                        fp.Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == Side.Shadow).Sum(a => a.Total));
                Assert.IsNull(fp.Board.Console.Status.Trail.Q("console-presented-source"));
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Table()
        {
            var session = await TestScenarioRoom.Start("sh-character-event");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await session.Send(Side.Shadow, CommandKind.PlayEvent, Payload(sh.Board.View, Crebain));
                await session.Until(() => fp.Playback.Holding && fp.Board.Cards.CardId == Crebain);
                await session.Until(() => PresentationGlow.IsLit(fp.Board.TableEffects.Tab) || !fp.Playback.Holding);
                await session.Until(() => !fp.Playback.Holding);
                Assert.IsTrue(fp.Board.View.Players.Any(p => p.TableCards.Contains(Crebain)), "The card is on the table.");
                Assert.AreEqual(DisplayStyle.None, fp.Board.Cards.Root.style.display.value, "The reveal does not linger.");
            }
            finally
            {
                await session.End();
            }
        }

        private static CommandPayload Payload(PlayerView view, string card)
        {
            var cost = view.Options.First(o => o.Kind == CommandKind.PlayEvent).Events.First(e => e.CardId == card).Costs.First();
            return new CommandPayload { CardId = card, DieId = cost.DieId, AbilityId = cost.AbilityId, CardCost = cost.Face };
        }
    }
}
