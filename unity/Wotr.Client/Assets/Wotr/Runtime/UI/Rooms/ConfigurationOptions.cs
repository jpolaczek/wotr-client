using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class ConfigurationOption
    {
        public string Id { get; }
        public string Label { get; }
        public bool Value { get; }
        public Action<MatchConfiguration, bool> Set { get; }

        public ConfigurationOption(string id, string label, bool value, Action<MatchConfiguration, bool> set)
        {
            Id = id;
            Label = label;
            Value = value;
            Set = set;
        }
    }

    internal static class ConfigurationOptions
    {
        public static bool Dismay(MatchConfiguration config) => config.Dismay ?? (config.Lords && config.Kings);

        public static bool IsBase(MatchConfiguration c) => !c.Lords && !c.Kings && !c.Fate && !c.Council && !Dismay(c) &&
            !c.VisibleCorruption && !c.SequentialCorruption && !c.RemovingRulerTokens && !c.NewCities;

        public static IEnumerable<ConfigurationOption> Modules(MatchConfiguration c)
        {
            yield return new ConfigurationOption("lords", "Lords", c.Lords, (x, value) => { x.Lords = value; Dependencies(x, true); });
            yield return new ConfigurationOption("kings", "Kings", c.Kings, (x, value) => { x.Kings = value; Dependencies(x, true); });
            yield return new ConfigurationOption("fate", "Fate", c.Fate, (x, value) => { x.Fate = value; Dependencies(x, false); });
        }

        public static IEnumerable<ConfigurationOption> Variants(MatchConfiguration c)
        {
            if (c.Lords)
                yield return new ConfigurationOption("council", "Council of Rivendell", c.Council, (x, value) => x.Council = value);
            if (c.Lords && c.Kings)
            {
                yield return new ConfigurationOption("dismay", "Dismay", Dismay(c), (x, value) => { x.Dismay = value; Dependencies(x, false); });
                if (Dismay(c))
                    yield return new ConfigurationOption("removing-ruler-tokens", "Remove Ruler tokens", c.RemovingRulerTokens, (x, value) => x.RemovingRulerTokens = value);
            }
            if (c.Kings)
            {
                yield return new ConfigurationOption("visible-corruption", "Visible Corruption", c.VisibleCorruption, (x, value) => x.VisibleCorruption = value);
                yield return new ConfigurationOption("sequential-corruption", "Sequential Corruption", c.SequentialCorruption, (x, value) => x.SequentialCorruption = value);
            }
            if (c.Fate)
                yield return new ConfigurationOption("new-cities", "New Cities", c.NewCities, (x, value) => x.NewCities = value);
        }

        private static void Dependencies(MatchConfiguration config, bool rulerModuleChanged)
        {
            if (!config.Lords)
                config.Council = false;
            if (rulerModuleChanged)
                config.Dismay = config.Lords && config.Kings;
            if (!Dismay(config))
                config.RemovingRulerTokens = false;
            if (!config.Kings)
            {
                config.VisibleCorruption = false;
                config.SequentialCorruption = false;
            }
            if (!config.Fate)
                config.NewCities = false;
        }
    }
}
