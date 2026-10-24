using System;
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
    // UI-313: the layout matrix — 1366 × 768 and 1920 × 1080 at 100 % and 125 % — for the scenes with the most to place:
    // a long march with its camera travel, an event card's reveal, a battle's reveal and a Hunt with its tracks and roster.
    // In each the only new control is Skip, at most one caption shows at a time, and every presented window stays on
    // screen clear of the console. A frame of each scene at each layout goes to .artifacts/screenshots (ui313-*).
    public sealed class OpponentPresentationMatrixTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public IEnumerator ALongMarchStaysWithinTheBudgetAtEveryLayout()
        {
            foreach (var (width, scale) in Layouts)
            {
                using var stage = new PresentedBoardStage(width, scale, false);
                var army = stage.Put("sauron", UnitKind.Regular, "mount-gundabad", 4);
                yield return stage.Ready();
                var latest = stage.Latest();
                PresentedBoardStage.Place(latest, army.Take(2), "angmar");
                var march = stage.Paid(stage.Step(1), DieFace.Army);
                march.Movements.Add(new ActionMovement
                {
                    Kind = ActionMovementKind.March, From = "mount-gundabad", To = "angmar", Participants = army.Take(2).Select(stage.Unit).ToList()
                });
                var controls = Controls(stage);
                stage.Play(latest, "pelargir", march);
                yield return Watch(stage, controls, "ui313-march-" + width + "-" + scale + ".png", () => stage.Flying("angmar"));
                stage.AssertFinal(latest);
            }
        }

        [UnityTest]
        public IEnumerator TheEventCardStaysReadableAndClearAtEveryLayout()
        {
            foreach (var (width, scale) in Layouts)
            {
                using var stage = new PresentedBoardStage(width, scale, false);
                yield return stage.Ready();
                var controls = Controls(stage);
                OpponentCardPresentationTests.Play(stage, "grey-havens");
                var cards = stage.Board.Cards;
                yield return Watch(stage, controls, "ui313-card-" + width + "-" + scale + ".png",
                    () => cards.Root.style.display.value == DisplayStyle.Flex, () => Clear(stage, cards.Root));
            }
        }

        [UnityTest]
        public IEnumerator TheBattleWindowStaysOnScreenAtEveryLayout()
        {
            foreach (var (width, scale) in Layouts)
            {
                using var stage = OpponentBattlePresentationTests.Attack(out var fight, width, scale);
                yield return OpponentBattlePresentationTests.Open(stage, fight);
                var round = fight.Next();
                round.Reveal(Side.Shadow, OpponentBattlePresentationTests.ShadowCard);
                round.Reveal(Side.FreePeoples, OpponentBattlePresentationTests.FreeCard);
                round.Record(battle =>
                {
                    OpponentBattlePresentationTests.Faces(battle);
                    OpponentBattlePresentationTests.Roll(battle.Attacker, "a", 6, 5, 2, 1);
                });
                var controls = Controls(stage);
                stage.Play(round.Latest(), null, round.Steps.ToArray());
                yield return Watch(stage, controls, "ui313-battle-" + width + "-" + scale + ".png",
                    () => stage.Board.Battles.Presented.Q(className: "battle-card-revealed") != null, () => OnScreen(stage, stage.Board.Battles.Root));
            }
        }

        [UnityTest]
        public IEnumerator TheHuntAndTheFellowshipPanelStayOnScreenAtEveryLayout()
        {
            foreach (var (width, scale) in Layouts)
            {
                using var stage = OpponentHuntPresentationTests.Watching(width: width, scale: scale);
                yield return stage.Ready();
                var steps = OpponentHuntPresentationTests.Hit(stage, out var latest, casualty: true);
                var controls = Controls(stage);
                stage.Play(latest, null, steps);
                yield return Watch(stage, controls, "ui313-hunt-" + width + "-" + scale + ".png",
                    () => stage.Board.Fellowship.IsOpen && stage.Board.Hunt.IsOpen, () =>
                    {
                        OnScreen(stage, stage.Board.Hunt.Root);
                        Assert.IsFalse(stage.Board.Hunt.Root.worldBound.Overlaps(stage.Board.Fellowship.Root.worldBound),
                            "The Hunt window stands clear of the Fellowship panel.");
                    });
            }
        }

        // Every frame until the release: one new control (Skip) at most, one caption at most, Skip clear of the console.
        // When the scene reaches its key moment, the frame is saved and the scene's own placement is checked.
        private static IEnumerator Watch(PresentedBoardStage stage, int controls, string frame, Func<bool> key, Action check = null)
        {
            var saved = false;
            var deadline = Time.realtimeSinceStartup + 30;
            while (stage.Playback.Holding && Time.realtimeSinceStartup < deadline)
            {
                Assert.LessOrEqual(Controls(stage), controls + 1, "Only the Skip target is added.");
                Assert.LessOrEqual(stage.Board.Root.Query(PoliticsPresenter.CaptionName).ToList().Count, 1, "At most one caption.");
                var skip = stage.Presenters.Skip.Button;
                if (skip.parent != null && skip.worldBound.width > 0)
                    Assert.IsFalse(skip.worldBound.Overlaps(stage.Board.Console.Root.worldBound), "Skip stands clear of the console.");
                if (!saved && key())
                {
                    yield return new WaitForSecondsRealtime(.35f);
                    LayoutTests.Save(stage.Texture, frame);
                    check?.Invoke();
                    saved = true;
                }
                yield return null;
            }
            Assert.IsTrue(saved, "The scene reached its key moment: " + frame);
            Assert.IsFalse(stage.Playback.Holding, "The scene ends: " + frame);
        }

        // Controls outside the existing hosts a scene opens (the battle and Hunt windows, the card inspector, the panels, the
        // console trail where a played card docks as its source chip, §6.2) and the map's markers.
        private static int Controls(PresentedBoardStage stage)
        {
            var hosts = new VisualElement[]
            {
                stage.Board.Hunt.Root, stage.Board.Battles.Root, stage.Board.Cards.Root, stage.Board.Fellowship.Root, stage.Board.Map,
                stage.Board.OpponentDice, stage.Board.Console.Status.Trail
            };
            return stage.Board.Root.Query<Button>().ToList().Count(button => !hosts.Any(host => host.Contains(button)));
        }

        private static void OnScreen(PresentedBoardStage stage, VisualElement window)
        {
            var screen = stage.Board.Root.worldBound;
            Assert.IsTrue(screen.Contains(window.worldBound.min) && screen.Contains(window.worldBound.max - Vector2.one), window.name + " stays on screen.");
        }

        private static void Clear(PresentedBoardStage stage, VisualElement window)
        {
            OnScreen(stage, window);
            Assert.LessOrEqual(window.worldBound.yMax, stage.Board.Console.Root.parent.worldBound.yMin + 1, window.name + " stands clear of the console.");
        }
    }
}
