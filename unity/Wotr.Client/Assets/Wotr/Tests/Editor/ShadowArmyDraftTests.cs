using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ShadowArmyDraftTests
    {
        [Test]
        public void FreeManeuverNeedsItsCommanderAndPreservesBothGroupsAcrossUndoAndReconnect()
        {
            var view = View();
            var draft = new TurnDraft();
            BoardDraft.SelectRegion(draft, view, "a");
            ArmyDraft.Toggle(draft, new[] { "nazgul-a" });
            Assert.IsEmpty(ArmyDraft.Targets(draft, view));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            draft.Back();
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "c"));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "b"));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "c"));
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsFalse(ArmyDraft.Start(draft, view, "a"));
            draft.Reconcile(view);
            Assert.AreEqual(2, draft.Payload.Moves.Count);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsEmpty(draft.Payload.Moves[0].UnitIds.Intersect(draft.Payload.Moves[1].UnitIds));
        }

        [Test]
        public void PalantirCostNeverAssumesARingAndUndoRestoresTheExactSelection()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.DiscardPalantir,
                EligibleDice = { "will", "army" },
                DiceWithoutRing = { "will" },
                EligibleIds = { "ring" }
            };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            draft.Change(p => p.DieId = "army");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.RingId = "ring");
            Assert.IsTrue(draft.Ready(option));
            draft.Back();
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.DieId = "will");
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.RingId);
        }

        [Test]
        public void MapSelectsTheArmyIdAndNeverGuessesBetweenTwoGroupsInOneRegion()
        {
            var option = new ActionOption
            {
                EligibleIds = { "moria:field" },
                EffectChoices = { new EffectChoiceView { Id = "moria:field", TargetId = "moria" } }
            };
            Assert.AreEqual("moria:field", EffectChoiceDraft.RegionChoice(option, "moria"));
            Assert.IsEmpty(EffectChoiceDraft.RegionChoice(option, "hollin"));
            option.EligibleIds.Add("moria:garrison");
            option.EffectChoices.Add(new EffectChoiceView { Id = "moria:garrison", TargetId = "moria" });
            Assert.IsEmpty(EffectChoiceDraft.RegionChoice(option, "moria"));
        }

        [Test]
        public void RingDragKeepsPalantirRemovalAndDoesNotStartADieConversion()
        {
            var option = new ActionOption { Kind = CommandKind.DiscardPalantir, EligibleDice = { "die" }, EligibleIds = { "ring" } };
            var view = new PlayerView { You = Side.FreePeoples, Decision = new DecisionView { Kind = DecisionKind.Action }, Options = { option } };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Ring, "ring"), new DraftToken(DraftObject.Die, "die")));
            Assert.AreEqual(CommandKind.DiscardPalantir, draft.Kind);
            Assert.IsTrue(draft.Ready(option));
            Assert.IsNull(draft.Payload.Face);
            draft.Back();
            Assert.IsFalse(draft.Ready(option));
        }

        private static PlayerView View()
        {
            var option = new ActionOption { Kind = CommandKind.MoveArmies, Maximum = 2, ArmyEvent = new ArmyEventOption() };
            var view = new PlayerView { You = Side.Shadow, Decision = new DecisionView { Kind = DecisionKind.ArmyEvent }, Options = { option } };
            foreach (var from in new[] { "a", "b" })
            {
                view.Units.Add(new UnitView { Id = "unit-" + from, Side = Side.Shadow, Kind = UnitKind.Regular, Location = from });
                view.Units.Add(new UnitView { Id = "nazgul-" + from, Side = Side.Shadow, Kind = UnitKind.Nazgul, Location = from });
                var army = new ArmyOption { RegionId = from, UnitIds = { "unit-" + from, "nazgul-" + from }, RequiredAnyIds = { "nazgul-" + from } };
                army.Targets.Add(new ArmyTargetOption { RegionId = "c", EligibleIds = { "unit-" + from, "nazgul-" + from } });
                option.Armies.Add(army);
            }
            return view;
        }
    }
}
