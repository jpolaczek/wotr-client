using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-544 (owner, 2026-11-17: "when I use Wisdom of Elrond the nation activates and advances with a jump; the tokens must
    // always animate, never jump; the same for the Ring on the Corruption and Fellowship tracks, whatever effect causes
    // it"): the player's own change, applied straight to the board as any view is, travels; so does a change made while
    // the panel was closed, when it opens; a quiet view places at once. Frames: ui544-*.
    public sealed class TrackGlideTests
    {
        [UnityTest]
        public IEnumerator AnOwnPoliticalStepTurnsTheTokenOverThenTravels()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var nation = stage.Before.Nations.First(n => n.StepsToWar >= 2);
            nation.Active = false;
            yield return stage.Ready();
            stage.Board.Nations.Open();
            yield return Frames(10);
            var token = Token(stage, nation.Id);
            var start = token.worldBound.center.x;
            var latest = Step(stage, nation.Id);
            stage.Board.Apply(latest);
            var samples = new List<float>();
            var saved = false;
            for (var t = 0f; t < 1.4f; t += Time.unscaledDeltaTime)
            {
                samples.Add(token.worldBound.center.x);
                if (!saved && samples.Count > 1 && Between(samples.Last(), start, Target(stage, token, start)))
                {
                    LayoutTests.Save(stage.Texture, "ui544-politics-own-step.png");
                    saved = true;
                }
                yield return null;
            }
            var end = token.worldBound.center.x;
            Assert.Greater(Mathf.Abs(end - start), 10, "The token stands one step on.");
            Assert.IsTrue(samples.Any(x => Between(x, start, end)), "It travelled through the places between: " + string.Join(", ", samples.Select(x => x.ToString("0"))));
            Assert.IsTrue(token.ClassListContains("active-nation"), "It turned over to its active side.");
            Assert.AreEqual(0f, token.resolvedStyle.translate.x, .01f, "It settles on its place.");
        }

        [UnityTest]
        public IEnumerator AStepWhileThePanelWasClosedTravelsWhenItOpens()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var nation = stage.Before.Nations.First(n => n.StepsToWar >= 2);
            stage.Board.Nations.Open();
            yield return Frames(10);
            var token = Token(stage, nation.Id);
            var start = token.worldBound.center.x;
            stage.Board.Nations.Close();
            yield return Frames(30);
            stage.Board.Apply(Step(stage, nation.Id));
            yield return Frames(5);
            stage.Board.Nations.Open();
            var samples = new List<float>();
            for (var t = 0f; t < 1.4f; t += Time.unscaledDeltaTime)
            {
                if (stage.Board.Nations.IsOpen && token.worldBound.width > 0)
                    samples.Add(token.worldBound.center.x);
                yield return null;
            }
            var end = token.worldBound.center.x;
            Assert.Greater(Mathf.Abs(end - start), 10);
            Assert.IsTrue(samples.Any(x => Between(x, start, end)), "The step made while closed travels on opening.");
        }

        [UnityTest]
        public IEnumerator ThreeCorruptionMoveTheRingAlongTheTrack()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            stage.Board.Fellowship.Open();
            yield return Frames(10);
            var marker = stage.Board.Root.Q("corruption-track-marker");
            var start = marker.worldBound.center.x;
            var latest = stage.Latest();
            latest.Fellowship.Corruption += 3;
            stage.Board.Apply(latest);
            var samples = new List<float>();
            var saved = false;
            for (var t = 0f; t < 1.4f; t += Time.unscaledDeltaTime)
            {
                samples.Add(marker.worldBound.center.x);
                if (!saved && samples.Count == 8)
                {
                    LayoutTests.Save(stage.Texture, "ui544-corruption-three.png");
                    saved = true;
                }
                yield return null;
            }
            var end = marker.worldBound.center.x;
            Assert.Greater(end - start, 20, "Three cells on.");
            Assert.GreaterOrEqual(samples.Count(x => Between(x, start, end)), 3, "The Ring is seen on its way, not at once.");
            Assert.AreEqual(0f, marker.resolvedStyle.translate.x, .01f);
        }

        [UnityTest]
        public IEnumerator AQuietViewPlacesTheTokenAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var nation = stage.Before.Nations.First(n => n.StepsToWar >= 2);
            stage.Board.Nations.Open();
            yield return Frames(10);
            var token = Token(stage, nation.Id);
            stage.Board.ApplyQuietly(Step(stage, nation.Id));
            yield return Frames(2);
            Assert.AreEqual(0f, token.resolvedStyle.translate.x, .01f, "A catch-up places the token without travel.");
        }

        private static PlayerView Step(PresentedBoardStage stage, string id)
        {
            var latest = stage.Latest();
            var changed = latest.Nations.First(n => n.Id == id);
            changed.StepsToWar -= 1;
            changed.Active = true;
            return latest;
        }

        private static Button Token(PresentedBoardStage stage, string id) => stage.Board.Root.Q<Button>("nation-status-" + id);

        // Where the token is going: its place less what it still has to travel.
        private static float Target(PresentedBoardStage stage, VisualElement token, float start) =>
            token.worldBound.center.x - token.resolvedStyle.translate.x;

        private static bool Between(float x, float a, float b) => x > Mathf.Min(a, b) + 2 && x < Mathf.Max(a, b) - 2;

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }
    }
}
