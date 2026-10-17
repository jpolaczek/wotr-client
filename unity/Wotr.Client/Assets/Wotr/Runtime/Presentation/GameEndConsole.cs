using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-233: the console's way back to the end of the game after View board.
    public static class GameEndConsole
    {
        public const string Name = "console-game-end";

        public static readonly IReadOnlyList<ConsoleOption> Options = new List<ConsoleOption>
        {
            new ConsoleOption
            {
                Name = Name,
                Label = "Results & replay",
                Tooltip = "Open the end of the game: the result, the game log and Save replay.",
                Kind = ConsoleOptionKind.OpenGameEnd
            }
        };
    }
}
