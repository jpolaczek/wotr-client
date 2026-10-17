using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class MordorPresentationTests
    {
        [TestCase(null, "Turn activity unknown")]
        [TestCase(false, "Otherwise +1 Corruption")]
        [TestCase(true, "Move or hide attempted this turn")]
        public void ActivityUsesOnlyTheNullableServerFlag(bool? attempted, string expected)
        {
            var view = new FellowshipView { MordorStep = 0, MordorAttemptedThisTurn = attempted };
            var status = new FellowshipStatus(null);
            var data = BoardPresentation.Load();
            status.Apply(view, data);
            var label = status.Root.Q<Label>("mordor-activity");
            StringAssert.Contains(expected, label.text);
            Assert.AreEqual(DisplayStyle.Flex, label.style.display.value);
            Assert.AreEqual(attempted == false, label.ClassListContains("mordor-inactive"));
            var restored = JsonConvert.DeserializeObject<FellowshipView>(JsonConvert.SerializeObject(view));
            Assert.AreEqual(attempted, restored.MordorAttemptedThisTurn);
            status.Apply(restored, data, true);
            Assert.AreSame(label, status.Root.Q<Label>("mordor-activity"));
            Assert.AreEqual(DisplayStyle.None, label.style.display.value);
            view.MordorStep = null;
            status.Apply(view, data);
            Assert.AreEqual(DisplayStyle.None, label.style.display.value);
        }

        [Test]
        public void OlderProjectionIsUnknownEvenIfTheTrackOrCorruptionSuggestsActivity()
        {
            var view = JsonConvert.DeserializeObject<FellowshipView>("{\"mordorStep\":4,\"corruption\":11}");
            Assert.IsNull(view.MordorAttemptedThisTurn);
            var status = new FellowshipStatus(null);
            status.Apply(view, BoardPresentation.Load());
            Assert.AreEqual("Turn activity unknown", status.Root.Q<Label>("mordor-activity").text);
        }
    }
}
