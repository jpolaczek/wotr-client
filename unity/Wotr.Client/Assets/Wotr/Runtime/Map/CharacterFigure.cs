using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-402 (TABLETOP_FIGURES_UX, decision 5): one character on the map. It is its own miniature on a small round base
    // beside its region's army, never a figure of the army's marker. Built from the public projection only.
    public sealed class CharacterFigure
    {
        public string Id = "";
        public string DefinitionId = "";
        public string RegionId = "";
        public Side Side;
        public bool Garrison;
        public int Level;
        public int Leadership;
        public FigureArt Art = FigureAppearance.Companion;
        // UI-274: an Awakened king stands as his own character figure; "" for anyone else.
        public string SovereignId = "";

        public string Name => CharacterLibrary.Name(DefinitionId);

        public static CharacterFigure From(CharacterView character) => new CharacterFigure
        {
            Id = character.Id,
            DefinitionId = character.DefinitionId,
            RegionId = character.Location,
            Side = character.Side,
            Garrison = character.Garrison,
            Level = character.Level,
            Leadership = character.Leadership,
            Art = FigureAppearances.Art(character)
        };

        // Whether it stands with this force: the same region, side and field or garrison.
        public bool In(string region, Side side, bool garrison) => RegionId == region && Side == side && Garrison == garrison;
    }
}
