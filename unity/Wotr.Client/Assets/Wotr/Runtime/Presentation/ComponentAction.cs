using Wotr.Contracts;

namespace Wotr.Client
{
    // One concrete owner and cost, never a generic command-menu entry.
    public sealed class ComponentAction
    {
        public string Key { get; set; } = "";
        public string Owner { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public string Region { get; set; } = "";
        public string Symbol { get; set; } = "";
        // UI-138: an action icon (Art/UI/actions) for an action without an owner, shown instead of the symbol.
        public string Icon { get; set; } = "";
        // UI-286: a character ability's picture (AbilityPictures), shown on a small icon button instead of its text.
        public string Picture { get; set; } = "";
        public string Detail { get; set; } = "";
        public CommandKind Kind { get; set; }
        public string DieId { get; set; } = "";
        public string AbilityId { get; set; } = "";
        public string CardId { get; set; } = "";
        public string TokenId { get; set; } = "";
        public CharacterActionOption Character { get; set; }
        public MovementCostOption Movement { get; set; }
        public bool Enabled { get; set; }
    }
}
