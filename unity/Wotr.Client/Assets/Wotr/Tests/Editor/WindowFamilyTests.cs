using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class WindowFamilyTests
    {
        private VisualElement root, effects, politics, card, detail, topBar;
        private Button effectEntry, politicsLink, cardLink, detailLink, topBarLink;
        private List<string> closed;
        private WindowFamily family;

        [SetUp]
        public void Build()
        {
            root = new VisualElement();
            effects = Window("effects", out effectEntry);
            politics = Window("politics", out politicsLink);
            card = Window("card", out cardLink);
            detail = Window("detail", out detailLink);
            topBar = Window("topbar", out topBarLink);
            closed = new List<string>();
            // The top bar is not a closable board window.
            family = new WindowFamily(root, () => new[] { effects, politics, card, detail });
        }

        [Test]
        public void TheOwnerIsTheWindowThatContainsTheOpeningControl()
        {
            family.Opened(card, effectEntry, () => Close(card));
            Assert.AreSame(effects, family.OwnerOf(card));
            Assert.IsTrue(family.Watching);
            family.Opened(detail, topBarLink, () => Close(detail));
            Assert.IsNull(family.OwnerOf(detail), "A detail opened from the top bar stays independent.");
            family.Opened(detail, null, () => Close(detail));
            Assert.IsNull(family.OwnerOf(detail));
        }

        [Test]
        public void ClosingTheOwnerClosesItsChildrenInCascade()
        {
            family.Opened(card, effectEntry, () => Close(card));
            family.Opened(detail, cardLink, () => Close(detail));
            Assert.AreSame(card, family.OwnerOf(detail));
            family.CloseOrphans();
            Assert.IsEmpty(closed, "Nothing closes while the owners stay open.");
            effects.style.display = DisplayStyle.None;
            family.CloseOrphans();
            CollectionAssert.AreEqual(new[] { "card", "detail" }, closed);
            Assert.IsFalse(family.Watching, "The per-frame check stops without linked children.");
        }

        [Test]
        public void AnUnrelatedWindowOrAChildClosedByItselfCausesNoOtherClose()
        {
            family.Opened(card, effectEntry, () => Close(card));
            politics.style.display = DisplayStyle.None;
            family.CloseOrphans();
            Assert.IsEmpty(closed);
            Assert.IsTrue(family.Watching);
            card.style.display = DisplayStyle.None;
            family.CloseOrphans();
            Assert.IsEmpty(closed, "A child that closed itself is only forgotten.");
            Assert.IsFalse(family.Watching);
            card.style.display = DisplayStyle.Flex;
            effects.style.display = DisplayStyle.None;
            family.CloseOrphans();
            Assert.IsEmpty(closed);
        }

        [Test]
        public void ReopeningElsewhereMovesTheOwnerAndNavigationInsideTheChildKeepsIt()
        {
            family.Opened(detail, politicsLink, () => Close(detail));
            family.Opened(detail, detailLink, () => Close(detail));
            Assert.AreSame(politics, family.OwnerOf(detail), "Another topic inside the detail keeps the panel that opened it.");
            family.Opened(detail, cardLink, () => Close(detail));
            Assert.AreSame(card, family.OwnerOf(detail));
            politics.style.display = DisplayStyle.None;
            family.CloseOrphans();
            Assert.IsEmpty(closed);
            family.Forget(detail);
            Assert.IsFalse(family.Watching);
        }

        private VisualElement Window(string name, out Button control)
        {
            var window = new VisualElement { name = name };
            var body = new VisualElement();
            control = new Button { name = name + "-control" };
            body.Add(control);
            window.Add(body);
            root.Add(window);
            return window;
        }

        private void Close(VisualElement window)
        {
            window.style.display = DisplayStyle.None;
            closed.Add(window.name);
        }
    }
}
