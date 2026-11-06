using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-497 (R35, docs/ui/RANKED_UX.md): the Find game page of the menu window. It asks only what a ranked search needs:
    // each expansion and each pace as Off, Allow or Enforce, and a side or none. It has no name, visibility or variant, and
    // always opens in the defaults, so a side chosen last time cannot be played by accident.
    public sealed class FindGameForm
    {
        public const string NotReleased = "Not available on this server.";
        public const string LastPace = "At least one pace must stay on: a game needs a clock.";
        public const string NoSide = "Choose a side first.";
        public const string Stakes = "A ranked game is played to its end: leaving it or running out of time loses it, and your rating follows.";

        public const string SearchText = "Find ranked game";
        public const string CancelText = "Cancel search";
        public const string SearchStopped = "Your search stopped while the connection was lost. Start it again when you are ready.";
        public const string CancelUnconfirmed = "The server could not be reached, and your search has lapsed by now.";
        private const string Searching = "Searching… ";
        private const string Lost = "Connection lost, still trying… ";
        // About 30 updates a second while the indicator turns; a quarter turn a second reads as moving, not as busy.
        private const long TickMilliseconds = 33;
        private const float DegreesPerSecond = 90;

        public VisualElement Root { get; } = new VisualElement { name = "find-game-form" };
        public event Action<RankedSearchRequest> Find;
        public event Action CancelSearch;
        public event Action Back;
        private readonly SideChoice sides = new SideChoice("find", releasable: true);
        private readonly RankedChoiceRow lords = Expansion("find-lords", "Lords");
        private readonly RankedChoiceRow kings = Expansion("find-kings", "Kings");
        private readonly RankedChoiceRow fate = Expansion("find-fate", "Fate");
        private readonly RankedChoiceRow fast = Pace("find-fast", "Fast", "1:30 / 0:20");
        private readonly RankedChoiceRow standard = Pace("find-standard", "Standard", "3:00 / 0:30");
        private readonly RankedChoiceRow longest = Pace("find-long", "Long", "5:00 / 1:00");
        private readonly RankedChoiceRow sideChoice =
            new RankedChoiceRow("find-side-choice", "This side", "", RankedChoice.Allow, RankedChoice.Enforce);
        private readonly Label rating = new Label { name = "find-rating", pickingMode = PickingMode.Ignore };
        private readonly Label status = new Label { name = "find-status", pickingMode = PickingMode.Ignore };
        private readonly VisualElement columns = new VisualElement();
        private readonly VisualElement progress = new VisualElement { name = "find-progress", pickingMode = PickingMode.Ignore };
        private readonly VisualElement indicator = new VisualElement { name = "find-indicator", pickingMode = PickingMode.Ignore };
        private readonly Label elapsed = new Label { name = "find-elapsed", pickingMode = PickingMode.Ignore };
        private readonly VisualElement opponent = new VisualElement { name = "find-opponent", pickingMode = PickingMode.Ignore };
        private readonly Label opponentName = new Label { name = "find-opponent-name", pickingMode = PickingMode.Ignore };
        private readonly Label opponentRating = new Label { name = "find-opponent-rating", pickingMode = PickingMode.Ignore };
        private Button back;
        private Button submit;
        private IVisualElementScheduledItem ticking;
        private bool searching;
        private bool lost;
        private float searchStarted;
        private float angle;
        private readonly Dictionary<string, RankedChoiceRow> expansions;
        private readonly RankedChoiceRow[] paces;
        private HashSet<string> offered = new HashSet<string>();

        public FindGameForm()
        {
            expansions = new Dictionary<string, RankedChoiceRow>
            {
                [ExpansionModules.Lords] = lords, [ExpansionModules.Kings] = kings, [ExpansionModules.Fate] = fate
            };
            paces = new[] { fast, standard, longest };
            Root.AddToClassList("host-form");
            Root.AddToClassList("find-form");
            columns.AddToClassList("find-columns");
            columns.Add(Column(Heading("Expansions"), lords.Root, kings.Root, fate.Root, Heading("Time per move / per answer"),
                fast.Root, standard.Root, longest.Root));
            rating.AddToClassList("find-rating");
            var stakes = new Label(Stakes) { name = "find-note", pickingMode = PickingMode.Ignore };
            stakes.AddToClassList("find-note");
            var right = Column(Heading("Your side"), sides.Root, sideChoice.Root, rating, stakes);
            right.AddToClassList("find-column-last");
            columns.Add(right);
            Root.Add(columns);
            Root.Add(OpponentPanel());
            Root.Add(Footer());
            ticking = Root.schedule.Execute(Tick).Every(TickMilliseconds);
            ticking.Pause();
            foreach (var pace in paces)
                pace.Chosen += value => ChoosePace(pace, value);
            sides.Changed += _ => ShowSideChoice();
            Reset();
        }

        public Side? Side => sides.Choice;

        public bool IsSearching => searching;

        public float IndicatorAngle => angle;

        public RankedSearchRequest Request => new RankedSearchRequest
        {
            Lords = lords.Value,
            Kings = kings.Value,
            Fate = fate.Value,
            Faction = sides.Choice,
            FactionChoice = sideChoice.Value,
            Fast = fast.Value,
            Standard = standard.Value,
            Long = longest.Value
        };

        // Each opening starts here. The modules stay as last read until the server answers; the rating is read afresh.
        public void Reset()
        {
            var defaults = new RankedSearchRequest();
            sides.Release();
            sideChoice.Show(RankedChoice.Allow);
            status.text = "";
            fast.Show(defaults.Fast);
            standard.Show(defaults.Standard);
            longest.Show(defaults.Long);
            ShowExpansions();
            ShowPaces();
            ShowSideChoice();
            ShowRating(null);
            ShowSearching(false);
            opponent.style.display = DisplayStyle.None;
            columns.style.display = DisplayStyle.Flex;
        }

        // The modules the server releases (GET /api/rooms/offered-modules): the others must stay Off (D1).
        public void Offer(IEnumerable<string> modules)
        {
            var next = new HashSet<string>(modules);
            // A late answer must not undo a choice, nor show rows other than those a running search asked for.
            if (searching || next.SetEquals(offered))
                return;
            offered = next;
            ShowExpansions();
        }

        // UI-498: while a search runs only Cancel search responds. The choices keep their values and their own disabled
        // states, so a cancel returns every control as it was.
        public void ShowSearching(bool value)
        {
            searching = value;
            columns.SetEnabled(!value);
            back?.SetEnabled(!value);
            if (submit != null)
            {
                submit.text = value ? CancelText : SearchText;
                submit.SetEnabled(true);
            }
            progress.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            status.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
            if (!value)
            {
                ticking?.Pause();
                return;
            }
            lost = false;
            status.text = "";
            ShowElapsed(0);
            ticking?.Resume();
        }

        // The server's count, between polls continued here.
        public void ShowElapsed(int seconds)
        {
            searchStarted = Time.realtimeSinceStartup - seconds;
            ShowProgress();
        }

        public void ShowConnectionLost(bool value)
        {
            lost = value;
            ShowProgress();
        }

        public void ShowStatus(string text) => status.text = text;

        // The opponent, for the moment before the board opens. The rows make way and nothing responds any more.
        public void ShowOpponent(string name, int value)
        {
            ticking?.Pause();
            opponentName.text = name;
            opponentRating.text = "Rating " + value;
            columns.style.display = DisplayStyle.None;
            opponent.style.display = DisplayStyle.Flex;
            // The empty status takes the progress's room, so the button keeps its place.
            progress.style.display = DisplayStyle.None;
            status.style.display = DisplayStyle.Flex;
            submit.SetEnabled(false);
        }

        private void Tick()
        {
            ShowProgress();
            if (ReducedMotion())
                return;
            angle = (angle + DegreesPerSecond * TickMilliseconds / 1000f) % 360;
            indicator.style.rotate = new Rotate(angle);
        }

        private void ShowProgress()
        {
            var seconds = Mathf.Max(0, (int)(Time.realtimeSinceStartup - searchStarted));
            var text = (lost ? Lost : Searching) + seconds / 60 + ":" + (seconds % 60).ToString("00");
            if (elapsed.text != text)
                elapsed.text = text;
        }

        private bool ReducedMotion()
        {
            for (var element = Root; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        private VisualElement OpponentPanel()
        {
            opponent.AddToClassList("find-opponent");
            opponent.style.display = DisplayStyle.None;
            var heading = new Label("Your opponent") { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("host-label");
            heading.AddToClassList("host-heading");
            heading.AddToClassList("find-opponent-heading");
            opponent.Add(heading);
            opponentName.AddToClassList("find-opponent-name");
            opponent.Add(opponentName);
            opponentRating.AddToClassList("find-opponent-rating");
            opponent.Add(opponentRating);
            return opponent;
        }

        public void ShowRating(int? value) => rating.text = value.HasValue ? "Your rating: " + value.Value : "Your rating: –";

        private static VisualElement Column(params VisualElement[] parts)
        {
            var column = new VisualElement();
            column.AddToClassList("find-column");
            foreach (var part in parts)
                column.Add(part);
            return column;
        }

        private static Label Heading(string text)
        {
            var heading = new Label(text) { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("host-label");
            heading.AddToClassList("host-heading");
            return heading;
        }

        private static RankedChoiceRow Expansion(string id, string name)
        {
            var row = new RankedChoiceRow(id, name, "", RankedChoice.Off, RankedChoice.Allow, RankedChoice.Enforce);
            row.Explain(RankedChoice.Off, name + " must not be in the game.");
            row.Explain(RankedChoice.Allow, name + " is in the game if your opponent allows or enforces it.");
            row.Explain(RankedChoice.Enforce, name + " must be in the game.");
            return row;
        }

        private static RankedChoiceRow Pace(string id, string name, string limits)
        {
            return new RankedChoiceRow(id, name, limits, RankedChoice.Off, RankedChoice.Allow, RankedChoice.Enforce);
        }

        private void ShowExpansions()
        {
            foreach (var pair in expansions)
            {
                var released = offered.Contains(pair.Key);
                pair.Value.Show(released ? RankedChoice.Allow : RankedChoice.Off);
                pair.Value.Root.SetEnabled(released);
                pair.Value.Root.tooltip = released ? "" : NotReleased;
            }
        }

        // A second enforced pace lowers the first to Allow (D4), and the last pace cannot be turned off.
        private void ChoosePace(RankedChoiceRow chosen, RankedChoice value)
        {
            if (value == RankedChoice.Enforce)
                foreach (var other in paces.Where(pace => pace != chosen && pace.Value == RankedChoice.Enforce))
                    other.Show(RankedChoice.Allow);
            ShowPaces();
        }

        private void ShowPaces()
        {
            var last = paces.Count(pace => pace.Value != RankedChoice.Off) == 1;
            foreach (var pace in paces)
            {
                var name = pace.Root.Q<Label>(pace.Root.name + "-name").text;
                var locked = last && pace.Value != RankedChoice.Off;
                pace.Allow(RankedChoice.Off, !locked);
                pace.Explain(RankedChoice.Off, locked ? LastPace : "You will not play at the " + name + " pace.");
                pace.Explain(RankedChoice.Allow, "The game may use the " + name + " pace; the longest pace you share is used.");
                pace.Explain(RankedChoice.Enforce, "The game must use the " + name + " pace.");
            }
        }

        // Disabled until a side is chosen: Allow is a preference for it, Enforce a requirement (D3).
        private void ShowSideChoice()
        {
            var side = sides.Choice;
            sideChoice.Root.SetEnabled(side.HasValue);
            if (!side.HasValue)
                sideChoice.Show(RankedChoice.Allow);
            var name = side == Wotr.Contracts.Side.Shadow ? "the Shadow" : "the Free Peoples";
            sideChoice.Explain(RankedChoice.Allow, side.HasValue ? "You prefer " + name + ", but may be given the other side." : NoSide);
            sideChoice.Explain(RankedChoice.Enforce, side.HasValue ? "You play only as " + name + "." : NoSide);
        }

        private VisualElement Footer()
        {
            var footer = new VisualElement();
            footer.AddToClassList("host-footer");
            back = BoardScreen.Button("Back", () => Back?.Invoke());
            back.name = "cancel-find";
            back.AddToClassList("host-back");
            footer.Add(back);
            status.AddToClassList("host-status");
            footer.Add(status);
            progress.AddToClassList("find-progress");
            progress.style.display = DisplayStyle.None;
            indicator.AddToClassList("menu-row-emblem");
            indicator.AddToClassList("side-free");
            indicator.AddToClassList("find-indicator");
            progress.Add(indicator);
            elapsed.AddToClassList("find-elapsed");
            progress.Add(elapsed);
            footer.Add(progress);
            submit = BoardScreen.Button(SearchText, Press);
            submit.name = "submit-find";
            submit.AddToClassList("host-submit");
            submit.AddToClassList("find-submit");
            submit.tooltip = "Search for an opponent whose choices fit yours. The game starts as soon as one is found.";
            footer.Add(submit);
            return footer;
        }

        private void Press()
        {
            if (searching)
                CancelSearch?.Invoke();
            else
                Find?.Invoke(Request);
        }
    }
}
