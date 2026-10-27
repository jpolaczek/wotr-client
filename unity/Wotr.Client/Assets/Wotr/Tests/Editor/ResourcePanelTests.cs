using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ResourcePanelTests
    {
        // UI-276 (EXP-15 D7, D20): Dismay is made on the glowing Ruler dice — a die's click spends the first Dismay token,
        // since which one makes no difference — and keeping the tokens is the console's Pass; no panel hosts it.
        [Test]
        public void DismayIsTheRulerDiesClickAndKeepingIsThePass()
        {
            var view = View(DecisionKind.Dismay);
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseDismay, Enabled = true, EligibleIds = { "kings.token.theoden", "kings.token.dain" }, EligibleDice = { "ruler" }
            });
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclineDismay, Enabled = true });
            Assert.IsTrue(ResourcePresentation.PoolDecision(view));
            Assert.AreEqual(DecisionHost.Dice, DecisionRoutes.Of(view));
            StringAssert.StartsWith("Click a glowing Ruler die to spend a Dismay token", ResourcePresentation.PoolHint(view));
            Assert.AreEqual("kings.token.theoden", DismayChoice.Token(view, "ruler"));
            Assert.AreEqual("", DismayChoice.Token(view, "other"));
            var draft = new TurnDraft();
            DismayChoice.Choose(draft, "ruler", DismayChoice.Token(view, "ruler"));
            Assert.AreEqual(CommandKind.UseDismay, draft.Kind);
            Assert.AreEqual("ruler", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { "kings.token.theoden" }, draft.Payload.Ids);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            var pass = OptionalDecline.Option(view, new TurnDraft());
            Assert.AreEqual(CommandKind.DeclineDismay, pass.Command);
            Assert.AreEqual("Pass · Keep your Dismay tokens", pass.Tooltip);
            Assert.AreEqual("Or Pass to keep your Dismay tokens.", OptionalDecline.Hint(view, new TurnDraft()));
            Assert.IsFalse(DraftOptions.CanSelectToken(view, "kings.token.theoden"), "The die's click picks the token.");
            view.You = Side.FreePeoples;
            Assert.AreEqual("", DismayChoice.Token(view, "ruler"), "The opponent's Dismay lights nothing.");
        }

        // UI-264: the special-die pick and the Ruler discard are made on the pool's lit dice.
        [TestCase(DecisionKind.SpecialDieChoice)]
        [TestCase(DecisionKind.RulerDiscard)]
        public void PoolDecisionsAreMadeOnThePoolDice(DecisionKind kind)
        {
            var view = View(kind);
            Assert.IsTrue(ResourcePresentation.PoolDecision(view));
            // UI-273: the Ruler discard's hint says the click discards and when the die returns.
            StringAssert.StartsWith(kind == DecisionKind.RulerDiscard ? "Click a glowing die to discard it" : "Click a highlighted die",
                ResourcePresentation.PoolHint(view));
            view.You = Side.Shadow;
            Assert.IsFalse(ResourcePresentation.PoolDecision(view), "The opponent's pick lights nothing.");
            Assert.AreEqual("", ResourcePresentation.PoolHint(view));
        }

        [Test]
        public void RuneUsesServerConditionalRecoveryInsteadOfPromisingImmediateRemoval()
        {
            var die = new DieView
            {
                Id = "keeper", Owner = Side.FreePeoples, Source = "lords.die.vilya",
                SourceTitle = "Elrond", Face = DieFace.Army, RemoveSymbol = true,
                RecoveryDescription = "Removed at Recover only if Gandalf the White is then in play."
            };
            // UI-97: one short line; the conditional recovery stays in the Why? and help panels.
            Assert.AreEqual("Vilya · Army · ★ Remove", ExpansionLabels.DieDetail(die));
            StringAssert.DoesNotContain(die.RecoveryDescription, ExpansionLabels.DieDetail(die));
            StringAssert.DoesNotContain("Removed next recovery after use", ExpansionLabels.DieDetail(die));
        }

        [Test]
        public void UnavailableResourceExplainsCostLimitAndTimingWithoutGrantingPermission()
        {
            var view = View(DecisionKind.Action);
            view.Availability.Add(new ActionAvailabilityView
            {
                SubjectId = "ring", Title = "Change a die", Cost = "Transfer this Ring", Timing = "During your action",
                Conditions = { new AvailabilityConditionView { Code = "limit", Description = "A Ring was already used this turn." } }
            });
            var description = ResourcePresentation.Explain(view, "ring");
            StringAssert.Contains("Transfer this Ring", description);
            StringAssert.Contains("already used", description);
            StringAssert.Contains("During your action", description);
            Assert.IsFalse(ExpansionActions.Ring(view, new TurnDraft(), "ring"));
            Assert.IsEmpty(ResourcePresentation.Explain(view, "unknown"));
        }

        [Test]
        public void TokenDropRetainsSpecificRulerWithoutChoosingItsBoundAbility()
        {
            var view = View(DecisionKind.Action);
            view.RulerTokenActions.Add(new RulerTokenActionOption
            {
                TokenId = "token", AbilityId = "ability",
                Action = new ActionOption { Kind = CommandKind.Recruit, EligibleDice = { "ruler" } }
            });
            var draft = new TurnDraft();
            Assert.IsFalse(ExpansionActions.RulerToken(view, draft, "token", "standard"));
            Assert.IsTrue(ExpansionActions.RulerToken(view, draft, "token", "ruler"));
            Assert.AreEqual("token", draft.Payload.RulerTokenId);
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.IsNull(draft.Kind);
            Assert.IsEmpty(draft.Payload.AbilityId);
            view.RulerTokenActions.Clear();
            Assert.IsFalse(ExpansionActions.RulerToken(view, draft, "token", "replacement"));
            Assert.AreEqual("ruler", draft.Payload.DieId);
        }

        [Test]
        public void CouncilCanEnterAChieftainWithoutItsPrintedUseOrAnAutomaticallyChosenDie()
        {
            var view = CouncilView(false);
            var draft = CouncilDraft();
            Assert.IsTrue(ResourcePresentation.CanSelectCouncil(view, ExpansionDraft.CouncilPolitics));
            var entry = ComponentActions.For(view, draft).Single();
            Assert.AreEqual(CommandKind.EnterCharacter, entry.Kind);
            Assert.IsTrue(entry.Enabled);
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            Assert.AreEqual("kings.character.black-serpent", draft.Payload.CharacterId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
            Assert.IsEmpty(draft.Payload.DieId, "Selecting a Chieftain must not select an equivalent die.");
            Assert.IsFalse(draft.Ready(DraftOptions.For(view, draft)));

            view.CouncilTokens.Add(new CouncilTokenView { Id = ExpansionDraft.CouncilPolitics });
            var pool = new ActionPoolView(_ => { }, _ => { });
            var root = new VisualElement();
            pool.Apply(root, view, Side.Shadow, true);
            var token = root.Q<Button>("pool-" + ExpansionDraft.CouncilPolitics);
            Assert.IsTrue(token.ClassListContains("legal-target"),
                "The physical Council token highlights a legal Chieftain entry without its printed use.");

            view.Options.Clear();
            pool.Apply(root, view, Side.Shadow, true);
            Assert.AreSame(token, root.Q<Button>(token.name), "An option-only refresh preserves the physical control.");
            Assert.IsFalse(token.ClassListContains("legal-target"), "An expired entry loses its target highlight.");
        }

        // UI-347 (EXP-15 D13): a medallion's click starts its printed use at once; when a Chieftain can enter with the token,
        // the first click marks it (his row lights) and a click on the marked token starts the printed use; during his
        // entry the token is its cost. No Dice & tokens panel step remains.
        [Test]
        public void ACouncilMedallionStartsItsUseOrMarksItForAChieftain()
        {
            var view = CouncilView(true);
            Assert.AreEqual(CouncilClick.Mark, ResourcePresentation.Clicked(view, new TurnDraft(), ExpansionDraft.CouncilPolitics));
            var draft = CouncilDraft();
            Assert.AreEqual(CouncilClick.Use, ResourcePresentation.Clicked(view, draft, ExpansionDraft.CouncilPolitics));
            var hint = ResourcePresentation.CouncilHint(view, draft);
            StringAssert.StartsWith("Click a lit Chieftain's row", hint);
            StringAssert.Contains("click the token again for its printed use (Council · Politics)", hint);
            var entry = ComponentActions.For(view, draft).Single(c => c.Kind == CommandKind.EnterCharacter);
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            Assert.AreEqual(CouncilClick.EntryCost, ResourcePresentation.Clicked(view, draft, ExpansionDraft.CouncilPolitics));

            var plain = CouncilView(true);
            plain.Options.RemoveAll(o => o.Kind == CommandKind.EnterCharacter);
            Assert.AreEqual(CouncilClick.Use, ResourcePresentation.Clicked(plain, new TurnDraft(), ExpansionDraft.CouncilPolitics));
            plain.You = Side.FreePeoples;
            Assert.AreEqual(CouncilClick.None, ResourcePresentation.Clicked(plain, new TurnDraft(), ExpansionDraft.CouncilPolitics));
        }

        // UI-347 (D13): an entry that requires the only token it may use takes it, so no button asks for it.
        [Test]
        public void AChieftainsRequiredOnlyTokenIsTakenWithoutAButton()
        {
            var view = CouncilView(false);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            var entry = ComponentActions.For(view, draft).Single(c => c.Kind == CommandKind.EnterCharacter);
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
        }

        // UI-276 (EXP-15 D19): the Ruler tokens are graphics in the pool with their king's nation emblem: the Free Peoples'
        // lit while a die can use one; the Shadow's Dismay tokens are information, spent by a Ruler die's click.
        [Test]
        public void RulerTokensAreCoinsWithTheirKingsEmblem()
        {
            var view = View(DecisionKind.Action);
            view.Sovereigns.Add(new SovereignView { Id = "kings.sovereign.theoden", Title = "Théoden", Nation = "rohan" });
            view.RulerTokens.Add(new RulerTokenView { Id = "kings.token.theoden", SovereignId = "kings.sovereign.theoden", Location = "free-peoples" });
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = "kings.token.theoden", AbilityId = "ability", Action = new ActionOption() });
            var pool = new ActionPoolView(_ => { }, _ => { });
            var root = new VisualElement();
            pool.Apply(root, view, Side.FreePeoples, true);
            var token = root.Q<Button>("ruler-token-kings.token.theoden");
            Assert.IsTrue(token.ClassListContains("ruler-token-free") && token.ClassListContains("ruler-token-lit"));
            Assert.IsTrue(token.Q("ruler-token-emblem").ClassListContains("nation-emblem-rohan"));
            Assert.IsInstanceOf<DraftToken>(token.userData);

            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.RulerTokens[0].Location = "dismay";
            view.RulerTokenActions.Clear();
            root = new VisualElement();
            pool.Apply(root, view, Side.Shadow, true);
            var dismay = root.Q<Button>("ruler-token-kings.token.theoden");
            Assert.IsTrue(dismay.ClassListContains("ruler-token-dismay"));
            Assert.IsFalse(dismay.ClassListContains("ruler-token-lit"));
            Assert.IsNull(dismay.userData, "A Dismay token is not picked by hand.");
            StringAssert.Contains("Spent by clicking a glowing Ruler die", dismay.tooltip);
        }

        [Test]
        public void CouncilOffersBothUsesAndSuppressesUnrelatedComponentsWithoutChoosingAnAction()
        {
            var view = CouncilView(true);
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardDie, EligibleDice = { "muster" } });
            var draft = CouncilDraft("muster");
            var choices = ComponentActions.For(view, draft).ToList();
            CollectionAssert.AreEquivalent(new[] { CommandKind.UseCouncilToken, CommandKind.EnterCharacter }, choices.Select(c => c.Kind));
            Assert.IsTrue(choices.All(c => c.Enabled));
            Assert.IsNull(draft.Kind);
            Assert.AreEqual("muster", draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
            Assert.IsTrue(ComponentActions.Select(view, draft, choices.Single(c => c.Kind == CommandKind.UseCouncilToken)));
            Assert.AreEqual(CommandKind.UseCouncilToken, draft.Kind);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.AbilityId);
            Assert.IsEmpty(draft.Payload.DieId, "The printed use spends no die.");
            Assert.IsEmpty(draft.Payload.CouncilTokenId, "The printed use identifies its token through AbilityId.");
        }

        [Test]
        public void CouncilCostHighlightsCandidatesAndRetainsTokenWithoutEnablingOrdinaryDieActions()
        {
            var view = CouncilView(true);
            var draft = CouncilDraft();
            var board = new BoardScreen();
            var token = new Button { name = "pool-" + ExpansionDraft.CouncilPolitics };
            board.OwnDice.Add(token);
            foreach (var die in view.Dice)
                board.OwnDice.Add(new Button { name = die.Id, userData = new DraftToken(DraftObject.Die, die.Id) });
            var sends = 0;
            using var resources = new ResourceContextInteraction(board, (edit, complete) =>
            {
                edit();
                if (complete)
                    sends++;
            }, () => false, _ => { });
            using var actions = new ActionContextInteraction(board, edit => edit(), () => false);
            resources.Refresh(view, draft);
            Assert.IsTrue(token.ClassListContains("selected"));
            Assert.IsTrue(board.OwnDice.Q<Button>("ruler").ClassListContains("resource-candidate"));
            Assert.IsTrue(board.OwnDice.Q<Button>("muster").ClassListContains("resource-candidate"));
            Assert.IsFalse(board.OwnDice.Q<Button>("army").ClassListContains("resource-candidate"));
            Assert.IsTrue(resources.Die("army"));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsTrue(resources.Die("ruler"));
            resources.Refresh(view, draft);
            actions.Refresh(view, draft);
            Assert.IsNull(draft.Kind);
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
            Assert.IsEmpty(board.ActionDie.DieId);
            Assert.IsFalse(actions.ArmyActive);
            Assert.AreEqual(0, sends);
            Assert.IsFalse(draft.Reconcile(view));
            var entry = ComponentActions.For(view, draft).Single(c => c.Kind == CommandKind.EnterCharacter);
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
        }

        [Test]
        public void NeutralCouncilSelectionSurvivesSaveRestoreWithoutAnyDieOrChosenUse()
        {
            var view = CouncilView(true);
            var session = new TurnDraftSession();
            session.Update(view);
            session.Draft.Change(p => p.CouncilTokenId = ExpansionDraft.CouncilPolitics);
            session.Edited(view);
            var saved = session.Save();
            Assert.IsNotNull(saved);
            var restored = new TurnDraftSession();
            restored.Restore(saved);
            restored.Update(view);
            Assert.IsNull(restored.Draft.Kind);
            Assert.IsEmpty(restored.Draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, restored.Draft.Payload.CouncilTokenId);
        }

        [Test]
        public void CouncilRefreshClearsOnlyAnInvalidCostAndNeverSubstitutesAnotherDie()
        {
            var view = CouncilView(true);
            var draft = CouncilDraft("ruler");
            var entry = view.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions.Single();
            entry.Dice.Remove("ruler");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsNull(draft.Kind);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
            CollectionAssert.AreEqual(new[] { "muster" }, ResourcePresentation.CouncilDice(view, draft));
            view.Options.Clear();
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.CouncilTokenId);
        }

        [Test]
        public void CouncilEntryChosenBeforeItsDieKeepsTokenAndUsesTheExactFinalCost()
        {
            var view = CouncilView(false);
            var draft = CouncilDraft();
            var entry = ComponentActions.For(view, draft).Single();
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            draft.Change(p => p.RegionId = "near-harad");
            var complete = false;
            using var resources = new ResourceContextInteraction(new BoardScreen(), (edit, submit) =>
            {
                edit();
                complete = submit;
            }, () => false, _ => { });
            resources.Refresh(view, draft);
            Assert.IsTrue(resources.Die("ruler"));
            Assert.IsTrue(complete);
            Assert.IsTrue(draft.Ready(DraftOptions.For(view, draft)));
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.CouncilTokenId);
        }

        [Test]
        public void CouncilDoesNotExposeOpponentControlsOrTargetsAfterGameEnd()
        {
            var view = CouncilView(true);
            var draft = CouncilDraft();
            view.You = Side.FreePeoples;
            Assert.IsFalse(ResourcePresentation.CanSelectCouncil(view, ExpansionDraft.CouncilPolitics));
            Assert.IsEmpty(ComponentActions.For(view, draft));
            Assert.IsEmpty(ResourcePresentation.CouncilDice(view, draft));
            view.You = Side.Shadow;
            view.Winner = Side.FreePeoples;
            Assert.IsFalse(ResourcePresentation.CanSelectCouncil(view, ExpansionDraft.CouncilPolitics));
            Assert.IsEmpty(ResourcePresentation.CouncilDice(view, draft));
        }

        private static TurnDraft CouncilDraft(string die = "")
        {
            var draft = new TurnDraft();
            draft.Change(p =>
            {
                p.CouncilTokenId = ExpansionDraft.CouncilPolitics;
                p.DieId = die;
            });
            return draft;
        }

        private static PlayerView CouncilView(bool printedUse)
        {
            var view = View(DecisionKind.Action);
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Dice.Add(new DieView { Id = "muster", Owner = Side.Shadow, Location = DieLocation.Available, Face = DieFace.Muster });
            view.Dice.Add(new DieView { Id = "ruler", Owner = Side.Shadow, Location = DieLocation.Available, Source = "ruler", Face = DieFace.Army });
            view.Dice.Add(new DieView { Id = "army", Owner = Side.Shadow, Location = DieLocation.Available, Face = DieFace.Army });
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.EnterCharacter, EligibleDice = { "muster", "ruler" },
                CharacterActions =
                {
                    new CharacterActionOption
                    {
                        CharacterId = "kings.character.black-serpent", Dice = { "muster", "ruler" },
                        CouncilTokens = { ExpansionDraft.CouncilPolitics }, RequiresCouncilToken = true,
                        Targets = { "near-harad", "umbar" }, TargetCount = 1
                    }
                }
            });
            if (printedUse)
                view.Options.Add(new ActionOption
                {
                    Kind = CommandKind.UseCouncilToken, EligibleIds = { ExpansionDraft.CouncilPolitics }, Targets = { "sauron" }
                });
            return view;
        }

        private static PlayerView View(DecisionKind kind) => new PlayerView
        {
            MatchId = "resources", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "decision", Kind = kind }
        };
    }
}
