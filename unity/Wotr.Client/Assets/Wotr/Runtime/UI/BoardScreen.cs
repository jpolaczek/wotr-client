using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BoardScreen
    {
        private IVisualElementScheduledItem trackStage;
        // UI-310: the Fellowship as the last presentation left the tracks, and whether a Skip asked for no second stage.
        private FellowshipView presentedFellowship;
        private bool quietTracks;
        public VisualElement Root { get; } = new VisualElement { name = "board-screen" };
        public MapCanvas Map
        {
            get;
        }
        public VisualElement Tray { get; } = new VisualElement { name = "decision-tray", focusable = true };
        public VisualElement OwnDice { get; } = new VisualElement { name = "own-dice" };
        public VisualElement Hand { get; } = new VisualElement { name = "hand" };
        public VisualElement OpponentDice { get; } = new VisualElement { name = "opponent-dice" };
        public Label Phase { get; } = new Label();
        public Label Connection { get; } = new Label();
        public Label Tracks { get; } = new Label();
        public TopBarView TopBar { get; }
        public VictoryPanel Victory { get; }
        public BoardPresentation Presentation
        {
            get;
        }
        public bool IsFixture
        {
            get; private set;
        }
        public PlayerView View
        {
            get; private set;
        }
        public event Action InterfaceRequested;
        public event Action Reconnect;
        public event Action<string> DieSelected;
        public event Action<string> RulerTokenSelected;
        public event Action<string> CouncilTokenSelected;
        public event Action<string> CardSelected;
        // UI-91: the second click on an armed hand card; it takes the double-click path.
        public event Action<DraftToken> CardFinalized;
        public BoardInformation Information { get; }
        public BoardMenu Menu { get; } = new BoardMenu();
        public BoardEscape Escape { get; }
        public WindowFamily Family { get; }
        public RegionHover Hover { get; }
        public DecisionNotice Notice { get; }
        public ActionNoticeBar Notifications { get; }
        public ActionLogPanel Log { get; }
        public ActionContextRouter ActionHistory { get; private set; }
        public ActionDieContext ActionDie { get; } = new ActionDieContext();
        public ComponentActionView Components { get; } = new ComponentActionView();
        // UI-160: the turn console in the bottom row and the decision window that hosts choices without an object.
        public TurnConsole Console { get; } = new TurnConsole();
        public TurnRibbon Ribbon { get; } = new TurnRibbon();
        public GameEndScreen GameEnd { get; } = new GameEndScreen();
        // CHAT-01: the board's chat at the bottom left, beside the map legend.
        public GameChatPanel GameChat { get; } = new GameChatPanel();
        public GameDeletedNotice GameDeleted { get; } = new GameDeletedNotice();
        public DecisionWindow DecisionWindow { get; } = new DecisionWindow();
        // UI-161: the results an Elven Ring can give the chosen die, above that die.
        public RingFacePicker RingPicker { get; } = new RingFacePicker();
        // UI-272: the new results of a die changed by The Lore of the Elven Rings.
        public RingFacePicker LorePicker { get; } = new RingFacePicker("lore-face-picker");
        public PoliticalPanel Nations { get; }
        private readonly Label loading = new Label("Loading match…") { name = "match-loading" };
        public CardInspector Cards { get; } = new CardInspector();
        public HandDrawer HandDrawer { get; }
        // UI-326: two card backs dissolving where discarded cards leave.
        public CardDissolve Dissolve { get; }
        // UI-332: a tile the Hunt pool gains shows its face and drops into the Hunt plaque.
        public HuntTileDrop HuntDrops { get; }
        public DeckView Decks { get; } = new DeckView();
        public CardCostPicker CardCosts { get; } = new CardCostPicker();
        private readonly HandView handView;
        private readonly TableEffectsView tableEffects;
        private readonly MapEffectsOverlay mapEffects;
        private readonly EffectsScopeBadges effectScopes;
        public TableEffectsView TableEffects => tableEffects;
        private readonly ActionPoolView actionPool;
        // UI-130: reserves of every own nation during a Muster action.
        public ReinforcementsPanel Reinforcements { get; } = new ReinforcementsPanel();
        // UI-136: halos on the controls the chosen action die can use.
        public DieOptionGlow DieGlow { get; } = new DieOptionGlow();

        // UI-218: P switches the political view, unless the key types into a text field.
        private void PoliticalViewKey(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.P || e.actionKey || e.altKey || e.shiftKey)
                return;
            var target = e.target as VisualElement;
            if (target is TextField || target?.GetFirstAncestorOfType<TextField>() != null)
                return;
            Map.SetPoliticalView(!Map.PoliticalView);
            e.StopPropagation();
        }

        private void AttachDieGlows()
        {
            // UI-193: the left rail tabs glow gold round their medallions (Styles/LeftRail.uss), with no halo.
            DieGlow.Light(Characters.Tab, options => options.Characters);
            DieGlow.Light(Nations.Root, options => options.Politics);
            // UI-181: the Reserve tab lights for a die that can recruit, like the other tabs.
            DieGlow.Light(Reinforcements.Tab, options => options.Recruit);
            DieGlow.Attach(Fellowship.Status, options => options.Fellowship);
            foreach (var deck in Decks.Root.Query<Button>().ToList())
                DieGlow.Attach(deck, options => options.Draw);
        }

        // UI-134: the Reinforcements panel presses hand cards through the hand's own selection.
        public SovereignPanel Sovereigns { get; }
        public CharacterRosterPanel Characters { get; }
        public FellowshipPanel Fellowship { get; }
        public HuntPanel Hunt { get; }
        public BattleReport BattleReport { get; }
        public TestSessionBar TestBar { get; } = new TestSessionBar();
        // OBS-03: shown only on an observer's board.
        public ViewingBar Viewing { get; } = new ViewingBar();
        // REPLAY-05: shown only on a replay's board.
        public ReplayBar Replay { get; } = new ReplayBar();
        public bool Observing { get; private set; }
        private bool quiet;
        private bool figuresLive;
        public RegionalBattlePanel Battles { get; }
        public BattlePolicyControl BattlePolicy { get; } = new BattlePolicyControl();
        public void RefreshHand(TurnDraft draft) => handView.Refresh(draft, ActionDie);
        public string ExpandedCard => handView.ExpandedCard;
        // Unrelated previews step aside; a panel hosting the drag stays until the gesture ends.
        public void SuspendPreviews(DraftToken source, VisualElement origin)
        {
            handView.Suspend(source != null);
            Edges.Preview.Suspend(source != null, origin);
        }
        public void SetPlanning(bool active)
        {
            Root.EnableInClassList("board-planning", active);
        }
        public WindowLayout Windows { get; }
        public BoardEdgeLayout Edges { get; }

        public BoardScreen(WindowLayout windows = null)
        {
            Windows = windows ?? new WindowLayout();
            Presentation = BoardPresentation.Load();
            Presentation.ClearGameState();
            Root.AddToClassList("board-screen");
            Escape = new BoardEscape(this);
            Root.RegisterCallback<GeometryChangedEvent>(e =>
                Root.EnableInClassList("short-viewport", e.newRect.height > 0 && e.newRect.height < 700));
            TopBar = new TopBarView(Phase, OpponentDice, Connection, Menu.Toggle);
            var top = TopBar.Root;
            Menu.SettingsRequested += () => InterfaceRequested?.Invoke();
            var stage = new VisualElement { name = "board-stage", pickingMode = PickingMode.Ignore };
            stage.AddToClassList("stage");
            stage.Add(TopBar.Shadow);
            Map = new MapCanvas(Presentation);
            Map.Legend.SetOverlayHost(stage);
            stage.Add(GameChat.Root);
            stage.Add(GameChat.Icon);
            Root.RegisterCallback<KeyDownEvent>(PoliticalViewKey);
            Root.Add(Map);
            Root.Add(top);
            actionPool = new ActionPoolView(id => DieSelected?.Invoke(id), id => RulerTokenSelected?.Invoke(id));
            actionPool.CouncilChosen = id => CouncilTokenSelected?.Invoke(id);
            Sovereigns = new SovereignPanel(Presentation, id => Map.Focus(id));
            // UI-274: the kings' map objects light and act like their portraits.
            Sovereigns.Lit += Map.LightSovereigns;
            Map.SovereignMarkers.Selected += Sovereigns.Click;
            Root.Add(stage);
            // UI-100: no map control row. The wheel zooms, the Fellowship panel locates,
            // and the log and last battle icons join the notice stack (ActionNoticeBar).
            BattleReport = new BattleReport(Cards, Presentation);
            Information = new BoardInformation(Presentation);
            Log = new ActionLogPanel(Presentation);
            Log.Card.Archive.Opened += report => BattleReport.ShowFrom(report, Log.Popup);
            Log.CardRequested += id => Cards.Show(id, keep: true, allowPlay: false);
            // UI-169: the end of the game covers the whole board; its buttons reuse the Log and the menu's exit.
            GameEnd.LogRequested += Log.Open;
            GameEnd.MenuRequested += Menu.LeaveToGames;
            Log.Toggle.SetEnabled(false);
            BattleReport.Open.clicked += Information.Close;
            Notice = new DecisionNotice(this);
            Notifications = new ActionNoticeBar(this);
            Notifications.Root.AddToClassList("map-edge-surface");
            // UI-103: the notices hang under the top bar over the map, outside the layout, above the stage.
            Root.Insert(Root.IndexOf(stage) + 1, Notifications.Root);
            // TEST-03: hidden outside test games.
            Root.Insert(Root.IndexOf(Notifications.Root) + 1, TestBar.Root);
            Root.Insert(Root.IndexOf(TestBar.Root) + 1, Viewing.Root);
            Root.Insert(Root.IndexOf(Viewing.Root) + 1, Replay.Root);
            Log.CardBounds = CardBounds;
            Nations = new PoliticalPanel(Root, Presentation.Board);
            stage.Add(Nations.Root);
            stage.Add(Nations.Drawer);
            Fellowship = new FellowshipPanel(FocusFellowship, Map);
            Fellowship.Root.style.display = DisplayStyle.None;
            stage.Add(Fellowship.Status);
            stage.Add(Fellowship.Root);
            Hunt = new HuntPanel(Map, Presentation);
            Console.Hunt.Attach(Hunt.Tab);
            stage.Add(Hunt.Root);
            Victory = new VictoryPanel(Root, Map, () => Fellowship.Open(), Windows);
            Victory.KeepShortcutClearOf(Hand);
            Victory.KeepShortcutClearOf(Cards.Root);
            Victory.KeepShortcutClearOf(Fellowship.Root);
            Victory.KeepShortcutClearOf(Hunt.Root);
            stage.Add(Victory.Tab);
            stage.Add(Victory.Root);
            stage.Add(Victory.Banner);
            Tracks.AddToClassList("tracks");
            stage.Add(Tracks);
            tableEffects = new TableEffectsView(Root, Map);
            mapEffects = new MapEffectsOverlay(Map);
            effectScopes = new EffectsScopeBadges(this);
            stage.Add(tableEffects.Tab);
            stage.Add(tableEffects.Root);
            stage.Add(BattleReport.Root);
            stage.Add(Information.Root);
            Victory.KeepShortcutClearOf(tableEffects.Root);
            stage.Add(Sovereigns.Inspector);
            stage.Add(Reinforcements.Root);
            stage.Add(Cards.Root);
            stage.Add(Log.Root);
            stage.Add(Log.Popup);
            stage.Add(Notifications.Overflow);
            stage.Add(Ribbon.Root);
            stage.RegisterCallback<GeometryChangedEvent>(_ => Cards.FitViewport());
            HandDrawer = new HandDrawer(Root, Hand, Cards);
            stage.Add(HandDrawer.Tab);
            stage.Add(Hand);
            Hover = new RegionHover(Root, stage, Map);
            DecisionWindow.Opening += Hover.Close;
            DecisionWindow.Opening += Cards.CloseTransient;
            Hover.Panel.EffectInspected += tableEffects.Inspect;
            tableEffects.Opening += Hover.Close;
            tableEffects.Opening += Log.CloseTransient;
            tableEffects.Opening += Nations.CloseTransient;
            tableEffects.Opening += Fellowship.CloseTransient;
            tableEffects.Opening += Hunt.CloseTransient;
            tableEffects.Opening += HandDrawer.CloseTransient;
            tableEffects.Opening += Cards.CloseTransient;
            Hover.Opening += tableEffects.CloseTransient;
            Nations.Opening += tableEffects.CloseTransient;
            Fellowship.Opening += tableEffects.CloseTransient;
            Hunt.Opening += tableEffects.CloseTransient;
            HandDrawer.Opening += tableEffects.CloseTransient;
            Victory.Opening += Hover.Close;
            // UI-169: the score panel opened from the menu replaces the end-of-game screen, as View board does.
            Victory.Opening += GameEnd.Close;
            Victory.Opening += Cards.CloseTransient;
            Victory.Opening += Log.CloseTransient;
            Victory.Opening += Nations.CloseTransient;
            Victory.Opening += Fellowship.CloseTransient;
            Victory.Opening += Hunt.CloseTransient;
            Victory.Opening += HandDrawer.CloseTransient;
            Victory.Opening += tableEffects.CloseTransient;
            Hover.Opening += Victory.CloseTransient;
            Cards.Opening += Victory.CloseTransient;
            Nations.Opening += Victory.CloseTransient;
            Fellowship.Opening += Victory.CloseTransient;
            Hunt.Opening += Victory.CloseTransient;
            HandDrawer.Opening += Victory.CloseTransient;
            tableEffects.Opening += Victory.CloseTransient;
            Log.CardOpening += Victory.CloseTransient;
            Hover.Panel.Root.PlaceBehind(Cards.Root);
            Cards.Opening += Hover.Close;
            Cards.Opening += Log.CloseTransient;
            Hover.Opening += Log.CloseTransient;
            Nations.Opening += Log.CloseTransient;
            Fellowship.Opening += Log.CloseTransient;
            Hunt.Opening += Log.CloseTransient;
            HandDrawer.Opening += Log.CloseTransient;
            Log.CardOpening += Hover.Close;
            Log.CardOpening += Cards.CloseTransient;
            Log.CardOpening += Nations.CloseTransient;
            Log.CardOpening += Fellowship.CloseTransient;
            Log.CardOpening += Hunt.CloseTransient;
            Log.CardOpening += HandDrawer.CloseTransient;
            Hover.Opening += Cards.CloseTransient;
            Cards.Opening += Nations.CloseTransient;
            Hover.Opening += Nations.CloseTransient;
            Nations.Opening += Hover.Close;
            Nations.Opening += Cards.CloseTransient;
            Fellowship.Opening += Hover.Close;
            Fellowship.Opening += Cards.CloseTransient;
            Fellowship.Opening += Nations.CloseTransient;
            HandDrawer.Opening += Hover.Close;
            HandDrawer.Opening += Nations.CloseTransient;
            Nations.Opening += HandDrawer.CloseTransient;
            Hover.Opening += HandDrawer.CloseTransient;
            Fellowship.Opening += HandDrawer.CloseTransient;
            Hunt.Opening += Hover.Close;
            Hunt.Opening += Cards.CloseTransient;
            Hunt.Opening += Nations.CloseTransient;
            Hunt.Opening += HandDrawer.CloseTransient;
            Hunt.Opening += Fellowship.CloseTransient;
            Fellowship.Opening += Hunt.CloseTransient;
            Hover.Opening += Hunt.CloseTransient;
            Cards.Opening += () => { if (!Cards.Inspecting(Hunt.Root)) Hunt.CloseTransient(); };
            Hunt.HasInspection = () => Cards.Inspecting(Hunt.Root) && Cards.Root.style.display.value != DisplayStyle.None;
            Sovereigns.Opening += Hover.Close;
            Sovereigns.Opening += Cards.CloseTransient;
            Sovereigns.Opening += Nations.CloseTransient;
            Sovereigns.Opening += Hunt.CloseTransient;
            Sovereigns.Opening += Fellowship.CloseTransient;
            Sovereigns.Opening += HandDrawer.CloseTransient;
            Sovereigns.Opening += tableEffects.CloseTransient;
            Sovereigns.Opening += Victory.CloseTransient;
            Hover.Opening += Sovereigns.CloseTransient;
            Cards.Opening += Sovereigns.CloseTransient;
            Nations.Opening += Sovereigns.CloseTransient;
            Hunt.Opening += Sovereigns.CloseTransient;
            Fellowship.Opening += Sovereigns.CloseTransient;
            HandDrawer.Opening += Sovereigns.CloseTransient;
            tableEffects.Opening += Sovereigns.CloseTransient;
            Victory.Opening += Sovereigns.CloseTransient;
            loading.AddToClassList("match-loading");
            stage.Add(loading);
            stage.Add(DecisionWindow.Root);
            var decisionDock = DecisionWindow.Dock;
            decisionDock.Add(CardCosts.Root);
            decisionDock.Add(Components.Root);
            Battles = new RegionalBattlePanel(Tray, decisionDock, Map);
            Battles.AttachPolicy(BattlePolicy.Root);
            _ = new BattleWindowHover(Root, Map, Battles, Cards);
            Hover.Excluded = Battles.IsRegion;
            Battles.Opening += Hover.Close;
            Battles.Opening += Nations.CloseTransient;
            Battles.Opening += HandDrawer.CloseTransient;
            Battles.Opening += Victory.CloseTransient;
            Victory.Opening += Battles.CloseTransient;
            Fellowship.Attach(Tray, decisionDock);
            Hunt.Attach(Tray, decisionDock);
            tableEffects.Attach(Tray, decisionDock);
            Map.RegisterCallback<GeometryChangedEvent>(_ => Sovereigns.RequestPosition());
            stage.Insert(stage.IndexOf(Cards.Root), Battles.Root);
            var overlays = new[] { Map.Legend.Toggle, Map.Legend.PoliticalToggle, Map.Legend.Popup, GameChat.Icon, GameChat.Root, Nations.Root, Nations.Drawer, Sovereigns.Inspector, Reinforcements.Root,
                Fellowship.Root, Fellowship.Status, Hunt.Root, Hunt.Tab, Tracks, Information.Root, tableEffects.Tab, tableEffects.Root, BattleReport.Root, Battles.Root, Cards.Root,
                HandDrawer.Tab, Hand, Log.Root, Log.Popup, Notifications.Overflow, Victory.Tab, Victory.Root, Victory.Banner, DecisionWindow.Root };
            // UI-322: a panel fading out is closed, so it occupies nothing.
            bool Occupied(VisualElement element) => !Appearance.Closed(element) && element.resolvedStyle.display != DisplayStyle.None &&
                element.resolvedStyle.visibility == Visibility.Visible && element.worldBound.width > 0 && element.worldBound.height > 0 &&
                (element.childCount > 0 || element is TextElement);
            Sovereigns.Occlusions = () => overlays.Where(element => element != Sovereigns.Inspector && Occupied(element))
                .Select(element => element.worldBound);
            // UI-98: the Fellowship and Hunt plaques sit in the corner tab, a summary one hover away from the full
            // panels; like the hand, a decision window may cover it when the board has no other room (overlays).
            var fixedControls = new VisualElement[] { Map.Legend.Toggle, Map.Legend.PoliticalToggle, GameChat.Icon, Nations.Root,
                tableEffects.Tab, HandDrawer.Tab, Victory.Tab, Victory.Banner };
            foreach (var control in fixedControls)
            {
                control.AddToClassList("map-edge-control");
                if (control != Victory.Tab && control != Victory.Banner)
                    Victory.KeepShortcutClearOf(control);
            }
            Sovereigns.FixedOcclusions = () => fixedControls.Where(Occupied)
                .Select(element => element.worldBound);
            foreach (var element in overlays)
            {
                element.RegisterCallback<GeometryChangedEvent>(_ => Map.Refresh());
                if (element != Sovereigns.Inspector)
                    element.RegisterCallback<GeometryChangedEvent>(_ => Sovereigns.RequestPosition());
            }
            Connection.RegisterCallback<ClickEvent>(_ => Reconnect?.Invoke());
            var bottom = Row("bottom");
            bottom.AddToClassList("map-edge-surface");
            OwnDice.AddToClassList("dice-panel");
            Tray.AddToClassList("tray");
            Tray.AddToClassList("map-edge-focus-anchor");
            Hand.AddToClassList("hand");
            handView = new HandView(Hand, Cards, Decks, HandDrawer, id => CardSelected?.Invoke(id), token => CardFinalized?.Invoke(token))
            {
                Blocked = () => Map.Panning
            };
            bottom.Add(OwnDice);
            decisionDock.Add(Tray);
            bottom.Add(Console.Root);
            Root.Add(bottom);
            Root.Add(RingPicker.Root);
            Root.Add(LorePicker.Root);
            Root.Add(GameEnd.Root);
            var mapOcclusions = overlays.Concat(new[] { top, Notifications.Root, bottom }).ToArray();
            Map.Occlusions = () => mapOcclusions.Where(element => !Appearance.Closed(element) && element.resolvedStyle.display != DisplayStyle.None &&
                    element.worldBound.width > 0)
                .Select(element => new Rect(Map.WorldToLocal(element.worldBound.position), element.worldBound.size));
            AttachWindows();
            Characters = new CharacterRosterPanel(Root, Presentation, id => Map.Focus(id));
            Characters.Attach(Tray, decisionDock);
            _ = new BoardCharacterIntegration(this, stage);
            Edges = new BoardEdgeLayout(this, stage);
            // UI-324: a left panel held open narrows the middle; the windows placed there move clear of it.
            foreach (var panel in Edges.LeftPanels)
            {
                panel.RegisterCallback<GeometryChangedEvent>(_ => Windows.MoveMiddle());
                panel.RegisterCallback<Appearance.ClosingEvent>(_ => Windows.MoveMiddle());
            }
            Windows.MiddleMoved += Cards.MiddleMoved;
            Victory.KeepShortcutClearOf(Characters.Tab);
            AttachDieGlows();
            Dissolve = new CardDissolve(ReducedMotion);
            Root.Add(Dissolve.Root);
            HuntDrops = new HuntTileDrop(BoardMiddle, () => Hunt.Tab, ReducedMotion);
            Root.Add(HuntDrops.Root);
            Root.Add(Menu.Root);
            Root.Add(GameDeleted.Root);
            Escape.Attach();
            Family = new WindowFamily(Root, () => Escape.Roots);
            AttachFamily();
            Edges.Preview.Busy = EdgePanelBusy;
            Map.RegionInspected += Inspect;
            Phase.text = "Loading match…";
            Tracks.style.display = DisplayStyle.None;
        }

        // UI-89: previews opened from inside a board window close with that window.
        private void AttachFamily()
        {
            Cards.OpenedFrom += origin => Family.Opened(Cards.Root, origin, Cards.Close);
            // A report opened from a log entry belongs to that entry; the ⚔ icon by the notices opens it independently.
            BattleReport.OpenedFrom += origin => Family.Opened(BattleReport.Root, origin, BattleReport.Close);
        }

        // UI-98: a card or a character inspected from an unrolled edge panel keeps that panel open.
        // The Characters panel stays unrolled while a character or a sovereign it opened is inspected.
        private bool EdgePanelBusy(VisualElement panel) => Family.HasOpenChild(panel) ||
            panel == Characters.Root && (Characters.Inspector.Root.style.display.value != DisplayStyle.None || Sovereigns.IsOpen);

        private void AttachWindows()
        {
            _ = new FloatingWindow(Information.Root, Windows);
            _ = new FloatingWindow(BattleReport.Root, Windows);
            Log.Window = new FloatingWindow(Log.Root, Windows);
            ActionHistory = new ActionContextRouter(this);
            Log.CardWindow = new FloatingWindow(Log.Popup, Windows, () => Log.Pinned, Log.Pin, Log.PositionCard, rememberInitial: false);
            Cards.Window = new FloatingWindow(Cards.Root, Windows, () => Cards.Pinned, Cards.Pin, defaultAnchor: new Vector2(342, 8));
            Cards.CenterArea = BoardMiddle;
            Windows.Middle = BoardMiddle;
            Battles.Window = new FloatingWindow(Battles.Root, Windows, () => Battles.Pinned, Battles.Pin,
                Battles.Position, rememberInitial: false);
            // UI-162: the Hunt window is placed beside the Fellowship like the battle window beside its region.
            Hunt.Window = new FloatingWindow(Hunt.Root, Windows, () => Hunt.Pinned, Hunt.Open, Hunt.Position, rememberInitial: false);
            _ = new BattleCardPlacement(Cards, Battles);
            Sovereigns.Window = new FloatingWindow(Sovereigns.Inspector, Windows, () => Sovereigns.Pinned, Sovereigns.Pin,
                Sovereigns.Position, rememberInitial: false);
            Victory.Window = new FloatingWindow(Victory.Root, Windows, () => Victory.Pinned, Victory.Open, Victory.Position, rememberInitial: false);
        }

        public static VisualElement Row(string css = "row")
        {
            var row = new VisualElement();
            row.AddToClassList(css);
            return row;
        }
        public static Label Title(string text)
        {
            var l = new Label(text);
            l.AddToClassList("title");
            return l;
        }
        public static Button Button(string text, Action action)
        {
            var b = new Button(action) { text = text };
            b.AddToClassList("action");
            return b;
        }
        public void ShowFixture()
        {
            Map.Figures.Reset();
            Root.RemoveFromClassList("board-planning");
            Root.RemoveFromClassList("card-planning");
            Root.RemoveFromClassList("army-planning");
            Root.RemoveFromClassList("fellowship-planning");
            Root.RemoveFromClassList("character-planning");
            Root.RemoveFromClassList("live-board");
            Fellowship.Root.style.display = DisplayStyle.None;
            Fellowship.Status.style.display = DisplayStyle.None;
            Map.FellowshipMarker.Apply(null);
            Map.MordorTrack.Apply(MordorTrackState.Outside, false);
            Hunt.Close();
            Hunt.Tab.style.display = DisplayStyle.None;
            Cards.Close();
            HandDrawer.Close();
            Hover.Close();
            ActionDie.Clear();
            IsFixture = true;
            Victory.Hide();
            loading.style.display = DisplayStyle.None;
            Tracks.style.display = DisplayStyle.Flex;
            Presentation.UseStressFixture();
            Map.UpdateData(Presentation);
            Phase.text = "UI prototype · Action phase";
            Connection.text = "Local fixture";
            OpponentDice.Clear();
            OpponentDice.Add(ActionPoolView.OwnerLabel(Side.Shadow));
            TopBar.Opponent.Hide();
            foreach (var label in new[] { "C", "A", "M", "P", "A/M" })
                OpponentDice.Add(Die(label));
            OwnDice.Clear();
            OwnDice.Add(Title("FREE PEOPLES"));
            var row = Row();
            OwnDice.Add(row);
            foreach (var face in new[] { "C", "C", "P", "W" })
                row.Add(Die(face));
            OwnDice.Add(new Label("Spent  —    Hunt  C    Discarded  —"));
            Hand.Clear();
            Hand.Add(Title("YOUR HAND"));
            Hand.Add(new Label("Character     Strategy"));
            Hand.Add(new Label("No card selected"));
            Tracks.text = "FELLOWSHIP   Hidden\nProgress  0     Corruption  0\nVICTORY   FP 0 / 4    SH 0 / 10";
            Tray.Clear();
            Tray.Add(Title("Explore Middle-earth"));
            Tray.Add(new Label("Select a region to inspect its forces."));
            Tray.Add(new Label("Scroll to zoom · Move to an edge or right / middle drag to pan"));
        }
        public static Button Die(string face, Action action = null)
        {
            var b = new Button(action) { text = face };
            b.AddToClassList("die");
            b.tooltip = "C Character · A Army · M Muster · P Event · W Will of the West";
            return b;
        }
        // OBS-03: an observer's board — the bar with the shown side, and the end of the game without Victory or Defeat.
        public void Observe(Side perspective)
        {
            Observing = true;
            GameEnd.Observing = true;
            Viewing.Show(perspective);
        }

        // REPLAY-06: a jump, a change of side, a first view or a reconnect: the board shows the state as a new point of
        // reference — no flights and no tracks in two stages; anything still moving stops.
        public void ApplyQuietly(PlayerView view)
        {
            Map.Figures.SetConnected(false);
            quiet = true;
            Map.Quiet = true;
            ValuePulse.Quiet = true;
            try
            {
                Apply(view);
            }
            finally
            {
                quiet = false;
                Map.Quiet = false;
                ValuePulse.Quiet = false;
                Map.Figures.SetConnected(figuresLive);
            }
        }

        // Whether a step's presentation (flights, the second stage of the tracks) is still going on.
        public bool Presenting => Map.Figures.ActiveCount > 0 || TracksStaged;

        // UI-306: a presentation's staged view of the presented revision (recorded movers relocated, a region's control
        // changed) drives only the map's markers and settlements; the board's view, decision and windows stay as they are.
        public void Stage(PlayerView staged)
        {
            Presentation.Apply(staged);
            Map.UpdateData(Presentation);
        }

        // UI-310: a presentation's stage of the Fellowship — its figure and the Mordor track on the map, the plaque's and
        // the panel's tracks. The next applied view is compared with what it showed, so nothing animates twice.
        public void StageFellowship(FellowshipView fellowship)
        {
            Map.FellowshipMarker.Apply(fellowship, View.Hunt);
            Map.MordorTrack.Apply(MordorTrackState.For(fellowship));
            Fellowship.ShowTracks(fellowship);
            presentedFellowship = fellowship;
        }

        // UI-312: the opponent's dice pool and its top bar summary (the hand count) as a presentation stages them; the
        // released view redraws both.
        public void StageOpponentDice(PlayerView staged) => actionPool.Apply(OpponentDice, staged, Opponent(staged), false);
        public void StageOpponentSummary(PlayerView staged) => TopBar.Opponent.Apply(staged, Opponent(staged));

        // UI-330: an observer's presentation shows both sides' actions — the side the board is shown from has its dice at the
        // bottom and its cards in the hand, the other side its dice and its card count in the top bar. A player's
        // presentation only ever names the opponent.
        public bool Shown(Side? side) => View != null && side == View.You;
        public VisualElement DicePool(Side? side) => Shown(side) ? OwnDice : OpponentDice;
        public VisualElement HandOf(Side? side) => Shown(side) ? Hand : TopBar.Opponent.Root.Q("opponent-cards");

        public void StageDice(Side side, PlayerView staged)
        {
            if (side == staged.You)
                actionPool.Apply(OwnDice, staged, side, true);
            else
                StageOpponentDice(staged);
        }

        // The side's card count: the opponent's in the top bar; the shown side's cards arrive in its hand with the view.
        public void StageHand(Side side, PlayerView staged)
        {
            if (side != staged.You)
                StageOpponentSummary(staged);
        }
        private static Side Opponent(PlayerView view) => view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;

        // UI-311: a recorded battle state in its window — a new battle's window opens as it does for a new battle (UI-149),
        // and a window the player closed during this battle stays closed.
        public void StageBattle(PlayerView staged)
        {
            Battles.Apply(staged);
            Battles.Present(staged.Battle, staged, Cards);
        }

        public void EndBattlePresentation() => Battles.EndPresentation();

        // UI-310: the Fellowship's roster and the Hunt window as a presentation stages them; the released view replaces both.
        public void StageRoster(PlayerView staged) => Fellowship.Apply(staged, Presentation);
        public void StageHunt(PlayerView staged) => Hunt.Apply(staged);

        // UI-310: after Skip the latest tracks show at once, never in two stages.
        public void QuietTracks() => quietTracks = true;

        public void Apply(PlayerView view)
        {
            if (view == null)
                return;
            IsFixture = false;
            var previous = presentedFellowship ?? (View?.MatchId == view.MatchId ? View.Fellowship : null);
            presentedFellowship = null;
            View = view;
            loading.style.display = DisplayStyle.None;
            Tracks.style.display = DisplayStyle.None;
            Log.Toggle.SetEnabled(true);
            Root.AddToClassList("live-board");
            Presentation.Apply(view);
            Map.UpdateData(Presentation);
            var tracks = StageTracks(previous, view);
            Map.FellowshipMarker.Apply(tracks, view.Hunt);
            Map.MordorTrack.Apply(MordorTrackState.For(tracks));
            Map.Figures.Apply(view);
            Phase.text = $"Turn {view.Turn} · {PhaseName(view.Phase)}";
            ExpansionCrests.Show(TopBar.Crests, view.Configuration);
            actionPool.Apply(OwnDice, view, view.You, true);
            var opponent = view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            actionPool.Apply(OpponentDice, view, opponent, false);
            TopBar.Opponent.Apply(view, opponent);
            Characters.Apply(view);
            Sovereigns.Apply(view);
            handView.Apply(view);
            Dissolve.Settle(view, quiet);
            HuntDrops.Settle(view.HuntPool, quiet);
            HandDrawer.Apply(view);
            tableEffects.Apply(view);
            mapEffects.Apply(view);
            BattleReport.Apply(view);
            Nations.Apply(view);
            Information.Apply(view);
            Hover.Apply(view);
            Battles.Apply(view);
            var fellowship = view.Fellowship;
            Fellowship.Apply(view, Presentation);
            if (TracksStaged)
                Fellowship.ShowTracks(tracks);
            Hunt.Apply(view);
            effectScopes.Apply(view);
            Notice.Apply(view);
            Notifications.RefreshVisibility();
            Victory.Apply(view);
            Edges.Apply(view);
            GameEnd.Apply(view);
        }
        // AUDIO-06: a resolution that moves the Fellowship and changes its Corruption shows the tracks in two stages
        // (TrackSequence), on the panel and the map alike: the first stage now, the full view 0.38 s later. A newer
        // view during the first stage shows itself at once. Returns what the tracks show now.
        private FellowshipView StageTracks(FellowshipView previous, PlayerView view)
        {
            trackStage?.Pause();
            trackStage = null;
            var skipped = quietTracks;
            quietTracks = false;
            if (quiet || skipped || previous == null || !TrackSequence.Both(previous, view.Fellowship))
                return view.Fellowship;
            trackStage = Root.schedule.Execute(() =>
            {
                trackStage = null;
                ApplyTracks(view.Fellowship, view);
            }).StartingIn((long)(TrackSequence.SecondStageSeconds * 1000));
            return TrackSequence.FirstStage(previous, view.Fellowship);
        }

        private void ApplyTracks(FellowshipView fellowship, PlayerView view)
        {
            Map.FellowshipMarker.Apply(fellowship, view.Hunt);
            Map.MordorTrack.Apply(MordorTrackState.For(fellowship));
            Fellowship.ShowTracks(fellowship);
        }

        public bool TracksStaged => trackStage != null;

        // The action card stays clear of the top bar and the notice strip, whose icons read further notices.
        private Rect CardBounds()
        {
            var parent = Log.Popup.parent;
            var area = parent.contentRect;
            var chrome = Notifications.Root.resolvedStyle.display != DisplayStyle.None
                ? Notifications.Root.worldBound.yMax
                : TopBar.Root.worldBound.yMax;
            var top = float.IsNaN(chrome) ? area.yMin : parent.WorldToLocal(new Vector2(0, chrome)).y + 4;
            return Rect.MinMaxRect(area.xMin, Mathf.Clamp(top, area.yMin, area.yMax), area.xMax, area.yMax);
        }

        // UI-155: in Mordor the Fellowship's button centres the map on the track.
        private void FocusFellowship()
        {
            Fellowship.Open();
            if (View?.Fellowship.MordorStep.HasValue == true)
                Map.FocusMordorTrack();
            else
                Map.Focus(View?.Fellowship.RegionId ?? "rivendell");
        }
        public void SetConnection(string state)
        {
            figuresLive = state is "Connected" or "Replay";
            Map.Figures.SetConnected(figuresLive);
            var stopped = state is "CorruptSave" or "UnsupportedSaveVersion"
                or ClientCompatibility.ClientUpdateRequired or ClientCompatibility.ServerUpdateRequired;
            if (state == MatchConnection.Deleted)
            {
                ShowDeleted();
                return;
            }
            Connection.text = state == "Connected" ? "Live" : state == "Replay" ? "Replay" : state == "SessionExpired" ? "Session expired · Sign in again"
                : state == ClientCompatibility.ClientUpdateRequired ? "Update required"
                : state == ClientCompatibility.ServerUpdateRequired ? "Server update pending"
                : stopped ? "Save unavailable" : state == "Connecting" ? "Connecting…"
                : state == MatchConnection.DisplayError ? "Display error · Retrying" : "Reconnecting…";
            Connection.tooltip = state == "CorruptSave" ? "The server administrator needs to restore this saved game."
                : state == "UnsupportedSaveVersion" ? "This game needs a compatible server version."
                : state == ClientCompatibility.ClientUpdateRequired ? "Install the latest version to keep playing. This game stays saved on the server."
                : state == ClientCompatibility.ServerUpdateRequired ? "The server needs an update for this version. This game stays saved on the server."
                : state == MatchConnection.DisplayError ? "The latest game state could not be drawn. It is drawn again with the next update; Reconnect retries at once." : "";
            if (View == null && !IsFixture)
                loading.text = stopped ? Connection.tooltip : state == "SessionExpired" ? "Sign in again to load this match."
                    : state == "Reconnecting" ? "Waiting for the server…" : "Loading match…";
        }
        // MENU-22: the other player deleted the match; only the way back to the main menu is left.
        private void ShowDeleted()
        {
            Connection.text = "Game deleted";
            Connection.tooltip = "The other player deleted this game.";
            if (View == null && !IsFixture)
                loading.text = "This game was deleted.";
            GameDeleted.Open();
        }

        // OBS-05: the game can no longer be watched — deleted, or the observer's place ended; only the way back is left.
        public void ShowWatchEnded()
        {
            Connection.text = "Not watching";
            Connection.tooltip = "This game can no longer be watched from here.";
            if (View == null && !IsFixture)
                loading.text = "This game can no longer be watched.";
            GameDeleted.Open("Watching ended",
                "The game was deleted, or your place in its audience ended. Return to the main menu to watch again.");
        }

        // UI-167: the action passes to the player — the ribbon, or with reduced motion one flash of the console.
        public void SignalTurn(string text, Side side)
        {
            if (ReducedMotion())
                Console.Status.Flash();
            else
                Ribbon.Show(text, side);
        }

        // UI-326: a hand card the player is discarding dissolves where it stands (its unrolled face, else its slot) once
        // the view without it arrives.
        public void MarkDiscard(string id)
        {
            var card = Hand.Q<Button>(id);
            Dissolve.Mark(id, card?.Q("hand-card-face") ?? card, View?.Revision ?? -1);
        }

        // UI-324: the middle of the board a window opens in — between the top bar and the console, between the side
        // columns and right of a panel held open beside the left rail (a decision's Reserve, say, stays usable) — or null
        // before the board is laid out.
        private Rect? BoardMiddle()
        {
            var board = Root.worldBound;
            var top = TopBar.Root.worldBound.yMax;
            var bottom = Console.Root.parent?.worldBound.yMin ?? board.yMax;
            var left = OnScreen(Edges.Left) ? Edges.Left.worldBound.xMax : board.xMin;
            foreach (var panel in Edges.LeftPanels)
                if (Held(panel))
                    left = Mathf.Max(left, panel.parent.LocalToWorld(panel.layout).xMax);
            var right = OnScreen(Edges.Right) ? Edges.Right.worldBound.xMin : board.xMax;
            return bottom > top && right > left ? Rect.MinMaxRect(left, top, right, bottom) : (Rect?)null;
        }

        private static bool OnScreen(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;

        // A left panel open beside its tab, not a hover's preview that only lies over the board. Its laid-out place, so
        // its unrolling slide does not drag the windows along. A panel fading out is no longer held (UI-322).
        private static bool Held(VisualElement panel) => panel.parent != null && !Appearance.Closed(panel) && OnScreen(panel) &&
            !panel.ClassListContains(LeftRailPreview.PreviewClass);

        public bool ReducedMotion()
        {
            for (var element = Root; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        public static string SideName(Side side) => side == Side.FreePeoples ? "Free Peoples" : "Shadow";
        public static string PhaseName(GamePhase phase) => phase == GamePhase.ActionResolution ? "Actions" : phase == GamePhase.HuntAllocation ? "Hunt allocation" : phase.ToString();
        // A right click on the map reads a region; it replaces the battle report it would otherwise cover.
        private void Inspect(string region)
        {
            BattleReport.Close();
            Information.Region(region);
        }

        // Read-only views give way when the player returns to the current decision.
        internal void CloseInformation()
        {
            BattleReport.Root.style.display = DisplayStyle.None;
            Information.Close();
        }
    }
}
