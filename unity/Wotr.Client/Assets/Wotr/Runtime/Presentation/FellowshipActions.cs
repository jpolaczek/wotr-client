using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class FellowshipActions
    {
        public static bool UsesPanel(PlayerView view, TurnDraft draft) => view.Battle == null && view.EventLoss == null && !HuntActions.UsesPanel(view, draft) &&
            (view.Decision?.Kind is DecisionKind.FellowshipPhase or DecisionKind.Guide or
                DecisionKind.FellowshipRoute or DecisionKind.CompanionSeparation ||
                // UI-269/UI-270: the private companion versions, the starting Guide and the homelands are chosen on its
                // portraits.
                CompanionSetupPresentation.Versions(view) || CompanionSetupPresentation.Homelands(view) ||
                draft?.Kind is CommandKind.MoveFellowship or CommandKind.HideFellowship or CommandKind.DeclareFellowship or
                    CommandKind.RevealFellowship or CommandKind.SeparateCompanions or CommandKind.SelectGuide or CommandKind.EnterMordor);

        public static ActionOption Option(PlayerView view, ActionDieContext context, CommandKind kind, TurnDraft draft = null)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You)
                return null;
            var options = draft == null ? view.Options : new[] { DraftOptions.For(view, draft, kind) }.Where(o => o != null);
            return options.FirstOrDefault(o => o.Kind == kind && o.Enabled &&
                (o.EligibleDice.Count == 0 || o.EligibleDice.Contains(context.DieId) &&
                    (kind == CommandKind.HideFellowship || context.Supports(DieFace.Character))));
        }

        public static bool Choose(PlayerView view, TurnDraft draft, ActionDieContext context, CommandKind kind)
        {
            var option = Option(view, context, kind, draft);
            if (option == null)
                return false;
            var token = draft.Payload.RulerTokenId;
            var ability = option.RulerTravel?.AbilityId == draft.Payload.AbilityId ? draft.Payload.AbilityId : "";
            draft.Choose(kind, option.EligibleDice.Count > 0 ? context.DieId : "");
            draft.Change(p => { p.RulerTokenId = token; p.AbilityId = ability; });
            return true;
        }

        public static CommandKind? PortraitKind(PlayerView view, ActionDieContext context, string id, TurnDraft draft = null)
        {
            // UI-249: in the Fellowship phase a legal portrait chooses the Guide directly; there is no Change guide mode.
            if ((view?.Decision?.Kind is DecisionKind.Guide or DecisionKind.FellowshipPhase || draft?.Kind == CommandKind.SelectGuide) &&
                Option(view, context, CommandKind.SelectGuide, draft)?.EligibleIds.Contains(id) == true)
                return CommandKind.SelectGuide;
            if (RosterChoice(view, id))
                return CommandKind.ChooseEffect;
            if (RosterReaction(view, id) != null)
                return CommandKind.RespondEffect;
            return Option(view, context, CommandKind.SeparateCompanions, draft)?.EligibleIds.Contains(id) == true
                ? CommandKind.SeparateCompanions : null;
        }

        // UI-249: The Breaking of the Fellowship chooses its Companions on the Hunt panel's roster, not in a second list.
        public static bool RosterChoice(PlayerView view, string id) => CompanionsOption(view)?.EligibleIds.Contains(id) == true;

        public static ActionOption CompanionsOption(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == CompanionsPresentation &&
            view.DecisionOwner == view.You && HuntActions.UsesPanel(view)
                ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseEffect && o.Enabled) : null;

        public const string CompanionsPresentation = "FellowshipCompanions";

        // UI-247: the Guide's own reaction outside a battle or the Hunt (Gandalf the Grey's draw after an Event card) is
        // used on the Guide's portrait in the Fellowship panel.
        public static EffectChoiceView RosterReaction(PlayerView view, string id)
        {
            if (view?.Decision?.Kind != DecisionKind.Reaction || view.DecisionOwner != view.You || view.Battle != null ||
                view.Hunt != null || view.EventLoss != null || view.Fellowship == null || !FellowshipGuideText.IsGuide(view.Fellowship, id))
                return null;
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.RespondEffect && o.Enabled);
            var choices = option?.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) &&
                (c.SourceId == id || CharacterLibrary.Identity(c.SourceId) == id || FellowshipGuideText.IsGuide(view.Fellowship, c.SourceId))).ToList();
            return choices?.Count == 1 ? choices[0] : null;
        }

        // Whether this reaction is used on the Guide's portrait, so the console does not repeat it.
        public static bool ReactionOnRoster(PlayerView view, string reactionId) =>
            view?.Fellowship != null && RosterReaction(view, view.Fellowship.Guide)?.Id == reactionId;

        public static bool Portrait(PlayerView view, TurnDraft draft, ActionDieContext context, string id)
        {
            var kind = PortraitKind(view, context, id, draft);
            if (!kind.HasValue)
                return false;
            if (draft.Kind != kind)
                Choose(view, draft, context, kind.Value);
            if (kind == CommandKind.SelectGuide)
                draft.Change(p => p.GuideId = id);
            else if (kind == CommandKind.ChooseEffect)
                EffectChoiceDraft.Choose(draft, CompanionsOption(view), id);
            else if (kind == CommandKind.RespondEffect)
                ReactionDraft.Choose(draft, RosterReaction(view, id).Id);
            else
                TravelDraft.ToggleCompanion(draft, view, DraftOptions.For(view, draft), id);
            return true;
        }

        public static bool Immediate(CommandKind? kind) => kind == CommandKind.SelectGuide;

        public static string ActionHint(PlayerView view, TurnDraft draft, ActionDieContext context)
        {
            if (view.Decision == null || view.DecisionOwner != view.You)
                return view.DecisionOwner.HasValue ? "Waiting for " + (view.DecisionOwner == Side.FreePeoples ? "Free Peoples" : "Shadow")
                    : "No pending action";
            var kind = view.Fellowship.Hidden ? CommandKind.MoveFellowship : CommandKind.HideFellowship;
            var option = DraftOptions.For(view, draft, kind);
            if (option == null || !option.Enabled)
                return "No Fellowship action in this decision";
            if (Option(view, context, kind, draft) == null)
                return (view.Fellowship.Hidden ? "" : "Hide first · ") + "Select a Character die";
            // UI-220: the glowing status plaque is the hide.
            if (!view.Fellowship.Hidden)
                return "Click the glowing Fellowship plaque to hide here; no progress or extra Hunt die.";
            // UI-110: the glowing track symbol is the move.
            return view.Fellowship.MordorStep.HasValue ? "Click the glowing Mordor track symbol · Resolve a Hunt tile before any advance."
                : "Click the glowing track symbol · Progress +1 and resolve the Hunt; the map position stays unchanged.";
        }
    }
}
