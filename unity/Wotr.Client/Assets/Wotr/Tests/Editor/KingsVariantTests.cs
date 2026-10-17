using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsVariantTests
    {
        [TestCase("kings.fp.character.23")]
        [TestCase("kings.fp.strategy.16")]
        [TestCase("kings.fp.strategy.19")]
        [TestCase("kings.fp.strategy.22")]
        [TestCase("kings.fp.strategy.24")]
        [TestCase("kings.fp.strategy.25")]
        public void VisibleCorruptionChangesDisplayedMeaningWithoutMutatingSharedCardText(string id)
        {
            var original = CardLibrary.Cards[id].Event;
            StringAssert.Contains("hidden Corruption tile", original);
            var inspector = new CardInspector();
            var view = new PlayerView { MatchId = "visible", Configuration = new MatchConfiguration
            {
                Kings = true, VisibleCorruption = true
            } };
            inspector.Update(view);
            inspector.Show(id);
            var text = inspector.Root.Q<Label>("card-text").text;
            StringAssert.Contains("non-Kings Corruption tile", text);
            StringAssert.DoesNotContain("hidden Corruption tile", text);
            Assert.AreEqual(original, CardLibrary.Cards[id].Event);
            view.Configuration.VisibleCorruption = false;
            inspector.Update(view);
            Assert.AreEqual(original, inspector.Root.Q<Label>("card-text").text);
        }

        [Test]
        public void ForkAndUndoKeepExplicitCouncilAndRulerTokenCosts()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            draft.Payload.CouncilTokenId = "council.advance-politics";
            draft.Payload.RulerTokenId = "kings.token.brand";
            var copy = draft.Fork();
            draft.Change(p => p.CouncilTokenId = "council.move-shadow-characters");
            draft.Back();
            Assert.AreEqual(copy.Payload.CouncilTokenId, draft.Payload.CouncilTokenId);
            Assert.AreEqual(copy.Payload.RulerTokenId, draft.Payload.RulerTokenId);
        }
    }
}
