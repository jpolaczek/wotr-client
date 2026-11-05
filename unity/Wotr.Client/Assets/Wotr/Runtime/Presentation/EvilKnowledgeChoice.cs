using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-347 (EXP-15 D12, owner's decision): the Mouth of Sauron's Evil Knowledge, offered while a siege may go on, is used
    // on his portrait in the Battle window's force column, as the UI-247 reactions; no button stands beside Extend / Stop.
    public static class EvilKnowledgeChoice
    {
        public const string Ability = "evil-knowledge";
        public const string Offer = "Click to extend the siege by one round without downgrading an Elite · Evil Knowledge, once per action";

        public static bool Offered(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.SiegeContinuation && view.DecisionOwner == view.You && !view.Winner.HasValue &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseEvilKnowledge);

        // This portrait carries Evil Knowledge now.
        public static bool On(PlayerView view, CharacterView character) =>
            Offered(view) && CharacterLibrary.Find(character.DefinitionId)?.Abilities.Contains(Ability) == true;

        // A portrait of the battle carries it, so the Battle window needs no button for it.
        public static bool OnPortrait(PlayerView view) => Offered(view) && view.Battle != null &&
            new[] { view.Battle.Attacker, view.Battle.Defender }.Where(f => f != null).SelectMany(f => f.Characters).Any(c => On(view, c));
    }
}
