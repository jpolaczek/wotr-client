using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-303: the presentation cues each client of a two-player match released, so a scenario can compare what both
    // players are shown with each other and with their final boards.
    internal sealed class OpponentCueLog : IDisposable
    {
        private readonly MatchTestRoom room;
        private readonly Action<IReadOnlyList<PresentationCue>> fp, sh;
        public List<PresentationCue> Fp { get; } = new List<PresentationCue>();
        public List<PresentationCue> Sh { get; } = new List<PresentationCue>();

        public OpponentCueLog(MatchTestRoom room)
        {
            this.room = room;
            fp = Fp.AddRange;
            sh = Sh.AddRange;
            room.Fp.Activity.CuesReady += fp;
            room.Sh.Activity.CuesReady += sh;
        }

        public List<PresentationCue> Of(MountedMatchClient client) => client == room.Fp ? Fp : Sh;

        // Neither player is shown their own decisions, and a step both players are shown makes the same cues for both.
        public void AssertConsistent()
        {
            Assert.IsFalse(Fp.Any(cue => cue.Actor == Side.FreePeoples), "The Free Peoples are not shown their own decisions.");
            Assert.IsFalse(Sh.Any(cue => cue.Actor == Side.Shadow), "The Shadow is not shown its own decisions.");
            foreach (var log in new[] { Fp, Sh })
                Assert.AreEqual(log.Count, log.Select(cue => cue.Id).Distinct().Count(), "A cue is released once.");
            var free = BySource(Fp);
            var shadow = BySource(Sh);
            var shared = free.Keys.Intersect(shadow.Keys).ToList();
            Assert.IsTrue(shared.Any(source => source.EndsWith("/" + PresentationCueKind.Battle)), "Both players see the shared rolls.");
            foreach (var source in shared)
                CollectionAssert.AreEqual(free[source], shadow[source], source);
        }

        public static IEnumerable<string> Reveals(IEnumerable<PresentationCue> cues) => cues
            .Where(cue => cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.CombatReveal)
            .Select(cue => cue.Card.Id);

        private static Dictionary<string, List<string>> BySource(IEnumerable<PresentationCue> cues) => cues
            .GroupBy(cue => cue.StepId + "/" + cue.Kind)
            .ToDictionary(group => group.Key, group => group.Select(cue => cue.Id).ToList());

        public void Dispose()
        {
            room.Fp.Activity.CuesReady -= fp;
            room.Sh.Activity.CuesReady -= sh;
        }
    }
}
