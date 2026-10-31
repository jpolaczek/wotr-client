using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-432 (owner's rule, UI-430): a window list that repeats an object always on screen goes. The Fellowship tray keeps
    // only Undo step and Cancel of a drawn route; the decision window lists no die of an unsent Discard die and no deck
    // (the table cards' removals keep their dice on this branch, and the kings' lists are the expansions'); a
    // character's actions stay as the fallback for the moments its row offers none.
    public sealed class WindowListsTests
    {
        private const string Legolas = "base.character.legolas";
        private const string FarSighted = "kings.far-sighted";

        [Test]
        public void TheFellowshipTrayListsNoStayStayAllOrTravelMode()
        {
            var view = Separation(withRuler: true);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions, "ruler");
            draft.Change(p => p.Ids.Add(Legolas));
            var root = Build(view, draft, contextual: false);
            foreach (var name in new[] { "travel-stay", "travel-stay-all", "travel-standard", "travel-ruler" })
                Assert.IsNull(root.Q<Button>(name), name + " is its object's click now.");
            Assert.IsNull(root.Q<Label>("travel-far-sighted"), "The standard move names no mode.");
            Assert.IsNotNull(root.Q<Button>("route-cancel"), "Cancel stays: no object clears the separation's route.");

            draft.Change(p => p.AbilityId = FarSighted);
            root = Build(view, draft, contextual: false);
            Assert.AreEqual("Far-sighted · Range +1", root.Q<Label>("travel-far-sighted").text, "Thranduil's move names its mode.");
        }

        [Test]
        public void ADrawnRouteKeepsUndoStepAndCancel()
        {
            var view = Separation(withRuler: false);
            view.Options[0].Kind = CommandKind.DeclareFellowship;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DeclareFellowship);
            var root = Build(view, draft, contextual: true);
            Assert.IsNotNull(root.Q<Button>("route-undo"), "The console's ↶ is off in the Fellowship panel.");
            Assert.IsNotNull(root.Q<Button>("route-cancel"));
        }

        // Stay here was TravelDraft.Stay; the click on the lit origin region (TravelDraft.RouteTo) makes the same route.
        [Test]
        public void TheOriginRegionsClickStaysAsTheOldStayHereButtonDid()
        {
            var view = Separation(withRuler: false);
            var option = view.Options[0];
            var button = SeparationDraft();
            TravelDraft.Stay(button, option);
            var region = SeparationDraft();
            var data = BoardPresentation.Load();
            Assert.IsTrue(TravelDraft.Reachable(region, view, data).Contains("rivendell"), "The origin is lit.");
            Assert.IsTrue(TravelDraft.RouteTo(region, view, data, "rivendell"));
            Assert.AreEqual(JsonConvert.SerializeObject(button.Payload), JsonConvert.SerializeObject(region.Payload));
            CollectionAssert.AreEqual(new[] { "rivendell" }, region.Payload.Route);
        }

        // Stay all cleared the plan; with an empty plan the ✓ sends that same empty plan.
        [Test]
        public void AnEmptyPlanIsWhatStayAllSent()
        {
            var option = new ActionOption { Kind = CommandKind.MoveCompanions, Enabled = true, AllowEmptySelection = true, EligibleIds = { Legolas } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveCompanions);
            var before = JsonConvert.SerializeObject(draft.Payload);
            draft.Change(p => p.CompanionMoves.Clear());
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload), "Stay all changed nothing in an empty plan.");
            Assert.IsTrue(FellowshipDraft.Ready(draft, option), "The ✓ sends the empty plan.");
        }

        // Discard die's unsent step (offline, refused) lists no die: the pool's die is the same click (ChooseDie).
        [Test]
        public void AnUnsentDieDiscardListsNoDie()
        {
            var view = ActionView(new ActionOption { Kind = CommandKind.DiscardDie, Enabled = true, EligibleDice = { "a", "b" } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardDie, "a");
            var root = new VisualElement();
            var tray = new DecisionTray(root);
            tray.Render(view, draft, false, false);
            CollectionAssert.IsEmpty(Costs(root));
            Assert.IsFalse(tray.HasChoices, "The window opens for no list of dice.");
        }

        // A draw is made on the hand's decks (DirectCard); even a tray built without it lists neither deck nor die.
        [Test]
        public void ADrawListsNoDeckOrDie()
        {
            var view = ActionView(new ActionOption { Kind = CommandKind.DrawCard, Enabled = true, EligibleDice = { "a" }, Targets = { "character", "strategy" } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DrawCard, "a");
            var root = new VisualElement();
            new DecisionTray(root).Render(view, draft, false, false);
            CollectionAssert.IsEmpty(root.Query<Button>().ToList().Where(b => b.name.StartsWith("deck-") || b.name.StartsWith("cost-")).Select(b => b.name));
        }

        private static string[] Costs(VisualElement root) =>
            root.Query<Button>().ToList().Where(b => b.name.StartsWith("cost-")).Select(b => b.name).ToArray();

        private static VisualElement Build(PlayerView view, TurnDraft draft, bool contextual)
        {
            var root = new VisualElement();
            new FellowshipChoices(root, view, draft, change => change(), BoardPresentation.Load(), contextual: contextual).Refresh();
            return root;
        }

        private static TurnDraft SeparationDraft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions, "c");
            draft.Change(p => p.Ids.Add(Legolas));
            return draft;
        }

        private static PlayerView Separation(bool withRuler)
        {
            var option = new ActionOption
            {
                Kind = CommandKind.SeparateCompanions, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { Legolas }, EligibleDice = { "c" },
                Travel = { new TravelOption { Id = Legolas, Origin = "rivendell", Range = 1, Targets = { "rivendell", "eregion" } } }
            };
            if (withRuler)
                option.RulerTravel = new RulerTravelOption { AbilityId = FarSighted, Dice = { "ruler" } };
            var view = ActionView(option);
            view.Dice.Add(new DieView { Id = "ruler", Face = DieFace.Character, Source = "ruler", Owner = Side.FreePeoples, Location = DieLocation.Available });
            return view;
        }

        private static PlayerView ActionView(ActionOption option) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Options = { option },
            Dice =
            {
                new DieView { Id = "a", Face = DieFace.Character, Source = "standard", Owner = Side.FreePeoples, Location = DieLocation.Available },
                new DieView { Id = "b", Face = DieFace.Muster, Source = "standard", Owner = Side.FreePeoples, Location = DieLocation.Available },
                new DieView { Id = "c", Face = DieFace.Character, Source = "standard", Owner = Side.FreePeoples, Location = DieLocation.Available }
            }
        };
    }
}
