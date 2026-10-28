using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class DecisionTray
    {
        private readonly VisualElement root;
        private readonly BoardPresentation data;
        private readonly CardInspector cards;
        private readonly VersionComparison versions;
        public event Action<Action> DraftEdited;
        // UI-142: an edit that completes the decision (one figure to extend a siege) is sent at once.
        public event Action<Action> ChoiceCompleted;
        public event Action<CommandKind> ActionChosen;
        public event Action<CommandKind> DirectChosen;
        public event Action<string> DieChosen;
        public event Action<string> CardOrDieToggled;
        public event Action<string> DeckChosen;
        public event Action<string> CardInspected;
        // UI-116: the second click on an armed Combat tile plays it through the same path as a double-click.
        public event Action<DraftToken> CardFinalized;
        public event Action<string> RulerTokenChosen;
        public event Action Confirm;
        // UI-130: reopens the Reinforcements panel from a recruitment step shown in the tray.
        public event Action ReinforcementsRequested;
        // UI-160: the hint of the current step, shown in the turn console.
        public event Action<string, string> HintChanged;
        // UI-131: whether the tray's confirmation is legal now; the Reinforcements ✓ mirrors it.
        public event Action<bool> ConfirmChanged;
        // UI-162: the server preview of the draft, also for steps confirmed outside the tray (console, Hunt plaque).
        public event Action<bool> LegalChanged;
        public bool Legal { get; private set; }
        public event Action Cancel;
        private Button confirm;
        private Label status;
        private string layoutKey;
        private VisualElement confirmHost;

        // UI-370: where the confirm stands instead of the footer (the Fellowship panel's action row during the Council's
        // setup); null keeps it in the footer. A change rebuilds the tray.
        public VisualElement ConfirmHost
        {
            get => confirmHost;
            set
            {
                if (confirmHost == value)
                    return;
                confirmHost = value;
                layoutKey = null;
            }
        }
        // UI-373: the view and draft the confirm's tooltip names the die from.
        private PlayerView shownView;
        private TurnDraft shownDraft;
        private Action<ActionPreview> battlePreview;
        // UI-160/UI-164: whether the tray offers choices of its own: a shown button other than Back and the
        // Reinforcements flag. A step whose choices are on the board (a region, a die, a deck) leaves the decision
        // window closed, and its hint and Back stay in the turn console.
        // UI-211: a card decision's Confirm is the turn console's ✓, so it alone opens no window.
        public bool HasChoices => root.Query<Button>().ToList().Any(Offered);
        // What the tray's Confirm would do, and why it cannot yet (the preview's message), for the console's ✓.
        public string ConfirmTitle => confirm?.tooltip ?? "";
        public string Problem { get; private set; } = "";
        private bool cardDecision;
        // UI-298: in the Fellowship panel an interrupted response is checked with the console's glowing ✓ alone.
        public bool RecoversInConsole { get; private set; }
        public const string ConsoleCheckHint = "Click the glowing ✓ in the console to check its result.";
        public bool DirectArmy { get; set; }
        public bool DirectCard { get; set; }
        public bool DirectMuster { get; set; }
        public bool DirectPolitics { get; set; }
        public bool DirectTravel { get; set; }
        public bool ContextualFellowship { get; set; }
        public bool ContextualHunt { get; set; }
        public bool ContextualCharacter { get; set; }
        // UI-381: the console's ✓ confirms this choice; the tray keeps its confirmation hidden (its tooltip still names it).
        public bool ConfirmInConsole { get; set; }
        private readonly List<Action> refreshChoices = new List<Action>();
        // UI-327: the battle's ribbon outlasts the tray's redraws, so its marker glides from stage to stage. UI-335: it is the
        // battle window's own ribbon, the one its presented round shows, so a stage reached in the presentation is not glided
        // to again when the decision takes the row.
        private readonly StageRibbon battleRibbon;
        // UI-322: the battle's stacks as the tray last drew them, so a changed one pulses and a gone one fades out.
        private readonly BattleStackMemory battleStacks = new BattleStackMemory();

        public DecisionTray(VisualElement root, BoardPresentation data = null, CardInspector cards = null,
            VersionComparison versions = null, StageRibbon battleRibbon = null)
        {
            this.root = root;
            this.battleRibbon = battleRibbon ?? BattleStages.Ribbon();
            this.cards = cards;
            this.versions = versions;
            this.data = data ?? BoardPresentation.Load();
        }

        public void Render(PlayerView view, TurnDraft draft, bool busy, bool uncertain, string pendingProblem = "")
        {
            shownView = view;
            shownDraft = draft;
            cardDecision = CardDecisions.Owns(view, draft);
            root.EnableInClassList("event-army-planning", view.Decision?.Kind == DecisionKind.ArmyEvent);
            root.EnableInClassList("table-card-removal", draft.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardDenethor or CommandKind.DiscardLore or CommandKind.DiscardGates or CommandKind.DiscardWelcome);
            root.EnableInClassList("corruption-planning", view.Decision?.Kind == DecisionKind.CorruptionTile);
            root.EnableInClassList("draw-planning", draft.Kind == CommandKind.DrawCard);
            root.EnableInClassList("character-context", ContextualCharacter);
            root.EnableInClassList("excess-planning", draft.Kind == CommandKind.RemoveExcess);
            root.EnableInClassList("expansion-planning", ExpansionDraft.Handles(draft.Kind));
            var recruitment = DraftOptions.For(view, draft)?.EventRecruitment != null;
            root.EnableInClassList("event-recruitment", recruitment);
            var singleGroup = DraftOptions.For(view, draft)?.MaximumGroups == 1;
            var key = $"{view.MatchId}/{view.Revision}/{view.Decision?.Id}/{draft.Kind}/{uncertain}/{DirectArmy}/{DirectCard}/{DirectMuster}/{DirectPolitics}/{DirectTravel}/{ContextualFellowship}/{ContextualHunt}" +
                $"/{draft.Payload.Moves.Count}/{ArmyDraft.VisibleIndex(draft)}/{ArmyDraft.Current(draft)?.From}/{(recruitment || ExpansionDraft.Handles(draft.Kind) || CharacterDraft.UsesCharacters(draft.Kind) ? 0 : draft.Payload.Placements.Count)}" +
                $"/{draft.Payload.CardId}/{(draft.Kind == CommandKind.PlayEvent ? draft.Payload.DieId : "")}" +
                $"/{draft.Payload.RulerTokenId}/{draft.Payload.AbilityId}/{(CharacterDraft.UsesCharacters(draft.Kind) || draft.Kind == CommandKind.AwakenSovereign ? draft.Payload.CharacterId : "")}" +
                $"/{(singleGroup ? 0 : draft.Payload.CompanionMoves.Count)}/{(singleGroup ? 0 : draft.CompanionFocus)}";
            if (key != layoutKey || root.childCount == 0)
            {
                layoutKey = key;
                refreshChoices.Clear();
                Build(view, draft, uncertain);
            }
            root.SetEnabled(!busy);
            root.userData = new DraftToken(DraftObject.Tray, action: draft.Kind);
            foreach (var refresh in refreshChoices)
                refresh();
            confirm?.SetEnabled(uncertain && !busy);
            if (confirm != null && !uncertain && draft.Kind is CommandKind.RerollHunt or CommandKind.RerollBattle)
            {
                confirm.text = draft.Payload.Ids.Count == 0 ? "Keep roll" : "Reroll selected";
                confirm.tooltip = draft.Payload.Ids.Count == 0 ? "Keep this result without using optional rerolls" : "Roll only the selected failed dice";
            }
            if (uncertain && root.Q<Label>("pending-command-message") is { } message)
                message.text = PendingMessage(pendingProblem);
            if (status != null)
            {
                // UI-273: with Lords and Kings, a step before the first action is numbered.
                status.text = busy ? "Waiting for the server…" : PreActionSteps.Label(view, StatusHint(view, draft));
                status.style.display = status.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                HintChanged?.Invoke(status.text, status.text);
            }
            else
                HintChanged?.Invoke(busy ? "Waiting for the server…" : "", "");
        }

        // What the step waits for, as the console's hint; empty when its objects say it themselves.
        private string StatusHint(PlayerView view, TurnDraft draft)
        {
            if (draft.Kind == CommandKind.UseRing)
                return RingChoice.Hint(draft);
            if (draft.Kind == CommandKind.AllocateHunt)
                return HuntAllocationText.Hint(draft, HuntActions.Allocation(view));
            if (draft.Kind == CommandKind.DiscardCards)
                return "Click a card, then click it again to discard it face down.";
            if (NazgulFlightDraft.Active(view, draft))
                return NazgulFlightDraft.Hint(view, draft);
            if (CompanionJourneyDraft.Active(view, draft))
                return CompanionJourneyDraft.Hint(view, draft);
            if (CardFigureDraft.Active(view, draft))
                return CardFigureDraft.Hint(view, draft);
            if (CardArmyEvent.Active(view, draft))
                return CardArmyEvent.Hint(view, draft);
            var objectHint = ObjectHint(view, draft);
            if (objectHint.Length > 0)
                return objectHint;
            var destination = TravelDraft.DestinationHint(view, draft);
            if (destination.Length > 0)
                return destination;
            var council = CouncilTravel.Hint(view, draft);
            if (council.Length > 0)
                return council;
            if (ExpansionDraft.CouncilPoliticsPending(draft))
                return ExpansionDraft.CouncilPoliticsHint;
            if (draft.Kind == CommandKind.SelectGuide && ContextualFellowship)
                return FellowshipGuideText.ChooseHint;
            if (CompanionSetupPresentation.Versions(view) && draft.Kind == CommandKind.ChooseCompanionVersions)
                return CompanionSetupPresentation.Hint(draft, DraftOptions.For(view, draft));
            // UI-291: a reaction the console hosts is guided by the console's own hint (TurnConsoleState).
            var silent = ContextualHunt && draft.Kind == CommandKind.RespondEffect ||
                view.Decision?.Kind == DecisionKind.Reaction && CardConsoleOptions.Owns(view, draft) ||
                SovereignPresentation.OwnsDecision(view, draft) || ContextualCharacter || CompanionSetupPresentation.Homelands(view);
            return silent ? "" : DecisionObjects.Confirm(view, draft);
        }

        // The hint of a choice made on a board object: an entry's region, a Ring, a die, a card in the drawer.
        private static string ObjectHint(PlayerView view, TurnDraft draft)
        {
            foreach (var hint in new[]
            {
                CharacterChoices.RegionHint(view, draft), CostDice.RingHint(view, draft), LoreChoice.Hint(view, draft),
                SovereignPresentation.Hint(view, draft),
                RingRecovery.Hint(view, draft), InspectedHand.Cards(view).Count > 0 ? "Click a Shadow card in your hand drawer to discard it" : "",
                BalrogCardOrder.Hint(view), KingsCardChoices.Hint(view),
                ResourcePresentation.PoolHint(view)
            })
                if (hint.Length > 0)
                    return hint;
            return "";
        }

        private void Build(PlayerView view, TurnDraft draft, bool uncertain)
        {
            root.Clear();
            confirm?.RemoveFromHierarchy();
            confirm = null;
            status = null;
            battlePreview = null;
            RecoversInConsole = uncertain && FellowshipHost;
            if (uncertain)
            {
                root.Add(BoardScreen.Title("Confirming the last action"));
                root.Add(new Label { name = "pending-command-message" });
                if (RecoversInConsole)
                    return;
                confirm = BoardScreen.Button("Check result", () => Confirm?.Invoke());
                root.Add(confirm);
                return;
            }
            if (view.Winner.HasValue)
                return;
            // UI-160: the choices are on the board; the hint goes to the console, whose status reopens Reinforcements
            // and whose Back cancels a card selection.
            // UI-161: the Elven Ring gesture happens at the ring, the dice and the result popover. UI-162: the
            // Hunt allocation happens at the dice and the console's Hunt Box, whose plaque submits it (UI-172).
            if (DirectArmy || DirectCard || DirectMuster || DirectPolitics || DirectTravel || PoliticalActions.UsesPanel(draft) ||
                draft.Kind is CommandKind.UseRing or CommandKind.AllocateHunt)
            {
                status = new Label { name = "army-context-hint" };
                status.AddToClassList("draft-status");
                root.Add(status);
                return;
            }
            AddContext(view, draft);
            if (view.Decision == null)
            {
                AddWaitingCardFooter(view);
                return;
            }
            if (!draft.Kind.HasValue)
            {
                // UI-229: the Fellowship phase and the Guide decision are chosen in the Fellowship panel, not here.
                if (view.Decision.Kind != DecisionKind.Action &&
                    (!ContextualFellowship || view.Decision.Kind is not (DecisionKind.FellowshipPhase or DecisionKind.Guide)) &&
                    !CardConsoleOptions.Owns(view, draft))
                    Responses(view, draft);
                return;
            }
            if (draft.Kind == CommandKind.DiscardCards)
            {
                status = new Label { name = "discard-hint" };
                status.AddToClassList("draft-status");
                root.Add(status);
                return;
            }
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return;
            var footer = BoardScreen.Row();
            footer.AddToClassList("decision-choice-footer");
            AddDraftChoices(view, draft, option, footer);
            status = new Label(DecisionObjects.Prompt(view, draft) + ".");
            status.AddToClassList("draft-status");
            footer.Insert(0, status);
            if (view.Battle != null)
            {
                footer.AddToClassList("battle-choice-footer");
                PlayerSide(footer, view);
            }
            if (!ContextualHunt && !CompanionSetupPresentation.Setup(view) && !ResourcePresentation.PoolDecision(view) &&
                !FellowshipDraft.SingleRoute(draft.Kind) && !FellowshipActions.Immediate(draft.Kind) && !(FellowshipHost && FellowshipActions.SentOnClick(draft.Kind)) &&
                (view.Decision.Kind is DecisionKind.Action or DecisionKind.ArmyEvent ||
                view.Decision.Presentation is "DieAndResult" or "Region" || option.EventRecruitment != null || ExpansionDraft.Handles(draft.Kind) || FellowshipDraft.UsesFellowship(draft.Kind)))
            {
                var back = IconButton.Create("back", "Back", () => Cancel?.Invoke());
                back.name = "decision-back";
                footer.Add(back);
            }
            // UI-291: a reaction window is declined with the console's Pass, not a Decline or Skip button here.
            if (view.Decision.Kind == DecisionKind.ArmyEvent && !CardArmyEvent.Active(view, draft))
                foreach (var alternative in view.Options.Where(o => o.Kind != draft.Kind))
                {
                    var button = BoardScreen.Button(Label(alternative.Kind), () => ActionChosen?.Invoke(alternative.Kind));
                    button.name = "action-" + alternative.Kind;
                    button.SetEnabled(alternative.Enabled);
                    footer.Add(button);
                }
            // UI-274: a king's decision is sent by the click on his portrait, attempt button or tile; no Confirm.
            if (SovereignPresentation.OwnsDecision(view, draft) || draft.Kind == CommandKind.AwakenSovereign || ComponentActions.Immediate(view, draft) || ExpansionActions.Immediate(draft.Kind) || FellowshipActions.Immediate(draft.Kind) || ContextualFellowship && FellowshipDraft.UsesRoute(draft.Kind) &&
                option.Travel.Count > 0 && !TravelDraft.IsGrouped(draft.Kind) && !FellowshipDraft.LocationRoute(draft.Kind) ||
                option.Kind is CommandKind.ResolveHuntDamage or CommandKind.CommitCombatCard or CommandKind.AdvanceBattle or CommandKind.RetreatBattle or CommandKind.ExtendSiege || option.Kind == CommandKind.ChooseEffect &&
                option.Minimum == 1 && option.Maximum == 1 && option.EventRecruitment == null || option.Kind == CommandKind.RespondEffect)
            {
                root.Add(footer);
                return;
            }
            var text = ConfirmText(view, option);
            confirm = text == null ? IconButton.Create("confirm", Label(option.Kind), () => Confirm?.Invoke())
                : BoardScreen.Button(text, () => Confirm?.Invoke());
            confirm.name = "confirm-decision";
            confirm.tooltip = DieUse.Tooltip(view, draft, Label(option.Kind) + ".");
            if (ConfirmInConsole)
                confirm.style.display = DisplayStyle.None;
            confirm.SetEnabled(false);
            ConfirmChanged?.Invoke(false);
            (ConfirmHost ?? footer).Add(confirm);
            root.Add(footer);
        }

        // UI-208: a battle's decision buttons stand on the player's side: the attacker's on the left, the defender's on the right.
        private static void PlayerSide(VisualElement footer, PlayerView view) =>
            footer.EnableInClassList("battle-footer-attacker", view.Battle?.Attacker.Side == view.You);

        // UI-151: while the battle's cards are chosen, No card keeps its place in the footer, greyed out while the
        // opponent chooses, so the battle window keeps its size.
        private void AddWaitingCardFooter(PlayerView view)
        {
            if (view.Battle == null || BattleStages.Index(view.Battle.Stage) != 0)
                return;
            var footer = BoardScreen.Row();
            footer.AddToClassList("decision-choice-footer");
            footer.AddToClassList("battle-choice-footer");
            PlayerSide(footer, view);
            var waiting = new Label { name = "battle-card-waiting" };
            waiting.AddToClassList("draft-status");
            footer.Add(waiting);
            var none = BattleChoices.AddDirect(footer, "No card", "battle-no-card", CommandKind.CommitCombatCard, _ => { });
            none.SetEnabled(false);
            none.tooltip = "No card · Available while you choose your Combat card";
            root.Add(footer);
        }

        // UI-138: null means the confirm icon; its tooltip names the decision. A reroll keeps words: Refresh says
        // whether the press keeps the roll or rerolls the selected dice.
        private static string ConfirmText(PlayerView view, ActionOption option)
        {
            if (option.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses)
                return "Apply losses";
            if (option.Kind is CommandKind.RerollBattle or CommandKind.RerollHunt)
                return "Keep roll";
            if (option.Kind is CommandKind.DiscardCards or CommandKind.ChooseCompanionVersions or CommandKind.ChooseCouncilHomelands)
                return null;
            if (view.Decision.Kind == DecisionKind.CharacterEvent)
                return "Finish movement";
            if (view.CorruptionAttempt?.Sequential == true && option.Kind == CommandKind.ChooseCorruptionTile)
                return "Stop · Place tile";
            if (view.Decision.Presentation == "FellowshipCompanions" && view.Fellowship.MordorStep.HasValue)
                return "Remove from play";
            if (option.Kind == CommandKind.SeparateCompanions && option.Travel.Count == 0)
                return "Remove from play";
            return Label(option.Kind);
        }

        private void AddContext(PlayerView view, TurnDraft draft)
        {
            if (CompanionSetupPresentation.Setup(view))
                return;
            var title = BoardScreen.Title(view.Decision == null ? "Waiting for " + (view.DecisionOwner.HasValue ? BoardScreen.SideName(view.DecisionOwner.Value) : "opponent")
                : view.Decision.Window == "hunt.guide" ? "Guide response" : view.Decision.Window == "hunt.table" ? "Table response"
                : view.Decision.Kind == DecisionKind.CompanionSeparation && draft.Kind == CommandKind.MoveCompanions ? "Move companions · No extra die"
                : view.Decision.Window == "ents.follow-up" ? "Play a Character Event · No die"
                : view.Decision.Presentation == "ArmyFigure" ? "Choose a figure to eliminate"
                : view.Decision.Presentation == "CorruptionDraw" ? "Draw a Corruption tile"
                : view.Decision.Presentation == "CorruptionChoice" ? "Choose a Corruption tile"
                : view.Decision.Presentation == "CorruptionRemoval" ? "Random tile return"
                : view.Decision.Presentation == "ReserveFigures" ? "Choose reinforcements"
                : view.Decision.Presentation == "RecruitmentPlan" ? "Place reinforcements · No extra die"
                : view.Decision.Presentation == "NazgulStrike" ? "Resolve The Nazgûl Strike"
                : view.Decision.Presentation == "LureCompanion" ? "Lure of the Ring · " + CharacterLibrary.ShortName(view,
                    view.Options.First(o => o.Kind == CommandKind.ChooseEffect).EffectChoices[0].SourceId)
                : view.Decision.Presentation == "FellowshipCompanions" ? view.Fellowship.MordorStep.HasValue
                    ? "Choose Companions to remove" : "Choose Companions to separate"
                : view.Decision.Presentation == "CharacterCardDiscard" ? "Discard a Character card"
                : view.Decision.Presentation == "Deck" ? "Choose a deck"
                : view.Decision.Presentation == "CaptainOrder" ? "The Black Captain · Recruit or move Nazgûl"
                : view.Decision.Presentation == "DieAndResult" ? "Lore · Choose a die and its result"
                : view.Decision.Presentation == "SmeagolCorruption" ? "We Won’t Go Back · Choose a cost or decline"
                : view.Decision.Presentation == "Region" ? "Choose a region" : view.Decision.Presentation == "ActionDie" ? "Change action dice"
                : view.Decision.Window == "fellowship.revealed" ? "Revelation response" : Title(view.Decision.Kind, draft.Kind));
            root.Add(title);
            // UI-278 (D16): a card's choice of a king during a battle keeps the battle window, where a king who takes part
            // glows in his army.
            if (SovereignPresentation.OwnsDecision(view, draft) && !SovereignChoices.InBattleWindow(view))
                return;
            if (!ContextualHunt)
                HuntContext.Add(root, view, data);
            if (view.EventLoss != null)
            {
                title.text = data.Regions[view.EventLoss.RegionId].Name + " · " + view.EventLoss.Hits + " hits" +
                    (view.Decision == null ? " · Waiting for losses" : " · Choose losses");
                root.Add(new Label(CardLibrary.Title(view.EventLoss.SourceId) + " · Roll " + string.Join("  ", view.EventLoss.Rolls)) { name = "event-loss-roll" });
            }
            var completedBattleExcess = view.Battle?.Stage == "Complete" && view.Decision?.Kind == DecisionKind.RemoveExcess;
            if (view.Battle != null && !completedBattleExcess)
            {
                root.Add(battleRibbon.Root);
                BattleStages.Apply(battleRibbon, view.Battle);
                var battle = new BattleContext(root, view.Battle, view, draft, action => DraftEdited?.Invoke(action), id => CardInspected?.Invoke(id), cards,
                    action => ChoiceCompleted?.Invoke(action), battleStacks, kind => DirectChosen?.Invoke(kind));
                refreshChoices.Add(battle.Refresh);
                title.text = data.Regions[view.Battle.RegionId].Name + " · " + view.Battle.Mode + " · Round " + view.Battle.Round +
                    (view.Battle.MandatorySiegeRounds > 0 && view.Battle.Round <= view.Battle.MandatorySiegeRounds ? "/" + view.Battle.MandatorySiegeRounds : "") +
                    (view.Decision == null ? " · Waiting for opponent" : " · " +
                        (view.Decision.Presentation == "VoluntaryLosses" ? "Choose a sacrifice" : view.Decision.Presentation == "NazgulLeadership" ? NazgulLeadershipSummary.Title(view) : BattleTitle(view.Decision.Kind)));
            }
        }

        // UI-130/UI-131: reopens the Reinforcements panel, where card and ability recruitment take place.
        private Button ReinforcementsButton()
        {
            var reopen = IconButton.Create("reinforcements", "Reserve · Show the reserves and the planned reinforcements",
                () => ReinforcementsRequested?.Invoke());
            reopen.name = "open-reinforcements";
            return reopen;
        }

        private void AddDraftChoices(PlayerView view, TurnDraft draft, ActionOption option, VisualElement footer)
        {
            // UI-274: the kings' decisions are made on their portraits, attempt buttons, tiles and the map.
            if (SovereignPresentation.OwnsDecision(view, draft))
                return;
            if (ExpansionDraft.Handles(draft.Kind))
            {
                var choices = new ExpansionChoices(root, view, draft, action => DraftEdited?.Invoke(action), data, versions);
                refreshChoices.Add(choices.Refresh);
            }
            else if (option.EventRecruitment != null)
                root.Add(ReinforcementsButton());
            else if (CharacterDraft.UsesCharacters(draft.Kind))
            {
                var choices = new CharacterChoices(root, view, draft, action => DraftEdited?.Invoke(action), data, ContextualCharacter,
                    ReinforcementsButton);
                refreshChoices.Add(choices.Refresh);
            }
            else if (BattleDraft.UsesBattle(draft.Kind))
            {
                if (draft.Kind == CommandKind.CommitCombatCard)
                    root.Add(new BattleCombatHand(view, option, token => CardFinalized?.Invoke(token)).Root);
                var choices = new BattleChoices(root, footer, view, draft, action => DraftEdited?.Invoke(action),
                    action => ChoiceCompleted?.Invoke(action), kind => DirectChosen?.Invoke(kind), data);
                refreshChoices.Add(choices.Refresh);
                battlePreview = choices.Preview;
            }
            else if (NazgulFlightDraft.Active(view, draft))
            {
                // UI-212: the flights are chosen on the map; the console confirms them.
            }
            else if (CompanionJourneyDraft.Active(view, draft))
            {
                // UI-294: the companions move on the map; the console's ✓ finishes the journey.
            }
            else if (CardFigureDraft.Active(view, draft))
            {
                // UI-215: the figures are chosen in the region panel; the console confirms them.
            }
            else if (CardArmyEvent.Active(view, draft))
            {
                // UI-216: the armies move on the map and in the region panel; the console holds the rest.
            }
            else if (CardConsoleOptions.Owns(view, draft))
            {
                // UI-214: the options are buttons in the console; their objects (a table card, a card in hand) light up.
            }
            else if (FellowshipDraft.UsesFellowship(draft.Kind))
            {
                var choices = new FellowshipChoices(root, view, draft, action => DraftEdited?.Invoke(action), data, contextual: ContextualFellowship);
                refreshChoices.Add(choices.Refresh);
            }
            else if (CardDraft.UsesCards(draft.Kind))
            {
                var choices = new CardChoices(root, view, draft, action => DraftEdited?.Invoke(action), data, cards, ReinforcementsButton);
                refreshChoices.Add(choices.Refresh);
            }
            else if (BoardDraft.UsesBoard(draft.Kind))
            {
                // UI-289: armies move and attack on the map and in the region panel (DirectArmy, CardArmyEvent); the
                // tray keeps no figure lists for them.
                if (draft.Kind == CommandKind.RemoveExcess)
                {
                    var choices = new ExcessChoices(root, view, draft, action => DraftEdited?.Invoke(action));
                    refreshChoices.Add(choices.Refresh);
                }
                else if (draft.Kind == CommandKind.Recruit)
                    root.Add(ReinforcementsButton());
            }
            else
                Choices(view, draft, option);
        }

        // In a card decision the console confirms (UI-211), so its Confirm is no choice of the window. UI-297: the
        // effect trail is recalled only in the console, never in the tray.
        // UI-377: the Council token's travel is confirmed by the ✓ above the token (or the console's).
        private bool Offered(Button button) => button.name is not ("decision-back" or "open-reinforcements") &&
            !((cardDecision || CouncilTravel.Active(shownView, shownDraft)) && button == confirm) && Shown(button);

        // The tray sits in the Fellowship panel (FellowshipActions.UsesPanel), not in the Hunt window.
        private bool FellowshipHost => ContextualFellowship && !ContextualHunt;

        private string PendingMessage(string problem)
        {
            if (!RecoversInConsole)
                return problem.Length > 0 ? problem : "The response was interrupted. Recover its result to continue.";
            return (problem.Length > 0 ? problem + " · " : "The response was interrupted. ") + ConsoleCheckHint;
        }

        private bool Shown(VisualElement element)
        {
            for (var current = element; current != null && current != root; current = current.parent)
                if (current.style.display.value == DisplayStyle.None)
                    return false;
            return true;
        }

        public void Preview(bool legal, string message, string detail = "")
        {
            Problem = legal ? "" : message;
            confirm?.SetEnabled(legal);
            // UI-373: once the server confirms the step, the ✓ says what it does and with which die.
            if (legal && confirm != null && message.Length > 0)
                confirm.tooltip = DieUse.Tooltip(shownView, shownDraft, message);
            ConfirmChanged?.Invoke(confirm != null && legal);
            if (Legal != legal)
            {
                Legal = legal;
                LegalChanged?.Invoke(legal);
            }
            if (status != null)
            {
                status.text = message;
                status.tooltip = detail.Length > 0 ? message + "\n" + detail : message;
                status.style.display = message.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        // QUAL-19: a problem or notice the player must read also reaches the console's hint, because the tray often
        // sits in the closed decision window (UI-160). Step hints and preview summaries stay with Preview.
        public void Notice(string message)
        {
            Preview(false, message);
            HintChanged?.Invoke(message, message);
        }

        public void PreviewDetails(ActionPreview preview) => battlePreview?.Invoke(preview);

        private void Responses(PlayerView view, TurnDraft draft)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            // UI-247: reactions used on their portraits in the Battle window need no Respond step here. UI-291: the
            // console's Pass declines a reaction window. UI-347 (D12): Evil Knowledge is the Mouth of Sauron's portrait.
            foreach (var option in DraftOptions.All(view, draft).Where(o => o.Kind is not (CommandKind.PlayEvent or CommandKind.DeclineReaction) &&
                !(o.Kind == CommandKind.RespondEffect && BattleReactions.AllOnPortraits(view)) &&
                !(o.Kind == CommandKind.UseEvilKnowledge && EvilKnowledgeChoice.OnPortrait(view))))
            {
                var button = BoardScreen.Button(Label(option.Kind), () => ActionChosen?.Invoke(option.Kind));
                button.SetEnabled(option.Enabled);
                button.name = "action-" + option.Kind;
                button.userData = new DraftToken(DraftObject.Action, action: option.Kind);
                button.tooltip = option.Reason;
                row.Add(button);
            }
        }

        private void Choices(PlayerView view, TurnDraft draft, ActionOption option)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            // UI-248: a table card's removal takes its Ring on the Ring itself and its die in the pool (UI-272: Lore;
            // UI-278: Welcome and the Palantír of Barad-dûr), so the tray lists neither.
            foreach (var id in CostDice.InPool(view, draft) || CostDice.Removal(draft.Kind) ? new List<string>() : option.EligibleDice)
            {
                var die = view.Dice.Single(d => d.Id == id);
                var conversion = option.DrawCosts.Any(c => c.DieId == id && c.AbilityId == CardDrawDraft.Palantir);
                var label = ExpansionLabels.DieCost(view, draft, die) + (conversion ? " → E" : "");
                var detail = conversion ? "Palantír of Barad-dûr · Muster as Event · Once this turn" : die.Face.ToString();
                var button = Choice(label, () => draft.Payload.DieId == id, () => DieChosen?.Invoke(id), detail);
                button.name = "cost-" + id;
                button.userData = new DraftToken(DraftObject.Die, id);
                row.Add(button);
            }
            if (option.Kind == CommandKind.AllocateHunt && !ContextualHunt)
                HuntDice(row, draft, option);
            foreach (var deck in option.Kind == CommandKind.DrawCard ? option.Targets : new List<string>())
            {
                var button = Choice(deck == "character" ? "Character deck" : "Strategy deck", () => draft.Payload.Deck == deck, () => DeckChosen?.Invoke(deck));
                button.name = "deck-" + deck;
                button.userData = new DraftToken(DraftObject.Deck, deck, option.Kind);
                row.Add(button);
            }
        }

        private void HuntDice(VisualElement row, TurnDraft draft, ActionOption option)
        {
            foreach (var id in option.EligibleIds)
            {
                var button = Choice("·", () => draft.Payload.Ids.Contains(id), () => CardOrDieToggled?.Invoke(id), "Allocate this die to the Hunt");
                button.name = "select-" + id;
                refreshChoices.Add(() => button.text = draft.Payload.Ids.Contains(id) ? "Eye" : "·");
                row.Add(button);
            }
            var count = new Label();
            refreshChoices.Add(() => count.text = $"{draft.Payload.Ids.Count} selected · {(option.Minimum == option.Maximum ? $"choose {option.Minimum}" : $"choose {option.Minimum}–{option.Maximum}")}");
            root.Add(count);
        }

        private Button Choice(string label, Func<bool> selected, Action action, string tooltip = "")
        {
            var button = BoardScreen.Button(label, action);
            refreshChoices.Add(() => button.EnableInClassList("selected", selected()));
            button.tooltip = tooltip;
            return button;
        }

        public static string CardLabel(string id) => CardLibrary.Title(id);

        internal static string Title(DecisionKind kind, CommandKind? action) => ExpansionDecision.Title(kind).Length > 0 ? ExpansionDecision.Title(kind)
            : action.HasValue && ExpansionDecision.Label(action.Value).Length > 0 ? ExpansionDecision.Label(action.Value)
            : kind == DecisionKind.DiscardCards ? "Discard to six cards"
            : kind == DecisionKind.AllocateHunt ? "Allocate Hunt dice"
            : kind == DecisionKind.FellowshipPhase && !action.HasValue ? "Fellowship phase"
            : kind == DecisionKind.ArmyEvent ? (action == CommandKind.AttackArmy ? "Attack · No extra die" : "Move armies · No extra die")
            : kind == DecisionKind.CharacterEvent ? "Move selected characters · No extra die"
            : action == CommandKind.UseRing ? "Change a die · then take your action"
            : action == CommandKind.MoveArmies ? "Move armies · Selected figures move, dimmed figures stay"
            : action == CommandKind.AttackArmy ? "Attack · Choose participants and target"
            : action == CommandKind.Recruit ? "Reinforcements · One elite or two figures in different settlements"
            : action == CommandKind.AdvancePolitics ? "Advance politics"
            : action == CommandKind.RemoveExcess ? "Return excess units to the reserve"
            : action == CommandKind.DiscardProtection ? "Remove protection · Plan your cost"
            : action == CommandKind.DiscardDenethor ? "Remove Denethor’s Folly"
            : action == CommandKind.DiscardLore ? "Remove Lore · W or a die + Ring"
            : action == CommandKind.DiscardWelcome ? "Remove Your Welcome Is Doubtful"
            : action == CommandKind.DiscardGates ? "Remove The Gates are Closed"
            : action == CommandKind.DiscardBaradPalantir ? "Remove Barad-dûr Palantír · W or a die + Ring"
            : action == CommandKind.DiscardPalantir ? "Remove Orthanc Palantír · W or a die + Ring"
            : action == CommandKind.PlayEvent ? "Play an Event · Plan your choice"
            : action == CommandKind.DeclareFellowship ? "Declare · Choose your route"
            : action == CommandKind.RevealFellowship ? "Reveal · Choose your route"
            : action == CommandKind.SeparateCompanions ? "Separate companions"
            : action == CommandKind.MoveCompanions ? "Move companions · One die for all groups"
            : action == CommandKind.MoveShadowCharacters ? "Move Nazgûl & Minions · One die for all groups"
            : action == CommandKind.EnterCharacter ? "Bring a character into play"
            : action == CommandKind.UseCharacterAbility ? "Character ability"
            : action == CommandKind.SelectGuide ? "Choose the Guide"
            : action == CommandKind.MoveFellowship ? "Move the Fellowship"
            : action == CommandKind.HideFellowship ? "Hide the Fellowship"
            : action == CommandKind.EnterMordor ? "Enter Mordor"
            : kind == DecisionKind.HuntDamage ? "Resolve Hunt damage"
            : kind == DecisionKind.HuntReroll ? "Hunt · Choose rerolls"
            : kind == DecisionKind.EffectChoice ? "Resolve the played card · Choose the next step"
            : kind == DecisionKind.Reaction ? "Response window"
            : "Your action";

        public static string Label(CommandKind kind) => ExpansionDecision.Label(kind).Length > 0 ? ExpansionDecision.Label(kind) : kind switch
        {
            CommandKind.AttackArmy => "Attack",
            CommandKind.CommitCombatCard => "Commit card",
            CommandKind.RerollBattle => "Resolve rerolls",
            CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses => "Confirm losses",
            CommandKind.ContinueBattle => "Continue battle",
            CommandKind.StopBattle => "Stop battle",
            CommandKind.HoldBattle => "Hold position",
            CommandKind.RetreatBattle => "Retreat",
            CommandKind.AdvanceBattle => "Advance",
            CommandKind.FightFieldBattle => "Fight in field",
            CommandKind.RetreatIntoSiege => "Retreat inside",
            CommandKind.ExtendSiege => "Extend siege",
            CommandKind.ContinueFellowship => "Continue to Hunt allocation",
            CommandKind.AllocateHunt => "Allocate & roll",
            CommandKind.DiscardCards => "Discard selected cards",
            CommandKind.DiscardDie => "Discard a die",
            CommandKind.DrawCard => "Draw a card",
            CommandKind.UseRing => "Use a Ring",
            CommandKind.Pass => "Pass",
            CommandKind.DeclineReaction => "Decline",
            CommandKind.MoveArmies => "Move armies",
            CommandKind.Recruit => "Recruit",
            CommandKind.AdvancePolitics => "Advance politics",
            CommandKind.RemoveExcess => "Return selected units",
            CommandKind.DiscardProtection => "Remove protection",
            CommandKind.DiscardDenethor => "Remove Denethor",
            CommandKind.DiscardPalantir => "Remove Orthanc Palantír",
            CommandKind.DiscardBaradPalantir => "Remove Barad-dûr Palantír",
            CommandKind.DiscardLore => "Remove Lore",
            CommandKind.DiscardWelcome => "Remove Welcome",
            CommandKind.DrawCorruptionTile => "Draw tile",
            CommandKind.ChooseCorruptionTile => "Place tile",
            CommandKind.DiscardGates => "Remove Gates",
            CommandKind.FinishArmyEvent => "Skip maneuver",
            CommandKind.PlayEvent => "Play Event",
            CommandKind.ChooseEffect => "Confirm choice",
            CommandKind.RespondEffect => "Respond",
            CommandKind.DeclareFellowship => "Declare",
            CommandKind.RevealFellowship => "Reveal",
            CommandKind.HideFellowship => "Hide",
            CommandKind.SelectGuide => "Choose Guide",
            CommandKind.SeparateCompanions => "Separate companions",
            CommandKind.MoveCompanions => "Move companions",
            CommandKind.MoveShadowCharacters => "Move Nazgûl & Minions",
            CommandKind.EnterCharacter => "Enter character",
            CommandKind.UseCharacterAbility => "Use ability",
            CommandKind.MoveFellowship => "Move Fellowship",
            CommandKind.EnterMordor => "Enter Mordor",
            CommandKind.ResolveHuntDamage => "Resolve damage",
            CommandKind.RerollHunt => "Resolve Hunt roll",
            _ => kind.ToString()
        };

        private static string BattleTitle(DecisionKind kind) => kind switch
        {
            DecisionKind.CombatCard => "Choose card",
            DecisionKind.BattleReroll => "Choose rerolls",
            DecisionKind.Losses => "Choose losses",
            DecisionKind.BattleAdvance => "Advance or stay",
            DecisionKind.SiegeContinuation => "Extend or stop",
            DecisionKind.StrongholdDefense => "Field or stronghold",
            DecisionKind.BattleContinue => "Continue or stop",
            DecisionKind.BattleRetreat => "Hold or retreat",
            _ => "Resolve effect"
        };
    }
}
