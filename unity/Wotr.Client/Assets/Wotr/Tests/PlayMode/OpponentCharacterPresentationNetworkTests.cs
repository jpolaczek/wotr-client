using System;
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
    // UI-309 with two real clients on the character scenarios: the watching board names the opponent's entering or
    // transforming character with its portrait moment before the latest view (one figure, at its place), and a public
    // reaction marks its source's portrait in the battle window.
    public sealed class OpponentCharacterPresentationNetworkTests
    {
        [UnityTest]
        public IEnumerator EntriesAndTransformationsNameTheirCharacterOnTheWatchingBoard() => Wait(Entries());

        [UnityTest]
        public IEnumerator TheWhiteRiderMarksGandalfsBattlePortraitOnTheShadowsBoard() => Wait(WhiteRider());

        private static async Task Entries()
        {
            using (var room = await Connect("entry", ScenarioKind.Character))
            {
                await Enter(room, room.Fp, "gandalf-white", "fangorn", room.Sh, "Gandalf the White");
                Assert.AreEqual(1, Characters(room.Sh, "fangorn", Side.FreePeoples), "Grey becomes White once.");
                await Enter(room, room.Sh, "witch-king", "minas-morgul", room.Fp, "The Witch-king");
                Assert.AreEqual(1, Characters(room.Fp, "minas-morgul", Side.Shadow));
                await Enter(room, room.Fp, "aragorn", "pelargir", room.Sh, "Aragorn");
                Assert.AreEqual(1, Characters(room.Sh, "pelargir", Side.FreePeoples), "Strider becomes Aragorn in place.");
            }
        }

        private static async Task WhiteRider()
        {
            using (var room = await Connect("white-rider", ScenarioKind.Character))
            {
                var revision = room.Fp.Board.View.Revision;
                if (room.Fp.Board.Tray.parent == room.Fp.Board.Fellowship.Root)
                    room.Fp.Board.Fellowship.Open();
                await room.Settle();
                await ReactionGestures.Use(room, room.Fp, "character.white-rider");
                var gandalf = room.Sh.Board.View.Characters.Single(c => c.DefinitionId == "base.character.gandalf-white");
                await Seen(room, () => Ring(room.Sh) is VisualElement ring &&
                    ring.worldBound.Overlaps(room.Sh.Board.Root.Q("portrait-" + gandalf.Id).worldBound), "Gandalf's battle portrait is the source.");
                Assert.AreEqual(revision, room.Sh.Board.View.Revision, "The Shadow still presents the earlier view.");
                await room.At(revision + 1);
                Assert.IsTrue(room.Sh.Board.View.Battle.WhiteRiderActive);
                Assert.IsNull(Ring(room.Sh), "The mark leaves with the presentation.");
            }
        }

        // UI-66/UI-247: the entering row with its die (CharacterNetworkTests); the watcher shows the moment first.
        private static async Task Enter(MatchTestRoom room, MountedMatchClient client, string version, string region,
            MountedMatchClient watcher, string name)
        {
            var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                .Single(c => c.CharacterId.EndsWith("." + version));
            await ComponentGestures.Cost(room, client, choice.Dice[0]);
            var revision = client.Board.View.Revision;
            await ComponentGestures.Enter(room, client, choice);
            if (choice.Targets.Count > 1)
            {
                await room.Settle();
                client.Board.Map.Select(region);
            }
            await Seen(room, () => Moment(watcher) == name, name + " is named on the watching board.");
            Assert.AreEqual(revision, watcher.Board.View.Revision, "The moment plays before the latest view.");
            await room.At(revision + 1);
            Assert.AreEqual(region, watcher.Board.View.Characters.Single(c => c.DefinitionId.EndsWith("." + version)).Location);
        }

        private static string Moment(MountedMatchClient client) =>
            string.Join(",", client.Board.Map.Query<Label>("observed-portrait-name").ToList().Select(label => label.text));

        private static VisualElement Ring(MountedMatchClient client) => client.Board.Root.Q(CharacterPresenter.SourceRingName);

        private static int Characters(MountedMatchClient client, string region, Side side) =>
            client.Board.Presentation.Armies.Where(a => a.RegionId == region && a.Side == side).Sum(a => a.Characters);

        private static async Task Seen(MatchTestRoom room, Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(30, room.Token);
            Assert.IsTrue(condition(), what);
        }
    }
}
