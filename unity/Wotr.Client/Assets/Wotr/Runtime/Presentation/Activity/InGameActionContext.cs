using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum InGameActionHost { Default, Effects, Resources, Sovereign, Victory, Fellowship }

    // A historical anchor is described only by the returned, version-limited record.
    public sealed class InGameActionContext
    {
        public InGameActionHost Host { get; private set; }
        public Side? Owner { get; private set; }
        public string SubjectId { get; private set; } = "";
        public string Title { get; private set; } = "";

        public static InGameActionContext For(ActionLogView view, ActionLogStep selected)
        {
            var steps = Scope(view.Steps, selected).ToArray();
            var origin = selected ?? steps.LastOrDefault(step => step.Actor.HasValue);
            var result = new InGameActionContext { Owner = origin?.Actor ?? view.Action.Initiator };
            var outcome = steps.Select(step => step.Facts.Outcome).LastOrDefault(value => value?.Winner != null);
            if (outcome != null)
                return result.At(InGameActionHost.Victory, "Victory · " + VictoryPresentation.Reason(outcome.Victory));

            var sovereignStep = steps.LastOrDefault(step => IsSovereign(step.Command) ||
                step.Command == CommandKind.ChooseEffect && step.Facts.Sovereigns.Count > 0);
            if (sovereignStep != null)
            {
                var sovereign = steps.Where(step => step.Revision == sovereignStep.Revision)
                    .SelectMany(step => step.Facts.Sovereigns).LastOrDefault();
                return result.At(InGameActionHost.Sovereign, "Sovereigns" +
                    (sovereign == null ? "" : " · " + sovereign.Title), sovereign?.Id ?? "");
            }

            var effect = steps.SelectMany(step => step.Facts.Cards)
                .LastOrDefault(card => card.Use is ActionCardUse.TableEntered or ActionCardUse.TableRemoved);
            if (effect != null)
                return result.At(InGameActionHost.Effects, "Effects" +
                    (effect.Id.Length == 0 ? "" : " · " + CardLibrary.Title(effect.Id)), effect.Id);

            if (steps.Any(CharacterActionFacts.IsFellowshipContext))
                return result.At(InGameActionHost.Fellowship, "Fellowship");

            var resourceStep = origin != null && IsResource(origin) ? origin : null;
            if (resourceStep != null)
            {
                result.Owner = resourceStep.Actor ?? resourceStep.Costs.FirstOrDefault(cost => cost.Owner.HasValue)?.Owner ?? result.Owner;
                return result.At(InGameActionHost.Resources, "Resources");
            }
            return result;
        }

        private InGameActionContext At(InGameActionHost host, string title, string subjectId = "")
        {
            Host = host;
            Title = title;
            SubjectId = subjectId;
            return this;
        }

        private static IEnumerable<ActionLogStep> Scope(IReadOnlyList<ActionLogStep> steps, ActionLogStep selected)
        {
            if (selected == null)
                return steps;
            // A command may have automatic facts in the same committed revision.
            return steps.SkipWhile(step => step != selected).Take(1)
                .Concat(steps.SkipWhile(step => step != selected).Skip(1)
                    .TakeWhile(step => step.Automatic && !step.Actor.HasValue && step.Revision == selected.Revision));
        }

        private static bool IsSovereign(CommandKind? command) => command is CommandKind.AwakenSovereign or
            CommandKind.RemoveCorruptedSovereign or CommandKind.AttemptCorruption or CommandKind.DrawCorruptionTile or
            CommandKind.ChooseCorruptionTile;

        private static bool IsResource(ActionLogStep step) => step.Context.Kind is ActionContextKind.Resources or ActionContextKind.Dice ||
            step.Command is CommandKind.UseRing or CommandKind.UseCouncilToken or CommandKind.UseDismay ||
            step.Command == CommandKind.DrawCard && step.Costs.Any(cost => cost.Kind == ActionCostKind.RulerToken);
    }
}
