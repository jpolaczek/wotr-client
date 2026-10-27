using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-344 (owner's decisions EXP-15 D1-D5) on a mounted board: the opponent's expansion actions play on their objects
    // before the release, in both motion settings — a corruption at the king's capital (the tile's back, the meter's
    // hatched pip, the dark marker, the revealed tiles), a removal fading the marker, an awakening replacing the Leader,
    // the opponent's token chips, Dismay rerolling the player's Ruler die, Sméagol leaving the Fellowship. The Characters
    // panel never opens by itself. Frames go to .artifacts/screenshots (ui344-*).
    public sealed class OpponentExpansionPresentationTests
    {
        private const string Dain = "kings.sovereign.dain";
        private const string One = "base.hunt.standard.1.0.1";
        private const string Two = "base.hunt.standard.2.0.1";

        [UnityTest]
        public IEnumerator AnOpponentsCorruptionPlaysAtTheKingsCapital([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced);
            Kings(stage.Before);
            stage.Before.Sovereigns.Add(King(SovereignStatus.Initial, 1));
            Leader(stage);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Sovereigns[0] = King(SovereignStatus.Corrupted, 0);
            latest.Units.RemoveAll(u => u.Id == Dain);
            var attached = Sovereign(stage.Paid(stage.Step(1), DieFace.Eye), "CorruptionAdded", King(SovereignStatus.Initial, 2));
            var corrupted = Sovereign(stage.Step(4), "SovereignCorrupted", King(SovereignStatus.Corrupted, 0));
            stage.Play(latest, "grey-havens", attached, Reveal(stage, 2, One), Reveal(stage, 3, Two), corrupted);
            if (!reduced)
                yield return stage.Until(() => stage.Board.Root.Q(className: SovereignPresenter.TileBackClass) != null);
            yield return stage.Until(() => stage.Board.Root.Q(className: "presentation-sovereign-meter") != null);
            var meter = stage.Board.Root.Q(className: "presentation-sovereign-meter");
            Assert.AreEqual(4, meter.childCount, "A pip per point of resistance.");
            Assert.AreEqual(2, meter.Query(className: "hidden").ToList().Count, "Two hidden tiles; never a value.");
            LayoutTests.Save(stage.Texture, "ui344-tile-" + (reduced ? "quiet" : "motion") + ".png");
            yield return stage.Until(() => stage.Board.Map.SovereignMarkers.Find(Dain) != null);
            Assert.IsTrue(stage.Playback.Holding, "The marker emerges before the release.");
            yield return stage.Until(() => stage.Board.Root.Q(className: "presentation-revealed-tiles")?.childCount == 2);
            LayoutTests.Save(stage.Texture, "ui344-corruption-" + (reduced ? "quiet" : "motion") + ".png");
            Assert.AreEqual(DisplayStyle.None, stage.Board.Characters.Root.resolvedStyle.display, "The Characters panel never opens by itself.");
            yield return stage.Released();
            Assert.IsNotNull(stage.Board.Map.SovereignMarkers.Find(Dain));
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator AnOpponentsRemovalFadesTheMarker([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced, Side.Shadow);
            Kings(stage.Before);
            stage.Before.Sovereigns.Add(King(SovereignStatus.Corrupted, 0));
            yield return stage.Ready();
            var marker = stage.Board.Map.SovereignMarkers.Find(Dain);
            Assert.IsNotNull(marker);
            var latest = stage.Latest();
            latest.Sovereigns[0] = King(SovereignStatus.Removed, 0);
            stage.Play(latest, "grey-havens", Sovereign(stage.Paid(stage.Step(1), DieFace.Character), "SovereignRemoved", latest.Sovereigns[0]));
            yield return stage.Until(() => stage.Lit.Any(lit => lit.worldBound.Overlaps(marker.worldBound)));
            yield return stage.Until(() => stage.Board.Map.SovereignMarkers.Find(Dain) == null);
            Assert.IsTrue(stage.Playback.Holding, "The marker fades before the release.");
            yield return stage.Released();
            Assert.IsNull(stage.Board.Map.SovereignMarkers.Find(Dain));
        }

        [UnityTest]
        public IEnumerator AnAwakeningReplacesTheLeaderByTheKingInPlace([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced, Side.Shadow);
            Kings(stage.Before);
            stage.Before.Sovereigns.Add(King(SovereignStatus.Initial, 0));
            Leader(stage);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Units.RemoveAll(u => u.Id == Dain);
            var king = PresentedBoardStage.Character(latest, Dain, "kings.character.dain.awakened", Side.FreePeoples, "erebor");
            latest.Sovereigns[0] = King(SovereignStatus.Awakened, 0);
            var step = stage.Paid(stage.Step(1), DieFace.Muster);
            step.Facts.Figures.Add(new ActionFigureChange
            {
                Before = new ActionParticipant { Id = Dain, Side = Side.FreePeoples, NationId = "dwarves", UnitKind = UnitKind.Leader },
                After = PresentedBoardStage.Figure(king), From = "erebor", To = "erebor"
            });
            step.Facts.Sovereigns.Add(latest.Sovereigns[0]);
            stage.Play(latest, "grey-havens", step);
            yield return stage.Until(() => stage.Characters("erebor", Side.FreePeoples) == 1);
            Assert.IsTrue(stage.Playback.Holding, "The king stands before the release.");
            Assert.AreEqual(3, stage.Board.Presentation.Armies.Where(a => a.RegionId == "erebor" && a.Side == Side.FreePeoples).Sum(a => a.Total),
                "His Leader gave way to him; no second figure.");
            LayoutTests.Save(stage.Texture, "ui344-awakening-" + (reduced ? "quiet" : "motion") + ".png");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator TheOpponentsRulerTokenChangesOnItsChip([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced, Side.Shadow);
            Kings(stage.Before);
            stage.Before.RulerTokens.Add(new RulerTokenView { Id = "kings.token.dain", SovereignId = Dain, Location = "free-peoples" });
            yield return stage.Ready();
            var chip = stage.Board.TopBar.Opponent.Root.Q("opponent-ruler");
            Assert.AreEqual(DisplayStyle.Flex, chip.style.display.value, "The Free Peoples' Ruler tokens have their chip.");
            Assert.AreEqual("1", Value(chip));
            var latest = stage.Latest();
            latest.RulerTokens[0].Location = "removed";
            var step = stage.Paid(stage.Step(1), DieFace.Character);
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.RulerToken, Id = "kings.token.dain", Owner = Side.FreePeoples });
            step.Facts.Resources.Add(new ActionResourceChange { Kind = ActionCostKind.RulerToken, Id = "kings.token.dain", From = "free-peoples", To = "removed" });
            stage.Play(latest, null, step);
            yield return stage.Until(() => stage.Lit.Contains(chip));
            yield return stage.Until(() => Value(chip) == "0");
            Assert.IsTrue(stage.Playback.Holding, "The chip changes in the presentation, not at the release.");
            yield return stage.Released();
            Assert.AreEqual("0", Value(chip));
        }

        [UnityTest]
        public IEnumerator DismayRerollsMyRulerDieAsTheOpponentsBeat([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced);
            Kings(stage.Before);
            stage.Before.Configuration.Lords = true;
            stage.Before.Dice.Add(new DieView { Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Face = DieFace.Character, Location = DieLocation.Available });
            stage.Before.RulerTokens.Add(new RulerTokenView { Id = "kings.token.theoden", SovereignId = "kings.sovereign.theoden", Location = "dismay" });
            yield return stage.Ready();
            var chip = stage.Board.TopBar.Opponent.Root.Q("opponent-dismay");
            Assert.AreEqual("1", Value(chip), "The Shadow's Dismay tokens have their chip.");
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == "ruler").Face = DieFace.Muster;
            latest.RulerTokens[0].Location = "removed";
            var step = stage.Paid(stage.Step(1), DieFace.Army);
            step.Kind = "DismayUsed";
            step.Facts.Dice.Add(new ActionDieChange { Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Before = DieFace.Character,
                After = DieFace.Muster, From = DieLocation.Available, To = DieLocation.Available });
            step.Facts.Resources.Add(new ActionResourceChange { Kind = ActionCostKind.RulerToken, Id = "kings.token.theoden", From = "dismay", To = "removed" });
            stage.Play(latest, null, step);
            var die = stage.Board.OwnDice.Q<Button>("ruler");
            yield return stage.Until(() => stage.Lit.Contains(die));
            yield return stage.Until(() => stage.Board.Root.Q(className: "presentation-used-face") != null);
            Assert.IsTrue(stage.Playback.Holding, "My die's reroll is the opponent's beat, before the release.");
            LayoutTests.Save(stage.Texture, "ui344-dismay-" + (reduced ? "quiet" : "motion") + ".png");
            yield return stage.Until(() => Value(chip) == "0");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator SmeagolLeavingTheFellowshipLeavesItsRoster([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced, Side.Shadow);
            stage.Before.Fellowship.CompanionIdentities.Add("smeagol");
            stage.Before.Fellowship.CompanionCount++;
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Fellowship.CompanionIdentities.Remove("smeagol");
            latest.Fellowship.CompanionCount--;
            var step = stage.Step(1);
            step.Kind = "CompanionDiscarded";
            step.Facts.Figures.Add(new ActionFigureChange
            {
                After = new ActionParticipant { Id = "lords.character.smeagol", Side = Side.FreePeoples, DefinitionId = "lords.character.smeagol" },
                From = "fellowship", To = "removed"
            });
            stage.Play(latest, null, step);
            yield return stage.Until(() => stage.Lit.Any(lit => lit.name == "fellowship-portrait-smeagol"));
            yield return stage.Until(() => stage.Board.Fellowship.Root.Q("fellowship-portrait-smeagol") == null);
            Assert.IsTrue(stage.Playback.Holding, "He leaves the roster before the release.");
            yield return stage.Released();
        }

        private static void Kings(PlayerView view) => view.Configuration.Kings = true;

        // Dáin's Leader in Erebor's army, with the sovereign's id.
        private static void Leader(PresentedBoardStage stage)
        {
            var id = stage.Put("dwarves", UnitKind.Leader, "erebor", 1).Single();
            stage.Before.Units.Single(u => u.Id == id).Id = Dain;
            stage.Put("dwarves", UnitKind.Regular, "erebor", 2);
        }

        private static SovereignView King(SovereignStatus status, int tiles)
        {
            var king = new SovereignView
            {
                Id = Dain, Title = "Dáin Ironfoot", DefinitionId = Dain, Nation = "dwarves", RegionId = "erebor", Status = status, Resistance = 4
            };
            for (var index = 0; index < tiles; index++)
                king.Tiles.Add(new CorruptionTileView { Id = Dain + ":tile:" + index });
            return king;
        }

        private static ActionLogStep Sovereign(ActionLogStep step, string kind, SovereignView king)
        {
            step.Kind = kind;
            step.Facts.Sovereigns.Add(king);
            return step;
        }

        private static ActionLogStep Reveal(PresentedBoardStage stage, int order, string tile)
        {
            var step = stage.Step(order);
            step.Kind = "CorruptionTileRevealed";
            step.Actor = null;
            step.Facts.Targets.Add(tile);
            return step;
        }

        private static string Value(VisualElement chip) => chip.Q<Label>(className: "summary-value").text;
    }
}
