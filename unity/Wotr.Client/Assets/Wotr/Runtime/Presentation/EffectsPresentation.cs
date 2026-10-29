using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class EffectSource
    {
        public string Key;
        public string SourceId;
        public Side Owner;
        public string Kind;
        public string Title;
        public List<ActiveEffectView> Effects = new List<ActiveEffectView>();
    }

    // Only public projected data. The UI never interprets a card's rules to infer its scope.
    public static class EffectsPresentation
    {
        public static string Key(string source, Side owner) => owner + ":" + source;

        public static IReadOnlyList<EffectSource> Sources(PlayerView view)
        {
            var sources = new Dictionary<string, EffectSource>();
            foreach (var effect in view.Effects)
            {
                var key = SourceKey(sources, effect);
                if (!sources.TryGetValue(key, out var source))
                {
                    source = new EffectSource
                    {
                        Key = key, SourceId = effect.SourceId, Owner = effect.Owner,
                        Kind = effect.SourceKind.Length > 0 ? effect.SourceKind : "Card",
                        Title = Title(effect)
                    };
                    sources.Add(key, source);
                }
                source.Effects.Add(effect);
            }
            foreach (var player in view.Players)
                foreach (var id in player.TableCards)
                {
                    var key = Key(id, player.Side);
                    if (!sources.ContainsKey(key))
                        sources.Add(key, new EffectSource
                        {
                            Key = key, SourceId = id, Owner = player.Side, Kind = "Card", Title = CardLibrary.Title(id),
                            Effects = new List<ActiveEffectView> { new ActiveEffectView
                            {
                                Id = "table:" + id, SourceId = id, Owner = player.Side,
                                ExpiryDescription = "Until removed according to the source."
                            } }
                        });
                }
            return sources.Values.OrderBy(s => s.Owner).ThenBy(s => s.Title).ToList();
        }

        // UI-414 (owner): each titled ability of a source has its own button (Théoden's Corrupted ruler and Mark of the White
        // Hand); a card's effects share its title, so it keeps one button and its key.
        private static string SourceKey(Dictionary<string, EffectSource> sources, ActiveEffectView effect)
        {
            var key = Key(effect.SourceId, effect.Owner);
            return sources.TryGetValue(key, out var first) && first.Title != Title(effect) ? key + "/" + Title(effect) : key;
        }

        private static string Title(ActiveEffectView effect) => effect.Title.Length > 0 ? effect.Title : CardLibrary.Title(effect.SourceId);

        public static IEnumerable<string> LocalRegions(PlayerView view, ActiveEffectView effect)
        {
            foreach (var target in effect.Targets)
            {
                if (target.Kind == "Region")
                    yield return target.Id;
                else if (target.Kind == "Character")
                {
                    var location = view.Characters.FirstOrDefault(c => c.Id == target.Id)?.Location ??
                        view.Units.FirstOrDefault(u => u.Id == target.Id)?.Location;
                    if (!string.IsNullOrEmpty(location))
                        yield return location;
                }
            }
            if (effect.Targets.Count == 0 && effect.Modifier == "PreventShadowArmyEntry" && effect.TargetId.Length > 0)
                yield return effect.TargetId;
        }

        public static string Expiry(ActiveEffectView effect) => effect.ExpiryDescription.Length > 0 ? effect.ExpiryDescription
            : effect.Duration == "EndTurn" ? "At the end of this turn."
            : effect.Duration == "EndBattle" ? "At the end of this battle."
            : effect.Duration == "EndCombatRound" ? "At the end of this combat round." : "Until removed according to the source.";

        public static string RemovalCost(PlayerView view, ComponentAction action)
        {
            var option = view.Options.FirstOrDefault(o => o.Kind == action.Kind);
            if (action.Kind == CommandKind.DiscardProtection)
                return "Spend one die and discard one Army Event and one Character Event face down.";
            if (action.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardLore)
            {
                if (action.DieId.Length == 0 && option?.DiceWithoutRing.Count > 0)
                    return "Spend a Will of the West, or another eligible die and one chosen Elven Ring transferred to Shadow.";
                return option?.DiceWithoutRing.Contains(action.DieId) == true
                    ? "Spend this Will of the West. No Ring is required."
                    : "Spend this die and transfer one chosen Elven Ring to Shadow.";
            }
            return "Spend the selected eligible die.";
        }
    }
}
