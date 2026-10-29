using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-347 (EXP-15 D11, owner's decision): A Balrog is Come!'s order stands on its objects, replacing UI-271's console
    // buttons. The Balrog's region glows on the map; in its panel the Balrog's figure moves him alone and a figure of the
    // army he stands with moves that army with him (its move then starts at once); the console's Pass finishes
    // (OptionalDecline).
    public static class BalrogCardOrder
    {
        public const string Presentation = "BalrogOrder";
        public const string Alone = "balrog.alone";
        public const string Army = "balrog.army";
        private const string Balrog = "lords.character.balrog";

        public static bool Active(PlayerView view) => Option(view) != null;

        // UI-417: the order's options as the console's trail names them.
        public static string ChoiceName(string id) => id switch
        {
            Alone => "Balrog alone",
            Army => "Balrog with his army",
            OptionalDecline.BalrogFinish => "Finished without moving",
            _ => null
        };

        // Each lit figure and the choice its click makes.
        public static IReadOnlyDictionary<string, string> Figures(PlayerView view)
        {
            var figures = new Dictionary<string, string>();
            var option = Option(view);
            var balrog = option == null ? null : Figure(view);
            if (balrog == null)
                return figures;
            if (option.EligibleIds.Contains(Army))
                foreach (var unit in view.Units.Where(u => u.Location == balrog.Location && u.Side == balrog.Side))
                    figures[unit.Id] = Army;
            if (option.EligibleIds.Contains(Alone))
                figures[balrog.Id] = Alone;
            return figures;
        }

        // The Balrog's region while his order waits, or "".
        public static string Region(PlayerView view) => Active(view) ? Figure(view)?.Location ?? "" : "";

        public static string Offer(string choice) => choice == Alone
            ? "Click to move the Balrog alone · A Balrog is Come!"
            : "Click to move or attack with this army, the Balrog with it · A Balrog is Come!";

        public static string Hint(PlayerView view)
        {
            var option = Option(view);
            if (option == null)
                return "";
            var alone = option.EligibleIds.Contains(Alone);
            var army = option.EligibleIds.Contains(Army);
            if (!alone && !army)
                return "";
            // UI-417: the army's marker moves it with him; his figure in his region's panel moves him alone.
            if (!army)
                return "Click the Balrog's glowing army, then where he moves.";
            return "Click the Balrog's glowing army to move it with him" +
                (alone ? ", or his region and then his figure to move him alone" : "") + ".";
        }

        private static CharacterView Figure(PlayerView view) =>
            view.Characters.FirstOrDefault(c => c.DefinitionId == Balrog || c.Id == Balrog);

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
