using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Pinned history keeps the selected battle/version, independent of later board snapshots.
    // UI-112: the ⚔ control announces an unread Combat round and hides once the report has shown that round.
    public sealed class BattleReport
    {
        public const string RoundKeyPrefix = "battle-round/";
        public VisualElement Root { get; } = new VisualElement { name = "battle-report" };
        public Button Open { get; }
        public BattleReportView Displayed { get; private set; }
        public int RequestVersion { get; private set; }
        public event Action<string, int> Requested;
        // UI-89: the control behind an explicit open; loading the full history only refreshes the window.
        public event Action<VisualElement> OpenedFrom;
        // A round key the window has just shown; the session keeps it as a local read marker.
        public event Action<string> RoundRead;
        private readonly CardInspector inspector;
        private readonly BoardPresentation data;
        private readonly Label heading = BoardScreen.Title("Battle report");
        private readonly Label status = new Label { name = "battle-report-status" };
        private readonly Label round = new Label { name = "battle-report-round" };
        private readonly Button previous, next, retry;
        private readonly VisualElement navigation = BoardScreen.Row("battle-report-navigation");
        private readonly ScrollView body = new ScrollView(ScrollViewMode.Vertical) { name = "battle-report-history" };
        private readonly HashSet<string> readRounds = new HashSet<string>();
        private PlayerView view;
        private string requestedId = "";
        private int selected;

        public BattleReport(CardInspector inspector, BoardPresentation data)
        {
            this.inspector = inspector;
            this.data = data;
            Root.AddToClassList("battle-report");
            Root.AddToClassList("action-history");
            // UI-119: the report is a parchment chronicle inside the board's frame.
            Root.AddToClassList("battle-chronicle");
            Root.style.display = DisplayStyle.None;
            var header = BoardScreen.Row("window-header");
            FloatingWindow.MarkHandle(header);
            header.Add(heading);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-battle-report";
            close.AddToClassList("close-battle");
            close.tooltip = "Close battle report";
            header.Add(close);
            Root.Add(header);
            var state = BoardScreen.Row();
            state.Add(status);
            retry = IconButton.Create("refresh", "Retry", Request);
            retry.name = "retry-battle-report";
            state.Add(retry);
            Root.Add(state);
            previous = IconButton.Create("back", "Previous", () => Select(selected - 1));
            previous.name = "previous-battle-round";
            previous.tooltip = "Previous recorded round";
            next = IconButton.Create("next", "Next", () => Select(selected + 1));
            next.name = "next-battle-round";
            next.tooltip = "Next recorded round";
            round.AddToClassList("battle-round-tab");
            navigation.Add(previous);
            navigation.Add(round);
            navigation.Add(next);
            Root.Add(navigation);
            body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(body);
            Root.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { Close(); e.StopPropagation(); } });
            Open = IconButton.Create("battle", "The last battle", ShowLast);
            Open.name = "last-battle";
            Open.tooltip = "Last Combat round · Open battle report";
            Open.style.display = DisplayStyle.None;
        }

        public void Apply(PlayerView latest)
        {
            if (view?.MatchId != latest.MatchId) Close();
            view = latest;
            RefreshOpen();
        }

        // Match, battle and round: the same battle ID in another match is a different round.
        public static string RoundKey(string matchId, BattleView battle) => RoundKeyPrefix + matchId + "/" + battle.Id + "/" + battle.Round;

        // Restores markers saved by an earlier session before the first snapshot arrives.
        public void MarkRead(IEnumerable<string> keys)
        {
            readRounds.UnionWith(keys.Where(key => key.StartsWith(RoundKeyPrefix, StringComparison.Ordinal)));
            RefreshOpen();
        }

        private void RefreshOpen()
        {
            var last = view?.LastBattleRound;
            var unread = last != null && !readRounds.Contains(RoundKey(view.MatchId, last));
            Open.style.display = unread ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ReadLastRound()
        {
            var last = view?.LastBattleRound;
            if (last == null)
                return;
            var key = RoundKey(view.MatchId, last);
            if (!readRounds.Add(key))
                return;
            RefreshOpen();
            RoundRead?.Invoke(key);
        }

        // A report from the history counts when it shows the last round of the same battle.
        private bool ShowsLastRound(BattleReportView report)
        {
            var last = view?.LastBattleRound;
            return last != null && report.MatchId == view.MatchId && report.Id == last.Id &&
                report.Rounds.Any(round => round.Number == last.Round && round.Completed);
        }

        private void ShowLast()
        {
            var battle = view?.LastBattleRound;
            if (battle == null) return;
            Displayed = null;
            requestedId = battle.Id;
            OpenWindow(battle);
            navigation.style.display = DisplayStyle.None;
            body.Clear();
            new BattleContext(body, battle, view, null, null, null, inspector).Refresh();
            Request();
            OpenedFrom?.Invoke(Open);
            ReadLastRound();
            KeepKeyboardFocus();
        }

        // The read ⚔ disappears under the click and loses focus; the report takes it, so Escape still closes it.
        private void KeepKeyboardFocus() => Root.schedule.Execute(() =>
        {
            if (Root.resolvedStyle.display == DisplayStyle.Flex && Open.resolvedStyle.display == DisplayStyle.None)
                Root.Focus();
        });

        private void Request()
        {
            RequestVersion++;
            SetStatus("Last recorded round · Loading full report…", false);
            if (Requested == null) SetStatus("Last recorded round · Full history requires a connection", false);
            else Requested.Invoke(requestedId, RequestVersion);
        }

        public void ShowFrom(BattleReportView report, VisualElement origin)
        {
            Show(report);
            OpenedFrom?.Invoke(origin);
        }

        public void Show(BattleReportView report)
        {
            RequestVersion++;
            Displayed = report;
            requestedId = report.Id;
            selected = Math.Max(0, report.Rounds.Count - 1);
            OpenWindow(report.Latest ?? report.Initial);
            SetStatus(ActionCardText.Words(report.Status.ToString()) + (report.PartialHistory ? " · Earlier details unavailable" : ""), false);
            Select(selected);
            if (ShowsLastRound(report))
                ReadLastRound();
        }

        private void OpenWindow(BattleView battle)
        {
            heading.text = (battle == null ? "Battle" : ActionCardText.Region(battle.RegionId, data) + " · " + battle.Mode) + " · Report";
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        private void Select(int index)
        {
            if (Displayed == null) return;
            selected = Math.Max(0, Math.Min(index, Displayed.Rounds.Count - 1));
            navigation.style.display = DisplayStyle.Flex;
            var current = Displayed.Rounds.Count == 0 ? null : Displayed.Rounds[selected];
            round.text = current == null ? "No recorded Combat roll" : "Round " + current.Number +
                (current.Completed ? " · Completed" : " · In progress");
            previous.SetEnabled(selected > 0);
            next.SetEnabled(selected + 1 < Displayed.Rounds.Count);
            body.Clear();
            body.scrollOffset = Vector2.zero;
            BattleReportDetails.Render(body, Displayed, current, data, inspector);
        }

        public void SetStatus(string text, bool canRetry)
        {
            status.text = text;
            retry.style.display = canRetry ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Close()
        {
            RequestVersion++;
            Root.style.display = DisplayStyle.None;
        }
    }
}
