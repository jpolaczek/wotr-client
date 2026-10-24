using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-95: the Fellowship moves smoothly on the map and along its track for both players.
    public sealed class FellowshipMotionNetworkTests
    {
        [UnityTest]
        public IEnumerator DeclaredFellowshipFliesToItsRegionAndItsTrackMarkerGlides() => MatchTestRoom.Wait(Declare());

        private static async Task Declare()
        {
            using var room = await UI10TestRoom.Connect("ui34-guide-keep");
            var before = room.Fp.Board.View;
            Assert.AreEqual("rivendell", before.Fellowship.RegionId);
            Assert.AreEqual(5, before.Fellowship.Progress);
            await room.Settle();
            var track = room.Fp.Board.Fellowship.Root.Q("journey-track");
            Assert.IsNotNull(track, "The Fellowship panel shows the journey track.");
            var marker = track.Q<Image>(className: "fellowship-track-marker");
            Assert.IsNotNull(marker, "The journey track has its marker.");
            Assert.IsNotNull(before.Decision, "The scenario opens on a decision.");
            var origin = marker.worldBound.center.x;
            // UI-310: the Free Peoples' own declaration flies at their view; the Shadow's presentation flies it before its
            // release, watched meanwhile.
            var shadowFlew = room.Until(() => room.Sh.Board.Map.Figures.Root.Q("moving-fellowship") != null);
            var receipt = await room.Fp.Api.Post<CommandReceipt>($"api/matches/{before.MatchId}/commands", new GameCommand
            {
                MatchId = before.MatchId, DecisionId = before.Decision.Id, ExpectedRevision = before.Revision,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.DeclareFellowship,
                Payload = new CommandPayload { Route = { "rivendell", "trollshaws" } }
            }, room.Token);
            Assert.IsTrue(receipt.Accepted, receipt.Error);
            await room.Until(() => room.Fp.Board.View.Revision > before.Revision);
            Assert.AreEqual("trollshaws", room.Fp.Board.View.Fellowship.RegionId);
            Assert.IsNotNull(room.Fp.Board.Map.Figures.Root.Q("moving-fellowship"), "The declared Fellowship flies from its last known position · FreePeoples");
            await Task.Delay(120, room.Token);
            var middle = marker.worldBound.center.x;
            var start = track.Q<Label>("journey-step-0").worldBound.center.x;
            Assert.Less(middle, origin - 2, "The progress marker leaves its old cell.");
            Assert.Greater(middle, start + 2, "The progress marker is still on its way.");
            LayoutTests.Save(room.Fp.Viewport, "ui95-fellowship-motion.png");
            await Task.Delay(500, room.Token);
            Assert.That(marker.worldBound.center.x, Is.EqualTo(start).Within(1.5f), "The marker settles on step 0.");
            Assert.AreEqual(0, room.Fp.Board.Map.Figures.ActiveCount);
            await shadowFlew;
            await room.Until(() => room.Sh.Board.View.Revision > before.Revision);
            Assert.AreEqual("trollshaws", room.Sh.Board.View.Fellowship.RegionId, "The Shadow's presentation releases the declared region.");
        }
    }
}
