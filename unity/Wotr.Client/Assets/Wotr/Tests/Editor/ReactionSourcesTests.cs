using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-247: a character's reaction is used at its source — the White Rider on Gandalf the White's portrait in the Battle
    // window, Gandalf the Grey's draw on the Guide's portrait in the Fellowship panel — and the console's Pass declines
    // (UI-291).
    public sealed class ReactionSourcesTests
    {
        [Test]
        public void WhiteRiderIsUsedOnGandalfsPortraitInTheBattleWindow()
        {
            var view = WhiteRiderWindow();
            var gandalf = view.Battle.Attacker.Characters.Single(c => c.Id == "gandalf");

            Assert.AreEqual("character.white-rider", BattleReactions.Of(view, gandalf)?.Id);
            Assert.IsTrue(BattleReactions.OnPortrait(view, "character.white-rider"));
            Assert.IsTrue(BattleReactions.AllOnPortraits(view), "The tray needs no Respond step.");
            var root = new VisualElement();
            var context = new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { }, complete: _ => { });
            context.Refresh();
            var portrait = root.Q("portrait-gandalf");
            Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), "The source glows.");
            StringAssert.StartsWith("Click to use White Rider", portrait.tooltip);
            // UI-439: the portrait wears the White Rider's own icon while its click uses it.
            Assert.AreEqual(UnityEngine.Resources.Load<UnityEngine.Texture2D>(AbilityArt.Folder + "forfeit-leadership-to-cancel-nazgul-for-battle"),
                portrait.Q(BattleContext.OfferedAbilityName).style.backgroundImage.value.texture);

            var draft = new TurnDraft();
            BattleReactions.Choose(draft, "character.white-rider");
            Assert.AreEqual(CommandKind.RespondEffect, draft.Kind);
            Assert.AreEqual("character.white-rider", draft.Payload.ReactionId);
            Assert.IsTrue(CardDraft.Ready(draft, view.Options[0]), "One click completes the reaction.");

            view.Decision.Kind = DecisionKind.BattleContinue;
            Assert.IsNull(BattleReactions.Of(view, gandalf), "Outside a reaction window the portrait is only a portrait.");
        }

        // UI-423 (owner's playtest pattern, like UI-419): the battle window skips a reaction with its own Skip effect, which
        // sends the console's Pass and names the response it skips.
        [Test]
        public void TheBattleWindowSkipsTheWhiteRiderWithItsSkipEffect()
        {
            var view = WhiteRiderWindow();
            view.Battle.Stage = "BeforeReveal";
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            new DecisionTray(root).Render(view, draft, false, false);
            var skip = root.Q<Button>("battle-skip-effect");
            Assert.IsNotNull(skip, "Skip effect stands in the battle window's footer.");
            Assert.AreEqual("Skip effect", skip.text);
            Assert.AreEqual("Skip White Rider (" + CharacterLibrary.Name("base.character.gandalf-white") + ")", skip.tooltip);
            Assert.AreEqual("Pass · " + skip.tooltip, ReactionSkip.Tooltip(view), "Skip effect and Pass skip the same response.");
        }

        private static PlayerView WhiteRiderWindow()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var force = view.Battle.Attacker;
            var gandalf = new CharacterView { Id = "gandalf", DefinitionId = "base.character.gandalf-white", Side = Side.FreePeoples, Location = force.RegionId };
            view.Characters.Add(gandalf);
            force.Characters.Add(gandalf);
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = new DecisionView { Id = "white-rider", Kind = DecisionKind.Reaction, Window = "battle.start" };
            view.Options.Clear();
            view.Options.Add(Respond("character.white-rider", "base.character.gandalf-white", "White Rider"));
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true });
            return view;
        }

        [Test]
        public void GandalfTheGreysDrawIsUsedOnTheGuidesPortraitAndPassSkipsIt()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "grey-draw", Kind = DecisionKind.Reaction, Window = "event.completed" },
                Fellowship = new FellowshipView { Guide = "base.character.gandalf-grey" },
                OwnCompanions =
                {
                    new CharacterView { Id = "base.character.gandalf-grey", DefinitionId = "base.character.gandalf-grey", Level = 3 },
                    new CharacterView { Id = "base.character.strider", DefinitionId = "base.character.strider", Level = 3 }
                },
                Options =
                {
                    Respond("character.grey-draw", "base.character.gandalf-grey", "Draw matching card"),
                    new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true }
                }
            };
            var context = new ActionDieContext();
            Assert.AreEqual(CommandKind.RespondEffect, FellowshipActions.PortraitKind(view, context, "base.character.gandalf-grey"));
            Assert.IsNull(FellowshipActions.PortraitKind(view, context, "base.character.strider"));
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, new TurnDraft(), context, true);
            var gandalf = roster.Root.Q<Button>("fellowship-portrait-base.character.gandalf-grey");
            Assert.IsTrue(gandalf.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Click to use Draw matching card", gandalf.tooltip);
            Assert.IsTrue(roster.Choosing, "The panel's plaque invites a look.");

            // UI-291: the console keeps its hint; its Pass skips the draw and names it.
            Assert.IsEmpty(CardConsoleOptions.For(view, new TurnDraft()), "No option hides the console's hint.");
            var guidance = TurnConsoleState.For(view, new TurnDraft(), context, id => id).Guidance;
            StringAssert.StartsWith("Click the Guide's glowing portrait", guidance);
            StringAssert.EndsWith("or Pass to skip it.", guidance);
            Assert.AreEqual("Pass · Skip Draw matching card (Gandalf the Grey)", ReactionSkip.Tooltip(view));
            var draft = new TurnDraft();
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "base.character.gandalf-grey"));
            Assert.AreEqual(CommandKind.RespondEffect, draft.Kind);
            Assert.AreEqual("character.grey-draw", draft.Payload.ReactionId);
        }

        private static ActionOption Respond(string id, string source, string title) => new ActionOption
        {
            Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { id },
            EffectChoices = { new EffectChoiceView { Id = id, SourceId = source, Title = title, Detail = "The ability's effect." } }
        };
    }
}
