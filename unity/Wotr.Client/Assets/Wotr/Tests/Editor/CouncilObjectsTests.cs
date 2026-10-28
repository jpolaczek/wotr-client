using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-270: the Council of Rivendell's objects — the Shadow's token slots, the alternate Guides' responses in the Hunt,
    // the Fellowship Box count, the owner's alternate mark and Art of Disappearing's region on the map.
    public sealed class CouncilObjectsTests
    {
        [Test]
        public void TheTokenSlotsShowOfferedReadySpentAndMissingTokens()
        {
            var view = Shadow();
            view.Decision = new DecisionView { Id = "token", Kind = DecisionKind.CouncilToken };
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseCouncilToken, Enabled = true,
                EligibleIds = { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel } });
            Assert.AreEqual(CouncilTokenState.Offered, CouncilTokenSlots.State(view, ExpansionDraft.CouncilPolitics));
            StringAssert.StartsWith("Click to take Council · Politics", CouncilTokenSlots.Tooltip(view, ExpansionDraft.CouncilPolitics));
            view.Decision = null;
            view.Options.Clear();
            view.CouncilTokens.Add(new CouncilTokenView { Id = ExpansionDraft.CouncilTravel });
            Assert.AreEqual(CouncilTokenState.Missing, CouncilTokenSlots.State(view, ExpansionDraft.CouncilPolitics));
            Assert.AreEqual(CouncilTokenState.Ready, CouncilTokenSlots.State(view, ExpansionDraft.CouncilTravel));
            view.CouncilTokens[0].Spent = true;
            Assert.AreEqual(CouncilTokenState.Spent, CouncilTokenSlots.State(view, ExpansionDraft.CouncilTravel));
            var pool = new ActionPoolView(_ => { }, _ => { });
            var root = new VisualElement();
            pool.Apply(root, view, Side.Shadow, true);
            var politics = root.Q<Button>("pool-" + ExpansionDraft.CouncilPolitics);
            Assert.IsNotNull(politics, "Both slots stand in a Council game.");
            Assert.IsTrue(politics.ClassListContains("council-spent"));
            Assert.IsFalse(politics.enabledSelf);
            Assert.IsTrue(root.Q<Button>("pool-" + ExpansionDraft.CouncilTravel).ClassListContains("council-spent"));
            view.Configuration.Council = false;
            view.CouncilTokens.Clear();
            Assert.IsFalse(CouncilTokenSlots.Shown(view, Side.Shadow), "A game without the Council shows no token slots.");
        }

        [Test]
        // UI-270, UI-360 (EXP-15 D9): the alternate and the base Guides all respond in the Hunt's response row.
        public void TheAlternateAndTheBaseGuidesRespondInTheHuntRow()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Hunt = new HuntView(),
                Decision = new DecisionView { Id = "guide", Kind = DecisionKind.Reaction, Window = HuntGuideResponses.Window },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.RespondEffect, Enabled = true,
                        EligibleIds = { "council.boromir-guide", "character.merry-guide" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "council.boromir-guide", Title = "Separate · Reduce damage", SourceId = "base.character.boromir" },
                            new EffectChoiceView { Id = "character.merry-guide", Title = "Separate · Reduce damage", SourceId = "base.character.merry" }
                        }
                    }
                }
            };
            var offered = HuntGuideResponses.Offered(view);
            Assert.AreEqual(new[] { "council.boromir-guide", "character.merry-guide" }, offered.Select(c => c.Id).ToArray());
            Assert.IsTrue(HuntGuideResponses.InRow(view, "council.boromir-guide"));
            Assert.IsTrue(HuntGuideResponses.InRow(view, "character.merry-guide"));
            StringAssert.StartsWith("Click to use Boromir's response · Separate · Reduce damage", HuntGuideResponses.Tooltip(view, offered[0]));
            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);
            Assert.IsNotNull(responses.Root.Q<Button>("effect-choice-council.boromir-guide"));
            Assert.IsNotNull(responses.Root.Q<Button>("effect-choice-character.merry-guide"));
            Assert.AreEqual(DisplayStyle.Flex, responses.Root.style.display.value);
            view.Hunt = null;
            Assert.IsEmpty(HuntGuideResponses.Offered(view));
        }

        [Test]
        public void TheFreePeoplesPoolCountsTheFellowshipBoxInACouncilGame()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, Configuration = new MatchConfiguration { Lords = true, Council = true },
                Dice = { new DieView { Id = "box", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.FellowshipBox, Face = DieFace.Character } }
            };
            var root = new VisualElement();
            new ActionPoolView(_ => { }, _ => { }).Apply(root, view, Side.FreePeoples, true);
            var box = root.Q<Button>("pool-FellowshipBox");
            Assert.IsNotNull(box);
            Assert.AreEqual("1", box.text);
            StringAssert.StartsWith("Fellowship Box", box.tooltip);
            view.Configuration.Council = false;
            root = new VisualElement();
            new ActionPoolView(_ => { }, _ => { }).Apply(root, view, Side.FreePeoples, true);
            Assert.IsNull(root.Q<Button>("pool-FellowshipBox"), "Without the Council the row keeps its three counts.");
        }

        [TestCase("base.character.boromir", "lords.character.boromir", true)]
        [TestCase("base.character.gandalf-grey", "lords.character.gandalf-grey", true)]
        [TestCase("base.character.gimli", "fate.character.gimli-dwarven-lord", true)]
        [TestCase("base.character.gimli", "fate.character.gimli", false)]
        [TestCase("base.character.gandalf-grey", "base.character.gandalf-white", false)]
        [TestCase("lords.character.elrond", "lords.character.elrond", false)]
        public void OnlyAlternateVersionsWearTheOwnersMark(string figure, string version, bool alternate) =>
            Assert.AreEqual(alternate, CompanionSetupPresentation.Alternate(figure, version));

        [Test]
        public void ArtOfDisappearingChoosesItsRegionOnTheMapAlsoInABattle()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "return", Kind = DecisionKind.EffectChoice, Presentation = "Region" }
            };
            Assert.AreEqual(DecisionHost.Map, DecisionRoutes.Of(view));
            view.Battle = new BattleView();
            Assert.AreEqual(DecisionHost.Map, DecisionRoutes.Of(view));
        }

        // UI-376/UI-377: the political use waits for its nation on the Politics panel; the travel lists the Nazgûl it may
        // move above the token and guides the console until their destination.
        [Test]
        public void TheCouncilTokensUsesStandOnTheirObjects()
        {
            const string first = "base.sauron.nazgul.01";
            const string second = "base.sauron.nazgul.02";
            var view = Shadow();
            view.Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action };
            var option = new ActionOption { Kind = CommandKind.UseCouncilToken, Enabled = true,
                EligibleIds = { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel }, Targets = { "sauron" } };
            foreach (var (id, origin) in new[] { (first, "dol-guldur"), (second, "minas-morgul") })
            {
                option.Travel.Add(new TravelOption { Id = id, Origin = origin, Range = 99, Flight = true, Targets = { "hollin", "moria" } });
                view.Units.Add(new UnitView { Id = id, Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = origin });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseCouncilToken);
            ExpansionDraft.CouncilToken(draft, ExpansionDraft.CouncilPolitics);
            Assert.IsTrue(ExpansionDraft.CouncilPoliticsPending(draft));
            Assert.IsFalse(CouncilTravel.Active(view, draft));
            draft.Change(p => p.NationId = "sauron");
            Assert.IsFalse(ExpansionDraft.CouncilPoliticsPending(draft), "A chosen nation ends the wait.");
            ExpansionDraft.CouncilToken(draft, ExpansionDraft.CouncilTravel);
            Assert.IsTrue(CouncilTravel.Active(view, draft));
            CollectionAssert.AreEqual(new[] { first, second }, CouncilTravel.Figures(view, draft));
            StringAssert.StartsWith("Council token · Click the Nazgûl to move", CouncilTravel.Hint(view, draft));
        }

        // UI-379 (owner; T26: any or all Nazgûl, each to its own destination, in one action): a group of Nazgûl from two
        // regions is chosen and released by clicks, planned to one destination as one group per origin, nothing is sent
        // until the last figure is planned, and a planned Nazgûl clicked again is chosen again.
        [Test]
        public void TheCouncilTokensTravelPlansGroupsUntilFinish()
        {
            const string first = "base.sauron.nazgul.01";
            const string second = "base.sauron.nazgul.02";
            const string third = "base.sauron.nazgul.03";
            var view = Shadow();
            view.Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action };
            var option = new ActionOption { Kind = CommandKind.UseCouncilToken, Enabled = true, EligibleIds = { ExpansionDraft.CouncilTravel } };
            foreach (var (id, origin) in new[] { (first, "dol-guldur"), (second, "minas-morgul"), (third, "dol-guldur") })
            {
                option.Travel.Add(new TravelOption { Id = id, Origin = origin, Range = 99, Flight = true, Targets = { "hollin", "moria", "dol-guldur" } });
                view.Units.Add(new UnitView { Id = id, Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = origin, Nation = "sauron" });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseCouncilToken);
            ExpansionDraft.CouncilToken(draft, ExpansionDraft.CouncilTravel);
            Assert.IsTrue(CouncilTravel.Choose(view, draft, first));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, second));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, third));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, third), "A second click releases a chosen Nazgûl.");
            CollectionAssert.AreEqual(new[] { first, second }, CouncilTravel.Chosen(draft));
            Assert.IsEmpty(draft.Payload.CompanionMoves, "A chosen group is local, never part of the command.");
            StringAssert.StartsWith("Nazgûl ×2 · Click a highlighted region to move there", CouncilTravel.Hint(view, draft));
            CollectionAssert.AreEquivalent(new[] { "hollin", "moria" }, CouncilTravel.Targets(view, draft),
                "Neither Nazgûl's own region is a destination of the group.");
            Assert.IsTrue(CouncilTravel.Move(view, draft, "hollin", BoardPresentation.Load()));
            Assert.IsEmpty(CouncilTravel.Chosen(draft));
            Assert.AreEqual(2, draft.Payload.CompanionMoves.Count, "One planned group per origin.");
            CollectionAssert.AreEquivalent(new[] { "dol-guldur", "minas-morgul" }, draft.Payload.CompanionMoves.Select(m => m.Route[0]));
            Assert.IsTrue(draft.Payload.CompanionMoves.All(m => m.Route.Count == 2 && m.Route[1] == "hollin"));
            Assert.AreEqual(2, NazgulFlightDraft.Planned(view, draft).Count(p => p.RegionId == "hollin"), "Their ghost stands there with its count.");
            Assert.IsFalse(CouncilTravel.Complete(view, draft), "A third Nazgûl may still move.");
            Assert.IsTrue(draft.Ready(DraftOptions.For(view, draft)), "Finish may send the planned moves.");
            StringAssert.Contains("click ✓ to finish", CouncilTravel.Hint(view, draft));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, second));
            Assert.IsFalse(CouncilTravel.Planned(draft, second), "A planned Nazgûl clicked again gives up its destination.");
            CollectionAssert.AreEqual(new[] { second }, CouncilTravel.Chosen(draft));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, third));
            Assert.IsFalse(CouncilTravel.Move(view, draft, "minas-morgul", BoardPresentation.Load()), "Not a lit destination.");
            Assert.IsTrue(CouncilTravel.Move(view, draft, "moria", BoardPresentation.Load()));
            Assert.IsTrue(CouncilTravel.Complete(view, draft), "Every Nazgûl planned: the move goes at once.");
            StringAssert.Contains("Nazgûl ×3", CouncilTravel.ConfirmTooltip(view, draft));
            view.DecisionOwner = Side.FreePeoples;
            Assert.IsFalse(CouncilTravel.Active(view, draft), "Only the player's own action.");
            Assert.AreEqual("", CouncilTravel.Hint(view, draft));
        }

        private static PlayerView Shadow() => new PlayerView
        {
            You = Side.Shadow, DecisionOwner = Side.Shadow,
            Configuration = new MatchConfiguration { Lords = true, Council = true }
        };
    }
}
