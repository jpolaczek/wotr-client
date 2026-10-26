using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // AUDIO-05 (AU09, AU17): one resolution is a new projection with the accepted answer to an own command; the plain
    // acceptance sounds only when nothing more particular happened, whichever came first; a rejection fails once; the
    // first view, a catch-up after a reconnect and a recovered receipt are silent.
    public sealed class GameSoundsTests
    {
        private sealed class Commands : ICommandEvents
        {
            public event Action CommandSending;
            public event Action<GameCommand, CommandReceipt, bool> CommandAnswered;
            public bool GestureSuppressed => false;

            public void Answer(string id, bool accepted, long revision, bool recovered = false)
            {
                CommandSending?.Invoke();
                CommandAnswered?.Invoke(new GameCommand { CommandId = id },
                    new CommandReceipt { CommandId = id, Accepted = accepted, Revision = revision }, recovered);
            }
        }

        private SoundModule module;
        private Commands commands;
        private GameSounds game;
        private List<SoundCue> played;

        [SetUp]
        public void Create()
        {
            module = new SoundModule();
            commands = new Commands();
            game = new GameSounds(module, () => "", commands);
            played = new List<SoundCue>();
            module.Policy.Played += played.Add;
        }

        [TearDown]
        public void Release()
        {
            game.Dispose();
            module.Dispose();
        }

        private static PlayerView View(long revision, params string[] hand)
        {
            var view = new PlayerView { MatchId = "m", Revision = revision, Turn = 1, You = Side.FreePeoples };
            view.Hand.AddRange(hand);
            return view;
        }

        // Each case starts with nothing sounding, so the one-clip rule never hides the next one.
        private void Quiet() => module.Policy.Silence();

        [Test]
        public void TheFirstViewIsSilentAndAnAcceptedCommandSoundsOnceWhicheverArrivesFirst()
        {
            game.Update(View(1, "a"));
            CollectionAssert.IsEmpty(played, "The first view only sets the point of reference.");
            commands.Answer("c1", true, 2);
            game.Update(View(2, "a"));
            CollectionAssert.AreEqual(new[] { SoundCue.ActionAccepted }, played, "Answer first, then the view.");
            Quiet();
            game.Update(View(3, "a"));
            commands.Answer("c2", true, 3);
            CollectionAssert.AreEqual(new[] { SoundCue.ActionAccepted, SoundCue.ActionAccepted }, played, "The view first, then the answer.");
            Quiet();
            commands.Answer("c3", true, 4);
            game.Update(View(4));
            Assert.AreEqual(SoundCue.CardMove, played[played.Count - 1], "A played card is more particular than the acceptance.");
            Assert.AreEqual(3, played.Count);
            Quiet();
            game.Update(View(5, "b"));
            commands.Answer("c4", true, 5);
            Assert.AreEqual(4, played.Count, "The view already sounded its card; the late answer adds nothing.");
        }

        [Test]
        public void ARejectionFailsOnceAndARecoveredReceiptIsSilent()
        {
            game.Update(View(1));
            commands.Answer("c1", false, 1);
            commands.Answer("c1", false, 1);
            CollectionAssert.AreEqual(new[] { SoundCue.ActionFailed }, played);
            Quiet();
            commands.Answer("c2", true, 2, recovered: true);
            game.Update(View(2));
            Assert.AreEqual(1, played.Count, "Recovering a lost answer does not replay its success.");
        }

        // UI-310: a presentation's beat sounded the march when it showed it; the released view does not sound it again,
        // and a later march sounds as before.
        [Test]
        public void ATrackTheShownPresentationSoundedIsNotRepeatedByTheReleasedView()
        {
            game.Update(View(1));
            game.Play(new SoundRequest(SoundCue.FellowshipAdvanced, "presented/march"));
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced }, played, "The beat sounds its fact at once.");
            Quiet();
            var released = View(2);
            released.Fellowship.Progress = 1;
            game.Update(released);
            Assert.AreEqual(1, played.Count, "The release does not sound the march again.");
            Quiet();
            var later = View(3);
            later.Fellowship.Progress = 2;
            game.Update(later);
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced, SoundCue.FellowshipAdvanced }, played);
        }

        [Test]
        public void ACatchUpAfterAReconnectIsSilent()
        {
            game.Update(View(1));
            game.SetConnection("Reconnecting");
            game.Update(View(9, "x", "y"));
            game.SetConnection("Connected");
            CollectionAssert.IsEmpty(played, "The jump to the current state plays nothing.");
            game.Update(View(10, "x"));
            CollectionAssert.AreEqual(new[] { SoundCue.CardMove }, played, "Live again: the next change sounds.");
        }
    }
}
