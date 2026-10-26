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
            StringAssert.EndsWith("Or Pass to skip it.", button.tooltip);
            Assert.AreSame(FigureArtwork.Sprite(FigureAppearances.Character(Merry, Side.FreePeoples)), button.Q<Image>().sprite);
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
            Assert.IsNotNull(responses.Root.Q<Button>("effect-choice-" + GollumReveal));
            Assert.AreEqual("Click Gollum's glowing response in the Hunt window to use Reveal · Reduce damage · or Pass to skip it.",
                TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id).Guidance);
        }

        [Test]
        public void TheTrayListsNoGuideResponse()
        {
            var view = GuideWindow(Merry, MerryGuide, "Separate · Reduce damage", "");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            var tray = new DecisionTray(root) { ContextualHunt = true };
            tray.Render(view, draft, false, false);
            Assert.IsNull(root.Q<Button>("effect-choice-" + MerryGuide));
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
