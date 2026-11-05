using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-272 (CARD_CHOICES_UX): The Lore of the Elven Rings is used card → die → face. Its eligible dice glow in the pool
    // (a Hunt-box die stands in the pool's row), a click picks one, and the new results open in a popover over that die
    // (a special die also offers each result with the Remove symbol); the result's click sends the change. Keep results is
    // the console's button. With Kings, the Lore card in the Effects panel starts Lore before Dismay.
    public static class LoreChoice
    {
        public const string Presentation = "DieAndResult";
        public const string Keep = "lore.keep-results";
        public const string Card = "lords.sh.character.26";
        public const string Group = "Die";
        public const string CardUse = "Click to change one of your dice with The Lore of the Elven Rings before Dismay";

        public static bool Active(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue;

        public static ActionOption Option(PlayerView view) =>
            Active(view) ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseEffect && o.Enabled) : null;

        public static IEnumerable<string> Dice(PlayerView view) => Option(view)?.EffectChoices
            .Where(c => c.SelectionGroup == Group).Select(c => c.Id) ?? Enumerable.Empty<string>();

        public static string ChosenDie(PlayerView view, TurnDraft draft)
        {
            var dice = Dice(view).ToList();
            return draft.Payload.Ids.FirstOrDefault(dice.Contains) ?? "";
        }

        // The results the chosen die may take, as faces of the popover; a Remove result carries the star.
        public static IReadOnlyList<RingFace> Faces(PlayerView view, TurnDraft draft)
        {
            var die = ChosenDie(view, draft);
            var option = Option(view);
            if (die.Length == 0 || option == null)
                return Array.Empty<RingFace>();
            return option.EffectChoices.Where(c => c.SelectionGroup == "Result" && c.RequiresAny.Contains(die))
                .Select(Face).Where(face => face.HasValue).Select(face => face.Value).ToList();
        }

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (!Active(view))
                return "";
            return ChosenDie(view, draft).Length == 0
                ? "Click a lit die to change its result with Lore · Or Pass to keep your dice as they are"
                : "Click the die's new result above it · Or click another lit die";
        }

        private static readonly IReadOnlyDictionary<string, string> NoUse = new Dictionary<string, string>();

        // With Kings, Dismay may wait while Lore changes a die first: the Lore card in the Effects panel starts it.
        public static IReadOnlyDictionary<string, string> CardUses(PlayerView view) => view?.Decision?.Kind == DecisionKind.Dismay &&
            view.DecisionOwner == view.You && view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseLoreBeforeActions)
            ? new Dictionary<string, string> { [Card] = CardUse } : NoUse;

        // "lore.<Face>.<keep|remove>": the face and whether the die keeps the Remove symbol.
        private static RingFace? Face(EffectChoiceView choice)
        {
            var parts = choice.Id.Split('.');
            if (parts.Length != 3 || !Enum.TryParse(parts[1], out DieFace face))
                return null;
            return new RingFace(face, true, choice.Id, parts[2] == "remove");
        }
    }
}
