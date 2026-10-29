using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // UI-380 (owner's report, 2026-10-28): Brand's and Dáin's Ruler token from the test menu, as the owner plays it. The
    // token's click takes the Ruler die; the Characters tab, the king's row and his ability's chip glow; the ability's
    // action belongs to its chip (Click to use only while it is open), and the other ability shows no button.
    public sealed class RulerTokenMenuTests
    {
        [UnityTest]
        public IEnumerator BrandsTokenLightsTheCharactersTabHisRowAndCallToArms() =>
            MatchTestRoom.Wait(Token("kings-ruler-token-brand", "brand", "call-to-arms", "heir-of-bard"));

        [UnityTest]
        public IEnumerator DainsTokenLightsTheCharactersTabHisRowAndIronGuard() =>
            MatchTestRoom.Wait(Token("kings-ruler-token-dain", "dain", "iron-guard", "dwarvenking"));

        private static async Task Token(string scenario, string king, string ability, string other)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui380-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, scenario, token);
            await Task.Delay(500, token);
            var board = client.Board;
            var ruler = board.OwnDice.Q<Button>("ruler-token-kings.token." + king);
            Assert.IsNotNull(ruler, king + "'s token stands in the pool");
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            Assert.AreEqual("kings.die.fp.ruler", board.ActionDie.DieId, "The token's click takes its only die, the Ruler die.");
            Assert.IsTrue(board.Characters.Tab.ClassListContains("legal-target"), "The Characters tab glows.");
            LayoutTests.Save(client.Panel.targetTexture, "ui380-" + king + "-token.png");
            board.Characters.Open();
            await Task.Delay(400, token);
            var id = "kings.sovereign." + king;
            Assert.IsTrue(board.Characters.Root.Q<Button>("character-roster-" + id).ClassListContains("legal-target"), "His row glows.");
            board.Characters.Show(id, true);
            await Task.Delay(500, token);
            var inspector = board.Characters.Inspector.Root;
            Assert.IsNull(inspector.Q<Button>("component-character-" + id + "-kings." + ability),
                "The ability's action stands on its chip, not as a separate button.");
            var chip = inspector.Q<Button>("character-ability-" + ability);
            Assert.IsTrue(chip.ClassListContains(ChoiceGlow.ClassName), "The usable ability's chip glows.");
            PointerGestureEvents.ClickAt(inspector.Q<Button>("character-ability-" + other));
            await Task.Delay(400, token);
            Assert.IsFalse(Shown(inspector.Q<Button>("character-ability-use-kings." + ability)), "The other ability shows no button.");
            PointerGestureEvents.ClickAt(chip);
            await Task.Delay(400, token);
            Assert.IsTrue(Shown(inspector.Q<Button>("character-ability-use-kings." + ability)), "The open ability offers Click to use.");
            LayoutTests.Save(client.Panel.targetTexture, "ui380-" + king + "-ability.png");
        }

        // UI-384 (owner): the Ruler die's Muster lights Politics, Reserve and Characters, and every lit tab glows alike.
        [UnityTest]
        public IEnumerator EveryLitTabGlowsAlike() => MatchTestRoom.Wait(Tabs());

        private static async Task Tabs()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui384-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-brand-call-to-arms", token);
            await Task.Delay(500, token);
            var board = client.Board;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("kings.die.fp.ruler"));
            await Task.Delay(1200, token);
            var tabs = new VisualElement[] { board.Nations.Root, board.Reinforcements.Tab, board.Characters.Tab };
            foreach (var tab in tabs)
                Assert.IsTrue(tab.ClassListContains("legal-target"), tab.name + " glows for the Muster die.");
            LayoutTests.Save(client.Panel.targetTexture, "ui384-tabs.png");
            foreach (var tab in tabs.Skip(1))
            {
                Assert.That(Vector4.Distance(tab.resolvedStyle.backgroundColor, tabs[0].resolvedStyle.backgroundColor), Is.LessThan(0.08f),
                    tab.name + " shares the lit tabs' plate.");
                Assert.That(Vector4.Distance(tab.resolvedStyle.borderTopColor, tabs[0].resolvedStyle.borderTopColor), Is.LessThan(0.08f),
                    tab.name + " shares the lit tabs' rim.");
            }
            // UI-386 (owner): a second click on the chosen die releases it, and the tabs it lit go dark.
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("kings.die.fp.ruler"));
            await Task.Delay(600, token);
            Assert.AreEqual("", board.ActionDie.DieId, "The second click releases the die.");
            foreach (var tab in tabs)
                Assert.IsFalse(tab.ClassListContains("legal-target"), tab.name + " no longer glows.");
        }

        // UI-389 (owner's report): Thranduil's token lights all its uses at once — the Fellowship's move (its plaque and its
        // figure on the map) and his own row — and none of the Muster die's; a second click releases the token with its
        // Ruler die and nothing stays lit; the Fellowship's move then carries Far-sighted, and the server takes it.
        [UnityTest]
        public IEnumerator ThranduilsTokenLightsAllItsUsesAndASecondClickReleasesIt() => MatchTestRoom.Wait(Thranduil());

        private static async Task Thranduil()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui389-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-ruler-token-thranduil", token);
            await Task.Delay(500, token);
            var board = client.Board;
            var ruler = board.OwnDice.Q<Button>("ruler-token-kings.token.thranduil");
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            Assert.AreEqual("kings.die.fp.ruler", board.ActionDie.DieId);
            Assert.IsTrue(board.Fellowship.Status.ClassListContains(DieOptionGlow.LitClass), "The Fellowship plaque glows for the token's move.");
            Assert.IsTrue(board.Map.FellowshipMarker.MoveOffered, "The Fellowship's figure offers the token's move.");
            Assert.IsTrue(board.Characters.Tab.ClassListContains("legal-target"), "Thranduil's own move lights the Characters tab.");
            Assert.IsFalse(board.Nations.Root.ClassListContains(DieOptionGlow.LitClass), "The Muster die's Politics stays dark.");
            Assert.IsFalse(board.Reinforcements.Tab.ClassListContains(DieOptionGlow.LitClass), "The Muster die's recruitment stays dark.");
            LayoutTests.Save(client.Panel.targetTexture, "ui389-thranduil-token.png");
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            Assert.AreEqual("", board.ActionDie.DieId, "The second click releases the token and its Ruler die.");
            Assert.IsFalse(ruler.ClassListContains("selected"));
            Assert.IsFalse(board.Fellowship.Status.ClassListContains(DieOptionGlow.LitClass));
            Assert.IsFalse(board.Nations.Root.ClassListContains(DieOptionGlow.LitClass), "No other use lights in its place.");
            Assert.IsFalse(board.Characters.Tab.ClassListContains("legal-target"));
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            var progress = board.View.Fellowship.Progress;
            board.Map.FellowshipMarker.Activate();
            await Until(() => board.View.Fellowship.Progress == progress + 1, token);
            Assert.AreNotEqual("free-peoples", board.View.RulerTokens.Single(t => t.Id == "kings.token.thranduil").Location,
                "The server took the token's move with Far-sighted.");
        }

        // UI-389: Denethor's token lights both its uses — the decks and the Event card its Event face pays for — and none
        // of the Muster die's; a second click releases it with the Ruler die.
        [UnityTest]
        public IEnumerator DenethorsTokenLightsTheDecksAndTheCardButNoMusterUse() => MatchTestRoom.Wait(Denethor());

        private static async Task Denethor()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui389-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-fp-ruler-token", token);
            await Task.Delay(500, token);
            var board = client.Board;
            var ruler = board.OwnDice.Q<Button>("ruler-token-kings.token.denethor");
            var decks = board.Decks.Root.Query<Button>(className: "card-deck").ToList();
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            Assert.AreEqual("kings.die.fp.ruler", board.ActionDie.DieId);
            foreach (var deck in decks)
                Assert.IsTrue(deck.ClassListContains(DieOptionGlow.LitClass), deck.name + " glows for the token's draw.");
            Assert.IsTrue(board.Hand.Q<VisualElement>("base.fp.character.07").ClassListContains("playable-card"), "Horn of Gondor glows.");
            Assert.IsFalse(board.Nations.Root.ClassListContains(DieOptionGlow.LitClass), "The Muster die's Politics stays dark.");
            Assert.IsFalse(board.Reinforcements.Tab.ClassListContains(DieOptionGlow.LitClass), "The Muster die's recruitment stays dark.");
            PointerGestureEvents.ClickAt(ruler);
            await Task.Delay(600, token);
            Assert.AreEqual("", board.ActionDie.DieId, "The second click releases the token and its Ruler die.");
            foreach (var deck in decks)
                Assert.IsFalse(deck.ClassListContains(DieOptionGlow.LitClass));
            Assert.IsFalse(board.Nations.Root.ClassListContains(DieOptionGlow.LitClass), "No other use lights in its place.");
        }

        // UI-391 (owner): an ability is used straight from the Characters tab. After Dáin's token, his row's Iron Guard icon
        // glows beside the Dwarvenking icon, which does not; the icon's click starts Iron Guard (its target settlements
        // glow on the map) without the character's window.
        [UnityTest]
        public IEnumerator DainsIronGuardStartsFromItsIconInTheCharactersTab() => MatchTestRoom.Wait(Icons());

        private static async Task Icons()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui391-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-ruler-token-dain", token);
            await Task.Delay(500, token);
            var board = client.Board;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("ruler-token-kings.token.dain"));
            await Task.Delay(600, token);
            board.Characters.Open();
            await Task.Delay(500, token);
            var dain = "kings.sovereign.dain";
            var guard = board.Characters.Root.Q<Button>("roster-ability-" + dain + "-iron-guard");
            var king = board.Characters.Root.Q<Button>("roster-ability-" + dain + "-dwarvenking");
            Assert.IsNotNull(guard, "Iron Guard is an icon on Dáin's row.");
            Assert.IsNotNull(king, "Dwarvenking is an icon on Dáin's row.");
            Assert.IsTrue(guard.ClassListContains(ChoiceGlow.ClassName), "The usable Iron Guard glows.");
            Assert.IsFalse(king.ClassListContains(ChoiceGlow.ClassName), "Dwarvenking cannot be used now.");
            StringAssert.StartsWith("Iron Guard\n", guard.tooltip);
            StringAssert.Contains("Click to use", guard.tooltip);
            var row = board.Characters.Root.Q<Button>("character-roster-" + dain);
            Assert.IsTrue(row.Contains(guard), "The icons stand on Dáin's row.");
            // UI-408 (owner): a short roster stands two a line, its icons larger, beside the text.
            Assert.IsTrue(board.Characters.Wide, "Two characters a line fit this roster.");
            Assert.That(guard.worldBound.width, Is.GreaterThanOrEqualTo(17f), "The icons are larger two a line.");
            Assert.That(guard.worldBound.yMax, Is.LessThanOrEqualTo(row.worldBound.yMax + 0.5f), "The icons fit inside the row.");
            Assert.That(guard.worldBound.xMax, Is.LessThanOrEqualTo(row.worldBound.xMax + 0.5f), "The icons fit inside the row.");
            LayoutTests.Save(client.Panel.targetTexture, "ui391-roster-icons.png");
            PointerGestureEvents.ClickAt(guard);
            await Task.Delay(800, token);
            Assert.Greater(board.Map.DraftOverlay.Targets.Count, 0, "Iron Guard started: its settlements glow on the map.");
            LayoutTests.Save(client.Panel.targetTexture, "ui391-iron-guard-started.png");
            // UI-392 (owner): the Reserve does not open by itself; its tab glows and a lit settlement's click opens it, the
            // camera gliding there at the same zoom, and that settlement is marked as the chosen one until its figure.
            Assert.IsFalse(board.Reinforcements.IsOpen, "No panel opens with the ability.");
            Assert.IsTrue(board.Reinforcements.Tab.ClassListContains("legal-target"), "The Reserve tab glows.");
            var region = board.Map.DraftOverlay.Targets.Contains("erebor") ? "erebor" : board.Map.DraftOverlay.Targets.First();
            var zoom = board.Map.Camera.Zoom;
            board.Map.Select(region);
            await Task.Delay(1000, token);
            Assert.IsTrue(board.Reinforcements.IsOpen, "The settlement's click opens the Reserve.");
            Assert.That(board.Map.Camera.Zoom, Is.EqualTo(zoom).Within(0.001f), "The camera keeps its zoom.");
            Assert.IsFalse(board.Map.Gliding, "The glide has ended.");
            CollectionAssert.Contains(board.Map.DraftOverlay.ChosenRegions, region, "The clicked settlement is the chosen one.");
            LayoutTests.Save(client.Panel.targetTexture, "ui392-chosen-settlement.png");
            var figure = board.Reinforcements.Root.Query<Button>(className: "reserve-token").ToList()
                .First(button => button.name.StartsWith("character-reserve-dwarves-") && button.enabledInHierarchy && button.userData != null);
            PointerGestureEvents.ClickAt(figure);
            await Task.Delay(800, token);
            CollectionAssert.Contains(board.Map.DraftOverlay.PlannedRegions, region, "The figure's click places it in the chosen settlement.");
            CollectionAssert.DoesNotContain(board.Map.DraftOverlay.ChosenRegions, region, "A full settlement no longer waits.");
            // UI-393 (owner): the next figure is dragged to another lit settlement as its own icon; its slot shows that it
            // was lifted, and the drop places it there.
            var second = board.Map.DraftOverlay.Targets.First(target => target != region);
            var next = board.Reinforcements.Root.Query<Button>(className: "reserve-token").ToList()
                .First(button => button.name.StartsWith("character-reserve-dwarves-") && button.enabledInHierarchy && button.userData != null);
            var destination = MapContourProbe.ReachRegion(board.Map, second);
            MapContourProbe.BeginDrag(board, next);
            await Task.Delay(100, token);
            var lifted = board.Root.Q<Image>("drag-figure");
            Assert.AreEqual(DisplayStyle.Flex, lifted.style.display.value, "The figure's icon follows the pointer.");
            Assert.AreSame(next.Q<Image>(className: "reserve-token-figure").sprite, lifted.sprite, "It is the pressed figure's own art.");
            Assert.IsTrue(next.ClassListContains(DraftInput.LiftedClass), "Its slot shows that it was lifted.");
            LayoutTests.Save(client.Panel.targetTexture, "ui393-lifted-figure.png");
            MapContourProbe.Drop(board, destination);
            await Task.Delay(800, token);
            Assert.AreEqual(DisplayStyle.None, lifted.style.display.value);
            Assert.IsFalse(next.ClassListContains(DraftInput.LiftedClass));
            CollectionAssert.Contains(board.Map.DraftOverlay.PlannedRegions, second, "The drop places the figure there.");
        }

        private static bool Shown(VisualElement element) => element != null && element.resolvedStyle.display == DisplayStyle.Flex &&
            element.worldBound.width > 0 && element.panel != null;
    }
}
