using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-454 (owner, 2026-11-03): a recruit placed since the last refresh flies from its Reserve slot; one taken back ends its
    // flight. The first look at a scope (the match and the decision) only sets the baseline, so a restored draft, a new
    // decision or a reconnection flies nothing.
    public sealed class RecruitPlacementWatchTests
    {
        [Test]
        public void TheFirstLookAtAScopeFliesNothing()
        {
            var watch = new RecruitPlacementWatch();
            var change = watch.Observe("match/d1/plan", new[] { "rohan-elite-1", "rohan-regular-2" });
            CollectionAssert.IsEmpty(change.Added, "A draft restored with its placements flies nothing.");
            CollectionAssert.IsEmpty(change.Removed);
        }

        [Test]
        public void ARecruitPlacedSinceTheLastLookIsNewAndOneTakenBackIsGone()
        {
            var watch = new RecruitPlacementWatch();
            watch.Observe("match/d1/plan", new string[0]);
            var placed = watch.Observe("match/d1/plan", new[] { "rohan-elite-1" });
            CollectionAssert.AreEqual(new[] { "rohan-elite-1" }, placed.Added);
            CollectionAssert.IsEmpty(placed.Removed);
            var same = watch.Observe("match/d1/plan", new[] { "rohan-elite-1" });
            CollectionAssert.IsEmpty(same.Added, "An unchanged refresh flies nothing again.");
            var next = watch.Observe("match/d1/plan", new[] { "rohan-elite-1", "gondor-regular-3", "gondor-regular-4" });
            CollectionAssert.AreEqual(new[] { "gondor-regular-3", "gondor-regular-4" }, next.Added, "In the order placed.");
            var back = watch.Observe("match/d1/plan", new[] { "gondor-regular-4" });
            CollectionAssert.IsEmpty(back.Added);
            CollectionAssert.AreEqual(new[] { "gondor-regular-3", "rohan-elite-1" }, back.Removed);
        }

        [Test]
        public void ANewScopeOnlySetsTheBaselineAndRepeatsOrEmptyIdsCountOnce()
        {
            var watch = new RecruitPlacementWatch();
            watch.Observe("match/d1/muster", new string[0]);
            var other = watch.Observe("match/d2/muster", new[] { "rohan-elite-1" });
            CollectionAssert.IsEmpty(other.Added, "Another decision's placements are its baseline.");
            var twice = watch.Observe("match/d2/muster", new[] { "rohan-elite-1", "", null, "dwarves-regular-1", "dwarves-regular-1" });
            CollectionAssert.AreEqual(new[] { "dwarves-regular-1" }, twice.Added);
            CollectionAssert.IsEmpty(twice.Removed);
        }
    }
}
