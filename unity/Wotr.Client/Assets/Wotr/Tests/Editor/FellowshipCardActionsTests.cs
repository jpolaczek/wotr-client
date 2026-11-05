using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-249: There Is Another Way's Move and Hide are made on the Fellowship's track and Ring with one click, without
    // another die; UI-430 (owner): Stay (finish after healing) is the console's ✓, not a button.
    public sealed class FellowshipCardActionsTests
    {
        [Test]
        public void AnotherWayHidesOnTheRingMovesOnTheTrackAndFinishesWithTheConsolesConfirm()
        {
            var view = View(hidden: false);
            Assert.AreEqual(FellowshipCardActions.Hide, FellowshipCardActions.Choice(view, CommandKind.HideFellowship));
            Assert.AreEqual("", FellowshipCardActions.Choice(view, CommandKind.MoveFellowship));
            var controls = new FellowshipControls();
            controls.Refresh(view, Draft(view), new ActionDieContext(), true, false);
            Assert.IsTrue(controls.HideOffered, "The Ring glows for the card's Hide.");
            StringAssert.Contains("There Is Another Way", controls.HideTooltip);
            Assert.IsEmpty(CardConsoleOptions.For(view, Draft(view)), "No console button: Stay is the ✓.");
            var stay = FinishingOption.Option(view, Draft(view));
            Assert.AreEqual(FinishingOption.AnotherWayStay, stay.Id);
            Assert.IsTrue(stay.Enabled);
            Assert.AreEqual("Hide: click the Fellowship's glowing marker on its track · ✓ finishes after healing",
                FellowshipCardActions.Guidance(view));
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
                Decision = new DecisionView { Id = "another-way", Kind = DecisionKind.EffectChoice, Presentation = FellowshipCardActions.Presentation,
                    SourceId = "base.fp.character.10" },
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
