using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-250: companion groups are built by clicks alone: a companion that cannot join the current group, or any
    // companion once the current group has its route, starts the next group; a planned member selects its group; a
    // group left without members disappears.
    public sealed class CompanionGroupGestureTests
    {
        [Test]
        public void GroupsFormSelectAndDisappearWithoutGroupButtons()
        {
            var view = new PlayerView { You = Side.FreePeoples };
            var option = new ActionOption { Kind = CommandKind.MoveCompanions, Enabled = true, Maximum = 4 };
            foreach (var (id, origin) in new[] { ("gimli", "erebor"), ("legolas", "erebor"), ("boromir", "erebor"), ("merry", "bree") })
            {
                option.EligibleIds.Add(id);
                option.Travel.Add(new TravelOption { Id = id, Origin = origin, Range = 3, Targets = { origin, "dale" } });
            }
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveCompanions);

            TravelDraft.ToggleCompanion(draft, view, option, "gimli");
            TravelDraft.ToggleCompanion(draft, view, option, "legolas");
            Assert.AreEqual(1, draft.Payload.CompanionMoves.Count, "Companions from one place join the current group.");
            CollectionAssert.AreEqual(new[] { "gimli", "legolas" }, draft.Payload.CompanionMoves[0].Ids);

            Assert.IsFalse(TravelDraft.CanToggle(draft, view, option, "merry"), "Merry cannot join Gimli's group…");
            Assert.IsTrue(TravelDraft.Selectable(draft, view, option, "merry"), "…but his click starts the next group.");
            TravelDraft.ToggleCompanion(draft, view, option, "merry");
            Assert.AreEqual(2, draft.Payload.CompanionMoves.Count, "A companion from elsewhere starts the next group.");
            Assert.AreEqual(1, draft.CompanionFocus);

            TravelDraft.ToggleCompanion(draft, view, option, "gimli");
            Assert.AreEqual(0, draft.CompanionFocus, "A planned member selects its group.");
            CollectionAssert.AreEqual(new[] { "gimli", "legolas" }, draft.Payload.CompanionMoves[0].Ids, "Selecting does not toggle.");

            draft.Change(_ => TravelDraft.Group(draft).Route.AddRange(new[] { "erebor", "dale" }));
            TravelDraft.ToggleCompanion(draft, view, option, "boromir");
            Assert.AreEqual(3, draft.Payload.CompanionMoves.Count, "A group with its route is finished; the next click starts another.");
            CollectionAssert.AreEqual(new[] { "boromir" }, draft.Payload.CompanionMoves[2].Ids);

            TravelDraft.ToggleCompanion(draft, view, option, "merry");
            TravelDraft.ToggleCompanion(draft, view, option, "merry");
            Assert.AreEqual(2, draft.Payload.CompanionMoves.Count, "A group left without members disappears.");
            Assert.IsFalse(draft.Payload.CompanionMoves.Exists(g => g.Ids.Contains("merry")));
        }
    }
}
