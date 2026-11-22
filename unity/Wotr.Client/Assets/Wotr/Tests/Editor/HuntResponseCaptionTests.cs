using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // UI-573 (owner, 2026-11-22: "which skills of which characters they use"): each Free Peoples answer to the Hunt is named
    // by who or what answered, with its printed text; any other entry has no caption.
    public class HuntResponseCaptionTests
    {
        [Test]
        public void ResponsesNameTheRingTheGuideOrARandomCompanion()
        {
            var ring = HuntResponseCaptions.For(Response(HuntResponse.Ring), "");
            Assert.IsTrue(ring.Ring);
            Assert.AreEqual("The Ring-bearers take the damage", ring.Title);
            var guide = HuntResponseCaptions.For(Response(HuntResponse.Guide), "base.character.gandalf-grey");
            Assert.AreEqual("base.character.gandalf-grey", guide.Portrait);
            StringAssert.StartsWith("The Guide is the casualty: Gandalf", guide.Title);
            var random = HuntResponseCaptions.For(Response(HuntResponse.RandomCompanion), "");
            Assert.AreEqual("A random Companion is the casualty", random.Title);
            Assert.AreEqual("", random.Portrait);
        }

        [Test]
        public void ReactionsNameTheirCardOrTheCharactersHuntAbility()
        {
            var card = HuntResponseCaptions.For(Reaction("base.fp.character.05"), "");
            Assert.AreEqual("base.fp.character.05", card.Card);
            Assert.AreEqual(CardLibrary.Title("base.fp.character.05"), card.Title);
            Assert.IsNotEmpty(card.Detail, "The card's printed text.");
            var pippin = HuntResponseCaptions.For(Reaction("base.character.pippin"), "");
            Assert.AreEqual("base.character.pippin", pippin.Portrait);
            Assert.AreEqual(CharacterLibrary.Name("base.character.pippin") + " · Guide", pippin.Title, "The ability that answers the Hunt.");
            StringAssert.Contains("Hunt damage", pippin.Detail);
            Assert.AreEqual("The Free Peoples answer the Hunt", HuntResponseCaptions.For(Reaction("unknown.source"), "").Title);
            Assert.IsNull(HuntResponseCaptions.For(new HuntReportEntryView { Kind = HuntReportKind.Rolled }, ""));
            Assert.AreEqual("Hunt damage 2 → 1", HuntResponseCaptions.DamageChange(2, 1));
            Assert.AreEqual("Corruption 0 → 2", HuntResponseCaptions.Corruption(0, 2));
        }

        private static HuntReportEntryView Response(HuntResponse response) =>
            new HuntReportEntryView { Kind = HuntReportKind.Response, Response = response };

        private static HuntReportEntryView Reaction(string source) =>
            new HuntReportEntryView { Kind = HuntReportKind.Reaction, SourceId = source };
    }
}
