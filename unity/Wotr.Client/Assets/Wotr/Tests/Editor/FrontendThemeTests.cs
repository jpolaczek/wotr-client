using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // Sign-in, the main menu and the waiting room share one look: the map backdrop (MENU-15) and the credit of the
    // original game (MENU-16), neither of which takes input.
    public sealed class FrontendThemeTests
    {
        private static VisualElement[] Screens() =>
            new[] { new GatewayScreen().Root, new MainMenuScreen("Player").Root, new WaitingRoomScreen().Root };

        [Test]
        public void EveryScreenBeforeTheBoardStartsWithOneMapBackdrop()
        {
            foreach (var root in Screens())
            {
                var backdrop = root[0];
                Assert.AreEqual("frontend-backdrop", backdrop.name, root.name);
                Assert.IsTrue(backdrop.ClassListContains("frontend-backdrop"), root.name);
                Assert.AreEqual(PickingMode.Ignore, backdrop.pickingMode, root.name);
                Assert.AreEqual(0, backdrop.childCount, root.name);
                Assert.AreEqual(1, root.Query(className: "frontend-backdrop").ToList().Count, root.name);
            }
        }

        [Test]
        public void EveryScreenBeforeTheBoardCreditsTheOriginalGame()
        {
            foreach (var root in Screens())
            {
                var credits = root.Q<Label>("frontend-credits");
                Assert.IsNotNull(credits, root.name);
                Assert.AreEqual("Original game credited to Roberto Di Meglio, Marco Maggi and Francesco Nepitello · Published by Ares Games",
                    credits.text, root.name);
                Assert.AreEqual(PickingMode.Ignore, credits.pickingMode, root.name);
                Assert.AreSame(credits, root[1], root.name);
            }
        }
    }
}
