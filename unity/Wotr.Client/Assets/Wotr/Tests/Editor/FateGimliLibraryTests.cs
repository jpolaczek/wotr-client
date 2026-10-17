using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class FateGimliLibraryTests
    {
        [Test]
        public void BothKnownFrontsUseGimliIdentityAndUnknownPrivateVersionIsNeverInferred()
        {
            var view = new PlayerView();
            const string figure = "base.character.gimli";
            Assert.AreEqual(figure, CharacterLibrary.Version(view, figure));
            foreach (var version in new[] { "fate.character.gimli", "fate.character.gimli-dwarven-lord" })
            {
                Assert.AreEqual("gimli", CharacterLibrary.Identity(version));
                Assert.AreEqual("Gimli", CharacterLibrary.ShortName(version));
                view.OwnCompanions.Clear();
                view.OwnCompanions.Add(new CharacterView { Id = figure, DefinitionId = version });
                Assert.AreEqual(version, CharacterLibrary.Version(view, figure));
                Assert.AreEqual("Gimli", CharacterLibrary.Name(view, figure));
            }
        }
    }
}
