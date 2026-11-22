using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-568 and UI-569 (owner, 2026-11-22, after players' games): the game says what the rules leave out. Faramir's Rangers
    // recruits only with a Free Peoples Army in Osgiliath; a Companion who enters a Shadow Stronghold stops there, so Strider
    // leaving Rivendell with range 9 cannot reach Minas Tirith, 8 regions away through Moria and 10 around it.
    public sealed class ReachNotesTests
    {
        private const string Strider = "base.character.strider";
        private static readonly string[] ShadowStrongholds =
            { "barad-dur", "dol-guldur", "minas-morgul", "morannon", "moria", "mount-gundabad", "orthanc", "umbar" };

        [Test]
        public void FaramirsRangersSaysItRecruitsNothingWithoutAnArmyInOsgiliath()
        {
            var view = new PlayerView { You = Side.FreePeoples };
            Assert.AreEqual(CardPlayNotes.BeforePlay, CardPlayNotes.Play(view, CardPlayNotes.FaramirsRangers));
            Assert.AreEqual(CardPlayNotes.AfterRoll, CardPlayNotes.Roll(view, CardPlayNotes.FaramirsRangers));
            Assert.AreEqual("", CardPlayNotes.Play(view, "base.fp.strategy.05"), "Another card says nothing.");
            view.Units.Add(new UnitView { Id = "shadow", Nation = "sauron", Kind = UnitKind.Regular, Side = Side.Shadow, Location = "osgiliath" });
            Assert.AreEqual(CardPlayNotes.BeforePlay, CardPlayNotes.Play(view, CardPlayNotes.FaramirsRangers), "A Shadow Army is no help.");
            view.Units.Add(new UnitView { Id = "rohan", Nation = "rohan", Kind = UnitKind.Regular, Side = Side.FreePeoples, Location = "osgiliath" });
            Assert.AreEqual("", CardPlayNotes.Play(view, CardPlayNotes.FaramirsRangers), "Any Free Peoples Army recruits, Gondor or not.");
            Assert.AreEqual("", CardPlayNotes.Roll(view, CardPlayNotes.FaramirsRangers));
        }

        [Test]
        public void StriderLeavingRivendellLearnsThatMoriaStopsHim()
        {
            var data = BoardPresentation.Load();
            var view = Separation(data, out var draft);
            var reachable = TravelDraft.Reachable(draft, view, data);
            Assert.IsTrue(reachable.Contains("druadan-forest"), "Nine regions around Moria.");
            Assert.IsFalse(reachable.Contains("minas-tirith"));
            Assert.AreEqual("Out of reach: Moria stops a Companion who enters it (a Shadow Stronghold) · 10 regions around it, range 9",
                TravelDraft.ReachNote(draft, view, data, "minas-tirith"));
            Assert.AreEqual("", TravelDraft.ReachNote(draft, view, data, "druadan-forest"), "A reachable region needs no note.");
            Assert.AreEqual("", TravelDraft.ReachNote(draft, view, data, "umbar"), "Too far by any way: no stop to blame.");
            var hint = TravelDraft.DestinationHint(view, draft, data);
            StringAssert.StartsWith(CharacterLibrary.Name(view, Strider) + " · ", hint);
            StringAssert.Contains("Moria", hint);
            StringAssert.Contains("a Companion who enters", hint);
            StringAssert.EndsWith(" · Click a highlighted region to move there.", hint, "The instruction stays last.");
        }

        [Test]
        public void NothingIsSaidOutsideAWalkingCompanionsDestination()
        {
            var data = BoardPresentation.Load();
            var view = Separation(data, out var draft);
            draft.Change(p => p.Ids.Clear());
            Assert.AreEqual("", TravelDraft.ReachNote(draft, view, data, "minas-tirith"), "No Companion chosen yet.");
            Assert.AreEqual("", TravelDraft.StopHint(view, draft, data));
            Assert.AreEqual("", TravelDraft.ReachNote(new TurnDraft(), view, data, "minas-tirith"), "Another step.");
        }

        private static PlayerView Separation(BoardPresentation data, out TurnDraft draft)
        {
            var view = new PlayerView
            {
                MatchId = "reach",
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
                Fellowship = new FellowshipView { RegionId = "rivendell", Progress = 6 }
            };
            foreach (var stronghold in ShadowStrongholds)
                view.Control[stronghold] = Side.Shadow;
            var travel = new TravelOption { Id = Strider, Origin = "rivendell", Range = 9 };
            travel.StopRegions.AddRange(ShadowStrongholds);
            travel.Targets.AddRange(data.Regions.Keys);
            var separation = new ActionOption { Kind = CommandKind.SeparateCompanions, Enabled = true, EligibleIds = { Strider } };
            separation.Travel.Add(travel);
            view.Options.Add(separation);
            draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.SeparateCompanions);
            if (!draft.Payload.Ids.Contains(Strider))
                draft.Change(p => p.Ids.Add(Strider));
            return view;
        }
    }
}
