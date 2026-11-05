using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-373: a button that spends a die names the die and what it does.
    public sealed class DieUseTests
    {
        [Test]
        public void TheTooltipNamesTheDieAndTheEffect()
        {
            var view = new PlayerView
            {
                Dice =
                {
                    new DieView { Id = "fp-1", Face = DieFace.Muster, Source = "standard" },
                    new DieView { Id = "fp-ruler", Face = DieFace.Character, Source = "ruler" }
                }
            };
            Assert.AreEqual("Muster die", DieUse.Name(view, "fp-1"));
            Assert.AreEqual("Ruler die (Character)", DieUse.Name(view, "fp-ruler"));
            Assert.AreEqual("", DieUse.Name(view, ""));
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "fp-1");
            Assert.AreEqual("Use your Muster die: Draw a card from the strategy deck.",
                DieUse.Tooltip(view, draft, "Spend the selected die · Draw a card from the strategy deck."));
            Assert.AreEqual("Use your Muster die: Recruit.", DieUse.Tooltip(view, draft, "Recruit."));
            Assert.AreEqual("Discard selected cards face down.", DieUse.Tooltip(view, new TurnDraft(), "Discard selected cards face down."),
                "Without a die the effect stands alone.");
        }
    }
}
