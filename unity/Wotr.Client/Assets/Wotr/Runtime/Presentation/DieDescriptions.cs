using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-97: short die descriptions — the face, what it can do and its state, without paragraphs or rulebook
    // references. The options follow BASE pp.20–21 (docs/rules/LOGIC.md); legality still comes from the server.
    public static class DieDescriptions
    {
        public static string Face(DieFace? face) => face switch
        {
            DieFace.Character => "Character",
            DieFace.Army => "Army",
            DieFace.Muster => "Muster",
            DieFace.ArmyMuster => "Army / Muster",
            DieFace.Event => "Event",
            DieFace.WillOfTheWest => "Will of the West",
            DieFace.Eye => "Eye",
            DieFace.CardDraw => "Card Draw",
            DieFace.UseBalrog => "Use Balrog",
            null => "Not rolled yet",
            _ => face.ToString()
        };

        public static IReadOnlyList<string> Options(DieFace? face, Side owner) => face switch
        {
            DieFace.Character => owner == Side.FreePeoples
                ? new[] { "Move or hide the Fellowship", "Move or separate companions", "Move or attack with a led army", "Play a Character event" }
                : new[] { "Move Minions and Nazgûl", "Move or attack with a led army", "Play a Character event" },
            DieFace.Army => new[] { "Move up to two armies", "Attack with one army", "Play an Army event" },
            DieFace.Muster => owner == Side.FreePeoples
                ? new[] { "Advance a nation politically", "Recruit in a nation at war", "Play a Muster event" }
                : new[] { "Advance a nation politically", "Recruit in a nation at war", "Bring a Minion into play", "Play a Muster event" },
            DieFace.ArmyMuster => new[] { "Use as Army or as Muster" },
            DieFace.Event => new[] { "Draw an event card", "Play any event card" },
            DieFace.WillOfTheWest => new[] { "Use as any other result", "Bring Gandalf the White or Aragorn into play" },
            DieFace.Eye => new[] { "Goes to the Hunt Box and strengthens the Hunt" },
            DieFace.CardDraw => new[] { "Draw an event card" },
            DieFace.UseBalrog => new[] { "Activate or use the Balrog" },
            _ => new string[0]
        };

        // One short line of state; nothing for an available, unremarkable die.
        public static string State(DieView die)
        {
            var parts = new List<string>();
            var location = Location(die);
            if (location.Length > 0)
                parts.Add(location);
            if (die.SpecialDieChosen)
                parts.Add("Chosen");
            if (die.RemoveSymbol)
                parts.Add("★ Remove");
            if (die.PermanentlyRemoved)
                parts.Add("Removed for the game");
            return string.Join(" · ", parts);
        }

        public static string Title(DieView die) =>
            (ExpansionLabels.Special(die) ? ExpansionLabels.DieSource(die) + " · " : "") + Face(die.Face);

        // A single line for lists such as the Spent pool.
        public static string Summary(DieView die)
        {
            var state = State(die);
            return state.Length == 0 ? Title(die) : Title(die) + " · " + state;
        }

        public static string Tooltip(DieView die, IReadOnlyCollection<string> usesNow = null)
        {
            var lines = new List<string> { Title(die) };
            if (die.Location is not (DieLocation.Hunt or DieLocation.Discarded or DieLocation.Removed or DieLocation.FellowshipBox) &&
                !die.PermanentlyRemoved)
                lines.AddRange(Options(die.Face, die.Owner).Select(option => "• " + option));
            if (usesNow != null && usesNow.Count > 0)
                lines.Add("Now · " + string.Join(" · ", usesNow));
            var state = State(die);
            if (state.Length > 0)
                lines.Add(state);
            return string.Join("\n", lines);
        }

        private static string Location(DieView die) => die.Location switch
        {
            DieLocation.Resolving => "Paying for this action",
            DieLocation.Spent => die.SpentWith.HasValue && die.SpentWith != die.Owner
                ? "Spent by " + BoardScreen.SideName(die.SpentWith.Value) : "Spent",
            DieLocation.Hunt => die.HuntOrigin.Length > 0 ? "In the Hunt Box · " + FigureGrid.Name(die.HuntOrigin) : "In the Hunt Box",
            DieLocation.Discarded => "Set aside",
            DieLocation.Removed => "Removed from play",
            DieLocation.FellowshipBox => "In the Fellowship box",
            DieLocation.AwaitingChoice => "Waiting for the special-die choice",
            _ => ""
        };
    }
}
