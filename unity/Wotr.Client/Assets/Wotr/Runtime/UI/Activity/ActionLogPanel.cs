using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionLogPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "action-log" };
        public VisualElement Popup { get; } = new VisualElement { name = "action-summary" };
        public ActionCard Card { get; }
        public ActionLogList List { get; }
        public Button Toggle { get; }
        public bool IsOpen { get; private set; }
        public bool Pinned { get; private set; }
        public string SelectedId => Selection?.ActionId ?? "";
        public ActionOpenRequest Selection { get; private set; }
        // UI-100: the card opens centred on the board, clamped to these bounds (below the top bar and notices).
        public Func<Rect> CardBounds { get; set; }
        public ActionLogFilter Filter { get; private set; } = ActionLogFilter.All;
        public FloatingWindow Window { get; set; }
        public FloatingWindow CardWindow { get; set; }
        public ActionLogModel Model { get; private set; } = new ActionLogModel();
        public event Action Opened;
        public event Action CardOpening;
        public event Action<ActionLogFilter> FilterChanged;
        public event Action LoadOlder;
        public event Action Retry;
        public event Action<ActionOpenRequest, bool> Inspect;
        public event Action<ActionLogView, bool> Presented;
        public event Action CardClosed;
        public event Action<ActionLogView> ExplicitlyDisplayed;
        // UI-166: the played card's line on a compact card asks for its preview.
        public event Action<string> CardRequested;
        private readonly Dictionary<ActionLogFilter, Button> filters = new Dictionary<ActionLogFilter, Button>();
        private readonly Label message = new Label { name = "log-status" };
        private readonly Label cardMessage = new Label { name = "action-card-message" };
        private Button retry;
        private Button cardRetry;
        private Button recent;
        private Button more;
        private Button pin;
        private Label cardTitle;
        private bool complete;

        public ActionLogPanel(BoardPresentation data)
        {
            Root.AddToClassList("action-log");
            Root.AddToClassList("action-history");
            Popup.AddToClassList("action-summary");
            Popup.AddToClassList("action-history");
            Root.style.display = Popup.style.display = DisplayStyle.None;
            Toggle = BoardScreen.Button("Log", () => { if (IsOpen) Close(); else Open(); });
            Toggle.name = "open-log";
            Toggle.tooltip = "Log · Action history";
            List = new ActionLogList(data);
            Card = new ActionCard(data);
            BuildLog();
            BuildCard();
            RegisterInput();
            RefreshFilter();
            SetStatus("Open Log to load history.", false, false);
            RefreshNew();
        }

        private void BuildLog()
        {
            var header = FloatingWindow.Header("Log");
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-log";
            header.Add(close);
            Root.Add(header);
            var filterRow = BoardScreen.Row();
            foreach (var filter in new[] { ActionLogFilter.All, ActionLogFilter.Opponent, ActionLogFilter.You })
            {
                var button = BoardScreen.Button(filter.ToString(), () => ChooseFilter(filter));
                button.name = "log-filter-" + filter;
                filters[filter] = button;
                filterRow.Add(button);
            }
            Root.Add(filterRow);
            recent = BoardScreen.Button("New entries ↑", () => { List.Latest(); RefreshNew(); });
            recent.name = "log-new-entries";
            Root.Add(recent);
            List.Inspect += (id, step, pinned) => Request(new ActionOpenRequest(id, Model.Find(id)?.Action.Version ?? "", step, Filter), pinned);
            List.Expand += id => { List.SaveAnchor(); Model.Toggle(id); List.Apply(Model); };
            List.NearEnd += () => { if (Model.Older.Length > 0) LoadOlder?.Invoke(); };
            Root.Add(List.Root);
            var footer = BoardScreen.Row();
            footer.Add(message);
            retry = IconButton.Create("refresh", "Retry", () => Retry?.Invoke());
            retry.name = "log-retry";
            footer.Add(retry);
            more = IconButton.Create("expand", "Older · Load earlier actions", () => LoadOlder?.Invoke());
            more.name = "log-older";
            footer.Add(more);
            Root.Add(footer);
        }

        private void BuildCard()
        {
            var cardHeader = FloatingWindow.Header("Recorded action");
            cardTitle = cardHeader.Q<Label>(className: "title");
            pin = IconButton.Create("pin", "Pin · Keep this open", Pin);
            pin.name = "pin-action-summary";
            cardHeader.Add(pin);
            var cardClose = IconButton.Create("close", "Close", CloseCard);
            cardClose.name = "close-action-summary";
            cardHeader.Add(cardClose);
            Popup.Add(cardHeader);
            Popup.Add(cardMessage);
            cardRetry = IconButton.Create("refresh", "Retry", () => Inspect?.Invoke(Selection, Pinned));
            cardRetry.name = "action-card-retry";
            Popup.Add(cardRetry);
            Popup.Add(Card.Root);
            Card.Result.CardRequested += id => CardRequested?.Invoke(id);
            Card.Result.FullRecordRequested += OpenFullRecord;
        }

        // UI-166: Full record opens the Log and shows the same action with every step.
        private void OpenFullRecord()
        {
            var full = Selection?.Full();
            if (full == null)
                return;
            Open();
            Request(full, true);
        }

        private void RegisterInput()
        {
            Root.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { Close(); e.StopPropagation(); } });
            Popup.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { CloseCard(); e.StopPropagation(); } });
            // The card's height follows its content; keep it centred while the details load.
            Popup.RegisterCallback<GeometryChangedEvent>(e => { if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect)) PositionCard(); });
            Root.RegisterCallback<AttachToPanelEvent>(e => { if (e.target == Root) Application.focusChanged += Focus; });
            Root.RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == Root) Application.focusChanged -= Focus; });

        }

        public void Open()
        {
            IsOpen = true;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Window?.Refresh();
            List.Apply(Model);
            Toggle.EnableInClassList("selected", true);
            Opened?.Invoke();
        }

        public void Close()
        {
            List.SaveAnchor();
            IsOpen = false;
            Root.style.display = DisplayStyle.None;
            Toggle.EnableInClassList("selected", false);
            if (!Pinned) CloseCard();
        }

        public void ChooseFilter(ActionLogFilter next)
        {
            if (next == Filter) return;
            List.SaveAnchor();
            Filter = next;
            RefreshFilter();
            if (!Pinned) CloseCard();
            FilterChanged?.Invoke(next);
        }

        public void Bind(ActionLogModel next)
        {
            Model = next;
            List.Apply(next);
            RefreshNew();
        }

        public void Changed()
        {
            List.Apply(Model);
            RefreshNew();
        }

        public void SetStatus(string text, bool error, bool busy)
        {
            message.text = text;
            retry.style.display = error ? DisplayStyle.Flex : DisplayStyle.None;
            more.style.display = Model.Older.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            more.SetEnabled(!busy);
        }

        public void Request(string id, bool pinned) => Request(new ActionOpenRequest(id, Model.Find(id)?.Action.Version ?? "", filter: Filter), pinned);

        public void Request(ActionOpenRequest request, bool pinned)
        {
            if (request.ActionId.Length == 0 || Pinned && !pinned)
                return;
            Pinned = pinned;
            Select(request, pinned);
        }

        private void Select(ActionOpenRequest request, bool pinned)
        {
            CardOpening?.Invoke();
            // Every card starts in the centre; dragging moves only the card on screen.
            CardWindow?.Forget();
            Selection = request;
            complete = false;
            pin.style.display = Pinned ? DisplayStyle.None : DisplayStyle.Flex;
            Popup.style.display = DisplayStyle.Flex;
            Popup.BringToFront();
            Card.Root.style.display = DisplayStyle.None;
            SetCardStatus("Loading details…", false);
            PositionCard();
            Inspect?.Invoke(request, pinned);
        }

        public void ShowAction(ActionLogView view, bool isComplete, BattleReportView report = null)
        {
            if (view.Action.Id != SelectedId) return;
            // A later page may replace a group's header or append steps. The displayed version stays immutable.
            view = new ActionLogView { MatchId = view.MatchId, Action = view.Action, Steps = new List<ActionLogStep>(view.Steps) };
            complete = isComplete;
            Card.Apply(view, Selection?.StepId ?? "", Selection?.Compact == true);
            Popup.EnableInClassList("compact", Card.Compact);
            Card.ShowBattle(report);
            Card.Root.style.display = DisplayStyle.Flex;
            SetCardStatus(isComplete ? "" : "Loading remaining steps…", false);
            Presented?.Invoke(view, isComplete);
            PositionCard();
            if (Pinned && complete)
                Popup.schedule.Execute(() =>
                {
                    if (Pinned && complete && Card.Displayed == view && Popup.resolvedStyle.display != DisplayStyle.None && Popup.panel != null)
                        ExplicitlyDisplayed?.Invoke(view);
                });
        }

        public void SetContextTitle(string text) => cardTitle.text = text;

        public void SetCardStatus(string text, bool error)
        {
            cardMessage.text = text;
            cardMessage.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            cardRetry.style.display = error ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Pin() { if (Selection != null) Request(Selection, true); }
        public void LeaveSource() { }
        public void CloseCard()
        {
            Pinned = complete = false;
            Selection = null;
            CardClosed?.Invoke();
            Popup.style.display = DisplayStyle.None;
        }
        public void CloseTransient() { if (!Pinned) CloseCard(); }

        // The card's tallest form (Styles/Notices.uss max-height).
        private const float MaxCardHeight = 460;

        // Before its first layout the card is placed and framed around with its usual size.
        public Vector2 CardSize
        {
            get
            {
                var size = Popup.layout.size;
                if (size.x <= 0 || float.IsNaN(size.x)) size.x = 390;
                if (size.y <= 0 || float.IsNaN(size.y)) size.y = 300;
                return size;
            }
        }

        public void PositionCard()
        {
            if (CardWindow?.Restore() == true || Popup.parent == null) return;
            var area = Popup.parent.contentRect;
            var bounds = CardBounds?.Invoke() ?? area;
            // UI-103: on a short board the card shrinks to the room under the notices; its steps scroll.
            var limit = Mathf.Min(MaxCardHeight, bounds.height);
            if (Mathf.Abs(Popup.style.maxHeight.value.value - limit) > .5f)
                Popup.style.maxHeight = limit;
            var size = CardSize;
            size.y = Mathf.Min(size.y, limit);
            var point = WindowLayout.Clamp(area.center - size / 2, size, bounds);
            Popup.style.left = point.x;
            Popup.style.top = point.y;
        }

        private void RefreshFilter() { foreach (var pair in filters) pair.Value.EnableInClassList("selected", pair.Key == Filter); }
        private void RefreshNew() => recent.style.display = Model.HasNew ? DisplayStyle.Flex : DisplayStyle.None;
        private void Focus(bool focused) { if (!focused) CloseTransient(); }
    }
}
