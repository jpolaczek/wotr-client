using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-136: what the chosen action die opens, read only from the server's options, so each place where the player
    // can use it glows: Characters (entry, ability, character move), Politics (a step, Narya, the Council token, a
    // character's political ability — UI-366),
    // the Fellowship (move or hide), hand cards, the decks, armies and recruitment. UI-196: the Characters tab lights
    // only for what its roster offers (entry, ability, a move paid by a character's ability); a plain move of
    // companions or minions is made on the map, so it does not light the tab.
    public sealed class DieOptions
    {
        public static readonly DieOptions None = new DieOptions();

        public bool Characters { get; private set; }
        public bool Politics { get; private set; }
        public bool Fellowship { get; private set; }
        public bool Cards { get; private set; }
        public bool Draw { get; private set; }
        public bool Armies { get; private set; }
        public bool Recruit { get; private set; }

        public static DieOptions For(PlayerView view, TurnDraft draft, ActionDieContext die)
        {
            if (view?.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You || die == null ||
                die.DieId.Length == 0 || draft == null)
                return None;
            return new DieOptions
            {
                Characters = CharacterAction(view, die) || CharacterMove(view, die),
                Politics = Allowed(view, die, CommandKind.AdvancePolitics) || Allowed(view, die, CommandKind.UseNarya) ||
                    Allowed(view, die, CommandKind.UseCouncilToken) || CharacterPolitics.Pays(view, die),
                // UI-389: a separation is made in the Fellowship panel, so it lights the Fellowship as its move does.
                Fellowship = Allowed(view, die, CommandKind.MoveFellowship) || Allowed(view, die, CommandKind.HideFellowship) ||
                    Allowed(view, die, CommandKind.SeparateCompanions),
                Cards = view.Hand.Any(id => CardActions.CanPlay(view, draft, die, id)),
                Draw = CardActions.CanDraw(view, draft, die, "character") || CardActions.CanDraw(view, draft, die, "strategy"),
                Armies = Allowed(view, die, CommandKind.MoveArmies) || Allowed(view, die, CommandKind.AttackArmy),
                Recruit = Allowed(view, die, CommandKind.Recruit)
            };
        }

        public bool Any => Characters || Politics || Fellowship || Cards || Draw || Armies || Recruit;

        private static bool Allowed(PlayerView view, ActionDieContext die, CommandKind kind) =>
            view.Options.Any(o => o.Kind == kind && die.Allows(o));

        // Entry and abilities list their dice per character rather than on the option. UI-366: a political ability lights
        // Politics, where its nation's track uses it, rather than Characters.
        private static bool CharacterAction(PlayerView view, ActionDieContext die) => view.Options
            .Where(o => o.Enabled && o.Kind is CommandKind.EnterCharacter or CommandKind.UseCharacterAbility)
            .SelectMany(o => o.CharacterActions).Any(c => c.Dice.Contains(die.DieId) && !CharacterPolitics.IsPolitical(c.AbilityId));

        // A move paid by a character's ability is listed under that character (ComponentActions).
        private static bool CharacterMove(PlayerView view, ActionDieContext die) => view.Options
            .Where(o => o.Enabled)
            .SelectMany(o => o.MovementCosts).Any(c => c.AbilityId.Length > 0 && c.DieId == die.DieId);
    }
}
