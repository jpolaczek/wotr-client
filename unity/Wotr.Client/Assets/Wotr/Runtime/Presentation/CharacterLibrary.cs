using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CharacterText
    {
        public string Id = "";
        public string Title = "";
        public string Identity = "";
        public int? Level;
        public int? Leadership;
        public string NationIcon = "";
        public bool UnlimitedMovement;
        public List<string> Abilities = new List<string>();
    }

    public static class CharacterLibrary
    {
        private static IReadOnlyDictionary<string, CharacterText> characters;
        private static IReadOnlyDictionary<string, CharacterText> Characters => characters ??= Load();

        private static IReadOnlyDictionary<string, CharacterText> Load() => new[] { "base-characters", "lords-characters", "kings-characters", "fate-characters" }
            .SelectMany(name => JsonConvert.DeserializeObject<List<CharacterText>>(Resources.Load<TextAsset>(name).text))
            .ToDictionary(c => c.Id);

        // Only look up a version when its ID is present in this player's projection.
        public static CharacterText Find(string id) => Characters.TryGetValue(id, out var item) ? item : null;
        public static string Edition(string id) => string.IsNullOrEmpty(id) ? "" : FigureGrid.Name(id.Split('.')[0]);
        public static string Name(string id) => Characters.TryGetValue(id, out var item) ? item.Title : FigureGrid.Name(id);
        public static string Description(string id) => Characters.TryGetValue(id, out var item)
            ? item.Title + (item.Level.HasValue ? " · Level " + item.Level : "") + (item.Leadership.HasValue ? " · Leadership " + item.Leadership : "") +
                "\n" + string.Join(" · ", item.Abilities.Select(FigureGrid.Name)) : Name(id);
        public static string ShortName(string id) => id switch
        {
            "base.character.merry" => "Merry",
            "base.character.pippin" => "Pippin",
            "base.character.gandalf-grey" => "Grey",
            "base.character.gandalf-white" => "White",
            "base.character.mouth-of-sauron" => "Mouth",
            "base.character.witch-king" => "Witch-king",
            "lords.character.mouth-of-sauron" => "Mouth",
            "lords.character.witch-king" => "Witch-king",
            "lords.character.elrond" => "Elrond",
            "lords.character.galadriel" => "Galadriel",
            "lords.character.balrog" => "Balrog",
            "kings.character.black-serpent" => "Black Serpent",
            "kings.character.shadow-of-mirkwood" => "Mirkwood",
            _ => Name(id).Split(' ')[0]
        };
        public static string Symbol(string id)
        {
            var words = Name(id).Replace('-', ' ').Split(' ').Where(w => w.Length > 0 && w.ToLowerInvariant() != "the" && w.ToLowerInvariant() != "of").ToArray();
            return words.Length > 1 ? "" + words[0][0] + words[words.Length - 1][0]
                : words.Length == 1 ? words[0].Substring(0, System.Math.Min(2, words[0].Length)) : "C";
        }
        public static string Identity(string id) => Characters.TryGetValue(id, out var item) ? item.Identity : id;
        public static string Version(PlayerView view, string id)
        {
            var definition = view.Characters.Concat(view.OwnCompanions).FirstOrDefault(c => c.Id == id)?.DefinitionId;
            return string.IsNullOrEmpty(definition) ? id : definition;
        }
        public static string Name(PlayerView view, string id) => view.Units.Any(u => u.Id == id && u.Kind == UnitKind.Nazgul) ? "Nazgûl" : Name(Version(view, id));
        public static string ShortName(PlayerView view, string id) => view.Units.Any(u => u.Id == id && u.Kind == UnitKind.Nazgul) ? "Nazgûl" : ShortName(Version(view, id));
    }
}
