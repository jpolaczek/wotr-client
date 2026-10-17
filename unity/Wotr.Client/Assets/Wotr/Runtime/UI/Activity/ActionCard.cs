using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // The same read-only renderer is used by the log and contextual navigation.
    // UI-106: a parchment card with only what is needed to understand what happened. The heading names the side
    // once; steps show their effects without numbers, dice, sources or system bookkeeping, and a played card
    // shows the text of the part that was used.
    public sealed class ActionCard
    {
        public VisualElement Root { get; } = new VisualElement { name = "action-card" };
        public ActionLogView Displayed { get; private set; }
        public BattleArchiveSummary Archive { get; }
        public HuntArchiveSummary HuntArchive { get; }
        public ActionResultView Result { get; } = new ActionResultView();
        public bool Compact { get; private set; }
        private readonly BoardPresentation data;
        private readonly Label heading = new Label { name = "action-card-heading", enableRichText = false };
        private readonly VisualElement icon = new VisualElement { name = "action-card-icon", pickingMode = PickingMode.Ignore };
        private string iconClass = "";
        private readonly Label status = new Label { name = "action-card-status" };
        private readonly VisualElement played = new VisualElement { name = "action-card-played" };
        private readonly ScrollView body = new ScrollView(ScrollViewMode.Vertical) { name = "action-card-steps" };
        private readonly Dictionary<string, VisualElement> rendered = new Dictionary<string, VisualElement>();
        private readonly Label summary = new Label { name = "action-card-summary", enableRichText = false };
        private string playedKey = "";

        public ActionCard(BoardPresentation data)
        {
            this.data = data;
            Archive = new BattleArchiveSummary(data);
            HuntArchive = new HuntArchiveSummary(data);
            Root.AddToClassList("action-card");
            heading.AddToClassList("title");
            var title = new VisualElement { name = "action-card-title" };
            title.AddToClassList("action-card-title");
            icon.AddToClassList("action-card-icon");
            title.Add(icon);
            title.Add(heading);
            Root.Add(title);
            Root.Add(status);
            Root.Add(Result.Root);
            played.AddToClassList("action-card-played");
            Root.Add(played);
            Root.Add(HuntArchive.Root);
            body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            summary.AddToClassList("action-detail");
            Root.Add(body);
        }

        public void Apply(ActionLogView view, string stepId = "", bool compact = false)
        {
            var same = Displayed?.Action.Id == view.Action.Id;
            if (!same)
            {
                body.Clear();
                rendered.Clear();
                body.scrollOffset = UnityEngine.Vector2.zero;
            }
            Displayed = view;
            Compact = compact;
            Root.EnableInClassList("compact", compact);
            ApplyStatus(view.Action);
            if (compact)
                ApplyResult(view);
            else
                ApplyRecord(view, stepId);
        }

        // UI-166: the result in a few lines, without the step list, the parchment card text or the Hunt archive.
        private void ApplyResult(ActionLogView view)
        {
            var result = ActionResultText.For(view, data);
            heading.text = result.Header;
            ApplyEnamel(view.Action.Initiator);
            ApplyIcon(result);
            Result.Apply(result);
            Result.Root.style.display = DisplayStyle.Flex;
            played.style.display = DisplayStyle.None;
            playedKey = "";
            HuntArchive.Root.style.display = DisplayStyle.None;
            body.style.display = DisplayStyle.None;
        }

        private void ApplyRecord(ActionLogView view, string stepId)
        {
            heading.text = ActionCardText.Heading(view.Action);
            ApplyEnamel(null);
            SetIcon("");
            Result.Root.style.display = DisplayStyle.None;
            body.style.display = DisplayStyle.Flex;
            HuntArchive.Apply(ActionContextModel.For(view, stepId));
            ApplyPlayed(view);
            ApplySteps(view);
        }

        // The heading of a compact card is written in the enamel of the side that acted.
        private void ApplyEnamel(Side? side)
        {
            heading.EnableInClassList("side-free", side == Side.FreePeoples);
            heading.EnableInClassList("side-shadow", side == Side.Shadow);
        }

        // The kind of action at a glance: the card of a played Event, else the face of the die that paid for it.
        private void ApplyIcon(ActionResult result)
        {
            if (result.CardId.Length > 0)
                SetIcon("action-card-icon-card");
            else if (result.Die.HasValue)
                SetIcon(DieFaceIcon.ClassName + "-" + DieFaceIcon.Key(result.Die));
            else
                SetIcon("");
        }

        private void SetIcon(string next)
        {
            if (iconClass.Length > 0)
                icon.RemoveFromClassList(iconClass);
            iconClass = next;
            if (next.Length > 0)
                icon.AddToClassList(next);
            icon.style.display = next.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowBattle(BattleReportView report)
        {
            Archive.Apply(report);
            if (body.IndexOf(Archive.Root) != 0) body.Insert(0, Archive.Root);
        }

        // A completed action needs no status; an unfinished or partial one says so once.
        private void ApplyStatus(ActionLogSummary action)
        {
            var completed = !action.Legacy && action.Status == ActionLogStatus.Completed;
            status.text = completed ? "" : ActionCardText.Status(action);
            if (action.PartialHistory && !action.Legacy)
                status.text += (status.text.Length > 0 ? " · " : "") + "Earlier steps unavailable";
            status.style.display = status.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyPlayed(ActionLogView view)
        {
            var cards = view.Steps.SelectMany(step => step.Facts.Cards).Where(Shown)
                .GroupBy(card => card.Id).Select(group => group.First()).ToList();
            var key = string.Join("|", cards.Select(card => card.Id + ":" + card.Use));
            if (key == playedKey)
                return;
            playedKey = key;
            played.Clear();
            foreach (var card in cards)
                played.Add(PlayedCard(card));
            played.style.display = cards.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static bool Shown(ActionCardFact card) => card.Id.Length > 0 && CardLibrary.Cards.ContainsKey(card.Id) &&
            card.Use is ActionCardUse.Event or ActionCardUse.CombatReveal;

        private static VisualElement PlayedCard(ActionCardFact fact)
        {
            var card = CardLibrary.Cards[fact.Id];
            var combat = fact.Use == ActionCardUse.CombatReveal;
            var root = new VisualElement { name = "action-played-" + fact.Id };
            root.AddToClassList("action-played-card");
            root.Add(Text(card.Title, "action-played-title"));
            root.Add(Text(combat ? "Combat · " + card.CombatTitle : "Event", "action-played-kind"));
            root.Add(Text(combat ? card.Combat : CardLibrary.EventText(card, null), "action-played-text"));
            return root;
        }

        private void ApplySteps(ActionLogView view)
        {
            var offset = Archive.Root.parent == body.contentContainer ? 1 : 0;
            var position = 0;
            var ids = new HashSet<string>();
            foreach (var step in view.Steps)
            {
                var lines = Lines(step);
                if (lines.Length == 0)
                    continue;
                ids.Add(step.Id);
                if (!rendered.TryGetValue(step.Id, out var row))
                    rendered[step.Id] = row = Step(step, lines);
                if (body.IndexOf(row) != position + offset)
                    body.Insert(position + offset, row);
                position++;
            }
            foreach (var id in rendered.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                rendered[id].RemoveFromHierarchy();
                rendered.Remove(id);
            }
            // An action whose steps record no effect still says what it was.
            var last = view.Steps.LastOrDefault();
            summary.text = position == 0 && last != null && playedKey.Length == 0 ? ActionCardText.WithoutSide(last.Text, view.Action.Initiator ?? last.Actor) : "";
            if (summary.text.Length > 0 && summary.parent != body.contentContainer)
                body.Add(summary);
            else if (summary.text.Length == 0)
                summary.RemoveFromHierarchy();
        }

        private string[] Lines(ActionLogStep step) => step.Legacy
            ? new[] { step.Text }
            : ActionFactText.Effects(step, data).Distinct().ToArray();

        private static VisualElement Step(ActionLogStep step, IEnumerable<string> lines)
        {
            var row = new VisualElement { name = "action-step-" + step.Id };
            row.AddToClassList("action-card-step");
            foreach (var line in lines)
                row.Add(Text(line));
            return row;
        }

        private static Label Text(string value, string css = "action-detail")
        {
            var label = new Label(value) { enableRichText = false };
            label.AddToClassList(css);
            return label;
        }

        public static bool ContainsInput(VisualElement target)
        {
            for (var element = target; element != null; element = element.parent)
                if (element.ClassListContains("action-history")) return true;
            return false;
        }
    }
}
