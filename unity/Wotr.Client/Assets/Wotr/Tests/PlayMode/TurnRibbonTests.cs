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
    // UI-167 (SIGNALS_UX SG05–SG07) on a fresh base game with the real server: the first view lights the console
    // only; the Hunt allocation and then the first action pass to a player with their ribbon, never to the opponent;
    // the ribbon takes no pointer and leaves in about 1.5 s; with reduced motion the console flashes instead, and
    // its glow ends at the first step of the draft.
    public sealed class TurnRibbonTests
    {
        [UnityTest]
        public IEnumerator TheActionPassingToAPlayerShowsTheirRibbonOnlyThere() => Wait(Passing());

        [UnityTest]
        public IEnumerator ReducedMotionFlashesTheConsoleAndTheGlowEndsAtTheFirstStep() => Wait(Reduced());

        private static async Task Passing()
        {
            using var room = await CreateNormalGame();
            var fp = room.Fp.Board;
            var sh = room.Sh.Board;
            await room.Until(() => fp.View?.Decision?.Kind == DecisionKind.FellowshipPhase && fp.View.DecisionOwner == Side.FreePeoples);
            await room.Settle();
            Assert.AreEqual(0, fp.Ribbon.Shown, "The first view lights the console, not the ribbon.");
            Assert.IsTrue(fp.Console.Status.YourTurn, "An untouched starting decision glows.");
            Assert.IsFalse(sh.Console.Status.YourTurn);

            await Send(room, room.Fp, CommandKind.ContinueFellowship, new CommandPayload());
            await room.Until(() => sh.Ribbon.Shown == 1);
            Assert.AreEqual("ALLOCATE HUNT DICE", sh.Ribbon.Text.text);
            Assert.IsTrue(sh.Ribbon.Root.Q("turn-ribbon-band").ClassListContains("side-shadow"));
            Assert.AreEqual(0, fp.Ribbon.Shown, "The opponent gets no ribbon.");
            await room.Settle();
            AssertIgnoresPointer(sh);
            await Task.Delay(TimeSpan.FromMilliseconds(TurnRibbon.InMilliseconds + TurnRibbon.HoldMilliseconds + TurnRibbon.OutMilliseconds + 400),
                room.Token);
            Assert.IsFalse(sh.Ribbon.IsShowing, "The ribbon leaves by itself.");

            var allocation = sh.View.Options.First(o => o.Kind == CommandKind.AllocateHunt);
            var dice = allocation.EligibleIds.Concat(allocation.EligibleDice).Distinct().Take(allocation.Minimum).ToList();
            await Send(room, room.Sh, CommandKind.AllocateHunt, new CommandPayload { Ids = dice });
            await room.Until(() => fp.Ribbon.Shown == 1);
            StringAssert.StartsWith("YOUR ACTION · Turn ", fp.Ribbon.Text.text);
            Assert.IsTrue(fp.Ribbon.Root.Q("turn-ribbon-band").ClassListContains("side-free"));
            Assert.AreEqual(DecisionKind.Action, fp.View.Decision.Kind);
            Assert.AreEqual(1, sh.Ribbon.Shown, "The Shadow sees no ribbon for the Free Peoples' action.");
        }

        private static async Task Reduced()
        {
            using var room = await CreateNormalGame();
            room.Fp.Interface.Preferences.SetReducedMotion(true);
            var fp = room.Fp.Board;
            var sh = room.Sh.Board;
            await room.Until(() => fp.View?.Decision?.Kind == DecisionKind.FellowshipPhase);
            await Send(room, room.Fp, CommandKind.ContinueFellowship, new CommandPayload());
            await room.Until(() => sh.View?.Decision?.Kind == DecisionKind.AllocateHunt);
            var allocation = sh.View.Options.First(o => o.Kind == CommandKind.AllocateHunt);
            var dice = allocation.EligibleIds.Concat(allocation.EligibleDice).Distinct().Take(allocation.Minimum).ToList();
            await Send(room, room.Sh, CommandKind.AllocateHunt, new CommandPayload { Ids = dice });
            await room.Until(() => fp.Console.Status.Flashes == 1);
            Assert.AreEqual(0, fp.Ribbon.Shown, "Reduced motion: no ribbon, one flash of the console.");
            await room.Until(() => fp.View?.Decision?.Kind == DecisionKind.Action && fp.View.DecisionOwner == Side.FreePeoples);
            await room.Settle();
            // UI-292: the action's plaque opens nothing, so it neither glows nor presses; the headline and the flash
            // announce the turn.
            Assert.IsFalse(fp.Console.Status.YourTurn);
            Assert.IsFalse(fp.Console.Status.MustAct);
            Assert.IsTrue(ReadOnlyControl.Is(fp.Console.Status.Plaque));
            StringAssert.StartsWith("YOUR ACTION", fp.Console.Status.Headline.text);
            var die = fp.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face.HasValue).Id;
            ArmyGestures.Die(room.Fp, die);
            await room.Settle();
            Assert.IsFalse(fp.Console.Status.YourTurn, "The glow ends at the first step of the draft.");
        }

        // The ribbon draws over the map but a click where it shows reaches what lies beneath.
        private static void AssertIgnoresPointer(BoardScreen board)
        {
            var band = board.Ribbon.Root.Q("turn-ribbon-band");
            Assert.Greater(band.worldBound.width, 0);
            var picked = board.Root.panel.Pick(band.worldBound.center);
            for (var element = picked; element != null; element = element.parent)
                Assert.AreNotSame(board.Ribbon.Root, element, "The ribbon must not take the pointer.");
        }

        private static async Task Send(MatchTestRoom room, MountedMatchClient client, CommandKind kind, CommandPayload payload)
        {
            var view = client.Board.View;
            var receipt = await client.Api.Post<CommandReceipt>($"api/matches/{view.MatchId}/commands", new GameCommand
            {
                MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload
            }, room.Token);
            Assert.IsTrue(receipt.Accepted, kind + ": " + receipt.Error);
        }
    }
}
