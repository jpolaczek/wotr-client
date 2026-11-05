using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-497 (docs/ui/RANKED_UX.md): the Find game page fits with no scrolling in the four standard layouts, holds only what
    // a ranked search asks, starts in the defaults each time, and leaves Host a game as it was.
    public class FindGameLayoutTests
    {
        private static readonly string[] Modules = { ExpansionModules.Fate, ExpansionModules.Lords, ExpansionModules.Kings };

        [UnityTest]
        public IEnumerator EveryControlFitsWithoutScrollingInAllFourLayouts()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var menu = new MainMenuScreen("PlayerWithLongUsername12345678901");
                    content.Add(menu.Root);
                    yield return Settle();
                    var size = menu.Root.Q("menu-window").worldBound.size;
                    ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                    menu.FindGame.Offer(Modules);
                    menu.FindGame.ShowRating(1532);
                    yield return Settle();

                    Assert.AreEqual("Find game", menu.Root.Q<Label>("menu-window-title").text);
                    Assert.AreEqual(size, menu.Root.Q("menu-window").worldBound.size, "The window keeps its size.");
                    var form = menu.Root.Q("find-game-form");
                    var parts = form.Query<Button>().ToList().Cast<VisualElement>().Concat(form.Query<Label>("find-rating").ToList()).ToList();
                    Assert.Greater(parts.Count, 15);
                    foreach (var part in parts)
                        Fits(part, form);
                    NothingOverlaps(parts);
                    foreach (var row in form.Query(className: "find-row").ToList())
                    {
                        var label = row.Q(row.name + "-label");
                        Assert.IsFalse(label.worldBound.Overlaps(row.Q(row.name + "-segments").worldBound), row.name + "'s label meets its segments.");
                        foreach (var text in label.Query<Label>().ToList())
                            TextFits(text);
                    }
                    foreach (var card in form.Query<Button>(className: "side-card").ToList())
                        foreach (var text in card.Query<Label>().ToList())
                            Fits(text, card);
                    Fits(form.Q<Label>("find-note"), form);
                    Assert.IsNull(form.Q<ScrollView>(), "Nothing scrolls.");
                    Assert.AreEqual(0, form.Query<TextField>().ToList().Count, "No numeric or text entry.");
                    Fits(menu.Root.Q<Button>("find-game"), menu.Root.Q("menu-actions"));
                    Fits(menu.Root.Q<Button>("quit-game"), menu.Root.Q("menu-actions"));
                    LayoutTests.Save(target, $"ui497-find-{width}-{scale:0.00}.png");

                    ReturnTestFixture.Click(menu.Root.Q<Button>("find-shadow"));
                    ReturnTestFixture.Click(menu.Root.Q<Button>("find-side-choice-enforce"));
                    ReturnTestFixture.Click(menu.Root.Q<Button>("find-long-enforce"));
                    yield return Settle();
                    LayoutTests.Save(target, $"ui497-find-chosen-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        [UnityTest]
        public IEnumerator TheFormHoldsTheDefaultsAndAChosenSideCanBeReleased()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var menu = Mount(client);
                yield return Settle();
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                menu.FindGame.Offer(Modules);
                yield return Settle();
                var form = menu.FindGame;
                Assert.IsNull(form.Side, "No side is chosen by default.");
                var request = form.Request;
                Assert.AreEqual(RankedChoice.Allow, request.Lords);
                Assert.AreEqual(RankedChoice.Allow, request.Kings);
                Assert.AreEqual(RankedChoice.Allow, request.Fate);
                Assert.AreEqual(RankedChoice.Off, request.Fast);
                Assert.AreEqual(RankedChoice.Allow, request.Standard);
                Assert.AreEqual(RankedChoice.Off, request.Long);
                var choice = menu.Root.Q("find-side-choice");
                Assert.IsFalse(choice.enabledInHierarchy, "Allow and Enforce are disabled while no side is chosen.");
                Assert.Greater(choice.worldBound.height, 0, "They are disabled, not hidden.");

                ReturnTestFixture.Click(menu.Root.Q<Button>("find-shadow"));
                yield return Settle();
                Assert.AreEqual(Side.Shadow, form.Side);
                Assert.IsTrue(choice.enabledInHierarchy);
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-side-choice-enforce"));
                Assert.AreEqual(RankedChoice.Enforce, form.Request.FactionChoice);

                ReturnTestFixture.Click(menu.Root.Q<Button>("find-shadow"));
                yield return Settle();
                Assert.IsNull(form.Side, "A second click on the chosen side releases it.");
                Assert.IsFalse(menu.Root.Q<Button>("find-shadow").ClassListContains("selected"));
                Assert.IsFalse(choice.enabledInHierarchy);
                Assert.AreEqual(RankedChoice.Allow, form.Request.FactionChoice, "A released side leaves no requirement behind.");
                Assert.Greater(choice.worldBound.height, 0);

                ReturnTestFixture.Click(menu.Root.Q<Button>("find-fp"));
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-kings-enforce"));
                ReturnTestFixture.Click(menu.Root.Q<Button>("cancel-find"));
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                menu.FindGame.Offer(Modules);
                yield return Settle();
                Assert.IsNull(form.Side, "The side is never remembered.");
                Assert.AreEqual(RankedChoice.Allow, form.Request.Kings, "Each opening starts in the defaults.");
            }
        }

        [UnityTest]
        public IEnumerator EnforcingASecondPaceLowersTheFirstAndTheLastPaceCannotBeTurnedOff()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var menu = Mount(client);
                yield return Settle();
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                yield return Settle();
                var form = menu.FindGame;
                var standardOff = menu.Root.Q<Button>("find-standard-off");
                Assert.IsFalse(standardOff.enabledInHierarchy, "Standard is the last pace on.");
                Assert.AreEqual(FindGameForm.LastPace, standardOff.tooltip);

                ReturnTestFixture.Click(menu.Root.Q<Button>("find-long-enforce"));
                Assert.IsTrue(standardOff.enabledInHierarchy, "Another pace is on now.");
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-fast-enforce"));
                Assert.AreEqual(RankedChoice.Enforce, form.Request.Fast);
                Assert.AreEqual(RankedChoice.Allow, form.Request.Long, "The second enforced pace lowers the first to Allow.");
                Assert.AreEqual(RankedChoice.Allow, form.Request.Standard);

                ReturnTestFixture.Click(standardOff);
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-long-off"));
                yield return Settle();
                Assert.AreEqual(RankedChoice.Off, form.Request.Standard);
                Assert.AreEqual(RankedChoice.Off, form.Request.Long);
                var fastOff = menu.Root.Q<Button>("find-fast-off");
                Assert.IsFalse(fastOff.enabledInHierarchy);
                ReturnTestFixture.Click(fastOff);
                Assert.AreEqual(RankedChoice.Enforce, form.Request.Fast, "A click on a disabled Off does nothing.");
            }
        }

        [UnityTest]
        public IEnumerator AnExpansionTheServerDoesNotReleaseStaysOffAndDisabled()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var menu = Mount(client);
                yield return Settle();
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                yield return Settle();
                Assert.AreEqual(RankedChoice.Off, menu.FindGame.Request.Kings, "Off until the server answers.");
                Assert.IsFalse(menu.Root.Q("find-kings").enabledInHierarchy);

                menu.FindGame.Offer(new[] { ExpansionModules.Lords });
                yield return Settle();
                Assert.AreEqual(RankedChoice.Allow, menu.FindGame.Request.Lords);
                Assert.IsTrue(menu.Root.Q("find-lords").enabledInHierarchy);
                Assert.AreEqual(RankedChoice.Off, menu.FindGame.Request.Kings);
                Assert.IsFalse(menu.Root.Q("find-kings").enabledInHierarchy);
                Assert.AreEqual(FindGameForm.NotReleased, menu.Root.Q("find-kings").tooltip);
                Assert.Greater(menu.Root.Q("find-kings").worldBound.height, 0, "Disabled, not hidden.");
            }
        }

        [UnityTest]
        public IEnumerator EverySegmentSaysWhatItMeansAndHostGameStaysAsItWas()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var menu = Mount(client);
                yield return Settle();
                ReturnTestFixture.Click(menu.Root.Q<Button>("find-game"));
                menu.FindGame.Offer(Modules);
                yield return Settle();
                var buttons = menu.Root.Q("find-game-form").Query<Button>().ToList();
                foreach (var segment in buttons.Where(button => button.ClassListContains("host-segment")))
                    Assert.IsFalse(string.IsNullOrEmpty(segment.tooltip), segment.name);
                Assert.AreEqual("Kings must be in the game.", menu.Root.Q<Button>("find-kings-enforce").tooltip);
                Assert.IsFalse(string.IsNullOrEmpty(menu.Root.Q<Button>("submit-find").tooltip));

                ReturnTestFixture.Click(menu.Root.Q<Button>("host-game"));
                yield return Settle();
                var free = menu.Root.Q<Button>("host-fp");
                Assert.IsTrue(free.ClassListContains("selected"), "Host a game still starts with a side.");
                ReturnTestFixture.Click(free);
                Assert.IsTrue(free.ClassListContains("selected"), "A second click keeps it there.");
            }
        }

        private static MainMenuScreen Mount(MountedFlowClient client)
        {
            var target = new RenderTexture(1366, 768, 24);
            target.Create();
            client.Panel.targetTexture = target;
            var menu = new MainMenuScreen("Player");
            client.Interface.Content.Clear();
            client.Interface.Content.Add(menu.Root);
            return menu;
        }

        private static void NothingOverlaps(System.Collections.Generic.IReadOnlyList<VisualElement> parts)
        {
            for (var i = 0; i < parts.Count; i++)
            for (var j = i + 1; j < parts.Count; j++)
                Assert.IsFalse(parts[i].worldBound.Overlaps(parts[j].worldBound), parts[i].name + " overlaps " + parts[j].name);
        }

        private static void TextFits(Label label)
        {
            var needed = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(needed.x, label.layout.width + 1, label.text);
        }

        private static void Fits(VisualElement element, VisualElement bounds)
        {
            Assert.IsNotNull(element);
            Assert.Greater(element.worldBound.height, 0, element.name);
            Assert.Greater(element.worldBound.width, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.worldBound.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }
    }
}
