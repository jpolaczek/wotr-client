using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class ConfigurationScreenTests
    {
        private MountedMatchClient client;

        [SetUp]
        public void Mount() => client = new MountedMatchClient("http://localhost", "configuration-input");

        [TearDown]
        public void Unmount() => client.Dispose();

        private RoomScreen CreateScreen()
        {
            var screen = new RoomScreen();
            var root = client.Board.Root.parent;
            client.Board.Root.RemoveFromHierarchy();
            root.Add(screen.Root);
            Assert.IsNotNull(screen.Root.panel);
            return screen;
        }
        [Test]
        public void SwitchingRoomsWithTheSameVersionRefreshesTheirConfiguration()
        {
            var screen = CreateScreen();
            var first = new RoomView { ExpansionsAvailable = true, Id = "first", Version = 2, Configuration = new MatchConfiguration { Lords = true } };
            screen.Update(first);
            var retained = screen.Root.Q<Toggle>("config-lords");
            screen.Update(first);
            Assert.AreSame(retained, screen.Root.Q<Toggle>("config-lords"));
            screen.Update(new RoomView { ExpansionsAvailable = true, Id = "first", Version = 1 });
            Assert.AreSame(first, screen.Room, "Late polling must not restore an older configuration.");
            Assert.IsTrue(screen.Root.Q<Toggle>("config-lords").value);
            screen.Update(new RoomView { ExpansionsAvailable = true, Id = "second", Version = 2, Configuration = new MatchConfiguration { Fate = true } });
            Assert.IsFalse(screen.Root.Q<Toggle>("config-lords").value);
            Assert.IsTrue(screen.Root.Q<Toggle>("config-fate").value);
            Assert.IsNull(screen.Root.Q<Toggle>("config-council"));
            Assert.IsNotNull(screen.Root.Q<Toggle>("config-new-cities"));
        }

        [Test]
        public void DefaultDismayAndItsDependenciesChangeOnlyAfterServerAcknowledgement()
        {
            var screen = CreateScreen();
            var room = new RoomView { ExpansionsAvailable = true, Id = "table", Version = 1, Configuration = new MatchConfiguration
                { Lords = true, Kings = true, RemovingRulerTokens = true } };
            screen.Update(room);
            MatchConfiguration sent = null;
            screen.Configure += value => sent = value;
            var dismay = screen.Root.Q<Toggle>("config-dismay");
            Assert.IsTrue(dismay.value);
            Change(dismay, false);
            Assert.IsTrue(dismay.value, "A local toggle does not mutate authoritative configuration.");
            Assert.IsNull(room.Configuration.Dismay);
            Assert.IsFalse(sent.Dismay.Value);
            Assert.IsFalse(sent.RemovingRulerTokens);
            room.Configuration = sent;
            room.Version++;
            screen.Update(room);
            Assert.IsFalse(screen.Root.Q<Toggle>("config-dismay").value);
            Assert.IsNull(screen.Root.Q<Toggle>("config-removing-ruler-tokens"));
        }

        [TestCase("lords")]
        [TestCase("kings")]
        [TestCase("fate")]
        public void DisablingAnExpansionClearsItsDependentVariants(string module)
        {
            var screen = CreateScreen();
            screen.Update(new RoomView { ExpansionsAvailable = true, Configuration = new MatchConfiguration { Lords = true, Kings = true, Fate = true,
                Council = true, Dismay = true, RemovingRulerTokens = true, SequentialCorruption = true, VisibleCorruption = true, NewCities = true } });
            MatchConfiguration sent = null;
            screen.Configure += value => sent = value;
            Change(screen.Root.Q<Toggle>("config-" + module), false);
            if (module == "lords")
                Assert.IsFalse(sent.Council);
            if (module == "kings")
            {
                Assert.IsFalse(sent.SequentialCorruption);
                Assert.IsFalse(sent.VisibleCorruption);
            }
            if (module == "fate")
                Assert.IsFalse(sent.NewCities);
            else
            {
                Assert.IsFalse(sent.Dismay.Value);
                Assert.IsFalse(sent.RemovingRulerTokens);
            }
        }

        [Test]
        public void StartedRoomDisablesConfigurationAndOffersReturnToTheGame()
        {
            var screen = CreateScreen();
            screen.Update(new RoomView { ExpansionsAvailable = true, MatchId = "match", FreePeoplesAccepted = true, ShadowAccepted = true });
            Assert.IsFalse(screen.Root.Q<Toggle>("config-lords").enabledInHierarchy);
            Assert.IsFalse(screen.Root.Q<Button>("confirm-configuration").enabledSelf);
            Assert.IsTrue(screen.Root.Q<Button>("start-game").enabledSelf);
            Assert.AreEqual("Open game", screen.Root.Q<Button>("start-game").text);
        }

        [Test]
        public void BaseOnlyServerShowsNoExpansionChoicesAndReturnsAnOlderTableToTheBaseGame()
        {
            var screen = CreateScreen();
            screen.Update(new RoomView { Id = "base", Version = 1 });
            Assert.IsNull(screen.Root.Q<Toggle>("config-lords"), "UI-96: no expansion choice on a base-only server.");
            Assert.IsNull(screen.Root.Q<Toggle>("config-council"));
            Assert.AreEqual("Base game", screen.Root.Q<Label>("config-base-only").text);
            Assert.IsNull(screen.Root.Q<Button>("config-use-base"));
            screen.Update(new RoomView { Id = "older", Version = 2, FreePeoplesAccepted = true, ShadowAccepted = true,
                StartUnavailableReason = "ExpansionsUnavailable", Configuration = new MatchConfiguration { Lords = true } });
            Assert.IsFalse(screen.Root.Q<Button>("start-game").enabledSelf);
            Assert.AreEqual("This server offers only the base game.", screen.Root.Q<Button>("start-game").tooltip);
            MatchConfiguration sent = null;
            screen.Configure += value => sent = value;
            var useBase = screen.Root.Q<Button>("config-use-base");
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = useBase;
                useBase.SendEvent(submit);
            }
            Assert.IsNotNull(sent, "The table returns to the base game through the server.");
            Assert.IsFalse(sent.Lords || sent.Kings || sent.Fate);
        }

        private static void Change(Toggle toggle, bool value) => toggle.value = value;
    }
}
