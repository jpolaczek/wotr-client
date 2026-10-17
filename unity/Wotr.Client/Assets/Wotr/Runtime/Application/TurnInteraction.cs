using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class TurnInteraction : IDisposable, ICommandEvents
    {
        // AUDIO-04: a command leaves in this frame; the gesture that sent it waits for the sound of its result.
        public event Action CommandSending;
        // AUDIO-05: the answer to an own command; `recovered` when it was the pending command's receipt recovered.
        public event Action<GameCommand, CommandReceipt, bool> CommandAnswered;
        public bool GestureSuppressed => input.LastPressSuppressed;

        private readonly GameApi api;
        private readonly BoardScreen board;
        private string notice = "";
        private string skippedCombatChoice = "";
        private string pendingProblem = "";
        private readonly MatchConnection connection;
        private readonly DecisionTray tray;
        private readonly CommandSender sender;
        private readonly BattlePolicyInteraction policy;
        private readonly TurnDraftSession session = new TurnDraftSession();
        private readonly LocalMatchPersistence persistence;
        public Task LocalWrites => persistence?.Flushed ?? Task.CompletedTask;
        private TurnDraft draft => session.Draft;
        private readonly DraftInput input;
        private readonly ActionContextInteraction context;
        private readonly CardContextInteraction cards;
        private readonly BattleContextInteraction battles;
        private readonly MusterContextInteraction muster;
        // UI-130/UI-131: the Reinforcements panel for Muster and for card and ability recruitment.
        private readonly ReinforcementsInteraction reinforcements;
        private readonly PoliticalContextInteraction politics;
        private readonly TravelContextInteraction travel;
        private readonly NazgulFlightInteraction flights;
        private readonly CardFigureInteraction cardFigures;
        private readonly CardArmyInteraction cardArmies;
        private readonly FellowshipContextInteraction fellowshipContext;
        private readonly HuntContextInteraction huntContext;
        private readonly ResourceContextInteraction resources;
        private readonly ComponentContextInteraction components;
        private readonly ConsoleOptionsInteraction consoleOptions;
        private readonly TurnConsoleInteraction console;
        private readonly RingInteraction rings;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private PlayerView view;
        private ViewVersion version;
        private readonly TurnSignal signal = new TurnSignal();
        private long requiredRevision = -1;
        private CancellationTokenSource previewLifetime;
        private bool sending;
        private bool online;
        private bool previewLegal;
        private int generation;
        private int connectionEpoch;
        private string recoveryShown = "";
        private bool SelectionBusy => sending || sender.Pending != null || policy.Busy || view?.Revision < requiredRevision;

        public TurnInteraction(GameApi api, MatchConnection connection, BoardScreen board, LocalMatchPersistence persistence = null)
        {
            this.api = api;
            this.board = board;
            this.connection = connection;
            this.persistence = persistence;
            session.Restore(persistence?.Draft);
            sender = new CommandSender(api, persistence?.Actions);
            policy = new BattlePolicyInteraction(api, connection, board.BattlePolicy, persistence?.Policy);
            policy.Changed += PolicyChanged;
            tray = new DecisionTray(board.Tray, board.Presentation, board.Cards, board.CompanionSetup);
            tray.ActionChosen += Choose;
            tray.RulerTokenChosen += ChooseRulerToken;
            tray.DirectChosen += DirectBattleChoice;
            tray.DieChosen += ChooseDie;
            tray.CardInspected += id => { if (view?.Battle != null) board.Cards.ShowCombat(id); else board.Cards.Show(id, true); };
            tray.DraftEdited += EditChoice;
            tray.ChoiceCompleted += CompleteChoice;
            tray.RingChosen += id => EditChoice(() => draft.Change(p => p.RingId = id));
            tray.CardOrDieToggled += id => Edit(() => draft.Toggle(id));
            tray.DeckChosen += deck => Edit(() => draft.Change(p => p.Deck = deck));
            tray.Cancel += Back;
            tray.Confirm += Confirm;
            tray.ReinforcementsRequested += board.Reinforcements.Show;
            tray.ConfirmChanged += board.Reinforcements.ConfirmLegal;
            context = new ActionContextInteraction(board, Edit, () => SelectionBusy);
            cards = new CardContextInteraction(board, EditSelection,
                () => SelectionBusy, ShowChoiceProblem);
            battles = new BattleContextInteraction(EditSelection,
                () => SelectionBusy);
            muster = new MusterContextInteraction(board, EditSelection,
                () => SelectionBusy);
            reinforcements = new ReinforcementsInteraction(board, EditChoice, () => SelectionBusy, () => muster.Active,
                id => muster.RecruitAtOnly(id), Confirm, FinishSequence);
            politics = new PoliticalContextInteraction(board, EditSelection, () => SelectionBusy);
            travel = new TravelContextInteraction(board, EditSelection,
                () => SelectionBusy);
            fellowshipContext = new FellowshipContextInteraction(board, EditSelection,
                () => SelectionBusy);
            huntContext = new HuntContextInteraction(board, EditSelection, () => SelectionBusy);
            resources = new ResourceContextInteraction(board, EditSelection,
                () => SelectionBusy);
            flights = new NazgulFlightInteraction(board, EditSelection, () => SelectionBusy);
            cardFigures = new CardFigureInteraction(board, EditSelection, () => SelectionBusy);
            cardArmies = new CardArmyInteraction(board, EditSelection, () => SelectionBusy);
            context.FigureSelector = id => flights.Figure(id) || cardFigures.Figure(id) || cardArmies.Figure(id) || travel.Figure(id);
            // UI-142: no double-click; the scope lets the input suppress a fast second click after a committed step.
            input = new DraftInput(board.Root, Confirm, Back, Drop,
                () => view == null ? "" : view.MatchId + "/" + view.Revision + "/" + view.Decision?.Id + "/" + connectionEpoch);
            input.DragChanged += board.Nations.PreviewDrag;
            input.DragChanged += board.Battles.PreviewDrag;
            input.DragChanged += board.HandDrawer.PreviewDrag;
            input.DragChanged += board.Hover.PreviewDrag;
            input.DragChanged += board.Map.HistoryOverlay.PreviewDrag;
            input.DragChanged += SuspendPreviews;
            board.CardFinalized += FinalizeCard;
            tray.CardFinalized += FinalizeCard;
            board.Escape.CancelGesture = input.CancelGesture;
            board.Escape.CancelChoice = TryBack;
            board.Escape.ClearSelection = ClearArmySelection;
            board.DieSelected += ChooseDie;
            board.RulerTokenSelected += ChooseRulerToken;
            board.Map.RegionSelected += ChooseRegion;
            board.Map.ArmySelected += ChooseArmy;
            components = new ComponentContextInteraction(board, ChooseComponent);
            consoleOptions = new ConsoleOptionsInteraction(board, ChooseConsoleOption);
            console = new TurnConsoleInteraction(board, tray, Back, Confirm, FinishSequence, ChooseComponent, ChooseRing,
                () => reinforcements.Planning);
            rings = new RingInteraction(board, face => EditChoice(() => draft.Change(p => p.Face = face)), CancelRing);
            board.Characters.Enterable = figure => !SelectionBusy && board.Characters.Actions.Entry(figure) != null;
            board.Characters.EntryRequested += RequestEntry;
            if (persistence != null)
                persistence.Changed += Render;
        }

        private void ChooseRegion(string region)
        {
            if (battles.Target(region))
                return;
            // Inspection must not consume a map target of a nested card or reaction.
            var battleInspected = board.Battles.SelectRegion(region);
            if (view?.Decision == null)
            {
                if (!battleInspected)
                    board.Hover.Toggle(region);
                return;
            }
            if (muster.Active && draft.Kind == CommandKind.Recruit)
            {
                if (muster.Target(region))
                    return;
                var refusal = muster.Refusal(region);
                if (refusal.Length > 0)
                    ShowChoiceProblem(refusal);
                board.Hover.Toggle(region);
                return;
            }
            if (context.ArmyActive && draft.Kind is null or CommandKind.MoveArmies or CommandKind.AttackArmy)
            {
                if (view.Revision >= requiredRevision && context.Target(region))
                    SubmitPreparedStep();
                else
                    board.Hover.Toggle(region);
                return;
            }
            if (!draft.Kind.HasValue)
            {
                if (!battleInspected)
                    board.Hover.Toggle(region);
                return;
            }
            // UI-215: a region holding a card's offered figures opens its panel, where the figures are chosen.
            if (cardFigures.Region(region))
                return;
            // UI-216: a lit army of a card's army event starts its move and opens its panel.
            if (cardArmies.Region(region))
                return;
            if (draft.Kind == CommandKind.ChooseCouncilHomelands && view?.Decision != null)
            {
                Edit(() => CouncilDraft.Region(draft, DraftOptions.For(view, draft), region));
                return;
            }
            if (CharacterDraft.UsesCharacters(draft.Kind) && view?.Decision != null)
            {
                EditChoice(() => CharacterDraft.Region(draft, DraftOptions.For(view, draft), region));
                return;
            }
            if (draft.Kind == CommandKind.RetreatBattle && view.Options.Any(o => o.Kind == draft.Kind && o.Targets.Contains(region)))
            {
                Edit(() => draft.Change(p => p.RegionId = region));
                return;
            }
            if (flights.Active)
            {
                // UI-212: a card's Nazgûl fly by map clicks; any other region opens its panel.
                if (!flights.Region(region))
                    board.Hover.Toggle(region);
                return;
            }
            if (view?.Decision != null && TravelDraft.TargetRoute(draft.Kind))
            {
                // UI-105: a reachable destination moves the selected group at once; any other region opens its panel.
                if (!travel.Reach(region))
                    board.Hover.Toggle(region);
                return;
            }
            if (view?.Decision != null && FellowshipDraft.UsesRoute(draft.Kind))
            {
                // UI-219: each click adds a step; a route that uses all of the Progress moves at once, a shorter one
                // waits for ✓.
                travel.Step(region);
                return;
            }
            if (draft.Kind == CommandKind.ChooseEffect)
            {
                var option = DraftOptions.For(view, draft);
                if (option.EventRecruitment != null)
                {
                    Edit(() => EventRecruitDraft.Region(draft, option, region));
                    return;
                }
                if (view.Decision?.Presentation is "ArmyFigure" or "ArmyFigures")
                {
                    if (option.EffectChoices.Any(c => EffectChoiceDraft.FigureRegion(c) == region))
                        Edit(() => draft.Change(p =>
                        {
                            p.RegionId = region;
                            if (view.Decision.Presentation == "ArmyFigure")
                                p.Ids.Clear();
                        }));
                    return;
                }
                if (view.Decision?.Presentation == "Region")
                {
                    EditChoice(() => EffectChoiceDraft.ChooseRegion(draft, option, region));
                    return;
                }
                var choice = option.EligibleIds.Contains(region) ? region : "";
                if (choice.Length > 0)
                    EditChoice(() => CardDraft.Choose(draft, option, choice));
                return;
            }
            if (view?.Decision != null)
                Edit(() => BoardDraft.SelectRegion(draft, view, region));
        }

        private void ChooseArmy(DraftToken army)
        {
            if (context.ArmyActive || view?.Decision?.Kind == DecisionKind.Action && !draft.Kind.HasValue)
            {
                if (army.Side != view.You)
                    ChooseRegion(army.Id);
                else if (context.SelectArmy(army))
                    board.Hover.Open(army.Id);
                else
                    board.Hover.Toggle(army.Id);
                return;
            }
            ChooseRegion(army.Id);
        }

        private void FinishSequence() => Choose(CommandKind.FinishActionSequence);

        // UI-108: a double-click (or Enter) on a Characters row brings that figure into play for the chosen cost.
        private bool EnterCharacter(string figure)
        {
            var entry = SelectionBusy ? null : board.Characters.Actions.Entry(figure);
            if (entry == null)
                return false;
            ChooseComponent(entry);
            return true;
        }

        private void RequestEntry(string figure) => EnterCharacter(figure);

        private void ChooseComponent(ComponentAction component)
        {
            if (!CanChoose(component.Kind))
                return;
            var next = draft.Fork();
            if (!ComponentActions.Select(view, next, component))
                return;
            if (FellowshipActions.UsesPanel(view, next))
                board.Fellowship.Open();
            EditSelection(() => draft.Adopt(next), ComponentActions.Immediate(view, next));
        }

        private void SubmitPreparedStep()
        {
            notice = "";
            session.Edited(view);
            PersistDraft();
            board.Hover.Close();
            if (online)
                _ = Submit(true);
            else
                Render();
        }

        private void Confirm()
        {
            if (sender.Pending != null || previewLegal)
                _ = Submit();
        }

        private void Back() => Edit(() =>
        {
            // UI-161: Esc or Back ends the Elven Ring gesture as a whole.
            if (RingChoice.Armed(draft))
                draft.Clear();
            else if (draft.Kind is CommandKind.PlayEvent or CommandKind.DrawCard)
            {
                var cost = draft.Fork().Payload;
                draft.Clear();
                draft.Change(p => { p.DieId = cost.DieId; p.Face = cost.Face; p.AbilityId = cost.AbilityId; p.RulerTokenId = cost.RulerTokenId; });
            }
            else
                draft.Back();
            EnsureMandatoryDraft();
        });

        // UI-127: the first Escape over the open region panel drops the whole unsent selection; the die stays,
        // and an army action in progress rebuilds its step.
        private bool ClearArmySelection()
        {
            if (SelectionBusy || !context.HasArmySelection)
                return false;
            Edit(() =>
            {
                var cost = draft.Fork().Payload;
                draft.Clear();
                draft.Change(p => { p.DieId = cost.DieId; p.AbilityId = cost.AbilityId; });
                EnsureMandatoryDraft();
            });
            return true;
        }

        private bool TryBack()
        {
            if (SelectionBusy || !DraftCancellation.HasSelection(view, draft))
                return false;
            Back();
            return true;
        }

        private bool Drop(DraftToken source, DraftToken target)
        {
            if (sending || sender.Pending != null || policy.Busy || view == null)
                return false;
            if (draft.Kind == CommandKind.DiscardCards)
                return false;
            if (huntContext.Drop(source, target))
                return true;
            if (resources.Drop(source, target))
                return true;
            // UI-105: armies, figures, characters and Fellowship portraits move by clicks only; DraftInput never
            // drags them.
            if (source?.Object == DraftObject.Nation)
                return politics.Drop(source, target);
            if (muster.Active && source?.Object == DraftObject.Reserve)
                return muster.Drop(source, target);
            if (source?.Object == DraftObject.Card && draft.Kind is not (CommandKind.DiscardCards or CommandKind.DiscardProtection))
                return cards.Drop(source, target);
            if (!DraftGesture.Drop(draft, view, source, target))
            {
                tray.Preview(previewLegal, "That component cannot be used here.");
                return false;
            }
            notice = "";
            session.Edited(view);
            PersistDraft();
            if (source.Object == DraftObject.Card)
                board.Cards.Show(source.Id, true);
            var option = DraftOptions.For(view, draft);
            if (online && view.Revision >= requiredRevision && ComponentActions.Immediate(view, draft) && option != null && draft.Ready(option))
            {
                SubmitPreparedStep();
                return true;
            }
            Render();
            return true;
        }

        private void CancelRing() => Edit(() =>
        {
            draft.Clear();
            EnsureMandatoryDraft();
        });

        private void ChooseRing(string id)
        {
            // UI-161: the armed ring clicked again cancels its gesture.
            if (RingChoice.Armed(draft) && draft.Payload.RingId == id)
            {
                CancelRing();
                return;
            }
            if (resources.Ring(id))
                return;
            if (draft.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardLore && CanChoose(draft.Kind.Value))
            {
                if (id.Length == 0 || DraftOptions.For(view, draft).EligibleIds.Contains(id))
                    Edit(() => draft.Change(p => p.RingId = id));
                return;
            }
            if (CanChoose(CommandKind.UseRing))
                Edit(() =>
                {
                    if (draft.Kind != CommandKind.UseRing)
                        DraftGesture.Choose(draft, view, CommandKind.UseRing);
                    draft.Change(p => p.RingId = id);
                });
        }

        private void ChooseRulerToken(string id) => Edit(() =>
        {
            var die = draft.Payload.DieId;
            var token = draft.Payload.RulerTokenId == id ? "" : id;
            DraftOptions.SelectToken(draft, view, token);
            if (view.You == Side.FreePeoples && view.Dice.Any(d => d.Id == die && d.Owner == view.You && d.Location == DieLocation.Available))
                draft.Change(p => p.DieId = die);
        });

        public void Update(PlayerView latest)
        {
            if (latest == null)
                return;
            version ??= new ViewVersion(latest.MatchId);
            if (!version.IsNewer(latest))
                return;
            view = latest;
            if (view.Revision != version.Revision)
            {
                notice = session.Update(view);
                EnsureMandatoryDraft();
                PersistDraft();
            }
            Render();
            SignalTurn();
            // QUAL-09: recorded only once rendered, so a failed render can be repeated with the same view.
            version.Commit(latest);
            RecoverPending();
            SkipEmptyCombatChoice();
        }

        // UI-167: the ribbon (or the console's flash) when this view passes the action to the player.
        private void SignalTurn()
        {
            var text = signal.Observe(view);
            if (text.Length > 0)
                board.SignalTurn(text, view.You);
        }

        // UI-151: a Combat choice without a legal card can only be No card, so it is sent without a click, once per
        // decision, through the No card button's own path.
        private void SkipEmptyCombatChoice()
        {
            if (!online || view?.Decision?.Kind != DecisionKind.CombatCard || view.DecisionOwner != view.You ||
                skippedCombatChoice == view.Decision.Id)
                return;
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.CommitCombatCard);
            if (option == null || option.EligibleIds.Count > 0 || !CanChoose(CommandKind.CommitCombatCard))
                return;
            skippedCombatChoice = view.Decision.Id;
            DirectBattleChoice(CommandKind.CommitCombatCard);
        }

        private void EnsureMandatoryDraft()
        {
            if (view.Decision == null)
                return;
            if (!draft.Kind.HasValue)
                session.Edited(view);
            if (!draft.Kind.HasValue && view.Decision.Kind == DecisionKind.ActionSequence && view.ActionSequence is { } sequence)
            {
                draft.Choose(sequence.Kind, sequence.DieId);
                draft.Change(p =>
                {
                    p.Stepwise = true;
                    p.MovementCost = sequence.Face;
                    p.AbilityId = sequence.AbilityId;
                });
            }
            if (!draft.Kind.HasValue && view.Decision?.Kind == DecisionKind.AllocateHunt)
                draft.Choose(CommandKind.AllocateHunt);
            if (!draft.Kind.HasValue && view.Decision?.Kind == DecisionKind.DiscardCards)
                draft.Choose(CommandKind.DiscardCards);
            if (!draft.Kind.HasValue && view.Decision?.Kind == DecisionKind.RemoveExcess)
                draft.Choose(CommandKind.RemoveExcess);
            if (!draft.Kind.HasValue && view.Decision?.Kind == DecisionKind.EffectChoice)
                draft.Choose(CommandKind.ChooseEffect);
            if (draft.Kind.HasValue)
                return;
            var mandatory = view.Decision?.Kind switch
            {
                DecisionKind.FellowshipRoute => CommandKind.RevealFellowship,
                DecisionKind.Guide => CommandKind.SelectGuide,
                DecisionKind.CompanionSeparation => view.Options.FirstOrDefault(o => o.Kind is CommandKind.SeparateCompanions or CommandKind.MoveCompanions)?.Kind,
                DecisionKind.HuntReroll => CommandKind.RerollHunt,
                DecisionKind.HuntDamage => CommandKind.ResolveHuntDamage,
                DecisionKind.CorruptionTile => view.Options.First(o => o.Enabled).Kind,
                DecisionKind.CouncilVersions => CommandKind.ChooseCompanionVersions,
                DecisionKind.CouncilHomelands => CommandKind.ChooseCouncilHomelands,
                DecisionKind.CouncilToken => CommandKind.ChooseCouncilToken,
                DecisionKind.SpecialDieChoice => CommandKind.ChooseSpecialDie,
                DecisionKind.Dismay when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseDismay) => CommandKind.UseDismay,
                DecisionKind.KeepActionDie when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseVilya) => CommandKind.UseVilya,
                DecisionKind.Reaction when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.RespondEffect) => CommandKind.RespondEffect,
                DecisionKind.RulerDiscard => CommandKind.DiscardRulerDie,
                DecisionKind.CombatCard => CommandKind.CommitCombatCard,
                DecisionKind.Losses => view.EventLoss != null ? CommandKind.TakeEventLosses : CommandKind.TakeBattleLosses,
                DecisionKind.BattleReroll => CommandKind.RerollBattle,
                DecisionKind.BattleAdvance => CommandKind.AdvanceBattle,
                DecisionKind.BattleRetreat when view.Options.Any(o => o.Kind == CommandKind.RetreatBattle) => CommandKind.RetreatBattle,
                DecisionKind.SiegeContinuation => CommandKind.ExtendSiege,
                DecisionKind.ArmyEvent => view.Options.FirstOrDefault(o => o.Enabled && o.ArmyEvent != null)?.Kind,
                DecisionKind.CharacterEvent => CommandKind.MoveShadowCharacters,
                _ => (CommandKind?)null
            };
            if (mandatory.HasValue)
                DraftGesture.Choose(draft, view, mandatory.Value);
        }

        public void SetConnection(string status)
        {
            var connected = status == "Connected";
            if (online != connected)
                connectionEpoch++;
            if (online && !connected)
                signal.Rebase();
            online = connected;
            if (view != null)
                Render();
            RecoverPending();
            SkipEmptyCombatChoice();
        }

        private void RecoverPending()
        {
            if (!online || view == null)
                return;
            if (sender.Pending != null && !policy.Busy)
                _ = Submit();
            else
                policy.Recover();
        }

        private void PolicyChanged()
        {
            Render();
            if (!policy.Busy)
                RecoverPending();
        }

        private void PersistDraft() => persistence?.SaveDraft(session.Save());

        private bool CanChoose(CommandKind kind) => !sending && sender.Pending == null && !policy.Busy &&
            view != null && view.Revision >= requiredRevision && DraftOptions.All(view, draft).Any(o => o.Kind == kind && o.Enabled);

        // UI-214: an option of a card or a reaction chosen in the console (or a table card at its ×) completes the
        // decision, so it is sent at once when the draft is ready.
        private void ChooseConsoleOption(ConsoleOption choice)
        {
            if (SelectionBusy || view?.Decision == null)
                return;
            if (choice.Kind == ConsoleOptionKind.Decline)
            {
                Choose(CommandKind.DeclineReaction);
                return;
            }
            if (choice.Kind == ConsoleOptionKind.Action && choice.Command.HasValue)
            {
                Choose(choice.Command.Value);
                return;
            }
            if (choice.Kind == ConsoleOptionKind.Reaction)
            {
                CompleteChoice(() =>
                {
                    if (draft.Kind != CommandKind.RespondEffect)
                        draft.Choose(CommandKind.RespondEffect);
                    draft.Change(p => p.ReactionId = choice.Id);
                });
                return;
            }
            var option = DraftOptions.For(view, draft);
            if (option != null && !draft.Payload.Ids.Contains(choice.Id))
                CompleteChoice(() => CardDraft.Choose(draft, option, choice.Id));
        }

        private void Choose(CommandKind kind)
        {
            if (!CanChoose(kind))
                return;
            notice = "";
            DraftGesture.Choose(draft, view, kind);
            session.Edited(view);
            PersistDraft();
            if (kind is CommandKind.Pass or CommandKind.ContinueFellowship or CommandKind.DeclineReaction or CommandKind.FinishActionSequence ||
                BattleDraft.IsImmediate(kind) || ExpansionDecision.IsImmediate(kind))
                _ = Submit(true); // A simple decision is already the explicit commit; no second confirmation.
            else
                Render();
        }

        private void DirectBattleChoice(CommandKind kind)
        {
            if (!CanChoose(kind))
                return;
            // These labelled buttons explicitly commit an empty card/advance selection.
            if (kind is CommandKind.CommitCombatCard or CommandKind.AdvanceBattle)
            {
                draft.Choose(kind);
                session.Edited(view);
                PersistDraft();
                _ = Submit(true);
            }
            else
                Choose(kind);
        }

        private void ChooseDie(string id)
        {
            if (SelectionBusy || view?.Decision == null)
                return;
            if (view.Decision.Kind == DecisionKind.ActionSequence)
                return;
            if (huntContext.Die(id))
                return;
            if (resources.Die(id))
                return;
            if (view.Decision.Kind == DecisionKind.Action && ActionDieContext.DirectIntent(draft) &&
                draft.Payload.DieId.Length > 0 && draft.Payload.RulerTokenId.Length == 0 && draft.Payload.AbilityId.Length == 0 &&
                view.Dice.Any(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available))
            {
                Edit(() => { draft.Clear(); draft.Change(p => p.DieId = id); });
                return;
            }
            if (muster.Active && view.Dice.Any(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available))
            {
                Edit(() => { draft.Clear(); draft.Change(p => p.DieId = id); });
                return;
            }
            if (travel.Active && view.Dice.Any(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available))
            {
                Edit(() => { draft.Clear(); draft.Change(p => p.DieId = id); });
                return;
            }
            if (view.Decision.Kind == DecisionKind.Action && draft.Payload.DieId.Length > 0 && (draft.Payload.Stepwise || context.ArmyActive) && draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy &&
                view.Dice.Any(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available))
            {
                Edit(() => { draft.Clear(); draft.Change(p => p.DieId = id); });
                return;
            }
            if (draft.Kind == CommandKind.ChooseEffect && view.Decision.Presentation is "ActionDie" or "DieAndResult")
            {
                var effect = DraftOptions.For(view, draft);
                if (effect.EligibleIds.Contains(id))
                    EditChoice(() => CardDraft.Choose(draft, effect, id));
                return;
            }
            if (draft.Kind == CommandKind.PlayEvent)
            {
                var cardOption = DraftOptions.For(view, draft);
                Edit(() => CardDraft.SelectDie(draft, cardOption, id));
                return;
            }
            if (!draft.Kind.HasValue && view.Dice.Any(d => d.Id == id && d.Owner == view.You && d.Location == DieLocation.Available))
            {
                Edit(() => draft.Change(p => { p.DieId = id; p.Face = null; p.AbilityId = ""; }));
                return;
            }
            var option = DraftOptions.For(view, draft);
            if (option?.EligibleDice.Contains(id) == true)
                Edit(() => draft.Change(p =>
                {
                    p.DieId = id;
                    CardDrawDraft.MatchCost(draft, option);
                    if (draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
                        ArmyDraft.MatchCost(draft, option);
                }));
        }

        private void Edit(Action change)
        {
            if (view == null || SelectionBusy)
                return;
            notice = "";
            change();
            session.Edited(view);
            PersistDraft();
            Render();
        }

        private void ShowChoiceProblem(string message)
        {
            generation++;
            previewLifetime?.Cancel();
            previewLegal = false;
            notice = message;
            tray.Notice(message);
        }

        private void EditChoice(Action change) => EditSelection(change, false);

        private void CompleteChoice(Action change) => EditSelection(change, true);

        private void EditSelection(Action change, bool complete)
        {
            if (view == null || SelectionBusy)
                return;
            notice = "";
            change();
            session.Edited(view);
            PersistDraft();
            var option = DraftOptions.For(view, draft);
            if ((complete || draft.Kind == CommandKind.AwakenSovereign || CardActions.SingleEffect(view, draft) || FellowshipActions.Immediate(draft.Kind) ||
                ExpansionActions.Immediate(draft.Kind) || ComponentActions.Immediate(view, draft)) &&
                online && option != null && draft.Ready(option))
                SubmitPreparedStep();
            else
                Render();
        }

        private void Render()
        {
            if (view == null)
                return;
            var directArmy = RefreshContexts();
            previewLifetime?.Cancel();
            previewLifetime?.Dispose();
            previewLifetime = null;
            previewLegal = false;
            var currentGeneration = ++generation;
            RenderLayout();
            if (!online)
            {
                tray.Notice("Offline · Your draft is saved locally.");
                return;
            }
            if (view.Revision < requiredRevision)
            {
                tray.Notice("Refreshing the saved result…");
                return;
            }
            if (sending || sender.Pending != null || policy.Busy)
                return;
            if (!draft.Kind.HasValue && board.ActionDie.Choices.Count > 0)
            {
                tray.Preview(false, "Choose a highlighted object · Its gesture determines how this die is used.");
                return;
            }
            if (directArmy)
            {
                tray.Preview(false, notice.Length > 0 ? notice : ArmyActionTargets.Hint(draft, view, board.ActionDie));
                return;
            }
            if (tray.DirectCard)
            {
                tray.Preview(false, notice.Length > 0 ? notice : draft.Kind == CommandKind.PlayEvent
                    ? "Choose a highlighted nation." : "Choose a deck.");
                return;
            }
            if (muster.Active && draft.Kind is null or CommandKind.Recruit or CommandKind.AdvancePolitics)
                tray.Preview(false, notice.Length > 0 ? notice : draft.Kind == CommandKind.AdvancePolitics
                    ? "Choose the highlighted space on this nation’s track."
                    : draft.ReserveId.Length > 0 ? "Choose a highlighted settlement."
                    : view.Decision?.Kind == DecisionKind.ActionSequence ? "Choose a reserve figure, then a highlighted settlement · ✓ ends early."
                    : "Choose a reserve figure in Reinforcements, or a nation’s arrow in Politics.");
            else if (politics.Active)
                tray.Preview(false, PoliticalActions.Hint(view, draft, board.ActionDie));
            else
                RenderHints();
            if (draft.Kind.HasValue)
            {
                var option = DraftOptions.For(view, draft);
                if (option != null && draft.Ready(option) && (draft.Kind != CommandKind.MoveArmies || ArmyDraft.Problem(draft, view).Length == 0) &&
                    (draft.Kind != CommandKind.AttackArmy || AttackDraft.Problem(draft, view).Length == 0))
                {
                    previewLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                    _ = Preview(currentGeneration, previewLifetime.Token);
                }
            }
        }

        private bool RefreshContexts()
        {
            context.Refresh(view, draft);
            cards.Refresh(view, draft);
            battles.Refresh(view, draft);
            muster.Refresh(view, draft);
            reinforcements.Refresh(view, draft);
            politics.Refresh(view, draft);
            travel.Refresh(view, draft);
            flights.Refresh(view, draft);
            cardFigures.Refresh(view, draft);
            cardArmies.Refresh(view, draft);
            fellowshipContext.Refresh(view, draft);
            huntContext.Refresh(view, draft);
            resources.Refresh(view, draft);
            components.Refresh(view, draft, !SelectionBusy);
            consoleOptions.Refresh(view, draft, !SelectionBusy);
            board.CompanionSetup.Context(view);
            var directArmy = context.ArmyActive && draft.Kind is null or CommandKind.MoveArmies or CommandKind.AttackArmy ||
                view.Decision?.Kind == DecisionKind.ActionSequence && view.ActionSequence?.Kind == CommandKind.MoveArmies;
            board.Root.EnableInClassList("direct-army", directArmy || travel.Active);
            board.Root.EnableInClassList("direct-muster", muster.Active || politics.Active);
            // UI-136: before an action is chosen, every control the die can use glows.
            board.DieGlow.Show(draft.Kind.HasValue ? DieOptions.None : DieOptions.For(view, draft, board.ActionDie));
            board.OwnDice.EnableInClassList(DiceDecision.ClassName, DiceDecision.Active(view, draft));
            board.Root.EnableInClassList("fellowship-context", FellowshipActions.UsesPanel(view, draft) || HuntActions.UsesPanel(view, draft));
            tray.DirectArmy = directArmy;
            tray.DirectCard = draft.Kind is CommandKind.PlayEvent or CommandKind.DrawCard;
            tray.DirectMuster = muster.Active && draft.Kind is null or CommandKind.Recruit;
            tray.DirectPolitics = politics.Active && draft.Kind is null or CommandKind.AdvancePolitics or CommandKind.UseCouncilToken;
            tray.DirectTravel = travel.Active;
            tray.ContextualFellowship = FellowshipActions.UsesPanel(view, draft) || HuntActions.UsesPanel(view, draft);
            tray.ContextualHunt = HuntActions.UsesPanel(view, draft);
            tray.ContextualCharacter = CharacterRosterActions.OwnsDecision(view, draft);
            return directArmy;
        }

        private void RenderLayout()
        {
            var allocation = HuntActions.Allocation(view);
            foreach (var button in board.OwnDice.Query<Button>().ToList())
            {
                if (button.userData is not DraftToken token)
                    continue;
                var selected = token.Object == DraftObject.Die
                    ? token.Id == draft.Payload.DieId || draft.Payload.Ids.Contains(token.Id)
                    : token.Object == DraftObject.RulerToken ? token.Id == draft.Payload.RulerTokenId ||
                        draft.Kind == CommandKind.UseDismay && draft.Payload.Ids.Contains(token.Id)
                    : token.Object == DraftObject.Ring && token.Id == draft.Payload.RingId;
                button.EnableInClassList("selected", selected);
                if (token.Object == DraftObject.Die)
                {
                    var die = view.Dice.FirstOrDefault(d => d.Id == token.Id);
                    if (die != null)
                        button.tooltip = DieHints.Describe(view, die) + AllocationHint(allocation, token.Id);
                }
                if (token.Object == DraftObject.Die && view.Decision?.Presentation is "ActionDie" or "DieAndResult")
                    button.SetEnabled(view.Options.Any(o => o.Kind == CommandKind.ChooseEffect && o.EligibleIds.Contains(token.Id)));
            }
            var fellowship = FellowshipDraft.UsesFellowship(draft.Kind) || view.Hunt != null;
            var battle = view.Battle != null || view.EventLoss != null;
            board.SetPlanning(board.ActionDie.DieId.Length > 0 || BoardDraft.UsesBoard(draft.Kind) || CardDraft.UsesCards(draft.Kind) || CharacterDraft.UsesCharacters(draft.Kind) ||
                ExpansionDraft.Handles(draft.Kind) || fellowship || battle);
            board.Root.EnableInClassList("expansion-planning", ExpansionDraft.Handles(draft.Kind));
            board.Root.EnableInClassList("council-planning", view.Decision?.Kind is DecisionKind.CouncilVersions or DecisionKind.CouncilHomelands);
            board.Root.EnableInClassList("companion-setup-context", CompanionSetupPresentation.Owns(view));
            board.Root.EnableInClassList("battle-planning", battle);
            board.Root.EnableInClassList("event-loss-planning", view.EventLoss != null);
            board.Root.EnableInClassList("loss-planning", draft.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses);
            board.Root.EnableInClassList("card-planning", CardDraft.UsesCards(draft.Kind));
            board.Root.EnableInClassList("army-planning", BoardDraft.UsesBoard(draft.Kind));
            board.Root.EnableInClassList("fellowship-planning", fellowship);
            board.Root.EnableInClassList("character-planning", CharacterDraft.UsesCharacters(draft.Kind));
            board.Root.EnableInClassList("character-action-context", CharacterRosterActions.OwnsDecision(view, draft));
            board.RefreshHand(draft);
            policy.Render(view, online, sending || sender.Pending != null);
            tray.Render(view, draft, sending || policy.Busy, sender.Pending != null && !sending, pendingProblem);
            console.Refresh(view, draft, online, sending || policy.Busy, sender.Pending != null);
            rings.Refresh(view, draft, online && !sending && !policy.Busy && sender.Pending == null);
            RevealRecovery();
            board.Map.DraftOverlay.Apply(view, draft, context.ArmyActive || muster.Active ? board.ActionDie : null);
        }

        private string AllocationHint(ActionOption allocation, string id)
        {
            var hint = allocation == null ? "" : HuntActions.PoolHint(draft, allocation, id);
            return hint.Length == 0 ? "" : "\n" + hint;
        }

        private void RevealRecovery()
        {
            var pending = sender.Pending?.CommandId ?? "";
            if (sending || pending == recoveryShown)
                return;
            recoveryShown = pending;
            if (pending.Length == 0)
                return;
            if (board.Tray.parent == board.Hunt.DecisionHost)
                board.Hunt.Open();
            else if (board.Tray.parent == board.ResourceDecision.DecisionHost)
                board.ResourceDecision.Open();
            else if (board.Tray.parent == board.Sovereigns.DecisionHost)
                board.Sovereigns.OpenDecision();
            else if (board.Tray.parent == board.TableEffects.DecisionHost)
                board.TableEffects.Open();
            else if (board.Tray.parent == board.Characters.DecisionHost)
                board.Characters.OpenDecision();
            else if (board.Tray.parent == board.CompanionSetup.DecisionHost)
                board.CompanionSetup.Open();
            else if (board.Tray.parent == board.Fellowship.Root)
                board.Fellowship.Open();
        }

        private void RenderHints()
        {
            if (!string.IsNullOrEmpty(persistence?.Error))
                tray.Notice(persistence.Error);
            if (notice.Length > 0)
                tray.Notice(notice);
            var boardHint = BoardDraft.Hint(draft, view);
            if (boardHint.Length > 0)
                tray.Preview(false, boardHint);
            var fellowshipHint = FellowshipDraft.Hint(draft, view);
            if (fellowshipHint.Length > 0)
                tray.Preview(false, fellowshipHint);
            var huntHint = draft.Kind == CommandKind.AllocateHunt && HuntActions.Allocation(view) is { } allocation
                ? HuntActions.Hint(draft, allocation) : "";
            if (huntHint.Length > 0)
                tray.Preview(false, huntHint);
            var battleHint = BattleDraft.Hint(draft, view);
            if (battleHint.Length > 0)
                tray.Preview(false, battleHint);
            if (draft.Kind == CommandKind.PlayEvent && draft.Payload.DieId.Length == 0 &&
                !DraftOptions.All(view, draft).Where(o => o.Kind == draft.Kind).SelectMany(o => o.Events).Any(e => e.CardId == draft.Payload.CardId && e.Costs.Any(c => c.DieId.Length == 0)))
                tray.Preview(false, draft.Payload.CardId.Length == 0 ? "Choose an available card from your hand." : "Choose an action die from your pool.");
        }

        private async Task Preview(int currentGeneration, CancellationToken cancellation)
        {
            try
            {
                var command = draft.Command(view);
                var preview = await api.Post<ActionPreview>($"api/matches/{view.MatchId}/preview", command, cancellation);
                if (generation != currentGeneration || lifetime.IsCancellationRequested)
                    return;
                previewLegal = preview.Legal && preview.Revision == command.ExpectedRevision &&
                    view.Revision == command.ExpectedRevision && view.Decision?.Id == command.DecisionId && draft.Kind == command.Kind;
                var summary = previewLegal ? TurnSummary.Preview(view, draft, preview, board.Presentation) : "";
                tray.Preview(previewLegal, previewLegal ? (notice.Length > 0 ? notice + " " + summary : summary) :
                    preview.Error == "MoreArmyMovesRequired" ? "Plan another army on the map to complete this card." : "This choice is no longer legal. Review your selection.",
                    previewLegal && BattleDraft.UsesBattle(draft.Kind) ? BattleSummary.LossDetails(view, preview) : "");
                if (previewLegal) tray.PreviewDetails(preview);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception)
            {
                if (generation == currentGeneration)
                    tray.Notice("Preview unavailable. Check the connection.");
            }
        }

        private async Task Submit(bool immediate = false)
        {
            if (sending || policy.Busy || !online || view == null || view.Revision < requiredRevision ||
                sender.Pending == null && (view.Decision == null || !draft.Kind.HasValue || !immediate && !previewLegal))
                return;
            sending = true;
            CommandSending?.Invoke();
            pendingProblem = "";
            var recovering = sender.Pending != null;
            var command = recovering ? sender.Pending : draft.Command(view);
            Render();
            try
            {
                var receipt = recovering ? await sender.Recover(lifetime.Token) : await sender.Send(command, lifetime.Token);
                if (lifetime.IsCancellationRequested)
                    return;
                requiredRevision = Math.Max(requiredRevision, receipt.Revision);
                CommandAnswered?.Invoke(command, receipt, recovering);
                if (receipt.Accepted)
                {
                    session.Complete(command);
                    if (view.Decision?.Id != command.DecisionId)
                        EnsureMandatoryDraft();
                    PersistDraft();
                    notice = "";
                }
                else
                    notice = "The state changed or this choice is not legal. Review your selection.";
                await connection.Refresh();
                if (!receipt.Accepted)
                    tray.Notice("The state changed or this choice is not legal. Review your selection.");
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (LocalSaveException error) { pendingProblem = error.Message; }
            catch (Exception) { /* Keep the exact pending command for receipt recovery, never invent a new ID. */ }
            finally
            {
                sending = false;
                if (!lifetime.IsCancellationRequested)
                    Render();
            }
        }

        // UI-91: an armed hand card's second click is the same final gesture as a double-click.
        private void FinalizeCard(DraftToken token) => cards.Play(token);

        private void SuspendPreviews(DraftToken source) => board.SuspendPreviews(source, input.DragSource);

        public void Dispose() => Dispose(true);

        public void Dispose(bool saveDraft)
        {
            if (persistence != null)
                persistence.Changed -= Render;
            if (saveDraft && view != null)
                PersistDraft();
            board.Escape.CancelGesture = null;
            board.Escape.ClearSelection = null;
            board.Escape.CancelChoice = null;
            input.Dispose();
            input.DragChanged -= board.Nations.PreviewDrag;
            input.DragChanged -= board.Battles.PreviewDrag;
            input.DragChanged -= board.HandDrawer.PreviewDrag;
            input.DragChanged -= board.Hover.PreviewDrag;
            input.DragChanged -= board.Map.HistoryOverlay.PreviewDrag;
            input.DragChanged -= SuspendPreviews;
            board.CardFinalized -= FinalizeCard;
            tray.CardFinalized -= FinalizeCard;
            policy.Changed -= PolicyChanged;
            policy.Dispose();
            board.DieSelected -= ChooseDie;
            board.RulerTokenSelected -= ChooseRulerToken;
            cards.Dispose();
            reinforcements.Dispose();
            politics.Dispose();
            fellowshipContext.Dispose();
            huntContext.Dispose();
            resources.Dispose();
            context.Dispose();
            board.Map.RegionSelected -= ChooseRegion;
            board.Map.ArmySelected -= ChooseArmy;
            components.Dispose();
            consoleOptions.Dispose();
            rings.Dispose();
            console.Dispose();
            board.Characters.EntryRequested -= RequestEntry;
            board.Characters.Enterable = _ => false;
            previewLifetime?.Cancel();
            previewLifetime?.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
