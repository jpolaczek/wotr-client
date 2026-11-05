using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-248: a payment by one of several different action dice is made in the own dice pool; the decision window does
    // not list the dice again, and Palantír of Orthanc takes its Ring on the Ring itself.
    public sealed class CostDiceTests
    {
        [Test]
        public void ACharactersDifferentEntryDiceAreChosenInThePool()
        {
            var view = View(CommandKind.EnterCharacter);
            view.Options[0].CharacterActions.Add(new CharacterActionOption
                { CharacterId = "base.character.gandalf-white", Dice = { "will", "character" }, Targets = { "fangorn" } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            draft.Change(p => p.CharacterId = "base.character.gandalf-white");
            CollectionAssert.AreEquivalent(new[] { "will", "character" }, CostDice.Offered(view, draft));
            Assert.IsTrue(DiceDecision.Active(view, draft), "The pool's frame lights.");
            draft.Change(p => p.DieId = "will");
            Assert.IsFalse(CostDice.Glows(view, draft, "will"), "The chosen die is selected, not glowing.");
            Assert.IsTrue(CostDice.Glows(view, draft, "character"), "The other die stays a choice.");

            view.Dice.Single(d => d.Id == "character").Face = DieFace.WillOfTheWest;
            Assert.IsEmpty(CostDice.Offered(view, draft), "Identical dice leave nothing to choose (UI-239).");
        }

        [Test]
        public void PalantirOfOrthancListsNoDiceOrRingsAndAWillDieDropsTheRing()
        {
            var view = View(CommandKind.DiscardPalantir);
            view.Options[0].EligibleDice.AddRange(new[] { "will", "character" });
            view.Options[0].EligibleIds.Add("base.ring.narya");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardPalantir, "character");
            draft.Change(p => p.RingId = "base.ring.narya");
            Assert.IsTrue(CostDice.Glows(view, draft, "will"));
            Assert.IsTrue(CostDice.DropsRing(view, draft, "will"), "A Will die pays alone.");
            Assert.IsFalse(CostDice.DropsRing(view, draft, "character"));

            var host = new VisualElement();
            var tray = new DecisionTray(host);
            tray.Render(view, draft, false, false);
            Assert.IsNull(host.Q<Button>("cost-will"), "The pool holds the dice.");
            Assert.IsNull(host.Q<Button>("cost-character"));
            Assert.IsNull(host.Q<Button>("ring-cost-base.ring.narya"), "The Ring is chosen on the Ring.");
            Assert.IsNull(host.Q<Button>("palantir-no-ring"));
        }

        // UI-272: the Lords' Lore of the Elven Rings is removed like Palantír of Orthanc: dice in the pool, the Ring on
        // its socket, a Will die alone; the hint asks for the Ring while a non-Will die waits for one.
        [Test]
        public void LoreRemovalListsNoDiceOrRingsAndAsksForTheRingOnItsSocket()
        {
            var view = View(CommandKind.DiscardLore);
            view.Options[0].EligibleDice.AddRange(new[] { "will", "character" });
            view.Options[0].DiceWithoutRing.Add("will");
            view.Options[0].EligibleIds.Add("lords.ring.vilya");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardLore, "character");
            CollectionAssert.AreEquivalent(new[] { "will", "character" }, CostDice.Offered(view, draft));
            StringAssert.StartsWith("Click a lit Elven Ring", CostDice.RingHint(view, draft));
            Assert.IsTrue(CostDice.DropsRing(view, draft, "will"), "A Will die pays alone.");
            draft.Change(p => p.RingId = "lords.ring.vilya");
            Assert.AreEqual("", CostDice.RingHint(view, draft));

            var host = new VisualElement();
            var tray = new DecisionTray(host);
            tray.Render(view, draft, false, false);
            Assert.IsNull(host.Q<Button>("cost-will"), "The pool holds the dice.");
            Assert.IsNull(host.Q<Button>("ring-cost-lords.ring.vilya"), "The Ring is chosen on its socket.");
            Assert.IsNull(host.Q<Button>("lore-no-ring"));
        }

        // UI-272: The Gates are Closed takes one of its eligible dice in the pool.
        [Test]
        public void GatesRemovalTakesItsDieInThePool()
        {
            var view = View(CommandKind.DiscardGates);
            view.Options[0].EligibleDice.AddRange(new[] { "will", "character" });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardGates, "will");
            Assert.IsTrue(CostDice.Glows(view, draft, "character"));
            Assert.AreEqual("", CostDice.RingHint(view, draft), "No Ring pays for the Gates.");
            var host = new VisualElement();
            new DecisionTray(host).Render(view, draft, false, false);
            Assert.IsNull(host.Q<Button>("cost-character"));
        }

        private static PlayerView View(CommandKind kind) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Phase = GamePhase.ActionResolution,
            Decision = new DecisionView { Id = "pay", Kind = DecisionKind.Action },
            Fellowship = new FellowshipView { RegionId = "rivendell" },
            Dice =
            {
                new DieView { Id = "will", Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available },
                new DieView { Id = "character", Owner = Side.FreePeoples, Face = DieFace.Character, Source = "standard", Location = DieLocation.Available }
            },
            Options = { new ActionOption { Kind = kind, Enabled = true, Minimum = 0, Maximum = 1 } }
        };
    }
}
