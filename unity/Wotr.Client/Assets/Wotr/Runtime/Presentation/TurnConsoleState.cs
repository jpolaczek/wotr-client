using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-160: the turn console's headline — who acts and what now — from the projection and the local draft only.
    public sealed class TurnConsoleState
    {
        public Side? Actor { get; private set; }
        public bool Own { get; private set; }
        public string Headline { get; private set; } = "";
        // What to do when the current step gives no hint of its own.
        public string Guidance { get; private set; } = "";

        public static TurnConsoleState For(PlayerView view, TurnDraft draft, ActionDieContext die, Func<string, string> regionName)
        {
            if (view.Winner.HasValue)
                return new TurnConsoleState
                {
                    Actor = view.Winner, Headline = "GAME OVER · " + Name(view.Winner.Value) + " win",
                    Guidance = "The game has ended. Click the status for the end of the game; the board and the Log stay to read."
                };
            var owner = view.DecisionOwner;
            var own = view.Decision != null && owner == view.You;
            return new TurnConsoleState
            {
                Actor = owner,
                Own = own,
                Headline = own ? RingChoice.Headline(draft) is { Length: > 0 } ring ? ring : OwnHeadline(view, draft, die)
                    : WaitingHeadline(view, owner, regionName),
                Guidance = own ? OwnGuidance(view, die) : owner.HasValue
                    ? "The opponent is deciding. The notices under the top bar show what they do."
                    : "The game moves to its next step."
            };
        }

        private static string OwnHeadline(PlayerView view, TurnDraft draft, ActionDieContext die) => view.Decision.Kind switch
        {
            DecisionKind.Action or DecisionKind.ActionSequence => "YOUR ACTION" + DieText(die) + Progress(view),
            DecisionKind.FellowshipPhase => "FELLOWSHIP PHASE",
            DecisionKind.AllocateHunt => "ALLOCATE HUNT DICE",
            _ => "YOUR DECISION · " + DecisionTitle(view, draft)
        };

        // UI-211: a card's decision names its card; other decisions name their step.
        private static string DecisionTitle(PlayerView view, TurnDraft draft)
        {
            var card = CardDecisions.CardTitle(view);
            return card.Length > 0 ? card : DecisionTray.Title(view.Decision.Kind, draft.Kind);
        }

        private static string OwnGuidance(PlayerView view, ActionDieContext die) => view.Decision.Kind switch
        {
            DecisionKind.Action when die.DieId.Length == 0 => "Choose an action die, then what it acts on: the map, a card or a panel.",
            DecisionKind.Action => "Choose what this die acts on: the highlighted map, cards and panels." + FellowshipDieGuidance(view, die),
            DecisionKind.FellowshipPhase => FellowshipGuidance(view),
            DecisionKind.AllocateHunt => "Click dice to place them in the Hunt Box, then click the Hunt plaque to allocate and roll.",
            DecisionKind.Guide => FellowshipGuideText.ChooseHint,
            DecisionKind.EffectChoice when FellowshipCardActions.Guidance(view) is { Length: > 0 } card => card,
            DecisionKind.Reaction when FellowshipActions.RosterReaction(view, view.Fellowship?.Guide ?? "") is { } reaction =>
                "Click the Guide's glowing portrait in the Fellowship panel to use " + reaction.Title + " · or Pass to skip it.",
            _ => "Click the status to open this decision."
        };

        // UI-282: a die that can move or hide the Fellowship off the Mordor track names its glowing figure on the map.
        private static string FellowshipDieGuidance(PlayerView view, ActionDieContext die)
        {
            if (view.Fellowship == null || view.Fellowship.MordorStep.HasValue)
                return "";
            if (view.Fellowship.Hidden && FellowshipActions.Option(view, die, CommandKind.MoveFellowship) != null)
                return " The glowing Fellowship figure moves the Fellowship.";
            if (!view.Fellowship.Hidden && FellowshipActions.Option(view, die, CommandKind.HideFellowship) != null)
                return " The glowing Fellowship figure hides the Fellowship.";
            return "";
        }

        // UI-236: the map's figure declares and the Mordor track's gate enters, so the guidance names them.
        private static string FellowshipGuidance(PlayerView view)
        {
            var steps = new List<string>();
            if (Offered(view, CommandKind.DeclareFellowship))
                steps.Add("Declare: click the glowing Fellowship figure on the map");
            if (Offered(view, CommandKind.EnterMordor))
                steps.Add("Enter Mordor: click the glowing gate");
            if (Offered(view, CommandKind.SelectGuide))
                steps.Add("Guide: click a glowing companion");
            steps.Add(steps.Count == 0 ? "Continue to the Hunt." : "or continue to the Hunt.");
            return string.Join(" · ", steps);
        }

        private static bool Offered(PlayerView view, CommandKind kind) => view.Options.Any(o => o.Kind == kind && o.Enabled);

        private static string DieText(ActionDieContext die) =>
            die.DieId.Length == 0 || !die.Face.HasValue ? "" : " · " + FaceName(die.Face.Value) + " die";

        // The steps of a die already spent in part: the second march, recruitment points, groups of characters.
        private static string Progress(PlayerView view)
        {
            var sequence = view.ActionSequence;
            if (sequence == null || sequence.Owner != view.You)
                return "";
            return sequence.Kind switch
            {
                CommandKind.MoveArmies => " · March " + (sequence.CompletedSteps + 1) + " of " + (sequence.CompletedSteps + sequence.Remaining),
                CommandKind.Recruit => " · " + sequence.Remaining + (sequence.Remaining == 1 ? " recruitment point left" : " recruitment points left"),
                CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters => " · " + sequence.UsedIds.Count + " moved, " + sequence.Remaining + " left",
                _ => ""
            };
        }

        private static string WaitingHeadline(PlayerView view, Side? owner, Func<string, string> regionName)
        {
            if (!owner.HasValue)
                return BoardScreen.PhaseName(view.Phase).ToUpperInvariant();
            var context = view.Battle != null ? " · battle at " + regionName(view.Battle.RegionId)
                : view.Hunt != null ? " · the Hunt"
                : view.Phase == GamePhase.Fellowship ? " · Fellowship phase"
                : view.Phase == GamePhase.HuntAllocation ? " · Hunt allocation"
                : "";
            return "WAITING FOR " + Name(owner.Value).ToUpperInvariant() + context;
        }

        public static string FaceName(DieFace face) => face switch
        {
            DieFace.ArmyMuster => "Army/Muster",
            DieFace.WillOfTheWest => "Will of the West",
            _ => face.ToString()
        };

        private static string Name(Side side) => BoardScreen.SideName(side);
    }
}
