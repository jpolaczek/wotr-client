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
            // UI-420 (owner): each medallion explains its own number; a modifier of zero is left out.
            Assert.AreEqual("Strength " + view.Battle.Attacker.Strength, statistics.Q("battle-stat-strength").tooltip);
            Assert.AreEqual("Leadership 2", statistics.Q("battle-stat-leadership").tooltip);
            Assert.AreEqual("Combat dice 5", statistics.Q("battle-stat-dice").tooltip);
            Assert.AreEqual("Hits on 5+", statistics.Q("battle-stat-hit").tooltip);
            Assert.IsTrue(string.IsNullOrEmpty(statistics.tooltip), "The row between the medallions has no tooltip of its own.");
            StringAssert.StartsWith("Sauron · " + view.Battle.Defender.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular) + " Regular",
                sauron.tooltip, "UI-420 (owner): one line per nation, as on the map's army marker.");
            Assert.IsNull(root.Q("battle-garrison-shelf"), "No ledge without figures outside the fight.");
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
        }

        // UI-420 (owner): a non-zero modifier stands on the medallion it changes, signed; the card slot names the card
        // and its click instead of repeating the force's modifiers.
        [Test]
        public void MedallionsShowOnlyTheirOwnNonZeroModifiers()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Attacker;
            force.CombatModifier = 1;
            force.RerollModifier = -1;
            force.CaptainBonus = 1;
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { });
            var attacker = root.Q("battle-attacker");
            Assert.AreEqual("Hits on 5+\nCombat modifier +1 · Leader reroll modifier -1", attacker.Q("battle-stat-hit").tooltip);
            Assert.AreEqual("Combat dice 5\nCaptain bonus +1", attacker.Q("battle-stat-dice").tooltip);
            Assert.AreEqual("Leadership 2", attacker.Q("battle-stat-leadership").tooltip);
            var card = attacker.Q<Button>("battle-card-attacker");
            StringAssert.EndsWith("\nClick to inspect", card.tooltip);
            StringAssert.DoesNotContain("modifier", card.tooltip);
            Assert.IsTrue(string.IsNullOrEmpty(attacker.Q<Label>("attacker-strength").tooltip), "The heading has no notes to add.");
            StringAssert.DoesNotContain("+0", root.Q("battle-defender").Q("battle-stat-hit").tooltip);
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
            // UI-437 (owner): each name stands under its medallion, in the force row with the figures.
            Assert.AreEqual("⦸ Gimli", root.Q("battle-character-gimli").Q<Label>("battle-character-name").text, "The name keeps the cancelled mark.");
            Assert.AreEqual("Boromir", root.Q("battle-character-boromir").Q<Label>("battle-character-name").text);
            Assert.AreSame(root.Q("battle-attacker").Q("battle-force-row"), portraits.parent, "The characters stand in the force row.");
            Assert.AreSame(portraits.parent, root.Q("battle-attacker").Q("battle-composition").parent, "Beside the figures.");
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
            // UI-420 (owner): the ledge's tooltip spells out its symbols rather than repeating its text.
            StringAssert.Contains("Rear guard · Rohan · 1 Regular", shelf.Q<Label>("battle-nonparticipants").tooltip);
            Assert.IsNotNull(shelf.Q(className: "battle-garrison-icon"));
            var stages = BattleStages.Create(view.Battle);
            Assert.AreEqual(5, stages.Query(className: "battle-stage").ToList().Count);
            Assert.IsNotNull(stages.Q("stage-glide"), "UI-327: one marker glides between the stages.");
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
            // UI-439: Evil Knowledge's button carries the ability's own icon (AbilityArt), not a battle icon.
            Assert.AreEqual("", BattleChoices.IconFor(CommandKind.UseEvilKnowledge));
            StringAssert.StartsWith("Evil Knowledge · Click to extend the siege", BattleChoices.DirectTooltip(CommandKind.UseEvilKnowledge));
            Assert.AreEqual("", BattleChoices.IconFor(CommandKind.CommitCombatCard), "Only decisions with art get an icon.");

            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle, mode: BattleMode.Sortie);
            view.Options.Add(new ActionOption { Kind = CommandKind.UseEvilKnowledge, Enabled = true });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ContinueBattle);
            var root = new VisualElement();
            var footer = new VisualElement();
            new BattleChoices(root, footer, view, draft, action => action(), action => action(), _ => { }, BoardPresentation.Load());
            var stop = footer.Q<Button>("action-StopBattle");
            Assert.IsNotNull(stop);
            Assert.IsTrue(stop.ClassListContains("battle-direct"));
            Assert.IsTrue(stop.Q("battle-direct-icon").ClassListContains("battle-direct-icon-stop"));
            // UI-440 (owner): without the Mouth's portrait, Evil Knowledge is its bare skill icon — no frame, no words —
            // glowing while usable and named in its tooltip; its click sends the same command.
            var evil = footer.Q<Button>("action-UseEvilKnowledge");
            Assert.IsTrue(evil.ClassListContains(BattleChoices.SkillChoiceClass));
            Assert.IsFalse(evil.ClassListContains("battle-direct"));
            Wotr.EditorTests.SkillIconChecks.Bare(evil, AbilityArt.Folder + "evil-knowledge");
            Assert.IsTrue(evil.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Evil Knowledge · ", evil.tooltip);
            Assert.AreEqual("Stop battle", stop.text, "The text stays the control; the icon only decorates it.");
            // UI-420 (owner): a direct choice says what its click does and names the battle's region.
            Assert.AreEqual("Stop battle · End the attack on Lossarnach; your army stays in Minas Tirith", stop.tooltip);

            var report = new BattleReport(null, BoardPresentation.Load());
            Assert.IsTrue(report.Root.ClassListContains("battle-chronicle"));
            Assert.IsTrue(report.Root.Q<Label>("battle-report-round").ClassListContains("battle-round-tab"));
            Assert.IsNotNull(report.Root.Q<Button>("close-battle-report"));
        }

        // UI-420 (owner): every direct battle choice says what its click does and names the battle's region; Keep all and
        // the card setting say what they keep or skip.
        [Test]
        public void DirectBattleChoicesNameWhatTheyDoAndWhere()
        {
            var data = BoardPresentation.Load();
            var view = BattleScreenFixture.Create(CommandKind.RetreatBattle);
            Assert.AreEqual("Continue battle · Fight another Combat round in Lossarnach", BattleChoices.DirectTooltip(view, CommandKind.ContinueBattle, data));
            Assert.AreEqual("Hold position · Do not retreat; fight on in Lossarnach", BattleChoices.DirectTooltip(view, CommandKind.HoldBattle, data));
            Assert.AreEqual("Fight in field · Defend Lossarnach outside the stronghold", BattleChoices.DirectTooltip(view, CommandKind.FightFieldBattle, data));
            Assert.AreEqual("Retreat inside · Withdraw into the Lossarnach stronghold", BattleChoices.DirectTooltip(view, CommandKind.RetreatIntoSiege, data));
            Assert.AreEqual("Stay · No figure advances into Lossarnach", BattleChoices.DirectTooltip(view, CommandKind.AdvanceBattle, data));
            view.Battle.Attacker.RegionId = view.Battle.RegionId;
            Assert.AreEqual("Stop battle · End the attack; your army stays in Lossarnach", BattleChoices.DirectTooltip(view, CommandKind.StopBattle, data));
            StringAssert.StartsWith("Evil Knowledge · Click to extend the siege", BattleChoices.DirectTooltip(view, CommandKind.UseEvilKnowledge, data));

            var draft = new TurnDraft();
            draft.Choose(CommandKind.RetreatBattle);
            var footer = new VisualElement();
            new BattleChoices(new VisualElement(), footer, view, draft, action => action(), action => action(), _ => { }, data);
            Assert.AreEqual("Hold position · Do not retreat; fight on in Lossarnach", footer.Q<Button>("action-HoldBattle").tooltip);

            var losses = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            losses.Battle.VoluntaryLosses = true;
            losses.Options[0].Minimum = 0;
            var sacrifice = new TurnDraft();
            sacrifice.Choose(CommandKind.TakeBattleLosses);
            footer = new VisualElement();
            new BattleChoices(new VisualElement(), footer, losses, sacrifice, action => action(), action => action(), _ => { }, data);
            Assert.AreEqual("Keep all · Take no voluntary losses", footer.Q<Button>("battle-keep-all").tooltip);

            var skip = BattlePolicyControl.ToggleTooltip(false, false);
            var ask = BattlePolicyControl.ToggleTooltip(false, true);
            StringAssert.StartsWith("Skip cards this battle · ", skip);
            StringAssert.StartsWith("Ask for cards · ", ask);
        }
    }
}
