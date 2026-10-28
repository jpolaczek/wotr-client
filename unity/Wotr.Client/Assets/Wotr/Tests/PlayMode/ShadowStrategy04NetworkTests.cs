using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class ShadowStrategy04NetworkTests
    {
        [UnityTest] public IEnumerator MistyMixKeepsControlsAndDraftThroughUndoAndReconnect() => Wait(Misty());
        [UnityTest] public IEnumerator BesiegedMistyRecruitsThenReturnsChosenOldUnits() => Wait(MistySiege());
        [UnityTest] public IEnumerator OrcsDistributesTwoScarceFiguresAcrossBothRegions() => Wait(Orcs());
        [UnityTest] public IEnumerator HordePlacesFiveFiguresInOneEasternRegion() => Wait(Horde());
        [UnityTest] public IEnumerator MonstersPlacesIndependentEliteAndScarceRegular() => Wait(Monsters());
        [UnityTest] public IEnumerator MusteringsResolvesTwoExcessWindowsAfterTenRecruits() => Wait(Musterings());
        [UnityTest] public IEnumerator PitsPlacesSixInThreeStrongholds() => Wait(Pits(false));
        [UnityTest] public IEnumerator PitsShortageUsesFiveWithoutEmptyChoices() => Wait(Pits(true));
        [UnityTest] public IEnumerator DesperateBattleShowsBothBonusesAndAllowsRerollChoices() => Wait(Desperate());
        [UnityTest] public IEnumerator OnslaughtCanKeepAllFigures() => Wait(Onslaught());

        private static async Task Misty()
        {
            using (var room = await Connect("sh-str04-misty", ScenarioKind.CardPack))
            {
                var button = room.Sh.Board.Reinforcements.Root.Q<Button>("event-reserve-sauron-Regular");
                await Recruit(room, "sauron", "moria", UnitKind.Regular, 1);
                Assert.AreSame(button, room.Sh.Board.Reinforcements.Root.Q<Button>(button.name), "Editing a placement must refresh existing controls.");
                Click(room.Sh, "event-placement-0");
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                await Recruit(room, "sauron", "moria", UnitKind.Regular, 1);
                await Recruit(room, "sauron", "moria", UnitKind.Elite, 1);
                await Recruit(room, "sauron", "moria", UnitKind.Nazgul, 1);
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Nation == "sauron" && u.Location == "moria"));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                // UI-131/UI-381: the console's ✓ commits the whole plan once the server's preview finds it legal.
                Assert.IsNull(room.Sh.Board.Reinforcements.Root.Q<Button>("reinforcements-confirm"), "The panel has no ✓ of its own.");
                var finish = room.Sh.Board.Console.Actions.Confirm;
                await room.Until(() => finish.enabledSelf);
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(finish);
                await room.At(revision + 1);
                Assert.AreEqual(3, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "moria"));
                Assert.AreEqual(3, room.Fp.Board.View.History.Count(h => h.Kind == "EventRecruited"));
                Assert.AreEqual(DisplayStyle.None, room.Sh.Board.Reinforcements.Root.resolvedStyle.display, "The panel closes with the plan.");
            }
        }

        private static async Task MistySiege()
        {
            using (var room = await Connect("sh-str04-misty-siege", ScenarioKind.CardPack))
            {
                await Recruit(room, "sauron", "moria", UnitKind.Elite, 2);
                await Recruit(room, "sauron", "moria", UnitKind.Nazgul, 1);
                await room.Confirm(room.Sh);
                Assert.AreEqual(DecisionKind.RemoveExcess, room.Sh.Board.View.Decision.Kind);
                var old = room.Sh.Board.View.Units.Where(u => u.Nation == "southrons" && u.Location == "moria").Take(2).Select(u => u.Id).ToList();
                foreach (var id in old)
                    await room.ChooseFigure(room.Sh, id);
                await room.Confirm(room.Sh);
                Assert.AreEqual(3, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "moria" && u.Garrison));
                Assert.IsTrue(old.All(id => room.Fp.Board.View.Units.Single(u => u.Id == id).Location == "reserve"));
            }
        }

        private static async Task Orcs()
        {
            using (var room = await Connect("sh-str04-orcs-shortage", ScenarioKind.CardPack))
            {
                await Recruit(room, "sauron", "dol-guldur", UnitKind.Regular, 1);
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                await Recruit(room, "sauron", "mount-gundabad", UnitKind.Regular, 1);
                await room.Confirm(room.Sh);
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "dol-guldur"));
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "mount-gundabad"));
            }
        }

        private static async Task Horde()
        {
            using (var room = await Connect("sh-str04-horde", ScenarioKind.CardPack))
            {
                await Recruit(room, "southrons", "khand", UnitKind.Regular, 5);
                await room.Confirm(room.Sh);
                Assert.AreEqual(5, room.Fp.Board.View.Units.Count(u => u.Nation == "southrons" && u.Location == "khand"));
            }
        }

        private static async Task Monsters()
        {
            using (var room = await Connect("sh-str04-monsters", ScenarioKind.CardPack))
            {
                await Recruit(room, "sauron", "ettenmoors", UnitKind.Regular, 1);
                await Recruit(room, "sauron", "trollshaws", UnitKind.Elite, 1);
                await room.Confirm(room.Sh);
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "ettenmoors" && u.Kind == UnitKind.Regular));
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "trollshaws" && u.Kind == UnitKind.Elite));
            }
        }

        private static async Task Musterings()
        {
            using (var room = await Connect("sh-str04-musterings-excess", ScenarioKind.CardPack))
            {
                Assert.AreEqual(10, room.Fp.Board.View.History.Count(h => h.Kind == "EventRecruited"));
                for (var window = 0; window < 2; window++)
                {
                    var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.RemoveExcess);
                    foreach (var id in option.EligibleIds.Take(option.Minimum))
                        await room.ChooseFigure(room.Sh, id);
                    await room.Confirm(room.Sh);
                }
                Assert.AreEqual(10, room.Fp.Board.View.Units.Count(u => u.Location == "nurn"));
                Assert.AreEqual(10, room.Fp.Board.View.Units.Count(u => u.Location == "gorgoroth"));
            }
        }

        private static async Task Pits(bool shortage)
        {
            using (var room = await Connect(shortage ? "sh-str04-pits-shortage" : "sh-str04-pits", ScenarioKind.CardPack))
            {
                if (shortage)
                {
                    await Recruit(room, "sauron", "barad-dur", UnitKind.Regular, 2);
                    await Recruit(room, "sauron", "mount-gundabad", UnitKind.Regular, 2);
                    await Recruit(room, "sauron", "moria", UnitKind.Regular, 1);
                    await room.Confirm(room.Sh);
                }
                else
                {
                    await PlaceWithOneToken(room, "sauron", UnitKind.Regular, "barad-dur", "barad-dur", "mount-gundabad", "mount-gundabad", "moria", "moria");
                    await ConfirmInConsole(room);
                }
                Assert.AreEqual(shortage ? 5 : 6, room.Fp.Board.View.History.Count(h => h.Kind == "EventRecruited"));
                Assert.AreEqual(3, room.Fp.Board.View.Units.Where(u => u.Nation == "sauron" && u.Location != "reserve" && u.Location != "casualties").Select(u => u.Location).Distinct().Count());
            }
        }

        private static async Task Desperate()
        {
            using (var room = await Connect("sh-str04-desperate", ScenarioKind.CardPack))
            {
                Assert.AreEqual(1, room.Sh.Board.View.Battle.Attacker.CombatModifier);
                Assert.AreEqual(1, room.Sh.Board.View.Battle.Defender.RerollModifier);
                await room.Confirm(room.Sh.Board.View.Decision != null ? room.Sh : room.Fp);
                await room.Confirm(room.Sh.Board.View.Decision != null ? room.Sh : room.Fp);
                Assert.AreEqual(DecisionKind.BattleContinue, room.Sh.Board.View.Decision.Kind);
            }
        }

        private static async Task Onslaught()
        {
            using (var room = await Connect("sh-str04-onslaught", ScenarioKind.CardPack))
            {
                Click(room.Sh, "battle-keep-all");
                await room.Confirm(room.Sh);
                Assert.AreEqual(DecisionKind.BattleContinue, room.Sh.Board.View.Decision.Kind);
                Assert.AreEqual(3, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "osgiliath"));
            }
        }

        // UI-213 CC04: the lit token of the lit Reserve tab is armed once and every region click places one of its
        // figures; the console counts them and no decision window opens.
        private static async Task PlaceWithOneToken(MatchTestRoom room, string nation, UnitKind kind, params string[] regions)
        {
            var reserve = room.Sh.Board.Reinforcements;
            Assert.IsTrue(reserve.Tab.ClassListContains("legal-target"), "The Reserve tab is lit.");
            var token = reserve.Root.Q<Button>("event-reserve-" + nation + "-" + kind);
            Assert.IsTrue(token.ClassListContains("eligible-token"), "The token the card may place is lit.");
            // UI-301: with the shared ChoiceGlow rim and plate.
            Assert.AreEqual(DisplayStyle.Flex, token.Q("reserve-choice-glow").resolvedStyle.display);
            CardNetworkTests.Click(token);
            await room.Settle();
            Assert.IsTrue(token.ClassListContains("selected"));
            foreach (var region in regions)
            {
                MapContourProbe.ClickRegion(room.Sh.Board.Map, region);
                await room.Settle();
            }
            Assert.That(room.Sh.Board.Console.Status.Hint.text, Does.StartWith("Recruited " + regions.Length + " of " + regions.Length));
            Assert.IsFalse(token.ClassListContains("selected"), "Every figure of the card is planned: the token is released.");
            Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
        }

        private static async Task ConfirmInConsole(MatchTestRoom room)
        {
            var confirm = room.Sh.Board.Console.Actions.Confirm;
            await room.Until(() => confirm.enabledSelf);
            await room.Settle();
            var revision = room.Sh.Board.View.Revision;
            CardNetworkTests.Click(confirm);
            await room.At(revision + 1);
        }

        private static async Task Recruit(MatchTestRoom room, string nation, string region, UnitKind kind, int count)
        {
            room.Sh.Board.Map.Select(region);
            await room.Settle();
            for (var i = 0; i < count; i++)
                Click(room.Sh, "event-reserve-" + nation + "-" + kind);
        }
    }
}
