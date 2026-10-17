using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // AUDIO-06 (AU13, AU14): one resolution that moves the Fellowship and changes its Corruption shows the two tracks
    // one after the other with both sounds — the journey Fellowship first, Mordor Corruption first — and a newer state
    // during the first stage shows itself at once and drops the second sound. With Reduced motion the moved marker is
    // marked instead of gliding.
    public sealed class TrackPairTests
    {
        private static PlayerView Next(PlayerView view, int progress, int corruption, int? mordor = null)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            next.Fellowship.Progress = progress;
            next.Fellowship.Corruption = corruption;
            next.Fellowship.MordorStep = mordor;
            return next;
        }

        private static string Value(MountedMatchClient client, string name) => client.Board.Root.Q<Label>(name).text;

        private static IEnumerator Pump(MountedMatchClient client, float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                client.Interface.Sounds.Update();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheJourneyShowsTheFellowshipThenTheRingWithBothSounds()
        {
            using var client = new MountedMatchClient("http://localhost", "track-pair");
            var view = new BoardFixture().View;
            view.Fellowship.Progress = 1;
            view.Fellowship.Corruption = 3;
            client.Apply(view);
            yield return null;
            var played = new List<SoundCue>();
            client.Interface.Sounds.Policy.Played += played.Add;
            var both = Next(view, 2, 5);
            client.Refresh(both);
            yield return null;
            Assert.IsTrue(client.Board.TracksStaged);
            Assert.AreEqual("2", Value(client, "journey-label"), "The Fellowship moves first.");
            Assert.AreEqual("3", Value(client, "corruption-label"), "The Ring waits for the second stage.");
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced }, played);
            yield return Pump(client, 0.6f);
            Assert.IsFalse(client.Board.TracksStaged);
            Assert.AreEqual("5", Value(client, "corruption-label"));
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced, SoundCue.CorruptionChanged }, played,
                "Both sounds once, in order, nothing else for the same resolution.");

            played.Clear();
            var again = Next(both, 3, 6);
            client.Refresh(again);
            yield return null;
            var newer = Next(again, 3, 6);
            client.Refresh(newer);
            yield return Pump(client, 0.6f);
            Assert.IsFalse(client.Board.TracksStaged, "A newer state ends the first stage at once.");
            Assert.AreEqual("6", Value(client, "corruption-label"));
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced }, played, "The newer state drops the second sound.");
        }

        // AUDIO-07: when the same resolution also passes a decision to the player, its tone follows the pair, once.
        [UnityTest]
        public IEnumerator ADecisionOfTheSameResolutionSoundsAfterThePair()
        {
            using var client = new MountedMatchClient("http://localhost", "track-pair-decision");
            var view = new BoardFixture().View;
            view.Fellowship.Progress = 1;
            view.Fellowship.Corruption = 3;
            view.DecisionOwner = view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            view.Decision = new DecisionView { Id = "opponent", Kind = DecisionKind.Action };
            client.Apply(view);
            yield return null;
            var played = new List<SoundCue>();
            client.Interface.Sounds.Policy.Played += played.Add;
            var both = Next(view, 2, 5);
            both.DecisionOwner = view.You;
            both.Decision = new DecisionView { Id = "mine", Kind = DecisionKind.HuntDamage };
            client.Refresh(both);
            yield return Pump(client, 1f);
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced, SoundCue.CorruptionChanged, SoundCue.DecisionRequired }, played);
        }

        [UnityTest]
        public IEnumerator MordorShowsTheCorruptionFirstAndReducedMotionMarksTheMarker()
        {
            using var client = new MountedMatchClient("http://localhost", "track-pair-mordor");
            client.Interface.Preferences.SetReducedMotion(true);
            var view = new BoardFixture().View;
            view.Fellowship.MordorStep = 1;
            view.Fellowship.Corruption = 6;
            client.Apply(view);
            yield return null;
            var played = new List<SoundCue>();
            client.Interface.Sounds.Policy.Played += played.Add;
            client.Refresh(Next(view, 0, 8, 2));
            yield return null;
            Assert.AreEqual("8", Value(client, "corruption-label"), "In Mordor the Hunt's Corruption comes first.");
            Assert.AreEqual("1", Value(client, "mordor-label"), "The step waits for the second stage.");
            Assert.IsTrue(client.Board.Root.Q("corruption-track").ClassListContains(FellowshipTrackRow.ChangedClass),
                "With Reduced motion the moved marker is marked.");
            yield return Pump(client, 0.6f);
            Assert.AreEqual("2", Value(client, "mordor-label"));
            CollectionAssert.AreEqual(new[] { SoundCue.CorruptionChanged, SoundCue.FellowshipAdvanced }, played);
        }
    }
}
