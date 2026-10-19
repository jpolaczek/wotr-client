using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-31: the menu window's demo page. The side cards are the only question: a click on one is the complete
    // choice and starts the demo against the CPU at once (UI-142). DEMO-06: above them the same rules as a waiting
    // room (RulesStrip in one row), set before the click; only the modules this server releases can be chosen.
    public sealed class DemoStartPage
    {
        public VisualElement Root { get; } = new VisualElement { name = "demo-start" };
        public event Action<Side, MatchConfiguration> Start;
        public event Action Back;
        public RulesStrip Rules { get; } = new RulesStrip(row: true);
        public MatchConfiguration Configuration { get; private set; } = new MatchConfiguration();
        private readonly SideChoice sides = new SideChoice("demo");
        private List<string> offered = new List<string>();

        public DemoStartPage()
        {
            Root.AddToClassList("host-form");
            Root.Add(Note("A private game against a simple CPU, to learn the game at your own pace. Choose the rules, then your side to begin."));
            Rules.Toggled += Toggle;
            Root.Add(Rules.Root);
            sides.Chosen += side => Start?.Invoke(side, Configuration);
            Root.Add(sides.Root);
            Root.Add(Note("A new demo replaces your previous one."));
            Root.Add(Footer());
            Refresh();
        }

        // The modules the server releases (GET /api/rooms/offered-modules); none until it answers.
        public void Offer(IEnumerable<string> modules)
        {
            offered = new List<string>(modules);
            Refresh();
        }

        private void Toggle(string rule)
        {
            Configuration = RulesChoices.Toggle(Configuration, rule);
            Refresh();
        }

        // The page is the host of a table it has not made yet.
        private void Refresh() => Rules.Show(new RoomView
        {
            IsHost = true,
            Status = RoomStatus.Waiting,
            Configuration = Configuration,
            OfferedModules = offered,
            ExpansionsAvailable = offered.Count > 0
        }, false, readiness: false);

        private static Label Note(string text)
        {
            var note = new Label(text) { pickingMode = PickingMode.Ignore };
            note.AddToClassList("demo-note");
            return note;
        }

        private VisualElement Footer()
        {
            var footer = new VisualElement();
            footer.AddToClassList("host-footer");
            var back = BoardScreen.Button("Back", () => Back?.Invoke());
            back.name = "cancel-demo";
            back.AddToClassList("host-back");
            footer.Add(back);
            return footer;
        }
    }
}
