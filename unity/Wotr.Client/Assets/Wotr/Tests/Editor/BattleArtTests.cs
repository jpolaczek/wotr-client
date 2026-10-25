using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.Client.Tests
{
    // UI-115: the illustrated forces are built from the server's battle view, never from the current map.
    public sealed class BattleArtTests
    {
        [Test]
        public void ForcesShowFiguresPerNationAndKindWithCountsAndFourStatistics()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { });
            var defender = root.Q("battle-defender");
            Assert.IsTrue(defender.ClassListContains("sh"));
            var sauron = defender.Q("battle-nation-sauron");
            Assert.IsNotNull(sauron, "Shadow regulars stand under the Sauron emblem.");
            Assert.IsTrue(sauron.Q("battle-nation-emblem").ClassListContains("nation-emblem-sauron"));
            var figure = sauron.Q("battle-figure-sauron-Regular");
            Assert.IsNotNull(figure.Q<Image>(className: "figure-art-image"), "The figure comes from the board atlas.");
            Assert.AreEqual("×" + view.Battle.Defender.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular),
                figure.Q<Label>("battle-figure-count").text);
            var statistics = root.Q("battle-attacker").Q("battle-statistics");
            CollectionAssert.AreEqual(new[] { "battle-stat-strength", "battle-stat-leadership", "battle-stat-dice", "battle-stat-hit" },
                statistics.Children().Select(c => c.name).ToArray());
            Assert.AreEqual(view.Battle.Attacker.Strength.ToString(), statistics.Q("battle-stat-strength").Q<Label>("battle-stat-value").text);
            Assert.AreEqual(view.Battle.Attacker.HitTarget + "+", statistics.Q("battle-stat-hit").Q<Label>("battle-stat-value").text);
            StringAssert.Contains("Combat modifier", statistics.Q("battle-stat-dice").tooltip, "The modifiers stay in the tooltip.");
            Assert.IsNull(root.Q("battle-garrison-shelf"), "No ledge without figures outside the fight.");
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
        }

        [Test]
        public void CharactersBecomeMedallionsAndCancelledAbilitiesSealThem()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Attacker;
            var gimli = new CharacterView { Id = "gimli", DefinitionId = "base.character.gimli", Side = force.Side, Location = force.RegionId };
            var boromir = new CharacterView { Id = "boromir", DefinitionId = "base.character.boromir", Side = force.Side, Location = force.RegionId };
            view.Characters.Add(gimli);
            view.Characters.Add(boromir);
            force.Characters.Add(gimli);
            force.Characters.Add(boromir);
            force.CancelledAbilities.Add("gimli");
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { });
            var portraits = root.Q("battle-attacker").Q("battle-portraits");
            Assert.AreEqual(2, portraits.childCount);
            var gimliMedallion = portraits.Q("portrait-gimli");
            Assert.IsTrue(gimliMedallion.ClassListContains(PortraitMedallion.CancelledClass));
            Assert.IsTrue(gimliMedallion.ClassListContains("portrait-free"));
            StringAssert.Contains("cancelled", gimliMedallion.tooltip);
            Assert.IsFalse(portraits.Q("portrait-boromir").ClassListContains(PortraitMedallion.CancelledClass));
            Assert.AreEqual("B", portraits.Q("portrait-boromir").Q<Label>("portrait-initial").text);
            StringAssert.Contains("⦸ Gimli", root.Q<Label>("battle-characters").text, "The names line keeps the cancelled mark.");
        }

        [Test]
        public void FiguresOutsideTheFightSitOnTheLedgeAndTheStageRibbonMarksTheCurrentStage()
        {
            var view = BattleScreenFixture.Create(CommandKind.AdvanceBattle, mode: BattleMode.Relief);
            var force = view.Battle.Attacker;
            view.Units.Add(new UnitView { Id = "rear", Nation = "rohan", Kind = UnitKind.Regular, Side = force.Side, Location = force.RegionId });
            view.Battle.Stage = "DefenderReroll";
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { });
            var shelf = root.Q("battle-attacker").Q("battle-garrison-shelf");
            Assert.IsNotNull(shelf);
            StringAssert.Contains("Rear guard", shelf.Q<Label>("battle-nonparticipants").text);
            Assert.IsNotNull(shelf.Q(className: "battle-garrison-icon"));
            var stages = BattleStages.Create(view.Battle);
            Assert.AreEqual(5, stages.childCount);
            Assert.IsTrue(stages.Q("battle-stage-reroll").ClassListContains("current-stage"));
            Assert.IsFalse(stages.Q("battle-stage-roll").ClassListContains("current-stage"));
            Assert.IsNotNull(stages.Q("battle-stage-reroll").Q(className: "battle-stage-icon-reroll"));
        }

        [Test]
        public void CombatDiceCarryTheirFaceAndStateClassesForTheArtKit()
        {
            var battle = BattleScreenFixture.Create(CommandKind.RerollBattle).Battle;
            var rerolled = new BattleDieView { Id = "d", Result = 2, HitTarget = 5, Hit = false, Reroll = 6, RerollHit = true };
            var button = new BattleDieButton(rerolled, "Combat", battle, null);
            var first = button.Q<DiePips>("battle-first-roll");
            var second = button.Q<DiePips>("battle-reroll-result");
            Assert.IsTrue(first.Illustrated && second.Illustrated, "Battle dice take their faces from the art kit.");
            Assert.IsTrue(first.ClassListContains(DiePips.FaceClassPrefix + "2"));
            Assert.IsTrue(second.ClassListContains(DiePips.FaceClassPrefix + "6"));
            Assert.IsTrue(first.ClassListContains("battle-pips-rerolled"), "The first roll dims once re-rolled.");
            Assert.IsFalse(first.ClassListContains("battle-pips-hit"));
            Assert.IsTrue(second.ClassListContains("battle-pips-hit"), "The re-roll that hit glows.");
            Assert.IsNotNull(button.Q("battle-reroll-arrow"));

            var miss = new BattleDieView { Id = "m", Result = 1, HitTarget = 5 };
            var missButton = new BattleDieButton(miss, "Combat", battle, null);
            var face = missButton.Q<DiePips>("battle-first-roll");
            Assert.IsTrue(face.ClassListContains(DiePips.FaceClassPrefix + "1"));
            Assert.IsFalse(face.ClassListContains("battle-pips-hit"));
            Assert.IsNull(missButton.Q("battle-reroll-arrow"));

            // UI-325 (owner review 2026-10-25): Hunt dice are the same ivory dice, picked by the same face class.
            var hunt = new HuntDieFace();
            hunt.Apply(4);
            Assert.IsTrue(hunt.Illustrated, "Hunt dice are illustrated dice, not painted pips.");
            Assert.IsTrue(hunt.ClassListContains(HuntDieFace.FaceClass));
            Assert.IsTrue(hunt.ClassListContains(DiePips.FaceClassPrefix + "4"));
        }

        [Test]
        public void LossTokensShowFigureArtOverlaysOrderAndHitSeals()
        {
            var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.TakeBattleLosses);
            var root = new VisualElement();
            var losses = new BattleLossChoices(root, view, draft, action => action());
            losses.Refresh();
            var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite);
            var token = root.Q<Button>("loss-" + elite.Id);
            Assert.IsTrue(token.ClassListContains("illustrated-figure"), "The token carries the atlas figure.");
            Assert.IsNotNull(token.Q<Image>("figure-art"));
            Assert.IsTrue(token.Q("loss-figure-nation").ClassListContains("nation-emblem-" + elite.Nation));
            Assert.AreEqual("", token.text);
            Assert.AreEqual(view.Battle.PendingHits, root.Q("battle-hit-seals").childCount, "One seal per hit to assign.");
            Assert.AreEqual(0, root.Q("battle-hit-seals").Children().Count(s => s.ClassListContains("battle-hit-seal-spent")));

            BattleDraft.CycleLoss(draft, elite);
            losses.Refresh();
            StringAssert.StartsWith("1.", token.text);
            StringAssert.Contains("↓", token.text);
            Assert.IsTrue(token.Q("loss-overlay").ClassListContains("loss-overlay-downgrade"));
            Assert.IsTrue(token.ClassListContains("selected"));
            Assert.AreEqual(1, root.Q("battle-hit-seals").Children().Count(s => s.ClassListContains("battle-hit-seal-spent")), "A downgrade spends one seal.");

            BattleDraft.CycleLoss(draft, elite);
            losses.Refresh();
            StringAssert.Contains("×", token.text);
            Assert.IsTrue(token.Q("loss-overlay").ClassListContains("loss-overlay-remove"));
            Assert.IsTrue(token.ClassListContains("loss-removed"));
            Assert.AreEqual(2, root.Q("battle-hit-seals").Children().Count(s => s.ClassListContains("battle-hit-seal-spent")), "A removed elite spends two seals.");
            StringAssert.Contains("Assigned 2 / 2", root.Q<Label>("battle-loss-budget").text);
            Assert.AreEqual(2, root.Q("battle-loss-legend").Query(className: "battle-loss-legend-icon").ToList().Count);
        }

        [Test]
        public void EndOfBattleChoicesCarryTheirIconsAndTheReportIsAChronicle()
        {
            Assert.AreEqual("continue", BattleChoices.IconFor(CommandKind.ContinueBattle));
            Assert.AreEqual("stop", BattleChoices.IconFor(CommandKind.StopBattle));
            Assert.AreEqual("hold", BattleChoices.IconFor(CommandKind.HoldBattle));
            Assert.AreEqual("retreat", BattleChoices.IconFor(CommandKind.RetreatIntoSiege));
            Assert.AreEqual("advance", BattleChoices.IconFor(CommandKind.AdvanceBattle));
            Assert.AreEqual("extend-siege", BattleChoices.IconFor(CommandKind.ExtendSiege));
            Assert.AreEqual("", BattleChoices.IconFor(CommandKind.CommitCombatCard), "Only decisions with art get an icon.");

            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle, mode: BattleMode.Sortie);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ContinueBattle);
            var root = new VisualElement();
            var footer = new VisualElement();
            new BattleChoices(root, footer, view, draft, action => action(), action => action(), _ => { }, BoardPresentation.Load());
            var stop = footer.Q<Button>("action-StopBattle");
            Assert.IsNotNull(stop);
            Assert.IsTrue(stop.ClassListContains("battle-direct"));
            Assert.IsTrue(stop.Q("battle-direct-icon").ClassListContains("battle-direct-icon-stop"));
            Assert.AreEqual("Stop battle", stop.text, "The text stays the control; the icon only decorates it.");

            var report = new BattleReport(null, BoardPresentation.Load());
            Assert.IsTrue(report.Root.ClassListContains("battle-chronicle"));
            Assert.IsTrue(report.Root.Q<Label>("battle-report-round").ClassListContains("battle-round-tab"));
            Assert.IsNotNull(report.Root.Q<Button>("close-battle-report"));
        }
    }
}
