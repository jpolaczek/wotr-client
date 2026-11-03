using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ComponentActions
    {
        public static IEnumerable<ComponentAction> For(PlayerView view, TurnDraft draft)
        {
            if (view.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You || view.Winner.HasValue)
                yield break;
            var council = ResourcePresentation.CouncilPending(draft) ? draft.Payload.CouncilTokenId : "";
            var locations = view.Characters.ToDictionary(c => c.Id, c => c.Location);
            var die = ActionDieContext.Of(view, draft);
            foreach (var option in DraftOptions.All(view, draft))
                foreach (var item in Owners(view, draft, option, die))
                {
                    if (council.Length > 0)
                    {
                        if (!CouncilUse(option.Kind, item, council))
                            continue;
                        if (item.Character != null)
                            item.Enabled = item.Character.Dice.Count > 0;
                    }
                    item.Kind = option.Kind;
                    item.DieId = option.Kind is CommandKind.Pass or CommandKind.UseCouncilToken ? "" : draft.Payload.DieId;
                    if (council.Length > 0 && item.Character != null && !item.Character.Dice.Contains(item.DieId))
                        item.DieId = "";
                    item.TokenId = draft.Payload.RulerTokenId;
                    if (item.Kind != CommandKind.EnterCharacter && locations.TryGetValue(item.OwnerId, out var location) &&
                        location is not "reserve" and not "eliminated" and not "fellowship")
                        item.Region = location;
                    item.Enabled &= option.Enabled;
                    yield return item;
                }
        }

        private static bool CouncilUse(CommandKind kind, ComponentAction item, string token) =>
            kind == CommandKind.UseCouncilToken && item.AbilityId == token ||
            kind == CommandKind.EnterCharacter && item.Character?.CouncilTokens.Contains(token) == true;

        // UI-385 (owner): a king owns his ability by his name ("Dáin"), never by his id ("kings.sovereign.dain").
        internal static string OwnerName(PlayerView view, string id) =>
            view.Sovereigns.FirstOrDefault(s => s.Id == id)?.Title.Split(' ')[0] ?? CharacterLibrary.ShortName(id);

        // UI-385: the printed ability an action uses ("Dwarvenking"), when it is not the action's own title; an expansion's
        // command names it with its prefix ("kings.dwarvenking").
        public static string AbilityTitle(CharacterActionOption character)
        {
            var id = character?.AbilityId ?? "";
            var printed = CharacterAbilityLibrary.Title(id) ?? CharacterAbilityLibrary.Title(id.Substring(id.IndexOf('.') + 1));
            return printed == null || printed == character.Title ? "" : printed;
        }

        // UI-461 (owner, 2026-11-03): a Will die lights what it pays as itself, or as the result the face menu chose.
        // UI-459 (owner, 2026-11-03): a die that is its ability (the Balrog die's Use Balrog) moves on the map, so its
        // moves stand as no icon.
        private static IEnumerable<ComponentAction> Owners(PlayerView view, TurnDraft draft, ActionOption option, ActionDieContext context)
        {
            var die = draft.Payload.DieId;
            var implied = context.Implied ? context.AbilityId : "";
            if (CharacterDraft.UsesCharacters(option.Kind))
                foreach (var character in option.CharacterActions)
                    yield return new ComponentAction
                    {
                        Key = "character-" + character.CharacterId + "-" + character.AbilityId,
                        Owner = OwnerName(view, character.CharacterId),
                        OwnerId = character.CharacterId,
                        Symbol = CharacterSymbol(option.Kind, character.AbilityId),
                        Picture = CharacterPicture(view, option.Kind, character),
                        Badge = option.Kind == CommandKind.EnterCharacter ? "" : AbilityBadges.For(option.Kind, character.AbilityId),
                        Definition = CharacterLibrary.Version(view, character.CharacterId),
                        Detail = TooltipText.Parts(character.Title, character.Detail),
                        Character = character, Enabled = character.Dice.Contains(die) && context.Takes(option.Kind, character)
                    };
            foreach (var cost in option.MovementCosts.Where(c => c.AbilityId.Length > 0 && c.DieId == die && c.AbilityId != implied))
                yield return new ComponentAction
                {
                    Key = option.Kind + "-" + cost.AbilityId,
                    Owner = CharacterLibrary.ShortName(cost.RequiredCharacterId.Length > 0 ? cost.RequiredCharacterId : "base.character.mouth-of-sauron"),
                    OwnerId = cost.RequiredCharacterId.Length > 0 ? cost.RequiredCharacterId : "base.character.mouth-of-sauron",
                    Symbol = option.Kind == CommandKind.AttackArmy ? "⚔" : "→",
                    Picture = AbilityArt.For(cost.AbilityId),
                    Badge = AbilityBadges.For(option.Kind, cost.AbilityId),
                    Definition = CharacterLibrary.Version(view, cost.RequiredCharacterId.Length > 0 ? cost.RequiredCharacterId : "base.character.mouth-of-sauron"),
                    Detail = (CharacterAbilityLibrary.Title(cost.AbilityId) ?? FigureGrid.Name(cost.AbilityId)) + " · " + TravelLabel(option.Kind),
                    AbilityId = cost.AbilityId, Movement = cost, Enabled = !context.Will || context.Supports(cost.Face)
                };
            if (option.RulerTravel != null && option.RulerTravel.Dice.Contains(die))
                yield return new ComponentAction
                {
                    Key = "far-sighted-" + option.Kind, Owner = "Thranduil",
                    OwnerId = "kings.sovereign.thranduil",
                    Symbol = option.Kind == CommandKind.MoveFellowship ? "◇→" : option.Kind == CommandKind.SeparateCompanions ? "♟↗" : "♟→",
                    // UI-276 (D18): Thranduil's ability picture, glowing with an eligible die; the standard move is not using it.
                    Picture = AbilityArt.For(option.RulerTravel.AbilityId),
                    Badge = AbilityBadges.For(option.Kind, option.RulerTravel.AbilityId),
                    Definition = CharacterLibrary.Version(view, "kings.sovereign.thranduil"),
                    Detail = "Far-sighted · " + TravelLabel(option.Kind),
                    // UI-456 (owner): Move companions waits for a companion outside the Fellowship.
                    AbilityId = option.RulerTravel.AbilityId, Enabled = FarSightedMoves.Offered(option),
                    Reason = FarSightedMoves.Reason(option)
                };
            if (option.Kind == CommandKind.UseCouncilToken)
                foreach (var token in option.EligibleIds)
                    yield return new ComponentAction
                    {
                        Key = token, Owner = "Council", Symbol = token == ExpansionDraft.CouncilPolitics ? "⚑" : "♟→",
                        Detail = ExpansionLabels.CouncilToken(token), AbilityId = token, Enabled = true
                    };
            if (option.Kind == CommandKind.UseNarya)
                yield return new ComponentAction
                {
                    // UI-439: Narya's own icon; its tooltip names the ring once.
                    Key = "narya", Owner = "Narya", Symbol = "○★", Picture = AbilityArt.For(CommandKind.UseNarya),
                    Detail = "Put the nation of Gandalf’s Settlement at war", Enabled = option.EligibleDice.Contains(die)
                };
            if (option.Kind is CommandKind.Pass or CommandKind.DiscardDie)
                yield return new ComponentAction
                {
                    Key = option.Kind.ToString(), Symbol = option.Kind == CommandKind.Pass ? "⇥" : "×",
                    Icon = option.Kind == CommandKind.Pass ? "pass" : "discard",
                    Detail = option.Kind == CommandKind.Pass ? "Pass · " + ConsoleActions.PassDetail : "Discard the selected die without effect",
                    Enabled = option.Kind == CommandKind.Pass || option.EligibleDice.Contains(die)
                };
            var card = RemovalCard(view, option.Kind);
            foreach (var id in option.Kind == CommandKind.DiscardProtection ? option.Targets : card.Length > 0 ? new[] { card } : Enumerable.Empty<string>())
                yield return new ComponentAction
                {
                    Key = "remove-" + id, Owner = CardLibrary.Title(id), Symbol = "×", CardId = id,
                    Detail = "Remove " + CardLibrary.Title(id) + " · Spend the selected die and any Ring or cards its removal needs",
                    Enabled = option.EligibleDice.Contains(die) && (!context.Will || WillOfTheWestUses.Pays(option.Kind, context.WillFace))
                };
        }

        public static bool Select(PlayerView view, TurnDraft draft, ComponentAction item)
        {
            item = For(view, draft).FirstOrDefault(current => current.Enabled && current.Key == item.Key && current.DieId == item.DieId && current.TokenId == item.TokenId);
            if (item == null)
                return false;
            var option = DraftOptions.For(view, draft, item.Kind);
            var council = draft.Payload.CouncilTokenId;
            draft.Choose(item.Kind, item.DieId);
            ExpansionDraft.Initialize(draft, view, option, item.TokenId);
            if (item.Character != null)
                CharacterDraft.Select(draft, view, item.Character);
            draft.Change(p =>
            {
                p.DieId = item.DieId;
                if (item.AbilityId.Length > 0)
                    p.AbilityId = item.AbilityId;
                if (item.Movement != null)
                    p.MovementCost = item.Movement.Face;
                p.Stepwise = item.Kind is CommandKind.MoveArmies or CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters;
                if (item.Kind == CommandKind.DiscardProtection)
                    p.CardId = item.CardId;
                if (item.Kind == CommandKind.EnterCharacter && item.Character?.CouncilTokens.Contains(council) == true)
                    p.CouncilTokenId = council;
                // UI-347 (D13): an entry that requires the only token it may use takes it; no button asks.
                else if (item.Kind == CommandKind.EnterCharacter && item.Character is { RequiresCouncilToken: true, CouncilTokens: { Count: 1 } })
                    p.CouncilTokenId = item.Character.CouncilTokens[0];
            });
            return true;
        }

        public static bool Immediate(PlayerView view, TurnDraft draft)
        {
            // UI-450: a ruler's recruitment is sent by the click that places its last figure (a complete final click).
            if (CharacterDraft.UsesCharacters(draft.Kind))
                return DraftOptions.For(view, draft) is { } option && CharacterDraft.Selected(draft, option) is { } choice &&
                    (choice.Recruitment.Count == 0 || choice.RulerRecruitment != null);
            return draft.Kind is CommandKind.Pass or CommandKind.DiscardDie or CommandKind.MoveFellowship or CommandKind.DiscardPalantir
                or CommandKind.DiscardBaradPalantir or CommandKind.DiscardDenethor or CommandKind.DiscardLore
                or CommandKind.DiscardGates or CommandKind.DiscardWelcome;
        }

        private static string TravelLabel(CommandKind kind) => kind switch
        {
            CommandKind.MoveArmies => "Move armies", CommandKind.AttackArmy => "Attack",
            CommandKind.MoveFellowship => "Move the Fellowship", CommandKind.SeparateCompanions => "Separate companions",
            _ => "Move characters"
        };

        // UI-286: an entry is the board pin (it enters the character, it uses no ability); UI-439: an ability's action shows
        // the ability's own icon, on the card of the version that uses it.
        public const string EntryPicture = "Wotr/Art/UI/characters/on-board";

        private static string CharacterPicture(PlayerView view, CommandKind kind, CharacterActionOption character) =>
            kind == CommandKind.EnterCharacter ? EntryPicture
                : AbilityArt.For(CharacterLibrary.Version(view, character.CharacterId), character.AbilityId);

        private static string CharacterSymbol(CommandKind kind, string ability) => kind == CommandKind.EnterCharacter ? "↑" : ability switch
        {
            "gothmog-recruit" => "+●",
            "gothmog-upgrade" => "●→◆",
            "activate-balrog" => "★",
            "deactivate-balrog" => "○",
            _ => "★"
        };

        private static string RemovalCard(PlayerView view, CommandKind kind) => kind switch
        {
            CommandKind.DiscardPalantir => "base.sh.character.21",
            CommandKind.DiscardBaradPalantir => "kings.sh.strategy.25",
            CommandKind.DiscardDenethor => view.Configuration.Kings ? "kings.sh.strategy.03" : "base.sh.strategy.03",
            CommandKind.DiscardLore => "lords.sh.character.26",
            CommandKind.DiscardGates => "lords.sh.character.27",
            CommandKind.DiscardWelcome => "kings.sh.character.25",
            _ => ""
        };
    }
}
