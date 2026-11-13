using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class InGameContractTests
    {
        [Test]
        public void HuntDiceReductionRoundTripsAndOlderResponsesDefaultToNoReduction()
        {
            var json = "{\"hunt\":{\"diceReduction\":1,\"rerollBonus\":1}," +
                "\"actionBoundary\":{\"version\":12,\"fellowshipMove\":{}}}";
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(
                JsonConvert.DeserializeObject<PlayerView>(json)));
            Assert.AreEqual(12, copied.ActionBoundary.Version);
            Assert.AreEqual(1, copied.Hunt.DiceReduction);
            Assert.AreEqual(1, copied.Hunt.RerollBonus);
            Assert.AreEqual(0, JsonConvert.DeserializeObject<HuntView>("{}").DiceReduction);
        }

        [Test]
        public void DirectBattleReturnRoundTripsWithoutExposingTheOpposingCard()
        {
            const string json = "{\"battle\":{\"stage\":\"DefenderCard\",\"round\":1," +
                "\"cardsRevealed\":false,\"attacker\":{\"cardCommitted\":true," +
                "\"cardBack\":\"strategy\",\"cardId\":null}}," +
                "\"actionBoundary\":{\"version\":12,\"battle\":{\"completesPaidAction\":true}}}";
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(
                JsonConvert.DeserializeObject<PlayerView>(json)));

            Assert.AreEqual(12, copied.ActionBoundary.Version);
            Assert.IsTrue(copied.ActionBoundary.Battle.CompletesPaidAction);
            Assert.AreEqual("DefenderCard", copied.Battle.Stage);
            Assert.AreEqual("strategy", copied.Battle.Attacker.CardBack);
            Assert.IsNull(copied.Battle.Attacker.CardId);
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
        }

        [TestCase(Side.FreePeoples)]
        [TestCase(Side.Shadow)]
        public void CorruptionDrawMemoryStaysSeparateFromTheCurrentlySelectableTile(Side side)
        {
            var first = "{\"id\":\"first\",\"tileId\":\"first\",\"value\":0}";
            var latest = "{\"id\":\"latest\",\"tileId\":\"latest\",\"value\":1}";
            var json = "{\"corruptionAttempt\":{\"sequential\":true,\"drawnCount\":2,\"drawLimit\":3," +
                "\"ownDrawnTiles\":[" + (side == Side.Shadow ? first + "," + latest : "") + "]," +
                "\"ownChoices\":[" + (side == Side.Shadow ? latest : "") + "]}}";
            var view = JsonConvert.DeserializeObject<PlayerView>(json);
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            Assert.AreEqual(2, copied.CorruptionAttempt.DrawnCount);
            Assert.AreEqual(side == Side.Shadow ? 2 : 0, copied.CorruptionAttempt.OwnDrawnTiles.Count);
            Assert.AreEqual(side == Side.Shadow ? 1 : 0, copied.CorruptionAttempt.OwnChoices.Count);
            if (side == Side.Shadow)
                Assert.AreEqual("latest", copied.CorruptionAttempt.OwnChoices.Single().TileId);
            else
                StringAssert.DoesNotContain("latest", JsonConvert.SerializeObject(copied));
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
            Assert.IsEmpty(JsonConvert.DeserializeObject<CorruptionAttemptView>("{\"drawnCount\":2}").OwnDrawnTiles);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KingsFactsRetainJointUnknownFacesWithoutAssigningCoveredTiles(bool tokenUsed)
        {
            var json = "{\"actionBoundary\":{\"version\":9,\"kings\":{" +
                "\"palantirUsedThisTurn\":true,\"dismayDeclinedThisTurn\":true," +
                "\"rulerTokenUsedThisTurn\":" + (tokenUsed ? "true" : "false") + "," +
                "\"rulerDiscardOrder\":[\"Shadow\",\"FreePeoples\"]," +
                "\"unassignedHuntTiles\":[{\"source\":\"base\",\"pool\":\"standard\",\"value\":\"1\",\"count\":3}]} ," +
                "\"huntTiles\":[{\"location\":\"pool\",\"hiddenCount\":2},{\"location\":\"denethor\",\"hiddenCount\":1}]}," +
                "\"sovereigns\":[{\"id\":\"denethor\",\"tiles\":[{\"id\":\"denethor:tile:0\",\"value\":null}]}]}";
            var view = JsonConvert.DeserializeObject<PlayerView>(json);
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            var facts = copied.ActionBoundary.Kings;
            Assert.AreEqual(9, copied.ActionBoundary.Version);
            Assert.IsTrue(facts.PalantirUsedThisTurn);
            Assert.IsTrue(facts.DismayDeclinedThisTurn);
            Assert.AreEqual(tokenUsed, facts.RulerTokenUsedThisTurn);
            CollectionAssert.AreEqual(new[] { Side.Shadow, Side.FreePeoples }, facts.RulerDiscardOrder);
            Assert.AreEqual(3, facts.UnassignedHuntTiles.Single().Count);
            Assert.AreEqual(3, copied.ActionBoundary.HuntTiles.Sum(z => z.HiddenCount));
            Assert.IsTrue(copied.ActionBoundary.HuntTiles.All(z => z.Tiles.Count == 0));
            Assert.IsNull(copied.Sovereigns.Single().Tiles.Single().Value);
            Assert.IsEmpty(copied.Sovereigns.Single().Tiles.Single().TileId);
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
            Assert.IsNull(JsonConvert.DeserializeObject<ActionBoundaryView>("{\"version\":8}").Kings);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LordsFactsPreservePublicResourcesWithoutGrantingAChoice(bool fellowshipMove)
        {
            var json = "{\"actionBoundary\":{\"version\":8,\"lords\":{" +
                "\"witchKingReplaced\":true,\"smeagolLeft\":true,\"loreOfferedThisTurn\":true," +
                "\"rolledDiceCounts\":{\"FreePeoples\":6,\"Shadow\":9}," +
                "\"specialDieChoiceOrder\":[\"Shadow\",\"FreePeoples\"]," +
                "\"paidWillOfWestIds\":[\"paid-die\"]," +
                "\"completedActionWasFellowshipMove\":" + (fellowshipMove ? "true" : "false") + "," +
                "\"council\":{\"homelandsChosen\":true,\"fellowshipMovedThisTurn\":true,\"fellowshipBoxDieId\":\"paid-die\"}}}}";
            var view = JsonConvert.DeserializeObject<PlayerView>(json);
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            var facts = copied.ActionBoundary.Lords;
            Assert.AreEqual(8, copied.ActionBoundary.Version);
            Assert.AreEqual(9, facts.RolledDiceCounts[Side.Shadow]);
            CollectionAssert.AreEqual(new[] { Side.Shadow, Side.FreePeoples }, facts.SpecialDieChoiceOrder);
            CollectionAssert.AreEqual(new[] { "paid-die" }, facts.PaidWillOfWestIds);
            Assert.IsTrue(facts.WitchKingReplaced);
            Assert.IsTrue(facts.SmeagolLeft);
            Assert.IsTrue(facts.LoreOfferedThisTurn);
            Assert.IsTrue(facts.Council.HomelandsChosen);
            Assert.IsTrue(facts.Council.FellowshipMovedThisTurn);
            Assert.AreEqual("paid-die", facts.Council.FellowshipBoxDieId);
            Assert.AreEqual(fellowshipMove, facts.CompletedActionWasFellowshipMove);
            Assert.IsEmpty(copied.OwnCompanionVersions);
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
            Assert.IsNull(JsonConvert.DeserializeObject<ActionBoundaryView>("{\"version\":7}").Lords);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeclarationFactsRoundTripWithoutGrantingAChoiceOrCopyingAReturnToken(bool additionalHunt)
        {
            var facts = new FellowshipDeclarationView
            {
                Route = new System.Collections.Generic.List<string> { "moria", "hollin" },
                RemainingStrongholds = additionalHunt
                    ? new System.Collections.Generic.List<string> { "moria" } : new System.Collections.Generic.List<string>(),
                GuideWindowsOffered = new System.Collections.Generic.List<string> { "base.character.merry" },
                CasualtyConsequencesPending = additionalHunt
            };
            var view = new PlayerView
            {
                Phase = GamePhase.Fellowship,
                ActionBoundary = new ActionBoundaryView { FellowshipDeclaredThisTurn = true, FellowshipDeclaration = facts }
            };
            var json = JsonConvert.SerializeObject(view);
            var copied = JsonConvert.DeserializeObject<PlayerView>(json);
            Assert.AreEqual(57, copied.ActionBoundary.Version);
            CollectionAssert.AreEqual(facts.Route, copied.ActionBoundary.FellowshipDeclaration.Route);
            CollectionAssert.AreEqual(facts.RemainingStrongholds, copied.ActionBoundary.FellowshipDeclaration.RemainingStrongholds);
            CollectionAssert.AreEqual(facts.GuideWindowsOffered, copied.ActionBoundary.FellowshipDeclaration.GuideWindowsOffered);
            Assert.AreEqual(additionalHunt, copied.ActionBoundary.FellowshipDeclaration.CasualtyConsequencesPending);
            Assert.IsTrue(copied.ActionBoundary.FellowshipDeclaredThisTurn);
            Assert.IsEmpty(copied.ActionBoundary.PaidDieIds);
            Assert.IsNull(copied.ActionBoundary.FellowshipMove);
            Assert.IsNull(copied.ActionBoundary.CardHunt);
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
            StringAssert.DoesNotContain("ReturnDecision", json);
            StringAssert.DoesNotContain("NextStep", json);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HuntContextsReadInheritedProgressWithoutGrantingADecision(bool card)
        {
            var name = card ? "cardHunt" : "fellowshipMove";
            var json = "{\"actionBoundary\":{\"version\":6,\"paidDieIds\":[\"paid-die\"],\"" + name + "\":{" +
                "\"cardId\":\"base.sh.character.05\",\"cardCost\":\"Event\",\"palantirDrawPending\":true," +
                "\"guideWindowsOffered\":[\"base.character.merry\"],\"casualtyConsequencesPending\":true," +
                "\"departureRegionId\":\"moria\",\"departureProgress\":2," +
                "\"departingCompanions\":[\"base.character.merry\"],\"placementStarted\":true}}}";
            var view = JsonConvert.DeserializeObject<PlayerView>(json);
            var copied = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            HuntProgressView progress = card ? (HuntProgressView)copied.ActionBoundary.CardHunt : copied.ActionBoundary.FellowshipMove;
            Assert.AreEqual(6, copied.ActionBoundary.Version);
            CollectionAssert.AreEqual(new[] { "base.character.merry" }, progress.GuideWindowsOffered);
            CollectionAssert.AreEqual(new[] { "base.character.merry" }, progress.DepartingCompanions);
            Assert.IsTrue(progress.CasualtyConsequencesPending);
            Assert.IsTrue(progress.PlacementStarted);
            Assert.AreEqual("moria", progress.DepartureRegionId);
            Assert.AreEqual(2, progress.DepartureProgress);
            if (card)
            {
                Assert.AreEqual("base.sh.character.05", copied.ActionBoundary.CardHunt.CardId);
                Assert.AreEqual(DieFace.Event, copied.ActionBoundary.CardHunt.CardCost);
                Assert.IsTrue(copied.ActionBoundary.CardHunt.PalantirDrawPending);
                Assert.IsNull(copied.ActionBoundary.FellowshipMove);
            }
            Assert.IsNull(copied.Decision);
            Assert.IsEmpty(copied.Options);
        }

        [Test]
        public void OlderProjectionHasNeutralOptionalDescriptions()
        {
            var view = JsonConvert.DeserializeObject<PlayerView>("{\"effects\":[{\"sourceId\":\"old-card\"}],\"sovereigns\":[{}]}");
            Assert.IsEmpty(view.Availability);
            Assert.IsEmpty(view.MilitaryVictory);
            Assert.IsEmpty(view.Effects[0].Targets);
            Assert.IsEmpty(view.Effects[0].Description);
            Assert.IsNull(view.Sovereigns[0].Corruption);
            var missing = JsonConvert.DeserializeObject<ActionAvailabilityView>("{}");
            Assert.IsFalse(missing.ConditionsMet);
            Assert.IsFalse(missing.WindowOpen);
            Assert.IsFalse(missing.CanAct);
        }

        [Test]
        public void ReadsServerDescriptionsWithoutInferringPermissionOrHiddenCorruption()
        {
            var path = Path.Combine(Application.dataPath, "Wotr/Tests/Editor/InGameWire.json");
            var view = JsonConvert.DeserializeObject<PlayerView>(File.ReadAllText(path));
            Assert.AreEqual(Side.FreePeoples, view.You);
            var awakening = view.Availability.Single(a => a.SubjectId == "kings.sovereign.dain" && a.ActionId == "awaken");
            Assert.IsTrue(awakening.ConditionsMet);
            Assert.IsFalse(awakening.WindowOpen);
            Assert.IsFalse(awakening.CanAct);
            Assert.IsNotEmpty(awakening.EligibleDice);
            Assert.IsNotEmpty(awakening.Source);
            Assert.IsTrue(awakening.Conditions.All(c => c.Met));
            Assert.IsNull(view.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Corruption);
            foreach (var victory in view.MilitaryVictory)
                Assert.AreEqual(victory.Points, victory.Regions.Sum(r => r.Points));
            Assert.AreEqual(10, view.MilitaryVictory.Single(v => v.Side == Side.Shadow).Threshold);
            Assert.IsTrue(view.Effects.Any(e => e.SourceKind == "Card" && e.Description.Length > 0));
            Assert.IsNull(view.Winner);
        }
    }
}
