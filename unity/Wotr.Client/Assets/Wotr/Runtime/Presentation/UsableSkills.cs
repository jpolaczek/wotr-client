using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-441 (owner, 2026-11-02): "make sure the character always lights up whenever one of its skills can be used at that
    // moment", not only the skill's icon. One rule, built once per refresh, names this player's characters with a skill
    // usable now — the very condition that lights a skill icon in the character's window (CharacterAbilityChips):
    // - an action of the turn that uses one of the printed abilities of the character's current card (its own command or a
    //   variant of it, a Muster die's move or attack, Far-sighted, Narya), an ability used on its own object included
    //   (Gothmog's Reserve tokens, the Balrog's figure), offered with the chosen die or token. UI-443 (owner): before a
    //   die is chosen nothing that needs one lights, even when one die alone could pay for it;
    // - a reaction of the character offered to this player now (a battle portrait, a Hunt response, a Guide portrait, a
    //   deck, a Ring's socket), used on the object that offers it (SkillReactionPlaces).
    // Every place that draws the character lights it from this set: its Characters row and the Characters tab, its map
    // miniature (or the force an Initial king leads), its Fellowship portrait, a king's portrait and its window's header.
    public static class UsableSkills
    {
        public static readonly IReadOnlyDictionary<string, Skill> None = new Dictionary<string, Skill>();

        // One character's usable skills: their printed names, in order, and where a reaction among them is used.
        public sealed class Skill
        {
            public Skill(string figureId, IReadOnlyList<string> names, string place)
            {
                FigureId = figureId;
                Names = names;
                Place = place;
            }

            public string FigureId { get; }
            public IReadOnlyList<string> Names { get; }
            // "Click the glowing portrait in the battle window" for a reaction; "" when the skill's icon uses it.
            public string Place { get; }
        }

        // The characters (by their Characters row's figure id) with a skill usable now. components: the turn's actions
        // (ComponentActions.For); editable: the player may act now (no command is being sent).
        public static IReadOnlyDictionary<string, Skill> Characters(PlayerView view, TurnDraft draft,
            IEnumerable<ComponentAction> components, bool editable)
        {
            if (view == null || !editable || view.Winner.HasValue)
                return None;
            var found = new Found();
            if (draft?.Kind == null)
                AddActions(found, view, components);
            AddReactions(found, view);
            return found.Skills();
        }

        // A skill icon's click uses this action now: offered with the chosen die or token (UI-443: never before one).
        public static bool Usable(ComponentAction action) => action.Enabled;

        // TOOLTIP_AUDIT rule 1: why the character glows and what to click — "Usable now: Fling Them into the Fray · Click
        // its glowing icon"; a reaction names the object that offers it instead of the icon.
        public static string Line(Skill skill, string iconUse) => skill == null ? ""
            : TooltipText.Parts("Usable now: " + string.Join(", ", skill.Names), skill.Place.Length > 0 ? skill.Place : iconUse);

        // The line of each drawn piece (a map figure, a portrait) whose character has a usable skill, by the piece's id: a
        // figure's id, or one of its versions' (Gandalf the White's figure on the map, a companion's portrait, a king).
        public static IReadOnlyDictionary<string, string> Lines(PlayerView view, IReadOnlyDictionary<string, Skill> skills,
            IEnumerable<string> pieces, string iconUse)
        {
            var lines = new Dictionary<string, string>();
            if (skills.Count == 0)
                return lines;
            foreach (var piece in pieces.Distinct())
            {
                var figure = skills.ContainsKey(piece) ? piece : CharacterRosterActions.Figure(view, piece);
                if (skills.TryGetValue(figure, out var skill))
                    lines[piece] = Line(skill, iconUse);
            }
            return lines;
        }

        private static void AddActions(Found found, PlayerView view, IEnumerable<ComponentAction> components)
        {
            var rows = view.OwnCharacterRoster.Where(row => row.Side == view.You && row.CurrentDefinitionId.Length > 0)
                .GroupBy(row => row.FigureId).ToDictionary(group => group.Key, group => group.First());
            var gandalf = rows.Values.FirstOrDefault(row => row.Identity == "gandalf")?.FigureId ?? "";
            var cards = new Dictionary<string, PrintedAbilities>();
            foreach (var action in components)
            {
                if (action.Kind == CommandKind.EnterCharacter || !Usable(action))
                    continue;
                // CharacterRosterActions.For: an action belongs to its owner's row; Narya to Gandalf's.
                var figure = action.Kind == CommandKind.UseNarya ? gandalf : CharacterRosterActions.Row(view, action);
                if (rows.TryGetValue(figure, out var row) && Card(cards, row.CurrentDefinitionId).Of(action) is { } ability)
                    found.Add(figure, ability.Title, "");
            }
        }

        private static PrintedAbilities Card(Dictionary<string, PrintedAbilities> cards, string definition)
        {
            if (!cards.TryGetValue(definition, out var card))
                cards[definition] = card = new PrintedAbilities(definition);
            return card;
        }

        // A card's printed abilities, read once per refresh.
        private sealed class PrintedAbilities
        {
            private readonly IReadOnlyList<CharacterAbilityText> abilities;
            private readonly HashSet<string> ids;

            public PrintedAbilities(string definition)
            {
                abilities = CharacterAbilityLibrary.For(definition);
                ids = abilities.Select(ability => ability.Id).ToHashSet();
            }

            // The ability of the card that the action uses, as CharacterAbilitySlots places it among the card's icons; null
            // when the card prints none of it.
            public CharacterAbilityText Of(ComponentAction action)
            {
                var id = CharacterAbilitySlots.PrintedOf(action, ids);
                return id.Length == 0 ? null : abilities.First(ability => ability.Id == id);
            }
        }

        private static void AddReactions(Found found, PlayerView view)
        {
            if (view.Decision?.Kind != DecisionKind.Reaction || view.DecisionOwner != view.You)
                return;
            foreach (var option in view.Options.Where(o => o.Enabled && o.Kind == CommandKind.RespondEffect))
                foreach (var choice in option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id)))
                {
                    var figure = ReactionFigure(view, choice);
                    if (figure.Length > 0)
                        found.Add(figure, ReactionName(view, figure, choice), SkillReactionPlaces.For(view, choice.Id));
                }
        }

        // The printed ability the reaction uses ("Sorcerer"), as its character's card names it; else the response's title.
        private static string ReactionName(PlayerView view, string figure, EffectChoiceView choice)
        {
            var definition = view.OwnCharacterRoster.FirstOrDefault(row => row.FigureId == figure)?.CurrentDefinitionId ?? "";
            var printed = CharacterAbilityLibrary.For(definition).FirstOrDefault(ability => ability.Id == AbilityArt.Printed(choice.Id));
            if (printed != null)
                return printed.Title;
            return choice.Title.Length > 0 ? choice.Title : "Response";
        }

        // The Characters row of the reaction's source: its figure or one of its versions, a Guide's Hunt response its Guide.
        private static string ReactionFigure(PlayerView view, EffectChoiceView choice)
        {
            var source = HuntGuideResponses.InRow(view, choice.Id) ? HuntGuideResponses.Figure(view, choice) : choice.SourceId;
            var figure = CharacterRosterActions.Figure(view, source);
            if (figure.Length > 0 || source.Length == 0)
                return figure;
            var identity = CharacterLibrary.Identity(source);
            return view.OwnCharacterRoster.FirstOrDefault(row => row.Side == view.You && row.Identity == identity)?.FigureId ?? "";
        }

        // The skills found per character, each name once, in the order found.
        private sealed class Found
        {
            private readonly Dictionary<string, List<string>> names = new Dictionary<string, List<string>>();
            private readonly Dictionary<string, string> places = new Dictionary<string, string>();

            public void Add(string figure, string name, string place)
            {
                if (!names.TryGetValue(figure, out var list))
                {
                    names[figure] = list = new List<string>();
                    places[figure] = "";
                }
                if (!list.Contains(name))
                    list.Add(name);
                if (places[figure].Length == 0)
                    places[figure] = place;
            }

            public IReadOnlyDictionary<string, Skill> Skills() =>
                names.ToDictionary(pair => pair.Key, pair => new Skill(pair.Key, pair.Value, places[pair.Key]));
        }
    }
}
