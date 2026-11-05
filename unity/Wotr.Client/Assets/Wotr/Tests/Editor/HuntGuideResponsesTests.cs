using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-360 (EXP-15 D9): every Guide response in the Hunt is the Guide's portrait in the Hunt window's response row.
    public sealed class HuntGuideResponsesTests
    {
        private const string Merry = "base.character.merry";
        private const string Gollum = "base.character.gollum";
        private const string MerryGuide = "character.merry-guide";
        private const string GollumReveal = "character.gollum-reveal";

        [Test]
        public void AHobbitGuidesSeparationIsHisPortraitInTheResponseRow()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "Separate this Companion to reduce Hunt damage by one.");
            Assert.IsTrue(HuntGuideResponses.InRow(view, MerryGuide));

            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);
            Assert.AreEqual(DisplayStyle.Flex, responses.Root.style.display.value);
            var button = responses.Root.Q<Button>("effect-choice-" + MerryGuide);
            Assert.AreEqual("Merry", button.Q<Label>("hunt-response-name").text);
            Assert.AreEqual("Separate · Reduce damage", button.Q<Label>("hunt-response-consequence").text);
            StringAssert.StartsWith("Click to use Merry's response · Separate · Reduce damage", button.tooltip);
            // UI-423: the window's own Skip effect, beside the console's Pass.
            StringAssert.EndsWith("Or click Skip effect (or Pass) to skip it.", button.tooltip);
            Assert.AreSame(FigureArtwork.Sprite(FigureAppearances.Character(Merry, Side.FreePeoples)), button.Q<Image>().sprite);
            // UI-439: the figure keeps the Guide recognisable; the response's ability wears its own icon as a badge.
            Assert.AreEqual(UnityEngine.Resources.Load<UnityEngine.Texture2D>(AbilityArt.Folder + "guide-separate-to-reduce-hunt-one"),
                button.Q(HuntResponsesView.AbilityBadgeName).style.backgroundImage.value.texture);
            Assert.IsTrue(button.ClassListContains(ChoiceGlow.ClassName), "A usable response glows.");

            responses.Context(view, new TurnDraft(), false);
            Assert.IsFalse(button.enabledSelf);
            Assert.IsFalse(button.ClassListContains(ChoiceGlow.ClassName), "A response the player cannot use now does not glow.");
        }

        [Test]
        public void GollumsRevealStandsInTheRowAndTheConsoleNamesIt()
        {
            var view = GuideWindow(Gollum, GollumReveal, "Reveal · Reduce damage", "Reveal the Fellowship to reduce Hunt damage by one.");
            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);
            var reveal = responses.Root.Q<Button>("effect-choice-" + GollumReveal);
            Assert.IsNotNull(reveal);
            Assert.AreEqual(UnityEngine.Resources.Load<UnityEngine.Texture2D>(AbilityArt.Folder + "voluntary-reveal-to-reduce-hunt-one"),
                reveal.Q(HuntResponsesView.AbilityBadgeName).style.backgroundImage.value.texture, "UI-439: Gollum's reveal, not his Guide.");
            Assert.AreEqual("Click Gollum's glowing response in the Hunt window to use Reveal · Reduce damage · or Pass to skip it.",
                TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id).Guidance);
        }

        // UI-420: an untitled response does not say "response" twice, and an empty detail leaves no blank line.
        [Test]
        public void AnUntitledGuideResponseIsNamedOnce()
        {
            var view = GuideWindow(Merry, MerryGuide, "", "");
            Assert.AreEqual("Click to use Merry's Guide response\nOr click Skip effect (or Pass) to skip it.",
                HuntGuideResponses.Tooltip(view, HuntGuideResponses.Offered(view)[0]));
        }

        [Test]
        public void TheTrayListsNoGuideResponse()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "");
            Assert.IsNull(HuntTray(view).Q<Button>("effect-choice-" + MerryGuide));
        }

        // UI-423 (owner's playtest pattern, like UI-419): the Hunt window has its own Skip effect, which sends the console's
        // Pass and names the response it skips; a window without a decline offers none, and its responses do not name it.
        [Test]
        public void TheHuntWindowsSkipEffectNamesTheResponseItSkips()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "");
            var skip = HuntTray(view).Q<Button>("hunt-skip-effect");
            Assert.IsNotNull(skip, "Skip effect stands in the Hunt window.");
            Assert.AreEqual("Skip effect", skip.text);
            Assert.AreEqual("Skip Separate · Reduce damage (" + CharacterLibrary.Name(Merry) + ")", skip.tooltip);
            Assert.AreEqual("Pass · " + skip.tooltip, ReactionSkip.Tooltip(view), "Skip effect and Pass skip the same response.");

            view.Options.RemoveAll(o => o.Kind == CommandKind.DeclineReaction);
            Assert.IsNull(HuntTray(view).Q<Button>("hunt-skip-effect"), "No decline, no Skip effect.");
            Assert.AreEqual("Click to use Merry's response · Separate · Reduce damage",
                HuntGuideResponses.Tooltip(view, HuntGuideResponses.Offered(view)[0]));
        }

        // UI-423: a tray outside the Hunt and battle windows (the console's own reaction window, as Nenya's alone is,
        // UI-395) keeps the console's Pass as its only skip.
        [Test]
        public void ATrayOutsideTheHuntAndBattleWindowsOffersNoSkipEffect()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "");
            view.Hunt = null;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            new DecisionTray(root).Render(view, draft, false, false);
            Assert.IsNotNull(ReactionSkip.Option(view), "The console's Pass still declines.");
            Assert.IsNull(root.Q<Button>("hunt-skip-effect"));
            Assert.IsNull(root.Q<Button>("battle-skip-effect"));
        }

        private static VisualElement HuntTray(PlayerView view)
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            new DecisionTray(root) { ContextualHunt = true }.Render(view, draft, false, false);
            return root;
        }

        [Test]
        public void TheOpponentAndOtherWindowsOfferNoGuideResponse()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "");
            view.You = Side.Shadow;
            Assert.IsEmpty(HuntGuideResponses.Offered(view), "The opponent's window offers nothing.");
            view.You = Side.FreePeoples;
            view.Decision.Window = "hunt.table";
            Assert.IsEmpty(HuntGuideResponses.Offered(view), "Only the Guide's window.");
            view.Decision.Window = HuntGuideResponses.Window;
            view.Hunt = null;
            Assert.IsEmpty(HuntGuideResponses.Offered(view), "Only during a Hunt.");
        }

        private static PlayerView GuideWindow(string guide, string reaction, string title, string detail) => new PlayerView
        {
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "guide", Kind = DecisionKind.Reaction, Window = HuntGuideResponses.Window },
            Hunt = new HuntView { Damage = 2 },
            Fellowship = new FellowshipView { Guide = guide },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { reaction },
                    EffectChoices = { new EffectChoiceView { Id = reaction, SourceId = guide, Title = title, Detail = detail } }
                },
                new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true }
            }
        };
    }
}
