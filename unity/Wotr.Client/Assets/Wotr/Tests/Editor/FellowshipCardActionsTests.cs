using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-249: There Is Another Way's Move and Hide are made on the Fellowship's track and Ring with one click, without
    // another die; Stay (finish after healing) keeps its console button.
    public sealed class FellowshipCardActionsTests
    {
        [Test]
        public void AnotherWayHidesOnTheRingMovesOnTheTrackAndKeepsStayInTheConsole()
        {
            var view = View(hidden: false);
            Assert.AreEqual(FellowshipCardActions.Hide, FellowshipCardActions.Choice(view, CommandKind.HideFellowship));
            Assert.AreEqual("", FellowshipCardActions.Choice(view, CommandKind.MoveFellowship));
            var controls = new FellowshipControls();
            controls.Refresh(view, Draft(view), new ActionDieContext(), true, false);
            Assert.IsTrue(controls.HideOffered, "The Ring glows for the card's Hide.");
            StringAssert.Contains("There Is Another Way", controls.HideTooltip);
            CollectionAssert.AreEqual(new[] { "fellowship.stay" }, CardConsoleOptions.For(view, Draft(view)).Select(o => o.Id).ToArray(),
                "Only Stay stays a console button.");
            StringAssert.StartsWith("Hide: click the glowing Ring", FellowshipCardActions.Guidance(view));
            var draft = new TurnDraft();
            FellowshipCardActions.Choose(view, draft, FellowshipCardActions.Hide);
            Assert.AreEqual(CommandKind.ChooseEffect, draft.Kind);
            CollectionAssert.AreEqual(new[] { FellowshipCardActions.Hide }, draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.DieId, "The card's Hide charges no die.");

            view = View(hidden: true);
            controls.Refresh(view, Draft(view), new ActionDieContext(), true, false);
            Assert.IsTrue(controls.MoveOffered, "The track glows for the card's Move.");
            Assert.IsFalse(controls.HideOffered);
            StringAssert.Contains("Event die stays spent", controls.MoveDescription);
            StringAssert.StartsWith("Move: click the glowing track", FellowshipCardActions.Guidance(view));
            Assert.IsFalse(FellowshipCardActions.OnObject(view, "fellowship.stay"));
        }

        private static TurnDraft Draft(PlayerView view)
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }

        private static PlayerView View(bool hidden)
        {
            var other = hidden ? FellowshipCardActions.Move : FellowshipCardActions.Hide;
            return new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Phase = GamePhase.ActionResolution,
                Decision = new DecisionView { Id = "another-way", Kind = DecisionKind.EffectChoice, Presentation = FellowshipCardActions.Presentation },
                Fellowship = new FellowshipView { RegionId = "rivendell", Hidden = hidden, Guide = "base.character.gollum" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1,
                        EligibleIds = { "fellowship.stay", other },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "fellowship.stay", Title = "Stay", Detail = "Finish after healing." },
                            new EffectChoiceView { Id = other, Title = hidden ? "Move" : "Hide" }
                        }
                    }
                }
            };
        }
    }
}
