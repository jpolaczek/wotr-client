using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client.Updates;

namespace Wotr.Client.Tests
{
    // DIST-17: the sign-in screen and the main menu light their Settings with the update's note; the note goes when the
    // update does. No other screen carries it.
    public sealed class FirstMenuUpdateTests
    {
        private static readonly UpdateNote Available = new UpdateNote("Update available", "Version 0.9.1 is available.");

        [Test]
        public void TheSignInScreenLightsItsSettings()
        {
            var screen = new GatewayScreen(() => "https://wotr.example.test", _ => { });
            AssertLit(screen.Root, false);
            screen.ShowUpdate(Available);
            AssertLit(screen.Root, true);
            screen.ShowUpdate(UpdateNote.None);
            AssertLit(screen.Root, false);
        }

        [Test]
        public void TheMainMenuLightsItsSettings()
        {
            var screen = new MainMenuScreen("Player");
            screen.ShowUpdate(Available);
            AssertLit(screen.Root, true);
            screen.ShowUpdate(UpdateNote.None);
            AssertLit(screen.Root, false);
        }

        private static void AssertLit(VisualElement root, bool lit)
        {
            var settings = root.Q<Button>("open-interface");
            var note = settings.Q<Label>("settings-update-note");
            Assert.AreEqual(lit, settings.ClassListContains(SettingsUpdateHint.GlowClass));
            Assert.AreEqual(lit ? DisplayStyle.Flex : DisplayStyle.None, note.style.display.value);
            Assert.AreEqual(lit ? "Update available" : "", note.text);
            Assert.AreEqual("Settings", settings.text, "the button keeps its own label");
        }
    }
}
