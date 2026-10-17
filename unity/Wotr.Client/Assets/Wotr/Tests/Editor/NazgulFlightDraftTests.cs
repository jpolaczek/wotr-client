using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-212: a card's Nazgûl flights are distributed on the map: arm a stack once, then one Nazgûl per destination click.
    public sealed class NazgulFlightDraftTests
    {
        private static readonly string[] Targets = { "mordor", "moria", "rohan", "angmar" };

        private static TravelOption Flight(string id, string origin) => new TravelOption { Id = id, Origin = origin, Flight = true, Targets = Targets.ToList() };

        private static PlayerView View() => new PlayerView
        {
            You = Side.Shadow,
            DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "search", Kind = DecisionKind.CharacterEvent, SourceId = "base.sh.character.09" },
            Units =
            {
                new UnitView { Id = "nazgul-1", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "mordor" },
                new UnitView { Id = "nazgul-2", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "mordor" },
                new UnitView { Id = "nazgul-3", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "moria" }
            },
            Characters = { new CharacterView { Id = "witch-king", Side = Side.Shadow, Location = "mordor" } },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.MoveShadowCharacters, Enabled = true, AllowEmptySelection = true,
                    EligibleIds = { "nazgul-1", "nazgul-2", "nazgul-3", "witch-king" },
                    Travel = { Flight("nazgul-2", "mordor"), Flight("nazgul-1", "mordor"), Flight("witch-king", "mordor"), Flight("nazgul-3", "moria") }
                }
            }
        };

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            return draft;
        }

        [Test]
        public void OneArmedStackFliesItsNazgulOneByOneAndTheWitchKingOnlyWhenArmedHimself()
        {
            var view = View();
            var draft = Draft();
            Assert.IsTrue(NazgulFlightDraft.Active(view, draft));
            CollectionAssert.AreEquivalent(new[] { "mordor", "moria" }, NazgulFlightDraft.Sources(view, draft).Select(s => s.RegionId));
            Assert.IsFalse(NazgulFlightDraft.Fly(view, draft, "rohan"), "Nothing is armed yet.");
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "mordor"));
            Assert.AreEqual("nazgul-1", NazgulFlightDraft.Next(view, draft).Id);
            Assert.IsFalse(NazgulFlightDraft.Fly(view, draft, "mordor"), "A flight to its own region is no move.");
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "rohan"));
            Assert.AreEqual("mordor", draft.ArmedSource, "The stack stays armed while it has an unmoved Nazgûl.");
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "rohan"));
            Assert.AreEqual("", draft.ArmedSource, "Only the Witch-king is left: the region is released.");
            Assert.AreEqual(1, draft.Payload.CompanionMoves.Count, "Two Nazgûl from one origin to one destination fly as one group.");
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "mordor"));
            Assert.AreEqual("witch-king", draft.ArmedSource, "A region holding only the Witch-king arms him.");
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "angmar"));
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "moria"));
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "mordor"));
            Assert.AreEqual(4, NazgulFlightDraft.Moved(draft));
            CollectionAssert.AreEqual(new[] { "nazgul-1", "nazgul-2" }, draft.Payload.CompanionMoves[0].Ids);
            CollectionAssert.AreEqual(new[] { "mordor", "rohan" }, draft.Payload.CompanionMoves[0].Route);
            CollectionAssert.AreEqual(new[] { "moria", "mordor" }, draft.Payload.CompanionMoves[2].Route);
            Assert.IsEmpty(NazgulFlightDraft.Sources(view, draft));
        }

        [Test]
        public void AFigureArmsItselfAndBackUndoesTheLastStep()
        {
            var view = View();
            var draft = Draft();
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "witch-king"));
            Assert.AreEqual("mordor", NazgulFlightDraft.ArmedRegion(view, draft));
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "angmar"));
            Assert.AreEqual("", draft.ArmedSource, "An armed figure is released after its flight.");
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "mordor"));
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "moria"));
            draft.Back();
            Assert.AreEqual(1, NazgulFlightDraft.Moved(draft), "Back undoes the last flight.");
            Assert.AreEqual("mordor", draft.ArmedSource, "and restores the armed region.");
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "mordor"), "Arming the armed region releases it.");
            Assert.AreEqual("", draft.ArmedSource);
            Assert.IsFalse(NazgulFlightDraft.Arm(view, draft, "rohan"), "A region without an unmoved Nazgûl is no source.");
        }

        [Test]
        public void TheMapAndTheConsoleShowTheFlightsAndTheWindowStaysClosed()
        {
            var view = View();
            var draft = Draft();
            Assert.IsNull(NazgulFlightDraft.Armed(view, draft));
            Assert.That(NazgulFlightDraft.Hint(view, draft), Does.Contain("moving none is allowed"));
            NazgulFlightDraft.Arm(view, draft, "mordor");
            Assert.AreEqual("mordor", NazgulFlightDraft.Armed(view, draft).RegionId);
            Assert.That(NazgulFlightDraft.Hint(view, draft), Does.StartWith("Click a lit region"));
            NazgulFlightDraft.Fly(view, draft, "rohan");
            var planned = NazgulFlightDraft.Planned(view, draft).Single();
            Assert.AreEqual("rohan", planned.RegionId);
            Assert.AreEqual(UnitKind.Nazgul, planned.Kind);
            Assert.AreEqual("Nazgûl moved 1", CardDecisions.Progress(view, draft));
            view.EffectTrail.Add(new EffectTrailView { SourceId = "base.sh.character.09" });
            var root = new VisualElement();
            var tray = new DecisionTray(root);
            tray.Render(view, draft, false, false);
            Assert.IsNotNull(root.Q(className: "committed-choice"), "The trail recalls the played card.");
            Assert.IsFalse(tray.HasChoices, "The flights are chosen on the map and the trail is no choice: no window.");
            Assert.IsNull(root.Q<Button>("new-companion-group"));
        }
    }
}
