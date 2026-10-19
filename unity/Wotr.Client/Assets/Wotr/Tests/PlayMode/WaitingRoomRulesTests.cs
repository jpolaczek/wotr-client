using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-263: the rules strip in a mounted waiting room: a host's click asks the server for the next configuration and
    // the strip changes only with the room the server returns; a guest, an unreleased module and a started game keep
    // their controls in place, disabled.
    public class WaitingRoomRulesTests
    {
        private MountedMatchClient client;

        [SetUp]
        public void Mount() => client = new MountedMatchClient("http://localhost", "waiting-room-rules");

        [TearDown]
        public void Unmount() => client.Dispose();

        private WaitingRoomScreen CreateScreen()
        {
            var screen = new WaitingRoomScreen();
            var root = client.Board.Root.parent;
            client.Board.Root.RemoveFromHierarchy();
            root.Add(screen.Root);
            Assert.IsNotNull(screen.Root.panel);
            return screen;
        }

        [UnityTest]
        public IEnumerator TheHostsClickAsksForTheNextRulesAndTheStripWaitsForTheServer()
        {
            var screen = CreateScreen();
            var room = Host(1, new MatchConfiguration { Lords = true, Kings = true, RemovingRulerTokens = true });
            screen.Update(room);
            yield return Layout();
            MatchConfiguration sent = null;
            screen.Configure += value => sent = value;
            var dismay = screen.Rules.VariantChip("dismay");
            Assert.IsTrue(dismay.ClassListContains("on"));
            CardNetworkTests.Click(dismay);
            Assert.IsFalse(sent.Dismay.Value);
            Assert.IsFalse(sent.RemovingRulerTokens);
            Assert.IsTrue(dismay.ClassListContains("on"), "A click does not change the rules before the server answers.");
            screen.Update(Host(2, sent));
            Assert.IsFalse(screen.Rules.VariantChip("dismay").ClassListContains("on"));
            Assert.IsFalse(screen.Rules.VariantChip("removing-ruler-tokens").enabledSelf);
            screen.Update(Host(1, room.Configuration));
            Assert.IsFalse(screen.Rules.VariantChip("dismay").ClassListContains("on"), "A late refresh never restores older rules.");
        }

        [UnityTest]
        public IEnumerator TheMedallionRemovesItsExpansionWithItsVariants([Values("lords", "kings", "fate")] string module)
        {
            var screen = CreateScreen();
            screen.Update(Host(1, new MatchConfiguration { Lords = true, Kings = true, Fate = true, Council = true, Dismay = true,
                RemovingRulerTokens = true, SequentialCorruption = true, VisibleCorruption = true, NewCities = true }));
            yield return Layout();
            MatchConfiguration sent = null;
            screen.Configure += value => sent = value;
            var medallion = screen.Rules.Medallion(module);
            Assert.IsTrue(medallion.ClassListContains("on"));
            StringAssert.StartsWith("Click to remove ", medallion.tooltip);
            CardNetworkTests.Click(medallion);
            Assert.IsFalse(RulesChoices.IsOn(sent, module));
            if (module == "lords")
                Assert.IsFalse(sent.Council);
            if (module == "kings")
                Assert.IsFalse(sent.VisibleCorruption || sent.SequentialCorruption);
            if (module == "fate")
                Assert.IsFalse(sent.NewCities);
            else
                Assert.IsFalse(sent.Dismay.Value || sent.RemovingRulerTokens);
        }

        [UnityTest]
        public IEnumerator TheGuestSeesTheRulesInPlaceButCannotChangeThem()
        {
            var screen = CreateScreen();
            var room = Host(1, new MatchConfiguration { Fate = true });
            room.IsHost = false;
            screen.Update(room);
            yield return Layout();
            var sent = 0;
            screen.Configure += _ => sent++;
            foreach (var button in Controls(screen))
            {
                Assert.IsFalse(button.enabledSelf, button.name);
                Assert.AreNotEqual(DisplayStyle.None, button.resolvedStyle.display, button.name);
            }
            StringAssert.Contains("Only the host chooses the rules.", screen.Rules.Medallion("fate").tooltip);
            CardNetworkTests.Click(screen.Rules.Medallion("lords"));
            Assert.AreEqual(0, sent);
            Assert.AreEqual("Accept these rules and get ready to play.", screen.Root.Q<Button>("room-ready").tooltip);
        }

        [UnityTest]
        public IEnumerator AnUnreleasedModuleAndAStartedGameKeepTheirControlsDisabled()
        {
            var screen = CreateScreen();
            var room = Host(1, new MatchConfiguration());
            room.OfferedModules = new List<string> { "fate" };
            screen.Update(room);
            yield return Layout();
            Assert.IsTrue(screen.Rules.Medallion("fate").enabledSelf);
            foreach (var module in new[] { "lords", "kings" })
            {
                Assert.IsFalse(screen.Rules.Medallion(module).enabledSelf);
                StringAssert.EndsWith("Not yet available on this server.", screen.Rules.Medallion(module).tooltip);
            }
            var started = Host(2, new MatchConfiguration { Fate = true });
            started.Status = RoomStatus.Started;
            started.MatchId = "match";
            screen.Update(started);
            Assert.IsTrue(Controls(screen).All(button => !button.enabledSelf));
            Assert.IsTrue(screen.Rules.Medallion("fate").ClassListContains("on"));
            StringAssert.Contains("Fate of Erebor", screen.Root.Q<Label>("waiting-information").text);
        }

        // Clicks land on laid-out controls.
        private static IEnumerator Layout()
        {
            for (var frame = 0; frame < 3; frame++)
                yield return null;
        }

        private static IEnumerable<Button> Controls(WaitingRoomScreen screen) =>
            screen.Rules.Root.Query<Button>().ToList();

        private static RoomView Host(int version, MatchConfiguration configuration) => new RoomView
        {
            Id = "table", Name = "Table", Version = version, IsHost = true, Role = RoomRole.Player, You = Side.FreePeoples,
            FreePeoples = "Host", Shadow = "Guest", Host = "Host", ExpansionsAvailable = true, Configuration = configuration,
            OfferedModules = new List<string> { "fate", "lords", "kings" }
        };
    }
}
