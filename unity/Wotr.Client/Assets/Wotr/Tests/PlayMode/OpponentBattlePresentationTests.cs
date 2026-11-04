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
    // window, and a retreat or an advance returns to the map. Frames go to .artifacts/screenshots (ui311-*). TEST-20 (owner,
    // 2026-11-04): each scene also plays with the Shadow watching the Free Peoples attack.
    public sealed class OpponentBattlePresentationTests
    {
        private const string BattleId = "presented-battle";
        private const string WitchKing = "base.character.witch-king";
        private const string Gimli = "base.character.gimli";
        internal const string ShadowCard = "base.sh.strategy.13";
        internal const string FreeCard = "base.fp.strategy.14";

        [UnityTest]
        public IEnumerator TheAttackOpensTheBattleWindowAfterItsTraceAndTheReleaseGivesBackTheControls([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
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

        // UI-335 (owner 2026-10-25): the presented round and the decision share the window's one ribbon — the decision only
        // joins it — so a stage reached in the presentation is not glided to again at the release.
        [UnityTest]
        public IEnumerator ThePresentedRoundAndTheDecisionShareOneRibbon([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return stage.Ready();
            stage.Play(fight.Latest(), "grey-havens", fight.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Battles.IsOpen && stage.Board.Battles.Presenting);
            var ribbon = stage.Board.Battles.Ribbon;
            Assert.IsTrue(stage.Board.Battles.Presented.Contains(ribbon.Root), "The presented round shows the window's ribbon.");
            yield return stage.Released();
            yield return null;
            Assert.AreSame(ribbon.Root, stage.Board.Tray.Q("battle-stages"), "The decision shows the same row.");
            var marker = ribbon.Marker.layout.x;
            yield return new WaitForSecondsRealtime((float)StageRibbon.GlideSeconds / 2);
            Assert.AreEqual(marker, ribbon.Marker.layout.x, .5f, "No second glide at the release.");
        }

        [UnityTest]
        public IEnumerator TheCommittedCardStaysABackUntilBothCardsTurnFaceUpTogether([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Decide(stage.Opponent);
            round.Record(battle =>
            {
                battle.Stage = "DefenderCard";
                Commit(battle.Attacker);
            }).Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
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
            stage.Save("ui311-reveal.png");
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains(BattlePresenter.CardMarkClass) != true);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 2.8f, "The reveal holds about 3 s.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator AClickOnARevealedCardOpensItReadOnlyAndHoldsThePresentation([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            round.Reveal(Side.Shadow, ShadowCard);
            round.Reveal(Side.FreePeoples, FreeCard);
            round.Record(Faces);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-revealed") == true);
            PointerGestureEvents.ClickAt(Card(stage, "attacker"));
            yield return stage.Until(() => stage.Board.Cards.CardId == CardOf(stage.Opponent) && stage.Board.Cards.Root.style.display.value == DisplayStyle.Flex);
            Assert.AreEqual(DisplayStyle.None, stage.Board.Cards.Root.Q(className: "card-toolbar").resolvedStyle.display, "Read-only: no Play bar.");
            yield return new WaitForSecondsRealtime(4f);
            Assert.IsTrue(stage.Playback.Holding, "The presentation holds while the card is read.");
            PointerGestureEvents.ClickAt(stage.Board.Cards.Root.Q<Button>("close-card"));
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ARollRingsTheDiceItChangedAndARerollOnlyItsDie([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
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
            stage.Save("ui311-roll.png");
            yield return stage.Until(() => Rolled(stage).SequenceEqual(new[] { "battle-die-d0", "battle-die-d1" }));
            yield return stage.Until(() => Rolled(stage).SequenceEqual(new[] { "battle-die-d1" }));
            Assert.IsTrue(stage.Playback.Holding, "The reroll rings only its die.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ACancelledCardIsRingedWithItsCancellation([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
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
        public IEnumerator CommittedLossesAreMarkedBeforeTheyLeaveWithADowngradeAsItsReplacement([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            var nation = stage.Main;
            var regular = round.Units(nation, UnitKind.Regular).First();
            var elite = round.Units(nation, UnitKind.Elite).First();
            var replacement = stage.Reserve(nation, UnitKind.Regular, 1).Single();
            var losses = round.Decide(stage.Opponent);
            losses.Facts.LossDetails.Add(new ActionLossFact { Figure = stage.Unit(regular), Operation = BattleLossKind.Remove, RegionId = "minas-morgul", Hits = 1 });
            losses.Facts.LossDetails.Add(new ActionLossFact
            {
                Figure = stage.Unit(elite), Operation = BattleLossKind.Downgrade, RegionId = "minas-morgul", Hits = 1, ReplacementId = replacement,
                ReplacementFrom = "reserve"
            });
            round.Change(battle =>
            {
                battle.Attacker.Units.RemoveAll(u => u.Id == regular || u.Id == elite);
                battle.Attacker.Units.Add(new UnitView { Id = replacement, Nation = nation, Kind = UnitKind.Regular, Side = stage.Opponent, Location = "minas-morgul" });
                battle.Attacker.ParticipantIds = battle.Attacker.Units.Select(u => u.Id).ToList();
            });
            var final = round.Latest();
            PresentedBoardStage.Place(final, new[] { regular, elite }, "reserve");
            PresentedBoardStage.Place(final, new[] { replacement }, "minas-morgul");
            stage.Play(final, null, round.Steps.ToArray());
            // UI-337: one hit per committed loss, in the recorded order — first the Regular…
            yield return stage.Until(() => Struck(stage, nation + "-Regular"));
            var burst = stage.Board.Root.Q(BattleHits.BurstName);
            Assert.IsNotNull(burst, "A hit bursts over the struck stack.");
            Assert.IsTrue(stage.Board.Battles.Presented.Q("battle-attacker").Q("battle-figure-" + nation + "-Regular").worldBound
                .Contains(burst.worldBound.center), "The hit lands on the stack.");
            Assert.AreEqual("×3", Figure(stage, "attacker", nation + "-Regular"), "The figure stands while it is hit.");
            Assert.IsFalse(Struck(stage, nation + "-Elite"), "One loss at a time.");
            Assert.IsFalse(stage.Board.Battles.Presented.Q("battle-figure-" + nation + "-Regular").ClassListContains(BattleHits.LeavingClass),
                "Two Regulars stay.");
            stage.Save("ui337-hit-regular.png");
            yield return stage.Until(() => Figure(stage, "attacker", nation + "-Regular") == "×2");
            Assert.AreEqual("×1", Figure(stage, "attacker", nation + "-Elite"), "The Elite is not hit yet.");
            // …then the Elite, the last of its stack, which fades and becomes a Regular.
            yield return stage.Until(() => Struck(stage, nation + "-Elite"));
            Assert.IsTrue(stage.Board.Battles.Presented.Q("battle-figure-" + nation + "-Elite").ClassListContains(BattleHits.LeavingClass));
            yield return null;
            yield return null;
            stage.Save("ui337-hit-elite.png");
            yield return stage.Until(() => Figure(stage, "attacker", nation + "-Elite") == null);
            Assert.AreEqual("×3", Figure(stage, "attacker", nation + "-Regular"), "One Regular left, one replaced the Elite.");
            Assert.IsNull(stage.Board.Root.Q(BattleHits.BurstName), "The hit is over.");
            stage.Save("ui337-downgraded.png");
            yield return stage.Released();
        }

        private static bool Struck(PresentedBoardStage stage, string kind) =>
            stage.Board.Battles.Presented.Q("battle-attacker")?.Q("battle-figure-" + kind)?.ClassListContains(BattleHits.HitClass) == true;

        [UnityTest]
        public IEnumerator TheWitchKingLostWithHisArmyIsRingedOnHisPortrait([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            var commander = Commander(stage);
            var king = PresentedBoardStage.Figure(stage.Before.Characters.Single(c => c.Id == commander));
            round.Decide(stage.Opponent).Facts.Figures.Add(new ActionFigureChange { Before = king, After = king, From = "minas-morgul", To = "casualties" });
            round.Change(battle =>
            {
                battle.Attacker.Units.Clear();
                battle.Attacker.Characters.Clear();
                battle.Attacker.ParticipantIds.Clear();
            });
            var final = round.Latest();
            final.Characters.RemoveAll(c => c.Id == commander);
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Battles.Presented.Q("portrait-" + commander) is { } portrait &&
                PresentationGlow.IsLit(portrait));
            stage.Save("ui311-character-lost.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ALaterRoundStaysInTheSameWindow([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
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
        public IEnumerator ARetreatAndAnAdvanceReturnToTheMap([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var there = stage.Army("osgiliath");
            var round = fight.Next();
            var defenders = stage.Before.Units.Where(u => u.Location == "osgiliath" && u.Side == stage.Viewer).Select(u => u.Id).ToList();
            var attackers = round.Units(stage.Main, null);
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
            // UI-333: the battle is decided and the next scene (the retreat on the map) begins: its window steps aside.
            Assert.IsFalse(stage.Board.Battles.IsOpen, "The finished battle's window closes as the next scene starts.");
            stage.Save("ui333-window-closed.png");
            yield return stage.Until(() => stage.Flying("osgiliath"));
            Assert.IsFalse(stage.Board.Battles.IsOpen, "It stays closed for the advance.");
            yield return stage.Released();
            Assert.AreEqual(there + attackers.Count, stage.Army("osgiliath"));
        }

        // UI-333: a battle window the player dragged to a place of their own stays through the next scenes.
        [UnityTest]
        public IEnumerator AMovedBattleWindowStaysWhenTheNextSceneStarts([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            stage.Board.Windows.Remember(stage.Board.Battles.Root.name, new Vector2(40, 60));
            var round = fight.Next();
            var attackers = round.Units(stage.Main, null);
            round.Record(battle => battle.Stage = "Complete").Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.Advance, From = "minas-morgul", To = "osgiliath", Participants = attackers.Select(stage.Unit).ToList()
            });
            var final = round.Latest();
            PresentedBoardStage.Place(final, attackers, "osgiliath");
            stage.Play(final, null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Flying("osgiliath"));
            Assert.IsTrue(stage.Board.Battles.IsOpen, "Moved by the player, it stays.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ASiegeExtensionShowsItsFigureChangeInTheWindow([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            var elite = round.Units(stage.Main, UnitKind.Elite).Single();
            var regular = stage.Reserve(stage.Main, UnitKind.Regular, 1).Single();
            var extend = round.Decide(stage.Opponent);
            extend.Costs.Add(new ActionCost { Kind = ActionCostKind.Figure, Id = elite, Owner = stage.Opponent, Source = "extend-siege" });
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

        // UI-491 (owner, 2026-11-04): a quiet decision reads as one short caption on its side's column, and the ribbon steps as
        // ever. The commands that come with a scene of their own have none.
        [UnityTest]
        public IEnumerator AQuietDecisionStandsAsOneCaptionOnItsSidesColumn([Values(Side.FreePeoples, Side.Shadow)] Side viewer,
            [Values(CommandKind.HoldBattle, CommandKind.ContinueBattle, CommandKind.StopBattle, CommandKind.FightFieldBattle, CommandKind.UseEvilKnowledge)]
            CommandKind command)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            var decision = round.Decide(stage.Opponent);
            decision.Command = command;
            round.Change(battle => battle.Stage = "NextRound");
            var stageBefore = StageName(stage);
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            var plate = default(Label);
            yield return stage.Until(() => (plate = stage.Board.Root.Q<Label>(BattlePresenter.PlateName)) != null);
            var started = Time.realtimeSinceStartup;
            Assert.AreEqual(BattleChoiceCaption.Of(command), plate.text);
            Assert.AreEqual(PickingMode.Ignore, plate.pickingMode, "Nothing to click.");
            Assert.AreEqual(1, stage.Board.Root.Query(BattlePresenter.PlateName).ToList().Count, "One caption.");
            Assert.AreEqual(stageBefore, StageName(stage), "It comes before what the decision causes.");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(stage.Board.Battles.Presented.Q("battle-attacker").worldBound.Overlaps(plate.worldBound), "On the side that decided.");
            if (command == CommandKind.HoldBattle)
                LayoutTests.Save(stage.Texture, "ui491-caption-" + viewer + ".png");
            yield return stage.Until(() => stage.Board.Root.Q(BattlePresenter.PlateName) == null);
            Assert.AreEqual(BattlePresenter.ChoiceSeconds, Time.realtimeSinceStartup - started, .35f, "A short beat.");
            yield return stage.Released();
            Assert.IsEmpty(stage.Lit);
        }

        [UnityTest]
        public IEnumerator NoCardIsACaptionAndACommittedCardStaysABackWithoutOne([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            yield return Open(stage, fight);
            var round = fight.Next();
            var none = round.Decide(stage.Opponent);
            none.Command = CommandKind.CommitCombatCard;
            none.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.CombatCommit, Deck = "", Count = 0 });
            round.Change(battle => battle.Stage = "DefenderCard");
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q<Label>(BattlePresenter.PlateName)?.text == "No card");
            yield return new WaitForSecondsRealtime(.25f);
            LayoutTests.Save(stage.Texture, "ui491-no-card-" + viewer + ".png");
            yield return stage.Released();
            var next = round.Next();
            var committed = next.Decide(stage.Opponent);
            committed.Command = CommandKind.CommitCombatCard;
            committed.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
            next.Change(battle =>
            {
                battle.Stage = "DefenderCard";
                battle.Round = 2;
                Commit(battle.Attacker);
            });
            stage.Play(next.Latest(), null, next.Steps.ToArray());
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-back") == true);
            Assert.IsNull(stage.Board.Root.Q(BattlePresenter.PlateName), "A committed card is hidden: no caption.");
            Assert.IsFalse(Card(stage, "attacker").text.Contains("Initiative"), "And no identity.");
            yield return stage.Released();
        }

        // UI-491: the player's closed window hosts nothing, and takes no time.
        [UnityTest]
        public IEnumerator AClosedWindowWaitsForNoCaption()
        {
            using var stage = Attack(out var fight);
            yield return Open(stage, fight);
            stage.Board.Battles.Close();
            var round = fight.Next();
            round.Decide(stage.Opponent).Command = CommandKind.HoldBattle;
            round.Change(battle => battle.Stage = "NextRound");
            stage.Play(round.Latest(), null, round.Steps.ToArray());
            for (var frame = 0; frame < 10 && stage.Playback.Holding; frame++)
            {
                Assert.IsNull(stage.Board.Root.Q(BattlePresenter.PlateName));
                yield return null;
            }
            yield return stage.Released();
        }

        // UI-493 (owner, 2026-11-04): the hand count steps as the card goes down, keeping its deck, and never names it.
        [UnityTest]
        public IEnumerator TheHandCountStepsWithTheCommittedCard([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
            stage.Hands(4, 4);
            yield return Open(stage, fight);
            var count = stage.Board.TopBar.Opponent.Root.Q("opponent-cards").Q<Label>(className: "summary-value");
            Assert.AreEqual("4", count.text);
            var round = fight.Next();
            round.Decide(stage.Opponent).Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
            round.Change(battle =>
            {
                battle.Stage = "DefenderCard";
                Commit(battle.Attacker);
            });
            var latest = round.Latest();
            latest.Players.Single(p => p.Side == stage.Opponent).HandCount = 3;
            stage.Play(latest, null, round.Steps.ToArray());
            yield return stage.Until(() => count.text == "3");
            Assert.IsTrue(stage.Playback.Holding, "It steps in the presentation.");
            Assert.AreEqual(4, stage.Board.View.Players.Single(p => p.Side == stage.Opponent).HandCount, "The board's view waits.");
            yield return stage.Until(() => Card(stage, "attacker")?.ClassListContains("battle-card-back") == true);
            yield return stage.Released();
            Assert.AreEqual("3", count.text, "The release does not count it again.");
        }

        [UnityTest]
        public IEnumerator SkipEndsOnTheLatestStateAndGivesTheControlsBack([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Attack(out var fight, viewer: viewer);
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
            Assert.IsEmpty(stage.Lit);
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
                .Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 1 });
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
        // TEST-20: the Shadow may watch instead, Gondor's three Regulars, an Elite and Gimli attacking its Sauron Elite.
        internal static PresentedBoardStage Attack(out Fight fight, int width = 1366, float scale = 1f, Side viewer = Side.FreePeoples)
        {
            var stage = new PresentedBoardStage(width, scale, false, viewer);
            stage.Put(stage.Main, UnitKind.Regular, "minas-morgul", 3);
            stage.Put(stage.Main, UnitKind.Elite, "minas-morgul", 1);
            stage.Put(stage.Own, UnitKind.Elite, "osgiliath", 1);
            PresentedBoardStage.Character(stage.Before, Commander(stage), Commander(stage), stage.Opponent, "minas-morgul");
            fight = new Fight(stage, 1, Opening(stage));
            fight.Decide(stage.Opponent);
            return stage;
        }

        private static string Commander(PresentedBoardStage stage) => stage.Opponent == Side.Shadow ? WitchKing : Gimli;

        // The attack's revision plays and its window stays open; the next revisions continue the battle.
        internal static IEnumerator Open(PresentedBoardStage stage, Fight fight)
        {
            yield return stage.Ready();
            stage.Play(fight.Latest(), null, fight.Steps.ToArray());
            yield return stage.Released();
            Assert.IsTrue(stage.Board.Battles.IsOpen, "The battle's window is open.");
        }

        private static BattleView Opening(PresentedBoardStage stage) => new BattleView
        {
            Id = BattleId, RegionId = "osgiliath", Round = 1, Stage = "AttackerCard", Mode = BattleMode.Field,
            Attacker = Force(stage.Before, stage.Opponent, "minas-morgul"), Defender = Force(stage.Before, stage.Viewer, "osgiliath")
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

        private static string CardOf(Side side) => side == Side.Shadow ? ShadowCard : FreeCard;

        internal static void Faces(BattleView battle)
        {
            battle.CardsRevealed = true;
            battle.Stage = "BeforeRoll";
            battle.Attacker.CardCommitted = battle.Defender.CardCommitted = true;
            battle.Attacker.CardId = CardOf(battle.Attacker.Side);
            battle.Defender.CardId = CardOf(battle.Defender.Side);
        }

        internal static void Roll(BattleForceView force, string prefix, params int[] results)
        {
            force.Dice = results.Select((result, index) => new BattleDieView
            {
                Id = prefix + index, Result = result, HitTarget = 5, Hit = result >= 5
            }).ToList();
            force.Hits = results.Count(result => result >= 5);
        }

        private static string StageName(PresentedBoardStage stage) =>
            stage.Board.Battles.Ribbon.Root.Q(className: "current-stage")?.name ?? "";

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
        internal sealed class Fight
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
