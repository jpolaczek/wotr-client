using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-311 on a mounted Free Peoples board: the Shadow attacks Osgiliath from Minas Morgul and the battle's recorded
    // rounds play in the existing battle window, from public snapshots — a committed card stays a back, both cards turn
    // face up together, a roll rings the dice it changed, committed casualties are marked before they leave (a downgrade
    // as its replacement), a character lost with its army is ringed on its portrait, later rounds stay in the same
    // window, and a retreat or an advance returns to the map. Frames go to .artifacts/screenshots (ui311-*).
    public sealed class OpponentBattlePresentationTests
    {
        private const string BattleId = "presented-battle";
        private const string WitchKing = "base.character.witch-king";
        private const string ShadowCard = "base.sh.strategy.13";
        private const string FreeCard = "base.fp.strategy.14";

        [UnityTest]
        public IEnumerator TheAttackOpensTheBattleWindowAfterItsTraceAndTheReleaseGivesBackTheControls()
        {
            using var stage = Attack(out var fight);
            yield return stage.Ready();
            stage.Play(fight.Latest(), "grey-havens", fight.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Battles.IsOpen && stage.Board.Battles.Presenting);
            Assert.IsTrue(stage.Playback.Holding, "The window opens inside the presentation, after the attack's trace.");
            Assert.AreEqual(DisplayStyle.None, stage.Board.Tray.style.display.value, "The recorded state stands in for the controls.");
            yield return stage.Released();
            Assert.IsTrue(stage.Board.Battles.IsOpen);
            Assert.IsFalse(stage.Board.Battles.Presenting, "The release gives the window back to the decision controls.");
            Assert.AreNotEqual(DisplayStyle.None, stage.Board.Tray.style.display.value);
        }

        [UnityTest]
        public IEnumerator TheCommittedCardStaysABackUntilBothCardsTurnFaceUpTogether()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Decide(Side.Shadow);
            round.Record(battle =>
            {
                battle.Stage = "DefenderCard";
                Commit(battle.Attacker);
            }).Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
            round.Record(battle => Commit(battle.Defender));
            round.Reveal(Side.Shadow, ShadowCard);
            round.Reveal(Side.FreePeoples, FreeCard);
            round.Record(Faces);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-back") == true);
            Assert.IsFalse(Card(stage, "attacker").text.Contains("Initiative"), "A back has no identity.");
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-revealed") == true);
            var started = Time.realtimeSinceStartup;
            Assert.IsTrue(Card(stage, "defender").ClassListContains("battle-card-revealed"), "Both cards turn face up together.");
            StringAssert.Contains("Initiative", Card(stage, "attacker").text, "The initiative reads on the card.");
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains(BattlePresenter.CardMarkClass) == true);
            LayoutTests.Save(stage.Texture, "ui311-reveal.png");
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains(BattlePresenter.CardMarkClass) != true);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 2.8f, "The reveal holds about 3 s.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator AClickOnARevealedCardOpensItReadOnlyAndHoldsThePresentation()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Reveal(Side.Shadow, ShadowCard);
            round.Reveal(Side.FreePeoples, FreeCard);
            round.Record(Faces);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-revealed") == true);
            PointerGestureEvents.ClickAt(Card(stage, "attacker"));
            yield return stage.Until(() => stage.Board.Cards.CardId == ShadowCard && stage.Board.Cards.Root.style.display.value == DisplayStyle.Flex);
            Assert.AreEqual(DisplayStyle.None, stage.Board.Cards.Root.Q(className: "card-toolbar").resolvedStyle.display, "Read-only: no Play bar.");
            yield return new WaitForSecondsRealtime(4f);
            Assert.IsTrue(stage.Playback.Holding, "The presentation holds while the card is read.");
            PointerGestureEvents.ClickAt(stage.Board.Cards.Root.Q<Button>("close-card"));
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ARollRingsTheDiceItChangedAndARerollOnlyItsDie()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Record(battle => Roll(battle.Attacker, "a", 6, 5, 2, 1));
            round.Record(battle => Roll(battle.Defender, "d", 6, 3));
            round.Record(battle =>
            {
                var die = battle.Defender.Dice.Single(d => d.Id == "d1");
                die.Reroll = 5;
                die.RerollHit = true;
                battle.Defender.Hits = 2;
            });
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Rolled(stage).Count == 4);
            CollectionAssert.AreEquivalent(new[] { "battle-die-a0", "battle-die-a1", "battle-die-a2", "battle-die-a3" }, Rolled(stage), "The attacker's roll.");
            LayoutTests.Save(stage.Texture, "ui311-roll.png");
            yield return stage.Until(() => Rolled(stage).SequenceEqual(new[] { "battle-die-d0", "battle-die-d1" }));
            yield return stage.Until(() => Rolled(stage).SequenceEqual(new[] { "battle-die-d1" }));
            Assert.IsTrue(stage.Playback.Holding, "The reroll rings only its die.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ACancelledCardIsRingedWithItsCancellation()
        {
            using var stage = Attack(out var fight);
            fight.Record(Faces);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Record(battle => battle.Defender.CardCancelled = true);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "defender")?.ClassListContains(BattlePresenter.CardMarkClass) == true);
            StringAssert.Contains("Cancelled", Card(stage, "defender").text);
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator CommittedLossesAreMarkedBeforeTheyLeaveWithADowngradeAsItsReplacement()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            var regular = round.Units("sauron", UnitKind.Regular).First();
            var elite = round.Units("sauron", UnitKind.Elite).First();
            var replacement = stage.Reserve("sauron", UnitKind.Regular, 1).Single();
            var losses = round.Decide(Side.Shadow);
            losses.Facts.LossDetails.Add(new ActionLossFact { Figure = stage.Unit(regular), Operation = BattleLossKind.Remove, RegionId = "minas-morgul", Hits = 1 });
            losses.Facts.LossDetails.Add(new ActionLossFact
            {
                Figure = stage.Unit(elite), Operation = BattleLossKind.Downgrade, RegionId = "minas-morgul", Hits = 1, ReplacementId = replacement,
                ReplacementFrom = "reserve"
            });
            round.Change(battle =>
            {
                battle.Attacker.Units.RemoveAll(u => u.Id == regular || u.Id == elite);
                battle.Attacker.Units.Add(new UnitView { Id = replacement, Nation = "sauron", Kind = UnitKind.Regular, Side = Side.Shadow, Location = "minas-morgul" });
                battle.Attacker.ParticipantIds = battle.Attacker.Units.Select(u => u.Id).ToList();
            });
            var final = round.Latest();
            PresentedBoardStage.Place(final, new[] { regular, elite }, "reserve");
            PresentedBoardStage.Place(final, new[] { replacement }, "minas-morgul");
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q(BattlePresenter.PlateName) != null);
            var plate = stage.Board.Root.Q(BattlePresenter.PlateName);
            CollectionAssert.AreEquivalent(new[] { "−1", "" }, plate.Query<Label>("observed-delta-count").ToList().Select(l => l.text),
                "A removal counts; a downgrade is its replacement.");
            Assert.AreEqual(1, plate.Query<Label>().ToList().Count(l => l.text == "→"), "Elite → Regular.");
            Assert.AreEqual("×1", Figure(stage, "attacker", "sauron-Elite"), "The losses still stand while marked.");
            LayoutTests.Save(stage.Texture, "ui311-losses.png");
            yield return stage.Until(() => Figure(stage, "attacker", "sauron-Elite") == null);
            Assert.AreEqual("×3", Figure(stage, "attacker", "sauron-Regular"), "One Regular left, one replaced the Elite.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator TheWitchKingLostWithHisArmyIsRingedOnHisPortrait()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            var king = PresentedBoardStage.Figure(stage.Before.Characters.Single(c => c.Id == WitchKing));
            round.Decide(Side.Shadow).Facts.Figures.Add(new ActionFigureChange { Before = king, After = king, From = "minas-morgul", To = "casualties" });
            round.Change(battle =>
            {
                battle.Attacker.Units.Clear();
                battle.Attacker.Characters.Clear();
                battle.Attacker.ParticipantIds.Clear();
            });
            var final = round.Latest();
            final.Characters.RemoveAll(c => c.Id == WitchKing);
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q(BattlePresenter.RingName) is { } ring &&
                stage.Board.Battles.Presented.Q("portrait-" + WitchKing) is { } portrait && ring.worldBound.Overlaps(portrait.worldBound));
            LayoutTests.Save(stage.Texture, "ui311-character-lost.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ALaterRoundStaysInTheSameWindow()
        {
            using var stage = Attack(out var fight);
            fight.Record(Faces);
            yield return Open(stage, fight);
            var window = stage.Board.Battles.Root.worldBound;
            var round = fight.Next();
            round.Record(battle =>
            {
                battle.Round = 2;
                battle.Stage = "AttackerCard";
                battle.CardsRevealed = false;
                foreach (var force in new[] { battle.Attacker, battle.Defender })
                {
                    force.CardCommitted = false;
                    force.CardId = null;
                    force.Dice.Clear();
                }
            });
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Battles.Root.Q<Label>(className: "title")?.text.EndsWith("Round 2") == true);
            Assert.IsTrue(stage.Board.Battles.IsOpen);
            Assert.AreEqual(window.position, stage.Board.Battles.Root.worldBound.position, "The window keeps its place.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ARetreatAndAnAdvanceReturnToTheMap()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            var defenders = stage.Before.Units.Where(u => u.Location == "osgiliath" && u.Side == Side.FreePeoples).Select(u => u.Id).ToList();
            var attackers = round.Units("sauron", null);
            round.Record(battle => battle.Stage = "Advance").Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.Retreat, From = "osgiliath", To = "minas-tirith", Participants = defenders.Select(stage.Unit).ToList()
            });
            round.Record(battle => battle.Stage = "Complete").Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.Advance, From = "minas-morgul", To = "osgiliath", Participants = attackers.Select(stage.Unit).ToList()
            });
            var final = round.Latest();
            PresentedBoardStage.Place(final, defenders, "minas-tirith");
            PresentedBoardStage.Place(final, attackers, "osgiliath");
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Flying("minas-tirith"));
            yield return stage.Until(() => stage.Flying("osgiliath"));
            yield return stage.Released();
            Assert.AreEqual(attackers.Count, stage.Army("osgiliath"));
        }

        [UnityTest]
        public IEnumerator ASiegeExtensionShowsItsFigureChangeInTheWindow()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            var elite = round.Units("sauron", UnitKind.Elite).Single();
            var regular = stage.Reserve("sauron", UnitKind.Regular, 1).Single();
            var extend = round.Decide(Side.Shadow);
            extend.Costs.Add(new ActionCost { Kind = ActionCostKind.Figure, Id = elite, Owner = Side.Shadow, Source = "extend-siege" });
            extend.Facts.Figures.Add(stage.Change(elite, "minas-morgul", "reserve"));
            extend.Facts.Figures.Add(stage.Change(regular, "reserve", "minas-morgul"));
            round.Change(battle => battle.Round = 2);
            var final = round.Latest();
            PresentedBoardStage.Place(final, new[] { elite }, "reserve");
            PresentedBoardStage.Place(final, new[] { regular }, "minas-morgul");
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q(BattlePresenter.PlateName)?.Q<Label>("observed-delta-count")?.text == "−1");
            yield return stage.Until(() => stage.Board.Root.Q(BattlePresenter.PlateName)?.Q<Label>("observed-delta-count")?.text == "+1");
            Assert.IsTrue(stage.Playback.Holding, "The cost is a visible figure change, not a prompt.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator SkipEndsOnTheLatestStateAndGivesTheControlsBack()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Reveal(Side.Shadow, ShadowCard);
            round.Reveal(Side.FreePeoples, FreeCard);
            round.Record(Faces);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-revealed") == true);
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            Assert.IsFalse(stage.Board.Battles.Presenting);
            Assert.IsNull(stage.Board.Root.Q(BattlePresenter.RingName));
            Assert.IsNull(stage.Board.Root.Q(BattlePresenter.PlateName));
        }

        [UnityTest]
        public IEnumerator AShortBattleIsRecordedAsAMutedClip()
        {
            using var stage = Attack(out var fight, width: 1920, scale: 1.25f);
            yield return stage.Ready();
            var frame = 0;
            stage.Play(fight.Latest(), "grey-havens", fight.Steps.ToArray());
            yield return Clip(stage, () => frame++);
            var round = fight.Next();
            round.Record(battle => Commit(battle.Attacker))
                .Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
            round.Record(battle => Commit(battle.Defender));
            round.Reveal(Side.Shadow, ShadowCard);
            round.Reveal(Side.FreePeoples, FreeCard);
            round.Record(battle =>
            {
                Faces(battle);
                Roll(battle.Attacker, "a", 6, 5, 2);
            });
            round.Record(battle => Roll(battle.Defender, "d", 6, 1));
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            var started = Time.realtimeSinceStartup;
            yield return Clip(stage, () => frame++);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 4.5f, "The commits, the 3 s reveal and both rolls play in turn.");
        }

        // Frames of one presentation until its release.
        private static IEnumerator Clip(PresentedBoardStage stage, Func<int> frame)
        {
            var deadline = Time.realtimeSinceStartup + 20;
            while (stage.Playback.Holding && Time.realtimeSinceStartup < deadline)
            {
                LayoutTests.Save(stage.Texture, "ui311-clip-" + frame().ToString("000") + ".png");
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.IsFalse(stage.Playback.Holding, "The presentation ends within the clip.");
        }

        // The Free Peoples watch the Shadow's attack on Osgiliath: Sauron's three Regulars, an Elite and the Witch-king from
        // Minas Morgul against Gondor's Regular and Elite. The attack's own step records the battle's opening state.
        private static PresentedBoardStage Attack(out Fight fight, int width = 1366, float scale = 1f)
        {
            var stage = new PresentedBoardStage(width, scale, false);
            stage.Put("sauron", UnitKind.Regular, "minas-morgul", 3);
            stage.Put("sauron", UnitKind.Elite, "minas-morgul", 1);
            stage.Put("gondor", UnitKind.Elite, "osgiliath", 1);
            PresentedBoardStage.Character(stage.Before, WitchKing, WitchKing, Side.Shadow, "minas-morgul");
            fight = new Fight(stage, 1, Opening(stage.Before));
            fight.Decide(Side.Shadow);
            return stage;
        }

        // The attack's revision plays and its window stays open; the next revisions continue the battle.
        private static IEnumerator Open(PresentedBoardStage stage, Fight fight)
        {
            yield return stage.Ready();
            stage.Play(fight.Latest(), null, fight.Steps.ToArray());
            yield return stage.Released();
            Assert.IsTrue(stage.Board.Battles.IsOpen, "The battle's window is open.");
        }

        private static BattleView Opening(PlayerView view) => new BattleView
        {
            Id = BattleId, RegionId = "osgiliath", Round = 1, Stage = "AttackerCard", Mode = BattleMode.Field,
            Attacker = Force(view, Side.Shadow, "minas-morgul"), Defender = Force(view, Side.FreePeoples, "osgiliath")
        };

        private static BattleForceView Force(PlayerView view, Side side, string region)
        {
            var units = view.Units.Where(u => u.Location == region && u.Side == side).Select(Copy).ToList();
            var characters = view.Characters.Where(c => c.Location == region && c.Side == side).Select(Copy).ToList();
            return new BattleForceView
            {
                Side = side, RegionId = region, Units = units, Characters = characters, Strength = units.Count, CombatDice = Math.Min(5, units.Count),
                HitTarget = 5, Leadership = characters.Count, ParticipantIds = units.Select(u => u.Id).Concat(characters.Select(c => c.Id)).ToList()
            };
        }

        // A committed card as the public state records it: a back of its deck.
        private static void Commit(BattleForceView force)
        {
            force.CardCommitted = true;
            force.CardBack = "strategy";
        }

        private static void Faces(BattleView battle)
        {
            battle.CardsRevealed = true;
            battle.Stage = "BeforeRoll";
            battle.Attacker.CardCommitted = battle.Defender.CardCommitted = true;
            battle.Attacker.CardId = ShadowCard;
            battle.Defender.CardId = FreeCard;
        }

        private static void Roll(BattleForceView force, string prefix, params int[] results)
        {
            force.Dice = results.Select((result, index) => new BattleDieView
            {
                Id = prefix + index, Result = result, HitTarget = 5, Hit = result >= 5
            }).ToList();
            force.Hits = results.Count(result => result >= 5);
        }

        private static Button Card(PresentedBoardStage stage, string role) => stage.Board.Battles.Presented.Q<Button>("battle-card-" + role);

        private static List<string> Rolled(PresentedBoardStage stage) => stage.Board.Battles.Presented
            .Query(className: BattlePresenter.RollClass).ToList().Select(die => die.name).ToList();

        // A figure stack's count in a column ("×3"), or null when the kind is gone.
        private static string Figure(PresentedBoardStage stage, string role, string kind) => stage.Board.Battles.Presented
            .Q("battle-" + role)?.Q("battle-figure-" + kind)?.Q<Label>("battle-figure-count")?.text;

        private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

        // One revision of a battle as the engine records it: the decision's step (with its actor) records the revision's
        // final state, the automatic steps after it the states on the way; a reveal records no state. Every step belongs to
        // the attack's action.
        private sealed class Fight
        {
            private readonly PresentedBoardStage stage;
            private readonly long revision;
            private readonly PlayerView view;
            private readonly List<ActionLogStep> decisions = new List<ActionLogStep>();
            private BattleView state;
            public List<ActionLogStep> Steps { get; } = new List<ActionLogStep>();

            public Fight(PresentedBoardStage stage, long revision, BattleView state, PlayerView view = null)
            {
                this.stage = stage;
                this.revision = revision;
                this.view = view;
                this.state = state;
            }

            // The opponent's decision; its state is the revision's last one.
            public ActionLogStep Decide(Side actor)
            {
                var step = Step(actor, "ActionDecision");
                decisions.Add(step);
                return step;
            }

            // An automatic step with the state after it.
            public ActionLogStep Record(Action<BattleView> change = null)
            {
                Change(change);
                var step = Step(null, "BattleFact");
                step.Facts.Battle = Copy(state);
                return step;
            }

            // A change the decision makes without a step of its own.
            public void Change(Action<BattleView> change) => change?.Invoke(state);

            public ActionLogStep Reveal(Side owner, string card)
            {
                var step = Step(null, "CombatCardRevealed");
                step.Facts.Cards.Add(new ActionCardFact { Owner = owner, Use = ActionCardUse.CombatReveal, Id = card, Deck = "strategy" });
                return step;
            }

            // The battle's next revision, from the state this one ended on.
            public Fight Next() => new Fight(stage, revision + 1, Copy(state), Latest());

            public PlayerView Latest()
            {
                foreach (var decision in decisions)
                    decision.Facts.Battle = Copy(state);
                var latest = view == null ? stage.Next(stage.Before) : Copy(view);
                latest.Revision = revision;
                latest.Battle = Copy(state);
                return latest;
            }

            public List<string> Units(string nation, UnitKind? kind) => state.Attacker.Units
                .Where(u => u.Nation == nation && (kind == null || u.Kind == kind)).Select(u => u.Id).ToList();

            private ActionLogStep Step(Side? actor, string kind)
            {
                var step = stage.Step(Steps.Count + 1, revision);
                step.ActionId = "presented-battle-action";
                step.Actor = actor;
                step.Automatic = !actor.HasValue;
                step.Kind = kind;
                step.Context.BattleId = BattleId;
                Steps.Add(step);
                return step;
            }
        }
    }
}
