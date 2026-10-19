using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ResourcePanelTests
    {
        [Test]
        public void ClosingPreservesDraftAndRefreshDoesNotReopenTheSameDecision()
        {
            var panel = new ResourcePanel();
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            panel.Attach(tray, dock);
            var view = View(DecisionKind.Dismay);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseDismay);
            draft.Change(p => p.Ids.Add("token"));
            panel.Context(view, draft);
            Assert.IsTrue(panel.IsOpen);
            Assert.AreSame(panel.DecisionHost, tray.parent);
            panel.Close();
            view.Revision++;
            panel.Context(view, draft);
            Assert.IsFalse(panel.IsOpen);
            CollectionAssert.AreEqual(new[] { "token" }, draft.Payload.Ids);
            panel.Open();
            Assert.IsTrue(panel.IsOpen);
            panel.Close();
            view.Decision.Id = "next";
            panel.Context(view, draft);
            Assert.IsTrue(panel.IsOpen);
            view.DecisionOwner = Side.Shadow;
            panel.Context(view, draft);
            Assert.IsFalse(panel.IsOpen);
            Assert.AreEqual(DisplayStyle.None, panel.Tab.style.display.value);
            Assert.AreSame(dock, tray.parent);
        }

        [Test]
        public void OnlyOwnResourceWindowsUseThisHostAndNeverReplaceHuntOrNarya()
        {
            var view = View(DecisionKind.Dismay);
            Assert.IsTrue(ResourcePresentation.OwnsDecision(view, null));
            view.You = Side.Shadow;
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, null));
            view.You = Side.FreePeoples;
            view.Decision.Kind = DecisionKind.Reaction;
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, null));
            view.Decision.Kind = DecisionKind.Action;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseNarya);
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, draft));
            // UI-161: the base Elven Ring is used at its console socket, the dice and the result popover.
            draft.Choose(CommandKind.UseRing);
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, draft));
            view.Decision.Kind = DecisionKind.Dismay;
            Assert.IsTrue(ResourcePresentation.OwnsDecision(view, draft));
            view.Winner = Side.Shadow;
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, draft));
        }

        // UI-264: the special-die pick and the Ruler discard are made on the pool's lit dice, never in this panel.
        [TestCase(DecisionKind.SpecialDieChoice)]
        [TestCase(DecisionKind.RulerDiscard)]
        public void PoolDecisionsStayOutOfThePanel(DecisionKind kind)
        {
            var view = View(kind);
            Assert.IsFalse(ResourcePresentation.OwnsDecision(view, null));
            Assert.IsTrue(ResourcePresentation.PoolDecision(view));
            StringAssert.StartsWith("Click a highlighted die", ResourcePresentation.PoolHint(view));
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
            var entry = ResourcePresentation.CouncilChoices(view, draft).Single();
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

        [Test]
        public void CouncilOffersBothUsesAndSuppressesUnrelatedComponentsWithoutChoosingAnAction()
        {
            var view = CouncilView(true);
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardDie, EligibleDice = { "muster" } });
            var draft = CouncilDraft("muster");
            var choices = ResourcePresentation.CouncilChoices(view, draft).ToList();
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
            }, () => false);
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
            var entry = ResourcePresentation.CouncilChoices(view, draft).Single(c => c.Kind == CommandKind.EnterCharacter);
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
            var entry = ResourcePresentation.CouncilChoices(view, draft).Single();
            Assert.IsTrue(ComponentActions.Select(view, draft, entry));
            draft.Change(p => p.RegionId = "near-harad");
            var complete = false;
            using var resources = new ResourceContextInteraction(new BoardScreen(), (edit, submit) =>
            {
                edit();
                complete = submit;
            }, () => false);
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
            Assert.IsEmpty(ResourcePresentation.CouncilChoices(view, draft));
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
