using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-268: the three named Elven Rings of Lords of Middle-earth (LORDS pp. 15, 19–20; rules T24). They stand in
    // fixed console sockets — Vilya, Nenya, Narya — each in its own art. A socket glows while its Keeper ability is
    // offered now and its click uses it: Nenya in the Hunt's tile step replaces the drawn Eye, Vilya after a paid action
    // keeps that action's die. Narya's activation stays on the nation's row of the Politics panel. Every named ring
    // still changes a die like any Elven Ring.
    public static class NamedRings
    {
        public const string Vilya = "lords.ring.vilya";
        public const string Nenya = "lords.ring.nenya";
        public const string Narya = "lords.ring.narya";
        public const string NenyaReaction = "lords.nenya.replace";
        private const string Prefix = "lords.ring.";
        private static readonly string[] Order = { Vilya, Nenya, Narya };
        public static readonly string[] Keys = { "vilya", "nenya", "narya" };

        // "vilya", "nenya" or "narya"; "" for a base ring.
        public static string Key(string ringId) => Array.IndexOf(Order, ringId) < 0 ? "" : ringId.Substring(Prefix.Length);

        // The socket of a ring: the named rings in their fixed places, then base rings in id order.
        public static int Place(string ringId)
        {
            var index = Array.IndexOf(Order, ringId);
            return index < 0 ? Order.Length : index;
        }

        // The Hunt's Nenya response offered to the player now, or null.
        public static ActionOption NenyaResponse(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.Reaction && view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect && o.EligibleIds.Contains(NenyaReaction))
                : null;

        // The reaction is taken on the Nenya socket, so the Hunt window's list leaves it out.
        public static bool OnSocket(PlayerView view, string reactionId) => reactionId == NenyaReaction && NenyaResponse(view) != null;

        public static bool VilyaDecision(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.KeepActionDie && view.DecisionOwner == view.You && !view.Winner.HasValue;

        // The ring's socket glows for its Keeper ability now.
        public static bool KeeperReady(PlayerView view, string ringId) =>
            ringId == Nenya && NenyaResponse(view) != null ||
            ringId == Vilya && VilyaDecision(view) &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseVilya && o.EligibleIds.Contains(Vilya));

        // What a click on the glowing socket does.
        public static string UseNow(string ringId) => ringId == Nenya
            ? "Click to remove this Eye tile from the game and draw another. Nenya passes to the Shadow."
            : "Click to keep the die that paid for this action. Vilya passes to the Shadow.";

        public static string Keeper(string ringId) => ringId switch
        {
            Vilya => "Keeper · after a paid action, Elrond may keep its die; Vilya then passes to the Shadow.",
            Nenya => "Keeper · when a standard Eye tile is drawn, Galadriel may replace it; Nenya then passes to the Shadow.",
            Narya => "Keeper · with Gandalf outside the Fellowship in an unconquered Free Peoples Settlement, any die puts " +
                "that nation at war, on its row in the Politics panel.",
            _ => ""
        };

        // UI-268: Vilya's other answer, as a console button: the die is spent as usual and Vilya stays.
        public static ConsoleOption SpendTheDie(PlayerView view)
        {
            var decline = view.Options.FirstOrDefault(o => o.Kind == CommandKind.DeclineVilya);
            return decline == null ? null : new ConsoleOption
            {
                Id = "spend-the-die",
                Name = "action-DeclineVilya",
                Label = "Spend the die",
                Tooltip = decline.Enabled ? "Spend the die as usual; Vilya stays with the Free Peoples" : decline.Reason,
                Kind = ConsoleOptionKind.Action,
                Command = CommandKind.DeclineVilya,
                Enabled = decline.Enabled
            };
        }
    }
}
