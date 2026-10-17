using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Built once per projected view, never by PointerMove. Hidden state is not an input.
    public sealed class RegionViewIndex
    {
        private readonly Dictionary<string, RegionContents> regions = new Dictionary<string, RegionContents>();

        public RegionViewIndex(PlayerView view)
        {
            foreach (var unit in view.Units)
            {
                GetOrAdd(unit.Location).Units.Add(unit);
            }
            foreach (var character in view.Characters)
            {
                GetOrAdd(character.Location).Characters.Add(character);
            }
            foreach (var effect in view.Effects)
                foreach (var region in EffectsPresentation.LocalRegions(view, effect))
                    GetOrAdd(region).Effects.Add(effect);
            if (!view.Fellowship.MordorStep.HasValue && view.Fellowship.RegionId.Length > 0)
                GetOrAdd(view.Fellowship.RegionId).Fellowship = view.Fellowship;
        }

        public RegionContents Get(string id) => regions.TryGetValue(id, out var value) ? value : null;

        public bool Occupied(string id) => regions.TryGetValue(id, out var value) &&
            (value.Units.Count > 0 || value.Characters.Count > 0 || value.Fellowship != null);

        private RegionContents GetOrAdd(string id)
        {
            if (!regions.TryGetValue(id, out var value))
            {
                value = new RegionContents();
                regions.Add(id, value);
            }
            return value;
        }
    }

    public sealed class RegionContents
    {
        public List<UnitView> Units { get; } = new List<UnitView>();
        public List<CharacterView> Characters { get; } = new List<CharacterView>();
        public List<ActiveEffectView> Effects { get; } = new List<ActiveEffectView>();
        public FellowshipView Fellowship { get; set; }
    }
}
